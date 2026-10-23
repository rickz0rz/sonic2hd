// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.DUST.DustInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using SonicOrca.Core.Objects.Base;
using SonicOrca.Geometry;

#nullable disable
namespace SONICORCA.OBJECTS.DUST;

/// <summary>A small cloud of dust. It moves at a fixed velocity while its animation plays one time.</summary>
public class DustInstance : ParticleObject
{
	public Vector2 Velocity { get; set; }

	public DustInstance()
		: base("/ANIGROUP")
	{
	}

	public void SetDustAnimationIndex(int index) => _animationInstance.Index = index;

	protected override void OnUpdate()
	{
		MovePrecise(Velocity);
		base.OnUpdate();
	}
}
