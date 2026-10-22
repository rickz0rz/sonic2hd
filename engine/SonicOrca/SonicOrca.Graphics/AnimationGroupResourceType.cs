using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Resources;

namespace SonicOrca.Graphics;

internal class AnimationGroupResourceType : ResourceType
{
	public override string Name => "animationgroup, xml";

	public override string DefaultExtension => ".anigroup.xml";

	public override bool CompressByDefault => true;

	public AnimationGroupResourceType()
		: base(ResourceTypeIdentifier.AnimationGroup)
	{
	}

	public override async Task<ILoadedResource> LoadAsync(ResourceLoadArgs e, CancellationToken ct = default(CancellationToken))
	{
		XmlDocument xmlDocument = new XmlDocument();
		await Task.Run(() =>
		{
			xmlDocument.Load(e.InputStream);
		});
		XmlNode xmlNode = xmlDocument.SelectSingleNode("anigroup");
		IEnumerable<string> enumerable = from x in xmlNode.SelectNodes("textures/texture").OfType<XmlNode>()
			select e.GetAbsolutePath(x.InnerText);
		IEnumerable<Animation> animations = from x in xmlNode.SelectNodes("animations/animation").OfType<XmlNode>()
			select GetAnimationFromXmlNode(x);
		e.PushDependencies(enumerable);
		return new AnimationGroup(e.ResourceTree, enumerable, animations)
		{
			Resource = e.Resource
		};
	}

	private Animation GetAnimationFromXmlNode(XmlNode node)
	{
		int? nextFrameIndex = null;
		int? loopFrameIndex = null;
		int defaultTexture = 0;
		int defaultWidth = 0;
		int defaultHeight = 0;
		int defaultOffsetX = 0;
		int defaultOffsetY = 0;
		int defaultDelay = 0;
		if (node.TryGetAttributeValue("texture", out var value))
		{
			defaultTexture = int.Parse(value);
		}
		if (node.TryGetAttributeValue("w", out value))
		{
			defaultWidth = int.Parse(value);
		}
		if (node.TryGetAttributeValue("h", out value))
		{
			defaultHeight = int.Parse(value);
		}
		if (node.TryGetAttributeValue("offset_x", out value))
		{
			defaultOffsetX = int.Parse(value);
		}
		if (node.TryGetAttributeValue("offset_y", out value))
		{
			defaultOffsetY = int.Parse(value);
		}
		if (node.TryGetAttributeValue("delay", out value))
		{
			defaultDelay = int.Parse(value);
		}
		if (node.TryGetAttributeValue("next", out value))
		{
			nextFrameIndex = int.Parse(value);
		}
		if (node.TryGetAttributeValue("loop", out value))
		{
			loopFrameIndex = int.Parse(value);
		}
		return new Animation(from x in node.SelectNodes("frame").OfType<XmlNode>()
			select GetFrameFromXmlNode(x, defaultTexture, defaultWidth, defaultHeight, defaultOffsetX, defaultOffsetY, defaultDelay), nextFrameIndex, loopFrameIndex);
	}

	private Animation.Frame GetFrameFromXmlNode(XmlNode node, int defaultTexture, int defaultWidth, int defaultHeight, int defaultOffsetX, int defaultOffsetY, int defaultDelay)
	{
		Animation.Frame result = default;
		int x = int.Parse(node.Attributes["x"].Value);
		int y = int.Parse(node.Attributes["y"].Value);
		int width = (node.TryGetAttributeValue("w", out var value) ? int.Parse(value) : defaultWidth);
		int height = (node.TryGetAttributeValue("h", out value) ? int.Parse(value) : defaultHeight);
		result.TextureIndex = (node.TryGetAttributeValue("texture", out value) ? int.Parse(value) : defaultTexture);
		int x2 = (node.TryGetAttributeValue("offset_x", out value) ? int.Parse(value) : defaultOffsetX);
		int y2 = (node.TryGetAttributeValue("offset_y", out value) ? int.Parse(value) : defaultOffsetY);
		result.Delay = (node.TryGetAttributeValue("delay", out value) ? int.Parse(value) : defaultDelay);
		result.Offset = new Vector2i(x2, y2);
		result.Source = new Rectanglei(x, y, width, height);
		return result;
	}
}
