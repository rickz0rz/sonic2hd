namespace SonicOrca.Graphics;

public struct Resolution
{
	public int Width { get; }

	public int Height { get; }

	public Resolution(int width, int height)
	{
		Width = width;
		Height = height;
	}

	public override string ToString()
	{
		return $"{Width} x {Height}";
	}
}
