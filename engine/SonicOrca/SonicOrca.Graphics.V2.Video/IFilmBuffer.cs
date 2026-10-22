using System;
using SonicOrca.Resources;

namespace SonicOrca.Graphics.V2.Video;

public interface IFilmBuffer : IDisposable, ILoadedResource
{
	int Width { get; }

	int Height { get; }

	double CurrentTime { get; }

	double Duration { get; }

	void Decode();

	byte[] GetArgbData();
}
