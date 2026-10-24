using System;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.ARZFALLINGPILLAR;

/// <summary>
/// A pillar that drops its lower part, as in Sonic 2 (Obj23). When a player is within $80 pixels on the x axis, the
/// lower part shakes for 8 frames, then falls with gravity $38 until it lands on the floor. Both parts are solid.
/// </summary>
public class ARZFallingPillarInstance : ActiveObject
{
	private const int Scale = 4;

	private const int HalfWidth = 0x10 * Scale;

	private const int TopHalfHeight = 0x20 * Scale;

	private const int LowerHalfHeight = 0x10 * Scale;

	private const int LowerOffset = 0x30 * Scale;

	private const int TriggerDistance = 0x80 * Scale;

	/// <summary>The x offsets of the shake, one each frame.</summary>
	private static readonly int[] Shake = { 1, 0, -1, 0, 1, -1, 1, 0 };

	private AnimationInstance _animation;

	private State _state;

	private int _shakeIndex;

	private Vector2 _lower;

	private double _velocityY;

	private int _lowerFrame = 1;

	private enum State
	{
		Wait,
		Shake,
		Fall,
		Landed
	}

	public ARZFallingPillarInstance() => DesignBounds = new Rectanglei(-HalfWidth - 32, -TopHalfHeight, HalfWidth * 2 + 64, 0x200 * Scale);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(ARZFallingPillarType.AnimationGroupResourceKey));
		_lower = new Vector2(0.0, LowerOffset);
		UpdateVectors();
	}

	private void UpdateVectors()
	{
		var top = CollisionVector.FromRectangle(this, new Rectanglei(-HalfWidth, -TopHalfHeight, HalfWidth * 2, TopHalfHeight * 2));
		var lower = CollisionVector.FromRectangle(this, new Rectanglei((int)_lower.X - HalfWidth, (int)_lower.Y - LowerHalfHeight, HalfWidth * 2, LowerHalfHeight * 2));
		CollisionVectors = new[] { top[0], top[1], top[2], top[3], lower[0], lower[1], lower[2], lower[3] };
		if (Level != null)
		{
			RegisterCollisionUpdate();
		}
	}

	protected override void OnUpdate()
	{
		switch (_state)
		{
			case State.Wait:
				foreach (ICharacter character in Level.ObjectManager.Characters)
				{
					if (!character.IsDebug && Math.Abs(character.Position.X - Position.X) < TriggerDistance)
					{
						_state = State.Shake;
						_shakeIndex = Shake.Length;
					}
				}
				break;
			case State.Shake:
				if (_shakeIndex == 0)
				{
					_state = State.Fall;
					break;
				}
				_shakeIndex--;
				_lower = new Vector2(Shake[Shake.Length - 1 - _shakeIndex] * Scale, LowerOffset);
				UpdateVectors();
				break;
			case State.Fall:
			{
				_lower.Y += _velocityY;
				_velocityY += 0x38 * Scale / 256.0;
				Vector2i bottom = Position + new Vector2i((int)_lower.X, (int)_lower.Y + LowerHalfHeight);
				int? floor = LandscapeFloor.GetDistance(Level, bottom);
				if (floor < 0)
				{
					_lower.Y += floor.Value * Scale;
					_velocityY = 0.0;
					_lowerFrame = 2;
					_state = State.Landed;
				}
				UpdateVectors();
				break;
			}
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		_animation.Index = 0;
		objectRenderer.Render(_animation);
		_animation.Index = _lowerFrame;
		objectRenderer.Render(_animation, _lower);
	}
}
