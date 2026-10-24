using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;

namespace SONICORCA.OBJECTS.CNZBONUSBLOCK;

/// <summary>
/// The object type for the bonus blocks of Casino Night Zone (ObjD8 in s2disasm), which go away after three hits.
/// "s2hd-data import-level cnz" adds the art of the original.
/// </summary>
[SonicOrca.Core.Objects.Metadata.Name("Bonus block")]
[Description("Block from Casino Night Zone that goes away after three hits")]
[SonicOrca.Core.Objects.Metadata.Classification(ObjectClassification.General)]
[ObjectInstance(typeof(CNZBonusBlockInstance))]
public class CNZBonusBlockType : ObjectType
{
	[Dependency]
	public const string AnimationGroupResourceKey = "/ANIGROUP";

	/// <summary>The game data has no bumper sound. The spring sound is the nearest.</summary>
	[Dependency]
	public const string SoundResourceKey = "SONICORCA/SOUND/SPRING";

	/// <summary>The number of groups of blocks (CNZ_saucer_data).</summary>
	private const int GroupCount = 64;

	/// <summary>The number of blocks of each group that the players destroyed in this level.</summary>
	public int[] DestroyedInGroup { get; } = new int[GroupCount];

	protected override void OnStart()
	{
		System.Array.Clear(DestroyedInGroup);
	}
}
