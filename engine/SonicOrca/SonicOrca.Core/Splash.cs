using System.Linq;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca.Core;

/// <summary>
/// A water splash animation that stays on the surface of the water.
/// </summary>
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
		// The splash follows the surface of the first water area.
		Rectanglei waterArea = _waterManager.WaterAreas.First();
		_position.Y = waterArea.Top;
		_animation.Animate();
		if (_animation.Cycles > 0)
		{
			Finished = true;
		}
	}

	public void Draw(I2dRenderer renderer)
	{
		int height = _animation.CurrentFrame.Source.Height;
		Vector2i drawPosition = _position + new Vector2i(0, -height / 2);
		_animation.Draw(renderer, drawPosition);
	}
}
