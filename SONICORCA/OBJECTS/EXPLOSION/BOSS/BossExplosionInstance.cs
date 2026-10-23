// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.EXPLOSION.BOSS.BossExplosionInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using SonicOrca.Core;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Extensions;

#nullable disable
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
