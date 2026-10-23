using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using SonicOrca.Extensions;
using SonicOrca.Resources;

namespace SonicOrca.Audio;

/// <summary>
/// Loads a ".sampleinfo.xml" file into a <see cref="SampleInfo"/>. The file gives the sample path and an optional loop point.
/// </summary>
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
			if (xmlDocument.TryGetNodeInnerText("sampleinfo/loop", out string loopText))
			{
				loopSampleIndex = int.Parse(loopText);
			}
			return new SampleInfo(e.ResourceTree, absolutePath, loopSampleIndex)
			{
				Resource = e.Resource
			};
		});
	}
}
