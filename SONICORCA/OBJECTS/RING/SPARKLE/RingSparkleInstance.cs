using SonicOrca.Core.Objects.Base;

namespace SONICORCA.OBJECTS.RING.SPARKLE;

/// <summary>A short sparkle particle that shows where a ring was collected.</summary>
public class RingSparkleInstance : ParticleObject
{
	public RingSparkleInstance()
		: base(RingSparkleType.AnimationGroupResourceKey)
	{
		AdditiveBlending = true;
		FilterMultiplier = 0.0;
	}
}
