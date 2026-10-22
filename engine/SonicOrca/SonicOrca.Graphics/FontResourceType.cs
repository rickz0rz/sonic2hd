using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Resources;

namespace SonicOrca.Graphics;

internal class FontResourceType : ResourceType
{
	public override string Name => "font, xml";

	public override string DefaultExtension => ".font.xml";

	public override bool CompressByDefault => true;

	public FontResourceType()
		: base(ResourceTypeIdentifier.Font)
	{
	}

	public override async Task<ILoadedResource> LoadAsync(ResourceLoadArgs e, CancellationToken ct = default(CancellationToken))
	{
		XmlDocument xmlDocument = new XmlDocument();
		await Task.Run(() =>
		{
			xmlDocument.Load(e.InputStream);
		});
		XmlNode xmlNode = xmlDocument.SelectSingleNode("font");
		string absolutePath = e.GetAbsolutePath(xmlNode.SelectSingleNode("shape").InnerText);
		string[] array = (from x in xmlNode.SelectNodes("overlay").OfType<XmlNode>()
			select e.GetAbsolutePath(x.InnerText)).ToArray();
		int defaultWidth = int.Parse(xmlNode.GetNodeInnerText("width", "0"));
		int height = int.Parse(xmlNode.GetNodeInnerText("height", "0"));
		int tracking = int.Parse(xmlNode.GetNodeInnerText("tracking", "0"));
		Vector2i? shadow = null;
		XmlNode xmlNode2 = xmlNode.SelectSingleNode("shadow");
		if (xmlNode2 != null)
		{
			shadow = new Vector2i(int.Parse(xmlNode2.Attributes["x"].Value), int.Parse(xmlNode2.Attributes["y"].Value));
		}
		IEnumerable<Font.CharacterDefinition> characterDefinitions = from x in xmlNode.SelectNodes("chardefs/chardef").OfType<XmlNode>()
			select ParseCharacterDefinition(x);
		e.PushDependency(absolutePath);
		e.PushDependencies(array);
		return new Font(e.ResourceTree, absolutePath, array, defaultWidth, height, tracking, shadow, characterDefinitions)
		{
			Resource = e.Resource
		};
	}

	private Font.CharacterDefinition ParseCharacterDefinition(XmlNode chardefNode)
	{
		char key = chardefNode.Attributes["char"].Value.First();
		XmlNode xmlNode = chardefNode.SelectSingleNode("rect");
		Rectanglei sourceRectangle = new Rectanglei(int.Parse(xmlNode.Attributes["x"].Value), int.Parse(xmlNode.Attributes["y"].Value), int.Parse(xmlNode.Attributes["w"].Value), int.Parse(xmlNode.Attributes["h"].Value));
		XmlNode xmlNode2 = chardefNode.SelectSingleNode("offset");
		string value;
		return new Font.CharacterDefinition(offset: (xmlNode2 == null) ? default(Vector2i) : new Vector2i(int.Parse(xmlNode2.Attributes["x"].Value), int.Parse(xmlNode2.Attributes["y"].Value)), width: chardefNode.TryGetNodeInnerText("width", out value) ? int.Parse(value) : sourceRectangle.Width, key: key, sourceRectangle: sourceRectangle);
	}
}
