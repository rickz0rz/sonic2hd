using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using SonicOrca.Extensions;
using SonicOrca.Resources;

namespace SonicOrca.Graphics.V2.Animation;

/// <summary>
/// Loads a composition group from a ".composition.xml" file. The file holds one composition with its assets,
/// its layers and the key frames of each layer transform.
/// </summary>
public class CompositionGroupResourceType : ResourceType
{
	/// <summary>
	/// The number of components (X, Y and Z) in a vector key frame value.
	/// </summary>
	private const int VectorComponentCount = 3;

	public override string Name => "composition, xml";

	public override string DefaultExtension => ".composition.xml";

	public CompositionGroupResourceType()
		: base(ResourceTypeIdentifier.CompositionGroup)
	{
	}

	public override async Task<ILoadedResource> LoadAsync(ResourceLoadArgs e, CancellationToken ct = default)
	{
		List<string> textureResourceKeys = new List<string>();
		List<CompositionAsset> assets = new List<CompositionAsset>();
		List<CompositionLayer> layers = new List<CompositionLayer>();
		List<Composition> compositions = new List<Composition>();
		XmlDocument xmlDocument = new XmlDocument();
		await Task.Run(() =>
		{
			xmlDocument.Load(e.InputStream);
		});
		XmlNode rootNode = xmlDocument.SelectSingleNode("root");
		rootNode.TryGetNodeInnerText("v", out string version);
		rootNode.TryGetNodeInnerText("fr", out string frameRateText);
		rootNode.TryGetNodeInnerText("ip", out string startFrameText);
		rootNode.TryGetNodeInnerText("op", out string endFrameText);
		rootNode.TryGetNodeInnerText("w", out string widthText);
		rootNode.TryGetNodeInnerText("h", out string heightText);
		rootNode.TryGetNodeInnerText("nm", out string compositionName);
		foreach (XmlNode assetNode in rootNode.SelectNodes("assets").OfType<XmlNode>())
		{
			assetNode.TryGetNodeInnerText("id", out string assetId);
			assetNode.TryGetNodeInnerText("w", out string assetWidthText);
			assetNode.TryGetNodeInnerText("h", out string assetHeightText);
			assetNode.TryGetNodeInnerText("u", out string assetPath);
			assetNode.TryGetNodeInnerText("p", out string assetFileName);
			assets.Add(new CompositionAsset(assetId, int.Parse(assetWidthText), int.Parse(assetHeightText), assetPath, assetFileName));
		}
		foreach (XmlNode layerNode in rootNode.SelectNodes("layers").OfType<XmlNode>())
		{
			layerNode.TryGetNodeInnerText("ind", out string indexText);
			layerNode.TryGetNodeInnerText("ty", out string layerTypeText);
			layerNode.TryGetNodeInnerText("td", out string layerSubTypeText);
			layerNode.TryGetNodeInnerText("nm", out string layerName);
			layerNode.TryGetNodeInnerText("bm", out string blendModeText);
			layerNode.TryGetNodeInnerText("cl", out string fileExtension);
			layerNode.TryGetNodeInnerText("refId", out string textureReference);
			layerNode.TryGetNodeInnerText("ip", out string layerStartFrameText);
			layerNode.TryGetNodeInnerText("op", out string layerEndFrameText);
			uint index = uint.Parse(indexText);
			uint layerType = uint.Parse(layerTypeText);
			// The sub type and the blend mode are optional. A missing or bad value gives 0.
			uint.TryParse(layerSubTypeText, out uint layerSubType);
			uint.TryParse(blendModeText, out uint blendMode);
			uint layerStartFrame = uint.Parse(layerStartFrameText);
			uint layerEndFrame = uint.Parse(layerEndFrameText);
			XmlNode transformNode = layerNode.SelectSingleNode("ks");
			XmlNode opacityNode = transformNode.SelectSingleNode("o");
			XmlNode rotationNode = transformNode.SelectSingleNode("r");
			XmlNode positionNode = transformNode.SelectSingleNode("p");
			XmlNode scaleNode = transformNode.SelectSingleNode("s");
			List<CompositionLayerTween> tweens = new List<CompositionLayerTween>();
			tweens.AddRange(ParseScalarTransform(opacityNode, CompositionLayerTween.Type.OPACITY, layerStartFrame, layerEndFrame));
			tweens.AddRange(ParseScalarTransform(rotationNode, CompositionLayerTween.Type.ROTATION, layerStartFrame, layerEndFrame));
			tweens.AddRange(ParseVectorTransform(positionNode, CompositionLayerTween.Type.POSITION, layerStartFrame, layerEndFrame));
			tweens.AddRange(ParseVectorTransform(scaleNode, CompositionLayerTween.Type.SCALE, layerStartFrame, layerEndFrame));
			CompositionLayerAnimatableTransform transform = new CompositionLayerAnimatableTransform();
			foreach (CompositionLayerTween tween in tweens)
			{
				transform.AddKeyFrameTween(tween);
			}
			layers.Add(new CompositionLayer(index, layerType, layerSubType, blendMode, layerName, fileExtension, textureReference, layerStartFrame, layerEndFrame, transform));
		}
		layers = layers.OrderBy(l => l.Index).ToList();
		uint frameRate = uint.Parse(frameRateText);
		uint startFrame = uint.Parse(startFrameText);
		uint endFrame = uint.Parse(endFrameText);
		uint width = uint.Parse(widthText);
		uint height = uint.Parse(heightText);
		compositions.Add(new Composition(version, frameRate, startFrame, endFrame, width, height, compositionName, layers));

		// The texture key is the folder of this resource, the asset path and the asset file name without its
		// extension. An underscore in the file name is a folder separator.
		string fullKeyPath = e.Resource.FullKeyPath;
		string folderKeyPath = fullKeyPath.Remove(fullKeyPath.LastIndexOf("/"));
		foreach (CompositionAsset asset in assets)
		{
			string assetKey = asset.FileName.Remove(asset.FileName.LastIndexOf("."));
			assetKey = assetKey.Replace('_', '/');
			textureResourceKeys.Add(folderKeyPath + "/" + asset.Path.ToUpper() + assetKey.ToUpper());
		}
		e.PushDependencies(textureResourceKeys);
		return new CompositionGroup(e.ResourceTree, textureResourceKeys, assets, compositions)
		{
			Resource = e.Resource
		};
	}

	private IEnumerable<CompositionLayerTween> ParseScalarTransform(XmlNode transformNode, CompositionLayerTween.Type tweenType, uint layerStartFrame, uint layerEndFrame)
	{
		List<CompositionLayerTween> tweens = new List<CompositionLayerTween>();
		if (TransformPropertyHasKeyFrames(transformNode))
		{
			tweens.AddRange(ParseScalarTweenKeySet(transformNode.SelectNodes("k").OfType<XmlNode>(), tweenType));
		}
		else
		{
			tweens.Add(ParseSimpleTweenTransform(transformNode, tweenType, layerStartFrame, layerEndFrame));
		}
		return tweens;
	}

	private IEnumerable<CompositionLayerTween> ParseVectorTransform(XmlNode transformNode, CompositionLayerTween.Type tweenType, uint layerStartFrame, uint layerEndFrame)
	{
		List<CompositionLayerTween> tweens = new List<CompositionLayerTween>();
		if (TransformPropertyHasKeyFrames(transformNode))
		{
			tweens.AddRange(ParseVectorTweenKeySet(transformNode.SelectNodes("k").OfType<XmlNode>(), tweenType));
		}
		else
		{
			tweens.Add(ParseSimpleTweenTransform(transformNode, tweenType, layerStartFrame, layerEndFrame));
		}
		return tweens;
	}

	/// <summary>
	/// Makes one tween for each pair of adjacent key frames. Each tween starts at the frame of a key and ends at the
	/// frame of the next key. A key without start and end values keeps the end value of the tween before it.
	/// </summary>
	private IEnumerable<CompositionLayerTween> ParseScalarTweenKeySet(IEnumerable<XmlNode> tweenKeySet, CompositionLayerTween.Type tweenType)
	{
		if (tweenType != CompositionLayerTween.Type.OPACITY && tweenType != CompositionLayerTween.Type.ROTATION)
		{
			throw new NotImplementedException();
		}
		Type tweenClass = GetTweenClass(tweenType);
		List<CompositionLayerTween> tweens = new List<CompositionLayerTween>();
		XmlNode lastKeyNode = tweenKeySet.First();
		XmlNode firstKeyNode = tweenKeySet.First();
		firstKeyNode.TryGetNodeInnerText("t", out string firstTimeText);
		if (!TryParseScalarStartEnd(firstKeyNode, out KeyValuePair<double, double> firstStartEnd))
		{
			firstStartEnd = tweenType switch
			{
				CompositionLayerTween.Type.OPACITY => new KeyValuePair<double, double>(100.0, 100.0),
				CompositionLayerTween.Type.ROTATION => new KeyValuePair<double, double>(0.0, 0.0),
				_ => throw new NotImplementedException(),
			};
		}
		uint firstFrame = ParseFrame(firstTimeText);
		// The open tween has no end frame yet. The next key gives it one.
		CompositionLayerTween openTween = (CompositionLayerTween)Activator.CreateInstance(tweenClass, firstFrame, uint.MaxValue, firstStartEnd);
		foreach (XmlNode keyNode in tweenKeySet.Skip(1))
		{
			lastKeyNode = keyNode;
			keyNode.TryGetNodeInnerText("t", out string timeText);
			if (!TryParseScalarStartEnd(keyNode, out KeyValuePair<double, double> nextStartEnd))
			{
				nextStartEnd = new KeyValuePair<double, double>(openTween.EndValues.First().Value, openTween.EndValues.First().Value);
			}
			KeyValuePair<double, double> closedStartEnd = new KeyValuePair<double, double>(openTween.StartValues.First().Value, openTween.EndValues.First().Value);
			uint frame = ParseFrame(timeText);
			openTween = (CompositionLayerTween)Activator.CreateInstance(tweenClass, openTween.StartFrame, frame, closedStartEnd);
			tweens.Add(openTween);
			if (keyNode != tweenKeySet.Last())
			{
				openTween = (CompositionLayerTween)Activator.CreateInstance(tweenClass, frame, uint.MaxValue, nextStartEnd);
			}
		}
		if (lastKeyNode == tweenKeySet.First())
		{
			throw new NotSupportedException();
		}
		return tweens;
	}

	/// <summary>
	/// Does the same as <see cref="ParseScalarTweenKeySet"/> for a value with X, Y and Z components.
	/// </summary>
	private IEnumerable<CompositionLayerTween> ParseVectorTweenKeySet(IEnumerable<XmlNode> tweenKeySet, CompositionLayerTween.Type tweenType)
	{
		if (tweenType != CompositionLayerTween.Type.POSITION && tweenType != CompositionLayerTween.Type.SCALE)
		{
			throw new NotImplementedException();
		}
		Type tweenClass = GetTweenClass(tweenType);
		List<CompositionLayerTween> tweens = new List<CompositionLayerTween>();
		XmlNode lastKeyNode = tweenKeySet.First();
		XmlNode firstKeyNode = tweenKeySet.First();
		firstKeyNode.TryGetNodeInnerText("t", out string firstTimeText);
		List<KeyValuePair<double, double>> firstStartEnds = TryParseVectorStartEnds(firstKeyNode);
		if (firstStartEnds == null)
		{
			double defaultValue = tweenType switch
			{
				CompositionLayerTween.Type.SCALE => 100.0,
				CompositionLayerTween.Type.POSITION => 0.0,
				_ => throw new NotImplementedException(),
			};
			firstStartEnds = new List<KeyValuePair<double, double>>();
			for (int i = 0; i < VectorComponentCount; i++)
			{
				firstStartEnds.Add(new KeyValuePair<double, double>(defaultValue, defaultValue));
			}
		}
		uint firstFrame = ParseFrame(firstTimeText);
		// The open tween has no end frame yet. The next key gives it one.
		CompositionLayerTween openTween = (CompositionLayerTween)Activator.CreateInstance(tweenClass, firstFrame, uint.MaxValue, firstStartEnds[0], firstStartEnds[1], firstStartEnds[2]);
		foreach (XmlNode keyNode in tweenKeySet.Skip(1))
		{
			lastKeyNode = keyNode;
			keyNode.TryGetNodeInnerText("t", out string timeText);
			List<KeyValuePair<double, double>> nextStartEnds = TryParseVectorStartEnds(keyNode);
			if (nextStartEnds == null)
			{
				nextStartEnds = new List<KeyValuePair<double, double>>();
				for (int i = 0; i < VectorComponentCount; i++)
				{
					nextStartEnds.Add(new KeyValuePair<double, double>(openTween.EndValues.ElementAt(i).Value, openTween.EndValues.ElementAt(i).Value));
				}
			}
			List<KeyValuePair<double, double>> closedStartEnds = new List<KeyValuePair<double, double>>();
			for (int i = 0; i < VectorComponentCount; i++)
			{
				closedStartEnds.Add(new KeyValuePair<double, double>(openTween.StartValues.ElementAt(i).Value, openTween.EndValues.ElementAt(i).Value));
			}
			uint frame = ParseFrame(timeText);
			openTween = (CompositionLayerTween)Activator.CreateInstance(tweenClass, openTween.StartFrame, frame, closedStartEnds[0], closedStartEnds[1], closedStartEnds[2]);
			tweens.Add(openTween);
			if (keyNode != tweenKeySet.Last())
			{
				openTween = (CompositionLayerTween)Activator.CreateInstance(tweenClass, frame, uint.MaxValue, nextStartEnds[0], nextStartEnds[1], nextStartEnds[2]);
			}
		}
		if (lastKeyNode == tweenKeySet.First())
		{
			throw new NotSupportedException();
		}
		return tweens;
	}

	/// <summary>
	/// Makes one tween with a constant value for a transform property that has no key frames.
	/// </summary>
	private CompositionLayerTween ParseSimpleTweenTransform(XmlNode tweenKey, CompositionLayerTween.Type tweenType, uint layerStartFrame, uint layerEndFrame)
	{
		Type tweenClass = GetTweenClass(tweenType);
		CompositionLayerTween tween;
		if (tweenType == CompositionLayerTween.Type.OPACITY || tweenType == CompositionLayerTween.Type.ROTATION)
		{
			double value = ParseDouble(tweenKey.SelectSingleNode("k").InnerText);
			KeyValuePair<double, double> startEnd = new KeyValuePair<double, double>(value, value);
			tween = (CompositionLayerTween)Activator.CreateInstance(tweenClass, layerStartFrame, layerEndFrame, startEnd);
		}
		else
		{
			IEnumerable<XmlNode> componentNodes = tweenKey.SelectNodes("k").OfType<XmlNode>();
			if (componentNodes.Count() != VectorComponentCount)
			{
				throw new NotSupportedException();
			}
			// Only X and Y are used. The Z component is ignored.
			string xText = componentNodes.ElementAt(0).InnerText;
			string yText = componentNodes.ElementAt(1).InnerText;
			double x = ParseDouble(xText);
			double y = ParseDouble(yText);
			KeyValuePair<double, double> startEndX = new KeyValuePair<double, double>(x, x);
			KeyValuePair<double, double> startEndY = new KeyValuePair<double, double>(y, y);
			tween = (CompositionLayerTween)Activator.CreateInstance(tweenClass, layerStartFrame, layerEndFrame, startEndX, startEndY);
		}
		tween.HasKeyFrames = false;
		return tween;
	}

	private bool TransformPropertyHasKeyFrames(XmlNode node)
	{
		return node.SelectSingleNode("k").SelectSingleNode("t") != null;
	}

	private static Type GetTweenClass(CompositionLayerTween.Type tweenType)
	{
		return tweenType switch
		{
			CompositionLayerTween.Type.OPACITY => typeof(CompositionLayerOpacityTween),
			CompositionLayerTween.Type.ROTATION => typeof(CompositionLayerRotationTween),
			CompositionLayerTween.Type.POSITION => typeof(CompositionLayerPositionTween),
			CompositionLayerTween.Type.SCALE => typeof(CompositionLayerScaleTween),
			_ => null,
		};
	}

	/// <summary>
	/// Reads the start ("s") and end ("e") values of a scalar key. Returns false when one of them is missing or empty.
	/// </summary>
	private static bool TryParseScalarStartEnd(XmlNode keyNode, out KeyValuePair<double, double> startEnd)
	{
		keyNode.TryGetNodeInnerText("s", out string startText);
		keyNode.TryGetNodeInnerText("e", out string endText);
		if (string.IsNullOrEmpty(startText) || string.IsNullOrEmpty(endText))
		{
			startEnd = default;
			return false;
		}
		startEnd = new KeyValuePair<double, double>(ParseDouble(startText), ParseDouble(endText));
		return true;
	}

	/// <summary>
	/// Reads the start ("s") and end ("e") values of each component of a vector key. Returns null when the values
	/// are missing or empty.
	/// </summary>
	private static List<KeyValuePair<double, double>> TryParseVectorStartEnds(XmlNode keyNode)
	{
		List<string> startTexts = new List<string>();
		List<string> endTexts = new List<string>();
		IEnumerable<XmlNode> startNodes = keyNode.SelectNodes("s").OfType<XmlNode>();
		IEnumerable<XmlNode> endNodes = keyNode.SelectNodes("e").OfType<XmlNode>();
		if (startNodes.Count() == VectorComponentCount && endNodes.Count() == VectorComponentCount)
		{
			for (int i = 0; i < VectorComponentCount; i++)
			{
				startTexts.Add(startNodes.ElementAt(i).InnerText);
			}
			for (int i = 0; i < VectorComponentCount; i++)
			{
				endTexts.Add(endNodes.ElementAt(i).InnerText);
			}
		}
		else if (startNodes.Count() != 0 && endNodes.Count() != 0)
		{
			throw new NotImplementedException();
		}
		if (!startTexts.All(v => !string.IsNullOrEmpty(v)) || !endTexts.All(v => !string.IsNullOrEmpty(v)) || startTexts.Count <= 0 || endTexts.Count <= 0)
		{
			return null;
		}
		List<KeyValuePair<double, double>> startEnds = new List<KeyValuePair<double, double>>();
		for (int i = 0; i < VectorComponentCount; i++)
		{
			startEnds.Add(new KeyValuePair<double, double>(ParseDouble(startTexts[i]), ParseDouble(endTexts[i])));
		}
		return startEnds;
	}

	private static double ParseDouble(string text)
	{
		return double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
	}

	/// <summary>
	/// Reads a key frame time. The time can have a fraction, so it is rounded to the nearest frame.
	/// </summary>
	private static uint ParseFrame(string text)
	{
		return (uint)Math.Round(ParseDouble(text));
	}
}
