using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.CAPSULE;

/// <summary>The object type for <see cref="CapsuleInstance"/>, the animal capsule at the end of a boss act.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Capsule")]
[Description("Capsule from Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Capsule)]
[ObjectInstance(typeof(CapsuleInstance))]
public class CapsuleType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
