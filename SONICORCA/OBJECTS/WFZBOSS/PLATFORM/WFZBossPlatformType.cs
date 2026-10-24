using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.WFZBOSS.PLATFORM;

/// <summary>
/// The object type for a platform with spikes under it that the Wing Fortress Zone boss releases (ObjC5, routine 8).
/// The art is a frame of the boss. See <see cref="WFZBossPlatformInstance"/> for the behaviour.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("WFZ boss platform")]
[Description("Spiked platform of the Wing Fortress Zone boss, Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Platform)]
[ObjectInstance(typeof(WFZBossPlatformInstance))]
public class WFZBossPlatformType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "SONICORCA/OBJECTS/WFZBOSS/ANIGROUP";

	[Dependency]
	public const string ExplosionResourceKey = "SONICORCA/OBJECTS/EXPLOSION/BOSS";
}
