using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SonicOrca.Resources;

namespace SonicOrca.Core.Objects;

internal class ObjectTypeResourceType : ResourceType
{
	public override string Name => "object, cs";

	public override string DefaultExtension => ".object.cs";

	public ObjectTypeResourceType()
		: base(ResourceTypeIdentifier.Object)
	{
	}

	public override async Task<ILoadedResource> LoadAsync(ResourceLoadArgs e, CancellationToken ct = default(CancellationToken))
	{
		Type[] source = ScriptImport.Compile(await new StreamReader(e.InputStream).ReadToEndAsync());
		Type type = source.FirstOrDefault((Type x) => typeof(ObjectType).IsAssignableFrom(x));
		Type type2 = source.FirstOrDefault((Type x) => typeof(ActiveObject).IsAssignableFrom(x));
		if (type == null)
		{
			throw new ResourceException("No class inheriting ObjectType found.");
		}
		if (type2 == null)
		{
			throw new ResourceException("No class inheriting ActiveObject found.");
		}
		ObjectType objectType = (ObjectType)Activator.CreateInstance(type);
		objectType.Resource = e.Resource;
		e.PushDependencies(objectType.Dependencies.Select((string x) => e.GetAbsolutePath(x)));
		return objectType;
	}
}
