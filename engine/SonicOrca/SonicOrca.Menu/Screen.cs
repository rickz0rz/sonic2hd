using System.Threading;
using System.Threading.Tasks;
using SonicOrca.Graphics;

namespace SonicOrca.Menu;

public abstract class Screen
{
	public ScreenState State { get; set; }

	public Screen SwitchScreen { get; set; }

	public bool ManagerHasResponsibility { get; set; }

	public Screen()
	{
		State = ScreenState.Constructed;
		ManagerHasResponsibility = true;
	}

	public virtual void Initialise()
	{
	}

	public virtual Task LoadAsync(ScreenLoadingProgress progress, CancellationToken ct = default(CancellationToken))
	{
		return Task.FromResult(result: false);
	}

	public virtual void Update()
	{
	}

	public virtual void Draw(Renderer renderer)
	{
	}

	public virtual void Unload()
	{
	}

	public virtual void Deinitialise()
	{
	}

	protected void Finish()
	{
		State = ScreenState.Finished;
	}

	protected void SwitchEfficient(Screen screen)
	{
		State = ScreenState.SwitchedEfficiently;
		SwitchScreen = screen;
	}

	protected void Switch(Screen screen)
	{
		State = ScreenState.Switched;
		SwitchScreen = screen;
	}
}
