using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SonicOrca.Resources;

public class ResourceTree : IEnumerable<ResourceTree.Node>, IEnumerable
{
	public class Node : IEnumerable<Node>, IEnumerable
	{
		private readonly Node _parent;

		private readonly Dictionary<string, Node> _children = new Dictionary<string, Node>();

		private readonly string _key;

		public Node Parent => _parent;

		public ICollection<Node> Children => _children.Values;

		public string Key => _key;

		public Resource Resource { get; set; }

		public Node this[string key]
		{
			get
			{
				if (!_children.ContainsKey(key))
				{
					return null;
				}
				return _children[key];
			}
		}

		public Node()
		{
		}

		public Node(Node parent, string key, Resource resource = null)
		{
			_parent = parent;
			_key = key;
			Resource = resource;
		}

		public Node Add(string key, Resource resource)
		{
			Node orAdd = GetOrAdd(key);
			orAdd.Resource = resource;
			return orAdd;
		}

		public Node GetOrAdd(string key)
		{
			if (_children.ContainsKey(key))
			{
				return _children[key];
			}
			Node node = new Node(this, key);
			_children.Add(key, node);
			return node;
		}

		public void Remove(string key)
		{
			_children.Remove(key);
		}

		public IEnumerator<Node> GetEnumerator()
		{
			return _children.Values.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public override string ToString()
		{
			return _key;
		}
	}

	private readonly Node _head = new Node();

	public Node Head => _head;

	public Node this[string fullKeyPath]
	{
		get
		{
			if (string.IsNullOrEmpty(fullKeyPath))
			{
				return null;
			}
			Node node = _head;
			string[] array = fullKeyPath.Split('/');
			foreach (string key in array)
			{
				if ((node = node[key]) == null)
				{
					break;
				}
			}
			return node;
		}
	}

	public Node GetOrAdd(string fullKeyPath)
	{
		Node node = _head;
		string[] array = fullKeyPath.Split('/');
		foreach (string key in array)
		{
			node = node.GetOrAdd(key);
		}
		return node;
	}

	public Node SetOrAdd(string fullKeyPath, Resource resource)
	{
		Node orAdd = GetOrAdd(fullKeyPath);
		orAdd.Resource = resource;
		return orAdd;
	}

	public Node SetOrAddFromFile(string resourcePath)
	{
		return SetOrAdd("$" + resourcePath, Resource.FromFile(resourcePath));
	}

	public Node SetOrAddFromFile(string fullKeyPath, string resourcePath)
	{
		return SetOrAdd(fullKeyPath, Resource.FromFile(resourcePath, fullKeyPath));
	}

	public ILoadedResource GetLoadedResource(string fullKeyPath)
	{
		Node node = this[fullKeyPath] ?? throw new ResourceException(fullKeyPath + " doesn't exist.");
		if (node.Resource == null)
		{
			throw new ResourceException(fullKeyPath + " has no registered resource.");
		}
		return node.Resource.LoadedResource ?? throw new ResourceException(fullKeyPath + " has not been loaded.");
	}

	public T GetLoadedResource<T>(string fullKeyPath) where T : ILoadedResource
	{
		ILoadedResource loadedResource = GetLoadedResource(fullKeyPath);
		if (!(loadedResource is T))
		{
			throw new ResourceException(fullKeyPath + " doesn't have the correct resource type.");
		}
		return (T)loadedResource;
	}

	public T GetLoadedResource<T>(ILoadedResource parentLoadedResource, string fullKeyPath) where T : ILoadedResource
	{
		fullKeyPath = parentLoadedResource.Resource.GetAbsolutePath(fullKeyPath);
		return GetLoadedResource<T>(fullKeyPath);
	}

	public bool TryGetLoadedResource(string fullKeyPath, out ILoadedResource loadedResource)
	{
		loadedResource = null;
		Node node = this[fullKeyPath];
		if (node == null || node.Resource == null)
		{
			return false;
		}
		loadedResource = node.Resource.LoadedResource;
		return loadedResource != null;
	}

	public bool TryGetLoadedResource<T>(string fullKeyPath, out T loadedResource) where T : class, ILoadedResource
	{
		if (TryGetLoadedResource(fullKeyPath, out var loadedResource2))
		{
			loadedResource = loadedResource2 as T;
			return loadedResource != null;
		}
		loadedResource = null;
		return false;
	}

	public void RemoveEmptyNodes()
	{
		RemoveEmptyNodes(_head);
	}

	private void RemoveEmptyNodes(Node node)
	{
		Node[] array = node.ToArray();
		foreach (Node node2 in array)
		{
			RemoveEmptyNodes(node2);
		}
		if (node.Parent != null && node.Resource == null && node.Children.Count == 0)
		{
			node.Parent.Remove(node.Key);
		}
	}

	public void MergeWith(ResourceTree tree)
	{
		foreach (KeyValuePair<string, Node> item in tree.GetNodeListing())
		{
			Node orAdd = GetOrAdd(item.Key);
			if (item.Value.Resource != null)
			{
				orAdd.Resource = item.Value.Resource;
			}
		}
	}

	public IDictionary<string, Node> GetNodeListing()
	{
		Dictionary<string, Node> dictionary = new Dictionary<string, Node>();
		Stack<Tuple<string, Node>> stack = new Stack<Tuple<string, Node>>();
		stack.Push(new Tuple<string, Node>(string.Empty, _head));
		do
		{
			Tuple<string, Node> tuple = stack.Pop();
			string text = (string.IsNullOrEmpty(tuple.Item1) ? string.Empty : (tuple.Item1 + "/")) + tuple.Item2.Key;
			dictionary[text] = tuple.Item2;
			foreach (Node item in tuple.Item2)
			{
				stack.Push(new Tuple<string, Node>(text, item));
			}
		}
		while (stack.Count > 0);
		return dictionary;
	}

	public IDictionary<string, Resource> GetResourceListing()
	{
		Dictionary<string, Resource> dictionary = new Dictionary<string, Resource>();
		foreach (KeyValuePair<string, Node> item in from x in GetNodeListing()
													where x.Value.Resource != null
													select x)
		{
			dictionary[item.Key] = item.Value.Resource;
		}
		return dictionary;
	}

	public IEnumerator<Node> GetEnumerator()
	{
		Stack<Node> stack = new Stack<Node>();
		stack.Push(_head);
		do
		{
			Node topNode = stack.Pop();
			yield return topNode;
			foreach (Node item in topNode)
			{
				stack.Push(item);
			}
		}
		while (stack.Count > 0);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
