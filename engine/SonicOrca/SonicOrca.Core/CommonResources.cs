#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using SonicOrca.Resources;

namespace SonicOrca.Core;

/// <summary>
/// Loads the resources that levels share. Each level scheme adds its own resources to the resources that all schemes use.
/// </summary>
public class CommonResources : IDisposable
{
	/// <summary>
	/// One resource path, with the scheme and the key that find it.
	/// </summary>
	private class CommonResourceEntry
	{
		private readonly string _scheme;

		private readonly string _key;

		private readonly string _path;

		public string Scheme => _scheme;

		public string Key => _key;

		public string Path => _path;

		public CommonResourceEntry(string scheme, string key, string path)
		{
			_scheme = scheme.ToLower();
			_key = key.ToLower();
			_path = path.ToUpper();
		}

		public override string ToString()
		{
			return $"{_scheme}.{_key} = {_path}";
		}
	}

	private const string EntriesResourcePath = "SONICORCA/LEVELS/COMMONRESOURCES";

	private readonly Dictionary<string, IEnumerable<CommonResourceEntry>> _entries = new Dictionary<string, IEnumerable<CommonResourceEntry>>();

	private readonly SonicOrcaGameContext _gameContext;

	private ResourceSession _resourceSession;

	private string _loadedScheme;

	public string LoadedScheme => _loadedScheme;

	public CommonResources(SonicOrcaGameContext gameContext)
	{
		_gameContext = gameContext;
	}

	public void Dispose()
	{
		if (_resourceSession != null)
		{
			_resourceSession.Dispose();
		}
	}

	public async Task LoadEntriesAsync(CancellationToken ct = default(CancellationToken))
	{
		ResourceTree resourceTree = _gameContext.ResourceTree;
		using ResourceSession resourceSession = new ResourceSession(resourceTree);
		resourceSession.PushDependency(EntriesResourcePath);
		await resourceSession.LoadAsync(ct);
		XmlNode resourcesNode = _gameContext.ResourceTree.GetLoadedResource<XmlLoadedResource>(EntriesResourcePath).XmlDocument.SelectSingleNode("resources");
		foreach (IGrouping<string, CommonResourceEntry> schemeEntries in from x in ParseEntriesFromXmlNode(resourcesNode)
																		 group x by x.Scheme)
		{
			_entries[schemeEntries.Key] = schemeEntries.ToArray();
		}
	}

	private IEnumerable<CommonResourceEntry> ParseEntriesFromXmlNode(XmlNode resourcesNode)
	{
		foreach (XmlNode resourceNode in resourcesNode.SelectNodes("resource"))
		{
			// A resource without a scheme is common to all schemes.
			string scheme = string.Empty;
			XmlAttribute schemeAttribute = resourceNode.Attributes["scheme"];
			if (schemeAttribute != null)
			{
				scheme = schemeAttribute.Value;
			}
			string key = resourceNode.Attributes["key"].Value;
			string path = resourceNode.Attributes["path"].Value;
			yield return new CommonResourceEntry(scheme, key, path);
		}
	}

	public Task LoadSchemeAsync(LevelScheme scheme, CancellationToken ct = default(CancellationToken))
	{
		return LoadSchemeAsync(scheme.ToString().ToLower(), ct);
	}

	public async Task LoadSchemeAsync(string scheme, CancellationToken ct = default(CancellationToken))
	{
		if (!(_loadedScheme == scheme))
		{
			if (_resourceSession != null)
			{
				Trace.WriteLine("Unloading scheme " + _loadedScheme);
				_resourceSession.Unload();
			}
			Trace.WriteLine("Loading scheme " + scheme);
			ResourceSession newResourceSession = new ResourceSession(_gameContext.ResourceTree);
			newResourceSession.PushDependencies(GetResourcePaths(scheme));
			await newResourceSession.LoadAsync(ct);
			_resourceSession = newResourceSession;
			_loadedScheme = scheme;
		}
	}

	public void UnloadScheme()
	{
		if (_resourceSession != null)
		{
			Trace.WriteLine("Unloading scheme " + _loadedScheme);
			_resourceSession.Unload();
			_resourceSession = null;
			_loadedScheme = null;
		}
	}

	private IEnumerable<string> GetResourcePaths(string scheme)
	{
		if (_entries.TryGetValue(string.Empty, out IEnumerable<CommonResourceEntry> entries))
		{
			foreach (CommonResourceEntry entry in entries)
			{
				yield return entry.Path;
			}
		}
		if (!_entries.TryGetValue(scheme, out entries))
		{
			yield break;
		}
		foreach (CommonResourceEntry entry in entries)
		{
			yield return entry.Path;
		}
	}

	public string GetResourcePath(string key)
	{
		return GetResourcePath(_loadedScheme, key);
	}

	public string GetResourcePath(string scheme, string key)
	{
		// Look in the scheme first, then in the resources that are common to all schemes.
		CommonResourceEntry entry;
		if (_entries.TryGetValue(scheme, out IEnumerable<CommonResourceEntry> entries) && (entry = entries.FirstOrDefault(x => x.Key == key)) != null)
		{
			return entry.Path;
		}
		if (_entries.TryGetValue(string.Empty, out entries) && (entry = entries.FirstOrDefault(x => x.Key == key)) != null)
		{
			return entry.Path;
		}
		throw new ResourceException(key + " not found in the level common resources.");
	}
}
