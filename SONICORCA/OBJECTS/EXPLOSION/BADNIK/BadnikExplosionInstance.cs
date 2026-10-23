using SonicOrca.Core;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Extensions;

namespace SONICORCA.OBJECTS.EXPLOSION.BADNIK;

/// <summary>The explosion that a destroyed badnik makes. It plays its animation and sound one time.</summary>
public class BadnikExplosionInstance : ParticleObject
{
	public BadnikExplosionInstance()
	  : base("/ANIGROUP")
	{
	}

	protected override void OnStart()
	{
		base.OnStart();
		Level.SoundManager.PlaySound(this, Type.GetAbsolutePath("SONICORCA/SOUND/BADNIKEXPLOSION"));
		FilterMultiplier = 0.5;
	}
}
