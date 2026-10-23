#define TRACE
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SonicOrca.Geometry;

namespace SonicOrca.Core.Collision;

/// <summary>
/// Keeps the landscape collision vectors of a level in a quad tree and connects vectors that share end points.
/// </summary>
public class CollisionTable
{
	private readonly Level _level;

	private QuadTree<CollisionVector> _newCollisionQuadTree;

	public QuadTree<CollisionVector> InternalTree => _newCollisionQuadTree;

	public int Count => _newCollisionQuadTree.Count;

	public CollisionTable(Level level)
	{
		_level = level;
	}

	public void Initialise(LevelMap map)
	{
		Trace.WriteLine("Initialising collision table");
		IEnumerable<CollisionVector> collisionVectors = map.CollisionVectors;
		_newCollisionQuadTree = new QuadTree<CollisionVector>(collisionVectors);
		foreach (CollisionVector vector in collisionVectors)
		{
			vector.UpdateDerrivedFields();
		}
		UpdateAllConnections();
	}

	public IEnumerable<CollisionVector> GetVectorConnections(CollisionVector v)
	{
		HashSet<CollisionVector> results = new HashSet<CollisionVector>();
		int numPaths = _level.Map.CollisionPathLayers.Count;
		for (int i = 0; i < numPaths; i++)
		{
			CollisionVector connectionA = v.GetConnectionA(i);
			CollisionVector connectionB = v.GetConnectionB(i);
			if (connectionA != null && results.Add(connectionA))
			{
				yield return connectionA;
			}
			if (connectionB != null && results.Add(connectionB))
			{
				yield return connectionB;
			}
		}
	}

	/// <summary>
	/// Updates the connections of the given vectors, of the vectors that connect to them, and of the
	/// landscape vectors that share an end point with them.
	/// </summary>
	public void UpdateConnectionsFast(IEnumerable<CollisionVector> vectors)
	{
		Queue<CollisionVector> queue = new Queue<CollisionVector>(vectors);
		HashSet<CollisionVector> visited = new HashSet<CollisionVector>();
		while (queue.Count > 0)
		{
			CollisionVector current = queue.Dequeue();
			if (!visited.Add(current))
			{
				continue;
			}
			foreach (CollisionVector connection in GetVectorConnections(current))
			{
				queue.Enqueue(connection);
			}
			Rectanglei searchBounds = current.Bounds.Inflate(new Vector2i(8, 8));
			foreach (CollisionVector candidate in GetPossibleCollisionIntersections(searchBounds, landscape: true, objects: false))
			{
				if (candidate.AbsoluteA == current.AbsoluteA || candidate.AbsoluteA == current.AbsoluteB || candidate.AbsoluteB == current.AbsoluteA || candidate.AbsoluteB == current.AbsoluteB)
				{
					queue.Enqueue(candidate);
				}
			}
		}
		UpdateConnections(visited.ToArray());
	}

	public void UpdateAllConnections()
	{
		UpdateConnections(_level.Map.CollisionVectors);
	}

	public void UpdateConnections(IEnumerable<CollisionVector> vectors)
	{
		int pathCount = _level.Map.CollisionPathLayers.Count;
		foreach (CollisionVector vector in vectors)
		{
			vector.ResetConnections();
		}
		foreach (CollisionVector vector in vectors)
		{
			foreach (CollisionVector other in vectors)
			{
				if (vector != other)
				{
					ConnectIfJoined(vector, other, pathCount);
				}
			}
		}
	}

	public void UpdateConnectionsForVector(CollisionVector v1)
	{
		IList<CollisionVector> collisionVectors = _level.Map.CollisionVectors;
		int pathCount = _level.Map.CollisionPathLayers.Count;
		foreach (CollisionVector other in collisionVectors)
		{
			if (v1 != other)
			{
				ConnectIfJoined(v1, other, pathCount);
			}
		}
	}

	/// <summary>
	/// Connects <paramref name="vector"/> and <paramref name="other"/> on each path that they use,
	/// when the start of one vector is at the end of the other vector.
	/// </summary>
	private static void ConnectIfJoined(CollisionVector vector, CollisionVector other, int pathCount)
	{
		if (vector.AbsoluteA == other.AbsoluteB && CollisionVector.TestConnection(vector, other))
		{
			for (int path = 0; path < pathCount; path++)
			{
				if (other.HasPath(path))
				{
					vector.SetConnectionA(path, other);
				}
				if (vector.HasPath(path))
				{
					other.SetConnectionB(path, vector);
				}
			}
		}
		if (vector.AbsoluteB == other.AbsoluteA && CollisionVector.TestConnection(vector, other))
		{
			for (int path = 0; path < pathCount; path++)
			{
				if (other.HasPath(path))
				{
					vector.SetConnectionB(path, other);
				}
				if (vector.HasPath(path))
				{
					other.SetConnectionA(path, vector);
				}
			}
		}
	}

	public IEnumerable<CollisionVector> GetPossibleCollisionIntersections(Rectanglei bounds, bool landscape = true, bool objects = true)
	{
		if (objects)
		{
			foreach (CollisionVector vector in _level.ObjectManager.ActiveObjects.SelectMany(x => x.CollisionVectors))
			{
				yield return vector;
			}
		}
		if (!landscape)
		{
			yield break;
		}
		foreach (CollisionVector vector in _newCollisionQuadTree.Query(bounds))
		{
			yield return vector;
		}
	}

	public override string ToString()
	{
		return $"{_newCollisionQuadTree.Count} collision vectors";
	}
}
