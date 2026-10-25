using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Geometry;
using SonicOrca.Resources;

namespace SonicOrca.Graphics.V2.Animation;

/// <summary>
/// Plays and draws the first composition of a composition group. Each instance keeps its own frame counter.
/// </summary>
public class CompositionInstance
{
	/// <summary>
	/// Scale and opacity values in a composition are percentages. Multiply by this value to get a fraction.
	/// </summary>
	private const double PercentToFraction = 0.01;

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

	private Composition.Frame CurrentFrame => GetFrameForLayer(_activeLayers.ElementAt(_currentRenderLayerIndex));

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

	public void Draw(Renderer renderer, Vector2 offset = default, bool flipX = false, bool flipY = false)
	{
		Draw(renderer, Colours.White, offset, flipX, flipY);
	}

	/// <summary>
	/// Gets the frame of a layer. The texture index is the position of the layer's asset in the asset list.
	/// </summary>
	private Composition.Frame GetFrameForLayer(CompositionLayer layer)
	{
		Composition.Frame frame = default;
		CompositionLayerAnimatableTransform transform = layer.Transform;
		frame.TextureIndex = _compositionGroup.Assets.TakeWhile(x => x.ID != layer.TextureReference).Count();
		frame.Source = new Rectanglei(0, 0, _compositionGroup.Assets.ElementAt(frame.TextureIndex).Width, _compositionGroup.Assets.ElementAt(frame.TextureIndex).Height);
		frame.Opacity = transform.Opacity;
		frame.Position = transform.Position;
		frame.Rotation = transform.Rotation;
		frame.Scale = transform.Scale;
		return frame;
	}

	/// <summary>
	/// Gets the frame of the mask for a layer. The mask of a layer is the mask layer with the index just below it.
	/// </summary>
	private Composition.Frame GetMaskFrameForLayer(CompositionLayer layer)
	{
		CompositionLayer mask = _masks.Where(m => m.Index == layer.Index - 1 && m.SubKind == CompositionLayer.LayerSubKind.Mask).First();
		return GetFrameForLayer(mask);
	}

	/// <summary>
	/// Fills the mask list, the masked layer list and the active layer list for the current frame.
	/// A layer is active when it has started and it has a started key frame tween, or when it has no key frames.
	/// The active list holds the unmasked normal layers and the masked layers, from the highest index to the lowest.
	/// </summary>
	private List<CompositionLayer> GetActiveLayers()
	{
		IEnumerable<CompositionLayer> activeLayers = _composition.Layers.Where(l =>
			(_ellapsedFrames >= l.StartFrame && l.Transform.Tweens.Count > 0 && l.Transform.Tweens.Any(t => t.HasKeyFrames && _ellapsedFrames >= t.StartFrame))
			|| (_ellapsedFrames >= l.StartFrame && l.Transform.Tweens.All(t => !t.HasKeyFrames)));
		_masks.AddRange(activeLayers.Where(m => m.SubKind == CompositionLayer.LayerSubKind.Mask));
		_maskedLayers.AddRange(activeLayers.Where(l => activeLayers.Any(m => m.SubKind == CompositionLayer.LayerSubKind.Mask && m.Index == l.Index - 1)));
		IEnumerable<CompositionLayer> unmaskedLayers = activeLayers.Where(l => l.SubKind == CompositionLayer.LayerSubKind.None && !_composition.Layers.Any(m => m.SubKind == CompositionLayer.LayerSubKind.Mask && m.Index == l.Index - 1));
		_activeLayers.AddRange(unmaskedLayers);
		_activeLayers.AddRange(_maskedLayers);
		_activeLayers = _activeLayers.OrderByDescending(l => l.Index).ToList();
		return _activeLayers;
	}

	public void Draw(Renderer renderer, Colour colour, Vector2 offset = default, bool flipX = false, bool flipY = false)
	{
		_masks.Clear();
		_maskedLayers.Clear();
		_activeLayers.Clear();
		_activeLayers = GetActiveLayers();
		for (int i = 0; i < _activeLayers.Count; i++)
		{
			_currentRenderLayerIndex = i;
			Composition.Frame currentFrame = CurrentFrame;
			Rectangle targetDestination = new Rectangle(offset.X - currentFrame.Source.Width * 0.5, offset.Y - currentFrame.Source.Height * 0.5, currentFrame.Source.Width, currentFrame.Source.Height);
			AdditiveBlending = _activeLayers.ElementAt(i).BlendMode == BlendMode.Additive;
			if (!_maskedLayers.Contains(_activeLayers.ElementAt(i)))
			{
				IMaskRenderer maskRenderer = renderer.GetMaskRenderer();
				Draw(maskRenderer, colour, currentFrame.Source, targetDestination, flipX, flipY);
				continue;
			}
			IMaskRenderer maskedLayerRenderer = renderer.GetMaskRenderer();
			Composition.Frame maskFrame = GetMaskFrameForLayer(_activeLayers.ElementAt(i));
			Rectangle maskDestination = new Rectangle(
				offset.X - maskFrame.Source.Width * 0.5 * (maskFrame.Scale.X * PercentToFraction),
				offset.Y - maskFrame.Source.Height * 0.5 * (maskFrame.Scale.Y * PercentToFraction),
				maskFrame.Source.Width * maskFrame.Scale.X * PercentToFraction,
				maskFrame.Source.Height * maskFrame.Scale.Y * PercentToFraction);
			Draw(maskedLayerRenderer, maskFrame, colour, maskFrame.Source, maskDestination, currentFrame.Source, targetDestination, flipX, flipY);
		}
	}

	public void Draw(I2dRenderer renderer, Colour colour, Rectangle source, Rectangle destination, bool flipX = false, bool flipY = false)
	{
		Composition.Frame currentFrame = CurrentFrame;
		ITexture texture = _compositionGroup.Textures[currentFrame.TextureIndex];
		using (renderer.BeginMatixState())
		{
			Matrix4 modelMatrix = Matrix4.Identity;
			modelMatrix *= Matrix4.CreateTranslation(currentFrame.Position);
			modelMatrix = (modelMatrix * Matrix4.CreateScale(currentFrame.Scale.X * PercentToFraction, currentFrame.Scale.Y * PercentToFraction)).RotateZ(currentFrame.Rotation);
			colour.Alpha = (byte)Math.Clamp(Math.Round(currentFrame.Opacity * PercentToFraction * 255.0), 0.0, 255.0);
			renderer.ModelMatrix = modelMatrix;
			renderer.Colour = colour;
			renderer.BlendMode = AdditiveBlending ? BlendMode.Additive : BlendMode.Alpha;
			renderer.RenderTexture(texture, source, destination, flipX, flipY);
		}
	}

	public void Draw(IMaskRenderer renderer, Colour colour, Rectangle targetSource, Rectangle targetDestination, bool flipX = false, bool flipY = false)
	{
		Composition.Frame currentFrame = CurrentFrame;
		ITexture texture = _compositionGroup.Textures[currentFrame.TextureIndex];
		using (renderer.BeginMatixState())
		{
			double scaleSignX = 1.0;
			double scaleSignY = 1.0;
			if (flipX)
			{
				scaleSignX = -1.0;
			}
			if (flipY)
			{
				scaleSignY = -1.0;
			}
			Matrix4 modelMatrix = Matrix4.Identity;
			modelMatrix *= Matrix4.CreateTranslation(currentFrame.Position);
			modelMatrix = (modelMatrix * Matrix4.CreateScale(currentFrame.Scale.X * PercentToFraction * scaleSignX, currentFrame.Scale.Y * PercentToFraction * scaleSignY)).RotateZ(currentFrame.Rotation);
			renderer.BlendMode = AdditiveBlending ? BlendMode.Additive : BlendMode.Alpha;
			renderer.Colour = colour;
			renderer.TargetModelMatrix = modelMatrix;
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
			Matrix4 intersectionModelMatrix = Matrix4.Identity;
			intersectionModelMatrix *= Matrix4.CreateTranslation(maskFrame.Position);
			// Undo the layer scale around the centre of the target, so that the mask keeps its own size.
			Matrix4 targetModelMatrix = Matrix4.Identity;
			targetModelMatrix *= Matrix4.CreateTranslation(new Vector2(0.5));
			targetModelMatrix *= Matrix4.CreateScale(1.0 / (currentFrame.Scale.X * PercentToFraction), 1.0 / (currentFrame.Scale.Y * PercentToFraction));
			targetModelMatrix *= Matrix4.CreateRotationZ(0.0);
			targetModelMatrix *= Matrix4.CreateTranslation(new Vector2(-0.5));
			// Turn the mask around its centre.
			Matrix4 maskModelMatrix = Matrix4.Identity;
			maskModelMatrix *= Matrix4.CreateTranslation(new Vector2(0.5));
			maskModelMatrix *= Matrix4.CreateRotationZ(maskFrame.Rotation);
			maskModelMatrix *= Matrix4.CreateTranslation(new Vector2(-0.5));
			renderer.BlendMode = AdditiveBlending ? BlendMode.Additive : BlendMode.Alpha;
			renderer.Colour = colour;
			renderer.IntersectionModelMatrix = intersectionModelMatrix;
			renderer.MaskModelMatrix = maskModelMatrix;
			renderer.MaskSource = maskSource;
			renderer.MaskDestination = maskDestination;
			renderer.MaskTexture = maskTexture;
			renderer.TargetModelMatrix = targetModelMatrix;
			renderer.Source = targetSource;
			renderer.Destination = targetDestination;
			renderer.Texture = texture;
			renderer.Render();
		}
	}
}
