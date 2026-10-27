using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SonicOrca.Resources;

namespace SonicOrca.Core.Objects;

/// <summary>
/// Loads an object type from a C# script resource (".object.cs").
/// The script must have one class that inherits <see cref="ObjectType"/> and one class that inherits <see cref="ActiveObject"/>.
/// </summary>
internal class ObjectTypeResourceType : ResourceType
{
	public override string Name => "object, cs";

	public override string DefaultExtension => ".object.cs";

	public ObjectTypeResourceType()
		: base(ResourceTypeIdentifier.Object)
	{
	}

	public override async Task<ILoadedResource> LoadAsync(ResourceLoadArgs e, CancellationToken ct = default)
	{
		Type[] compiledTypes = ScriptImport.Compile(await new StreamReader(e.InputStream).ReadToEndAsync());
		Type objectTypeClass = compiledTypes.FirstOrDefault(x => typeof(ObjectType).IsAssignableFrom(x));
		Type activeObjectClass = compiledTypes.FirstOrDefault(x => typeof(ActiveObject).IsAssignableFrom(x));
		if (objectTypeClass == null)
		{
			throw new ResourceException("No class inheriting ObjectType found.");
		}
		if (activeObjectClass == null)
		{
			throw new ResourceException("No class inheriting ActiveObject found.");
		}
		ObjectType objectType = (ObjectType)Activator.CreateInstance(objectTypeClass);
		objectType.Resource = e.Resource;
		e.PushDependencies(objectType.Dependencies.Select(x => e.GetAbsolutePath(x)));
		e.PushDependencies(objectType.OptionalDependencies.Select(x => e.GetAbsolutePath(x)).Where(key => e.ResourceTree[key]?.Resource != null));
		return objectType;
	}
}
