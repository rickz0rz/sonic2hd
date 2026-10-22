using System;
using System.Threading;
using System.Threading.Tasks;
using SonicOrca.Resources;

namespace SonicOrca.Core;

public class LevelLoader
{
	private readonly SonicOrcaGameContext _gameContext;

	private Task _loadTask;

	private ResourceSession _resourceSession;

	private Area _area;

	private readonly Level _level;

	private bool _loadingArea;

	private bool _loadingLevel;

	private bool _levelLoaded;

	public Area Area => _area;

	public Level Level => _level;

	public bool HasLoadedArea
	{
		get
		{
			if (_loadingArea)
			{
				if (_loadTask.IsCompleted)
				{
					if (_loadTask.IsFaulted)
					{
						throw _loadTask.Exception.InnerException;
					}
					_loadingArea = false;
					return false;
				}
				return false;
			}
			return _area != null;
		}
	}

	public bool HasLoadedLevel
	{
		get
		{
			if (_loadingLevel)
			{
				if (_loadTask.IsCompleted)
				{
					if (_loadTask.IsFaulted)
					{
						throw _loadTask.Exception.InnerException;
					}
					_loadingLevel = false;
					return false;
				}
				return false;
			}
			return _levelLoaded;
		}
	}

	public LevelLoader(SonicOrcaGameContext gameContext)
	{
		_gameContext = gameContext;
		_resourceSession = new ResourceSession(gameContext.ResourceTree);
		_level = new Level(gameContext);
	}

	public void LoadArea(LevelPrepareSettings prepareSettings)
	{
		if (_area != null)
		{
			throw new InvalidOperationException("Area is already loaded.");
		}
		if (_loadingArea)
		{
			throw new InvalidOperationException("Area is already loading.");
		}
		_loadTask = LoadAreaAsync(prepareSettings);
		_loadingArea = true;
	}

	public void LoadLevel()
	{
		if (_area == null)
		{
			throw new InvalidOperationException("Area is not loaded.");
		}
		if (_loadingArea)
		{
			throw new InvalidOperationException("Area is currently loading.");
		}
		if (_loadingLevel)
		{
			throw new InvalidOperationException("Level is already loading.");
		}
		_loadTask = LoadLevelAsync();
		_loadingLevel = true;
	}

	public void UnloadLevel()
	{
		if (!_levelLoaded)
		{
			throw new InvalidOperationException("Level is not loaded.");
		}
		if (_loadingArea || _loadingLevel)
		{
			throw new InvalidOperationException("Can't unload while an area or level is being loaded.");
		}
		_level.Stop();
		_level.Unload();
		_levelLoaded = false;
	}

	public void UnloadArea()
	{
		if (_area == null)
		{
			throw new InvalidOperationException("Area is not loaded.");
		}
		if (_levelLoaded)
		{
			throw new InvalidOperationException("Can't unload area until level is unloaded.");
		}
		if (_loadingArea || _loadingLevel)
		{
			throw new InvalidOperationException("Can't unload while an area or level is being loaded.");
		}
		_area.Dispose();
		_area = null;
		_resourceSession.Unload();
	}

	private async Task LoadAreaAsync(LevelPrepareSettings prepareSettings, CancellationToken ct = default(CancellationToken))
	{
		_resourceSession.PushDependency(prepareSettings.AreaResourceKey);
		await _resourceSession.LoadAsync(ct);
		_area = _gameContext.ResourceTree.GetLoadedResource<Area>(prepareSettings.AreaResourceKey);
		_level.PrepareSettings = prepareSettings;
		await _level.LoadCommonAsync(ct);
	}

	private async Task LoadLevelAsync(CancellationToken ct = default(CancellationToken))
	{
		_area.Prepare(_level, _level.PrepareSettings);
		await _level.LoadAsync(_area, ct);
		_levelLoaded = true;
	}
}
