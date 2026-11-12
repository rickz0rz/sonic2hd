using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Geometry;

namespace SONICORCA.OBJECTS.WFZFIRESWITCH;

/// <summary>
/// The object type of the invisible line in Wing Fortress that changes the colours of the fire and the conveyor belts
/// (Obj8B in s2disasm). See <see cref="WfzFireSwitchInstance"/> for the behaviour.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("WFZ fire switch")]
[Description("Changes the colours of the fire and the conveyor belts of Wing Fortress, Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.General)]
[ObjectInstance(typeof(WfzFireSwitchInstance))]
public class WfzFireSwitchType : ObjectType
{
	public Vector2 GetLifeRadius(WfzFireSwitchInstance state)
	{
		return new Vector2(0, state.Height / 2);
	}
}
