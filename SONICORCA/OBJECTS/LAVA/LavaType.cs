using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Geometry;

namespace SONICORCA.OBJECTS.LAVA;

/// <summary>The object type of the lava area that hurts characters.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Lava")]
[Description("Lava from Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Obstacle)]
[ObjectInstance(typeof(LavaInstance))]
public class LavaType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	public Vector2 GetLifeRadius(LavaInstance state)
	{
		return (Vector2)(new Vector2i(state.Width, state.Height) / 2 * 64);
	}
}
