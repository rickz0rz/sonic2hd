using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.POINTS;

/// <summary>The object type for the score number that rises after the player gets points.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Points")]
[Description("Points from Sonic 1")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Particle)]
[ObjectInstance(typeof(PointsInstance))]
public class PointsType : ObjectType
{
	[Dependency]
	public const string FontResourceKey = "SONICORCA/FONTS/POINTS";
}
