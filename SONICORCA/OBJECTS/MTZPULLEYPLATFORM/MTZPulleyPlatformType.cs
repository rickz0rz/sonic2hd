using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.MTZPULLEYPLATFORM;

/// <summary>
/// The object type for the small platforms of Metropolis Zone that go around pulleys (Obj6C in s2disasm).
/// "s2hd-data import-level mtz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Pulley platforms")]
[Description("Small platforms from Metropolis Zone that go around two pulleys")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Platform)]
[ObjectInstance(typeof(MTZPulleyPlatformInstance))]
public class MTZPulleyPlatformType : ObjectType
{
	/// <summary>Obj6C_MapUnc_28372 with the lava cup art.</summary>
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
}
