using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Geometry;

namespace SONICORCA.OBJECTS.BUZZER;

/// <summary>The object type for <see cref="BuzzerInstance"/>, the flying wasp badnik.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Buzzer")]
[Description("Buzzer from Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Badnik)]
[ObjectInstance(typeof(BuzzerInstance))]
public class BuzzerType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	/// <summary>Called by <see cref="ObjectType.GetLifeRadius(IActiveObject)"/> through dynamic dispatch.</summary>
	public new Vector2 GetLifeRadius(ActiveObject state)
	{
		return (Vector2)new Vector2i(1024, 0);
	}
}
