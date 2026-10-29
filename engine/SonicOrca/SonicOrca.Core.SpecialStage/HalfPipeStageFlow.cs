using System;
using System.Collections.Generic;

namespace SonicOrca.Core.SpecialStage;

/// <summary>Something in the stage flow that needs a sound or a change in the music.</summary>
public enum HalfPipeFlowEvent
{
	CheckpointPassed,
	CheckpointFailed,
	EmeraldAppeared,
	/// <summary>The emerald is near: the music fades out.</summary>
	EmeraldNear,
	EmeraldWon,
	/// <summary>The won emerald is at its nearest: the emerald jingle plays (MusID_Emerald).</summary>
	EmeraldJingle,
	EmeraldMissed
}

/// <summary>
/// The sequence of a special stage, with the timing of Sonic 2 (s2disasm: Obj5A, Obj59, SSStartNewAct): the
/// "GET xx RINGS!" messages, the "RINGS TO GO" counter, the checkpoints, the emerald and the end of the stage.
/// It knows nothing about drawing. Call <see cref="Update"/> once for each frame.
/// </summary>
public sealed class HalfPipeStageFlow
{
	/// <summary>The frames from the start until the first "GET xx RINGS!" (after the start banner of the original).</summary>
	public const int FirstMessageFrame = 182;

	/// <summary>The message after a checkpoint with enough rings.</summary>
	public const string CheckpointPassedMessage = "COOL!";

	/// <summary>The message after a checkpoint without enough rings, or after the emerald test fails.</summary>
	public const string CheckpointFailedMessage = "NOT ENOUGH RINGS...";

	/// <summary>The frames that a message stays: "GET xx RINGS!", "COOL!" and "NOT ENOUGH RINGS...".</summary>
	public const int MessageFrames = 70;

	/// <summary>The frames after a failed checkpoint message until the stage ends.</summary>
	public const int FailEndFrames = 48;

	/// <summary>The frames after the emerald marker until the emerald appears.</summary>
	public const int EmeraldAppearFrames = 60;

	/// <summary>The frames after the emerald appears until the music fades out.</summary>
	public const int EmeraldNearFrames = 49;

	/// <summary>The frames after the emerald appears until its ring test (Obj59 animation 6).</summary>
	public const int EmeraldTestFrames = 57;

	/// <summary>The depth of the emerald when it appears (Obj59 objoff_30).</summary>
	public const int EmeraldStartDepth = 0x36;

	/// <summary>The emerald comes nearer by this depth each frame ($CCCC in 16.16 fixed point).</summary>
	public const double EmeraldDepthPerFrame = 0.8;

	/// <summary>
	/// The frames after the emerald is won until the emerald jingle plays. Obj59 tests the rings at animation 6 and
	/// plays the jingle at animation 9. The depth gets to these animations 57 and 64 frames after the emerald appears.
	/// </summary>
	public const int EmeraldJingleFrames = 7;

	/// <summary>
	/// The frames that the won emerald stays before the stage ends. Obj59 waits $63 frames after the jingle, and the
	/// stage ends on the next frame.
	/// </summary>
	public const int EmeraldWonEndFrames = EmeraldJingleFrames + 100;

	/// <summary>The frames that "NOT ENOUGH RINGS..." stays after the emerald test fails.</summary>
	public const int EmeraldMissedFrames = 80;

	/// <summary>The frames that "0 RINGS TO GO" shows before the counter hides.</summary>
	private const int ZeroCounterFrames = 12;

	private readonly HalfPipeTrack _track;

	private int _frame;

	private int _nextCheckpoint;

	private int _emeraldWonFrame = -1;

	private string _message;

	private int _messageFramesLeft;

	/// <summary>The message that comes after the current one, for example the next "GET xx RINGS!" after "COOL!".</summary>
	private string _nextMessage;

	private int _endFrame = -1;

	private bool _checkpointFailed;

	private int _emeraldStartFrame = -1;

	private bool _emeraldNear;

	private int _counterZeroFrames;

	public HalfPipeStageFlow(HalfPipeTrack track)
	{
		_track = track;
	}

	/// <summary>The message in the centre of the screen, or null.</summary>
	public string Message => _messageFramesLeft > 0 ? _message : null;

	/// <summary>The "xx RINGS TO GO!" counter, or null when it is hidden. It flashes: visible 6 of each 8 frames.</summary>
	public string Counter { get; private set; }

	/// <summary>The number of the "xx RINGS TO GO!" counter while it shows, or null.</summary>
	public int? RingsToGo { get; private set; }

	/// <summary>The rings needed at the next ring test, or 0 after the last one.</summary>
	public int CurrentGoal => _nextCheckpoint < _track.Checkpoints.Count ? _track.Checkpoints[_nextCheckpoint].RingGoal : 0;

	/// <summary>From the emerald marker on, only START has an effect.</summary>
	public bool InputMasked { get; private set; }

	/// <summary>After a failed emerald test the track nearly stops: one track frame each 60 frames.</summary>
	public bool TrackSlowed { get; private set; }

	public bool EmeraldVisible => _emeraldStartFrame >= 0 && _frame >= _emeraldStartFrame + EmeraldAppearFrames;

	/// <summary>
	/// The distance of the emerald ahead of the player, in the depth units of the original. A track frame is 4 depth
	/// units. The emerald moves on its own timer, as Obj59 does, and not with the track.
	/// </summary>
	public double EmeraldDepth { get; private set; } = EmeraldStartDepth;

	public bool EmeraldWon { get; private set; }

	/// <summary>
	/// The frames since the emerald jingle, for the bobbing of the won emerald. The emerald bobs only after the jingle
	/// (Obj59 routine 8). Until then, it comes nearer.
	/// </summary>
	public int EmeraldBobFrames { get; private set; }

	/// <summary>True when the stage has ended: show the results.</summary>
	public bool Finished { get; private set; }

	/// <param name="playerNode">The track node of the player.</param>
	/// <param name="rings">The rings of the players together.</param>
	public List<HalfPipeFlowEvent> Update(int playerNode, int rings)
	{
		var events = new List<HalfPipeFlowEvent>();
		if (Finished)
		{
			return events;
		}
		_frame++;
		// Count down the current message first, so that a new message shows for its full time
		UpdateMessage();
		if (_frame == FirstMessageFrame)
		{
			ShowMessage(GetRingsMessage(CurrentGoal));
		}
		UpdateCounter(playerNode, rings);
		UpdateCheckpoints(playerNode, rings, events);
		UpdateEmerald(playerNode, rings, events);
		if (EmeraldWon && _frame > _emeraldWonFrame + EmeraldJingleFrames)
		{
			EmeraldBobFrames++;
		}
		if (_endFrame >= 0 && _frame >= _endFrame)
		{
			Finished = true;
		}
		return events;
	}

	public static string GetRingsMessage(int goal) => $"GET {goal} RINGS!";

	private void ShowMessage(string message, string next = null)
	{
		_message = message;
		_messageFramesLeft = MessageFrames;
		_nextMessage = next;
	}

	private void UpdateMessage()
	{
		if (_messageFramesLeft > 0 && --_messageFramesLeft == 0 && _nextMessage != null)
		{
			ShowMessage(_nextMessage);
		}
	}

	private void UpdateCounter(int playerNode, int rings)
	{
		Counter = null;
		RingsToGo = null;
		// The counter hides at the ring test, and stays hidden after a failed one. It also hides at the emerald marker,
		// where Obj59 sets SS_NoCheckpointMsg_flag.
		if (_checkpointFailed || _nextCheckpoint >= _track.Checkpoints.Count || InputMasked)
		{
			return;
		}
		// The counter starts at the "rings to go" marker before the next ring test
		int testNode = _track.Checkpoints[_nextCheckpoint].TrackNode;
		bool active = _track.RingsToGoNodes.Exists(node => node <= playerNode && node < testNode && node > PreviousTestNode());
		if (!active)
		{
			return;
		}
		int toGo = Math.Max(0, CurrentGoal - rings);
		if (toGo == 0)
		{
			if (_counterZeroFrames >= ZeroCounterFrames)
			{
				return;
			}
			_counterZeroFrames++;
		}
		else
		{
			// It comes back when a bomb takes rings
			_counterZeroFrames = 0;
		}
		if (_frame % 8 < 6)
		{
			Counter = toGo == 1 ? "1 RING TO GO!" : $"{toGo} RINGS TO GO!";
			RingsToGo = toGo;
		}
	}

	private int PreviousTestNode() => _nextCheckpoint > 0 ? _track.Checkpoints[_nextCheckpoint - 1].TrackNode : -1;

	private void UpdateCheckpoints(int playerNode, int rings, List<HalfPipeFlowEvent> events)
	{
		// The emerald test is the last checkpoint. It is handled with the emerald.
		if (_nextCheckpoint >= 2 || _checkpointFailed || playerNode < _track.Checkpoints[_nextCheckpoint].TrackNode)
		{
			return;
		}
		if (rings >= CurrentGoal)
		{
			_nextCheckpoint++;
			_counterZeroFrames = 0;
			ShowMessage(CheckpointPassedMessage, GetRingsMessage(CurrentGoal));
			events.Add(HalfPipeFlowEvent.CheckpointPassed);
		}
		else
		{
			ShowMessage(CheckpointFailedMessage);
			events.Add(HalfPipeFlowEvent.CheckpointFailed);
			_checkpointFailed = true;
			_endFrame = _frame + MessageFrames + FailEndFrames;
		}
	}

	private void UpdateEmerald(int playerNode, int rings, List<HalfPipeFlowEvent> events)
	{
		if (_track.EmeraldNode < 0 || _checkpointFailed)
		{
			return;
		}
		if (_emeraldStartFrame < 0 && playerNode >= _track.EmeraldNode)
		{
			_emeraldStartFrame = _frame;
			InputMasked = true;
		}
		if (_emeraldStartFrame < 0)
		{
			return;
		}
		int sinceMarker = _frame - _emeraldStartFrame;
		int sinceAppear = sinceMarker - EmeraldAppearFrames;
		if (sinceAppear == 0)
		{
			events.Add(HalfPipeFlowEvent.EmeraldAppeared);
		}
		// The emerald stops after the jingle (Obj59 routine 8), or after a failed ring test (routine 6)
		if (sinceAppear >= 0 && (EmeraldWon ? sinceAppear <= EmeraldTestFrames + EmeraldJingleFrames : sinceAppear <= EmeraldTestFrames))
		{
			EmeraldDepth = EmeraldStartDepth - EmeraldDepthPerFrame * sinceAppear;
		}
		if (!_emeraldNear && sinceMarker >= EmeraldAppearFrames + EmeraldNearFrames)
		{
			_emeraldNear = true;
			events.Add(HalfPipeFlowEvent.EmeraldNear);
		}
		if (EmeraldWon && _frame == _emeraldWonFrame + EmeraldJingleFrames)
		{
			events.Add(HalfPipeFlowEvent.EmeraldJingle);
		}
		if (_nextCheckpoint == 2 && _endFrame < 0 && sinceAppear == EmeraldTestFrames)
		{
			_nextCheckpoint = 3;
			if (rings >= _track.Checkpoints[2].RingGoal)
			{
				EmeraldWon = true;
				events.Add(HalfPipeFlowEvent.EmeraldWon);
				_emeraldWonFrame = _frame;
				_endFrame = _frame + EmeraldWonEndFrames;
			}
			else
			{
				TrackSlowed = true;
				ShowMessage(CheckpointFailedMessage);
				_messageFramesLeft = EmeraldMissedFrames;
				events.Add(HalfPipeFlowEvent.EmeraldMissed);
				_endFrame = _frame + EmeraldMissedFrames;
			}
		}
	}
}
