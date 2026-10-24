using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.CRAWLTON;

/// <summary>
/// The object type for Crawlton, the snake badnik of Mystic Cave Zone (Obj9E in s2disasm). "s2hd-data import-level
/// mcz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Crawlton")]
[Description("Crawlton from Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Badnik)]
[ObjectInstance(typeof(CrawltonInstance))]
public class CrawltonType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
