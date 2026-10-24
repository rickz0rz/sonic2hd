using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.MCZBOSS;

/// <summary>
/// The object type for the Mystic Cave Zone boss, the drill ship of Eggman (Obj57 in s2disasm). The level area
/// creates it. "s2hd-data import-level mcz" adds the art of the original. See <see cref="MCZBossInstance"/> for the
/// behaviour.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("MCZ boss")]
[Description("Drill ship boss from Mystic Cave Zone, Sonic 2")]
[ObjectInstance(typeof(MCZBossInstance))]
public class MCZBossType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	/// <summary>The stones and the spikes that fall from the ceiling.</summary>
	[Dependency]
	public const string RockResourceKey = "/ROCK";

	[Dependency]
	public const string BossExplosionResourceKey = "SONICORCA/OBJECTS/EXPLOSION/BOSS";

	[Dependency]
	public const string BossHitSoundResourceKey = "SONICORCA/SOUND/BOSSHIT";
}
