using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.SPRINGBOARD;

/// <summary>The object type for the spring board, a see-saw board that launches a character up.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Spring board")]
[Description("Spring board from Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.General)]
[ObjectInstance(typeof(SpringBoardInstance))]
public class SpringBoardType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
	[Dependency]
	public const string SoundResourceKey = "SONICORCA/SOUND/SPRING";
}
