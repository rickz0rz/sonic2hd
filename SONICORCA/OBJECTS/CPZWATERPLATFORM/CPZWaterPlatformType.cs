using SonicOrca.Core;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Geometry;

namespace SONICORCA.OBJECTS.CPZWATERPLATFORM;

/// <summary>The object type for <see cref="CPZWaterPlatformInstance"/>, a platform that moves on rails.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Water platform")]
[Description("Water platform from Chemical Plant Zone, Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Platform)]
[ObjectInstance(typeof(CPZWaterPlatformInstance))]
public class CPZWaterPlatformType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	/// <summary>Called by <see cref="ObjectType.GetLifeRadius(IActiveObject)"/> through dynamic dispatch.</summary>
	public Vector2 GetLifeRadius(Platform state)
	{
		return state.MovementRadius;
	}
}
