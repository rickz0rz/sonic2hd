using System.Collections.Generic;
using System.Linq;
using System.Text;
using SonicOrca.Core;
using SonicOrca.Core.Editor;
using SonicOrca.Resources;
using Xunit;

namespace S2HD.Tests;

/// <summary>Tests the order of the levels of the story, and the changes of the levels tool of the editor.</summary>
public sealed class LevelOrderTests
{
	[Fact]
	public void StoryLevels_WithoutAnOrder_AreTheReleasedSonic2Levels()
	{
		ResourceTree tree = new ResourceTree();

		string[] story = Levels.GetStoryLevels(tree).Select(level => level.Mnemonic).ToArray();

		Assert.Equal(new[] { "ehz", "cpz", "htz" }, story);
	}

	[Fact]
	public void StoryLevels_FollowTheOrder_AndSkipTheLevelsThatCannotBePlayed()
	{
		ResourceTree tree = new ResourceTree();
		AddModLevel(tree, "MEADOW", "Sunny Meadow");
		// ARZ is not imported and "gone" does not exist, so the story skips them
		Add(tree, Levels.OrderResourceKey, Encoding.UTF8.GetString(Levels.WriteOrder(new[] { "htz", "meadow", "arz", "gone", "ehz" })));

		IReadOnlyList<LevelInfo> story = Levels.GetStoryLevels(tree);

		Assert.Equal(new[] { "htz", "meadow", "ehz" }, story.Select(level => level.Mnemonic));
		Assert.Equal("Sunny Meadow", story[1].Name);
		Assert.Equal(2, Levels.GetLevelIndex("MEADOW", tree));
		Assert.Equal(-1, Levels.GetLevelIndex("cpz", tree));
		Assert.Equal(new[] { "cpz" }, Levels.GetOtherLevels(tree).Select(level => level.Mnemonic));
	}

	[Fact]
	public void WriteOrder_GivesTheOrderThatReadOrderReads()
	{
		ResourceTree tree = new ResourceTree();
		Add(tree, Levels.OrderResourceKey, Encoding.UTF8.GetString(Levels.WriteOrder(new[] { "CPZ", "ehz" })));

		Assert.Equal(new[] { "cpz", "ehz" }, Levels.ReadOrder(tree));
	}

	[Fact]
	public void Move_ChangesThePosition_InsideTheStory()
	{
		List<string> story = new List<string> { "ehz", "cpz", "htz" };

		Assert.True(EditorLevelOrder.Move(story, "HTZ", -1));
		Assert.Equal(new[] { "ehz", "htz", "cpz" }, story);
		Assert.False(EditorLevelOrder.Move(story, "ehz", -1));
		Assert.False(EditorLevelOrder.Move(story, "cpz", 1));
		Assert.False(EditorLevelOrder.Move(story, "meadow", 1));
		Assert.Equal(new[] { "ehz", "htz", "cpz" }, story);
	}

	[Fact]
	public void SetInStory_AddsAtTheEnd_AndRemoves()
	{
		List<string> story = new List<string> { "ehz", "cpz" };

		Assert.True(EditorLevelOrder.SetInStory(story, "meadow", true));
		Assert.False(EditorLevelOrder.SetInStory(story, "meadow", true));
		Assert.True(EditorLevelOrder.SetInStory(story, "EHZ", false));
		Assert.False(EditorLevelOrder.SetInStory(story, "ehz", false));

		Assert.Equal(new[] { "cpz", "meadow" }, story);
	}

	[Fact]
	public void Titles_GiveSonic2LevelsNewNames_InTheStoryAndTheOtherLevels()
	{
		ResourceTree tree = new ResourceTree();
		Dictionary<string, string> titles = new Dictionary<string, string> { ["ehz"] = "Green Hill Zone", ["CPZ"] = "Lab Zone" };
		Add(tree, Levels.OrderResourceKey, Encoding.UTF8.GetString(Levels.WriteOrder(new[] { "ehz", "htz" }, titles)));

		Assert.Equal("Green Hill Zone", Levels.ReadTitles(tree)["EHZ"]);
		Assert.Equal(new[] { "Green Hill Zone", "Hill Top Zone" }, Levels.GetStoryLevels(tree).Select(level => level.Name));
		Assert.Equal("Lab Zone", Levels.GetOtherLevels(tree).Single().Name);
		Assert.Equal("Emerald Hill Zone", Levels.GetOriginalName("ehz"));
	}

	[Fact]
	public void WriteModLevelInfo_ChangesTheNameAndTheMusic_AndKeepsTheOtherAttributes()
	{
		byte[] info = Encoding.UTF8.GetBytes("<level name=\"Old\" music=\"SONICORCA/MUSIC/A\" start=\"startpos\" tileset=\"SONICORCA/LEVELS/EHZ/TILESET\" />");

		System.Xml.Linq.XElement level = System.Xml.Linq.XElement.Parse(Encoding.UTF8.GetString(
			Levels.WriteModLevelInfo(new System.IO.MemoryStream(info), "New", "SONICORCA/MUSIC/B")).TrimStart('\uFEFF'));

		Assert.Equal("New", (string)level.Attribute("name"));
		Assert.Equal("SONICORCA/MUSIC/B", (string)level.Attribute("music"));
		Assert.Equal("startpos", (string)level.Attribute("start"));
		Assert.Equal("SONICORCA/LEVELS/EHZ/TILESET", (string)level.Attribute("tileset"));
	}

	[Fact]
	public void MusicKeys_AreTheMusicInfoResources_InKeyOrder()
	{
		ResourceTree tree = new ResourceTree();
		foreach (string key in new[] { "SONICORCA/MUSIC/B", "SONICORCA/MUSIC/A", "SONICORCA/SOUND/C" })
		{
			tree.SetOrAdd(key, new Resource(key, ResourceTypeIdentifier.SampleInfo, new MemoryResourceSource(new byte[0])));
		}
		tree.SetOrAdd("SONICORCA/MUSIC/A/SAMPLE", new Resource("SONICORCA/MUSIC/A/SAMPLE", ResourceTypeIdentifier.SampleOGG, new MemoryResourceSource(new byte[0])));

		Assert.Equal(new[] { "SONICORCA/MUSIC/A", "SONICORCA/MUSIC/B" }, Levels.GetMusicKeys(tree));
	}

	private static void AddModLevel(ResourceTree tree, string folder, string name)
	{
		Add(tree, $"SONICORCA/LEVELS/{folder}/AREA", "#use SONICORCA.MODAREA");
		Add(tree, $"SONICORCA/LEVELS/{folder}/INFO", $"<level name=\"{name}\" />");
	}

	private static void Add(ResourceTree tree, string key, string text)
	{
		tree.SetOrAdd(key, new Resource(key, ResourceTypeIdentifier.Xml, new MemoryResourceSource(Encoding.UTF8.GetBytes(text))));
	}
}
