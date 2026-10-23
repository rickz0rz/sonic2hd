using System.Runtime.CompilerServices;
using SonicOrca.HelperLibraries.Png;
using SonicOrca.Resources;

namespace S2HD.Tests;

internal static class EngineAssemblyLoader
{
	/// <summary>Registers the resource types of the SonicOrca engine assembly before any test runs.</summary>
	[ModuleInitializer]
	internal static void RegisterResourceTypes()
	{
		ResourceType.RegisterTypesFrom(typeof(PngResourceType).Assembly);
	}
}
