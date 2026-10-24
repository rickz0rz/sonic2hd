// Lists, unpacks and packs the game's resource files (.dat).
//   dotnet run --project tools/s2hd-data -- list   <file.dat>
//   dotnet run --project tools/s2hd-data -- unpack <file.dat> <folder>
//   dotnet run --project tools/s2hd-data -- pack   <folder> <file.dat> [--base <original.dat>]
//   dotnet run --project tools/s2hd-data -- import-special-stages <s2disasm folder> <folder>
//   dotnet run --project tools/s2hd-data -- import-level <zone> <s2disasm folder> <folder>
// unpack writes each resource as a normal file (.png, .ogg, .map.xml and so on) that you can edit. pack makes a
// .dat file from such a folder. With --base, pack writes only the resources that are new or changed compared to
// the original, which gives a small file for the game's mods folder. import-special-stages converts the special
// stage layouts of the original game from a Sonic 2 disassembly (github.com/sonicretro/s2disasm) into that folder.
// import-level makes the map (with collision) and the object binding of a zone, for example CNZ, from the same
// disassembly.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SonicOrca.Core;
using SonicOrca.Core.SpecialStage;
using SonicOrca.HelperLibraries.Png;
using SonicOrca.Original;
using SonicOrca.Resources;

ResourceType.RegisterTypesFrom(typeof(PngResourceType).Assembly);

try
{
	return args switch
	{
		["list", string file] => List(file),
		["unpack", string file, string folder] => Unpack(file, folder),
		["pack", string folder, string file] => Pack(folder, file, null),
		["pack", string folder, string file, "--base", string baseFile] => Pack(folder, file, baseFile),
		["import-special-stages", string disassembly, string folder] => ImportSpecialStages(disassembly, folder),
		["import-level", string zone, string disassembly, string folder] => ImportLevel(zone, disassembly, folder),
		_ => Usage(),
	};
}
catch (Exception ex) when (ex is ResourceException or IOException or UnauthorizedAccessException)
{
	Console.Error.WriteLine("Error: " + ex.Message);
	return 1;
}

static int List(string file)
{
	foreach (var (key, resource) in Scan(file).GetResourceListing().OrderBy(r => r.Key, StringComparer.Ordinal))
	{
		string compressed = resource.Source.Compressed ? "gzip" : "";
		Console.WriteLine($"{resource.Identifier,-18} {resource.Source.Size,10} {compressed,-4} {key}");
	}
	return 0;
}

static int Unpack(string file, string folder)
{
	if (Directory.Exists(folder) && Directory.EnumerateFileSystemEntries(folder).Any())
	{
		Console.Error.WriteLine($"Error: The folder {folder} is not empty. Use an empty folder.");
		return 1;
	}
	int count = ResourceDirectory.Export(Scan(file), folder);
	Console.WriteLine($"Wrote {count} files to {folder}.");
	return 0;
}

static int Pack(string folder, string file, string baseFile)
{
	if (!Directory.Exists(folder))
	{
		Console.Error.WriteLine($"Error: The folder {folder} does not exist.");
		return 1;
	}
	var skipped = new List<string>();
	ResourceTree tree = ResourceDirectory.Import(folder, skipped);
	foreach (string path in skipped)
	{
		Console.Error.WriteLine($"Warning: Not packed, unknown file type: {path}");
	}
	if (baseFile != null)
	{
		tree = ResourceComparison.GetChanges(tree, Scan(baseFile));
	}
	int count = tree.GetResourceListing().Count;
	if (count == 0)
	{
		Console.Error.WriteLine(baseFile != null
			? "Error: No resources are different from the base file. The tool did not write a file."
			: "Error: The folder has no resources. The tool did not write a file.");
		return 1;
	}
	new ResourceFile(file).Write(tree);
	Console.WriteLine($"Wrote {count} resources to {file}.");
	return 0;
}

static int ImportSpecialStages(string disassembly, string folder)
{
	if (!File.Exists(Path.Combine(disassembly, SpecialStageLayouts.LayoutsFile)) || !File.Exists(Path.Combine(disassembly, SpecialStageLayouts.ObjectsFile)))
	{
		Console.Error.WriteLine($"Error: The folder {disassembly} does not contain the special stage data of s2disasm.");
		return 1;
	}
	List<HalfPipeStageData> stages = SpecialStageLayouts.ReadFromDisassembly(disassembly);
	string output = Path.Combine(folder, "SONICORCA", "SPECIALSTAGE");
	Directory.CreateDirectory(output);
	for (int i = 0; i < stages.Count; i++)
	{
		using var writer = new StreamWriter(Path.Combine(output, $"STAGE{i + 1}.xml"));
		stages[i].WriteXml(writer);
	}
	Console.WriteLine($"Wrote {stages.Count} special stages to {output}.");
	return 0;
}

static int ImportLevel(string zoneName, string disassembly, string folder)
{
	if (!OriginalZone.All.TryGetValue(zoneName, out OriginalZone zone))
	{
		Console.Error.WriteLine($"Error: The zone {zoneName} is not known. Known zones: {string.Join(", ", OriginalZone.All.Keys)}.");
		return 1;
	}
	if (!File.Exists(Path.Combine(disassembly, zone.ChunksFile)))
	{
		Console.Error.WriteLine($"Error: The folder {disassembly} does not contain the level data of s2disasm.");
		return 1;
	}
	ImportedLevel level = LevelImport.Import(zone, disassembly);
	string output = Path.Combine(new[] { folder }.Concat(zone.Key.Split('/')).ToArray());
	Directory.CreateDirectory(output);
	new LevelMapWriter(level.Map).Save(Path.Combine(output, "MAP.map.xml"));
	new LevelBindingWriter(level.Binding).Save(Path.Combine(output, "BINDING.binding.xml"));
	// The tiles in front of the characters: a new tile set that adds them to the HD tiles
	if (level.TileSet != null)
	{
		string tileSetFolder = Path.Combine(output, "TILESET");
		Directory.CreateDirectory(tileSetFolder);
		File.WriteAllText(Path.Combine(tileSetFolder, "_.tileset.xml"), level.TileSet);
		File.WriteAllBytes(Path.Combine(tileSetFolder, "PRIORITY.png"), level.PriorityTexture);
	}
	// The art of the objects that the game data does not have
	string KeyFolder(string key) => Path.Combine(new[] { folder }.Concat(key.Split('/')).ToArray());
	foreach (ImportedSprite sprite in level.Sprites)
	{
		string spriteFolder = KeyFolder(sprite.ObjectKey);
		Directory.CreateDirectory(spriteFolder);
		File.WriteAllBytes(Path.Combine(spriteFolder, "TEXTURE.png"), sprite.Texture);
		File.WriteAllText(Path.Combine(spriteFolder, "ANIGROUP.anigroup.xml"), sprite.AnimationGroup);
	}
	foreach ((string key, byte[] texture) in level.Textures ?? new Dictionary<string, byte[]>())
	{
		string file = KeyFolder(key) + ".png";
		Directory.CreateDirectory(Path.GetDirectoryName(file));
		File.WriteAllBytes(file, texture);
	}
	// A resource that names the code of each new object: in its folder when it has art, else next to the folder
	foreach ((string key, string codeNamespace) in level.NewObjects)
	{
		string resourceFile = Directory.Exists(KeyFolder(key)) ? Path.Combine(KeyFolder(key), "_.object.cs") : KeyFolder(key) + ".object.cs";
		Directory.CreateDirectory(Path.GetDirectoryName(resourceFile));
		File.WriteAllText(resourceFile, "#use " + codeNamespace);
	}
	Console.WriteLine($"Wrote {zone.Name} to {output}: {level.Map.CollisionVectors.Count} collision lines, {level.Binding.ObjectPlacements.Count} objects.");
	foreach (string note in level.Notes)
	{
		Console.WriteLine("  " + note);
	}
	return 0;
}

static ResourceTree Scan(string file)
{
	if (!File.Exists(file))
	{
		throw new ResourceException($"The file {file} does not exist.");
	}
	return new ResourceFile(file).Scan();
}

static int Usage()
{
	Console.Error.WriteLine("""
		Usage:
		  s2hd-data list   <file.dat>
		  s2hd-data unpack <file.dat> <folder>
		  s2hd-data pack   <folder> <file.dat> [--base <original.dat>]
		  s2hd-data import-special-stages <s2disasm folder> <folder>
		  s2hd-data import-level <zone> <s2disasm folder> <folder>
		""");
	return 2;
}
