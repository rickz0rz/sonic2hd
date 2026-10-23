using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.CPZTUBECAP;

/// <summary>The object type for <see cref="CPZTubeCapInstance"/>, a cap on a tube that a rolling character breaks.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Tube cap")]
[Description("Tube cap from Chemical Plant Zone, Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Obstacle)]
[ObjectInstance(typeof(CPZTubeCapInstance))]
public class CPZTubeCapType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
	[Dependency]
	public const string SoundResourceKey = "SONICORCA/SOUND/BREAKABLE";
}
