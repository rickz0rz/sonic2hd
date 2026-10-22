#define TRACE
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SonicOrca.Geometry;

namespace SonicOrca.Core.Collision;

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
		foreach (CollisionVector item in collisionVectors)
		{
			item.UpdateDerrivedFields();
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

	public void UpdateConnectionsFast(IEnumerable<CollisionVector> vectors)
	{
		_ = _level.Map.CollisionPathLayers.Count;
		Queue<CollisionVector> queue = new Queue<CollisionVector>(vectors);
		HashSet<CollisionVector> hashSet = new HashSet<CollisionVector>();
		while (queue.Count > 0)
		{
			CollisionVector collisionVector = queue.Dequeue();
			if (!hashSet.Add(collisionVector))
			{
				continue;
			}
			foreach (CollisionVector vectorConnection in GetVectorConnections(collisionVector))
			{
				queue.Enqueue(vectorConnection);
			}
			Rectanglei bounds = collisionVector.Bounds.Inflate(new Vector2i(8, 8));
			foreach (CollisionVector possibleCollisionIntersection in GetPossibleCollisionIntersections(bounds, landscape: true, objects: false))
			{
				if (possibleCollisionIntersection.AbsoluteA == collisionVector.AbsoluteA || possibleCollisionIntersection.AbsoluteA == collisionVector.AbsoluteB || possibleCollisionIntersection.AbsoluteB == collisionVector.AbsoluteA || possibleCollisionIntersection.AbsoluteB == collisionVector.AbsoluteB)
				{
					queue.Enqueue(possibleCollisionIntersection);
				}
			}
		}
		UpdateConnections(hashSet.ToArray());
	}

	public void UpdateAllConnections()
	{
		UpdateConnections(_level.Map.CollisionVectors);
	}

	public void UpdateConnections(IEnumerable<CollisionVector> vectors)
	{
		int count = _level.Map.CollisionPathLayers.Count;
		foreach (CollisionVector vector in vectors)
		{
			vector.ResetConnections();
		}
		foreach (CollisionVector vector2 in vectors)
		{
			foreach (CollisionVector vector3 in vectors)
			{
				if (vector2 == vector3)
				{
					continue;
				}
				if (vector2.AbsoluteA == vector3.AbsoluteB && CollisionVector.TestConnection(vector2, vector3))
				{
					for (int i = 0; i < count; i++)
					{
						if (vector3.HasPath(i))
						{
							vector2.SetConnectionA(i, vector3);
						}
						if (vector2.HasPath(i))
						{
							vector3.SetConnectionB(i, vector2);
						}
					}
				}
				if (!(vector2.AbsoluteB == vector3.AbsoluteA) || !CollisionVector.TestConnection(vector2, vector3))
				{
					continue;
				}
				for (int j = 0; j < count; j++)
				{
					if (vector3.HasPath(j))
					{
						vector2.SetConnectionB(j, vector3);
					}
					if (vector2.HasPath(j))
					{
						vector3.SetConnectionA(j, vector2);
					}
				}
			}
		}
	}

	public void UpdateConnectionsForVector(CollisionVector v1)
	{
		IList<CollisionVector> collisionVectors = _level.Map.CollisionVectors;
		int count = _level.Map.CollisionPathLayers.Count;
		foreach (CollisionVector item in collisionVectors)
		{
			if (v1 == item)
			{
				continue;
			}
			if (v1.AbsoluteA == item.AbsoluteB && CollisionVector.TestConnection(v1, item))
			{
				for (int i = 0; i < count; i++)
				{
					if (item.HasPath(i))
					{
						v1.SetConnectionA(i, item);
					}
					if (v1.HasPath(i))
					{
						item.SetConnectionB(i, v1);
					}
				}
			}
			if (!(v1.AbsoluteB == item.AbsoluteA) || !CollisionVector.TestConnection(v1, item))
			{
				continue;
			}
			for (int j = 0; j < count; j++)
			{
				if (item.HasPath(j))
				{
					v1.SetConnectionB(j, item);
				}
				if (v1.HasPath(j))
				{
					item.SetConnectionA(j, v1);
				}
			}
		}
	}

	public IEnumerable<CollisionVector> GetPossibleCollisionIntersections(Rectanglei bounds, bool landscape = true, bool objects = true)
	{
		if (objects)
		{
			foreach (CollisionVector item in _level.ObjectManager.ActiveObjects.SelectMany((ActiveObject x) => x.CollisionVectors))
			{
				yield return item;
			}
		}
		if (!landscape)
		{
			yield break;
		}
		foreach (CollisionVector item2 in _newCollisionQuadTree.Query(bounds))
		{
			yield return item2;
		}
	}

	public override string ToString()
	{
		return $"{_newCollisionQuadTree.Count} collision vectors";
	}
}
