using System;
using System.Linq;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Lighting;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Resources;

namespace SonicOrca.Core;

public abstract class ActiveObject : IActiveObject
{
	private Vector2i _position;

	private Vector2 _positionPrecise;

	private Vector2i _lastPosition;

	private Vector2 _lastPositionPrecise;

	private bool[] _activeLayers;

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
			Vector2 vector = new Vector2((double)Position.X - lifeRadius.X, (double)Position.Y - lifeRadius.Y);
			return new Rectangle(vector.X, vector.Y, lifeRadius.X * 2.0, lifeRadius.Y * 2.0);
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
		_activeLayers = (from x in Enumerable.Range(0, Level.Map.Layers.Count)
						 select true).ToArray();
		Priority = 256;
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
		Vector2i vector2i = default;
		if (viewOptions.Shadows)
		{
			IShadowInfo shadowInfo = ShadowInfo;
			if (shadowInfo == null)
			{
				return;
			}
			vector2i = Level.LightingManager.GetShadowOffset(Position, shadowInfo);
			if (vector2i == default(Vector2i))
			{
				return;
			}
		}
		I2dRenderer i2dRenderer = renderer.Get2dRenderer();
		using (i2dRenderer.BeginMatixState())
		{
			i2dRenderer.ModelMatrix *= Matrix4.CreateTranslation((double)(_position.X - viewport.Bounds.X + vector2i.X) * viewport.Scale.X, (double)(_position.Y - viewport.Bounds.Y + vector2i.Y) * viewport.Scale.Y);
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
		int count = Level.Map.CollisionPathLayers.Count;
		collisionVectors = CollisionVectors;
		foreach (CollisionVector collisionVector in collisionVectors)
		{
			CollisionVector[] collisionVectors2 = CollisionVectors;
			foreach (CollisionVector collisionVector2 in collisionVectors2)
			{
				if (collisionVector == collisionVector2)
				{
					continue;
				}
				if (collisionVector.AbsoluteA == collisionVector2.AbsoluteB && CollisionVector.TestConnection(collisionVector, collisionVector2))
				{
					for (int k = 0; k < count; k++)
					{
						if (collisionVector2.HasPath(k))
						{
							collisionVector.SetConnectionA(k, collisionVector2);
						}
						if (collisionVector.HasPath(k))
						{
							collisionVector2.SetConnectionB(k, collisionVector);
						}
					}
				}
				if (!(collisionVector.AbsoluteB == collisionVector2.AbsoluteA) || !CollisionVector.TestConnection(collisionVector, collisionVector2))
				{
					continue;
				}
				for (int l = 0; l < count; l++)
				{
					if (collisionVector2.HasPath(l))
					{
						collisionVector.SetConnectionB(l, collisionVector2);
					}
					if (collisionVector.HasPath(l))
					{
						collisionVector2.SetConnectionA(l, collisionVector);
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
