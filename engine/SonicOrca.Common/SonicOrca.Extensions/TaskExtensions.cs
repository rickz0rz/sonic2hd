using System.Threading.Tasks;

namespace SonicOrca.Extensions;

/// <summary>
/// Waits for tasks to complete on the calling thread.
/// </summary>
public static class TaskExtensions
{
	public static void RunSync(this Task task)
	{
		task.Wait();
	}

	public static T RunSync<T>(this Task<T> task)
	{
		task.Wait();
		return task.Result;
	}
}
