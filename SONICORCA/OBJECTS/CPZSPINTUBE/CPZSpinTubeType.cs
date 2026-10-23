using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.CPZSPINTUBE;

/// <summary>The object type for <see cref="CPZSpinTubeInstance"/>, the path that moves a character through a spin tube.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Spin tube path")]
[Description("Spin tube path from Chemical Plant Zone, Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Obstacle)]
[ObjectInstance(typeof(CPZSpinTubeInstance))]
public class CPZSpinTubeType : ObjectType
{
	[Dependency]
	public const string SpinballSoundResourceKey = "SONICORCA/SOUND/SPINBALL";
	[Dependency]
	public const string SpindashReleaseSoundResourceKey = "SONICORCA/SOUND/SPINDASH/RELEASE";
}
