using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.RICKY;

/// <summary>The object type for Ricky, an animal that a destroyed badnik sets free.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Ricky")]
[Description("Ricky from Sonic 1")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Animal)]
[ObjectInstance(typeof(RickyInstance))]
public class RickyType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
