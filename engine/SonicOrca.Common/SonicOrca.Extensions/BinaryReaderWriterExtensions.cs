using System.IO;
using System.Text;

namespace SonicOrca.Extensions;

public static class BinaryReaderWriterExtensions
{
	public static string ReadNullTerminatedString(this BinaryReader br)
	{
		StringBuilder stringBuilder = new StringBuilder();
		byte value;
		while ((value = br.ReadByte()) != 0)
		{
			stringBuilder.Append((char)value);
		}
		return stringBuilder.ToString();
	}

	public static void WriteNullTerminatedString(this BinaryWriter bw, string s)
	{
		if (!string.IsNullOrEmpty(s))
		{
			bw.Write(Encoding.ASCII.GetBytes(s));
		}
		bw.Write((byte)0);
	}
}
