using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.CNZSLOTREELS;

/// <summary>
/// The object type for the reels of a Casino Night Zone slot machine. In the original, the reels are level blocks
/// whose art the slot machine changes. "s2hd-data import-level cnz" places this object on each reel window and adds
/// the reel art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Slot reels")]
[Description("Reels of a slot machine from Casino Night Zone")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.General)]
[ObjectInstance(typeof(CNZSlotReelsInstance))]
public class CNZSlotReelsType : ObjectType
{
	/// <summary>The three reel strips side by side. Each strip has its 8 faces twice, so that a window never wraps.</summary>
	[Dependency]
	public const string TextureResourceKey = "/TEXTURE";
}
