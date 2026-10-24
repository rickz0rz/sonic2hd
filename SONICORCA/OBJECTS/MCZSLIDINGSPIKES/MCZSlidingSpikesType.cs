using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.MCZSLIDINGSPIKES;

/// <summary>
/// The object type for the spike block of Mystic Cave Zone that slides out of a wall (Obj76 in s2disasm).
/// "s2hd-data import-level mcz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Sliding spikes")]
[Description("Spike block that slides out of the wall from Mystic Cave Zone")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Platform)]
[ObjectInstance(typeof(MCZSlidingSpikesInstance))]
public class MCZSlidingSpikesType : ObjectType
{
	/// <summary>The block (level art).</summary>
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	/// <summary>The horizontal spikes (frame 4 of Obj36). The block shows the left half.</summary>
	[Dependency]
	public const string SpikeAnimationGroupResourceKey = "/SPIKE/ANIGROUP";
}
