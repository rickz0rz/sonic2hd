using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.MTZSPIKEBLOCK;

/// <summary>
/// The object type for the spike blocks of Metropolis Zone (Obj68 in s2disasm). "s2hd-data import-level mtz" adds
/// the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Spike block")]
[Description("Block from Metropolis Zone with a spike that comes out of each side in turn")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Obstacle)]
[ObjectInstance(typeof(MTZSpikeBlockInstance))]
public class MTZSpikeBlockType : ObjectType
{
	/// <summary>The block (ArtNem_MtzSpikeBlock).</summary>
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	/// <summary>The spike (ArtNem_MtzSpike).</summary>
	[Dependency]
	public const string SpikeAnimationGroupResourceKey = "/SPIKE/ANIGROUP";

	/// <summary>The sound when the spike starts to move (SndID_SpikesMove).</summary>
	[Dependency]
	public const string SpikeSoundResourceKey = "SONICORCA/SOUND/SPIKESLASH";
}
