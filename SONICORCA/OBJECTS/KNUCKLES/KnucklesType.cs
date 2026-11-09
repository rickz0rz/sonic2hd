using SonicOrca.Core;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.KNUCKLES;

/// <summary>
/// The object type for Knuckles, a playable character. The HD data has no Knuckles, so only the import of
/// tools/s2hd-data makes this object (import-knuckles), with the sprites of "Knuckles in Sonic 2".
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Knuckles")]
[Description("Knuckles from Knuckles in Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Character)]
[ObjectInstance(typeof(KnucklesInstance))]
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
public class KnucklesType : ObjectType
{
	/// <summary>The original sprites of Knuckles (KnucklesArt).</summary>
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
