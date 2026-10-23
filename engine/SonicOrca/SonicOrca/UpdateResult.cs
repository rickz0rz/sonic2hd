namespace SonicOrca;

/// <summary>
/// The result of one step of an update sequence: go to the next step, or wait a number of ticks first.
/// </summary>
public struct UpdateResult
{
	private readonly UpdateResultType _type;

	private readonly int _waitTicks;

	public UpdateResultType Type => _type;

	public int WaitTicks => _waitTicks;

	private UpdateResult(UpdateResultType type, int waitTicks = 0)
	{
		_type = type;
		_waitTicks = waitTicks;
	}

	public static UpdateResult Next()
	{
		return new UpdateResult(UpdateResultType.Next);
	}

	public static UpdateResult Wait(int ticks)
	{
		return new UpdateResult(UpdateResultType.Wait, ticks);
	}
}
