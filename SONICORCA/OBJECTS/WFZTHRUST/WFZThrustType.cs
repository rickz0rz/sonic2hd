using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.WFZTHRUST;

/// <summary>
/// The object type for the fire from the engines of Eggman's getaway ship at the end of Wing Fortress Zone (ObjBC in
/// s2disasm). "s2hd-data import-level wfz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Ship thrust")]
[Description("Engine fire of Eggman's ship from Wing Fortress Zone")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Scenery)]
[ObjectInstance(typeof(WFZThrustInstance))]
public class WFZThrustType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
