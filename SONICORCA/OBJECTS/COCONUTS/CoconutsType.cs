using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.COCONUTS;

/// <summary>The object type for <see cref="CoconutsInstance"/>, the monkey badnik that climbs trees.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Coconuts")]
[Description("Coconuts from Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Badnik)]
[ObjectInstance(typeof(CoconutsInstance))]
public class CoconutsType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
