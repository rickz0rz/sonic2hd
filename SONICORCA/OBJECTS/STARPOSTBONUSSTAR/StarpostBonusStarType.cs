using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.STARPOSTBONUSSTAR;

/// <summary>The object type for the star circle above a starpost, which opens the special stage.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Starpost bonus stars")]
[Description("Star circle from Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.General)]
[ObjectInstance(typeof(StarpostBonusStarInstance))]
public class StarpostBonusStarType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
	[Dependency]
	public const string EnterSoundResourceKey = "SONICORCA/SOUND/SPECIALSTAGETRIGGER";
}
