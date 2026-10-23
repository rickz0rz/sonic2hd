using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.CPZSPEEDBOOSTER;

/// <summary>The object type for <see cref="CPZSpeedBoosterInstance"/>, which pushes characters at high speed.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Speed Booster")]
[Description("Speed Booster from Chemical Plant Zone, Sonic 2")]
[ObjectInstance(typeof(CPZSpeedBoosterInstance))]
public class CPZSpeedBoosterType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
	[Dependency]
	public const string SoundResourceKey = "SONICORCA/SOUND/SPRING";
}
