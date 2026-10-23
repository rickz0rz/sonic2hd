using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.GRABBER;

/// <summary>The object type of the Grabber spider badnik from Chemical Plant Zone.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Grabber")]
[Description("Grabber from Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Badnik)]
[ObjectInstance(typeof(GrabberInstance))]
public class GrabberType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
