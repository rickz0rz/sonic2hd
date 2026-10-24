using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.CRAWL;

/// <summary>
/// The object type for Crawl, the shield badnik of Casino Night Zone (ObjC8 in s2disasm). "s2hd-data import-level
/// cnz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Crawl")]
[Description("Crawl from Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Badnik)]
[ObjectInstance(typeof(CrawlInstance))]
public class CrawlType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	/// <summary>The game data has no bumper sound. The spring sound is the nearest.</summary>
	[Dependency]
	public const string SoundResourceKey = "SONICORCA/SOUND/SPRING";
}
