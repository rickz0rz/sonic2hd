using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SonicOrca.Resources;

namespace SonicOrca.Core.Scripting;

/// <summary>
/// Loads an object type from a Lua script resource (".object.lua"). A mod adds an object with a folder such as
/// SONICORCA/OBJECTS/NAME, with the script in the file "_.object.lua" and the animations and textures next to it.
/// </summary>
internal class LuaObjectResourceType : ResourceType
{
	public override string Name => "object, lua";

	public override string DefaultExtension => ".object.lua";

	public LuaObjectResourceType()
		: base(ResourceTypeIdentifier.LuaObject)
	{
	}

	public override async Task<ILoadedResource> LoadAsync(ResourceLoadArgs e, CancellationToken ct = default)
	{
		string source = await new StreamReader(e.InputStream).ReadToEndAsync(ct);
		LuaObjectType objectType = await LuaObjectType.LoadAsync(e.Resource, source, ct);
		e.PushDependencies(objectType.Dependencies.Select(x => e.GetAbsolutePath(x)));
		return objectType;
	}
}
