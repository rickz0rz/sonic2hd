using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.SCZCLOUD;

/// <summary>
/// The object type for the clouds of Sky Chase Zone (ObjB3 in s2disasm). "s2hd-data import-level scz" adds the art of
/// the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Cloud")]
[Description("Cloud from Sky Chase Zone")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.General)]
[ObjectInstance(typeof(SCZCloudInstance))]
public class SCZCloudType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
