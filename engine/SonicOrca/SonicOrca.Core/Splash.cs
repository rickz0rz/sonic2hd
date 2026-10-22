using System.Linq;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca.Core;

internal class Splash
{
	private readonly WaterManager _waterManager;

	private readonly AnimationInstance _animation;

	private Vector2i _position;

	public bool Finished { get; private set; }

	public Splash(WaterManager waterManager, SplashType type, Vector2i position)
	{
		_waterManager = waterManager;
		if (type == SplashType.Enter)
		{
			_animation = new AnimationInstance(waterManager.SpashEnterAnimationGroup);
		}
		else
		{
			_animation = new AnimationInstance(waterManager.SpashExitAnimationGroup);
		}
		_position = position;
	}

	public void Animate()
	{
		if (_waterManager.WaterAreas.Count == 0)
		{
			Finished = true;
			return;
		}
		Rectanglei rectanglei = _waterManager.WaterAreas.First();
		_position.Y = rectanglei.Top;
		_animation.Animate();
		if (_animation.Cycles > 0)
		{
			Finished = true;
		}
	}

	public void Draw(I2dRenderer renderer)
	{
		int height = _animation.CurrentFrame.Source.Height;
		Vector2i vector2i = _position + new Vector2i(0, -height / 2);
		_animation.Draw(renderer, vector2i);
	}
}
