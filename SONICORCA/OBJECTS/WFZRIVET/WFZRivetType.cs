using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.WFZRIVET;

/// <summary>
/// The object type for the rivet that opens the way into the ship at the end of Wing Fortress Zone (ObjC2 in
/// s2disasm). "s2hd-data import-level wfz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Rivet")]
[Description("Rivet from Wing Fortress Zone that a spinning character breaks to open the ship")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.General)]
[ObjectInstance(typeof(WFZRivetInstance))]
public class WFZRivetType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	/// <summary>The rivet turns into the explosion of a badnik (Obj27).</summary>
	[Dependency]
	public const string ExplosionResourceKey = "SONICORCA/OBJECTS/EXPLOSION/BADNIK";
}
