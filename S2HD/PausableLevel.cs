using System;
using System.Collections.Generic;
using S2HD.Menu;
using SonicOrca;
using SonicOrca.Core;

namespace S2HD;

/// <summary>
/// Runs a level and shows the pause menu while a player pauses it. The story and the versus match use it.
/// </summary>
internal sealed class PausableLevel
{
	private readonly OptionsMenu _optionsMenu;

	private bool _optionsMenuShowing;

	public PausableLevel(OptionsMenu optionsMenu)
	{
		_optionsMenu = optionsMenu;
	}

	/// <summary>Runs the level until it ends.</summary>
	/// <param name="canRestart">True when the pause menu can offer to restart the level.</param>
	public IEnumerable<UpdateResult> Run(LevelGameState levelGameState, Func<bool> canRestart)
	{
		foreach (UpdateResult updateResult in levelGameState.Update())
		{
			Level level = levelGameState.Level;
			if (level != null && level.StateFlags.HasFlag(LevelStateFlags.Paused))
			{
				if (!_optionsMenuShowing)
				{
					_optionsMenuShowing = true;
					_optionsMenu.CanRestart = canRestart();
					_optionsMenu.Show();
				}
			}
			else if (_optionsMenuShowing)
			{
				_optionsMenuShowing = false;
				_optionsMenu.Hide();
			}
			else
			{
				levelGameState.HandleInput();
			}
			_optionsMenu.Update();
			yield return updateResult;
		}
	}
}
