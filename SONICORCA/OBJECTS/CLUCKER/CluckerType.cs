using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.CLUCKER;

/// <summary>
/// The object type for Clucker, the chicken turret badnik of Wing Fortress Zone (ObjAE in s2disasm). Its base (ObjAD)
/// is the sub type BASE, and its shots are the sub type SHOT. "s2hd-data import-level wfz" adds the art of the
/// original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Clucker")]
[Description("Clucker from Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Badnik)]
[ObjectInstance(typeof(CluckerInstance))]
public class CluckerType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	/// <summary>The shot that the Clucker fires.</summary>
	[Dependency]
	public const string ShotResourceKey = "/SHOT";
}
