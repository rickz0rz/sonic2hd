namespace SonicOrca.Core.Collision;

/// <summary>
/// The data that the engine sends to an object when a collision occurs.
/// </summary>
public class CollisionEvent
{
	private readonly ActiveObject _object;

	private CollisionInfo _collisionInfo;

	private readonly int _id;

	public ActiveObject ActiveObject => _object;

	public CollisionInfo CollisionInfo
	{
		get
		{
			return _collisionInfo;
		}
		set
		{
			_collisionInfo = value;
		}
	}

	public int Id => _id;

	public bool IgnoreCollision { get; set; }

	public bool MaintainVelocity { get; set; }

	public CollisionEvent(ActiveObject obj, int id)
	{
		_object = obj;
		_id = id;
	}

	public CollisionEvent(ActiveObject obj, CollisionInfo collisionInfo)
	{
		_object = obj;
		_collisionInfo = collisionInfo;
		_id = _collisionInfo.Vector.Id;
	}

	public override string ToString()
	{
		return $"CollisionEvent ({_object} for #{_id})";
	}
}
