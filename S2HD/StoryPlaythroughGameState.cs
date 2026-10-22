// Decompiled with JetBrains decompiler
// Type: S2HD.StoryPlaythroughGameState
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using S2HD.Menu;
using SonicOrca;
using SonicOrca.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

#nullable disable
namespace S2HD;

internal class StoryPlaythroughGameState : IGameState, IDisposable
{
  private S2HDSonicOrcaGameContext _gameContext;
  private LevelPrepareSettings _prepareSettings;
  private LevelGameState _levelGameState;
  private int _levelNumber;
  private OptionsMenu _optionsMenu;
  private bool _optionsMenuShowing;

  public bool QuitViaOptions { get; private set; }

  public StoryPlaythroughGameState(S2HDSonicOrcaGameContext gameContext)
  {
    this._gameContext = gameContext;
    this._prepareSettings = new LevelPrepareSettings();
    this._levelNumber = 1;
  }

  public void StartFrom(LevelPrepareSettings prepareSettings)
  {
    this._prepareSettings = prepareSettings;
    this._levelGameState = new LevelGameState((SonicOrcaGameContext) this._gameContext);
    this._levelGameState.PrepareSettings = prepareSettings;
    this._levelNumber = prepareSettings.LevelNumber;
    this._gameContext.Console.LevelGameState = this._levelGameState;
  }

  public IEnumerable<UpdateResult> Update()
  {
    this.QuitViaOptions = false;
    this._optionsMenu = new OptionsMenu(this._gameContext);
    this._optionsMenu.OnResume += (EventHandler) ((s, e) => this._levelGameState.Level.Unpause());
    this._optionsMenu.OnRestart += (EventHandler) ((s, e) =>
    {
      Level level = this._levelGameState.Level;
      if (level.Player.Lives <= 0)
        return;
      level.Unpause();
      level.FadeOut(LevelState.RestartCheckpoint);
      --level.Player.Lives;
    });
    this._optionsMenu.OnQuit += (EventHandler) ((s, e) =>
    {
      Level level = this._levelGameState.Level;
      level.Unpause();
      level.FadeOut(LevelState.Quit);
      this.QuitViaOptions = true;
    });
    Task optionsLoadTask = this._optionsMenu.LoadAsync();
    while (!optionsLoadTask.IsCompleted)
      yield return UpdateResult.Next();
    if (this._levelGameState == null)
      this.SetLevel(this._levelNumber);
    bool flag;
    do
    {
      foreach (UpdateResult updateResult in this._levelGameState.Update())
      {
        Level level = this._levelGameState.Level;
        if (level != null && level.StateFlags.HasFlag((Enum) LevelStateFlags.Paused))
        {
          if (!this._optionsMenuShowing)
          {
            this._optionsMenuShowing = true;
            this._optionsMenu.CanRestart = this._levelGameState.Player.Lives > 1;
            this._optionsMenu.Show();
          }
        }
        else if (this._optionsMenuShowing)
        {
          this._optionsMenuShowing = false;
          this._optionsMenu.Hide();
        }
        else
          this._levelGameState.HandleInput();
        this._optionsMenu.Update();
        yield return updateResult;
      }
      flag = this._levelGameState.Completed;
      if (flag)
      {
        this._prepareSettings.Lives = this._levelGameState.Player.Lives;
        this._prepareSettings.Score = this._levelGameState.Player.Score;
        if (!this.LoadNextLevel())
          flag = false;
      }
    }
    while (flag);
  }

  public void Draw()
  {
    if (this._levelGameState == null)
      return;
    this._levelGameState.Draw();
    this._optionsMenu.Draw(this._gameContext.Renderer);
  }

  private bool LoadNextLevel()
  {
    ++this._levelNumber;
    return this.SetLevel(this._levelNumber);
  }

  private bool SetLevel(int index)
  {
    if (this._levelGameState != null)
      this._levelGameState.Dispose();
    LevelInfo levelInfo = Levels.LevelList.Where<LevelInfo>((Func<LevelInfo, bool>) (x => x.GameIndex == 2)).Where<LevelInfo>((Func<LevelInfo, bool>) (x => !x.Unreleased)).Skip<LevelInfo>(this._levelNumber - 1).FirstOrDefault<LevelInfo>();
    if (levelInfo == null)
      return false;
    this._levelGameState = new LevelGameState((SonicOrcaGameContext) this._gameContext);
    this._levelGameState.PrepareSettings = new LevelPrepareSettings()
    {
      AreaResourceKey = Levels.GetAreaResourceKey(levelInfo.Mnemonic),
      Act = 1,
      Lives = this._prepareSettings.Lives,
      Score = this._prepareSettings.Score,
      Debugging = this._prepareSettings.Debugging,
      ProtagonistCharacter = this._prepareSettings.ProtagonistCharacter,
      SidekickCharacter = this._prepareSettings.SidekickCharacter
    };
    this._gameContext.Console.LevelGameState = this._levelGameState;
    return true;
  }

  public void Dispose()
  {
    if (this._levelGameState == null)
      return;
    this._levelGameState.Dispose();
  }
}
