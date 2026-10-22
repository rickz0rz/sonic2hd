using System;
using System.Text;
using csogg;

namespace csvorbis;

public class Comment
{
	private static string _vorbis = "vorbis";

	private static int OV_EIMPL = -130;

	public byte[][] user_comments;

	public int[] comment_lengths;

	public int comments;

	public byte[] vendor;

	public void init()
	{
		user_comments = null;
		comments = 0;
		vendor = null;
	}

	public void add(string comment)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(comment);
		add(bytes);
	}

	private void add(byte[] comment)
	{
		byte[][] destinationArray = new byte[comments + 2][];
		if (user_comments != null)
		{
			Array.Copy(user_comments, 0, destinationArray, 0, comments);
		}
		user_comments = destinationArray;
		int[] destinationArray2 = new int[comments + 2];
		if (comment_lengths != null)
		{
			Array.Copy(comment_lengths, 0, destinationArray2, 0, comments);
		}
		comment_lengths = destinationArray2;
		byte[] array = new byte[comment.Length + 1];
		Array.Copy(comment, 0, array, 0, comment.Length);
		user_comments[comments] = array;
		comment_lengths[comments] = comment.Length;
		comments++;
		user_comments[comments] = null;
	}

	public void add_tag(string tag, string contents)
	{
		if (contents == null)
		{
			contents = "";
		}
		add(tag + "=" + contents);
	}

	private static bool tagcompare(byte[] s1, byte[] s2, int n)
	{
		for (int i = 0; i < n; i++)
		{
			byte b = s1[i];
			byte b2 = s2[i];
			if (b >= 65)
			{
				b = (byte)(b - 65 + 97);
			}
			if (b2 >= 65)
			{
				b2 = (byte)(b2 - 65 + 97);
			}
			if (b != b2)
			{
				return false;
			}
		}
		return true;
	}

	public string query(string tag)
	{
		return query(tag, 0);
	}

	public string query(string tag, int count)
	{
		Encoding uTF = Encoding.UTF8;
		byte[] bytes = uTF.GetBytes(tag);
		int num = query(bytes, count);
		if (num == -1)
		{
			return null;
		}
		byte[] array = user_comments[num];
		for (int i = 0; i < comment_lengths[num]; i++)
		{
			if (array[i] == 61)
			{
				return new string(uTF.GetChars(array), i + 1, comment_lengths[num] - (i + 1));
			}
		}
		return null;
	}

	private int query(byte[] tag, int count)
	{
		int num = 0;
		int num2 = 0;
		int num3 = tag.Length;
		byte[] array = new byte[num3 + 2];
		Array.Copy(tag, 0, array, 0, tag.Length);
		array[tag.Length] = 61;
		for (num = 0; num < comments; num++)
		{
			if (tagcompare(user_comments[num], array, num3))
			{
				if (count == num2)
				{
					return num;
				}
				num2++;
			}
		}
		return -1;
	}

	internal int unpack(csBuffer opb)
	{
		int num = opb.read(32);
		if (num < 0)
		{
			clear();
			return -1;
		}
		vendor = new byte[num + 1];
		opb.read(vendor, num);
		comments = opb.read(32);
		if (comments < 0)
		{
			clear();
			return -1;
		}
		user_comments = new byte[comments + 1][];
		comment_lengths = new int[comments + 1];
		for (int i = 0; i < comments; i++)
		{
			int num2 = opb.read(32);
			if (num2 < 0)
			{
				clear();
				return -1;
			}
			comment_lengths[i] = num2;
			user_comments[i] = new byte[num2 + 1];
			opb.read(user_comments[i], num2);
		}
		if (opb.read(1) != 1)
		{
			clear();
			return -1;
		}
		return 0;
	}

	private int pack(csBuffer opb)
	{
		string text = "Xiphophorus libVorbis I 20000508";
		Encoding uTF = Encoding.UTF8;
		byte[] bytes = uTF.GetBytes(text);
		byte[] bytes2 = uTF.GetBytes(_vorbis);
		opb.write(3, 8);
		opb.write(bytes2);
		opb.write(text.Length, 32);
		opb.write(bytes);
		opb.write(comments, 32);
		if (comments != 0)
		{
			for (int i = 0; i < comments; i++)
			{
				if (user_comments[i] != null)
				{
					opb.write(comment_lengths[i], 32);
					opb.write(user_comments[i]);
				}
				else
				{
					opb.write(0, 32);
				}
			}
		}
		opb.write(1, 1);
		return 0;
	}

	public int header_out(Packet op)
	{
		csBuffer csBuffer2 = new csBuffer();
		csBuffer2.writeinit();
		if (pack(csBuffer2) != 0)
		{
			return OV_EIMPL;
		}
		op.packet_base = new byte[csBuffer2.bytes()];
		op.packet = 0;
		op.bytes = csBuffer2.bytes();
		Array.Copy(csBuffer2.buf(), 0, op.packet_base, 0, op.bytes);
		op.b_o_s = 0;
		op.e_o_s = 0;
		op.granulepos = 0L;
		return 0;
	}

	internal void clear()
	{
		for (int i = 0; i < comments; i++)
		{
			user_comments[i] = null;
		}
		user_comments = null;
		vendor = null;
	}

	public string getVendor()
	{
		return new string(Encoding.UTF8.GetChars(vendor));
	}

	public string getComment(int i)
	{
		Encoding uTF = Encoding.UTF8;
		if (comments <= i)
		{
			return null;
		}
		return new string(uTF.GetChars(user_comments[i]));
	}

	public string toString()
	{
		Encoding uTF = Encoding.UTF8;
		string text = "Vendor: " + new string(uTF.GetChars(vendor));
		for (int i = 0; i < comments; i++)
		{
			text = text + "\nComment: " + new string(uTF.GetChars(user_comments[i]));
		}
		return text + "\n";
	}
}
