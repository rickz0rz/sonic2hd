using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SonicOrca.Resources;

public sealed class Resource : IDisposable
{
	private readonly Lockable<List<Resource>> _childResources = new Lockable<List<Resource>>(new List<Resource>());

	private readonly SemaphoreSlim _loadSemaphore = new SemaphoreSlim(1, 1);

	private readonly string _fullKeyPath;

	private readonly ResourceTypeIdentifier _identifier;

	private ResourceSource _source;

	public static int LoadedResourceCount { get; private set; }

	public string FullKeyPath => _fullKeyPath;

	public ResourceTypeIdentifier Identifier => _identifier;

	/// <summary>
	/// The bytes of the resource. A change has no effect on a loaded resource. It applies on the next load, for
	/// example after the level editor saves a level.
	/// </summary>
	public ResourceSource Source
	{
		get => _source;
		set => _source = value;
	}

	public ILoadedResource LoadedResource { get; private set; }

	public int DependencyCount { get; private set; }

	public Resource(string fullKeyPath, ResourceTypeIdentifier identifier, ResourceSource source)
	{
		_fullKeyPath = fullKeyPath;
		_identifier = identifier;
		_source = source;
	}

	public void Dispose()
	{
		_loadSemaphore.Dispose();
	}

	public override string ToString()
	{
		return $"{Identifier} Loaded = {LoadedResource != null} DependencyCount = {DependencyCount}";
	}

	public async Task LoadAsync(ResourceSession session, CancellationToken ct = default(CancellationToken), int level = 0)
	{
		// Waits asynchronously: the lock is held across awaits, and blocking a thread on it deadlocks
		// single-threaded runtimes (WebAssembly). Released in finally so a failed load cannot lock the resource.
		await _loadSemaphore.WaitAsync(ct);
		try
		{
			if (LoadedResource == null)
			{
				ResourceType resourceType = ResourceType.FromIdentifier(Identifier);
				if (resourceType == null)
				{
					throw new ResourceException($"No registered resource type, {Identifier}");
				}
				using (Stream uncompressedStream = Source.ReadUncompressed())
				{
					ResourceLoadArgs loadArguments = new ResourceLoadArgs(session.ResourceTree, this, uncompressedStream);
					LoadedResource = await resourceType.LoadAsync(loadArguments, ct);
					if (Environment.Is64BitProcess)
					{
						await Task.WhenAll(loadArguments.Dependencies.Select((Resource x) => LoadChildResourceAsync(session, x, level, ct)).ToArray());
					}
					else
					{
						// One at a time in 32-bit processes (including WebAssembly), as the original did with Wait()
						foreach (Resource dependency in loadArguments.Dependencies)
						{
							await LoadChildResourceAsync(session, dependency, level, ct);
						}
					}
				}
				DependencyCount = 1;
				LoadedResourceCount++;
			}
			else
			{
				DependencyCount++;
			}
		}
		finally
		{
			_loadSemaphore.Release();
		}
	}

	private async Task LoadChildResourceAsync(ResourceSession session, Resource resource, int level, CancellationToken ct = default(CancellationToken))
	{
		bool resourceWasLoaded = resource.LoadedResource != null;
		await resource.LoadAsync(session, ct, level + 1);
		if (!resourceWasLoaded)
		{
			resource.LoadedResource.OnLoaded();
		}
		lock (_childResources.Sync)
		{
			_childResources.Instance.Add(resource);
		}
	}

	public void Unload(int level = 0)
	{
		_loadSemaphore.Wait();
		if (LoadedResource != null)
		{
			if (DependencyCount == 1)
			{
				foreach (Resource item in _childResources.Instance)
				{
					item.Unload(level + 1);
				}
				_childResources.Instance.Clear();
				LoadedResource.Dispose();
				LoadedResource = null;
				DependencyCount = 0;
				LoadedResourceCount--;
			}
			else
			{
				DependencyCount--;
			}
		}
		_loadSemaphore.Release();
	}

	public void Export(string path)
	{
		byte[] array = new byte[1024];
		using FileStream fileStream = new FileStream(path, FileMode.Create, FileAccess.Write);
		using Stream stream = Source.ReadUncompressed();
		int num;
		do
		{
			num = stream.Read(array, 0, array.Length);
			fileStream.Write(array, 0, num);
		}
		while (num != 0);
	}

	public static Resource FromFile(string path)
	{
		path = Path.GetFullPath(path);
		string fullKeyPath = "$" + path;
		ResourceTypeIdentifier identifier = ResourceType.FromPath(path).Identifier;
		ResourceSource source = new FileResourceSource(path, 0L, new FileInfo(path).Length);
		return new Resource(fullKeyPath, identifier, source);
	}

	public static Resource FromFile(string path, string fullResourceKeyPath)
	{
		return new Resource(fullResourceKeyPath, ResourceType.FromPath(path).Identifier, new FileResourceSource(path, 0L, new FileInfo(path).Length));
	}

	public string GetAbsolutePath(string keyPath)
	{
		if (keyPath.StartsWith("/"))
		{
			if (_fullKeyPath.StartsWith("$"))
			{
				return ResourcePath.GetRelativeFileResourceFromAbsolute(_fullKeyPath, keyPath);
			}
			keyPath = _fullKeyPath + keyPath;
			Stack<string> stack = new Stack<string>();
			int num;
			while ((num = keyPath.IndexOf('/')) != -1)
			{
				if (num == 0)
				{
					keyPath = keyPath.Substring(1);
					if (stack.Count > 0)
					{
						stack.Pop();
					}
				}
				else
				{
					stack.Push(keyPath.Substring(0, num));
					keyPath = keyPath.Substring(num + 1);
				}
			}
			stack.Push(keyPath);
			keyPath = string.Join("/", stack.ToArray().Reverse());
		}
		return keyPath;
	}
}
