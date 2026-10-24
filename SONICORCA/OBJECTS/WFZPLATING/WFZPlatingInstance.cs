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

namespace SONICORCA.OBJECTS.WFZPLATING;

/// <summary>
/// A breakable plating, as in Sonic 2 (ObjC1). Two columns of panels hang to the left of a pole. When the main player
/// touches the pole from the left, it hangs 20 pixels to the left of the pole. Up and down move it 1 pixel each frame,
/// up to 24 pixels from the centre. After subtype seconds (never with subtype 0), or when the player jumps, the player
/// lets go and falls. Then the plating breaks into four panels. Each panel flies to the left at $400 after a short
/// delay, falls, and turns.
/// </summary>
public class WFZPlatingInstance : ActiveObject
{
	private const int Scale = 4;

	private const int TicksPerSecond = 60;

	/// <summary>The player hangs this number of pixels to the left of the pole.</summary>
	private const int HangDistance = 20;

	private const int ClimbRange = 24;

	private const int FrameWhole = 0;

	private const int FramePanel = 1;

	/// <summary>The positions of the panels from the pole (ObjC1_Positions) and their delays (ObjC1_BreakTimes).</summary>
	private static readonly (int X, int Y, int Delay)[] Panels = { (-0x10, -0x10, 0), (-0x10, 0x10, 4), (-0x30, -0x10, 0x18), (-0x30, 0x10, 0x20) };

	/// <summary>The turn of a falling panel (Ani_objC1): frames 2, 3, 4, 5 and 1, 4 frames each.</summary>
	private static readonly int[] TurnFrames = { 2, 3, 4, 5, 1 };

	private const int TurnFrameTicks = 4;

	private const int PanelSpeedX = -0x400;

	private const int Gravity = 8;

	private readonly List<Panel> _panels = new List<Panel>();

	private AnimationInstance _animation;

	private int _seconds;

	private int _breakTicks;

	private ICharacter _hanging;

	private bool _broken;

	private bool _jumpPressed;

	private int _verticalDirection;

	private sealed class Panel
	{
		/// <summary>The position in original pixels, from the pole.</summary>
		public Vector2 Position;

		public int VelocityY;

		public int Delay;

		public int Ticks;
	}

	/// <summary>The number of seconds before the plating breaks under the player (the subtype of the original).</summary>
	[StateVariable]
	private int Seconds
	{
		get => _seconds;
		set => _seconds = value;
	}

	public WFZPlatingInstance() => DesignBounds = new Rectanglei(-0x40 * Scale, -0x20 * Scale, 0x40 * Scale, 0x40 * Scale);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(WFZPlatingType.AnimationGroupResourceKey));
		_breakTicks = _seconds * TicksPerSecond;
		// The touch box of the pole (collision size $21: 4 by $10 pixels to each side)
		CollisionRectangles = new[] { new CollisionRectangle(this, 0, -4 * Scale, -0x10 * Scale, 8 * Scale, 0x20 * Scale) };
	}

	protected override void OnUpdatePrepare()
	{
		// The character clears its input when an object controls it, so read the buttons before it updates
		_jumpPressed = false;
		_verticalDirection = 0;
		if (_hanging is Character character)
		{
			_jumpPressed = character.Input.ABC == CharacterInputButtonState.Pressed;
			_verticalDirection = System.Math.Sign(character.Input.VerticalDirection);
		}
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (_broken || _hanging != null || e.ActiveObject != Level.Player.Protagonist)
		{
			return;
		}
		ICharacter character = (ICharacter)e.ActiveObject;
		if (character.PositionPrecise.X <= PositionPrecise.X - HangDistance * Scale)
		{
			return;
		}
		if (character.IsObjectControlled || character.IsDebug || character.IsHurt || character.IsDying || character.IsDead)
		{
			return;
		}
		_hanging = character;
		character.GroundVelocity = 0.0;
		character.IsSpinball = false;
		character.IsJumping = false;
		character.Facing = -1;
		character.SpecialState = CharacterSpecialState.Grabbed;
		Hold(character, character.PositionPrecise.Y);
	}

	protected override void OnUpdate()
	{
		if (_broken)
		{
			UpdatePanels();
			return;
		}
		if (_hanging == null)
		{
			return;
		}
		if (_hanging.IsDebug || _hanging.IsHurt || _hanging.IsDying || _hanging.IsDead)
		{
			Break();
			return;
		}
		if (_breakTicks != 0 && --_breakTicks == 0)
		{
			Break();
			return;
		}
		// Up and down move the player along the pole, up to 24 pixels from the centre
		double y = _hanging.PositionPrecise.Y;
		if (_verticalDirection < 0)
		{
			y = System.Math.Max(y - Scale, PositionPrecise.Y - ClimbRange * Scale);
		}
		else if (_verticalDirection > 0)
		{
			y = System.Math.Min(y + Scale, PositionPrecise.Y + ClimbRange * Scale);
		}
		if (_jumpPressed)
		{
			Break();
			return;
		}
		Hold(_hanging, y);
	}

	private void Hold(ICharacter character, double y)
	{
		character.IsObjectControlled = true;
		character.IsAirborne = true;
		character.PositionPrecise = new Vector2(PositionPrecise.X - HangDistance * Scale, y);
		character.Velocity = new Vector2();
	}

	/// <summary>ObjC1_Release and ObjC1_BeginBreakup: the player falls, and the plating breaks into four panels.</summary>
	private void Break()
	{
		if (_hanging != null)
		{
			_hanging.IsObjectControlled = false;
			_hanging.SpecialState = CharacterSpecialState.Normal;
			_hanging.LeaveGround();
			_hanging.IsAirborne = true;
			_hanging = null;
		}
		_broken = true;
		CollisionRectangles = new CollisionRectangle[0];
		foreach ((int x, int y, int delay) in Panels)
		{
			_panels.Add(new Panel { Position = new Vector2(x, y), Delay = delay });
		}
		Level.SoundManager.PlaySound(this, Type.GetAbsolutePath(WFZPlatingType.BreakSoundResourceKey));
	}

	/// <summary>ObjC1_FallOff: each panel waits for its delay, then moves, falls and turns until it leaves the screen.</summary>
	private void UpdatePanels()
	{
		foreach (Panel panel in _panels)
		{
			if (panel.Delay > 0)
			{
				panel.Delay--;
				continue;
			}
			panel.Position += new Vector2(PanelSpeedX / 256.0, panel.VelocityY / 256.0);
			panel.VelocityY += Gravity;
			panel.Ticks++;
		}
		Rectangle camera = Level.Camera.Bounds;
		_panels.RemoveAll(panel => panel.Delay == 0 && !camera.IntersectsWith(PanelBounds(panel)));
		if (_panels.Count == 0)
		{
			// The original deletes the object without clearing its respawn flag, so it does not come back
			FinishForever();
		}
	}

	private Rectangle PanelBounds(Panel panel)
	{
		Vector2 centre = PositionPrecise + panel.Position * Scale;
		return new Rectangle(centre.X - 0x10 * Scale, centre.Y - 0x10 * Scale, 0x20 * Scale, 0x20 * Scale);
	}

	protected override void OnStop()
	{
		if (_hanging != null)
		{
			_hanging.IsObjectControlled = false;
			_hanging.SpecialState = CharacterSpecialState.Normal;
			_hanging = null;
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		if (!_broken)
		{
			_animation.Index = FrameWhole;
			objectRenderer.Render(_animation);
			return;
		}
		foreach (Panel panel in _panels)
		{
			_animation.Index = panel.Ticks == 0 ? FramePanel : TurnFrames[((panel.Ticks - 1) / TurnFrameTicks) % TurnFrames.Length];
			objectRenderer.Render(_animation, panel.Position * Scale);
		}
	}
}
