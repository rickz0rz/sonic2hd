#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SDL2;
using SonicOrca.Audio;

namespace SonicOrca.SDL2;

/// <summary>
/// Audio output through SDL2: 44.1 kHz, stereo, signed 16-bit. SDL calls <see cref="ReadDataCallback"/> on its audio
/// thread, which mixes the registered sample providers.
/// </summary>
public class SDL2AudioContext : AudioContext, IDisposable
{

	private readonly Lockable<List<ISampleProvider>> _registeredSampleProviders = new Lockable<List<ISampleProvider>>(new List<ISampleProvider>());

	private uint _outputDevice;

	private SDL.SDL_AudioSpec _outputAudioSpec;

	public SDL2AudioContext(SDL2Platform platform)
	{
		Trace.WriteLine("Initialising SDL2 audio");
		if (SDL.SDL_InitSubSystem(SDL.SDL_INIT_AUDIO) != 0)
		{
			throw SDL2Exception.FromError("Unable to initialise an audio driver.");
		}
		SDL.SDL_AudioSpec desired = new SDL.SDL_AudioSpec
		{
			channels = 2,
			format = SDL.AUDIO_S16LSB,
			freq = 44100,
			samples = 2048,
			callback = ReadDataCallback
		};
		_outputDevice = SDL.SDL_OpenAudioDevice(null, 0, ref desired, out _outputAudioSpec, 0);
		if (_outputDevice == 0)
		{
			throw SDL2Exception.FromError("Unable to create output buffer.");
		}
		SDL.SDL_PauseAudioDevice(_outputDevice, 0);
	}

	public void Dispose()
	{
		SDL.SDL_CloseAudioDevice(_outputDevice);
		Trace.WriteLine("Quitting SDL2 audio");
		SDL.SDL_QuitSubSystem(SDL.SDL_INIT_AUDIO);
	}

	public override void RegisterSampleProvider(ISampleProvider sampleProvider)
	{
		lock (_registeredSampleProviders.Sync)
		{
			_registeredSampleProviders.Instance.Add(sampleProvider);
		}
	}

	public override void UnregisterSampleProvider(ISampleProvider sampleProvider)
	{
		lock (_registeredSampleProviders.Sync)
		{
			_registeredSampleProviders.Instance.Remove(sampleProvider);
		}
	}

	private void ReadDataCallback(IntPtr userdata, IntPtr stream, int length)
	{
		byte[] buffer = new byte[length];
		if (Mixer != null)
		{
			ISampleProvider[] providers;
			lock (_registeredSampleProviders.Sync)
			{
				providers = _registeredSampleProviders.Instance.ToArray();
			}
			Mixer.Mix(buffer, 0, length, providers);
		}
		Marshal.Copy(buffer, 0, stream, length);
	}
}
