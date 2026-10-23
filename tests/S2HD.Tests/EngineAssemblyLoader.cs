using System.Runtime.CompilerServices;

namespace S2HD.Tests;

internal static class EngineAssemblyLoader
{
	/// <summary>
	/// ResourceType registers the resource types it finds in the loaded assemblies, the first time it is used.
	/// They are defined in the SonicOrca engine assembly, so load that assembly before any test runs.
	/// </summary>
	[ModuleInitializer]
	internal static void LoadEngineAssembly()
	{
		RuntimeHelpers.RunClassConstructor(typeof(SonicOrca.HelperLibraries.Png.PngResourceType).TypeHandle);
	}
}
