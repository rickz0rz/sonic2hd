#define TRACE
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SonicOrca.Geometry;

/// <summary>
/// A collection that sorts items into a tree of rectangular areas, so that a query for an area is fast.
/// </summary>
/// <remarks>
/// When an item is outside the bounds of the tree, the tree grows to contain the item and builds all nodes again.
/// </remarks>
public class QuadTree<T> : ICollection<T>, IEnumerable<T>, IEnumerable where T : IBounds
{
	/// <summary>
	/// One area of the tree. A node keeps the items that do not fit fully in one of its four children.
	/// </summary>
	private class Node
	{
		private readonly QuadTree<T> _quadTree;

		private readonly Rectanglei _bounds;

		private readonly List<T> _items = new List<T>();

		private Node[] _children = new Node[0];

		public Rectanglei Bounds => _bounds;

		public int Count
		{
			get
			{
				int count = _items.Count;
				foreach (Node child in _children)
				{
					count += child.Count;
				}
				return count;
			}
		}

		public IEnumerable<T> Items
		{
			get
			{
				foreach (T item in _items)
				{
					yield return item;
				}
				foreach (Node child in _children)
				{
					foreach (T childItem in child.Items)
					{
						yield return childItem;
					}
				}
			}
		}

		public Node(QuadTree<T> quadTree, Rectanglei bounds)
		{
			_quadTree = quadTree;
			_bounds = bounds;
		}

		/// <returns><c>false</c> when the item is not fully inside the bounds of this node.</returns>
		public bool Add(T item)
		{
			if (!_bounds.Contains(item.Bounds))
			{
				return false;
			}
			if (_children.Length == 0)
			{
				// A leaf keeps items until it has more than the target count. Then it tries to subdivide.
				if (_items.Count <= _quadTree._subdivideTargetCount)
				{
					_items.Add(item);
					return true;
				}
				ReStructure();
			}
			foreach (Node child in _children)
			{
				if (child._bounds.Contains(item.Bounds))
				{
					Trace.Indent();
					bool added = child.Add(item);
					Trace.Unindent();
					return added;
				}
			}
			_items.Add(item);
			return true;
		}

		public bool Remove(T item)
		{
			if (_items.Remove(item))
			{
				return true;
			}
			foreach (Node child in _children)
			{
				if (child.Remove(item))
				{
					return true;
				}
			}
			return false;
		}

		public IEnumerable<T> Query(Rectanglei queryBounds)
		{
			foreach (T item in _items)
			{
				if (queryBounds.IntersectsWith(item.Bounds))
				{
					yield return item;
				}
			}
			foreach (Node child in _children)
			{
				if (child._items.Count == 0 && child._children.Length == 0)
				{
					continue;
				}
				if (child._bounds.Contains(queryBounds))
				{
					// The query is fully inside this child, so no other child can have a match.
					foreach (T childItem in child.Query(queryBounds))
					{
						yield return childItem;
					}
					break;
				}
				if (queryBounds.Contains(child._bounds))
				{
					// The child is fully inside the query, so all its items match.
					foreach (T childItem in child.Items)
					{
						yield return childItem;
					}
				}
				else if (child._bounds.IntersectsWith(queryBounds))
				{
					foreach (T childItem in child.Query(queryBounds))
					{
						yield return childItem;
					}
				}
			}
		}

		/// <summary>
		/// Creates the children and adds the items of this node again, so that they move into the children.
		/// </summary>
		private void ReStructure()
		{
			T[] items = _items.ToArray();
			_items.Clear();
			CreateChildren();
			foreach (T item in items)
			{
				Add(item);
			}
		}

		private void CreateChildren()
		{
			// Note: the minimum node size is compared with the area of the node, not with its width or height.
			if (_bounds.Area > _quadTree._minNodeSize)
			{
				int halfWidth = _bounds.Width / 2;
				int halfHeight = _bounds.Height / 2;
				_children = new Node[4];
				_children[0] = new Node(_quadTree, new Rectanglei(_bounds.X, _bounds.Y, halfWidth, halfHeight));
				_children[1] = new Node(_quadTree, new Rectanglei(_bounds.X, _bounds.Y + halfHeight, halfWidth, halfHeight));
				_children[2] = new Node(_quadTree, new Rectanglei(_bounds.X + halfWidth, _bounds.Y, halfWidth, halfHeight));
				_children[3] = new Node(_quadTree, new Rectanglei(_bounds.X + halfWidth, _bounds.Y + halfHeight, halfWidth, halfHeight));
			}
		}

		public int GetDepth()
		{
			if (_children.Length == 0)
			{
				return 1;
			}
			return _children.Select(x => x.GetDepth()).Max();
		}

		public override string ToString()
		{
			return $"{_items.Count} items, {_children.Length} nodes";
		}
	}

	private Node _root;

	private Rectanglei _bounds;

	private readonly int _minNodeSize = 4096;

	private readonly int _subdivideTargetCount = 4;

	public int Count => _root.Count;

	public bool IsReadOnly => false;

	public QuadTree(Rectanglei bounds, int minNodeSize = 4096, int subdivideTargetCount = 4)
	{
		_minNodeSize = minNodeSize;
		_subdivideTargetCount = subdivideTargetCount;
		_bounds = bounds;
		_root = new Node(this, _bounds);
	}

	/// <summary>
	/// Creates a tree with bounds that contain all the given items, and adds the items.
	/// </summary>
	public QuadTree(IEnumerable<T> items, int minNodeSize = 4096, int subdivideTargetCount = 4)
	{
		_minNodeSize = minNodeSize;
		_subdivideTargetCount = subdivideTargetCount;
		T[] itemArray = items.ToArray();
		if (itemArray.Length == 0)
		{
			_bounds = new Rectanglei(0, 0, minNodeSize, minNodeSize);
			_root = new Node(this, _bounds);
			return;
		}
		int left = itemArray.Min(i => i.Bounds.Left);
		int top = itemArray.Min(i => i.Bounds.Top);
		int right = itemArray.Max(i => i.Bounds.Right);
		int bottom = itemArray.Max(i => i.Bounds.Bottom);
		_bounds = new Rectanglei(left, top, right - left, bottom - top);
		_root = new Node(this, _bounds);
		Trace.WriteLine("QuadTree bounds: " + _bounds);
		foreach (T item in itemArray)
		{
			Add(item);
		}
	}

	public IEnumerable<T> Query(Rectanglei queryBounds)
	{
		return _root.Query(queryBounds);
	}

	public void AddRange(IEnumerable<T> items)
	{
		T[] newItems = items.ToArray();
		if (newItems.Any(x => !_bounds.Contains(x.Bounds)))
		{
			// Grow the bounds one time for all the new items, then build the tree again.
			Trace.WriteLine("Resizing quad tree");
			foreach (T item in newItems)
			{
				_bounds = _bounds.Union(item.Bounds);
			}
			RebuildRoot();
		}
		foreach (T item in newItems)
		{
			Add(item);
		}
	}

	public int GetDepth()
	{
		return _root.GetDepth();
	}

	public void Add(T item)
	{
		if (!_root.Add(item))
		{
			Trace.WriteLine("Resizing quad tree");
			_bounds = _bounds.Union(item.Bounds);
			RebuildRoot();
			_root.Add(item);
		}
	}

	/// <summary>
	/// Replaces the root with a new node for the current bounds, and adds all the existing items to it.
	/// </summary>
	private void RebuildRoot()
	{
		T[] existingItems = _root.Items.ToArray();
		_root = new Node(this, _bounds);
		foreach (T item in existingItems)
		{
			_root.Add(item);
		}
	}

	public void Clear()
	{
		_root = new Node(this, _bounds);
	}

	public bool Contains(T item)
	{
		return _root.Items.Contains(item);
	}

	public void CopyTo(T[] array, int arrayIndex)
	{
		_root.Items.ToArray().CopyTo(array, arrayIndex);
	}

	public bool Remove(T item)
	{
		return _root.Remove(item);
	}

	public IEnumerator<T> GetEnumerator()
	{
		return _root.Items.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return _root.Items.GetEnumerator();
	}
}
