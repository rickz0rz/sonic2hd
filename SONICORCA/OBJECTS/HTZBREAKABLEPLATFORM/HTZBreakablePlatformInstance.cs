// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.HTZBREAKABLEPLATFORM.HTZBreakablePlatformInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

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

#nullable disable
namespace SONICORCA.OBJECTS.HTZBREAKABLEPLATFORM;

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
	private AnimationInstance _animation;
	private bool _breakBlockOff;
	private Player _playerThatBroke;

	[StateVariable]
	private uint Paths { get; set; } = uint.MaxValue;

	[StateVariable]
	private int RemainingBlocks { get; set; } = 5;

	[StateVariable]
	private int ResetPath { get; set; } = -1;

	public HTZBreakablePlatformInstance()
	{
		DesignBounds = new Rectanglei(-64, -160, 128 /*0x80*/, 384);
	}

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"), 5 - RemainingBlocks);
		CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei());
		CollisionVectors[1].Id = 1;
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
				foreach (Vector2 particleVelocity in (IEnumerable<Vector2>)HTZBreakablePlatformInstance.ParticleVelocities)
					Level.ObjectManager.AddObject(new ObjectPlacement(Type.GetAbsolutePath("SONICORCA/OBJECTS/HTZROCK/PARTICLE"), Level.Map.Layers.IndexOf(Level.Map.Layers.Last<LevelLayer>()), Position, new
					{
						Velocity = new Vector2(particleVelocity.X, particleVelocity.Y)
					}));
				Level.SoundManager.PlaySound(this, Type.GetAbsolutePath("SONICORCA/SOUND/BREAKABLE"));
				Level.CreateScoreObject(_playerThatBroke.AwardChainedScore(), Position);
				--RemainingBlocks;
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
			UpdateBlockCollision();
		}
	}

	private void UpdateBlockCollision()
	{
		if (RemainingBlocks <= 0 || CollisionVectors.Length == 0)
			return;
		int height = RemainingBlocks * 64 /*0x40*/;
		CollisionVector.UpdateRectangle(CollisionVectors, new Rectanglei(-64, 160 /*0xA0*/ - height, 128 /*0x80*/, height));
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
		if (e.Id != 1 || e.ActiveObject.Type.Classification != ObjectClassification.Character)
			return;
		ICharacter activeObject = (ICharacter)e.ActiveObject;
		if (RemainingBlocks == 5)
		{
			if ((Paths & 1 << activeObject.Path) == 0L)
				return;
			if (activeObject.IsSpinball)
			{
				BreakBlocks(e, activeObject);
			}
			else
			{
				if (ResetPath == -1)
					return;
				activeObject.Path = ResetPath;
				if (activeObject.Mode == CollisionMode.Top)
					return;
				activeObject.GroundVelocity = 0.0;
				activeObject.LeaveGround();
			}
		}
		else
		{
			if (!activeObject.IsSpinball)
				return;
			BreakBlocks(e, activeObject);
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
