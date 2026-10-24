using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.MTZBOSS;

/// <summary>
/// The object type for the Metropolis Zone boss, the ship of Eggman with the orbiting balls (Obj54 and Obj53 in
/// s2disasm). The level area adds the boss 90 frames after the camera locks in the arena of act 3. "s2hd-data
/// import-level mtz" adds the art of the original. See <see cref="MTZBossInstance"/> for the behaviour.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("MTZ boss")]
[Description("Balloon ship boss from Metropolis Zone, Sonic 2")]
[ObjectInstance(typeof(MTZBossInstance))]
public class MTZBossType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	[Dependency]
	public const string BossExplosionResourceKey = "SONICORCA/OBJECTS/EXPLOSION/BOSS";

	[Dependency]
	public const string BossHitSoundResourceKey = "SONICORCA/SOUND/BOSSHIT";

	/// <summary>The game data has no laser sounds. The laser shot (SndID_LaserBurst) uses the fireball sound.</summary>
	[Dependency]
	public const string LaserSoundResourceKey = "SONICORCA/SOUND/FIREBALL";

	/// <summary>A ball that bursts (SndID_BossExplosion).</summary>
	[Dependency]
	public const string BurstSoundResourceKey = "SONICORCA/SOUND/BOSSEXPLOSION";
}
