using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.STARPOST;

/// <summary>The object type for the starpost, a checkpoint post that sets the restart position.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Starpost")]
[Description("Starpost from Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.General)]
[ObjectInstance(typeof(StarpostInstance))]
public class StarpostType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
	[Dependency]
	public const string SoundResourceKey = "SONICORCA/SOUND/STARPOST";
	[Dependency]
	public const string BonusStarResourceKey = "SONICORCA/OBJECTS/STARPOSTBONUSSTAR";
}
