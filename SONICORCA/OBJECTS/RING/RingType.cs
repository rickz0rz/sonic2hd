using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.RING;

/// <summary>
/// The object type for a ring. It owns one animation that all placed rings share, so that they spin together.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Ring")]
[Description("Ring from Sonic 1")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Ring)]
[ObjectInstance(typeof(RingInstance))]
public class RingType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
	[Dependency]
	public const string SparkleObjectResourceKey = "/SPARKLE";
	[Dependency]
	public const string CollectSoundResourceKey = "SONICORCA/SOUND/RING";
	[Dependency]
	public const string GlowParticleResourceKey = "SONICORCA/PARTICLE/GLOW";
	[Dependency]
	public const string PerfectSoundResourceKey = "SONICORCA/SOUND/PERFECT";

	public AnimationInstance AnimationInstance { get; private set; }

	public int LastAnimationTick { get; set; }

	protected override void OnStart()
	{
		AnimationInstance = new AnimationInstance(Level.GameContext.ResourceTree, this.GetAbsolutePath(AnimationGroupResourceKey));
	}

	protected override void OnAnimate() => AnimationInstance.Animate();
}
