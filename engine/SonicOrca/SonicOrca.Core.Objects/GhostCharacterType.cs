using SonicOrca.Core.Objects.Metadata;

namespace SonicOrca.Core.Objects;

[Name("Ghost")]
[Description("Ghost character simulating playback.")]
[ObjectInstance(typeof(GhostCharacterInstance))]
internal class GhostCharacterType : ObjectType
{
	public const string VirtualResourceKey = "SONICORCA/OBJECTS/GHOST";
}
