using System;
using SonicOrca.Audio;
using SonicOrca.Graphics;
using SonicOrca.Input;

namespace SonicOrca;

public interface IPlatform : IDisposable
{
	WindowContext Window { get; }

	InputContext Input { get; }

	AudioContext Audio { get; }

	string GraphicsAPI { get; }

	string GraphicsVendor { get; }

	int TotalMemory { get; }

	void Initialise();

	Version GetOpenGLVersion();
}
