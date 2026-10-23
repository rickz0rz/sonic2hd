using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.SPIKES;

/// <summary>The object type for spikes that hurt a character. Some spikes move in and out.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Spikes")]
[Description("Spikes from Sonic 1")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Obstacle)]
[ObjectInstance(typeof(SpikesInstance))]
public class SpikesType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
	[Dependency]
	public const string SpikeMovementSoundResourceKey = "SONICORCA/SOUND/SPIKESLASH";
}
