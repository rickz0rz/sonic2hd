using System;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Lighting;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Resources;

namespace SonicOrca.Core;

/// <summary>
/// The base class for an object that is alive in a level. It has a position, collision data and a life cycle.
/// </summary>
public abstract class ActiveObject : IActiveObject
{
	private const int DefaultPriority = 256;

	private Vector2i _position;

	private Vector2 _positionPrecise;

	private Vector2i _lastPosition;

	private Vector2 _lastPositionPrecise;

	public string Name { get; set; }

	public Guid Uid { get; set; }

	public Level Level { get; private set; }

	public ObjectType Type { get; private set; }

	public ObjectEntry Entry { get; private set; }

	public LevelLayer Layer { get; set; }

	public int Priority { get; set; }

	public ActiveObject ParentObject { get; set; }

	public CollisionVector[] CollisionVectors { get; set; }

	public CollisionRectangle[] CollisionRectangles { get; set; }

	public CameraProperties CameraProperties { get; set; }

	public bool LockLifetime { get; set; }

	public bool Finished { get; private set; }

	public Rectanglei DesignBounds { get; protected set; }

	public float Brightness { get; set; }

	public ShadowInfo ShadowInfo { get; set; }

	public Vector2i Position
	{
		get
		{
			return _position;
		}
		set
		{
			_position = value;
			_positionPrecise = value;
		}
	}

	public Vector2 PositionPrecise
	{
		get
		{
			return _positionPrecise;
		}
		set
		{
			_position = (Vector2i)value;
			_positionPrecise = value;
		}
	}

	public Vector2i LastPosition => _lastPosition;

	public Vector2 LastPositionPrecise => _lastPositionPrecise;

	public Rectanglei LifetimeArea
	{
		get
		{
			Vector2 lifeRadius = Type.GetLifeRadius((IActiveObject)Entry.State);
			Vector2 topLeft = new Vector2(Position.X - lifeRadius.X, Position.Y - lifeRadius.Y);
			return new Rectangle(topLeft.X, topLeft.Y, lifeRadius.X * 2.0, lifeRadius.Y * 2.0);
		}
	}

	public bool IsSubObject => ParentObject != null;

	public ResourceTree ResourceTree => Level.GameContext.ResourceTree;

	public ActiveObject()
	{
		CollisionVectors = new CollisionVector[0];
		CollisionRectangles = new CollisionRectangle[0];
		DesignBounds = new Rectangle(-32.0, -32.0, 64.0, 64.0);
	}

	public void Initialise(ObjectEntry entry)
	{
		Entry = entry;
		Level = entry.Level;
		Type = entry.Type;
		Layer = Level.Map.Layers[entry.Layer];
		Position = entry.Position;
		Name = entry.Name;
		Uid = entry.Uid;
		Priority = DefaultPriority;
		CameraProperties = new CameraProperties();
		CameraProperties.Box = new Rectangle(-64.0, -192.0, 64.0, 256.0);
		CameraProperties.Delay = new Vector2i(0, 0);
		CameraProperties.MaxVelocity = new Vector2(64.0, 64.0);
		CameraProperties.Offset = new Vector2(0.0, 0.0);
		StateVariableAttribute.SetObjectState(this, Entry.State);
		ShadowInfo = new ShadowInfo
		{
			OcclusionSize = new Vector2i(16),
			MaxShadowOffset = new Vector2i(int.MaxValue)
		};
	}

	public void Finish()
	{
		Finished = true;
	}

	public void FinishForever()
	{
		Finish();
		Entry.FinishForever();
	}

	public void Start()
	{
		OnStart();
		RegisterCollisionUpdate();
	}

	public void UpdateEditor()
	{
		OnUpdateEditor();
	}

	public void UpdatePrepare()
	{
		_lastPosition = _position;
		_lastPositionPrecise = _positionPrecise;
		OnUpdatePrepare();
	}

	public void Update()
	{
		OnUpdate();
	}

	public void UpdateCollision()
	{
		OnUpdateCollision();
	}

	public void Collision(CollisionEvent e)
	{
		OnCollision(e);
	}

	public void Animate()
	{
		OnAnimate();
	}

	public void Draw(Renderer renderer, Viewport viewport, LayerViewOptions viewOptions)
	{
		// In the shadow pass, draw the object again at its shadow offset. Draw nothing when there is no offset.
		Vector2i shadowOffset = default;
		if (viewOptions.Shadows)
		{
			IShadowInfo shadowInfo = ShadowInfo;
			if (shadowInfo == null)
			{
				return;
			}
			shadowOffset = Level.LightingManager.GetShadowOffset(Position, shadowInfo);
			if (shadowOffset == default(Vector2i))
			{
				return;
			}
		}
		I2dRenderer i2dRenderer = renderer.Get2dRenderer();
		using (i2dRenderer.BeginMatixState())
		{
			i2dRenderer.ModelMatrix *= Matrix4.CreateTranslation((_position.X - viewport.Bounds.X + shadowOffset.X) * viewport.Scale.X, (_position.Y - viewport.Bounds.Y + shadowOffset.Y) * viewport.Scale.Y);
			IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
			objectRenderer.ClipRectangle = viewport.Destination;
			objectRenderer.ModelMatrix = i2dRenderer.ModelMatrix;
			objectRenderer.SetDefault();
			objectRenderer.Shadow = viewOptions.Shadows;
			objectRenderer.Filter = viewOptions.Filter;
			objectRenderer.FilterAmount = viewOptions.FilterAmount;
			objectRenderer.Scale = viewport.Scale;
			if (viewOptions.ShowObjects)
			{
				OnDraw(renderer, viewOptions);
			}
		}
	}

	public void DrawCollision(Renderer renderer, Viewport viewport)
	{
		CollisionVector[] collisionVectors = CollisionVectors;
		for (int i = 0; i < collisionVectors.Length; i++)
		{
			collisionVectors[i].Draw(renderer, viewport);
		}
		CollisionRectangle[] collisionRectangles = CollisionRectangles;
		for (int i = 0; i < collisionRectangles.Length; i++)
		{
			collisionRectangles[i].Draw(renderer, viewport);
		}
	}

	public void Stop()
	{
		OnStop();
	}

	protected void RegisterCollisionUpdate()
	{
		CollisionVector[] collisionVectors = CollisionVectors;
		for (int i = 0; i < collisionVectors.Length; i++)
		{
			collisionVectors[i].UpdateDerrivedFields();
		}
		// Connect each pair of vectors that share an end point, on each collision path that the other vector is on.
		int pathCount = Level.Map.CollisionPathLayers.Count;
		collisionVectors = CollisionVectors;
		foreach (CollisionVector collisionVector in collisionVectors)
		{
			CollisionVector[] otherVectors = CollisionVectors;
			foreach (CollisionVector otherVector in otherVectors)
			{
				if (collisionVector == otherVector)
				{
					continue;
				}
				if (collisionVector.AbsoluteA == otherVector.AbsoluteB && CollisionVector.TestConnection(collisionVector, otherVector))
				{
					for (int path = 0; path < pathCount; path++)
					{
						if (otherVector.HasPath(path))
						{
							collisionVector.SetConnectionA(path, otherVector);
						}
						if (collisionVector.HasPath(path))
						{
							otherVector.SetConnectionB(path, collisionVector);
						}
					}
				}
				if (!(collisionVector.AbsoluteB == otherVector.AbsoluteA) || !CollisionVector.TestConnection(collisionVector, otherVector))
				{
					continue;
				}
				for (int path = 0; path < pathCount; path++)
				{
					if (otherVector.HasPath(path))
					{
						collisionVector.SetConnectionB(path, otherVector);
					}
					if (collisionVector.HasPath(path))
					{
						otherVector.SetConnectionA(path, collisionVector);
					}
				}
			}
		}
	}

	public void Move(int x, int y)
	{
		Position += new Vector2i(x, y);
	}

	public void Move(Vector2i offset)
	{
		Position += offset;
	}

	public void MovePrecise(double x, double y)
	{
		PositionPrecise += new Vector2(x, y);
	}

	public void MovePrecise(Vector2 offset)
	{
		PositionPrecise += offset;
	}

	protected virtual void OnStart()
	{
	}

	protected virtual void OnUpdateEditor()
	{
	}

	protected virtual void OnUpdatePrepare()
	{
	}

	protected virtual void OnUpdate()
	{
	}

	protected virtual void OnUpdateCollision()
	{
	}

	protected virtual void OnCollision(CollisionEvent e)
	{
	}

	protected virtual void OnAnimate()
	{
	}

	protected virtual void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
	}

	protected virtual void OnStop()
	{
	}
}
