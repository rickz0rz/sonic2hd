namespace SonicOrca.Core;

/// <summary>
/// Plays the animation scripts of a Sonic 2 object byte for byte, as AnimateSprite and AnimateSprite_Checked do. A
/// script is the dc.b list of the original: the frame duration, the mapping frames, and an end flag ($FA to $FF) with
/// its argument. $FF starts the script again, $FE goes back, and $FD changes to another script. $FC and $FA tell the
/// object to go to its next routine or secondary routine, and $FB tells it to reset its secondary routine. Call
/// <see cref="Step"/> once for each frame, where the original calls the subroutine. Step returns the end flag that the
/// script reached, or 0. The caller does what the flag does to the routine counters of its object.
/// </summary>
public sealed class AnimateSpriteScript
{
	public const int Loop = 0xFF;

	public const int GoBack = 0xFE;

	public const int ChangeAnimation = 0xFD;

	public const int NextRoutine = 0xFC;

	public const int Reset = 0xFB;

	public const int NextSecondaryRoutine = 0xFA;

	private readonly byte[][] _scripts;

	private int _previous = -1;

	public AnimateSpriteScript(params byte[][] scripts) => _scripts = scripts;

	/// <summary>
	/// True for AnimateSprite_Checked (Mecha Sonic): $FC holds the last frame, and $FF also returns its flag.
	/// </summary>
	public bool Checked { get; init; }

	/// <summary>anim: the script to play. A new value starts the script from its first frame.</summary>
	public int Animation { get; set; }

	/// <summary>mapping_frame.</summary>
	public int Frame { get; set; }

	/// <summary>anim_frame: the position in the script.</summary>
	public int Index { get; private set; }

	/// <summary>anim_frame_duration.</summary>
	public int Timer { get; private set; }

	/// <summary>
	/// Clears the animation state as many objects do with "clr.l mapping_frame": the frame, the script position and
	/// the script number. The next step starts script 0.
	/// </summary>
	public void Clear(int frame = 0)
	{
		Frame = frame;
		Animation = 0;
		_previous = 0;
		Index = 0;
		Timer = 0;
	}

	/// <summary>Goes back to the first frame of the script (anim_frame = 0), but keeps the timer.</summary>
	public void Rewind() => Index = 0;

	public int Step()
	{
		if (Animation != _previous)
		{
			_previous = Animation;
			Index = 0;
			Timer = 0;
		}
		if (--Timer >= 0)
		{
			return 0;
		}
		byte[] script = _scripts[Animation];
		Timer = script[0];
		int value = script[1 + Index];
		if (value < 0x80)
		{
			Frame = value;
			Index++;
			return 0;
		}
		switch (value)
		{
			case Loop:
				Index = 0;
				Frame = script[1] & 0x7F;
				Index++;
				break;
			case GoBack:
				if (Checked)
				{
					Timer = 0;
					Index++;
				}
				else
				{
					Index -= script[2 + Index];
					Frame = script[1 + Index] & 0x7F;
					Index++;
				}
				break;
			case ChangeAnimation:
				if (!Checked)
				{
					Animation = script[2 + Index];
				}
				break;
			case NextRoutine:
				if (Checked)
				{
					Timer = 1;
				}
				else
				{
					Timer = 0;
					Index++;
				}
				break;
			case Reset:
				Index = 0;
				break;
		}
		return value;
	}
}
