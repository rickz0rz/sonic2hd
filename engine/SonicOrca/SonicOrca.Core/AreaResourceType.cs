using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SonicOrca.Resources;

namespace SonicOrca.Core;

internal class AreaResourceType : ResourceType
{
	public override string Name => "area, cs";

	public override string DefaultExtension => ".area.cs";

	public override bool CompressByDefault => true;

	public AreaResourceType()
		: base(ResourceTypeIdentifier.Area)
	{
	}

	public override async Task<ILoadedResource> LoadAsync(ResourceLoadArgs e, CancellationToken ct = default(CancellationToken))
	{
		Type type = ScriptImport.Compile(await new StreamReader(e.InputStream).ReadToEndAsync()).FirstOrDefault((Type x) => typeof(Area).IsAssignableFrom(x));
		if (type == null)
		{
			throw new ResourceException("No class inheriting Area found.");
		}
		Area area = (Area)Activator.CreateInstance(type);
		area.Resource = e.Resource;
		e.PushDependencies(area.Dependencies.Select((string x) => e.GetAbsolutePath(x)));
		return area;
	}
}
