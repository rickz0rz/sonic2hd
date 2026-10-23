using System.Collections.Generic;
using System.Linq;
using SonicOrca.Geometry;
using SonicOrca.Resources;

namespace SonicOrca.Graphics.V2.Animation;

public class CompositionInstance
{
	private readonly CompositionGroup _compositionGroup;

	private int _ellapsedFrames;

	private int _currentRenderLayerIndex;

	private List<CompositionLayer> _activeLayers = new List<CompositionLayer>();

	private Composition _composition;

	private bool _playing;

	private bool _finished;

	private List<CompositionLayer> _maskedLayers = new List<CompositionLayer>();

	private List<CompositionLayer> _masks = new List<CompositionLayer>();

	public CompositionGroup CompositionGroup => _compositionGroup;

	public bool AdditiveBlending { get; set; }

	public bool Playing => _playing;

	public bool Finished => _finished;

	private Composition.Frame CurrentFrame
	{
		get
		{
			Composition.Frame result = default;
			CompositionLayer currentLayer = _activeLayers.ElementAt(_currentRenderLayerIndex);
			CompositionLayerAnimatableTransform transform = currentLayer.Transform;
			result.TextureIndex = _compositionGroup.Assets.TakeWhile((CompositionAsset x) => x.ID != currentLayer.TextureReference).Count();
			result.Source = new Rectanglei(0, 0, _compositionGroup.Assets.ElementAt(result.TextureIndex).Width, _compositionGroup.Assets.ElementAt(result.TextureIndex).Height);
			result.Opacity = transform.Opacity;
			result.Position = transform.Position;
			result.Rotation = transform.Rotation;
			result.Scale = transform.Scale;
			return result;
		}
	}

	public CompositionInstance(ResourceTree resourceTree, string resourceKey)
	{
		_compositionGroup = resourceTree.GetLoadedResource<CompositionGroup>(resourceKey);
		_composition = _compositionGroup.First();
	}

	public CompositionInstance(CompositionGroup compositionGroup)
	{
		_compositionGroup = compositionGroup;
		_composition = _compositionGroup.First();
	}

	public void ResetFrame()
	{
		_ellapsedFrames = 0;
		_playing = false;
		_finished = false;
		foreach (CompositionLayer layer in _composition.Layers)
		{
			layer.ResetFrame();
		}
	}

	public void Animate()
	{
		if (_ellapsedFrames >= _composition.StartFrame && _ellapsedFrames <= _composition.EndFrame)
		{
			foreach (CompositionLayer layer in _composition.Layers)
			{
				layer.Animate();
			}
		}
		if (_ellapsedFrames > _composition.EndFrame)
		{
			_playing = false;
			_finished = true;
		}
		else
		{
			_playing = true;
			_ellapsedFrames++;
		}
	}

	public void Seek(int ticks)
	{
		ResetFrame();
		for (int i = 0; i < ticks; i++)
		{
			Animate();
		}
	}

	public void Draw(Renderer renderer, Vector2 offset = default(Vector2), bool flipX = false, bool flipY = false)
	{
		Draw(renderer, Colours.White, offset, flipX, flipY);
	}

	private Composition.Frame GetMaskFrameForLayer(CompositionLayer layer)
	{
		Composition.Frame result = default;
		CompositionLayer currentMask = (from m in _masks
										select (m) into m
										where m.Index == layer.Index - 1 && m.SubKind == CompositionLayer.LayerSubKind.Mask
										select m).First();
		CompositionLayerAnimatableTransform transform = currentMask.Transform;
		result.TextureIndex = _compositionGroup.Assets.TakeWhile((CompositionAsset x) => x.ID != currentMask.TextureReference).Count();
		result.Source = new Rectanglei(0, 0, _compositionGroup.Assets.ElementAt(result.TextureIndex).Width, _compositionGroup.Assets.ElementAt(result.TextureIndex).Height);
		result.Opacity = transform.Opacity;
		result.Position = transform.Position;
		result.Rotation = transform.Rotation;
		result.Scale = transform.Scale;
		return result;
	}

	private List<CompositionLayer> GetActiveLayers()
	{
		IEnumerable<CompositionLayer> activeLayers = from l in _composition.Layers
													 select (l) into l
													 where (_ellapsedFrames >= l.StartFrame && l.Transform.Tweens.Count > 0 && l.Transform.Tweens.Any((CompositionLayerTween t) => t.HasKeyFrames && _ellapsedFrames >= t.StartFrame)) || (_ellapsedFrames >= l.StartFrame && l.Transform.Tweens.All((CompositionLayerTween t) => !t.HasKeyFrames))
													 select l;
		_masks.AddRange(from m in activeLayers
						select (m) into m
						where m.SubKind == CompositionLayer.LayerSubKind.Mask
						select m);
		_maskedLayers.AddRange(from l in activeLayers
							   select (l) into l
							   where activeLayers.Any((CompositionLayer m) => m.SubKind == CompositionLayer.LayerSubKind.Mask && m.Index == l.Index - 1)
							   select l);
		IEnumerable<CompositionLayer> collection = from l in activeLayers
												   select (l) into l
												   where l.SubKind == CompositionLayer.LayerSubKind.None && !_composition.Layers.Any((CompositionLayer m) => m.SubKind == CompositionLayer.LayerSubKind.Mask && m.Index == l.Index - 1)
												   select l;
		_activeLayers.AddRange(collection);
		_activeLayers.AddRange(_maskedLayers);
		_activeLayers = _activeLayers.OrderByDescending((CompositionLayer l) => l.Index).ToList();
		return _activeLayers;
	}

	public void Draw(Renderer renderer, Colour colour, Vector2 offset = default(Vector2), bool flipX = false, bool flipY = false)
	{
		_masks.Clear();
		_maskedLayers.Clear();
		_activeLayers.Clear();
		_activeLayers = GetActiveLayers();
		for (int i = 0; i < _activeLayers.Count(); i++)
		{
			_currentRenderLayerIndex = i;
			Composition.Frame currentFrame = CurrentFrame;
			Rectangle targetDestination = new Rectangle(offset.X - (double)currentFrame.Source.Width * 0.5, offset.Y - (double)currentFrame.Source.Height * 0.5, currentFrame.Source.Width, currentFrame.Source.Height);
			AdditiveBlending = _activeLayers.ElementAt(i).BlendMode == BlendMode.Additive;
			if (!_maskedLayers.Contains(_activeLayers.ElementAt(i)))
			{
				IMaskRenderer maskRenderer = renderer.GetMaskRenderer();
				Draw(maskRenderer, colour, currentFrame.Source, targetDestination, flipX, flipY);
				continue;
			}
			IMaskRenderer maskRenderer2 = renderer.GetMaskRenderer();
			Composition.Frame maskFrameForLayer = GetMaskFrameForLayer(_activeLayers.ElementAt(i));
			Rectangle maskDestination = new Rectangle(offset.X - (double)maskFrameForLayer.Source.Width * 0.5 * (maskFrameForLayer.Scale.X * 0.01), offset.Y - (double)maskFrameForLayer.Source.Height * 0.5 * (maskFrameForLayer.Scale.Y * 0.01), (double)maskFrameForLayer.Source.Width * maskFrameForLayer.Scale.X * 0.01, (double)maskFrameForLayer.Source.Height * maskFrameForLayer.Scale.Y * 0.01);
			Draw(maskRenderer2, maskFrameForLayer, colour, maskFrameForLayer.Source, maskDestination, currentFrame.Source, targetDestination, flipX, flipY);
		}
	}

	public void Draw(I2dRenderer renderer, Colour colour, Rectangle source, Rectangle destination, bool flipX = false, bool flipY = false)
	{
		Composition.Frame currentFrame = CurrentFrame;
		ITexture texture = _compositionGroup.Textures[currentFrame.TextureIndex];
		using (renderer.BeginMatixState())
		{
			Matrix4 identity = Matrix4.Identity;
			identity *= Matrix4.CreateTranslation(currentFrame.Position);
			identity = (identity * Matrix4.CreateScale(currentFrame.Scale.X * 0.01, currentFrame.Scale.Y * 0.01)).RotateZ(currentFrame.Rotation);
			colour.Alpha = (byte)(currentFrame.Opacity * 0.01 * 256.0);
			renderer.ModelMatrix = identity;
			renderer.Colour = colour;
			renderer.BlendMode = ((!AdditiveBlending) ? BlendMode.Alpha : BlendMode.Additive);
			renderer.RenderTexture(texture, source, destination, flipX, flipY);
		}
	}

	public void Draw(IMaskRenderer renderer, Colour colour, Rectangle targetSource, Rectangle targetDestination, bool flipX = false, bool flipY = false)
	{
		Composition.Frame currentFrame = CurrentFrame;
		ITexture texture = _compositionGroup.Textures[currentFrame.TextureIndex];
		using (renderer.BeginMatixState())
		{
			double num = 1.0;
			double num2 = 1.0;
			if (flipX)
			{
				num = -1.0;
			}
			if (flipY)
			{
				num2 = -1.0;
			}
			Matrix4 identity = Matrix4.Identity;
			identity *= Matrix4.CreateTranslation(currentFrame.Position);
			identity = (identity * Matrix4.CreateScale(currentFrame.Scale.X * 0.01 * num, currentFrame.Scale.Y * 0.01 * num2)).RotateZ(currentFrame.Rotation);
			renderer.BlendMode = ((!AdditiveBlending) ? BlendMode.Alpha : BlendMode.Additive);
			renderer.Colour = colour;
			renderer.TargetModelMatrix = identity;
			renderer.Source = targetSource;
			renderer.Destination = targetDestination;
			renderer.Texture = texture;
			renderer.MaskTexture = null;
			renderer.Render();
		}
	}

	public void Draw(IMaskRenderer renderer, Composition.Frame maskFrame, Colour colour, Rectangle maskSource, Rectangle maskDestination, Rectangle targetSource, Rectangle targetDestination, bool flipX = false, bool flipY = false)
	{
		Composition.Frame currentFrame = CurrentFrame;
		ITexture texture = _compositionGroup.Textures[currentFrame.TextureIndex];
		ITexture maskTexture = _compositionGroup.Textures[maskFrame.TextureIndex];
		using (renderer.BeginMatixState())
		{
			Matrix4 identity = Matrix4.Identity;
			identity *= Matrix4.CreateTranslation(maskFrame.Position);
			Matrix4 identity2 = Matrix4.Identity;
			identity2 *= Matrix4.CreateTranslation(new Vector2(0.5));
			identity2 *= Matrix4.CreateScale(1.0 / (currentFrame.Scale.X * 0.01), 1.0 / (currentFrame.Scale.Y * 0.01));
			identity2 *= Matrix4.CreateRotationZ(0.0);
			identity2 *= Matrix4.CreateTranslation(new Vector2(-0.5));
			Matrix4 identity3 = Matrix4.Identity;
			identity3 *= Matrix4.CreateTranslation(new Vector2(0.5));
			identity3 *= Matrix4.CreateRotationZ(maskFrame.Rotation);
			identity3 *= Matrix4.CreateTranslation(new Vector2(-0.5));
			renderer.BlendMode = ((!AdditiveBlending) ? BlendMode.Alpha : BlendMode.Additive);
			renderer.Colour = colour;
			renderer.IntersectionModelMatrix = identity;
			renderer.MaskModelMatrix = identity3;
			renderer.MaskSource = maskSource;
			renderer.MaskDestination = maskDestination;
			renderer.MaskTexture = maskTexture;
			renderer.TargetModelMatrix = identity2;
			renderer.Source = targetSource;
			renderer.Destination = targetDestination;
			renderer.Texture = texture;
			renderer.Render();
		}
	}
}
