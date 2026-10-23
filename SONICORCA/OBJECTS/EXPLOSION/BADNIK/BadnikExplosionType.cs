using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.EXPLOSION.BADNIK;

/// <summary>The object type of the explosion particle that a destroyed badnik makes.</summary>
[SonicOrca.Core.Objects.Metadata.Name("Badnik explosion")]
[Description("Badnik explosion from Sonic 1")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Particle)]
[ObjectInstance(typeof(BadnikExplosionInstance))]
public class BadnikExplosionType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
	[Dependency]
	public const string SoundResourceKey = "SONICORCA/SOUND/BADNIKEXPLOSION";
}
