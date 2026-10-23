using SonicOrca.Core.Objects.Metadata;

namespace SonicOrca.Core.Objects;

/// <summary>
/// The built-in object type for <see cref="GhostCharacterInstance"/>. It has no script file.
/// </summary>
[Name("Ghost")]
[Description("Ghost character simulating playback.")]
[ObjectInstance(typeof(GhostCharacterInstance))]
internal class GhostCharacterType : ObjectType
{
	public const string VirtualResourceKey = "SONICORCA/OBJECTS/GHOST";
}
