using System.Collections.Generic;
using System.Linq;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.HTZBREAKABLEPLATFORM;

/// <summary>
/// A column of breakable blocks from Hill Top Zone. A character in a spinball that hits the top breaks off one block,
/// which throws out rock particles and gives score. The object finishes when no blocks remain.
/// </summary>
public class HTZBreakablePlatformInstance : ActiveObject
{
	private static readonly IReadOnlyCollection<Vector2> ParticleVelocities = new Vector2[6]
	{
		new Vector2(-6.0, -14.0),
		new Vector2(-4.0, -18.0),
		new Vector2(-2.0, -22.0),
		new Vector2(2.0, -24.0),
		new Vector2(4.0, -16.0),
		new Vector2(6.0, -12.0)
	};
	private const int MaximumBlocks = 5;
	private const int BlockHeight = 64;

	/// <summary>The Y position of the bottom of the column.</summary>
	private const int ColumnBottom = 160;

	private const int TopCollisionId = 1;

	private AnimationInstance _animation;
	private bool _breakBlockOff;
	private Player _playerThatBroke;

	/// <summary>A bit mask of the paths on which the full column reacts to characters.</summary>
	[StateVariable]
	private uint Paths { get; set; } = uint.MaxValue;

	[StateVariable]
	private int RemainingBlocks { get; set; } = MaximumBlocks;

	/// <summary>
	/// The path that a character that is not in a spinball goes to when it touches the full column. -1 means no
	/// change.
	/// </summary>
	[StateVariable]
	private int ResetPath { get; set; } = -1;

	public HTZBreakablePlatformInstance()
	{
		DesignBounds = new Rectanglei(-64, -160, 128, 384);
	}

	protected override void OnStart()
	{
		// The animation index is the number of blocks that are already broken.
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"), MaximumBlocks - RemainingBlocks);
		CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei());
		// Vector 1 is the top of the column.
		CollisionVectors[1].Id = TopCollisionId;
		CollisionVectors[1].Flags = CollisionFlags.NoAngle | CollisionFlags.NoBalance;
		UpdateBlockCollision();
	}

	protected override void OnUpdate()
	{
		if (RemainingBlocks == 0)
		{
			FinishForever();
		}
		else
		{
			if (_breakBlockOff)
			{
				BreakBlockOff();
			}
			UpdateBlockCollision();
		}
	}

	private void BreakBlockOff()
	{
		foreach (Vector2 particleVelocity in ParticleVelocities)
		{
			Level.ObjectManager.AddObject(new ObjectPlacement(Type.GetAbsolutePath("SONICORCA/OBJECTS/HTZROCK/PARTICLE"), Level.Map.Layers.IndexOf(Level.Map.Layers.Last()), Position, new
			{
				Velocity = new Vector2(particleVelocity.X, particleVelocity.Y)
			}));
		}
		Level.SoundManager.PlaySound(this, Type.GetAbsolutePath("SONICORCA/SOUND/BREAKABLE"));
		// The original (Obj2F) breaks the whole column at once and gives one score. So in Original physics, only the
		// first block of a full column gives score.
		if (!Level.OriginalPhysics || RemainingBlocks == MaximumBlocks)
			Level.CreateScoreObject(_playerThatBroke.AwardChainedScore(), Position);
		--RemainingBlocks;
		// Store the count in the level entry so that the object keeps it when it is created again.
		(Entry.State as HTZBreakablePlatformInstance).RemainingBlocks = RemainingBlocks;
		_breakBlockOff = false;
		if (RemainingBlocks == 0)
		{
			FinishForever();
			CollisionVectors = new CollisionVector[0];
		}
		else
		{
			++_animation.Index;
			foreach (ICharacter standingCharacter in GetStandingCharacters())
				standingCharacter.LeaveGround();
		}
	}

	private void UpdateBlockCollision()
	{
		if (RemainingBlocks <= 0 || CollisionVectors.Length == 0)
			return;
		int height = RemainingBlocks * BlockHeight;
		CollisionVector.UpdateRectangle(CollisionVectors, new Rectanglei(-64, ColumnBottom - height, 128, height));
		RegisterCollisionUpdate();
	}

	private List<ICharacter> GetStandingCharacters()
	{
		List<ICharacter> standingCharacters = new List<ICharacter>();
		foreach (Character character in Level.ObjectManager.Characters)
		{
			if (character.GroundVector == CollisionVectors[1])
				standingCharacters.Add(character);
		}
		return standingCharacters;
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (e.Id != TopCollisionId || e.ActiveObject.Type.Classification != ObjectClassification.Character)
			return;
		ICharacter character = (ICharacter)e.ActiveObject;
		if (RemainingBlocks == MaximumBlocks)
		{
			if ((Paths & (1 << character.Path)) == 0L)
				return;
			if (character.IsSpinball)
			{
				BreakBlocks(e, character);
			}
			else
			{
				if (ResetPath == -1)
					return;
				character.Path = ResetPath;
				if (character.Mode == CollisionMode.Top)
					return;
				character.GroundVelocity = 0.0;
				character.LeaveGround();
			}
		}
		else
		{
			if (!character.IsSpinball)
				return;
			BreakBlocks(e, character);
		}
	}

	private void BreakBlocks(CollisionEvent e, ICharacter character)
	{
		_breakBlockOff = true;
		_playerThatBroke = character.Player;
		character.LeaveGround();
		character.Velocity = new Vector2(character.Velocity.X, -1.0);
		e.MaintainVelocity = true;
	}

	protected override void OnAnimate() => _animation.Animate();

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		renderer.GetObjectRenderer().Render(_animation);
	}
}
