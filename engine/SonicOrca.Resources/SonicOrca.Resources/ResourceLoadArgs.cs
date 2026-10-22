using System.Collections.Generic;
using System.IO;

namespace SonicOrca.Resources;

public sealed class ResourceLoadArgs
{
	private readonly ResourceTree _resourceTree;

	private readonly Resource _resource;

	private readonly Stream _inputStream;

	private readonly Lockable<List<Resource>> _dependencies = new Lockable<List<Resource>>(new List<Resource>());

	public ResourceTree ResourceTree => _resourceTree;

	public Resource Resource => _resource;

	public Stream InputStream => _inputStream;

	public IReadOnlyCollection<Resource> Dependencies => _dependencies.Instance;

	public ResourceLoadArgs(ResourceTree resourceTree, Resource resource, Stream inputStream)
	{
		_resourceTree = resourceTree;
		_resource = resource;
		_inputStream = inputStream;
	}

	public void PushDependencies(IEnumerable<string> fullKeyPaths)
	{
		foreach (string fullKeyPath in fullKeyPaths)
		{
			PushDependency(fullKeyPath);
		}
	}

	public void PushDependencies(params string[] fullKeyPaths)
	{
		foreach (string fullKeyPath in fullKeyPaths)
		{
			PushDependency(fullKeyPath);
		}
	}

	public void PushDependency(string fullKeyPath)
	{
		ResourceTree.Node node = _resourceTree[fullKeyPath];
		if (node == null)
		{
			if (!fullKeyPath.StartsWith("$"))
			{
				throw new ResourceException($"Resource node not found, {fullKeyPath}.");
			}
			node = _resourceTree.SetOrAddFromFile(fullKeyPath.Substring(1));
		}
		Resource resource = node.Resource;
		if (resource == null)
		{
			throw new ResourceException($"Resource node has no resource or has missing resource type loader, {fullKeyPath}.");
		}
		PushDependency(resource);
	}

	private void PushDependency(Resource resource)
	{
		lock (_dependencies.Sync)
		{
			_dependencies.Instance.Add(resource);
		}
	}

	public string GetAbsolutePath(string keyPath)
	{
		return _resource.GetAbsolutePath(keyPath);
	}
}
