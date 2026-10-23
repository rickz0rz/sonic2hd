using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.EHZBRIDGE.STAKE;

/// <summary>The object type for <see cref="EHZBridgeStakeInstance"/>, the post at each end of a bridge.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Bridge stake")]
[Description("Bridge stake from Emerald Hill Zone, Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Scenery)]
[ObjectInstance(typeof(EHZBridgeStakeInstance))]
public class EHZBridgeStakeType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "//ANIGROUP";
}
