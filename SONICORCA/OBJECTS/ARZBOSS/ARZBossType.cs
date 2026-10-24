using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.ARZBOSS;

/// <summary>
/// The object type for the Aquatic Ruin Zone boss (Obj89 in s2disasm): Eggman with a hammer, and two pillars that
/// shoot arrows. "s2hd-data import-level arz" adds the art of the original. See <see cref="ARZBossInstance"/> for
/// the behaviour.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("ARZ boss")]
[Description("Eggman with the hammer from Aquatic Ruin Zone, Sonic 2")]
[ObjectInstance(typeof(ARZBossInstance))]
public class ARZBossType : ObjectType
{
	/// <summary>The Egg Mobile, Eggman, the hammer and the jet flames (Obj89_MapUnc_30E04).</summary>
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	/// <summary>The pillars, their eyes and the arrows (Obj89_MapUnc_30D68).</summary>
	[Dependency]
	public const string PillarAnimationGroupResourceKey = "/PILLAR/ANIGROUP";

	[Dependency]
	public const string BossExplosionResourceKey = "SONICORCA/OBJECTS/EXPLOSION/BOSS";

	[Dependency]
	public const string BossHitSoundResourceKey = "SONICORCA/SOUND/BOSSHIT";
}
