using System;
using System.Collections.Generic;
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

/// <summary>
/// Tests the Lua object types that do not need a level: the definition of a script, its state variables for the
/// level editor, and the errors of a script that is not correct.
/// </summary>
public sealed class LuaObjectTests
{
	private const string BlockScript = """
		object {
		  api = 1,
		  name = "Item block",
		  description = "A test block",
		  classification = "general",
		  dependencies = { animations = "/ANIGROUP", coin = "SONICORCA/OBJECTS/COIN" },
		  state = { rings = 1, contents = "coin", speed = 1.5, visible = true },
		  saved = { used = false },
		}

		function start(self) end
		""";

	private static Task<LuaObjectType> Load(string source)
	{
		Resource resource = new Resource("SONICORCA/OBJECTS/BLOCK", ResourceTypeIdentifier.LuaObject, new MemoryResourceSource(Encoding.UTF8.GetBytes(source)));
		return LuaObjectType.LoadAsync(resource, source).AsTask();
	}

	[Fact]
	public async Task Definition_GivesTheMetadataAndTheDependencies()
	{
		LuaObjectType type = await Load(BlockScript);

		Assert.Equal("Item block", type.Name);
		Assert.Equal("A test block", type.Description);
		Assert.Equal(ObjectClassification.General, type.Classification);
		// A dependency that starts with "/" is in the folder of the object
		Assert.Equal(new[] { "SONICORCA/OBJECTS/BLOCK/ANIGROUP", "SONICORCA/OBJECTS/COIN" }, type.Dependencies.OrderBy(d => d));
	}

	[Fact]
	public async Task State_GivesTheEditorProperties_ByName_WithTheirTypes()
	{
		LuaObjectType type = await Load(BlockScript);

		IReadOnlyList<EditorProperty> properties = EditorProperty.For(type);

		Assert.Equal(new[] { "contents", "rings", "speed", "visible" }, properties.Select(p => p.Name));
		Assert.Equal(new[] { typeof(string), typeof(int), typeof(double), typeof(bool) }, properties.Select(p => p.Type));
		Assert.Equal(1, properties[1].DefaultValue);
		Assert.All(properties, p => Assert.True(p.IsEditable));
		// The saved values are not properties of the editor
		Assert.DoesNotContain(properties, p => p.Name == "used");
	}

	[Fact]
	public async Task Instance_IsALuaObject_ThatKeepsItsStateInATable()
	{
		LuaObjectType type = await Load(BlockScript);

		ActiveObject instance = type.CreateInstance();

		Assert.IsType<LuaActiveObject>(instance);
		Assert.IsAssignableFrom<IStateTable>(instance);
	}

	[Fact]
	public async Task Instance_IsAPlatform_ThatTheCharactersRide()
	{
		LuaObjectType type = await Load(BlockScript);

		Assert.IsAssignableFrom<SonicOrca.Core.Objects.IPlatform>(type.CreateInstance());
	}

	[Fact]
	public async Task Script_ForApi2_Loads()
	{
		LuaObjectType type = await Load("object { api = 2, name = \"Platform\" }");

		Assert.Equal("Platform", type.Name);
	}

	[Fact]
	public async Task Script_WithTheBossBase_IsABoss_ThatLoadsTheExplosionAndTheHitSound()
	{
		LuaObjectType type = await Load("object { api = 7, base = \"boss\" }");

		Assert.True(type.IsBoss);
		Assert.Contains("SONICORCA/OBJECTS/EXPLOSION/BOSS", type.Dependencies);
		Assert.Contains("SONICORCA/SOUND/BOSSHIT", type.Dependencies);
	}

	[Fact]
	public async Task Script_WithAnUnknownBase_DoesNotLoad()
	{
		ResourceException error = await Assert.ThrowsAsync<ResourceException>(() => Load("object { base = \"dragon\" }"));
		Assert.Contains("dragon", error.Message);
	}

	[Fact]
	public async Task Script_WithoutADefinition_DoesNotLoad()
	{
		ResourceException error = await Assert.ThrowsAsync<ResourceException>(() => Load("function start(self) end"));
		Assert.Contains("object", error.Message);
	}

	[Fact]
	public async Task Script_ForANewerApi_DoesNotLoad()
	{
		ResourceException error = await Assert.ThrowsAsync<ResourceException>(() => Load("object { api = 99 }"));
		Assert.Contains("99", error.Message);
	}

	[Fact]
	public async Task Script_WithAnUnknownClassification_DoesNotLoad()
	{
		await Assert.ThrowsAsync<ResourceException>(() => Load("object { classification = \"spaceship\" }"));
	}

	[Fact]
	public void Placement_ThatNamesAnObjectType_LoadsThatType()
	{
		ResourceTree tree = new ResourceTree();
		ResourceSource empty = new MemoryResourceSource(Array.Empty<byte>());
		tree.SetOrAdd("SONICORCA/OBJECTS/COIN", new Resource("SONICORCA/OBJECTS/COIN", ResourceTypeIdentifier.LuaObject, empty));
		tree.SetOrAdd("SONICORCA/OBJECTS/RING", new Resource("SONICORCA/OBJECTS/RING", ResourceTypeIdentifier.Object, empty));
		tree.SetOrAdd("SONICORCA/SOUND/RING", new Resource("SONICORCA/SOUND/RING", ResourceTypeIdentifier.SampleOGG, empty));
		ObjectPlacement[] placements =
		{
			new ObjectPlacement("SONICORCA/OBJECTS/BLOCK", 0, default, new { contents = "SONICORCA/OBJECTS/COIN", sound = "SONICORCA/SOUND/RING" }),
			new ObjectPlacement("SONICORCA/OBJECTS/BLOCK", 0, default, new { contents = "SONICORCA/OBJECTS/RING", rings = 3 }),
			new ObjectPlacement("SONICORCA/OBJECTS/BLOCK", 0, default, new { contents = "coin" }),
		};

		string[] keys = LevelBindingResourceType.GetReferencedObjectTypes(placements, tree).OrderBy(k => k).ToArray();

		// A sound, a number and a dependency name are not object types
		Assert.Equal(new[] { "SONICORCA/OBJECTS/COIN", "SONICORCA/OBJECTS/RING" }, keys);
	}

	[Fact]
	public async Task Script_InAFolder_ReloadsWhenItsFileChanges_AndKeepsTheOldScriptOnAnError()
	{
		string folder = Path.Combine(Path.GetTempPath(), "s2hd-lua-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(folder);
		try
		{
			string path = Path.Combine(folder, "_.object.lua");
			File.WriteAllText(path, "object { name = \"First\" }");
			Resource resource = new Resource("SONICORCA/OBJECTS/BLOCK", ResourceTypeIdentifier.LuaObject, new FileResourceSource(path, 0, new FileInfo(path).Length));
			LuaObjectType type = await LuaObjectType.LoadAsync(resource, File.ReadAllText(path));
			Assert.Equal("First", type.Name);

			File.WriteAllText(path, "object { name = \"Second\", state = { speed = 2 } }");
			File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddSeconds(1));
			type.ReloadIfChanged();
			Assert.Equal("Second", type.Name);
			Assert.Equal("speed", Assert.Single(type.StateProperties).Name);

			// The checks have a minimum interval, so wait for the next check
			await Task.Delay(600);
			File.WriteAllText(path, "object { name = ");
			File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddSeconds(2));
			type.ReloadIfChanged();
			Assert.Equal("Second", type.Name);
		}
		finally
		{
			Directory.Delete(folder, recursive: true);
		}
	}

	[Fact]
	public async Task Budget_StopsAnEndlessLoop_EvenInPcall_AndTheNextCallRuns()
	{
		LuaObjectType type = await Load("""
			object { name = "Loop" }
			function forever() while true do end end
			function hidden() while true do pcall(function() while true do end end) end end
			function quick() local n = 0 for i = 1, 1000 do n = n + i end return n end
			""");

		Assert.ThrowsAny<Exception>(() => type.CallGlobal("forever"));
		Assert.ThrowsAny<Exception>(() => type.CallGlobal("hidden"));
		// Each call has a new budget
		Assert.Equal(500500.0, type.CallGlobal("quick")[0].Read<double>());
	}

	[Fact]
	public async Task Budget_StopsAnEndlessLoop_WhenTheScriptLoads()
	{
		await Assert.ThrowsAnyAsync<Exception>(() => Load("object { name = \"Loop\" } while true do end"));
	}

	[Theory]
	[InlineData("io")]
	[InlineData("os")]
	[InlineData("debug")]
	[InlineData("require")]
	[InlineData("dofile")]
	[InlineData("loadfile")]
	[InlineData("load")]
	public async Task Sandbox_HasNo(string name)
	{
		// The definition gets the value of the global, so the test can read it
		LuaObjectType type = await Load($"object {{ name = type({name}) }}");

		Assert.Equal("nil", type.Name);
	}
}
