using System;
using System.Reflection;

namespace SonicOrca;

public static class SonicOrcaInfo
{
	public static Version Version = Assembly.GetExecutingAssembly().GetName().Version;
}
