namespace SonicOrca.Core.SpecialStage;

/// <summary>The input of a player in the half-pipe for one frame.</summary>
/// <param name="JumpPressed">True on the frame that a jump button was pressed.</param>
public readonly record struct HalfPipeInput(bool Left, bool Right, bool JumpPressed);

/// <summary>
/// The computer control of Tails in the special stage, as in Sonic 2 (s2disasm: loc_33908 and SSTailsCPU_Control).
/// Tails does what Sonic did 16 frames before. When the second pad has input, its player controls Tails, until
/// 180 frames after the last input. The computer controls Tails only on the ground: in the air, Tails uses only the
/// second pad. Call <see cref="Record"/> once for each frame before Tails moves.
/// </summary>
public sealed class HalfPipeTailsCpu
{
	public const int DelayFrames = 16;

	/// <summary>The frames after the last input of the second pad until the computer controls Tails again.</summary>
	public const int HumanControlFrames = 180;

	// The recorded inputs of Sonic (SS_Ctrl_Record_Buf). Index 0 is the newest. The input of the current frame waits
	// in _newest, and moves into the buffer on the next frame.
	private readonly HalfPipeInput[] _buffer = new HalfPipeInput[DelayFrames];

	private HalfPipeInput _newest;

	private int _humanFramesLeft;

	/// <summary>True while the player of the second pad controls Tails.</summary>
	public bool HumanControl => _humanFramesLeft > 0;

	/// <summary>Records the input of Sonic for this frame.</summary>
	public void Record(HalfPipeInput sonicInput)
	{
		for (int i = DelayFrames - 1; i > 0; i--)
		{
			_buffer[i] = _buffer[i - 1];
		}
		_buffer[0] = _newest;
		_newest = sonicInput;
	}

	/// <summary>The input of Tails on the ground for this frame.</summary>
	/// <param name="pad2">The input of the second pad.</param>
	/// <param name="pad2Held">True while a direction or a jump button of the second pad is held.</param>
	public HalfPipeInput GetGroundInput(HalfPipeInput pad2, bool pad2Held)
	{
		if (pad2Held)
		{
			// The original means to clear the buffer, but its pointer does not move, so only the first two entries
			// are cleared
			_buffer[0] = default;
			_buffer[1] = default;
			_humanFramesLeft = HumanControlFrames;
			return pad2;
		}
		if (_humanFramesLeft > 0)
		{
			_humanFramesLeft--;
			return pad2;
		}
		return _buffer[DelayFrames - 1];
	}
}
