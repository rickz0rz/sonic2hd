using System;
using System.Reflection;

namespace SonicOrca;

/// <summary>
/// Contains information about the engine assembly.
/// </summary>
public static class SonicOrcaInfo
{
	public static Version Version = Assembly.GetExecutingAssembly().GetName().Version;
}
