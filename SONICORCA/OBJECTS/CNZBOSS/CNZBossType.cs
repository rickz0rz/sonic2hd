using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.CNZBOSS;

/// <summary>
/// The object type for the Casino Night Zone boss (Obj51 in s2disasm). "s2hd-data import-level cnz" adds the art of
/// the original: the CNZ boss parts and the egg pod, with the boss palette. See <see cref="CNZBossInstance"/> for the
/// behaviour.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("CNZ boss")]
[Description("The electric Eggman from Casino Night Zone, Sonic 2")]
[ObjectInstance(typeof(CNZBossInstance))]
public class CNZBossType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	[Dependency]
	public const string BossExplosionResourceKey = "SONICORCA/OBJECTS/EXPLOSION/BOSS";

	[Dependency]
	public const string BossHitSoundResourceKey = "SONICORCA/SOUND/BOSSHIT";

	/// <summary>The sound when an electric ball hits the floor and breaks in two.</summary>
	[Dependency]
	public const string BallBreakSoundResourceKey = "SONICORCA/SOUND/BOSSEXPLOSION";
}
