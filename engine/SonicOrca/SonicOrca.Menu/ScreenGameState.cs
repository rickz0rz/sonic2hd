using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SonicOrca.Menu;

/// <summary>
/// Runs a <see cref="Screen"/> as a game state: loads it, initialises it, then updates and draws it until it
/// finishes. When the screen switches to another screen, the game state runs the new screen in the same way.
/// </summary>
public sealed class ScreenGameState : IGameState
{
	private readonly SonicOrcaGameContext _gameContext;

	private Screen _screen;

	public ScreenGameState(SonicOrcaGameContext gameContext, Screen screen)
	{
		_gameContext = gameContext;
		_screen = screen;
	}

	public IEnumerable<UpdateResult> Update()
	{
		while (_screen != null)
		{
			_screen.State = ScreenState.Loading;
			Task loadTask = _screen.LoadAsync(new ScreenLoadingProgress(_screen));
			while (!loadTask.IsCompleted)
			{
				yield return UpdateResult.Next();
			}
			if (loadTask.IsFaulted)
			{
				Trace.WriteLine("Unable to load " + _screen.GetType().Name + ": " + loadTask.Exception?.GetBaseException().Message);
				_screen.State = ScreenState.LoadFailed;
				yield break;
			}
			_screen.State = ScreenState.Loaded;
			_screen.Initialise();
			_screen.State = ScreenState.Active;
			while (_screen.State == ScreenState.Active)
			{
				_screen.Update();
				yield return UpdateResult.Next();
			}
			Screen next = _screen.State is ScreenState.Switched or ScreenState.SwitchedEfficiently ? _screen.SwitchScreen : null;
			Close();
			_screen = next;
		}
	}

	public void Draw()
	{
		if (_screen?.State == ScreenState.Active)
		{
			_screen.Draw(_gameContext.Renderer);
		}
	}

	public void Dispose()
	{
		if (_screen != null && _screen.State is not (ScreenState.Constructed or ScreenState.Loading or ScreenState.LoadFailed))
		{
			Close();
		}
		_screen = null;
	}

	private void Close()
	{
		_screen.Unload();
		_screen.State = ScreenState.Unloaded;
		_screen.Deinitialise();
		_screen.State = ScreenState.Deinitialised;
	}
}
