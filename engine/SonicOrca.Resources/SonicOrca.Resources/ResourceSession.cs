using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SonicOrca.Resources;

public class ResourceSession : IDisposable
{
	private readonly ResourceTree _resourceTree;

	private readonly HashSet<Resource> _unloadedResources = new HashSet<Resource>();

	private readonly HashSet<Resource> _intermediateResources = new HashSet<Resource>();

	private readonly HashSet<Resource> _loadedResources = new HashSet<Resource>();

	private bool _disposed;

	private object _unloadedResourcesSync = new object();

	public ResourceTree ResourceTree => _resourceTree;

	public ResourceSession(ResourceTree tree)
	{
		_resourceTree = tree;
	}

	public void Dispose()
	{
		Unload();
		_disposed = true;
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
		Resource resource;
		if (node == null)
		{
			if (!fullKeyPath.StartsWith("$"))
			{
				throw new ResourceException($"Resource node not found, {fullKeyPath}.");
			}
			resource = Resource.FromFile(fullKeyPath.Substring(1));
		}
		else
		{
			resource = node.Resource;
		}
		if (resource == null)
		{
			throw new ResourceException($"Resource node has no resource or has missing resource type loader, {fullKeyPath}.");
		}
		PushDependency(resource);
	}

	public void PushDependency(Resource resource)
	{
		CheckDisposed();
		lock (_unloadedResourcesSync)
		{
			if (!_unloadedResources.Contains(resource) && !_intermediateResources.Contains(resource) && !_loadedResources.Contains(resource))
			{
				_unloadedResources.Add(resource);
			}
		}
	}

	public async Task LoadAsync(CancellationToken ct = default(CancellationToken), bool serial = false)
	{
		CheckDisposed();
		HashSet<Resource> localLoaded = new HashSet<Resource>();
		object localLoadedSync = new object();
		try
		{
			while (_unloadedResources.Count > 0)
			{
				_intermediateResources.Clear();
				_intermediateResources.UnionWith(_unloadedResources);
				_unloadedResources.Clear();
				if (serial)
				{
					foreach (Resource resource in _intermediateResources)
					{
						await resource.LoadAsync(this, ct);
						localLoaded.Add(resource);
					}
					continue;
				}
				await Task.WhenAll(_intermediateResources.Select((Resource resource2) => Task.Run(async () =>
				{
					ct.ThrowIfCancellationRequested();
					await resource2.LoadAsync(this, ct);
					lock (localLoadedSync)
					{
						localLoaded.Add(resource2);
					}
				})).ToArray());
			}
			foreach (Resource item in localLoaded.Where((Resource r) => r.DependencyCount == 1))
			{
				item.LoadedResource.OnLoaded();
			}
			_loadedResources.UnionWith(localLoaded);
		}
		catch (Exception)
		{
			foreach (Resource item2 in localLoaded)
			{
				item2.Unload();
			}
			throw;
		}
		finally
		{
			_intermediateResources.Clear();
		}
	}

	public void Unload()
	{
		CheckDisposed();
		foreach (Resource item in _loadedResources.Where((Resource r) => r.LoadedResource != null))
		{
			item.Unload();
		}
		_loadedResources.Clear();
	}

	private void CheckDisposed()
	{
		if (_disposed)
		{
			throw new ObjectDisposedException(typeof(ResourceSession).Name);
		}
	}
}
