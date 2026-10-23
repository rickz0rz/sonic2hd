using System;
using System.Threading;
using System.Threading.Tasks;
using SonicOrca.Graphics;
using SonicOrca.Resources;

namespace SonicOrca.Core;

/// <summary>
/// The title card that shows when a level starts.
/// </summary>
public interface ILevelTitleCard : IDisposable
{
	bool AllowLevelToStart { get; }

	bool AllowCharacterControl { get; }

	bool Finished { get; }

	bool Seamless { get; set; }

	bool UnlockCamera { get; set; }

	Task LoadAsync(ResourceTree resourceTree, CancellationToken ct = default(CancellationToken));

	void Update();

	void Draw(Renderer renderer);

	void Start();
}
