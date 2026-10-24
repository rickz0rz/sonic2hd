using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.SILVERSONIC;

/// <summary>
/// The object type for Silver Sonic, the first boss of Death Egg Zone (ObjAF in s2disasm, Mecha Sonic). The level area
/// creates it. "s2hd-data import-level dez" adds the art of the original. See <see cref="SilverSonicInstance"/> for
/// the behaviour.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Silver Sonic")]
[Description("Silver Sonic boss from Death Egg Zone, Sonic 2")]
[ObjectInstance(typeof(SilverSonicInstance))]
public class SilverSonicType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	/// <summary>The window in the wall through which Eggman watches the fight.</summary>
	[Dependency]
	public const string WindowResourceKey = "/WINDOW";

	[Dependency]
	public const string BossExplosionResourceKey = "SONICORCA/OBJECTS/EXPLOSION/BOSS";

	[Dependency]
	public const string BossHitSoundResourceKey = "SONICORCA/SOUND/BOSSHIT";

	[Dependency]
	public const string SpindashSoundResourceKey = "SONICORCA/SOUND/SPINDASH/RELEASE";

	[Dependency]
	public const string BossMusicResourceKey = "SONICORCA/MUSIC/BOSS/S2";
}
