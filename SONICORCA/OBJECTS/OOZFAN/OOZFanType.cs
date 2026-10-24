using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.OOZFAN;

/// <summary>
/// The object type for the fans of Oil Ocean Zone (Obj3F in s2disasm). "s2hd-data import-level ooz" adds the art of
/// the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Fan")]
[Description("Fan from Oil Ocean Zone")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.General)]
[ObjectInstance(typeof(OOZFanInstance))]
public class OOZFanType : ObjectType
{
	/// <summary>The fan that blows to the side.</summary>
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	/// <summary>The fan that blows up.</summary>
	[Dependency]
	public const string VerticalAnimationGroupResourceKey = "/VERTICAL/ANIGROUP";
}
