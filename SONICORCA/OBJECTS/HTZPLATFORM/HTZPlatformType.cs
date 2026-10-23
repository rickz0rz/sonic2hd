using SonicOrca.Core;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Geometry;

namespace SONICORCA.OBJECTS.HTZPLATFORM;

/// <summary>The object type of the floating platform from Hill Top Zone.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Floating platform")]
[Description("Floating platform from Hill Top Zone, Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Platform)]
[ObjectInstance(typeof(HTZPlatformInstance))]
public class HTZPlatformType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	public Vector2 GetLifeRadius(Platform state)
	{
		return state.MovementRadius;
	}
}
