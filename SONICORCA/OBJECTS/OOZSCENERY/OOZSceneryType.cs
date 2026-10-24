using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.OOZSCENERY;

/// <summary>
/// The object type for the falling oil of Oil Ocean Zone (Obj1C in s2disasm). "s2hd-data import-level ooz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Falling oil")]
[Description("Falling oil from Oil Ocean Zone")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Scenery)]
[ObjectInstance(typeof(OOZSceneryInstance))]
public class OOZSceneryType : ObjectType
{
	/// <summary>The narrow streams (Obj1C_MapUnc_11406).</summary>
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	/// <summary>The wide streams (Obj1C_MapUnc_114AE).</summary>
	[Dependency]
	public const string WideAnimationGroupResourceKey = "/WIDE/ANIGROUP";
}
