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
/// Loads an animation group from an ".anigroup.xml" file.
/// </summary>
internal class AnimationGroupResourceType : ResourceType
{
	public override string Name => "animationgroup, xml";

	public override string DefaultExtension => ".anigroup.xml";

	public override bool CompressByDefault => true;

	public AnimationGroupResourceType()
		: base(ResourceTypeIdentifier.AnimationGroup)
	{
	}

	public override async Task<ILoadedResource> LoadAsync(ResourceLoadArgs e, CancellationToken ct = default)
	{
		XmlDocument xmlDocument = new XmlDocument();
		await Task.Run(() =>
		{
			xmlDocument.Load(e.InputStream);
		});
		XmlNode anigroupNode = xmlDocument.SelectSingleNode("anigroup");
		IEnumerable<string> textureResourceKeys = anigroupNode.SelectNodes("textures/texture").OfType<XmlNode>().Select(x => e.GetAbsolutePath(x.InnerText));
		IEnumerable<Animation> animations = anigroupNode.SelectNodes("animations/animation").OfType<XmlNode>().Select(GetAnimationFromXmlNode);
		e.PushDependencies(textureResourceKeys);
		return new AnimationGroup(e.ResourceTree, textureResourceKeys, animations)
		{
			Resource = e.Resource
		};
	}

	/// <summary>
	/// Reads an animation node. Its attributes give the default values for the frames that do not set them.
	/// </summary>
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
		if (node.TryGetAttributeValue("texture", out string value))
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
		IEnumerable<Animation.Frame> frames = node.SelectNodes("frame").OfType<XmlNode>()
			.Select(x => GetFrameFromXmlNode(x, defaultTexture, defaultWidth, defaultHeight, defaultOffsetX, defaultOffsetY, defaultDelay));
		return new Animation(frames, nextFrameIndex, loopFrameIndex);
	}

	private Animation.Frame GetFrameFromXmlNode(XmlNode node, int defaultTexture, int defaultWidth, int defaultHeight, int defaultOffsetX, int defaultOffsetY, int defaultDelay)
	{
		Animation.Frame frame = default;
		int x = int.Parse(node.Attributes["x"].Value);
		int y = int.Parse(node.Attributes["y"].Value);
		int width = node.TryGetAttributeValue("w", out string value) ? int.Parse(value) : defaultWidth;
		int height = node.TryGetAttributeValue("h", out value) ? int.Parse(value) : defaultHeight;
		frame.TextureIndex = node.TryGetAttributeValue("texture", out value) ? int.Parse(value) : defaultTexture;
		int offsetX = node.TryGetAttributeValue("offset_x", out value) ? int.Parse(value) : defaultOffsetX;
		int offsetY = node.TryGetAttributeValue("offset_y", out value) ? int.Parse(value) : defaultOffsetY;
		frame.Delay = node.TryGetAttributeValue("delay", out value) ? int.Parse(value) : defaultDelay;
		frame.Offset = new Vector2i(offsetX, offsetY);
		frame.Source = new Rectanglei(x, y, width, height);
		return frame;
	}
}
