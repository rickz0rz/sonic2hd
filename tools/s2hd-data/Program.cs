// Lists, unpacks and packs the game's resource files (.dat).
//   dotnet run --project tools/s2hd-data -- list   <file.dat>
//   dotnet run --project tools/s2hd-data -- unpack <file.dat>... <folder> [--images]
//   dotnet run --project tools/s2hd-data -- pack   <folder> <file.dat> [--base <original.dat>]
//   dotnet run --project tools/s2hd-data -- prune  <folder> <file.dat>...
//   dotnet run --project tools/s2hd-data -- import-special-stages <s2disasm folder> <folder>
//   dotnet run --project tools/s2hd-data -- import-level <zone> <s2disasm folder> <folder>
// unpack writes each resource as a normal file (.png, .ogg, .map.xml and so on) that you can edit. With more than
// one .dat file, a later file replaces the resources of an earlier one, as in the game. With --images, unpack
// writes only the images. The game loads such a folder from its mods folder. prune deletes the files of a folder
// that are the same as in the .dat files, so that the folder keeps only the changes. pack makes a
// .dat file from such a folder. With --base, pack writes only the resources that are new or changed compared to
// the original, which gives a small file for the game's mods folder. import-special-stages converts the special
// stage layouts of the original game from a Sonic 2 disassembly (github.com/sonicretro/s2disasm) into that folder.
// import-level makes the map (with collision) and the object binding of a zone, for example CNZ, from the same
// disassembly. import-music plays the songs that the demo data does not have on emulated sound chips, and uses
// ffmpeg (or S2HD_FFMPEG) to encode them as Ogg Vorbis with a loop point.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SonicOrca.Core;
using SonicOrca.Core.SpecialStage;
using SonicOrca.HelperLibraries.Png;
using SonicOrca.Original;
using SonicOrca.Original.Music;
using SonicOrca.Resources;

ResourceType.RegisterTypesFrom(typeof(PngResourceType).Assembly);

try
{
	return args switch
	{
		["list", string file] => List(file),
		["unpack", .. var unpackArgs] when unpackArgs.Count(a => a != "--images") >= 2 =>
			Unpack(unpackArgs.Where(a => a != "--images").SkipLast(1).ToArray(), unpackArgs.Last(a => a != "--images"), unpackArgs.Contains("--images")),
		["prune", string pruneFolder, .. var baseFiles] when baseFiles.Length > 0 => Prune(pruneFolder, baseFiles),
		["pack", string folder, string file] => Pack(folder, file, null),
		["pack", string folder, string file, "--base", string baseFile] => Pack(folder, file, baseFile),
		["import-special-stages", string disassembly, string folder] => ImportSpecialStages(disassembly, folder),
		["import-level", string zone, string disassembly, string folder] => ImportLevel(zone, disassembly, folder),
		["import-demos", string disassembly, string folder] => ImportDemos(disassembly, folder),
		["import-music", string disassembly, string folder] => ImportMusic(disassembly, folder),
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

static int Unpack(string[] files, string folder, bool imagesOnly)
{
	if (Directory.Exists(folder) && Directory.EnumerateFileSystemEntries(folder).Any())
	{
		Console.Error.WriteLine($"Error: The folder {folder} is not empty. Use an empty folder.");
		return 1;
	}
	ResourceTree tree = ScanAll(files);
	Func<Resource, bool> include = imagesOnly ? IsImage : null;
	int count = ResourceDirectory.Export(tree, folder, include);
	Console.WriteLine($"Wrote {count} files to {folder}.");
	return 0;
}

static bool IsImage(Resource resource) => resource.Identifier is ResourceTypeIdentifier.TexturePNG or ResourceTypeIdentifier.TextureBMP
	or ResourceTypeIdentifier.TextureGIF or ResourceTypeIdentifier.TextureJPG;

static int Prune(string folder, string[] baseFiles)
{
	if (!Directory.Exists(folder))
	{
		Console.Error.WriteLine($"Error: The folder {folder} does not exist.");
		return 1;
	}
	string root = Path.GetFullPath(folder);
	ResourceTree changes = ResourceComparison.GetChanges(ResourceDirectory.Import(root), ScanAll(baseFiles));
	var changedFiles = new HashSet<string>(changes.GetResourceListing().Values
		.Select(r => Path.GetFullPath(((FileResourceSource)r.Source).Path)), StringComparer.Ordinal);
	int deleted = 0;
	foreach (string path in ResourceDirectory.Import(root).GetResourceListing().Values.Select(r => Path.GetFullPath(((FileResourceSource)r.Source).Path)))
	{
		if (!changedFiles.Contains(path))
		{
			File.Delete(path);
			deleted++;
		}
	}
	// Remove the folders that are empty now, deepest first
	foreach (string directory in Directory.GetDirectories(root, "*", SearchOption.AllDirectories).OrderByDescending(d => d.Length))
	{
		if (!Directory.EnumerateFileSystemEntries(directory).Any())
		{
			Directory.Delete(directory);
		}
	}
	Console.WriteLine($"Deleted {deleted} files that are the same as in the .dat files. {changedFiles.Count} changed files stay in {folder}.");
	return 0;
}

/// <summary>Reads the .dat files into one tree. A later file replaces the resources of an earlier file, as in the game.</summary>
static ResourceTree ScanAll(IEnumerable<string> files)
{
	ResourceTree tree = new ResourceTree();
	foreach (string file in files)
	{
		tree.MergeWith(Scan(file));
	}
	return tree;
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
	// The art of the messages, the START banner and the results screen, which the game data does not have
	foreach (ImportedSprite sprite in SpecialStageArt.Import(disassembly))
	{
		string spriteFolder = Path.Combine(new[] { folder }.Concat(sprite.ObjectKey.Split('/')).ToArray());
		Directory.CreateDirectory(spriteFolder);
		File.WriteAllBytes(Path.Combine(spriteFolder, "TEXTURE.png"), sprite.Texture);
		File.WriteAllText(Path.Combine(spriteFolder, "ANIGROUP.anigroup.xml"), sprite.AnimationGroup);
	}
	Console.WriteLine($"Wrote {stages.Count} special stages and their art to {output}.");
	return 0;
}

static int ImportDemos(string disassembly, string folder)
{
	if (!File.Exists(Path.Combine(disassembly, "s2.asm")))
	{
		Console.Error.WriteLine($"Error: The folder {disassembly} does not contain s2.asm.");
		return 1;
	}
	string output = Path.Combine(folder, "SONICORCA", "RECORDINGS");
	Directory.CreateDirectory(output);
	foreach ((string zone, string label) in OriginalDemos.All)
	{
		byte[] recording = OriginalDemos.ToRecording(OriginalDemos.ReadButtons(disassembly, label));
		File.WriteAllBytes(Path.Combine(output, zone + ".rec"), recording);
	}
	Console.WriteLine($"Wrote {OriginalDemos.All.Count} demos to {output}.");
	return 0;
}

static int ImportMusic(string disassembly, string folder)
{
	if (!Directory.Exists(Path.Combine(disassembly, MusicImport.MusicFolder)) || !Directory.Exists(Path.Combine(disassembly, MusicImport.DacFolder)))
	{
		Console.Error.WriteLine($"Error: The folder {disassembly} does not contain the songs of s2disasm.");
		return 1;
	}
	string ffmpeg = Environment.GetEnvironmentVariable("S2HD_FFMPEG") ?? "ffmpeg";
	MusicImport.Import(disassembly, folder, ffmpeg, Console.WriteLine);
	Console.WriteLine($"Wrote {MusicImport.Songs.Count} songs to {folder}.");
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
	// The collision data of the original, so that the level editor can trace the collision again
	if (level.TileCollision != null)
	{
		File.WriteAllBytes(Path.Combine(output, TileCollision.ResourceName + ".xml"), level.TileCollision.ToXml());
	}
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
		  s2hd-data unpack <file.dat>... <folder> [--images]
		  s2hd-data pack   <folder> <file.dat> [--base <original.dat>]
		  s2hd-data prune  <folder> <file.dat>...
		  s2hd-data import-special-stages <s2disasm folder> <folder>
		  s2hd-data import-level <zone> <s2disasm folder> <folder>
		  s2hd-data import-demos <s2disasm folder> <folder>
		  s2hd-data import-music <s2disasm folder> <folder>
		""");
	return 2;
}
