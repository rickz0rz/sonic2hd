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

public class CompositionGroupResourceType : ResourceType
{
	public override string Name => "composition, xml";

	public override string DefaultExtension => ".composition.xml";

	public CompositionGroupResourceType()
		: base(ResourceTypeIdentifier.CompositionGroup)
	{
	}

	public override async Task<ILoadedResource> LoadAsync(ResourceLoadArgs e, CancellationToken ct = default(CancellationToken))
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
		XmlNode xmlNode = xmlDocument.SelectSingleNode("root");
		string value = "";
		string value2 = "";
		string value3 = "";
		string value4 = "";
		string value5 = "";
		string value6 = "";
		string value7 = "";
		xmlNode.TryGetNodeInnerText("v", out value);
		xmlNode.TryGetNodeInnerText("fr", out value2);
		xmlNode.TryGetNodeInnerText("ip", out value3);
		xmlNode.TryGetNodeInnerText("op", out value4);
		xmlNode.TryGetNodeInnerText("w", out value5);
		xmlNode.TryGetNodeInnerText("h", out value6);
		xmlNode.TryGetNodeInnerText("nm", out value7);
		foreach (XmlNode item in xmlNode.SelectNodes("assets").OfType<XmlNode>())
		{
			string value8 = "";
			string value9 = "";
			string value10 = "";
			string value11 = "";
			string value12 = "";
			item.TryGetNodeInnerText("id", out value8);
			item.TryGetNodeInnerText("w", out value9);
			item.TryGetNodeInnerText("h", out value10);
			item.TryGetNodeInnerText("u", out value11);
			item.TryGetNodeInnerText("p", out value12);
			assets.Add(new CompositionAsset(value8, int.Parse(value9), int.Parse(value10), value11, value12));
		}
		foreach (XmlNode item2 in xmlNode.SelectNodes("layers").OfType<XmlNode>())
		{
			string value13 = "";
			string value14 = "";
			string value15 = "";
			string value16 = "";
			string value17 = "";
			string value18 = "";
			string value19 = "";
			string value20 = "";
			string value21 = "";
			string value22 = "";
			item2.TryGetNodeInnerText("ind", out value13);
			item2.TryGetNodeInnerText("ty", out value14);
			item2.TryGetNodeInnerText("td", out value15);
			item2.TryGetNodeInnerText("nm", out value16);
			item2.TryGetNodeInnerText("bm", out value22);
			item2.TryGetNodeInnerText("cl", out value17);
			item2.TryGetNodeInnerText("refId", out value18);
			item2.TryGetNodeInnerText("sr", out value19);
			item2.TryGetNodeInnerText("ip", out value20);
			item2.TryGetNodeInnerText("op", out value21);
			uint index = uint.Parse(value13);
			uint layerType = uint.Parse(value14);
			uint result = 0u;
			uint.TryParse(value15, out result);
			uint result2 = 0u;
			uint.TryParse(value22, out result2);
			string name = value16;
			string fileExtension = value17;
			string textureReference = value18;
			uint num = uint.Parse(value20);
			uint num2 = uint.Parse(value21);
			XmlNode xmlNode2 = item2.SelectSingleNode("ks");
			XmlNode transformNode = xmlNode2.SelectSingleNode("o");
			XmlNode xmlNode3 = xmlNode2.SelectSingleNode("r");
			XmlNode xmlNode4 = xmlNode2.SelectSingleNode("p");
			XmlNode xmlNode5 = xmlNode2.SelectSingleNode("s");
			xmlNode3.SelectNodes("k").OfType<XmlNode>();
			xmlNode4.SelectNodes("k").OfType<XmlNode>();
			xmlNode5.SelectNodes("k").OfType<XmlNode>();
			List<CompositionLayerTween> list = new List<CompositionLayerTween>();
			list.AddRange(ParseScalarTransform(transformNode, CompositionLayerTween.Type.OPACITY, num, num2));
			list.AddRange(ParseScalarTransform(xmlNode3, CompositionLayerTween.Type.ROTATION, num, num2));
			list.AddRange(ParseVectorTransform(xmlNode4, CompositionLayerTween.Type.POSITION, num, num2));
			list.AddRange(ParseVectorTransform(xmlNode5, CompositionLayerTween.Type.SCALE, num, num2));
			CompositionLayerAnimatableTransform compositionLayerAnimatableTransform = new CompositionLayerAnimatableTransform();
			foreach (CompositionLayerTween item3 in list)
			{
				compositionLayerAnimatableTransform.AddKeyFrameTween(item3);
			}
			layers.Add(new CompositionLayer(index, layerType, result, result2, name, fileExtension, textureReference, num, num2, compositionLayerAnimatableTransform));
		}
		layers = layers.OrderBy((CompositionLayer l) => l.Index).ToList();
		string version = value;
		uint frameRate = uint.Parse(value2);
		uint startFrame = uint.Parse(value3);
		uint endFrame = uint.Parse(value4);
		uint width = uint.Parse(value5);
		uint height = uint.Parse(value6);
		string name2 = value7;
		compositions.Add(new Composition(version, frameRate, startFrame, endFrame, width, height, name2, layers));
		string fullKeyPath = e.Resource.FullKeyPath;
		string text = fullKeyPath.Remove(fullKeyPath.LastIndexOf("/"));
		foreach (CompositionAsset item4 in assets)
		{
			string text2 = item4.FileName.Remove(item4.FileName.LastIndexOf("."));
			text2 = text2.Replace('_', '/');
			textureResourceKeys.Add(text + "/" + item4.Path.ToUpper() + text2.ToUpper());
		}
		e.PushDependencies(textureResourceKeys);
		return new CompositionGroup(e.ResourceTree, textureResourceKeys, assets, compositions)
		{
			Resource = e.Resource
		};
	}

	private IEnumerable<CompositionLayerTween> ParseScalarTransform(XmlNode transformNode, CompositionLayerTween.Type tweenType, uint layerStartFrame, uint layerEndFrame)
	{
		List<CompositionLayerTween> list = new List<CompositionLayerTween>();
		if (TransformPropertyHasKeyFrames(transformNode))
		{
			list.AddRange(ParseScalarTweenKeySet(transformNode.SelectNodes("k").OfType<XmlNode>(), tweenType));
		}
		else
		{
			list.Add(ParseSimpleTweenTransform(transformNode, tweenType, layerStartFrame, layerEndFrame));
		}
		return list;
	}

	private IEnumerable<CompositionLayerTween> ParseVectorTransform(XmlNode transformNode, CompositionLayerTween.Type tweenType, uint layerStartFrame, uint layerEndFrame)
	{
		List<CompositionLayerTween> list = new List<CompositionLayerTween>();
		if (TransformPropertyHasKeyFrames(transformNode))
		{
			list.AddRange(ParseVectorTweenKeySet(transformNode.SelectNodes("k").OfType<XmlNode>(), tweenType));
		}
		else
		{
			list.Add(ParseSimpleTweenTransform(transformNode, tweenType, layerStartFrame, layerEndFrame));
		}
		return list;
	}

	private IEnumerable<CompositionLayerTween> ParseScalarTweenKeySet(IEnumerable<XmlNode> tweenKeySet, CompositionLayerTween.Type tweenType)
	{
		if (tweenType != CompositionLayerTween.Type.OPACITY && tweenType != CompositionLayerTween.Type.ROTATION)
		{
			throw new NotImplementedException();
		}
		Type type = ((tweenType == CompositionLayerTween.Type.OPACITY) ? typeof(CompositionLayerOpacityTween) : typeof(CompositionLayerRotationTween));
		List<CompositionLayerTween> list = new List<CompositionLayerTween>();
		CompositionLayerTween compositionLayerTween = null;
		XmlNode xmlNode = tweenKeySet.First();
		XmlNode node = tweenKeySet.First();
		string value = "";
		string value2 = "";
		string value3 = "";
		node.TryGetNodeInnerText("t", out value);
		node.TryGetNodeInnerText("s", out value2);
		node.TryGetNodeInnerText("e", out value3);
		KeyValuePair<double, double> keyValuePair = ((!string.IsNullOrEmpty(value2) && !string.IsNullOrEmpty(value3)) ? new KeyValuePair<double, double>(double.Parse(value2, NumberStyles.Float, CultureInfo.InvariantCulture), double.Parse(value3, NumberStyles.Float, CultureInfo.InvariantCulture)) : (tweenType switch
		{
			CompositionLayerTween.Type.OPACITY => new KeyValuePair<double, double>(100.0, 100.0),
			CompositionLayerTween.Type.ROTATION => new KeyValuePair<double, double>(0.0, 0.0),
			_ => throw new NotImplementedException(),
		}));
		uint num = (uint)Math.Round(double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture));
		compositionLayerTween = (CompositionLayerTween)Activator.CreateInstance(type, num, uint.MaxValue, keyValuePair);
		foreach (XmlNode item in tweenKeySet.Skip(1))
		{
			xmlNode = item;
			string value4 = "";
			string value5 = "";
			string value6 = "";
			item.TryGetNodeInnerText("t", out value4);
			item.TryGetNodeInnerText("s", out value5);
			item.TryGetNodeInnerText("e", out value6);
			KeyValuePair<double, double> keyValuePair2;
			if (!string.IsNullOrEmpty(value5) && !string.IsNullOrEmpty(value6))
			{
				keyValuePair2 = new KeyValuePair<double, double>(double.Parse(value5, NumberStyles.Float, CultureInfo.InvariantCulture), double.Parse(value6, NumberStyles.Float, CultureInfo.InvariantCulture));
			}
			else
			{
				if (tweenType != CompositionLayerTween.Type.OPACITY && tweenType != CompositionLayerTween.Type.ROTATION)
				{
					throw new NotImplementedException();
				}
				keyValuePair2 = new KeyValuePair<double, double>(compositionLayerTween.EndValues.First().Value, compositionLayerTween.EndValues.First().Value);
			}
			KeyValuePair<double, double> keyValuePair3 = new KeyValuePair<double, double>(compositionLayerTween.StartValues.First().Value, compositionLayerTween.EndValues.First().Value);
			uint num2 = (uint)Math.Round(double.Parse(value4, NumberStyles.Float, CultureInfo.InvariantCulture));
			compositionLayerTween = (CompositionLayerTween)Activator.CreateInstance(type, compositionLayerTween.StartFrame, num2, keyValuePair3);
			list.Add(compositionLayerTween);
			if (item != tweenKeySet.Last())
			{
				compositionLayerTween = (CompositionLayerTween)Activator.CreateInstance(type, num2, uint.MaxValue, keyValuePair2);
			}
		}
		if (xmlNode == tweenKeySet.First())
		{
			throw new NotSupportedException();
		}
		return list;
	}

	private IEnumerable<CompositionLayerTween> ParseVectorTweenKeySet(IEnumerable<XmlNode> tweenKeySet, CompositionLayerTween.Type tweenType)
	{
		if (tweenType != CompositionLayerTween.Type.POSITION && tweenType != CompositionLayerTween.Type.SCALE)
		{
			throw new NotImplementedException();
		}
		Type type = ((tweenType == CompositionLayerTween.Type.POSITION) ? typeof(CompositionLayerPositionTween) : typeof(CompositionLayerScaleTween));
		List<CompositionLayerTween> list = new List<CompositionLayerTween>();
		CompositionLayerTween compositionLayerTween = null;
		XmlNode xmlNode = tweenKeySet.First();
		XmlNode xmlNode2 = tweenKeySet.First();
		string value = "";
		List<string> list2 = new List<string>();
		List<string> list3 = new List<string>();
		xmlNode2.TryGetNodeInnerText("t", out value);
		IEnumerable<XmlNode> source = xmlNode2.SelectNodes("s").OfType<XmlNode>();
		IEnumerable<XmlNode> source2 = xmlNode2.SelectNodes("e").OfType<XmlNode>();
		if (source.Count() == 3 && source2.Count() == 3)
		{
			list2.Add(source.ElementAt(0).InnerText);
			list2.Add(source.ElementAt(1).InnerText);
			list2.Add(source.ElementAt(2).InnerText);
			list3.Add(source2.ElementAt(0).InnerText);
			list3.Add(source2.ElementAt(1).InnerText);
			list3.Add(source2.ElementAt(2).InnerText);
		}
		else if (source.Count() != 0 && source2.Count() != 0)
		{
			throw new NotImplementedException();
		}
		List<KeyValuePair<double, double>> list4 = new List<KeyValuePair<double, double>>();
		if (list2.All((string v) => !string.IsNullOrEmpty(v)) && list3.All((string v) => !string.IsNullOrEmpty(v)) && list2.Count > 0 && list3.Count > 0)
		{
			for (int num = 0; num < 3; num++)
			{
				list4.Add(new KeyValuePair<double, double>(double.Parse(list2[num], NumberStyles.Float, CultureInfo.InvariantCulture), double.Parse(list3[num], NumberStyles.Float, CultureInfo.InvariantCulture)));
			}
		}
		else
		{
			switch (tweenType)
			{
				case CompositionLayerTween.Type.SCALE:
					{
						for (int num3 = 0; num3 < 3; num3++)
						{
							list4.Add(new KeyValuePair<double, double>(100.0, 100.0));
						}
						break;
					}
				case CompositionLayerTween.Type.POSITION:
					{
						for (int num2 = 0; num2 < 3; num2++)
						{
							list4.Add(new KeyValuePair<double, double>(0.0, 0.0));
						}
						break;
					}
				default:
					throw new NotImplementedException();
			}
		}
		uint num4 = (uint)Math.Round(double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture));
		compositionLayerTween = (CompositionLayerTween)Activator.CreateInstance(type, num4, uint.MaxValue, list4[0], list4[1], list4[2]);
		foreach (XmlNode item2 in tweenKeySet.Skip(1))
		{
			xmlNode = item2;
			string value2 = "";
			List<string> list5 = new List<string>();
			List<string> list6 = new List<string>();
			item2.TryGetNodeInnerText("t", out value2);
			IEnumerable<XmlNode> source3 = item2.SelectNodes("s").OfType<XmlNode>();
			IEnumerable<XmlNode> source4 = item2.SelectNodes("e").OfType<XmlNode>();
			if (source3.Count() == 3 && source4.Count() == 3)
			{
				list5.Add(source3.ElementAt(0).InnerText);
				list5.Add(source3.ElementAt(1).InnerText);
				list5.Add(source3.ElementAt(2).InnerText);
				list6.Add(source4.ElementAt(0).InnerText);
				list6.Add(source4.ElementAt(1).InnerText);
				list6.Add(source4.ElementAt(2).InnerText);
			}
			else if (source3.Count() != 0 && source4.Count() != 0)
			{
				throw new NotImplementedException();
			}
			List<KeyValuePair<double, double>> list7 = new List<KeyValuePair<double, double>>();
			if (list5.All((string v) => !string.IsNullOrEmpty(v)) && list6.All((string v) => !string.IsNullOrEmpty(v)) && list5.Count > 0 && list6.Count > 0)
			{
				for (int num5 = 0; num5 < 3; num5++)
				{
					list7.Add(new KeyValuePair<double, double>(double.Parse(list5[num5], NumberStyles.Float, CultureInfo.InvariantCulture), double.Parse(list6[num5], NumberStyles.Float, CultureInfo.InvariantCulture)));
				}
			}
			else
			{
				if (tweenType != CompositionLayerTween.Type.POSITION && tweenType != CompositionLayerTween.Type.SCALE)
				{
					throw new NotImplementedException();
				}
				for (int num6 = 0; num6 < 3; num6++)
				{
					list7.Add(new KeyValuePair<double, double>(compositionLayerTween.EndValues.ElementAt(num6).Value, compositionLayerTween.EndValues.ElementAt(num6).Value));
				}
			}
			List<KeyValuePair<double, double>> list8 = new List<KeyValuePair<double, double>>();
			for (int num7 = 0; num7 < 3; num7++)
			{
				KeyValuePair<double, double> item = new KeyValuePair<double, double>(compositionLayerTween.StartValues.ElementAt(num7).Value, compositionLayerTween.EndValues.ElementAt(num7).Value);
				list8.Add(item);
			}
			uint num8 = (uint)Math.Round(double.Parse(value2, NumberStyles.Float, CultureInfo.InvariantCulture));
			compositionLayerTween = (CompositionLayerTween)Activator.CreateInstance(type, compositionLayerTween.StartFrame, num8, list8[0], list8[1], list8[2]);
			list.Add(compositionLayerTween);
			if (item2 != tweenKeySet.Last())
			{
				compositionLayerTween = (CompositionLayerTween)Activator.CreateInstance(type, num8, uint.MaxValue, list7[0], list7[1], list7[2]);
			}
		}
		if (xmlNode == tweenKeySet.First())
		{
			throw new NotSupportedException();
		}
		return list;
	}

	private CompositionLayerTween ParseSimpleTweenTransform(XmlNode tweenKey, CompositionLayerTween.Type tweenType, uint layerStartFrame, uint layerEndFrame)
	{
		Type type = null;
		switch (tweenType)
		{
			case CompositionLayerTween.Type.OPACITY:
				type = typeof(CompositionLayerOpacityTween);
				break;
			case CompositionLayerTween.Type.ROTATION:
				type = typeof(CompositionLayerRotationTween);
				break;
			case CompositionLayerTween.Type.POSITION:
				type = typeof(CompositionLayerPositionTween);
				break;
			case CompositionLayerTween.Type.SCALE:
				type = typeof(CompositionLayerScaleTween);
				break;
		}
		CompositionLayerTween compositionLayerTween = null;
		if (tweenType == CompositionLayerTween.Type.OPACITY || tweenType == CompositionLayerTween.Type.ROTATION)
		{
			double num = double.Parse(tweenKey.SelectSingleNode("k").InnerText, NumberStyles.Float, CultureInfo.InvariantCulture);
			KeyValuePair<double, double> keyValuePair = new KeyValuePair<double, double>(num, num);
			compositionLayerTween = (CompositionLayerTween)Activator.CreateInstance(type, layerStartFrame, layerEndFrame, keyValuePair);
		}
		else
		{
			string text = "";
			string text2 = "";
			IEnumerable<XmlNode> source = tweenKey.SelectNodes("k").OfType<XmlNode>();
			if (source.Count() != 3)
			{
				throw new NotSupportedException();
			}
			text = source.ElementAt(0).InnerText;
			text2 = source.ElementAt(1).InnerText;
			double num2 = double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
			double num3 = double.Parse(text2, NumberStyles.Float, CultureInfo.InvariantCulture);
			KeyValuePair<double, double> keyValuePair2 = new KeyValuePair<double, double>(num2, num2);
			KeyValuePair<double, double> keyValuePair3 = new KeyValuePair<double, double>(num3, num3);
			compositionLayerTween = (CompositionLayerTween)Activator.CreateInstance(type, layerStartFrame, layerEndFrame, keyValuePair2, keyValuePair3);
		}
		compositionLayerTween.HasKeyFrames = false;
		return compositionLayerTween;
	}

	private bool TransformPropertyHasKeyFrames(XmlNode node)
	{
		return node.SelectSingleNode("k").SelectSingleNode("t") != null;
	}
}
