using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.BUBBLEGENERATOR;

/// <summary>The object type for <see cref="BubbleGeneratorInstance"/>, which makes air bubbles under water.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Bubble Generator")]
[Description("Bubble Generator from Sonic 1")]
[ObjectInstance(typeof(BubbleGeneratorInstance))]
public class BubbleGeneratorType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
