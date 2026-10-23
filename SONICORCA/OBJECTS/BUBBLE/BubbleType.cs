using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.BUBBLE;

/// <summary>The object type for <see cref="BubbleInstance"/>, an air bubble under water.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Bubble")]
[Description("Bubble from Sonic 1")]
[ObjectInstance(typeof(BubbleInstance))]
public class BubbleType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
