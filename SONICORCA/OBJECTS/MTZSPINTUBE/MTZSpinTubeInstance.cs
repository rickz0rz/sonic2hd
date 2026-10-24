using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Core;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.MTZSPINTUBE;

/// <summary>
/// The entrance of a spin tube, as in Sonic 2 (Obj67 in Metropolis Zone). A character that comes into the entrance
/// becomes a spinball and bobs up and down for $40 frames. Then the tube sends the character along the path of the
/// subtype (bits 0-3) at $1000 on the longer axis, from point to point. The points are positions in the act, as in
/// the original. Subtype bit 7 takes the path from its end. At the end of the path, the character stops, or keeps
/// its speed with subtype bit 4. The entrance flashes while a character is in the tube.
/// </summary>
public class MTZSpinTubeInstance : ActiveObject
{
	private const int Scale = 4;

	private const double Unit = Scale / 256.0;

	private const int PathSpeed = 0x1000;

	/// <summary>The character bobs while the angle goes from 0 to $80, 2 each frame.</summary>
	private const int BobEndAngle = 0x80;

	/// <summary>The flash (Ani_obj67 animation 1): each frame shows for 2 frames, then the last two frames repeat.</summary>
	private static readonly int[] FlashFrames = { 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 1, 0 };

	private const int FlashLoopStart = 13;

	/// <summary>The paths of the tubes (misc/obj67.asm): the points, in original pixels in the act.</summary>
	private static readonly int[][] Paths =
	{
		new[] { 0x7A8, 0x270, 0x750, 0x270, 0x740, 0x280, 0x740, 0x3E0, 0x750, 0x3F0, 0x7A8, 0x3F0 },
		new[] { 0xC58, 0x5F0, 0xE28, 0x5F0 },
		new[] { 0x1828, 0x6B0, 0x17D0, 0x6B0, 0x17C0, 0x6C0, 0x17C0, 0x7E0, 0x17B0, 0x7F0, 0x1758, 0x7F0 },
		new[] { 0x5D8, 0x370, 0x780, 0x370 },
		new[] { 0x5D8, 0x5F0, 0x700, 0x5F0 },
		new[] { 0xBD8, 0x1F0, 0xC30, 0x1F0, 0xC40, 0x1E0, 0xC40, 0xC0, 0xC50, 0xB0, 0xCA8, 0xB0 },
		new[] { 0x1728, 0x330, 0x15D0, 0x330, 0x15C0, 0x320, 0x15C0, 0x240, 0x15D0, 0x230, 0x1628, 0x230 },
		new[] { 0x6D8, 0x1F0, 0x730, 0x1F0, 0x740, 0x1E0, 0x740, 0x100, 0x750, 0xF0, 0x7A8, 0xF0 },
		new[] { 0x7D8, 0x330, 0x828, 0x330, 0x840, 0x340, 0x840, 0x458, 0x828, 0x470, 0x7D8, 0x470 },
		new[] { 0xFD8, 0x3B0, 0x1028, 0x3B0, 0x1040, 0x398, 0x1040, 0x2C4, 0x1058, 0x2B0, 0x10A8, 0x2B0 },
		new[] { 0xFD8, 0x4B0, 0x1028, 0x4B0, 0x1040, 0x4C0, 0x1040, 0x5D8, 0x1058, 0x5F0, 0x10A8, 0x5F0 },
		new[] { 0x2058, 0x430, 0x20A8, 0x430, 0x20C0, 0x418, 0x20C0, 0x2C0, 0x20D0, 0x2B0, 0x2128, 0x2B0 },
		new[] { 0x2328, 0x5B0, 0x22D0, 0x5B0, 0x22C0, 0x5A0, 0x22C0, 0x4C0, 0x22D0, 0x4B0, 0x2328, 0x4B0 },
	};

	private readonly Dictionary<ICharacter, Rider> _riders = new Dictionary<ICharacter, Rider>();

	private AnimationInstance _animation;

	private int _subtype;

	private bool _flipX;

	private int _originalX;

	private int _originalY;

	private Vector2i[] _points = Array.Empty<Vector2i>();

	private int _flashTicks;

	private enum Mode
	{
		Bob,
		Path
	}

	private sealed class Rider
	{
		public Mode Mode;

		public int Angle;

		/// <summary>The frames to the next point.</summary>
		public int Frames;

		/// <summary>The index of the next point.</summary>
		public int Next;

		/// <summary>The position in original pixels.</summary>
		public Vector2 Position;

		/// <summary>The velocity in 1/256 original pixels each frame.</summary>
		public Vector2i Velocity;
	}

	/// <summary>The subtype of the original.</summary>
	[StateVariable]
	private int Subtype
	{
		get => _subtype;
		set => _subtype = value;
	}

	/// <summary>The entrance faces the other way.</summary>
	[StateVariable]
	private bool FlipX
	{
		get => _flipX;
		set => _flipX = value;
	}

	/// <summary>The x position of the original placement: the paths are positions in the act.</summary>
	[StateVariable]
	private int OriginalX
	{
		get => _originalX;
		set => _originalX = value;
	}

	/// <summary>The y position of the original placement.</summary>
	[StateVariable]
	private int OriginalY
	{
		get => _originalY;
		set => _originalY = value;
	}

	public MTZSpinTubeInstance() => DesignBounds = new Rectanglei(-0x20 * Scale, -0x20 * Scale, 0x40 * Scale, 0x40 * Scale);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(MTZSpinTubeType.AnimationGroupResourceKey));
		int index = ((_subtype & 0x80) != 0 ? -_subtype : _subtype) & 0xF;
		if (index >= Paths.Length)
		{
			return;
		}
		int[] path = Paths[index];
		_points = new Vector2i[path.Length / 2];
		for (int i = 0; i < _points.Length; i++)
		{
			_points[i] = new Vector2i(path[i * 2], path[i * 2 + 1]);
		}
		if ((_subtype & 0x80) != 0)
		{
			Array.Reverse(_points);
		}
	}

	/// <summary>The HD position of a position in the act of the original.</summary>
	private Vector2 ToLevel(Vector2 original) =>
		(Vector2)Position + new Vector2((original.X - _originalX) * Scale, (original.Y - _originalY) * Scale);

	protected override void OnUpdate()
	{
		foreach (ICharacter character in Level.ObjectManager.Characters.ToArray())
		{
			if (_riders.TryGetValue(character, out Rider rider))
			{
				UpdateRider(character, rider);
			}
			else
			{
				TryTake(character);
			}
		}
		LockLifetime = _riders.Count > 0;
		_flashTicks = _riders.Count > 0 ? _flashTicks + 1 : 0;
	}

	/// <summary>Takes a character that comes into the entrance (loc_271D0).</summary>
	private void TryTake(ICharacter character)
	{
		if (_points.Length < 2 || character.IsDebug || character.IsDying || character.IsDead || character.IsObjectControlled)
		{
			return;
		}
		int dx = (int)Math.Floor((character.Position.X - Position.X) / (double)Scale) + 3 + (_flipX ? 0xA : 0);
		int dy = (int)Math.Floor((character.Position.Y - Position.Y) / (double)Scale) + 0x20;
		if ((uint)dx >= 0x10 || (uint)dy >= 0x40)
		{
			return;
		}
		if (!character.IsAirborne)
		{
			character.LeaveGround();
		}
		character.ObjectLink = this;
		character.IsObjectControlled = true;
		character.CheckCollision = false;
		character.IsAirborne = true;
		character.IsSpinball = true;
		character.IsPushing = false;
		character.Velocity = new Vector2();
		character.GroundVelocity = 0x800 * Unit;
		character.PositionPrecise = (Vector2)Position;
		_riders[character] = new Rider { Mode = Mode.Bob };
		_flashTicks = 0;
		Level.SoundManager.PlaySound(this, Type.GetAbsolutePath(MTZSpinTubeType.EnterSoundResourceKey));
	}

	private void UpdateRider(ICharacter character, Rider rider)
	{
		if (character.ObjectLink != this || character.IsDebug || character.IsDying || character.IsDead)
		{
			Drop(character);
			return;
		}
		if (rider.Mode == Mode.Bob)
		{
			// The character bobs up to 8 pixels (loc_27260)
			int angle = rider.Angle;
			rider.Angle += 2;
			Hold(character, (Vector2)Position - new Vector2(0.0, (OriginalMath.Sin(angle) >> 5) * Scale));
			if (rider.Angle == BobEndAngle)
			{
				rider.Mode = Mode.Path;
				rider.Position = _points[0];
				rider.Next = 1;
				Hold(character, ToLevel(rider.Position));
				Aim(rider);
				Level.SoundManager.PlaySound(this, Type.GetAbsolutePath(MTZSpinTubeType.LaunchSoundResourceKey));
			}
			return;
		}
		if (--rider.Frames >= 0)
		{
			rider.Position += new Vector2(rider.Velocity.X / 256.0, rider.Velocity.Y / 256.0);
			Hold(character, ToLevel(rider.Position));
			return;
		}
		// The character reaches the point (loc_27294)
		rider.Position = _points[rider.Next];
		rider.Next++;
		Hold(character, ToLevel(rider.Position));
		if (rider.Next >= _points.Length)
		{
			Release(character, rider);
			return;
		}
		Aim(rider);
	}

	/// <summary>
	/// Finds the velocity to the next point and the number of frames (loc_27374): $1000 on the longer axis, and the
	/// other axis in proportion.
	/// </summary>
	private void Aim(Rider rider)
	{
		Vector2i target = _points[rider.Next];
		int dx = target.X - (int)Math.Floor(rider.Position.X);
		int dy = target.Y - (int)Math.Floor(rider.Position.Y);
		int speedX = dx < 0 ? -PathSpeed : PathSpeed;
		int speedY = dy < 0 ? -PathSpeed : PathSpeed;
		if (Math.Abs(dy) >= Math.Abs(dx))
		{
			int steps = dy * 0x10000 / speedY;
			rider.Velocity = new Vector2i(dx == 0 ? 0 : dx * 0x10000 / steps, speedY);
			rider.Frames = Math.Abs(steps) >> 8;
		}
		else
		{
			int steps = dx * 0x10000 / speedX;
			rider.Velocity = new Vector2i(speedX, dy == 0 ? 0 : dy * 0x10000 / steps);
			rider.Frames = Math.Abs(steps) >> 8;
		}
	}

	private static void Hold(ICharacter character, Vector2 position)
	{
		character.PositionPrecise = position;
		character.IsAirborne = true;
		character.IsSpinball = true;
		character.Velocity = new Vector2(0.0, 0.001);
	}

	/// <summary>Lets the character go at the end of the path (loc_272EE).</summary>
	private void Release(ICharacter character, Rider rider)
	{
		Vector2 velocity = (_subtype & 0x10) != 0 ? new Vector2(rider.Velocity.X * Unit, rider.Velocity.Y * Unit) : new Vector2();
		Drop(character);
		character.IsAirborne = true;
		character.Velocity = velocity;
	}

	private void Drop(ICharacter character)
	{
		_riders.Remove(character);
		if (character.ObjectLink == this)
		{
			character.ObjectLink = null;
		}
		character.IsObjectControlled = false;
		character.CheckCollision = true;
	}

	protected override void OnStop()
	{
		foreach (ICharacter character in _riders.Keys.ToArray())
		{
			Drop(character);
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		if (_riders.Count == 0)
		{
			return;
		}
		int index = _flashTicks / 2;
		if (index >= FlashFrames.Length)
		{
			index = FlashLoopStart + (index - FlashLoopStart) % 2;
		}
		if (FlashFrames[index] == 0)
		{
			// Frame 0 is empty
			return;
		}
		_animation.Index = FlashFrames[index];
		renderer.GetObjectRenderer().RenderOriginal(_animation, _flipX);
	}
}
