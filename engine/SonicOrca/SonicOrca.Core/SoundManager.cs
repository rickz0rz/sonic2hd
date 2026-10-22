using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Audio;
using SonicOrca.Geometry;
using SonicOrca.Resources;

namespace SonicOrca.Core;

public class SoundManager
{
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
			SampleInstance sampleInstance = _jingleInstance.FirstOrDefault((SampleInstance x) => x?.Playing ?? false);
			if (sampleInstance == null)
			{
				sampleInstance = _musicInstance;
			}
			return sampleInstance;
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
		_simpleInstances.RemoveAll((SampleInstance x) => !x.Playing);
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
			FadeOutMusic(120);
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
			SampleInstance sampleInstance = _jingleInstance[i];
			if (sampleInstance != null && sampleInstance.Playing)
			{
				sampleInstance.Stop();
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
			_musicFadeAmount = 1.0 / (double)fadeDuration;
		}
	}

	public void FadeOutMusic(int fadeDuration)
	{
		_musicFadeState = MusicFadeState.FadingOut;
		_musicFadeVolume = CurrentInstance.Volume;
		_musicFadeAmount = 1.0 / (double)fadeDuration;
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
		Resource resource = _gameContext.ResourceTree[resourceKey].Resource;
		SampleInfo sampleInfo = ((resource.Identifier == ResourceTypeIdentifier.SampleInfo) ? ((SampleInfo)resource.LoadedResource) : new SampleInfo((Sample)resource.LoadedResource));
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
		default:
			return;
		case JingleType.Life:
			MuteMusic();
			break;
		case JingleType.Invincibility:
			StopMusic();
			break;
		case JingleType.SpeedShoes:
			StopMusic();
			break;
		case JingleType.Super:
			StopMusic();
			break;
		case JingleType.Drowning:
			StopMusic();
			break;
		}
		for (int i = 0; i < NumJingleTypes; i++)
		{
			if (_jingleInstance[i] != null)
			{
				_jingleInstance[i].Volume = 0.0;
			}
		}
		if (_jingleInstance[(int)jingleType] == null)
		{
			_jingleInstance[(int)jingleType] = new SampleInstance(_gameContext, _jingleSampleInfo[(int)jingleType]);
		}
		else
		{
			_jingleOrder.Remove(jingleType);
		}
		_jingleOrder.Add(jingleType);
		_jingleInstance[(int)jingleType].Classification = SampleInstanceClassification.Music;
		_jingleInstance[(int)jingleType].Volume = 1.0;
		_jingleInstance[(int)jingleType].SeekToStart();
		_jingleInstance[(int)jingleType].Play();
	}

	public void StopJingle(JingleType jingleType)
	{
		if (_jingleInstance[(int)jingleType] == null)
		{
			return;
		}
		_jingleInstance[(int)jingleType].Stop();
		_jingleInstance[(int)jingleType].Dispose();
		_jingleInstance[(int)jingleType] = null;
		_jingleOrder.Remove(jingleType);
		if (_jingleOrder.Count == 0)
		{
			switch (jingleType)
			{
			case JingleType.Life:
				if (_musicFadeState != MusicFadeState.FadingOut)
				{
					FadeInMusic();
				}
				break;
			default:
				PlayMusic(continueFadeOut: true);
				break;
			case JingleType.Drowning:
				break;
			}
		}
		else
		{
			JingleType jingleType2 = _jingleOrder.Last();
			_jingleInstance[(int)jingleType2].Volume = 1.0;
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
		_sounds.RemoveAll((LevelSound s) => s.Finished);
	}
}
