using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Audio;
using SonicOrca.Geometry;
using SonicOrca.Resources;

namespace SonicOrca.Core;

/// <summary>
/// Plays the music, the jingles and the sounds of a level.
/// A jingle (for example the extra life or invincibility music) stops or mutes the level music while it plays.
/// </summary>
public class SoundManager
{
	/// <summary>
	/// The number of ticks that the current music fades out before the next music starts.
	/// </summary>
	private const int CrossFadeDuration = 120;

	private enum MusicFadeState
	{
		None,
		FadingOut,
		FadingIn
	}

	private readonly int NumJingleTypes = EnumHelpers.GetEnumCount(typeof(JingleType));

	private readonly SampleInfo[] _jingleSampleInfo = new SampleInfo[EnumHelpers.GetEnumCount(typeof(JingleType))];

	private readonly SampleInstance[] _jingleInstance = new SampleInstance[EnumHelpers.GetEnumCount(typeof(JingleType))];

	private readonly List<JingleType> _jingleOrder = new List<JingleType>();

	private readonly List<LevelSound> _sounds = new List<LevelSound>();

	private readonly List<SampleInstance> _simpleInstances = new List<SampleInstance>();

	private readonly SonicOrcaGameContext _gameContext;

	private readonly Level _level;

	private readonly ResourceTree _resourceTree;

	private SampleInfo _music;

	private SampleInfo _queuedMusic;

	private SampleInstance _musicInstance;

	private JingleType _pausedJingle;

	private MusicFadeState _musicFadeState;

	private double _musicFadeVolume;

	private double _musicFadeAmount;

	public SampleInstance MusicInstance => _musicInstance;

	public SampleInstance CurrentInstance
	{
		get
		{
			// A playing jingle has priority over the music.
			return _jingleInstance.FirstOrDefault(x => x?.Playing ?? false) ?? _musicInstance;
		}
	}

	public SoundManager(Level level)
	{
		_gameContext = level.GameContext;
		_level = level;
		_resourceTree = _gameContext.ResourceTree;
	}

	public void Update()
	{
		UpdateMusicFading();
		CheckFinishedJingles();
		UpdateSounds();
		_simpleInstances.RemoveAll(x => !x.Playing);
	}

	public void PlayMusic(string fullKeyPath)
	{
		if (!string.IsNullOrEmpty(fullKeyPath))
		{
			PlayMusic(_resourceTree.GetLoadedResource<SampleInfo>(fullKeyPath));
		}
	}

	public void PlayMusic(SampleInfo si)
	{
		SelectMusic(si);
		PlayMusic();
	}

	public void CrossFadeMusic(string fullKeyPath)
	{
		CrossFadeMusic(_resourceTree.GetLoadedResource<SampleInfo>(fullKeyPath));
	}

	public void CrossFadeMusic(SampleInfo si)
	{
		if (CurrentInstance != null)
		{
			_queuedMusic = si;
			FadeOutMusic(CrossFadeDuration);
		}
		else
		{
			SelectMusic(si);
			PlayMusic();
		}
	}

	private void SelectMusic(SampleInfo si)
	{
		if (_music != si)
		{
			if (_musicInstance != null)
			{
				_musicInstance.Stop();
				_musicInstance.Dispose();
			}
			_music = si;
			_musicInstance = new SampleInstance(_gameContext, si);
		}
	}

	public void UpdateMusicFading()
	{
		SampleInstance currentInstance = CurrentInstance;
		if (currentInstance == null)
		{
			return;
		}
		switch (_musicFadeState)
		{
			case MusicFadeState.FadingOut:
				_musicFadeVolume -= _musicFadeAmount;
				currentInstance.Volume = Math.Min(currentInstance.Volume, _musicFadeVolume);
				if (currentInstance.Volume <= 0.0)
				{
					currentInstance.Volume = 0.0;
					currentInstance.Stop();
					_musicFadeState = MusicFadeState.None;
					// Start the music that CrossFadeMusic queued.
					if (_queuedMusic != null)
					{
						SelectMusic(_queuedMusic);
						_queuedMusic = null;
						PlayMusic();
					}
				}
				break;
			case MusicFadeState.FadingIn:
				currentInstance.Volume += _musicFadeAmount;
				if (currentInstance.Volume >= 1.0)
				{
					currentInstance.Volume = 1.0;
					_musicFadeState = MusicFadeState.None;
				}
				break;
		}
	}

	public void PlayMusic(bool continueFadeOut = false)
	{
		if (_musicInstance != null)
		{
			StopAllJingles();
			_musicInstance.Volume = 1.0;
			if (continueFadeOut && _musicFadeState == MusicFadeState.FadingOut)
			{
				_musicInstance.Volume = _musicFadeVolume;
			}
			else
			{
				_musicFadeState = MusicFadeState.None;
			}
			_musicInstance.Classification = SampleInstanceClassification.Music;
			_musicInstance.SeekToStart();
			_musicInstance.Play();
		}
	}

	public void PauseAll()
	{
		_pausedJingle = _jingleOrder.LastOrDefault();
		for (int i = 0; i < NumJingleTypes; i++)
		{
			SampleInstance jingleInstance = _jingleInstance[i];
			if (jingleInstance != null && jingleInstance.Playing)
			{
				jingleInstance.Stop();
			}
		}
		if (_musicInstance != null)
		{
			_musicInstance.Stop();
		}
		foreach (LevelSound sound in _sounds)
		{
			sound.Pause();
		}
	}

	public void ResumeAll()
	{
		if (_pausedJingle != JingleType.None && _jingleInstance[(int)_pausedJingle] != null)
		{
			_jingleInstance[(int)_pausedJingle].Play();
		}
		else if (_musicInstance != null)
		{
			_musicInstance.Play();
		}
		_pausedJingle = JingleType.None;
		foreach (LevelSound sound in _sounds)
		{
			sound.Resume();
		}
	}

	public void FadeInMusic(int fadeDuration = 60)
	{
		if (_musicInstance != null)
		{
			_musicInstance.Volume = 0.0;
			_musicInstance.Play();
			_musicFadeState = MusicFadeState.FadingIn;
			_musicFadeAmount = 1.0 / fadeDuration;
		}
	}

	public void FadeOutMusic(int fadeDuration)
	{
		// A level without music (see AreaResourceType) has nothing to fade out
		if (CurrentInstance == null)
		{
			return;
		}
		_musicFadeState = MusicFadeState.FadingOut;
		_musicFadeVolume = CurrentInstance.Volume;
		_musicFadeAmount = 1.0 / fadeDuration;
	}

	public void MuteMusic()
	{
		if (_musicInstance != null)
		{
			_musicInstance.Volume = 0.0;
		}
	}

	public void StopMusic()
	{
		if (_musicInstance != null)
		{
			_musicInstance.Stop();
		}
	}

	public void CheckFinishedJingles()
	{
		for (int i = 0; i < NumJingleTypes; i++)
		{
			if (_jingleInstance[i] != null && !_jingleInstance[i].Playing)
			{
				StopJingle((JingleType)i);
			}
		}
	}

	public void SetJingle(JingleType jingleType, string resourceKey)
	{
		// The resource can be a sample info or a plain sample.
		Resource resource = _gameContext.ResourceTree[resourceKey].Resource;
		SampleInfo sampleInfo = (resource.Identifier == ResourceTypeIdentifier.SampleInfo) ? (SampleInfo)resource.LoadedResource : new SampleInfo((Sample)resource.LoadedResource);
		_jingleSampleInfo[(int)jingleType] = sampleInfo;
	}

	public void SetJingle(JingleType jingleType, SampleInfo si)
	{
		_jingleSampleInfo[(int)jingleType] = si;
	}

	public void PlayJingleOnce(Sample sample)
	{
		SampleInstance sampleInstance = new SampleInstance(_gameContext, sample);
		sampleInstance.Classification = SampleInstanceClassification.Music;
		sampleInstance.Play();
		_simpleInstances.Add(sampleInstance);
	}

	public void PlayJingle(JingleType jingleType)
	{
		if (_jingleSampleInfo[(int)jingleType] == null)
		{
			return;
		}
		switch (jingleType)
		{
			case JingleType.Life:
				// The extra life jingle only mutes the music, so the music continues after the jingle.
				MuteMusic();
				break;
			case JingleType.Invincibility:
			case JingleType.SpeedShoes:
			case JingleType.Super:
			case JingleType.Drowning:
				StopMusic();
				break;
			default:
				return;
		}

		// Silence the other jingles. The newest jingle is the last item in the jingle order.
		for (int i = 0; i < NumJingleTypes; i++)
		{
			if (_jingleInstance[i] != null)
			{
				_jingleInstance[i].Volume = 0.0;
			}
		}
		int index = (int)jingleType;
		if (_jingleInstance[index] == null)
		{
			_jingleInstance[index] = new SampleInstance(_gameContext, _jingleSampleInfo[index]);
		}
		else
		{
			_jingleOrder.Remove(jingleType);
		}
		_jingleOrder.Add(jingleType);
		_jingleInstance[index].Classification = SampleInstanceClassification.Music;
		_jingleInstance[index].Volume = 1.0;
		_jingleInstance[index].SeekToStart();
		_jingleInstance[index].Play();
	}

	public void StopJingle(JingleType jingleType)
	{
		int index = (int)jingleType;
		if (_jingleInstance[index] == null)
		{
			return;
		}
		_jingleInstance[index].Stop();
		_jingleInstance[index].Dispose();
		_jingleInstance[index] = null;
		_jingleOrder.Remove(jingleType);
		if (_jingleOrder.Count == 0)
		{
			// No jingle is left, so bring back the music.
			switch (jingleType)
			{
				case JingleType.Life:
					if (_musicFadeState != MusicFadeState.FadingOut)
					{
						FadeInMusic();
					}
					break;
				case JingleType.Drowning:
					break;
				default:
					PlayMusic(continueFadeOut: true);
					break;
			}
		}
		else
		{
			// Make the newest jingle that is left audible again.
			JingleType newestJingle = _jingleOrder.Last();
			_jingleInstance[(int)newestJingle].Volume = 1.0;
		}
	}

	public void StopAllJingles()
	{
		for (int i = 0; i < NumJingleTypes; i++)
		{
			if (_jingleInstance[i] != null && _jingleInstance[i].Playing)
			{
				StopJingle((JingleType)i);
			}
		}
	}

	public void StopAll()
	{
		StopMusic();
		StopAllJingles();
		foreach (LevelSound sound in _sounds)
		{
			sound.Dispose();
		}
		_sounds.Clear();
		foreach (SampleInstance simpleInstance in _simpleInstances)
		{
			simpleInstance.Dispose();
		}
	}

	public void PlaySound(IActiveObject activeObject, string resourceKey)
	{
		PlaySound(activeObject.Position, resourceKey);
	}

	public void PlaySound(Vector2i position, string resourceKey)
	{
		if (_resourceTree.TryGetLoadedResource(resourceKey, out Sample loadedResource))
		{
			PlaySound(position, loadedResource);
		}
	}

	public void PlaySound(Vector2i position, Sample sample)
	{
		LevelSound levelSound = new LevelSound(_level, sample, position);
		levelSound.Play();
		AddLevelSound(levelSound);
	}

	public void AddLevelSound(LevelSound levelSound)
	{
		_sounds.Add(levelSound);
	}

	public void PlaySound(string resourceKey)
	{
		if (_resourceTree.TryGetLoadedResource(resourceKey, out Sample loadedResource))
		{
			PlaySound(loadedResource);
		}
	}

	public void PlaySound(Sample sample)
	{
		SampleInstance sampleInstance = new SampleInstance(_gameContext, sample);
		sampleInstance.Play();
		_simpleInstances.Add(sampleInstance);
	}

	public void PlaySoundVisibleOnly(string resourceKey, Vector2i position)
	{
		if (_level.Camera.Bounds.Contains(position))
		{
			PlaySound(resourceKey);
		}
	}

	private void UpdateSounds()
	{
		foreach (LevelSound sound in _sounds)
		{
			sound.Update();
		}
		_sounds.RemoveAll(s => s.Finished);
	}
}
