using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SonicOrca.Core;
using SonicOrca.Core.Editor;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Geometry;
using SonicOrca.Resources;
using Xunit;

namespace S2HD.Tests;

/// <summary>
/// Tests the parts of the level editor that do not need the game: the undo history, the placement copies, the
/// object properties and the editor file.
/// </summary>
public sealed class LevelEditorTests
{
	public enum TestDirection
	{
		Left,
		Up,
		Right
	}

	public sealed class TestInstance : ActiveObject
	{
		public int Strength { get; set; } = 3;

		public bool Flipped { get; set; }

		public TestDirection Direction { get; set; } = TestDirection.Up;

		public Vector2i Offset { get; set; }
	}

	[ObjectInstance(typeof(TestInstance))]
	public sealed class TestType : ObjectType
	{
	}

	private static ObjectPlacement MakePlacement(int x = 100, int y = 200) =>
		new ObjectPlacement("SONICORCA/OBJECTS/TEST", Guid.NewGuid(), "Test 0", 2, new Vector2i(x, y), new { Strength = 5 });

	[Fact]
	public void With_MakesACopy_AndKeepsTheOldPlacement()
	{
		ObjectPlacement placement = MakePlacement();
		placement.Mappings = new List<KeyValuePair<string, object>> { new KeyValuePair<string, object>("Target", Guid.NewGuid().ToString()) };

		ObjectPlacement moved = placement.WithPosition(new Vector2i(300, 400));

		Assert.Equal(new Vector2i(100, 200), placement.Position);
		Assert.Equal(new Vector2i(300, 400), moved.Position);
		Assert.Equal(placement.Uid, moved.Uid);
		Assert.Equal(placement.Key, moved.Key);
		Assert.Equal(placement.Behaviour, moved.Behaviour);
		Assert.Equal(placement.Mappings, moved.Mappings);
		// An object entry reads the common values from Entry, so the copy must have the new position there too
		Assert.Contains(moved.Entry, pair => pair.Key == "Position" && pair.Value.ToString().Contains("300"));
		Assert.Equal("New name", placement.With(placement.Uid, placement.Position, placement.Layer, placement.Behaviour, "New name").Name);
	}

	[Fact]
	public void History_UndoAndRedo_GiveBackTheSnapshots()
	{
		ObjectPlacement first = MakePlacement();
		ObjectPlacement second = MakePlacement(500);
		LevelMarker marker = new LevelMarker("start", "", new Vector2i(1, 2), null);
		EditorSnapshot before = new EditorSnapshot { Placements = new[] { first, second }, Selection = new[] { second.Uid }, Markers = new[] { marker } };
		EditorSnapshot after = new EditorSnapshot { Placements = new[] { first } };
		EditorHistory history = new EditorHistory();

		history.Record(before);

		Assert.Same(before, history.Undo(after));
		Assert.Equal(after, history.Redo(before));
		Assert.Null(history.Redo(after));
		Assert.True(history.CanUndo);
	}

	[Fact]
	public void History_TheRedoStep_HasTheTileEditsOfTheUndoneStep()
	{
		TileEdit edit = new TileEdit(1, 2, 3, 10, 20, 0, 15);
		EditorHistory history = new EditorHistory();
		history.Record(new EditorSnapshot { TileEdits = new[] { edit } });

		EditorSnapshot undone = history.Undo(new EditorSnapshot());
		EditorSnapshot redone = history.Redo(new EditorSnapshot());

		Assert.Equal(new[] { edit }, undone.TileEdits);
		Assert.Equal(new[] { edit }, redone.TileEdits);
	}

	[Fact]
	public void History_ANewChange_RemovesTheRedoSteps()
	{
		EditorHistory history = new EditorHistory();
		history.Record(new EditorSnapshot());
		history.Undo(new EditorSnapshot());

		history.Record(new EditorSnapshot());

		Assert.False(history.CanRedo);
		Assert.True(history.CanUndo);
	}

	[Fact]
	public void History_KeepsAtMostMaxSteps()
	{
		EditorHistory history = new EditorHistory();
		for (int i = 0; i < EditorHistory.MaxSteps + 10; i++)
		{
			history.Record(new EditorSnapshot());
		}

		int count = 0;
		while (history.Undo(new EditorSnapshot()) != null)
		{
			count++;
		}
		Assert.Equal(EditorHistory.MaxSteps, count);
	}

	private const string TestMap = """
		<?xml version="1.0" encoding="utf-8"?>
		<map>
		  <tiles>
		    <layer name="back" minimap_colour="FF102030">
		      <tiles parallax_y="0.5"><row>1,2</row><row>3,4</row></tiles>
		    </layer>
		    <layergroup name="front">
		      <layer name="fg">
		        <lighting type="inside" light="0.5" />
		        <tiles><row>h5,v6</row><row>hv7,4100</row></tiles>
		      </layer>
		    </layergroup>
		  </tiles>
		  <collision><paths><path layer="1" /></paths><vectors><vector a="0,0" b="64,0" paths="1" flags="0" /></vectors></collision>
		  <markers><marker name="startpos_stk_1" x="10" y="20" layer="1" /><marker tag="inside" x="0" y="0" width="64" height="32" /></markers>
		  <unknown keep="yes" />
		</map>
		""";

	private static (System.Xml.XmlDocument Document, LevelMap Map) LoadTestMap()
	{
		System.Xml.XmlDocument document = new System.Xml.XmlDocument();
		document.LoadXml(TestMap.Trim());
		LevelMap map = new LevelMap();
		LevelLayer back = new LevelLayer(map) { Name = "back" };
		back.Resize(2, 2);
		LevelLayer front = new LevelLayer(map) { Name = "fg" };
		front.Resize(2, 2);
		map.Layers.Add(back);
		map.Layers.Add(front);
		return (document, map);
	}

	[Fact]
	public void MapPatch_WritesTheMarkers_AndKeepsTheRestOfTheFile()
	{
		(System.Xml.XmlDocument document, LevelMap map) = LoadTestMap();
		map.Markers.Add(new LevelMarker("startpos_stk_1", "", new Vector2i(640, 2600), map.Layers[1]));

		new LevelMapWriter(map).Patch(document, tiles: false, collision: false, markers: true);

		System.Xml.XmlNodeList markers = document.SelectNodes("map/markers/marker");
		Assert.Equal(1, markers.Count);
		Assert.Equal("640", markers[0].Attributes["x"].Value);
		Assert.Equal("1", markers[0].Attributes["layer"].Value);
		// The parts that the patch does not write stay as they are
		Assert.NotNull(document.SelectSingleNode("map/unknown[@keep='yes']"));
		Assert.Equal("0.5", document.SelectSingleNode("map/tiles/layer/tiles/@parallax_y").Value);
		Assert.Equal("h5,v6", document.SelectSingleNode("map/tiles/layergroup/layer/tiles/row").InnerText);
		Assert.NotNull(document.SelectSingleNode("map/collision/vectors/vector"));
	}

	[Fact]
	public void MapPatch_WritesTheTileRows_WithFlipsAndLargeValues()
	{
		(System.Xml.XmlDocument document, LevelMap map) = LoadTestMap();
		map.Layers[1].Tiles[0, 0] = 0x4000 | 5;
		map.Layers[1].Tiles[1, 0] = 0x8000 | 6;
		map.Layers[1].Tiles[0, 1] = 0xC000 | 7;
		map.Layers[1].Tiles[1, 1] = 5000;

		new LevelMapWriter(map).Patch(document, tiles: true, collision: false, markers: false);

		string[] rows = document.SelectNodes("map/tiles/layergroup/layer/tiles/row").OfType<System.Xml.XmlNode>().Select(n => n.InnerText).ToArray();
		Assert.Equal(new[] { "h5,v6", "hv7,5000" }, rows);
		Assert.Equal("inside", document.SelectSingleNode("map/tiles/layergroup/layer/lighting/@type").Value);
	}

	[Fact]
	public void MapPatch_RemovesAnEmptySection()
	{
		(System.Xml.XmlDocument document, LevelMap map) = LoadTestMap();

		new LevelMapWriter(map).Patch(document, tiles: false, collision: true, markers: true);

		Assert.Null(document.SelectSingleNode("map/markers"));
		Assert.Null(document.SelectSingleNode("map/collision"));
	}

	[Fact]
	public void MapPatch_NeedsTheSameLayers()
	{
		(System.Xml.XmlDocument document, LevelMap map) = LoadTestMap();
		map.Layers.RemoveAt(1);

		Assert.Throws<InvalidOperationException>(() => new LevelMapWriter(map).Patch(document, tiles: true, collision: false, markers: false));
	}

	[Fact]
	public void Properties_ComeFromTheStateVariables_WithTheirDefaults()
	{
		IReadOnlyList<EditorProperty> properties = EditorProperty.For(new TestType());

		Assert.Equal(new[] { "Strength", "Flipped", "Direction", "Offset" }, properties.Select(p => p.Name));
		Assert.Equal(3, properties[0].DefaultValue);
		Assert.All(properties, p => Assert.True(p.IsEditable));
	}

	[Fact]
	public void Property_ReadsTheBehaviour_OrTheDefault()
	{
		IReadOnlyList<EditorProperty> properties = EditorProperty.For(new TestType());
		ObjectPlacement placement = MakePlacement();

		Assert.Equal(5, properties[0].GetValue(placement));
		Assert.Equal(TestDirection.Up, properties[2].GetValue(placement));
	}

	[Fact]
	public void Property_WritesOnlyAValueThatIsNotTheDefault()
	{
		EditorProperty strength = EditorProperty.For(new TestType())[0];
		ObjectPlacement placement = MakePlacement();

		ObjectPlacement changed = strength.SetValue(placement, 8);
		ObjectPlacement reset = strength.SetValue(changed, 3);

		Assert.Equal(8, strength.GetValue(changed));
		Assert.Contains(changed.Behaviour, pair => pair.Key == "Strength");
		Assert.DoesNotContain(reset.Behaviour, pair => pair.Key == "Strength");
		Assert.Equal(5, strength.GetValue(placement));
	}

	[Fact]
	public void Property_StepsThroughAnEnumeration_AndAYesOrNo()
	{
		IReadOnlyList<EditorProperty> properties = EditorProperty.For(new TestType());

		Assert.Equal(TestDirection.Right, properties[2].Step(TestDirection.Up, 1));
		Assert.Equal(TestDirection.Left, properties[2].Step(TestDirection.Right, 1));
		Assert.Equal(TestDirection.Right, properties[2].Step(TestDirection.Left, -1));
		Assert.Equal(true, properties[1].Step(false, 1));
		Assert.Null(properties[0].Step(3, 1));
	}

	[Fact]
	public void Property_ReadsTypedValues()
	{
		IReadOnlyList<EditorProperty> properties = EditorProperty.For(new TestType());

		Assert.True(properties[0].TryParse(" -12 ", out object number));
		Assert.Equal(-12, number);
		Assert.False(properties[0].TryParse("twelve", out _));
		Assert.True(properties[2].TryParse("right", out object direction));
		Assert.Equal(TestDirection.Right, direction);
		Assert.True(properties[3].TryParse("16, -32", out object offset));
		Assert.Equal(new Vector2i(16, -32), (Vector2i)offset);
	}

	[Fact]
	public void Property_AVector_IsStoredAsXAndY()
	{
		EditorProperty offset = EditorProperty.For(new TestType())[3];

		ObjectPlacement changed = offset.SetValue(MakePlacement(), new Vector2i(16, -32));

		Assert.Equal(new Vector2i(16, -32), (Vector2i)offset.GetValue(changed));
	}

	[Fact]
	public void ModFile_IsPerZone()
	{
		string path = EditorModFile.GetPath("/user", "SONICORCA/LEVELS/CNZ/BINDING");

		Assert.Equal(Path.Combine("/user", "mods", "editor", "CNZ.dat"), path);
	}

	[Fact]
	public void ModFile_Save_WritesTheBinding_AndKeepsTheOtherResources()
	{
		string userData = Path.Combine(Path.GetTempPath(), "s2hd-editor-" + Guid.NewGuid().ToString("N"));
		try
		{
			const string key = "SONICORCA/LEVELS/TEST/BINDING";
			LevelBinding binding = new LevelBinding
			{
				Resource = new Resource(key, ResourceTypeIdentifier.Binding, new MemoryResourceSource(new byte[0]))
			};
			binding.ObjectPlacements.Add(MakePlacement(1234, 5678));
			string path = EditorModFile.GetPath(userData, key);
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			ResourceTree other = new ResourceTree();
			other.SetOrAdd("SONICORCA/LEVELS/TEST/OTHER", new Resource("SONICORCA/LEVELS/TEST/OTHER", ResourceTypeIdentifier.Binding, new MemoryResourceSource(new byte[] { 1, 2, 3 })));
			new ResourceFile(path).Write(other);

			Assert.Equal(path, EditorModFile.Save(binding, userData));

			IDictionary<string, Resource> resources = new ResourceFile(path).Scan().GetResourceListing();
			Assert.Equal(new[] { "SONICORCA/LEVELS/TEST/BINDING", "SONICORCA/LEVELS/TEST/OTHER" }, resources.Keys.OrderBy(k => k));
			string xml = ReadText(resources[key]);
			Assert.Contains("SONICORCA/OBJECTS/TEST", xml);
			Assert.Contains("X=\"1234\"", xml);
			// The resource of the binding now reads the saved data, for a load later in the session
			Assert.Equal(xml, ReadText(binding.Resource));
			Assert.False(File.Exists(path + ".tmp"));
		}
		finally
		{
			if (Directory.Exists(userData))
			{
				Directory.Delete(userData, recursive: true);
			}
		}
	}

	[Fact]
	public void ModFile_ReadIntoMemory_DoesNotKeepTheFileInUse()
	{
		string file = Path.Combine(Path.GetTempPath(), "s2hd-editor-" + Guid.NewGuid().ToString("N") + ".dat");
		try
		{
			ResourceTree tree = new ResourceTree();
			tree.SetOrAdd("A/B", new Resource("A/B", ResourceTypeIdentifier.Binding, new MemoryResourceSource(new byte[] { 7, 8, 9 })));
			new ResourceFile(file).Write(tree);

			Resource resource = EditorModFile.ReadIntoMemory(file).GetResourceListing()["A/B"];
			File.Delete(file);

			using Stream stream = resource.Source.ReadUncompressed();
			using MemoryStream copy = new MemoryStream();
			stream.CopyTo(copy);
			Assert.Equal(new byte[] { 7, 8, 9 }, copy.ToArray());
		}
		finally
		{
			File.Delete(file);
		}
	}

	private static string ReadText(Resource resource)
	{
		using Stream stream = resource.Source.ReadUncompressed();
		using StreamReader reader = new StreamReader(stream);
		return reader.ReadToEnd();
	}
}
