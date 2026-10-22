using System;
using SonicOrca.Resources;

namespace SonicOrca.Graphics;

public interface ITexture : IDisposable, ILoadedResource
{
	int Width { get; }

	int Height { get; }

	int Id { get; }

	TextureFiltering Filtering { get; set; }

	TextureWrapping Wrapping { get; set; }

	byte[] GetArgbData();

	void SetArgbData(int width, int height, byte[] data);
}
