using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.FORCESPINBALL;

/// <summary>The object type of the invisible marker that makes characters stay in a spinball.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Force spinball")]
[Description("Forces characters to stay in a spinball.")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.General)]
[ObjectInstance(typeof(ForceSpinballInstance))]
public class ForceSpinballType : ObjectType
{
}
