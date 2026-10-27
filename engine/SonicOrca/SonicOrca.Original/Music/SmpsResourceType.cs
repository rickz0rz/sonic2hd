using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SonicOrca.Resources;

namespace SonicOrca.Original.Music;

/// <summary>
/// Loads a ".smps" file into a <see cref="SongSample"/>. The file has the bytes of an assembled <see cref="SmpsSong"/>
/// and the DAC drums. Version 2 also tells if the song is a sound effect. <see cref="Write"/> makes the file.
/// </summary>
public sealed class SmpsResourceType : ResourceType
{
	private const string Signature = "SMPS";

	private const int FormatVersion = 2;

	/// <summary>The bit of the flags (from version 2) for a sound effect.</summary>
	private const int SoundEffectFlag = 1;

	public override string Name => "smps";

	public override string DefaultExtension => ".smps";

	public override bool CompressByDefault => true;

	public SmpsResourceType()
		: base(ResourceTypeIdentifier.SampleSMPS)
	{
	}

	public override async Task<ILoadedResource> LoadAsync(ResourceLoadArgs e, CancellationToken ct = default(CancellationToken))
	{
		return await Task.Run<ILoadedResource>(() =>
		{
			// The reader is not disposed because that would close the input stream.
			BinaryReader reader = new BinaryReader(e.InputStream);
			if (Encoding.ASCII.GetString(reader.ReadBytes(Signature.Length)) != Signature)
			{
				throw new ResourceException("Invalid smps signature.");
			}
			int version = reader.ReadInt32();
			if (version < 1 || version > FormatVersion)
			{
				throw new ResourceException($"Unknown smps version {version}.");
			}
			int flags = version >= 2 ? reader.ReadByte() : 0;
			SmpsSong song = SmpsSong.FromData(reader.ReadBytes(reader.ReadInt32()), (flags & SoundEffectFlag) != 0);
			Dictionary<DacSample, (byte[] Samples, int Rate)> drums = new Dictionary<DacSample, (byte[], int)>();
			int drumCount = reader.ReadByte();
			for (int i = 0; i < drumCount; i++)
			{
				DacSample sample = (DacSample)reader.ReadByte();
				int rate = reader.ReadInt32();
				drums[sample] = (reader.ReadBytes(reader.ReadInt32()), rate);
			}
			return new SongSample(song, drums) { Resource = e.Resource };
		});
	}

	/// <summary>Writes a song and its DAC drums as a ".smps" file.</summary>
	public static void Write(string path, SmpsSong song, IReadOnlyDictionary<DacSample, (byte[] Samples, int Rate)> drums)
	{
		using BinaryWriter writer = new BinaryWriter(File.Create(path));
		writer.Write(Encoding.ASCII.GetBytes(Signature));
		writer.Write(FormatVersion);
		writer.Write((byte)(song.IsSoundEffect ? SoundEffectFlag : 0));
		writer.Write(song.Data.Length);
		writer.Write(song.Data);
		writer.Write((byte)drums.Count);
		foreach (KeyValuePair<DacSample, (byte[] Samples, int Rate)> drum in drums)
		{
			writer.Write((byte)drum.Key);
			writer.Write(drum.Value.Rate);
			writer.Write(drum.Value.Samples.Length);
			writer.Write(drum.Value.Samples);
		}
	}
}
