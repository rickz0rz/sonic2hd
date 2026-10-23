using System.Collections.Generic;
using System.Linq;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.HTZROCK;

/// <summary>
/// The breakable rock from Hill Top Zone. A character in a spinball that hits its top or bottom breaks it. The rock
/// then throws out rock particles and gives score.
/// </summary>
public class HTZRockInstance : ActiveObject
{
	private const int AnimationRock = 0;
	private const int BreakableCollisionId = 1;
	private static readonly IReadOnlyCollection<Vector2> ParticleVelocities = new Vector2[6]
	{
		new Vector2(-8.0, -10.0),
		new Vector2(-6.0, -8.0),
		new Vector2(-2.0, -10.0),
		new Vector2(2.0, -12.0),
		new Vector2(6.0, -8.0),
		new Vector2(8.0, -10.0)
	};
	private AnimationInstance _animation;
	private bool _broken;
	private Player _playerThatBroke;

	public HTZRockInstance()
	{
		DesignBounds = new Rectanglei(-96, -64, 192, 128);
	}

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"), AnimationRock);
		CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-96, -64, 192, 128));
		// Vector 1 is the top and vector 3 is the bottom of the rock.
		CollisionVectors[1].Id = BreakableCollisionId;
		CollisionVectors[3].Id = BreakableCollisionId;
		Priority = -256;
	}

	protected override void OnUpdate()
	{
		if (!_broken)
			return;
		foreach (Vector2 particleVelocity in ParticleVelocities)
		{
			Level.ObjectManager.AddObject(new ObjectPlacement(Type.GetAbsolutePath("/PARTICLE"), Level.Map.Layers.IndexOf(Level.Map.Layers.Last()), Position, new
			{
				Velocity = new Vector2(particleVelocity.X, particleVelocity.Y)
			}));
		}
		Level.SoundManager.PlaySound(this, Type.GetAbsolutePath("SONICORCA/SOUND/BREAKABLE"));
		Level.CreateScoreObject(_playerThatBroke.AwardChainedScore(), Position);
		Finish();
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (e.Id != BreakableCollisionId || e.ActiveObject.Type.Classification != ObjectClassification.Character)
			return;
		ICharacter character = (ICharacter)e.ActiveObject;
		if (!character.IsSpinball)
			return;
		_broken = true;
		_playerThatBroke = character.Player;
		// A character that falls onto the rock bounces up.
		if (character.Velocity.Y > 0.0)
			character.Velocity = new Vector2(character.Velocity.X, -12.0);
		e.MaintainVelocity = true;
	}

	protected override void OnAnimate() => _animation.Animate();

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		renderer.GetObjectRenderer().Render(_animation);
	}
}
