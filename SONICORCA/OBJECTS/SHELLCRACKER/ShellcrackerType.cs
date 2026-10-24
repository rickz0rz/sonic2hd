using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.SHELLCRACKER;

/// <summary>
/// The object type for Shellcracker, the crab badnik of Metropolis Zone (Obj9F and its claw ObjA0 in s2disasm).
/// "s2hd-data import-level mtz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Shellcracker")]
[Description("Shellcracker from Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Badnik)]
[ObjectInstance(typeof(ShellcrackerInstance))]
public class ShellcrackerType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
