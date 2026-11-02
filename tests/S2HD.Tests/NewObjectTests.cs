using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SonicOrca.Core;
using SonicOrca.Core.Editor;
using SonicOrca.Core.Scripting;
using SonicOrca.Resources;
using Xunit;

namespace S2HD.Tests;

/// <summary>Tests the files of a new Lua object that the level editor makes.</summary>
public sealed class NewObjectTests
{
	[Theory]
	[InlineData("WALKER2", null)]
	[InlineData("", "The key is empty.")]
	[InlineData("MY_BLOCK", "The key can have only capital letters and digits.")]
	[InlineData("block", "The key can have only capital letters and digits.")]
	[InlineData("ABCDEFGHIJKLMNOPQRSTUVWXY", "The key can have a maximum of 24 characters.")]
	public void CheckKey_GivesTheProblem(string key, string problem)
	{
		Assert.Equal(problem, NewObject.CheckKey(key));
	}

	[Fact]
	public void MakeKey_KeepsTheLettersAndDigits_InCapitals()
	{
		Assert.Equal("BIGROCK2", NewObject.MakeKey("Big rock_2!"));
	}

	[Theory]
	[InlineData(NewObjectKind.Scenery, ObjectClassification.Scenery, new string[0])]
	[InlineData(NewObjectKind.SolidBlock, ObjectClassification.General, new string[0])]
	[InlineData(NewObjectKind.Collectible, ObjectClassification.General, new[] { "rings" })]
	[InlineData(NewObjectKind.Enemy, ObjectClassification.Badnik, new[] { "speed", "start_right" })]
	[InlineData(NewObjectKind.MovingPlatform, ObjectClassification.Platform, new[] { "distance", "fall", "speed", "vertical" })]
	[InlineData(NewObjectKind.Boss, ObjectClassification.Badnik, new[] { "health", "speed" })]
	[InlineData(NewObjectKind.PowerUp, ObjectClassification.General, new string[0])]
	public async Task Script_LoadsWithTheClassificationAndTheStateOfItsKind(NewObjectKind kind, ObjectClassification classification, string[] state)
	{
		string source = NewObject.MakeScript(kind, "A \"test\" object", 96, 72);
		Resource resource = new Resource("SONICORCA/OBJECTS/TEST", ResourceTypeIdentifier.LuaObject, new MemoryResourceSource(Encoding.UTF8.GetBytes(source)));

		LuaObjectType type = await LuaObjectType.LoadAsync(resource, source, TestContext.Current.CancellationToken);

		Assert.Equal("A \"test\" object", type.Name);
		Assert.Equal(classification, type.Classification);
		Assert.Contains("SONICORCA/OBJECTS/TEST/ANIGROUP", type.Dependencies);
		Assert.Equal(state.OrderBy(s => s, StringComparer.Ordinal), type.StateProperties.Select(p => p.Name));
		Assert.Contains("local HALF_WIDTH, HALF_HEIGHT = 48, 36", source);
		Assert.Equal(kind == NewObjectKind.Boss, type.IsBoss);
	}

	[Fact]
	public async Task AbilityScript_LoadsAsAnAbility()
	{
		string source = NewObject.MakeAbilityScript("Spring shoes");
		Resource resource = new Resource("SONICORCA/OBJECTS/SHOESABILITY", ResourceTypeIdentifier.LuaObject, new MemoryResourceSource(Encoding.UTF8.GetBytes(source)));

		LuaObjectType type = await LuaObjectType.LoadAsync(resource, source, TestContext.Current.CancellationToken);

		Assert.True(type.IsAbility);
		Assert.Equal("Spring shoes ability", type.Name);
		Assert.True(LuaObjectType.IsAbilityScript(resource));
	}

	[Fact]
	public void Create_OfAPowerUp_WritesTheItemAndItsAbility()
	{
		string folder = Directory.CreateTempSubdirectory("s2hd-new-object-").FullName;
		try
		{
			ResourceTree tree = new ResourceTree();

			NewObject.Create(tree, folder, "SHOES", "Spring shoes", NewObjectKind.PowerUp, null);

			string objects = Path.Combine(folder, "SONICORCA", "OBJECTS");
			Assert.True(File.Exists(Path.Combine(objects, "SHOES", "_.object.lua")));
			string ability = Path.Combine(objects, "SHOESABILITY.object.lua");
			Assert.True(File.Exists(ability));
			// The item names the ability as a dependency
			Assert.Contains("ability = \"SONICORCA/OBJECTS/SHOESABILITY\"", File.ReadAllText(Path.Combine(objects, "SHOES", "_.object.lua")));
			Assert.Equal(ability, Assert.IsType<FileResourceSource>(tree["SONICORCA/OBJECTS/SHOESABILITY"].Resource.Source).Path);
			// The folder import of the game gives the same keys
			Assert.Equal(tree.GetResourceListing().Keys.OrderBy(k => k), ResourceDirectory.Import(folder).GetResourceListing().Keys.OrderBy(k => k));
		}
		finally
		{
			Directory.Delete(folder, recursive: true);
		}
	}

	[Theory]
	[InlineData(NewObjectKind.Scenery, 128, 128)]
	[InlineData(NewObjectKind.Collectible, 64, 64)]
	[InlineData(NewObjectKind.Enemy, 96, 72)]
	[InlineData(NewObjectKind.MovingPlatform, 128, 32)]
	[InlineData(NewObjectKind.Boss, 128, 128)]
	[InlineData(NewObjectKind.PowerUp, 64, 64)]
	public void Placeholder_IsAPngImageOfTheSizeOfItsKind(NewObjectKind kind, int width, int height)
	{
		Assert.Equal((width, height), NewObject.GetPngSize(NewObject.MakePlaceholder(kind)));
	}

	[Fact]
	public void GetPngSize_IsNull_ForDataThatIsNotPng()
	{
		Assert.Null(NewObject.GetPngSize(Encoding.ASCII.GetBytes("not a png image at all")));
		Assert.Null(NewObject.GetPngSize(null));
	}

	[Fact]
	public void Create_WritesTheFiles_AndAddsTheResourcesFromTheFiles()
	{
		string folder = Directory.CreateTempSubdirectory("s2hd-new-object-").FullName;
		try
		{
			ResourceTree tree = new ResourceTree();

			string script = NewObject.Create(tree, folder, "WALKER", "Walker", NewObjectKind.Enemy, null);

			string objectFolder = Path.Combine(folder, "SONICORCA", "OBJECTS", "WALKER");
			Assert.Equal(Path.Combine(objectFolder, "_.object.lua"), script);
			Assert.True(File.Exists(script));
			Assert.True(File.Exists(Path.Combine(objectFolder, "ANIGROUP.anigroup.xml")));
			Assert.True(File.Exists(Path.Combine(objectFolder, "TEXTURE.png")));
			Assert.Contains("w=\"96\" h=\"72\"", File.ReadAllText(Path.Combine(objectFolder, "ANIGROUP.anigroup.xml")));
			// The script is a file of a mod folder, so the game loads it again when the file changes
			Resource resource = tree["SONICORCA/OBJECTS/WALKER"].Resource;
			Assert.Equal(ResourceTypeIdentifier.LuaObject, resource.Identifier);
			Assert.Equal(script, Assert.IsType<FileResourceSource>(resource.Source).Path);
			Assert.Equal(ResourceTypeIdentifier.AnimationGroup, tree["SONICORCA/OBJECTS/WALKER/ANIGROUP"].Resource.Identifier);
			Assert.Equal(ResourceTypeIdentifier.TexturePNG, tree["SONICORCA/OBJECTS/WALKER/TEXTURE"].Resource.Identifier);
			// The folder import of the game gives the same keys
			Assert.Equal(tree.GetResourceListing().Keys.OrderBy(k => k), ResourceDirectory.Import(folder).GetResourceListing().Keys.OrderBy(k => k));

			InvalidOperationException again = Assert.Throws<InvalidOperationException>(() => NewObject.Create(tree, folder, "WALKER", "Walker", NewObjectKind.Enemy, null));
			Assert.Equal("The object WALKER exists.", again.Message);
		}
		finally
		{
			Directory.Delete(folder, recursive: true);
		}
	}
}
