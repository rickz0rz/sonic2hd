using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.SPRING;

/// <summary>The object type for a spring that bounces a character in one of eight directions.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Spring")]
[Description("Spring from Sonic 1")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.General)]
[ObjectInstance(typeof(SpringInstance))]
public class SpringType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
	[Dependency]
	public const string SoundResourceKey = "SONICORCA/SOUND/SPRING";
}
