using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.DEZDOOR;

/// <summary>
/// The object type for the one-way barrier of Death Egg Zone (Obj2D in s2disasm). "s2hd-data import-level dez" adds
/// the art of the original: the construction stripes. See <see cref="DEZDoorInstance"/> for the behaviour.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("DEZ one-way door")]
[Description("One-way barrier from Death Egg Zone, Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Obstacle)]
[ObjectInstance(typeof(DEZDoorInstance))]
public class DEZDoorType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
