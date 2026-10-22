using System;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca.Core;

public class Camera
{
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
		_trackBounds = new Rectangle(0.0, 0.0, 1920.0, 1080.0);
		_bounds = new Rectangle(0.0, 0.0, 1920.0, 1080.0);
		ScreenBounds = new Rectangle(0.0, 0.0, 1920.0, 1080.0);
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
			Vector2 vector = ObjectToTrack.Position;
			_trackBounds.X = vector.X - _trackBounds.Width / 2.0;
			_trackBounds.Y = vector.Y - _trackBounds.Height / 2.0;
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
		double x = Bounds.X;
		double y = Bounds.Y;
		Rectanglei rectanglei = ((Limits == Rectanglei.Empty) ? _level.Bounds : Limits);
		_bounds.X = Math.Max(Bounds.X, rectanglei.X);
		_bounds.Y = Math.Max(Bounds.Y, rectanglei.Y);
		_bounds.X = Math.Min(Bounds.Right, rectanglei.Right) - Bounds.Width;
		_bounds.Y = Math.Min(Bounds.Bottom, rectanglei.Bottom) - Bounds.Height;
		Vector2 velocity = Velocity;
		if (Bounds.X != x)
		{
			velocity.X = 0.0;
		}
		if (Bounds.Y != y)
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
			Acceleration = new Vector2(x * 0.5, y * 0.5);
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
		Vector2 vector = ObjectToTrack.Position;
		vector.X -= _trackBounds.X;
		vector.Y -= _trackBounds.Y;
		if (cameraProperties.Delay.X == 0)
		{
			if (vector.X < box.X)
			{
				_velocity.X = vector.X - box.X;
			}
			else if (vector.X > box.Right)
			{
				_velocity.X = vector.X - box.Right;
			}
			_velocity.X = MathX.Clamp(_velocity.X, cameraProperties.MaxVelocity.X);
		}
		else
		{
			cameraProperties.Delay = new Vector2i(cameraProperties.Delay.X - 1, cameraProperties.Delay.Y);
		}
		if (cameraProperties.Delay.Y == 0)
		{
			if (vector.Y < box.Y)
			{
				_velocity.Y = vector.Y - box.Y;
			}
			else if (vector.Y > box.Bottom)
			{
				_velocity.Y = vector.Y - box.Bottom;
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
			_bounds.X = (double)ObjectToTrack.Position.X - _trackBounds.Width / 2.0;
		}
		else
		{
			_bounds.X = MathX.GoTowards(_bounds.X, _trackBounds.X + cameraProperties.Offset.X, maxVelocity.X);
		}
		if (cameraProperties.MaxVelocity.Y == 0.0)
		{
			_bounds.Y = (double)ObjectToTrack.Position.Y - _trackBounds.Height / 2.0;
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
		double num = ScreenBounds.Width * 0.019999999552965164;
		double num2 = Math.Min(ScreenBounds.Width, ScreenBounds.Height) / 3.0;
		Rectangle rectangle = new Rectangle(ScreenBounds.X + num, ScreenBounds.Y + num, ScreenBounds.Width - num * 2.0, ScreenBounds.Height - num * 2.0);
		i2dRenderer.RenderLine(Colours.White, new Vector2(rectangle.X, rectangle.Y), new Vector2(rectangle.X + num2, rectangle.Y), 1.0);
		i2dRenderer.RenderLine(Colours.White, new Vector2(rectangle.X, rectangle.Y), new Vector2(rectangle.X, rectangle.Y + num2), 1.0);
		i2dRenderer.RenderLine(Colours.White, new Vector2(rectangle.Right, rectangle.Y), new Vector2(rectangle.Right - num2, rectangle.Y), 1.0);
		i2dRenderer.RenderLine(Colours.White, new Vector2(rectangle.Right, rectangle.Y), new Vector2(rectangle.Right, rectangle.Y + num2), 1.0);
		i2dRenderer.RenderLine(Colours.White, new Vector2(rectangle.X, rectangle.Bottom), new Vector2(rectangle.X, rectangle.Bottom - num2), 1.0);
		i2dRenderer.RenderLine(Colours.White, new Vector2(rectangle.X, rectangle.Bottom), new Vector2(rectangle.X + num2, rectangle.Bottom), 1.0);
		i2dRenderer.RenderLine(Colours.White, new Vector2(rectangle.Right, rectangle.Bottom), new Vector2(rectangle.Right - num2, rectangle.Bottom), 1.0);
		i2dRenderer.RenderLine(Colours.White, new Vector2(rectangle.Right, rectangle.Bottom), new Vector2(rectangle.Right, rectangle.Bottom - num2), 1.0);
	}

	protected void DrawCrossHair(Renderer renderer)
	{
		I2dRenderer i2dRenderer = renderer.Get2dRenderer();
		double num = Math.Min(ScreenBounds.Width, ScreenBounds.Height) / 3.0;
		i2dRenderer.RenderLine(Colours.White, new Vector2(ScreenBounds.CentreX, ScreenBounds.CentreY - num), new Vector2(ScreenBounds.CentreX, ScreenBounds.CentreY + num), 1.0);
		i2dRenderer.RenderLine(Colours.White, new Vector2(ScreenBounds.CentreX - num, ScreenBounds.CentreY), new Vector2(ScreenBounds.CentreX + num, ScreenBounds.CentreY), 1.0);
	}

	protected void DrawTracking(Renderer renderer)
	{
		if (ObjectToTrack != null)
		{
			Rectangle box = ObjectToTrack.CameraProperties.Box;
			box.X += _trackBounds.X - _bounds.X + _trackBounds.Width / 2.0;
			box.Y += _trackBounds.Y - _bounds.Y + _trackBounds.Height / 2.0;
			Vector2 vector = ObjectToTrack.Position;
			vector.X -= _bounds.X;
			vector.Y -= _bounds.Y;
			I2dRenderer i2dRenderer = renderer.Get2dRenderer();
			i2dRenderer.RenderRectangle(Colours.White, box, 1.0);
			i2dRenderer.RenderLine(Colours.White, new Vector2(vector.X, vector.Y - 16.0), new Vector2(vector.X, vector.Y + 16.0), 1.0);
			i2dRenderer.RenderLine(Colours.White, new Vector2(vector.X - 16.0, vector.Y), new Vector2(vector.X + 16.0, vector.Y), 1.0);
		}
	}

	protected void DrawPosition(Renderer renderer)
	{
		_level.DebugContext.DrawText(renderer, $"{_bounds.Left:0.0}, {_bounds.Top:0.0}", FontAlignment.Left, ScreenBounds.Left + 8.0, ScreenBounds.Top + 8.0, 0.75, 0);
		_level.DebugContext.DrawText(renderer, $"{_bounds.Right:0.0}, {_bounds.Bottom:0.0}", (FontAlignment)10, ScreenBounds.Right - 8.0, ScreenBounds.Bottom - 8.0, 0.75, 0);
	}

	protected void DrawSpyCameraModeLabel(Renderer renderer)
	{
		_level.DebugContext.DrawText(renderer, "SPY CAMERA", FontAlignment.Right, ScreenBounds.Right - 8.0, ScreenBounds.Top + 8.0, 0.75, 0);
	}
}
