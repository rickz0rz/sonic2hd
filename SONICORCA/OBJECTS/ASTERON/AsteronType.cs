using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.ASTERON;

/// <summary>
/// The object type for Asteron, the starfish badnik of Metropolis Zone that explodes into spikes (ObjA4 in
/// s2disasm). "s2hd-data import-level mtz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Asteron")]
[Description("Asteron from Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Badnik)]
[ObjectInstance(typeof(AsteronInstance))]
public class AsteronType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
