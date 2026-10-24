namespace SonicOrca.Core;

/// <summary>
/// Plays the animation scripts of a Sonic 2 object, as AnimateSprite does. Each script has a frame duration and a
/// list of mapping frames. At the end, a script starts again ($FF) or changes to another script ($FD). Call
/// <see cref="Step"/> once for each frame, where the original calls AnimateSprite.
/// </summary>
public sealed class AnimationScript
{
	private readonly Script[] _scripts;

	private int _previous = -1;

	private int _index;

	private int _timer;

	/// <summary>One script: each frame shows for Duration + 1 frames. Next is the script that follows, or -1 to loop.</summary>
	public readonly record struct Script(int Duration, int[] Frames, int Next = -1);

	public AnimationScript(params Script[] scripts) => _scripts = scripts;

	/// <summary>The script to play (anim). A new value starts the script at its first frame on the next step.</summary>
	public int Animation { get; set; }

	/// <summary>The mapping frame to draw.</summary>
	public int Frame { get; set; }

	public void Step()
	{
		if (Animation != _previous)
		{
			_previous = Animation;
			_index = 0;
			_timer = 0;
		}
		if (--_timer >= 0)
		{
			return;
		}
		Script script = _scripts[Animation];
		_timer = script.Duration;
		if (_index >= script.Frames.Length)
		{
			if (script.Next >= 0)
			{
				// The frame stays until the next script starts on the next step
				Animation = script.Next;
				return;
			}
			_index = 0;
		}
		Frame = script.Frames[_index++];
	}

	/// <summary>Goes back to the first frame of the script (anim_frame = 0), but keeps the timer.</summary>
	public void Rewind() => _index = 0;
}
