using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Geometry;

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
