using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.SOL;

/// <summary>The object type for Sol, a badnik with four fireballs that turn around it.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Sol")]
[Description("Sol from Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Badnik)]
[ObjectInstance(typeof(SolInstance))]
public class SolType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
