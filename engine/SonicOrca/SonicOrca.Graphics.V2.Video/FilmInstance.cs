using System.Linq;
using SonicOrca.Geometry;
using SonicOrca.Resources;

namespace SonicOrca.Graphics.V2.Video;

public class FilmInstance
{
	private readonly FilmGroup _filmGroup;

	private Film _film;

	private bool _playing;

	private bool _finished;

	private ITexture _renderTarget;

	public FilmGroup FilmGroup => _filmGroup;

	public bool Playing => _playing;

	public bool Finished => _finished;

	public double CurrentTime => _filmGroup.FilmBuffers.FirstOrDefault()?.CurrentTime ?? 0.0;

	public double Duration => _filmGroup.FilmBuffers.FirstOrDefault()?.Duration ?? 0.0;

	public FilmInstance(ResourceTree resourceTree, string resourceKey)
	{
		_filmGroup = resourceTree.GetLoadedResource<FilmGroup>(resourceKey);
		_film = _filmGroup.First();
		SetupRenderTarget();
	}

	public FilmInstance(FilmGroup filmGroup)
	{
		_filmGroup = filmGroup;
		_film = _filmGroup.First();
		SetupRenderTarget();
	}

	private void SetupRenderTarget()
	{
		IFilmBuffer filmBuffer = _filmGroup.FilmBuffers.First();
		IGraphicsContext graphicsContext = SonicOrcaGameContext.Singleton.Window.GraphicsContext;
		_renderTarget = graphicsContext.CreateTexture(filmBuffer.Width, filmBuffer.Height, 4, new byte[filmBuffer.Width * filmBuffer.Height * 4]);
	}

	public void Animate()
	{
		IFilmBuffer filmBuffer = _filmGroup.FilmBuffers.First();
		filmBuffer.Decode();
		if (filmBuffer.CurrentTime >= filmBuffer.Duration)
		{
			_finished = true;
		}
	}

	public void Seek(int ticks)
	{
	}

	public void Draw(Renderer renderer, Vector2 offset = default(Vector2), bool flipX = false, bool flipY = false)
	{
		Draw(renderer, Colours.White, offset, flipX, flipY);
	}

	public void Draw(Renderer renderer, Colour colour, Vector2 offset = default(Vector2), bool flipX = false, bool flipY = false)
	{
		IFilmBuffer filmBuffer = _filmGroup.FilmBuffers.First();
		Rectanglei rectanglei = new Rectanglei(0, 0, filmBuffer.Width, filmBuffer.Height);
		Rectanglei rectanglei2 = new Rectanglei(0, 0, filmBuffer.Width, filmBuffer.Height);
		I2dRenderer renderer2 = renderer.Get2dRenderer();
		Draw(renderer2, colour, rectanglei, rectanglei2, flipX, flipY);
	}

	public void Draw(I2dRenderer renderer, Colour colour, Rectangle source, Rectangle destination, bool flipX = false, bool flipY = false)
	{
		IFilmBuffer filmBuffer = _filmGroup.FilmBuffers.First();
		byte[] argbData = filmBuffer.GetArgbData();
		_renderTarget.SetArgbData(filmBuffer.Width, filmBuffer.Height, argbData);
		using (renderer.BeginMatixState())
		{
			Matrix4 identity = Matrix4.Identity;
			identity *= Matrix4.CreateScale((!flipX) ? 1 : (-1), (!flipY) ? 1 : (-1));
			renderer.ModelMatrix = identity;
			renderer.Colour = Colours.White;
			renderer.BlendMode = BlendMode.Opaque;
			renderer.RenderTexture(_renderTarget, source, destination, flipX, flipY);
		}
	}
}
