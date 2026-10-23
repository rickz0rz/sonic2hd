using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.TAILS;

/// <summary>The object type for Tails, a playable character that can fly.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Tails")]
[Description("Tails from Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Character)]
[ObjectInstance(typeof(TailsInstance))]
[Dependency("SONICORCA/OBJECTS/DUST")]
[Dependency("SONICORCA/OBJECTS/BARRIER/ANIGROUP")]
[Dependency("SONICORCA/OBJECTS/INVINCIBILITY/ANIGROUP")]
[Dependency("SONICORCA/OBJECTS/SPINDASH/ANIGROUP")]
[Dependency("SONICORCA/SOUND/BARRIER")]
[Dependency("SONICORCA/SOUND/BRAKE")]
[Dependency("SONICORCA/SOUND/SPINBALL")]
[Dependency("SONICORCA/SOUND/JUMP")]
[Dependency("SONICORCA/SOUND/SPINDASH/CHARGE")]
[Dependency("SONICORCA/SOUND/SPINDASH/RELEASE")]
[Dependency("SONICORCA/SOUND/HURT")]
[Dependency("SONICORCA/SOUND/HURT/SPIKES")]
[Dependency("SONICORCA/SOUND/RINGSCATTER")]
[Dependency("SONICORCA/SOUND/INHALE")]
[Dependency("SONICORCA/SOUND/SPLASH")]
[Dependency("SONICORCA/SOUND/DROWNWARNING")]
[Dependency("SONICORCA/SOUND/DROWN")]
public class TailsType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
	[Dependency]
	public const string TailAnimationGroupResourceKey = "/TAIL/ANIGROUP";
	[Dependency]
	public const string HeadAnimationGroupResourceKey = "/HEAD/ANIGROUP";
}
