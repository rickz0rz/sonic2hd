using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.OOZBOSS;

/// <summary>
/// The object type for the Oil Ocean Zone boss (Obj55 in s2disasm). The level area adds the boss when the camera locks
/// in the arena of act 2. The sprites are the art of the original (ArtNem_OOZBoss with the mappings of Obj55). Eggman
/// is part of this art. See <see cref="OOZBossInstance"/> for the behaviour.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Oil Ocean Zone boss")]
[Description("Submarine Eggman from Oil Ocean Zone, Sonic 2")]
[ObjectInstance(typeof(OOZBossInstance))]
public class OOZBossType : ObjectType
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

	/// <summary>The energy wave on the floor (SndID_LaserFloor) uses the fire sound.</summary>
	[Dependency]
	public const string WaveSoundResourceKey = "SONICORCA/SOUND/FIREBURN";
}
