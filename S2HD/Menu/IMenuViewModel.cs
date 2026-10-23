#nullable disable
namespace S2HD.Menu;

/// <summary>
/// The data of one menu page. <see cref="Tag"/> identifies the page (see <see cref="MenuTags"/>).
/// </summary>
internal interface IMenuViewModel
{
	object Tag { get; }
}
