using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.WOCKY;

/// <summary>The object type for Wocky, an animal that a destroyed badnik sets free.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Wocky")]
[Description("Wocky from Sonic 1")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Animal)]
[ObjectInstance(typeof(WockyInstance))]
public class WockyType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
