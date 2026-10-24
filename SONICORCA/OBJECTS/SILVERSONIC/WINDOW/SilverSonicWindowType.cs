using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.SILVERSONIC.WINDOW;

/// <summary>
/// The object type for the window in the wall of the Silver Sonic arena, through which Eggman watches the fight (ObjAF
/// in s2disasm, subtype $A4). Silver Sonic adds it. See <see cref="SilverSonicWindowInstance"/> for the behaviour.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Silver Sonic window")]
[Description("Window through which Eggman watches Silver Sonic, Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Scenery)]
[ObjectInstance(typeof(SilverSonicWindowInstance))]
public class SilverSonicWindowType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
