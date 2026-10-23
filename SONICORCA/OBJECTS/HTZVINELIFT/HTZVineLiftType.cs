using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.HTZVINELIFT;

/// <summary>The object type of the vine lift from Hill Top Zone.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Vine lift")]
[Description("Vine lift from Hill Top Zone, Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Scenery)]
[ObjectInstance(typeof(HTZVineLiftInstance))]
public class HTZVineLiftType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
	[Dependency]
	public const string SoundResourceKey = "SONICORCA/SOUND/VINELIFT";
}
