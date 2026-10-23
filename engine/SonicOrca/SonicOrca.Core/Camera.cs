using System;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca.Core;

/// <summary>
/// Moves the view of a level. It follows a tracked object, or the player moves it freely in spy mode.
/// </summary>
public class Camera
{
	private const double DefaultWidth = 1920.0;

	private const double DefaultHeight = 1080.0;

	private const double SpyModeAcceleration = 0.5;

	// This is the value of 0.02f after conversion to double. Keep it, so that the result does not change.
	private const double SafeAreaMarginRatio = 0.019999999552965164;

	private const double TrackingMarkerHalfLength = 16.0;

	private readonly Level _level;

	private Rectangle _bounds;

	private Rectangle _trackBounds;

	private Vector2 _velocity;

	public Rectangle ScreenBounds { get; set; }

	public ActiveObject ObjectToTrack { get; set; }

	public Vector2 Acceleration { get; set; }

	public Vector2 Deceleration { get; set; }

	public Vector2 MaxVelocity { get; set; }

	public Rectanglei Limits { get; set; }

	public Rectangle Bounds
	{
		get
		{
			return _bounds;
		}
		set
		{
			_bounds = value;
		}
	}

	public Vector2 Velocity => _velocity;

	public Vector2 Scale => new Vector2(ScreenBounds.Width / Bounds.Width, ScreenBounds.Height / Bounds.Height);

	public bool ShowDebugInformation { get; set; }

	public bool SpyMode { get; set; }

	public Camera(Level level)
	{
		_level = level;
		_trackBounds = new Rectangle(0.0, 0.0, DefaultWidth, DefaultHeight);
		_bounds = new Rectangle(0.0, 0.0, DefaultWidth, DefaultHeight);
		ScreenBounds = new Rectangle(0.0, 0.0, DefaultWidth, DefaultHeight);
		Acceleration = new Vector2(0.0, 0.0);
		Deceleration = new Vector2(0.5, 0.5);
		_velocity = new Vector2(0.0, 0.0);
		MaxVelocity = new Vector2(64.0, 64.0);
		SetScale(1.0);
	}

	public virtual void Update()
	{
		if (SpyMode)
		{
			UpdateSpyMode();
		}
		else
		{
			UpdateTrackMode();
		}
	}

	public void SetScale(double value)
	{
		_bounds.Width = ScreenBounds.Width / value;
		_bounds.Height = ScreenBounds.Height / value;
		_trackBounds.Width = Bounds.Width;
		_trackBounds.Height = Bounds.Height;
	}

	public void CentreObjectToTrack()
	{
		if (ObjectToTrack != null)
		{
			CameraProperties cameraProperties = ObjectToTrack.CameraProperties;
			Vector2 objectPosition = ObjectToTrack.Position;
			_trackBounds.X = objectPosition.X - _trackBounds.Width / 2.0;
			_trackBounds.Y = objectPosition.Y - _trackBounds.Height / 2.0;
			_bounds.X = _trackBounds.X + cameraProperties.Offset.X;
			_bounds.Y = _trackBounds.Y + cameraProperties.Offset.Y;
			ApplyBounds();
		}
	}

	public void Shift(int x, int y)
	{
		_bounds.X += x;
		_bounds.Y += y;
		ApplyBounds();
	}

	protected void ApplyMovement()
	{
		if (Acceleration.X == 0.0)
		{
			_velocity.X = MathX.ChangeSpeed(_velocity.X, 0.0 - Deceleration.X);
		}
		if (Acceleration.Y == 0.0)
		{
			_velocity.Y = MathX.ChangeSpeed(_velocity.Y, 0.0 - Deceleration.Y);
		}
		_velocity += Acceleration;
		_velocity.X = MathX.Clamp(_velocity.X, MaxVelocity.X);
		_velocity.Y = MathX.Clamp(_velocity.Y, MaxVelocity.Y);
		_bounds.X += _velocity.X;
		_bounds.Y += _velocity.Y;
	}

	protected void ApplyBounds()
	{
		double oldX = Bounds.X;
		double oldY = Bounds.Y;
		Rectanglei limits = (Limits == Rectanglei.Empty) ? _level.Bounds : Limits;
		_bounds.X = Math.Max(Bounds.X, limits.X);
		_bounds.Y = Math.Max(Bounds.Y, limits.Y);
		_bounds.X = Math.Min(Bounds.Right, limits.Right) - Bounds.Width;
		_bounds.Y = Math.Min(Bounds.Bottom, limits.Bottom) - Bounds.Height;
		// Known defect: this code sets a copy of the velocity, not _velocity. Thus it has no effect.
		// It is kept as it is, because a fix would change the camera movement.
		Vector2 velocity = Velocity;
		if (Bounds.X != oldX)
		{
			velocity.X = 0.0;
		}
		if (Bounds.Y != oldY)
		{
			velocity.Y = 0.0;
		}
	}

	private void UpdateSpyMode()
	{
		Acceleration = new Vector2(0.0, 0.0);
		if (!_level.DebugContext.Visible)
		{
			double x = _level.GameContext.Current[0].DirectionLeft.X;
			double y = _level.GameContext.Current[0].DirectionLeft.Y;
			Acceleration = new Vector2(x * SpyModeAcceleration, y * SpyModeAcceleration);
		}
		ApplyMovement();
		ApplyBounds();
	}

	private void UpdateTrackMode()
	{
		_velocity = new Vector2(0.0, 0.0);
		if (ObjectToTrack == null)
		{
			return;
		}
		CameraProperties cameraProperties = ObjectToTrack.CameraProperties;
		Rectangle box = cameraProperties.Box;
		box.X += _trackBounds.Width / 2.0;
		box.Y += _trackBounds.Height / 2.0;
		// Position of the object relative to the track bounds. The camera moves when the object leaves the box.
		Vector2 relativePosition = ObjectToTrack.Position;
		relativePosition.X -= _trackBounds.X;
		relativePosition.Y -= _trackBounds.Y;
		if (cameraProperties.Delay.X == 0)
		{
			if (relativePosition.X < box.X)
			{
				_velocity.X = relativePosition.X - box.X;
			}
			else if (relativePosition.X > box.Right)
			{
				_velocity.X = relativePosition.X - box.Right;
			}
			_velocity.X = MathX.Clamp(_velocity.X, cameraProperties.MaxVelocity.X);
		}
		else
		{
			cameraProperties.Delay = new Vector2i(cameraProperties.Delay.X - 1, cameraProperties.Delay.Y);
		}
		if (cameraProperties.Delay.Y == 0)
		{
			if (relativePosition.Y < box.Y)
			{
				_velocity.Y = relativePosition.Y - box.Y;
			}
			else if (relativePosition.Y > box.Bottom)
			{
				_velocity.Y = relativePosition.Y - box.Bottom;
			}
			_velocity.Y = MathX.Clamp(_velocity.Y, cameraProperties.MaxVelocity.Y);
		}
		else
		{
			cameraProperties.Delay = new Vector2i(cameraProperties.Delay.X, cameraProperties.Delay.Y - 1);
		}
		Vector2 maxVelocity = cameraProperties.MaxVelocity;
		maxVelocity.X = Math.Min(maxVelocity.X, MaxVelocity.X);
		maxVelocity.Y = Math.Min(maxVelocity.Y, MaxVelocity.Y);
		_trackBounds.X += _velocity.X;
		_trackBounds.Y += _velocity.Y;
		if (cameraProperties.MaxVelocity.X == 0.0)
		{
			_bounds.X = ObjectToTrack.Position.X - _trackBounds.Width / 2.0;
		}
		else
		{
			_bounds.X = MathX.GoTowards(_bounds.X, _trackBounds.X + cameraProperties.Offset.X, maxVelocity.X);
		}
		if (cameraProperties.MaxVelocity.Y == 0.0)
		{
			_bounds.Y = ObjectToTrack.Position.Y - _trackBounds.Height / 2.0;
		}
		else
		{
			_bounds.Y = MathX.GoTowards(_bounds.Y, _trackBounds.Y + cameraProperties.Offset.Y, maxVelocity.Y);
		}
		ApplyBounds();
	}

	public virtual void Draw(Renderer renderer)
	{
		DrawBounds(renderer, ScreenBounds, Bounds);
		if (ShowDebugInformation)
		{
			DrawSafeAreaGuides(renderer);
			if (ObjectToTrack != null && !SpyMode)
			{
				DrawTracking(renderer);
			}
			else
			{
				DrawCrossHair(renderer);
			}
			DrawPosition(renderer);
		}
		if (SpyMode)
		{
			DrawSpyCameraModeLabel(renderer);
		}
	}

	protected void DrawBounds(Renderer renderer, Rectangle screenBounds, Rectangle bounds)
	{
	}

	protected void DrawSafeAreaGuides(Renderer renderer)
	{
		I2dRenderer i2dRenderer = renderer.Get2dRenderer();
		double margin = ScreenBounds.Width * SafeAreaMarginRatio;
		double cornerLength = Math.Min(ScreenBounds.Width, ScreenBounds.Height) / 3.0;
		Rectangle rectangle = new Rectangle(ScreenBounds.X + margin, ScreenBounds.Y + margin, ScreenBounds.Width - margin * 2.0, ScreenBounds.Height - margin * 2.0);
		i2dRenderer.RenderLine(Colours.White, new Vector2(rectangle.X, rectangle.Y), new Vector2(rectangle.X + cornerLength, rectangle.Y), 1.0);
		i2dRenderer.RenderLine(Colours.White, new Vector2(rectangle.X, rectangle.Y), new Vector2(rectangle.X, rectangle.Y + cornerLength), 1.0);
		i2dRenderer.RenderLine(Colours.White, new Vector2(rectangle.Right, rectangle.Y), new Vector2(rectangle.Right - cornerLength, rectangle.Y), 1.0);
		i2dRenderer.RenderLine(Colours.White, new Vector2(rectangle.Right, rectangle.Y), new Vector2(rectangle.Right, rectangle.Y + cornerLength), 1.0);
		i2dRenderer.RenderLine(Colours.White, new Vector2(rectangle.X, rectangle.Bottom), new Vector2(rectangle.X, rectangle.Bottom - cornerLength), 1.0);
		i2dRenderer.RenderLine(Colours.White, new Vector2(rectangle.X, rectangle.Bottom), new Vector2(rectangle.X + cornerLength, rectangle.Bottom), 1.0);
		i2dRenderer.RenderLine(Colours.White, new Vector2(rectangle.Right, rectangle.Bottom), new Vector2(rectangle.Right - cornerLength, rectangle.Bottom), 1.0);
		i2dRenderer.RenderLine(Colours.White, new Vector2(rectangle.Right, rectangle.Bottom), new Vector2(rectangle.Right, rectangle.Bottom - cornerLength), 1.0);
	}

	protected void DrawCrossHair(Renderer renderer)
	{
		I2dRenderer i2dRenderer = renderer.Get2dRenderer();
		double halfLength = Math.Min(ScreenBounds.Width, ScreenBounds.Height) / 3.0;
		i2dRenderer.RenderLine(Colours.White, new Vector2(ScreenBounds.CentreX, ScreenBounds.CentreY - halfLength), new Vector2(ScreenBounds.CentreX, ScreenBounds.CentreY + halfLength), 1.0);
		i2dRenderer.RenderLine(Colours.White, new Vector2(ScreenBounds.CentreX - halfLength, ScreenBounds.CentreY), new Vector2(ScreenBounds.CentreX + halfLength, ScreenBounds.CentreY), 1.0);
	}

	protected void DrawTracking(Renderer renderer)
	{
		if (ObjectToTrack != null)
		{
			Rectangle box = ObjectToTrack.CameraProperties.Box;
			box.X += _trackBounds.X - _bounds.X + _trackBounds.Width / 2.0;
			box.Y += _trackBounds.Y - _bounds.Y + _trackBounds.Height / 2.0;
			Vector2 screenPosition = ObjectToTrack.Position;
			screenPosition.X -= _bounds.X;
			screenPosition.Y -= _bounds.Y;
			I2dRenderer i2dRenderer = renderer.Get2dRenderer();
			i2dRenderer.RenderRectangle(Colours.White, box, 1.0);
			i2dRenderer.RenderLine(Colours.White, new Vector2(screenPosition.X, screenPosition.Y - TrackingMarkerHalfLength), new Vector2(screenPosition.X, screenPosition.Y + TrackingMarkerHalfLength), 1.0);
			i2dRenderer.RenderLine(Colours.White, new Vector2(screenPosition.X - TrackingMarkerHalfLength, screenPosition.Y), new Vector2(screenPosition.X + TrackingMarkerHalfLength, screenPosition.Y), 1.0);
		}
	}

	protected void DrawPosition(Renderer renderer)
	{
		_level.DebugContext.DrawText(renderer, $"{_bounds.Left:0.0}, {_bounds.Top:0.0}", FontAlignment.Left, ScreenBounds.Left + 8.0, ScreenBounds.Top + 8.0, 0.75, 0);
		_level.DebugContext.DrawText(renderer, $"{_bounds.Right:0.0}, {_bounds.Bottom:0.0}", FontAlignment.Right | FontAlignment.Bottom, ScreenBounds.Right - 8.0, ScreenBounds.Bottom - 8.0, 0.75, 0);
	}

	protected void DrawSpyCameraModeLabel(Renderer renderer)
	{
		_level.DebugContext.DrawText(renderer, "SPY CAMERA", FontAlignment.Right, ScreenBounds.Right - 8.0, ScreenBounds.Top + 8.0, 0.75, 0);
	}
}
