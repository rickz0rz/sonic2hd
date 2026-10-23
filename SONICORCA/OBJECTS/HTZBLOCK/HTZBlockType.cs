using SonicOrca.Core;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Geometry;

namespace SONICORCA.OBJECTS.HTZBLOCK;

/// <summary>The object type of the floating block from Hill Top Zone.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Floating block")]
[Description("Floating block from Hill Top Zone, Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Platform)]
[ObjectInstance(typeof(HTZBlockInstance))]
public class HTZBlockType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	public Vector2 GetLifeRadius(Platform state)
	{
		return state.MovementRadius;
	}
}
