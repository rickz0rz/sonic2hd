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

public class CommonResources : IDisposable
{
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
		resourceSession.PushDependency("SONICORCA/LEVELS/COMMONRESOURCES");
		await resourceSession.LoadAsync(ct);
		XmlNode resourcesNode = _gameContext.ResourceTree.GetLoadedResource<XmlLoadedResource>("SONICORCA/LEVELS/COMMONRESOURCES").XmlDocument.SelectSingleNode("resources");
		foreach (IGrouping<string, CommonResourceEntry> item in from x in ParseEntriesFromXmlNode(resourcesNode)
			group x by x.Scheme)
		{
			_entries[item.Key] = item.ToArray();
		}
	}

	private IEnumerable<CommonResourceEntry> ParseEntriesFromXmlNode(XmlNode resourcesNode)
	{
		foreach (XmlNode item in resourcesNode.SelectNodes("resource"))
		{
			string scheme = string.Empty;
			XmlAttribute xmlAttribute = item.Attributes["scheme"];
			if (xmlAttribute != null)
			{
				scheme = xmlAttribute.Value;
			}
			string value = item.Attributes["key"].Value;
			string value2 = item.Attributes["path"].Value;
			yield return new CommonResourceEntry(scheme, value, value2);
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
		if (_entries.TryGetValue(string.Empty, out var value))
		{
			foreach (CommonResourceEntry item in value)
			{
				yield return item.Path;
			}
		}
		if (!_entries.TryGetValue(scheme, out value))
		{
			yield break;
		}
		foreach (CommonResourceEntry item2 in value)
		{
			yield return item2.Path;
		}
	}

	public string GetResourcePath(string key)
	{
		return GetResourcePath(_loadedScheme, key);
	}

	public string GetResourcePath(string scheme, string key)
	{
		CommonResourceEntry commonResourceEntry;
		if (_entries.TryGetValue(scheme, out var value) && (commonResourceEntry = value.FirstOrDefault((CommonResourceEntry x) => x.Key == key)) != null)
		{
			return commonResourceEntry.Path;
		}
		if (_entries.TryGetValue(string.Empty, out value) && (commonResourceEntry = value.FirstOrDefault((CommonResourceEntry x) => x.Key == key)) != null)
		{
			return commonResourceEntry.Path;
		}
		throw new ResourceException(key + " not found in the level common resources.");
	}
}
