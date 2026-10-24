using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.WFZGRAB;

/// <summary>
/// The object type for the invisible grab bar of Wing Fortress Zone (ObjD9 in s2disasm). It has no art.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Grab bar")]
[Description("Invisible bar from Wing Fortress Zone that a character hangs from")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.General)]
[ObjectInstance(typeof(WFZGrabInstance))]
public class WFZGrabType : ObjectType
{
}
