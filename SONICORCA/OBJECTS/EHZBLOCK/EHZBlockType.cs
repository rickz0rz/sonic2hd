using SonicOrca.Core;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Geometry;

namespace SONICORCA.OBJECTS.EHZBLOCK;

/// <summary>The object type for <see cref="EHZBlockInstance"/>, a large floating block.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Floating block")]
[Description("Floating block from Emerald Hill Zone, Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Platform)]
[ObjectInstance(typeof(EHZBlockInstance))]
public class EHZBlockType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	/// <summary>Called by <see cref="ObjectType.GetLifeRadius(IActiveObject)"/> through dynamic dispatch.</summary>
	public Vector2 GetLifeRadius(Platform state)
	{
		return state.MovementRadius;
	}
}
