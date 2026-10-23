using System;

namespace SonicOrca.Graphics.V2.Animation;

public class CompositionLayer
{
	public enum LayerKind
	{
		Image
	}

	public enum LayerSubKind
	{
		None,
		Mask
	}

	private const uint _ddd = 0u;

	private uint _index;

	private LayerKind _layerType;

	private LayerSubKind _layerSubType;

	private BlendMode _layerBlendMode;

	private string _name;

	private string _fileExtension;

	private string _textureReference;

	private const double _sampleRate = 0.0;

	private uint _startFrame;

	private uint _endFrame;

	private const uint _bm = 0u;

	private uint _ellapsedFrames;

	private CompositionLayerAnimatableTransform _transform;

	public string TextureReference => _textureReference;

	public uint StartFrame => _startFrame;

	public uint EndFrame => _endFrame;

	public uint Index => _index;

	public LayerKind Kind => _layerType;

	public LayerSubKind SubKind => _layerSubType;

	public BlendMode BlendMode => _layerBlendMode;

	public CompositionLayerAnimatableTransform Transform => _transform;

	public CompositionLayer(uint index, uint layerType, uint layerSubType, uint layerBlendMode, string name, string fileExtension, string textureReference, uint startFrame, uint endFrame, CompositionLayerAnimatableTransform transform)
	{
		_index = index;
		if (layerType == 2)
		{
			_layerType = LayerKind.Image;
			switch (layerSubType)
			{
				case 0u:
					_layerSubType = LayerSubKind.None;
					break;
				case 1u:
					_layerSubType = LayerSubKind.Mask;
					break;
				default:
					throw new NotImplementedException();
			}
			switch (layerBlendMode)
			{
				case 0u:
					_layerBlendMode = BlendMode.Alpha;
					break;
				case 1u:
					_layerBlendMode = BlendMode.Additive;
					break;
				case 2u:
					_layerBlendMode = BlendMode.Opaque;
					break;
			}
			_name = name;
			_fileExtension = fileExtension;
			_textureReference = textureReference;
			_startFrame = startFrame;
			_endFrame = endFrame;
			_transform = transform;
			return;
		}
		throw new NotImplementedException();
	}

	public void ResetFrame()
	{
		_ellapsedFrames = 0u;
		_transform.ResetFrame();
	}

	public void Animate()
	{
		if (_ellapsedFrames <= _endFrame)
		{
			_transform.Animate();
		}
		if (_ellapsedFrames <= _endFrame)
		{
			_ellapsedFrames++;
		}
	}
}
