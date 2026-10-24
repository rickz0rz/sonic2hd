using System;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.ARZRISINGPILLAR;

/// <summary>
/// A pillar that rises, as in Sonic 2 (Obj2B). When a player is within $40 pixels on the x axis, its top rises 8
/// pixels each 4 frames, 6 times. When a character stands on it, it breaks into pieces that fly out, and the
/// characters on it fall in a ball.
/// </summary>
public class ARZRisingPillarInstance : ActiveObject
{
	private const int Scale = 4;

	private const int HalfWidth = 0x10 * Scale;

	private const int TriggerDistance = 0x40 * Scale;

	private const int Steps = 6;

	private const int IntactFrames = 7;

	/// <summary>The pieces of frames 7 to 13 follow the 14 frames: one frame for each height.</summary>
	private const int FirstPiece = 14;

	private static readonly int[] PieceCounts = { 8, 10, 10, 12, 12, 14, 14 };

	/// <summary>The speed of each piece, and its delay before it moves.</summary>
	private static readonly int[] PieceSpeeds = { 0x200, 0x200, 0x1C0, 0x1C0, 0x180, 0x180, 0x140, 0x140, 0x100, 0x100, 0xC0, 0xC0, 0x80, 0x80 };

	private static readonly int[] PieceDelays = { 0, 0, 0, 0, 4, 4, 8, 8, 0xC, 0xC, 0x10, 0x10, 0x14, 0x14 };

	private const int LifeTicks = 240;

	private AnimationInstance _animation;

	private int _frame;

	private int _halfHeight = 0x18 * Scale;

	private bool _rising;

	private int _stepTimer;

	private bool _broken;

	private int _ticks;

	private Vector2[] _pieces;

	private Vector2[] _pieceVelocities;

	private int[] _pieceDelays;

	public ARZRisingPillarInstance() => DesignBounds = new Rectanglei(-HalfWidth - 64, -0x60 * Scale, HalfWidth * 2 + 128, 0xC0 * Scale);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(ARZRisingPillarType.AnimationGroupResourceKey));
		UpdateVectors();
	}

	private void UpdateVectors()
	{
		CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-HalfWidth, -_halfHeight, HalfWidth * 2, _halfHeight * 2));
		if (Level != null)
		{
			RegisterCollisionUpdate();
		}
	}

	protected override void OnUpdate()
	{
		if (_broken)
		{
			UpdatePieces();
			return;
		}
		foreach (ICharacter character in Level.ObjectManager.Characters)
		{
			if (character.ObjectLink == this)
			{
				Break();
				return;
			}
			if (!_rising && _frame == 0 && !character.IsDebug && Math.Abs(character.Position.X - Position.X) < TriggerDistance)
			{
				_rising = true;
			}
		}
		if (_rising && _frame < Steps && --_stepTimer < 0)
		{
			// The top rises 8 pixels and the bottom stays
			_stepTimer = 3;
			PositionPrecise -= new Vector2(0.0, 4 * Scale);
			_halfHeight += 4 * Scale;
			_frame++;
			UpdateVectors();
		}
	}

	private void Break()
	{
		_broken = true;
		int first = FirstPiece;
		for (int f = 0; f < _frame; f++)
		{
			first += PieceCounts[f];
		}
		int count = PieceCounts[_frame];
		_pieces = new Vector2[count];
		_pieceVelocities = new Vector2[count];
		_pieceDelays = new int[count];
		for (int i = 0; i < count; i++)
		{
			double speed = PieceSpeeds[i] * Scale / 256.0;
			_pieceVelocities[i] = new Vector2(i % 2 == 0 ? -speed : speed, -speed);
			_pieceDelays[i] = PieceDelays[i];
		}
		_frame = first;
		foreach (ICharacter character in Level.ObjectManager.Characters)
		{
			if (character.ObjectLink == this)
			{
				character.LeaveGround();
				character.IsAirborne = true;
				character.IsSpinball = true;
			}
		}
		CollisionVectors = new CollisionVector[0];
		Level.SoundManager.PlaySound(this, Type.GetAbsolutePath(ARZRisingPillarType.SoundResourceKey));
	}

	private void UpdatePieces()
	{
		for (int i = 0; i < _pieces.Length; i++)
		{
			if (_pieceDelays[i] > 0)
			{
				_pieceDelays[i]--;
				continue;
			}
			_pieces[i] += _pieceVelocities[i];
			_pieceVelocities[i].Y += 0x18 * Scale / 256.0;
		}
		if (++_ticks > LifeTicks)
		{
			// Without respawn tracking, the original loads the pillar again when it comes back in range
			Finish();
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		if (!_broken)
		{
			_animation.Index = _frame;
			objectRenderer.Render(_animation);
			return;
		}
		for (int i = 0; i < _pieces.Length; i++)
		{
			_animation.Index = _frame + i;
			objectRenderer.Render(_animation, _pieces[i]);
		}
	}
}
