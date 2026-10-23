using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.FLICKY;

/// <summary>The object type of the Flicky bird that comes out of a destroyed badnik.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Flicky")]
[Description("Flicky from Sonic 1")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Animal)]
[ObjectInstance(typeof(FlickyInstance))]
public class FlickyType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
