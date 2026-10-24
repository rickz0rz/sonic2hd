namespace SonicOrca.Core;

/// <summary>
/// Plays the animation scripts of a Sonic 2 object exactly as AnimateSprite does, with all the end commands. Each
/// script is the dc.b list of the original: the frame duration, the mapping frames, then a command. $FF starts the
/// script again, $FE jumps back, $FD changes to another script, $FC and $FA tell the object to go to its next routine
/// or secondary routine, and $FB tells it to reset its secondary routine. Call <see cref="Step"/> once for each frame,
/// where the original calls AnimateSprite. Use <see cref="AnimationScript"/> for scripts that only loop or chain.
/// </summary>
public sealed class SpriteAnimator
{
	private readonly byte[][] _scripts;

	private int _previous;

	private int _index;

	private int _timer;

	/// <summary>What the script tells the object to do after a step.</summary>
	public enum Result
	{
		None,

		/// <summary>$FC: go to the next routine.</summary>
		NextRoutine,

		/// <summary>$FA: go to the next secondary routine.</summary>
		NextSecondaryRoutine,

		/// <summary>$FB: reset the secondary routine.</summary>
		ResetSecondaryRoutine
	}

	public SpriteAnimator(params byte[][] scripts) => _scripts = scripts;

	/// <summary>The script to play (anim). A new value starts the script at its first frame on the next step.</summary>
	public int Animation { get; set; }

	/// <summary>The mapping frame to draw (mapping_frame).</summary>
	public int Frame { get; set; }

	/// <summary>
	/// Clears the animation state as many objects do with "clr.l mapping_frame": the frame, the script position and
	/// the script number. The next step starts script 0.
	/// </summary>
	public void Reset(int frame = 0)
	{
		Frame = frame;
		Animation = 0;
		_previous = 0;
		_index = 0;
		_timer = 0;
	}

	public Result Step()
	{
		if (Animation != _previous)
		{
			_previous = Animation;
			_index = 0;
			_timer = 0;
		}
		if (--_timer >= 0)
		{
			return Result.None;
		}
		byte[] script = _scripts[Animation];
		_timer = script[0];
		int value = script[1 + _index];
		if (value < 0x80)
		{
			Frame = value;
			_index++;
			return Result.None;
		}
		switch (value)
		{
			case 0xFF:
				_index = 1;
				Frame = script[1] & 0x7F;
				return Result.None;
			case 0xFE:
				_index -= script[2 + _index];
				Frame = script[1 + _index] & 0x7F;
				_index++;
				return Result.None;
			case 0xFD:
				Animation = script[2 + _index];
				return Result.None;
			case 0xFC:
				_timer = 0;
				_index++;
				return Result.NextRoutine;
			case 0xFB:
				_index = 0;
				return Result.ResetSecondaryRoutine;
			case 0xFA:
				return Result.NextSecondaryRoutine;
			default:
				return Result.None;
		}
	}
}
