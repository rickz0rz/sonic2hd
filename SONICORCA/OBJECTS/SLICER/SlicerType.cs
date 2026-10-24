using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.SLICER;

/// <summary>
/// The object type for Slicer, the mantis badnik of Metropolis Zone (ObjA1 and its pincers ObjA2 in s2disasm).
/// "s2hd-data import-level mtz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Slicer")]
[Description("Slicer from Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Badnik)]
[ObjectInstance(typeof(SlicerInstance))]
public class SlicerType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
