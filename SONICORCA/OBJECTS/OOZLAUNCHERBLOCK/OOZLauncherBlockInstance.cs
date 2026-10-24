using System.Collections.Generic;
using System.Linq;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SONICORCA.OBJECTS.OOZLAUNCHBALL;

namespace SONICORCA.OBJECTS.OOZLAUNCHERBLOCK;

/// <summary>
/// A launcher block, as in Sonic 2 (Obj3D). The block is solid, $10 pixels to each side. When a character lands on
/// it or stands on it as a spinball, it breaks into 16 pieces that fall with gravity $18, and the character falls
/// on with its speed. In its place stays an invisible launcher. A character that enters the launcher becomes a
/// spinball, and moves at $800 in a straight line through the landscape until a launcher ball takes it or it leaves
/// the screen. The block with vertical stripes (subtype 0) catches the character in the $10 pixels below its centre
/// and sends it to the right. The block with horizontal stripes catches it in the $10 pixels above its centre and
/// sends it up.
/// </summary>
public class OOZLauncherBlockInstance : ActiveObject
{
	private const int Scale = 4;

	private const double Unit = Scale / 256.0;

	private const int HalfSize = 0x10 * Scale;

	private const int FragmentGravity = 0x18;

	private const int LaunchSpeed = 0x800;

	private const int CaptureRange = 0x10;

	/// <summary>The animation of the first piece. The pieces of the broken frame follow the 4 frames.</summary>
	private const int FirstPiece = 4;

	/// <summary>The speeds of the 16 pieces (word_2507A).</summary>
	private static readonly int[,] FragmentVelocities =
	{
		{ -0x400, -0x400 }, { -0x200, -0x400 }, { 0x200, -0x400 }, { 0x400, -0x400 },
		{ -0x3C0, -0x200 }, { -0x1C0, -0x200 }, { 0x1C0, -0x200 }, { 0x3C0, -0x200 },
		{ -0x380, 0x200 }, { -0x180, 0x200 }, { 0x180, 0x200 }, { 0x380, 0x200 },
		{ -0x340, 0x400 }, { -0x140, 0x400 }, { 0x140, 0x400 }, { 0x340, 0x400 }
	};

	/// <summary>Whether each character was a spinball before this frame, and its velocity then.</summary>
	private readonly Dictionary<ICharacter, (bool Spinball, Vector2 Velocity)> _before = new Dictionary<ICharacter, (bool, Vector2)>();

	private readonly Dictionary<ICharacter, Rider> _riders = new Dictionary<ICharacter, Rider>();

	private readonly List<Fragment> _fragments = new List<Fragment>();

	private AnimationInstance _animation;

	private bool _horizontal;

	private bool _broken;

	private sealed class Rider
	{
		public Vector2 Position;

		/// <summary>The velocity in HD pixels for each frame.</summary>
		public Vector2 Velocity;
	}

	private sealed class Fragment
	{
		public int Piece;

		/// <summary>The offset from the block, and the velocity, in HD pixels.</summary>
		public Vector2 Offset;

		public Vector2 Velocity;
	}

	/// <summary>The block has horizontal stripes and launches up (subtype not 0).</summary>
	[StateVariable]
	private bool Horizontal
	{
		get => _horizontal;
		set => _horizontal = value;
	}

	public OOZLauncherBlockInstance() => DesignBounds = new Rectanglei(-96, -96, 192, 192);

	protected override void OnStart()
	{
		string key = _horizontal ? OOZLauncherBlockType.HorizontalAnimationGroupResourceKey : OOZLauncherBlockType.AnimationGroupResourceKey;
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(key));
		CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-HalfSize, -HalfSize, HalfSize * 2, HalfSize * 2));
	}

	protected override void OnUpdate()
	{
		if (!_broken)
		{
			CheckBreak();
			// A character that lands loses its spinball and its fall speed after this update, so keep them now
			_before.Clear();
			foreach (ICharacter character in Level.ObjectManager.Characters)
			{
				_before[character] = (character.IsSpinball, character.Velocity);
			}
		}
		else
		{
			UpdateFragments();
			foreach (ICharacter character in Level.ObjectManager.Characters.ToArray())
			{
				UpdateLauncher(character);
			}
		}
		LockLifetime = _riders.Count > 0;
	}

	/// <summary>
	/// Breaks the block when the character on it was a spinball. When both characters stand on it, one spinball is
	/// enough, and the other character falls too.
	/// </summary>
	private void CheckBreak()
	{
		ICharacter[] standing = Level.ObjectManager.Characters.Where(c => c.ObjectLink == this && !c.IsAirborne).ToArray();
		bool WasSpinball(ICharacter c) => _before.TryGetValue(c, out var before) && before.Spinball;
		if (!standing.Any(WasSpinball))
		{
			return;
		}
		foreach (ICharacter character in standing)
		{
			character.LeaveGround();
			character.IsAirborne = true;
			if (WasSpinball(character))
			{
				character.IsSpinball = true;
				character.Velocity = _before[character].Velocity;
			}
		}
		_broken = true;
		CollisionVectors = new CollisionVector[0];
		RegisterCollisionUpdate();
		for (int i = 0; i < FragmentVelocities.GetLength(0); i++)
		{
			_fragments.Add(new Fragment
			{
				Piece = i,
				Velocity = new Vector2(FragmentVelocities[i, 0] * Unit, FragmentVelocities[i, 1] * Unit)
			});
		}
		Level.SoundManager.PlaySound(this, Type.GetAbsolutePath(OOZLauncherBlockType.BreakSoundResourceKey));
	}

	/// <summary>The pieces fall until they leave the screen.</summary>
	private void UpdateFragments()
	{
		for (int i = _fragments.Count - 1; i >= 0; i--)
		{
			Fragment fragment = _fragments[i];
			fragment.Offset += fragment.Velocity;
			fragment.Velocity += new Vector2(0.0, FragmentGravity * Unit);
			if (!Level.Camera.Bounds.Contains((Vector2)Position + fragment.Offset))
			{
				_fragments.RemoveAt(i);
			}
		}
	}

	private void UpdateLauncher(ICharacter character)
	{
		if (!_riders.TryGetValue(character, out Rider rider))
		{
			TryTake(character);
			return;
		}
		if (!OOZTransport.IsCarriedBy(this, character))
		{
			// A launcher ball took the character
			_riders.Remove(character);
			return;
		}
		if (character.IsDebug || character.IsDying || character.IsDead)
		{
			OOZTransport.Drop(character);
			_riders.Remove(character);
			return;
		}
		if (!OOZTransport.IsOnScreen(Level, character))
		{
			OOZTransport.Release(character, rider.Velocity);
			_riders.Remove(character);
			return;
		}
		rider.Position += rider.Velocity;
		OOZTransport.Hold(character, rider.Position, rider.Velocity);
	}

	private void TryTake(ICharacter character)
	{
		int dy = character.Position.Y / Scale - Position.Y / Scale + (_horizontal ? CaptureRange : 0);
		if (!OOZTransport.CanTake(character) ||
			(ushort)(character.Position.X / Scale - Position.X / Scale + CaptureRange) >= CaptureRange * 2 ||
			(ushort)dy >= CaptureRange)
		{
			return;
		}
		OOZTransport.Take(this, character);
		character.GroundVelocity = LaunchSpeed * Unit;
		var rider = new Rider();
		if (_horizontal)
		{
			rider.Position = new Vector2(Position.X, character.PositionPrecise.Y);
			rider.Velocity = new Vector2(0.0, -LaunchSpeed * Unit);
		}
		else
		{
			rider.Position = new Vector2(character.PositionPrecise.X, Position.Y);
			rider.Velocity = new Vector2(LaunchSpeed * Unit, 0.0);
		}
		OOZTransport.Hold(character, rider.Position, rider.Velocity);
		_riders[character] = rider;
		Level.SoundManager.PlaySound(this, Type.GetAbsolutePath(OOZLauncherBlockType.LaunchSoundResourceKey));
	}

	protected override void OnStop()
	{
		foreach (KeyValuePair<ICharacter, Rider> pair in _riders)
		{
			if (OOZTransport.IsCarriedBy(this, pair.Key))
			{
				OOZTransport.Release(pair.Key, pair.Value.Velocity);
			}
		}
		_riders.Clear();
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		if (!_broken)
		{
			_animation.Index = _horizontal ? 2 : 0;
			objectRenderer.Render(_animation);
			return;
		}
		foreach (Fragment fragment in _fragments)
		{
			_animation.Index = FirstPiece + fragment.Piece;
			objectRenderer.Render(_animation, fragment.Offset);
		}
	}
}
