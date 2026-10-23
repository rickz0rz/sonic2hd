namespace SonicOrca;

/// <summary>
/// Keeps an instance together with the object that callers lock to get access to that instance.
/// </summary>
public sealed class Lockable<T> where T : class
{
	private readonly T _instance;

	private readonly object _sync;

	public T Instance => _instance;

	public object Sync => _sync;

	public Lockable(T instance)
	{
		_instance = instance;
		_sync = new object();
	}

	public static implicit operator T(Lockable<T> lockable)
	{
		return lockable.Instance;
	}
}
