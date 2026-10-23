using SonicOrca.Core;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Geometry;

namespace SONICORCA.OBJECTS.EHZPLATFORM;

/// <summary>The object type for <see cref="EHZPlatformInstance"/>, a floating platform.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Floating platform")]
[Description("Floating platform from Emerald Hill Zone, Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Platform)]
[ObjectInstance(typeof(EHZPlatformInstance))]
public class EHZPlatformType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	/// <summary>Called by <see cref="ObjectType.GetLifeRadius(IActiveObject)"/> through dynamic dispatch.</summary>
	public Vector2 GetLifeRadius(Platform state)
	{
		return state.MovementRadius;
	}
}
