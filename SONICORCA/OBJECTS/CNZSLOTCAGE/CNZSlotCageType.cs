using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.CNZSLOTCAGE;

/// <summary>
/// The object type for the cages of Casino Night Zone (ObjD6 in s2disasm): the cages that give points, and the cages
/// of the slot machines. The type runs the slot machine of the level once each frame. "s2hd-data import-level cnz"
/// adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Slot cage")]
[Description("Cage from Casino Night Zone that gives points or runs a slot machine")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.General)]
[ObjectInstance(typeof(CNZSlotCageInstance))]
public class CNZSlotCageType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	[Dependency]
	public const string BombAnimationGroupResourceKey = "/BOMB/ANIGROUP";

	[Dependency]
	public const string RingAnimationGroupResourceKey = "SONICORCA/OBJECTS/RING/ANIGROUP";

	[Dependency]
	public const string RingSoundResourceKey = "SONICORCA/SOUND/RING";

	[Dependency]
	public const string BombSoundResourceKey = "SONICORCA/SOUND/HURT/SPIKES";

	/// <summary>The game data has no casino bonus sound. The tally switch sound is the nearest.</summary>
	[Dependency]
	public const string TickSoundResourceKey = "SONICORCA/SOUND/TALLY/SWITCH";

	protected override void OnStart()
	{
		CNZSlotMachine.For(Level).Reset();
	}

	protected override void OnUpdate()
	{
		CNZSlotMachine.For(Level).Update();
	}
}
