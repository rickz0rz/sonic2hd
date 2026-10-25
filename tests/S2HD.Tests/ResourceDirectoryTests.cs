using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SonicOrca.Resources;
using Xunit;

namespace S2HD.Tests;

/// <summary>Unpacking resources to a folder of files, packing them again, and finding changed resources.</summary>
public sealed class ResourceDirectoryTests : IDisposable
{
	private readonly string _directory = Directory.CreateTempSubdirectory("s2hd-tests-").FullName;

	public void Dispose() => Directory.Delete(_directory, recursive: true);

	[Fact]
	public void Export_WritesOneFilePerResource()
	{
		ResourceTree tree = MakeTree(new()
		{
			["SONICORCA/FONTS/HUD"] = (ResourceTypeIdentifier.Font, "<font />"),
			["SONICORCA/FONTS/HUD/SHAPE"] = (ResourceTypeIdentifier.TexturePNG, "png bytes"),
			["SONICORCA/LEVELS/EHZ/MAP"] = (ResourceTypeIdentifier.Map, "<map />"),
		});
		string output = Path.Combine(_directory, "out");

		int count = ResourceDirectory.Export(tree, output);

		Assert.Equal(3, count);
		Assert.Equal("<font />", File.ReadAllText(Path.Combine(output, "SONICORCA", "FONTS", "HUD", "_.font.xml")));
		Assert.Equal("png bytes", File.ReadAllText(Path.Combine(output, "SONICORCA", "FONTS", "HUD", "SHAPE.png")));
		Assert.Equal("<map />", File.ReadAllText(Path.Combine(output, "SONICORCA", "LEVELS", "EHZ", "MAP.map.xml")));
	}

	[Fact]
	public void Export_WithAFilter_WritesOnlyTheResourcesThatItAccepts()
	{
		ResourceTree tree = MakeTree(new()
		{
			["SONICORCA/FONTS/HUD"] = (ResourceTypeIdentifier.Font, "<font />"),
			["SONICORCA/FONTS/HUD/SHAPE"] = (ResourceTypeIdentifier.TexturePNG, "png bytes"),
			["SONICORCA/LEVELS/EHZ/MAP"] = (ResourceTypeIdentifier.Map, "<map />"),
		});
		string output = Path.Combine(_directory, "images");

		int count = ResourceDirectory.Export(tree, output, r => r.Identifier == ResourceTypeIdentifier.TexturePNG);

		Assert.Equal(1, count);
		Assert.Equal(new[] { Path.Combine(output, "SONICORCA", "FONTS", "HUD", "SHAPE.png") }, Directory.GetFiles(output, "*", SearchOption.AllDirectories));
	}

	[Fact]
	public void ExportThenImport_KeepsKeysTypesAndContent()
	{
		var expected = new Dictionary<string, (ResourceTypeIdentifier, string)>
		{
			["SONICORCA/FONTS/HUD"] = (ResourceTypeIdentifier.Font, "<font />"),
			["SONICORCA/FONTS/HUD/SHAPE"] = (ResourceTypeIdentifier.TexturePNG, "png bytes"),
			["SONICORCA/MENU/HEADER-PAUSE"] = (ResourceTypeIdentifier.TexturePNG, "header"),
			["SONICORCA/OBJECTS/SONIC"] = (ResourceTypeIdentifier.Object, "#use SONICORCA.OBJECTS.SONIC"),
		};
		string output = Path.Combine(_directory, "out");
		ResourceDirectory.Export(MakeTree(expected), output);

		IDictionary<string, Resource> imported = ResourceDirectory.Import(output).GetResourceListing();

		Assert.Equal(expected.Keys.OrderBy(k => k), imported.Keys.OrderBy(k => k));
		foreach (var (key, (type, text)) in expected)
		{
			Assert.Equal(type, imported[key].Identifier);
			Assert.Equal(text, Encoding.UTF8.GetString(ResourceFileTests.ReadAll(imported[key].Source.ReadUncompressed())));
		}
	}

	[Fact]
	public void Import_UsesUpperCaseKeysAndUnderscoreSeparators_AndSkipsUnknownFiles()
	{
		string root = Path.Combine(_directory, "mod");
		Directory.CreateDirectory(Path.Combine(root, "sonicorca", "hud"));
		File.WriteAllText(Path.Combine(root, "sonicorca", "hud", "lives_icon.png"), "icon");
		File.WriteAllText(Path.Combine(root, "sonicorca", "hud", "notes.txt"), "not a resource");
		var skipped = new List<string>();

		IDictionary<string, Resource> imported = ResourceDirectory.Import(root, skipped).GetResourceListing();

		Assert.Equal(new[] { "SONICORCA/HUD/LIVES/ICON" }, imported.Keys);
		Assert.Single(skipped, path => path.EndsWith("notes.txt", StringComparison.Ordinal));
	}

	[Fact]
	public void GetChanges_ReturnsOnlyNewAndChangedResources()
	{
		ResourceTree baseTree = MakeTree(new()
		{
			["A/SAME"] = (ResourceTypeIdentifier.Xml, "same"),
			["A/CHANGED"] = (ResourceTypeIdentifier.Xml, "old"),
			["A/RETYPED"] = (ResourceTypeIdentifier.Xml, "data"),
			["A/ONLYINBASE"] = (ResourceTypeIdentifier.Xml, "base"),
		});
		ResourceTree tree = MakeTree(new()
		{
			["A/SAME"] = (ResourceTypeIdentifier.Xml, "same"),
			["A/CHANGED"] = (ResourceTypeIdentifier.Xml, "new"),
			["A/RETYPED"] = (ResourceTypeIdentifier.TexturePNG, "data"),
			["A/NEW"] = (ResourceTypeIdentifier.Xml, "new"),
		});

		IDictionary<string, Resource> changes = ResourceComparison.GetChanges(tree, baseTree).GetResourceListing();

		Assert.Equal(new[] { "A/CHANGED", "A/NEW", "A/RETYPED" }, changes.Keys.OrderBy(k => k, StringComparer.Ordinal));
	}

	[Fact]
	public void FileResourceSource_SeeksInsideTheResource()
	{
		string path = Path.Combine(_directory, "file.bin");
		File.WriteAllBytes(path, Enumerable.Range(0, 100).Select(i => (byte)i).ToArray());
		using Stream stream = new FileResourceSource(path, offset: 10, size: 20).Read();

		Assert.Equal(5, stream.Seek(5, SeekOrigin.Begin));
		Assert.Equal(15, stream.ReadByte());
		Assert.Equal(6, stream.Position);
		stream.Position = 19;
		Assert.Equal(29, stream.ReadByte());
		Assert.Equal(-1, stream.ReadByte());
		Assert.Equal(18, stream.Seek(-2, SeekOrigin.End));
		Assert.Throws<IOException>(() => stream.Seek(1, SeekOrigin.End));
	}

	private ResourceTree MakeTree(Dictionary<string, (ResourceTypeIdentifier Type, string Text)> resources)
	{
		var tree = new ResourceTree();
		foreach (var (key, (type, text)) in resources)
		{
			string path = Path.Combine(_directory, Guid.NewGuid().ToString("N"));
			File.WriteAllText(path, text);
			tree.SetOrAdd(key, new Resource(key, type, new FileResourceSource(path, 0, new FileInfo(path).Length)));
		}
		return tree;
	}
}
