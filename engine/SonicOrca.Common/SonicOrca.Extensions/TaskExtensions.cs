using System.Threading.Tasks;

namespace SonicOrca.Extensions;

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
