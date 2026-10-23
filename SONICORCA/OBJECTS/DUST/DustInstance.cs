using SonicOrca.Core.Objects.Base;
using SonicOrca.Geometry;

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
