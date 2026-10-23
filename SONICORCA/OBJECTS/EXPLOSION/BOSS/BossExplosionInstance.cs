using SonicOrca.Core;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Extensions;

namespace SONICORCA.OBJECTS.EXPLOSION.BOSS;

/// <summary>The explosion that a boss makes. It plays its animation and sound one time.</summary>
public class BossExplosionInstance : ParticleObject
{
	public BossExplosionInstance()
	  : base("/ANIGROUP")
	{
	}

	protected override void OnStart()
	{
		base.OnStart();
		Level.SoundManager.PlaySound(this, Type.GetAbsolutePath("SONICORCA/SOUND/BOSSEXPLOSION"));
		FilterMultiplier = 0.0;
	}
}
