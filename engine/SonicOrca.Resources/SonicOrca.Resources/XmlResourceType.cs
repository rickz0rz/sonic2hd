using System;
using System.Threading;
using System.Threading.Tasks;

namespace SonicOrca.Resources;

public class XmlResourceType : ResourceType
{
	public override string Name => "xml";

	public override string DefaultExtension => ".xml";

	public override bool CompressByDefault => true;

	public XmlResourceType()
		: base(ResourceTypeIdentifier.Xml)
	{
	}

	public override Task<ILoadedResource> LoadAsync(ResourceLoadArgs e, CancellationToken ct = default(CancellationToken))
	{
		return Task.Run((Func<ILoadedResource>)(() => new XmlLoadedResource(e.InputStream)
		{
			Resource = e.Resource
		}));
	}
}
