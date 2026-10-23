using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Geometry;

namespace SONICORCA.OBJECTS.SPIKER;

/// <summary>The object type for Spiker, a drill badnik that fires its spike up when a character comes near.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Spiker")]
[Description("Spiker from Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Badnik)]
[ObjectInstance(typeof(SpikerInstance))]
public class SpikerType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	// ObjectType.GetLifeRadius finds this method by dynamic dispatch. A Spiker stays active 1024 units to each side,
	// so that a spike that it fired can finish its flight.
	public new Vector2 GetLifeRadius(ActiveObject state)
	{
		return (Vector2)new Vector2i(1024, 0);
	}
}
