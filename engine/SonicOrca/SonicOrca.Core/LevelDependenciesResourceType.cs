using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using SonicOrca.Resources;

namespace SonicOrca.Core;

public class LevelDependenciesResourceType : ResourceType
{
	public override string Name => "dependencies, xml";

	public override string DefaultExtension => ".dependencies.xml";

	public override bool CompressByDefault => true;

	public LevelDependenciesResourceType()
		: base(ResourceTypeIdentifier.LevelDependencies)
	{
	}

	public override async Task<ILoadedResource> LoadAsync(ResourceLoadArgs e, CancellationToken ct = default(CancellationToken))
	{
		XmlDocument xmlDocument = new XmlDocument();
		await Task.Run(() =>
		{
			xmlDocument.Load(e.InputStream);
		});
		LevelDependencies result = new LevelDependencies
		{
			Resource = e.Resource
		};
		XmlNode xmlNode = xmlDocument.SelectSingleNode("Dependencies");
		e.PushDependencies((from x in xmlNode.SelectNodes("Dependency").OfType<XmlNode>()
			select x.SelectSingleNode("Key").InnerText).Distinct());
		return result;
	}
}
