using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Resources;

namespace SonicOrca.Graphics;

/// <summary>
/// Loads a font from a ".font.xml" file.
/// </summary>
internal class FontResourceType : ResourceType
{
	public override string Name => "font, xml";

	public override string DefaultExtension => ".font.xml";

	public override bool CompressByDefault => true;

	public FontResourceType()
		: base(ResourceTypeIdentifier.Font)
	{
	}

	public override async Task<ILoadedResource> LoadAsync(ResourceLoadArgs e, CancellationToken ct = default)
	{
		XmlDocument xmlDocument = new XmlDocument();
		await Task.Run(() =>
		{
			xmlDocument.Load(e.InputStream);
		});
		XmlNode fontNode = xmlDocument.SelectSingleNode("font");
		string shapeResourceKey = e.GetAbsolutePath(fontNode.SelectSingleNode("shape").InnerText);
		string[] overlayResourceKeys = fontNode.SelectNodes("overlay").OfType<XmlNode>().Select(x => e.GetAbsolutePath(x.InnerText)).ToArray();
		int defaultWidth = int.Parse(fontNode.GetNodeInnerText("width", "0"));
		int height = int.Parse(fontNode.GetNodeInnerText("height", "0"));
		int tracking = int.Parse(fontNode.GetNodeInnerText("tracking", "0"));
		Vector2i? shadow = null;
		XmlNode shadowNode = fontNode.SelectSingleNode("shadow");
		if (shadowNode != null)
		{
			shadow = new Vector2i(int.Parse(shadowNode.Attributes["x"].Value), int.Parse(shadowNode.Attributes["y"].Value));
		}
		IEnumerable<Font.CharacterDefinition> characterDefinitions = fontNode.SelectNodes("chardefs/chardef").OfType<XmlNode>().Select(ParseCharacterDefinition);
		e.PushDependency(shapeResourceKey);
		e.PushDependencies(overlayResourceKeys);
		return new Font(e.ResourceTree, shapeResourceKey, overlayResourceKeys, defaultWidth, height, tracking, shadow, characterDefinitions)
		{
			Resource = e.Resource
		};
	}

	private Font.CharacterDefinition ParseCharacterDefinition(XmlNode chardefNode)
	{
		char key = chardefNode.Attributes["char"].Value.First();
		XmlNode rectNode = chardefNode.SelectSingleNode("rect");
		Rectanglei sourceRectangle = new Rectanglei(int.Parse(rectNode.Attributes["x"].Value), int.Parse(rectNode.Attributes["y"].Value), int.Parse(rectNode.Attributes["w"].Value), int.Parse(rectNode.Attributes["h"].Value));
		XmlNode offsetNode = chardefNode.SelectSingleNode("offset");
		Vector2i offset = (offsetNode == null) ? default : new Vector2i(int.Parse(offsetNode.Attributes["x"].Value), int.Parse(offsetNode.Attributes["y"].Value));
		int width = chardefNode.TryGetNodeInnerText("width", out string widthText) ? int.Parse(widthText) : sourceRectangle.Width;
		return new Font.CharacterDefinition(key, sourceRectangle, offset, width);
	}
}
