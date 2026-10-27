using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SonicOrca.Core;
using SonicOrca.Core.SpecialStage;
using SonicOrca.Original.Music;
using SonicOrca.Resources;

namespace SonicOrca.Original;

/// <summary>One step of an import from the disassembly. Each step writes resource files into the folder of its mod.</summary>
/// <param name="Mod">The name of the mod that the step adds to, for example "arz" or "music".</param>
/// <param name="Description">A short text for the progress, for example "Aquatic Ruin".</param>
/// <param name="Run">Imports from the disassembly folder (the first argument) into the mod folder (the second argument).</param>
public sealed record OriginalImportStep(string Mod, string Description, Action<string, string> Run);

/// <summary>
/// Makes the mods that come from the Sonic 2 disassembly (github.com/sonicretro/s2disasm): the zones that the demo
/// data does not have, the special stages, the attract demos and the music. tools/s2hd-data and the web build use
/// these steps. The data comes from the original game, so it stays local.
/// </summary>
public static class OriginalImport
{
	/// <summary>
	/// The version of the import. Change it when a change of the importers changes the mods, so that the web build
	/// makes its stored mods again.
	/// </summary>
	public const int Version = 3;

	/// <summary>The zones that the import makes, in the order of tools/import-all.sh.</summary>
	public static readonly IReadOnlyList<string> Zones = new[] { "arz", "cnz", "mcz", "ooz", "mtz", "scz", "wfz", "dez" };

	/// <summary>The file in the disassembly that shows that a folder is the disassembly.</summary>
	public const string MarkerFile = "s2.asm";

	/// <summary>
	/// Returns the steps of a full import, in order. The steps of one mod follow each other. The demos of ARZ and CNZ
	/// need the imports of these zones in the game, so the demos come after the zones.
	/// </summary>
	/// <param name="ffmpeg">The ffmpeg program for the music, or null for songs that play live (see <see cref="MusicImport.ImportSong"/>).</param>
	/// <param name="log">Gets the notes of the steps.</param>
	public static IReadOnlyList<OriginalImportStep> GetSteps(string ffmpeg, Action<string> log)
	{
		List<OriginalImportStep> steps = new List<OriginalImportStep>();
		foreach (string zone in Zones)
		{
			OriginalZone originalZone = OriginalZone.All[zone];
			steps.Add(new OriginalImportStep(zone, originalZone.Name, (disassembly, folder) => ImportLevel(originalZone, disassembly, folder, log)));
		}
		steps.Add(new OriginalImportStep("special-stages", "Special stages", (disassembly, folder) => ImportSpecialStages(disassembly, folder, log)));
		steps.Add(new OriginalImportStep("demos", "Demos", (disassembly, folder) => ImportDemos(disassembly, folder, log)));
		foreach ((string File, string Key) song in MusicImport.Songs)
		{
			steps.Add(new OriginalImportStep("music", "Music " + Path.GetFileNameWithoutExtension(song.File),
				(disassembly, folder) => log(MusicImport.ImportSong(song, disassembly, folder, ffmpeg))));
		}
		foreach ((string File, string Key) sound in MusicImport.Sounds)
		{
			steps.Add(new OriginalImportStep("music", "Sound " + Path.GetFileNameWithoutExtension(sound.File),
				(disassembly, folder) => log(MusicImport.ImportSound(sound, disassembly, folder, ffmpeg))));
		}
		return steps;
	}

	/// <summary>Returns the folder in a zip or a folder tree that has <see cref="MarkerFile"/>, or null.</summary>
	/// <param name="root">The folder to look in. GitHub puts the files of a zip in a folder such as s2disasm-master.</param>
	public static string FindDisassembly(string root)
	{
		if (File.Exists(Path.Combine(root, MarkerFile)))
		{
			return root;
		}
		return Directory.GetDirectories(root).FirstOrDefault(d => File.Exists(Path.Combine(d, MarkerFile)));
	}

	/// <summary>Makes the map (with collision), the object binding and the art of a zone.</summary>
	public static ImportedLevel ImportLevel(OriginalZone zone, string disassembly, string folder, Action<string> log)
	{
		if (!File.Exists(Path.Combine(disassembly, zone.ChunksFile)))
		{
			throw new IOException($"The folder {disassembly} does not contain the level data of s2disasm.");
		}
		ImportedLevel level = LevelImport.Import(zone, disassembly);
		string output = KeyFolder(folder, zone.Key);
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
		WriteSprites(folder, level.Sprites);
		foreach ((string key, byte[] texture) in level.Textures ?? new Dictionary<string, byte[]>())
		{
			string file = KeyFolder(folder, key) + ".png";
			Directory.CreateDirectory(Path.GetDirectoryName(file));
			File.WriteAllBytes(file, texture);
		}
		// A resource that names the code of each new object: in its folder when it has art, else next to the folder
		foreach ((string key, string codeNamespace) in level.NewObjects)
		{
			string resourceFile = Directory.Exists(KeyFolder(folder, key)) ? Path.Combine(KeyFolder(folder, key), "_.object.cs") : KeyFolder(folder, key) + ".object.cs";
			Directory.CreateDirectory(Path.GetDirectoryName(resourceFile));
			File.WriteAllText(resourceFile, "#use " + codeNamespace);
		}
		log($"Wrote {zone.Name} to {output}: {level.Map.CollisionVectors.Count} collision lines, {level.Binding.ObjectPlacements.Count} objects.");
		foreach (string note in level.Notes)
		{
			log("  " + note);
		}
		return level;
	}

	/// <summary>Makes the layouts of the special stages, and the art of the messages, the START banner and the results screen.</summary>
	public static void ImportSpecialStages(string disassembly, string folder, Action<string> log)
	{
		if (!File.Exists(Path.Combine(disassembly, SpecialStageLayouts.LayoutsFile)) || !File.Exists(Path.Combine(disassembly, SpecialStageLayouts.ObjectsFile)))
		{
			throw new IOException($"The folder {disassembly} does not contain the special stage data of s2disasm.");
		}
		List<HalfPipeStageData> stages = SpecialStageLayouts.ReadFromDisassembly(disassembly);
		string output = Path.Combine(folder, "SONICORCA", "SPECIALSTAGE");
		Directory.CreateDirectory(output);
		for (int i = 0; i < stages.Count; i++)
		{
			using StreamWriter writer = new StreamWriter(Path.Combine(output, $"STAGE{i + 1}.xml"));
			stages[i].WriteXml(writer);
		}
		// The art of the messages, the START banner and the results screen, which the game data does not have
		WriteSprites(folder, SpecialStageArt.Import(disassembly));
		log($"Wrote {stages.Count} special stages and their art to {output}.");
	}

	/// <summary>Makes the recordings of the attract demos.</summary>
	public static void ImportDemos(string disassembly, string folder, Action<string> log)
	{
		if (!File.Exists(Path.Combine(disassembly, MarkerFile)))
		{
			throw new IOException($"The folder {disassembly} does not contain {MarkerFile}.");
		}
		string output = Path.Combine(folder, "SONICORCA", "RECORDINGS");
		Directory.CreateDirectory(output);
		foreach ((string zone, string label) in OriginalDemos.All)
		{
			byte[] recording = OriginalDemos.ToRecording(OriginalDemos.ReadButtons(disassembly, label));
			File.WriteAllBytes(Path.Combine(output, zone + ".rec"), recording);
		}
		log($"Wrote {OriginalDemos.All.Count} demos to {output}.");
	}

	/// <summary>
	/// Makes a .dat file from a folder of resource files. With a base tree, the file has only the resources that are
	/// new or changed compared to the base. Returns the number of resources, or 0 when there is nothing to write (no
	/// file is written then).
	/// </summary>
	/// <param name="folder">The folder, in the layout of "s2hd-data unpack".</param>
	/// <param name="file">The .dat file to write.</param>
	/// <param name="baseTree">The resources of the original game data, or null.</param>
	/// <param name="skippedFiles">Gets the files with an unknown type, which are not packed.</param>
	public static int Pack(string folder, string file, ResourceTree baseTree, ICollection<string> skippedFiles = null)
	{
		ResourceTree tree = ResourceDirectory.Import(folder, skippedFiles);
		if (baseTree != null)
		{
			tree = ResourceComparison.GetChanges(tree, baseTree);
		}
		int count = tree.GetResourceListing().Count;
		if (count > 0)
		{
			new ResourceFile(file).Write(tree);
		}
		return count;
	}

	private static void WriteSprites(string folder, IEnumerable<ImportedSprite> sprites)
	{
		foreach (ImportedSprite sprite in sprites)
		{
			string spriteFolder = KeyFolder(folder, sprite.ObjectKey);
			Directory.CreateDirectory(spriteFolder);
			File.WriteAllBytes(Path.Combine(spriteFolder, "TEXTURE.png"), sprite.Texture);
			File.WriteAllText(Path.Combine(spriteFolder, "ANIGROUP.anigroup.xml"), sprite.AnimationGroup);
		}
	}

	/// <summary>The folder of a resource key, for example SONICORCA/LEVELS/ARZ in the output folder.</summary>
	private static string KeyFolder(string folder, string key) => Path.Combine(new[] { folder }.Concat(key.Split('/')).ToArray());
}
