// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.LAYERSWITCH.LayerSwitchType
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Geometry;

#nullable disable
namespace SONICORCA.OBJECTS.LAYERSWITCH;

/// <summary>The object type of the invisible line that changes the path and layer of the characters that cross it.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Layer switch")]
[Description("Allow characters to switch layers.")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.General)]
[ObjectInstance(typeof(LayerSwitchInstance))]
public class LayerSwitchType : ObjectType
{
	public Vector2 GetLifeRadius(LayerSwitchInstance state)
	{
		return new Vector2(state.Width / 2, state.Height / 2);
	}
}
