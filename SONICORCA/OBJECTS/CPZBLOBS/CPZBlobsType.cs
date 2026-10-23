using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.CPZBLOBS;

/// <summary>The object type for <see cref="CPZBlobsInstance"/>, a group of chemical blobs that jump between pipes.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Blobs")]
[Description("Blobs from Chemical Plant Zone, Sonic 2")]
[ObjectInstance(typeof(CPZBlobsInstance))]
public class CPZBlobsType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
	[Dependency]
	public const string SoundResourceKey = "SONICORCA/SOUND/BLOB";
}
