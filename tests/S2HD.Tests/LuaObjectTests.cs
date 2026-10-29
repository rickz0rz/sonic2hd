using System;
using System.Collections.Generic;
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
		return LuaObjectType.LoadAsync(resource, source);
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
