#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SDL2;
using SonicOrca.Audio;

namespace SonicOrca.SDL2;

public class SDL2AudioContext : AudioContext, IDisposable
{
	private readonly SDL2Platform _platform;

	private readonly Lockable<List<ISampleProvider>> _registeredSampleProviders = new Lockable<List<ISampleProvider>>(new List<ISampleProvider>());

	private uint _outputDevice;

	private SDL.SDL_AudioSpec _outputAudioSpec;

	public SDL2AudioContext(SDL2Platform platform)
	{
		_platform = platform;
		Trace.WriteLine("Initialising SDL2 audio");
		if (SDL.SDL_InitSubSystem(16u) != 0)
		{
			throw SDL2Exception.FromError("Unable to initialise an audio driver.");
		}
		SDL.SDL_AudioSpec desired = new SDL.SDL_AudioSpec
		{
			channels = 2,
			format = 32784,
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
		Trace.WriteLine("Quitting SDL2 video");
		SDL.SDL_QuitSubSystem(16u);
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
		byte[] array = new byte[length];
		if (Mixer != null)
		{
			ISampleProvider[] channels;
			lock (_registeredSampleProviders.Sync)
			{
				channels = _registeredSampleProviders.Instance.ToArray();
			}
			Mixer.Mix(array, 0, length, channels);
		}
		Marshal.Copy(array, 0, stream, length);
	}
}
