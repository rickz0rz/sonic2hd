using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using SonicOrca.Extensions;
using SonicOrca.Resources;

namespace SonicOrca.Audio;

internal class SampleInfoResourceType : ResourceType
{
	public override string Name => "sample info";

	public override string DefaultExtension => ".sampleinfo.xml";

	public override bool CompressByDefault => true;

	public SampleInfoResourceType()
		: base(ResourceTypeIdentifier.SampleInfo)
	{
	}

	public override async Task<ILoadedResource> LoadAsync(ResourceLoadArgs e, CancellationToken ct = default(CancellationToken))
	{
		return await Task.Run(() =>
		{
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.Load(e.InputStream);
			string absolutePath = e.GetAbsolutePath(xmlDocument.SelectSingleNode("sampleinfo/sample").InnerText);
			e.PushDependency(absolutePath);
			int? loopSampleIndex = null;
			if (xmlDocument.TryGetNodeInnerText("sampleinfo/loop", out var value))
			{
				loopSampleIndex = int.Parse(value);
			}
			return new SampleInfo(e.ResourceTree, absolutePath, loopSampleIndex)
			{
				Resource = e.Resource
			};
		});
	}
}
