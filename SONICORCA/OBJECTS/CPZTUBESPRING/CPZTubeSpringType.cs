using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.CPZTUBESPRING;

/// <summary>The object type for <see cref="CPZTubeSpringInstance"/>, a spring at the bottom of a tube.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Tube spring")]
[Description("Tube spring from Chemical Plant Zone, Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.General)]
[ObjectInstance(typeof(CPZTubeSpringInstance))]
public class CPZTubeSpringType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
	[Dependency]
	public const string SoundResourceKey = "SONICORCA/SOUND/SPRING";
}
