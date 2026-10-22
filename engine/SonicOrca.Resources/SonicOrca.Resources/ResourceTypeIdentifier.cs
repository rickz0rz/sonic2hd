namespace SonicOrca.Resources;

public enum ResourceTypeIdentifier : ushort
{
	Null = 0,
	Unknown = 1,
	Xml = 16,
	TextureBMP = 273,
	TextureGIF = 274,
	TextureJPG = 275,
	TexturePNG = 276,
	SampleInfo = 513,
	SampleWAV = 529,
	SampleMP3 = 530,
	SampleOGG = 531,
	Font = 769,
	VideoH264 = 1025,
	AnimationGroup = 4097,
	CompositionGroup = 4098,
	FilmGroup = 4099,
	Area = 4353,
	TileSet = 4354,
	Map = 4355,
	Object = 4356,
	Binding = 4357,
	LevelDependencies = 4358,
	InputRecording = 4608
}
