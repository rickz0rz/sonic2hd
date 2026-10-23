using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Geometry;

namespace SONICORCA.OBJECTS.HTZSEESAW;

/// <summary>The object type of the see-saw and its Sol fireball from Hill Top Zone.</summary>
[SonicOrca.Core.Objects.Metadata.Name("See Saw")]
[Description("See Saw and Sol from Hill Top Zone, Sonic 2")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.Platform)]
[ObjectInstance(typeof(HTZSeeSawInstance))]
public class HTZSeeSawType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";
	[Dependency]
	public const string SolAnimationGroupResourceKey = "SONICORCA/OBJECTS/SOL/ANIGROUP";
	[Dependency]
	public const string BounceSoundResourceKey = "SONICORCA/SOUND/SPRING";

	public new Vector2 GetLifeRadius(ActiveObject state)
	{
		return (Vector2)new Vector2i(0, 2048);
	}
}
