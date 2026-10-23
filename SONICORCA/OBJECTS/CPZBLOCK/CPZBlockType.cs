using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.CPZBLOCK;

/// <summary>The object type for <see cref="CPZBlockInstance"/>, a moving block or a set of stair blocks.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Block")]
[Description("Block from Chemical Plant Zone, Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Platform)]
[ObjectInstance(typeof(CPZBlockInstance))]
public class CPZBlockType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
