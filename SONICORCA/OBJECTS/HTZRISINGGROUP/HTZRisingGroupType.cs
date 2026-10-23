using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Geometry;

namespace SONICORCA.OBJECTS.HTZRISINGGROUP;

/// <summary>The object type of the controller that moves a group of rising blocks during the Hill Top Zone earthquake.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Rising group")]
[Description("Rising group from Hill Top Zone, Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Obstacle)]
[ObjectInstance(typeof(HTZRisingGroupInstance))]
public class HTZRisingGroupType : ObjectType
{
	public Vector2 GetLifeRadius(HTZRisingGroupInstance state)
	{
		return new Vector2(state.Size.X / 2.0, state.Size.Y / 2.0);
	}
}
