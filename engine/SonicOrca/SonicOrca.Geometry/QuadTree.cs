#define TRACE
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SonicOrca.Geometry;

public class QuadTree<T> : ICollection<T>, IEnumerable<T>, IEnumerable where T : IBounds
{
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
				int num = _items.Count;
				Node[] children = _children;
				foreach (Node node in children)
				{
					num += node.Count;
				}
				return num;
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
				Node[] children = _children;
				foreach (Node node in children)
				{
					foreach (T item2 in node.Items)
					{
						yield return item2;
					}
				}
			}
		}

		public Node(QuadTree<T> quadTree, Rectanglei bounds)
		{
			_quadTree = quadTree;
			_bounds = bounds;
		}

		public bool Add(T item)
		{
			if (!_bounds.Contains(item.Bounds))
			{
				return false;
			}
			if (_children.Length == 0)
			{
				if (_items.Count <= _quadTree._subdivideTargetCount)
				{
					_items.Add(item);
					return true;
				}
				ReStructure();
			}
			Node[] children = _children;
			foreach (Node node in children)
			{
				if (node._bounds.Contains(item.Bounds))
				{
					Trace.Indent();
					bool result = node.Add(item);
					Trace.Unindent();
					return result;
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
			Node[] children = _children;
			for (int i = 0; i < children.Length; i++)
			{
				if (children[i].Remove(item))
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
				_ = item.Bounds;
				if (queryBounds.IntersectsWith(item.Bounds))
				{
					yield return item;
				}
			}
			Node[] children = _children;
			foreach (Node child in children)
			{
				if (child._items.Count == 0 && child._children.Length == 0)
				{
					continue;
				}
				if (child._bounds.Contains(queryBounds))
				{
					foreach (T item2 in child.Query(queryBounds))
					{
						yield return item2;
					}
					break;
				}
				if (queryBounds.Contains(child._bounds))
				{
					foreach (T item3 in child.Items)
					{
						yield return item3;
					}
				}
				else
				{
					if (!child._bounds.IntersectsWith(queryBounds))
					{
						continue;
					}
					foreach (T item4 in child.Query(queryBounds))
					{
						yield return item4;
					}
				}
			}
		}

		private void ReStructure()
		{
			T[] array = _items.ToArray();
			_items.Clear();
			CreateChildren();
			T[] array2 = array;
			foreach (T item in array2)
			{
				Add(item);
			}
		}

		private void CreateChildren()
		{
			if (_bounds.Area > _quadTree._minNodeSize)
			{
				int num = _bounds.Width / 2;
				int num2 = _bounds.Height / 2;
				_children = new Node[4];
				_children[0] = new Node(_quadTree, new Rectanglei(_bounds.X, _bounds.Y, num, num2));
				_children[1] = new Node(_quadTree, new Rectanglei(_bounds.X, _bounds.Y + num2, num, num2));
				_children[2] = new Node(_quadTree, new Rectanglei(_bounds.X + num, _bounds.Y, num, num2));
				_children[3] = new Node(_quadTree, new Rectanglei(_bounds.X + num, _bounds.Y + num2, num, num2));
			}
		}

		public int GetDepth()
		{
			if (_children.Length == 0)
			{
				return 1;
			}
			return _children.Select((Node x) => x.GetDepth()).Max();
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

	public QuadTree(IEnumerable<T> items, int minNodeSize = 4096, int subdivideTargetCount = 4)
	{
		_minNodeSize = minNodeSize;
		_subdivideTargetCount = subdivideTargetCount;
		T[] array = items.ToArray();
		if (array.Length == 0)
		{
			_bounds = new Rectanglei(0, 0, minNodeSize, minNodeSize);
			_root = new Node(this, _bounds);
			return;
		}
		int num = array.Min((T i) => i.Bounds.Left);
		int num2 = array.Min((T i) => i.Bounds.Top);
		int num3 = array.Max((T i) => i.Bounds.Right);
		int num4 = array.Max((T i) => i.Bounds.Bottom);
		_bounds = new Rectanglei(num, num2, num3 - num, num4 - num2);
		_root = new Node(this, _bounds);
		Trace.WriteLine("QuadTree bounds: " + _bounds);
		T[] array2 = array;
		foreach (T item in array2)
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
		T[] array = items.ToArray();
		T[] array2;
		if (array.Any((T x) => !_bounds.Contains(x.Bounds)))
		{
			Trace.WriteLine("Resizing quad tree");
			array2 = array;
			for (int num = 0; num < array2.Length; num++)
			{
				T val = array2[num];
				_bounds = _bounds.Union(val.Bounds);
			}
			T[] array3 = _root.Items.ToArray();
			_root = new Node(this, _bounds);
			array2 = array3;
			foreach (T item in array2)
			{
				_root.Add(item);
			}
		}
		array2 = array;
		foreach (T item2 in array2)
		{
			Add(item2);
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
			T[] array = _root.Items.ToArray();
			_root = new Node(this, _bounds);
			T[] array2 = array;
			foreach (T item2 in array2)
			{
				_root.Add(item2);
			}
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
