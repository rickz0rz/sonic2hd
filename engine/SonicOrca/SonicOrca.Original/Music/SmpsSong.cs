using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SonicOrca.Original.Music;

/// <summary>
/// A song of the Sonic 2 sound driver, assembled from its SMPS2ASM source file of the disassembly (for example
/// "sound/music/86 - HTZ.asm"). The assembler knows the macros of sound/_smps2asm_inc.asm for the Sonic 2 driver. It
/// writes the same bytes as the build of the disassembly, except that a pointer is an offset in <see cref="Data"/>, in
/// the byte order of the Z80.
/// </summary>
public sealed class SmpsSong
{
	/// <summary>The PSG note that the songs use for the highest PSG frequency (nMaxPSG in SMPS2ASM).</summary>
	private const int MaxPsgNote = 0xC6;

	private static readonly string[] NoteNames = { "C", "Cs", "D", "Ds", "E", "F", "Fs", "G", "Gs", "A", "As", "B" };

	private static readonly Dictionary<string, string> NoteAliases = new Dictionary<string, string>
	{
		["Db"] = "Cs", ["Eb"] = "Ds", ["Gb"] = "Fs", ["Ab"] = "Gs", ["Bb"] = "As"
	};

	private static readonly string[] DacNames =
	{
		"dKick", "dSnare", "dClap", "dScratch", "dTimpani", "dHiTom", "dVLowClap", "dHiTimpani", "dMidTimpani",
		"dLowTimpani", "dVLowTimpani", "dMidTom", "dLowTom", "dFloorTom", "dHiClap", "dMidClap", "dLowClap"
	};

	private readonly List<byte> _data = new List<byte>();
	private readonly Dictionary<string, int> _labels = new Dictionary<string, int>(StringComparer.Ordinal);
	private readonly List<(int Offset, string Label)> _pointers = new List<(int, string)>();
	private readonly Dictionary<string, int> _voice = new Dictionary<string, int>(StringComparer.Ordinal);
	private int _sourceSmps2AsmVersion;

	/// <summary>The bytes of the song. The header is at offset 0.</summary>
	public byte[] Data { get; private set; }

	/// <summary>The offset of each label of the source file.</summary>
	public IReadOnlyDictionary<string, int> Labels => _labels;

	private SmpsSong()
	{
	}

	/// <summary>Assembles a song file.</summary>
	/// <param name="fixDataBugs">The value of FixMusicAndSFXDataBugs. True plays the songs as their composers meant.</param>
	public static SmpsSong FromFile(string path, bool fixDataBugs = true)
	{
		return FromText(File.ReadAllText(path), fixDataBugs);
	}

	/// <summary>Assembles the text of a song file.</summary>
	public static SmpsSong FromText(string text, bool fixDataBugs = true)
	{
		SmpsSong song = new SmpsSong();
		song.Assemble(text, fixDataBugs);
		return song;
	}

	private void Assemble(string text, bool fixDataBugs)
	{
		// Each entry is true when the lines of that "if" level are assembled.
		Stack<(bool Active, bool Taken)> conditions = new Stack<(bool, bool)>();
		int lineNumber = 0;
		foreach (string rawLine in text.Split('\n'))
		{
			lineNumber++;
			string line = StripComment(rawLine).TrimEnd();
			if (line.Length == 0)
			{
				continue;
			}
			string trimmed = line.Trim();
			string keyword = trimmed.Split(new[] { ' ', '\t' }, 2)[0].ToLowerInvariant();
			bool active = conditions.All(c => c.Active);
			if (keyword == "if")
			{
				bool value = active && EvaluateCondition(trimmed.Substring(2).Trim(), fixDataBugs);
				conditions.Push((value, value));
				continue;
			}
			if (keyword == "else")
			{
				(bool _, bool taken) = conditions.Pop();
				conditions.Push((!taken && conditions.All(c => c.Active), true));
				continue;
			}
			if (keyword == "endif")
			{
				conditions.Pop();
				continue;
			}
			if (!active)
			{
				continue;
			}
			try
			{
				AssembleLine(line);
			}
			catch (Exception ex) when (ex is FormatException or KeyNotFoundException or ArgumentException)
			{
				throw new FormatException($"Line {lineNumber}: {ex.Message} ({rawLine.Trim()})", ex);
			}
		}
		foreach ((int offset, string label) in _pointers)
		{
			if (!_labels.TryGetValue(label, out int target))
			{
				throw new FormatException($"Unknown label {label}.");
			}
			_data[offset] = (byte)target;
			_data[offset + 1] = (byte)(target >> 8);
		}
		Data = _data.ToArray();
	}

	private static string StripComment(string line)
	{
		int index = line.IndexOf(';');
		return index >= 0 ? line.Substring(0, index) : line;
	}

	private static bool EvaluateCondition(string condition, bool fixDataBugs)
	{
		bool negate = false;
		while (condition.StartsWith("~~", StringComparison.Ordinal))
		{
			negate = !negate;
			condition = condition.Substring(2).Trim();
		}
		if (condition != "FixMusicAndSFXDataBugs")
		{
			throw new FormatException($"Unknown condition {condition}.");
		}
		return fixDataBugs != negate;
	}

	private void AssembleLine(string line)
	{
		if (!char.IsWhiteSpace(line[0]))
		{
			// A label, at the start of the line
			int colon = line.IndexOf(':');
			string label = (colon >= 0 ? line.Substring(0, colon) : line).Trim();
			_labels[label] = _data.Count;
			line = colon >= 0 ? line.Substring(colon + 1) : "";
			if (line.Trim().Length == 0)
			{
				return;
			}
		}
		string trimmed = line.Trim();
		int split = trimmed.IndexOfAny(new[] { ' ', '\t' });
		string name = split >= 0 ? trimmed.Substring(0, split) : trimmed;
		string[] args = split >= 0
			? trimmed.Substring(split + 1).Split(',').Select(a => a.Trim()).ToArray()
			: Array.Empty<string>();
		AssembleMacro(name, args);
	}

	private void AssembleMacro(string name, string[] args)
	{
		switch (name)
		{
			case "dc.b":
				foreach (string arg in args)
				{
					Byte(Evaluate(arg));
				}
				break;
			case "smpsHeaderStartSong":
				if (Evaluate(args[0]) != 2)
				{
					throw new FormatException("Only songs for the Sonic 2 driver are supported.");
				}
				_sourceSmps2AsmVersion = args.Length > 1 ? Evaluate(args[1]) : 0;
				break;
			case "smpsHeaderVoice":
				Pointer(args[0]);
				break;
			case "smpsHeaderVoiceNull":
				Byte(0);
				Byte(0);
				break;
			case "smpsHeaderChan":
			case "smpsHeaderTempo":
				Byte(Evaluate(args[0]));
				Byte(Evaluate(args[1]));
				break;
			case "smpsHeaderDAC":
				Pointer(args[0]);
				Byte(args.Length > 1 ? Evaluate(args[1]) : 0);
				Byte(args.Length > 2 ? Evaluate(args[2]) : 0);
				break;
			case "smpsHeaderFM":
				Pointer(args[0]);
				Byte(Evaluate(args[1]));
				Byte(Evaluate(args[2]));
				break;
			case "smpsHeaderPSG":
				Pointer(args[0]);
				Byte(Evaluate(args[1]));
				Byte(Evaluate(args[2]));
				Byte(Evaluate(args[3]));
				Byte(Evaluate(args[4]));
				break;
			case "smpsPan":
				Byte(0xE0);
				Byte(Evaluate(args[0]) + (args.Length > 1 ? Evaluate(args[1]) : 0));
				break;
			case "smpsDetune":
			case "smpsAlterNote":
				Bytes(0xE1, args[0]);
				break;
			case "smpsNop":
				Bytes(0xE2, args[0]);
				break;
			case "smpsReturn":
				Byte(0xE3);
				break;
			case "smpsFade":
				Byte(0xE4);
				break;
			case "smpsChanTempoDiv":
				Bytes(0xE5, args[0]);
				break;
			case "smpsAlterVol":
			case "smpsPSGAlterVolS2":
				Bytes(0xE6, args[0]);
				break;
			case "smpsFMAlterVol":
				Bytes(0xE6, args.Length > 1 ? args[1] : args[0]);
				break;
			case "smpsNoteFill":
				Bytes(0xE8, args[0]);
				break;
			case "smpsChangeTransposition":
			case "smpsAlterPitch":
				Bytes(0xE9, args[0]);
				break;
			case "smpsSetTempoMod":
				Bytes(0xEA, args[0]);
				break;
			case "smpsSetTempoDiv":
				Bytes(0xEB, args[0]);
				break;
			case "smpsPSGAlterVol":
				Bytes(0xEC, args[0]);
				break;
			case "smpsSetvoice":
			case "smpsFMvoice":
				Bytes(0xEF, args[0]);
				break;
			case "smpsModSet":
				Byte(0xF0);
				foreach (string arg in args)
				{
					Byte(Evaluate(arg));
				}
				break;
			case "smpsModOn":
				Byte(0xF1);
				break;
			case "smpsStop":
				Byte(0xF2);
				break;
			case "smpsPSGform":
				Bytes(0xF3, args[0]);
				break;
			case "smpsModOff":
				Byte(0xF4);
				break;
			case "smpsPSGvoice":
				Bytes(0xF5, args[0]);
				break;
			case "smpsJump":
				Byte(0xF6);
				Pointer(args[0]);
				break;
			case "smpsLoop":
				Byte(0xF7);
				Byte(Evaluate(args[0]));
				Byte(Evaluate(args[1]));
				Pointer(args[2]);
				break;
			case "smpsCall":
				Byte(0xF8);
				Pointer(args[0]);
				break;
			case "smpsMaxRelRate":
			case "smpsWeirdD1LRR":
				Byte(0xF9);
				break;
			default:
				AssembleVoiceMacro(name, args);
				break;
		}
	}

	/// <summary>The smpsVc* macros of a voice. The last one, smpsVcTotalLevel, writes the 25 bytes of the voice.</summary>
	private void AssembleVoiceMacro(string name, string[] args)
	{
		string[] names = name switch
		{
			"smpsVcFeedback" => new[] { "FB" },
			"smpsVcAlgorithm" => new[] { "ALG" },
			"smpsVcUnusedBits" => new[] { "UNUSED", "D1RU1", "D1RU2", "D1RU3", "D1RU4" },
			"smpsVcDetune" => Ops("DT"),
			"smpsVcCoarseFreq" => Ops("CF"),
			"smpsVcRateScale" => Ops("RS"),
			"smpsVcAttackRate" => Ops("AR"),
			"smpsVcAmpMod" => Ops("AM"),
			"smpsVcDecayRate1" => Ops("D1R"),
			"smpsVcDecayRate2" => Ops("D2R"),
			"smpsVcDecayLevel" => Ops("DL"),
			"smpsVcReleaseRate" => Ops("RR"),
			"smpsVcTotalLevel" => Ops("TL"),
			_ => throw new FormatException($"Unknown macro {name}.")
		};
		for (int i = 0; i < names.Length; i++)
		{
			_voice[names[i]] = i < args.Length ? Evaluate(args[i]) : 0;
		}
		if (name == "smpsVcUnusedBits" && args.Length < 5)
		{
			for (int i = 1; i <= 4; i++)
			{
				_voice["D1RU" + i] = 0;
			}
		}
		if (name == "smpsVcTotalLevel")
		{
			WriteVoice();
		}
	}

	private static string[] Ops(string prefix) => new[] { prefix + "1", prefix + "2", prefix + "3", prefix + "4" };

	private void WriteVoice()
	{
		int V(string key) => _voice.TryGetValue(key, out int value) ? value : 0;
		int algorithm = V("ALG");
		int[] tlMask = new int[5];
		if (_sourceSmps2AsmVersion == 0)
		{
			tlMask[4] = algorithm == 7 ? 0x80 : 0;
			tlMask[3] = algorithm >= 4 ? 0x80 : 0;
			tlMask[2] = algorithm >= 5 ? 0x80 : 0;
			tlMask[1] = 0x80;
		}
		int amShift = _sourceSmps2AsmVersion == 0 ? 5 : 7;
		Byte((V("UNUSED") << 6) + (V("FB") << 3) + algorithm);
		// The Sonic 2 driver writes the operators in the order 4, 2, 3, 1.
		int[] order = { 4, 2, 3, 1 };
		foreach (int op in order)
		{
			Byte((V("DT" + op) << 4) + V("CF" + op));
		}
		foreach (int op in order)
		{
			Byte((V("RS" + op) << 6) + V("AR" + op));
		}
		foreach (int op in order)
		{
			Byte((V("AM" + op) << amShift) | V("D1R" + op) | (V("D1RU" + op) << 5));
		}
		foreach (int op in order)
		{
			Byte(V("D2R" + op));
		}
		foreach (int op in order)
		{
			Byte((V("DL" + op) << 4) + V("RR" + op));
		}
		foreach (int op in order)
		{
			Byte(V("TL" + op) | tlMask[op]);
		}
	}

	private void Byte(int value) => _data.Add((byte)value);

	private void Bytes(int command, string arg)
	{
		Byte(command);
		Byte(Evaluate(arg));
	}

	private void Pointer(string label)
	{
		_pointers.Add((_data.Count, label));
		Byte(0);
		Byte(0);
	}

	/// <summary>Evaluates a value: a number, a note, a drum, a constant, or a sum or product of those.</summary>
	private static int Evaluate(string expression)
	{
		expression = expression.Trim();
		int plus = FindOperator(expression, '+', '-');
		if (plus > 0)
		{
			int left = Evaluate(expression.Substring(0, plus));
			int right = Evaluate(expression.Substring(plus + 1));
			return expression[plus] == '+' ? left + right : left - right;
		}
		int times = FindOperator(expression, '*');
		if (times > 0)
		{
			return Evaluate(expression.Substring(0, times)) * Evaluate(expression.Substring(times + 1));
		}
		if (expression.StartsWith("-", StringComparison.Ordinal))
		{
			return -Evaluate(expression.Substring(1));
		}
		if (expression.StartsWith("$", StringComparison.Ordinal))
		{
			return int.Parse(expression.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
		}
		if (char.IsDigit(expression[0]))
		{
			return int.Parse(expression, CultureInfo.InvariantCulture);
		}
		return Symbol(expression);
	}

	/// <summary>Finds the last of the operators at the top level, which gives evaluation from left to right.</summary>
	private static int FindOperator(string expression, params char[] operators)
	{
		for (int i = expression.Length - 1; i > 0; i--)
		{
			if (Array.IndexOf(operators, expression[i]) >= 0)
			{
				return i;
			}
		}
		return -1;
	}

	private static int Symbol(string name)
	{
		switch (name)
		{
			case "nRst":
				return 0x80;
			case "smpsNoAttack":
				return 0xE7;
			case "nMaxPSG":
				return MaxPsgNote;
			case "panNone":
				return 0x00;
			case "panRight":
				return 0x40;
			case "panLeft":
				return 0x80;
			case "panCentre":
			case "panCenter":
				return 0xC0;
		}
		Match note = Regex.Match(name, @"^n([A-G][bs]?)(\d)$");
		if (note.Success)
		{
			string pitch = note.Groups[1].Value;
			int octave = note.Groups[2].Value[0] - '0';
			int semitone;
			if (NoteAliases.TryGetValue(pitch, out string alias))
			{
				semitone = Array.IndexOf(NoteNames, alias);
			}
			else if (pitch == "Cb")
			{
				semitone = -1;
			}
			else if (pitch == "Fb")
			{
				semitone = 4;
			}
			else if (pitch == "Es")
			{
				semitone = 5;
			}
			else if (pitch == "Bs")
			{
				semitone = 12;
			}
			else
			{
				semitone = Array.IndexOf(NoteNames, pitch);
				if (semitone < 0)
				{
					throw new FormatException($"Unknown note {name}.");
				}
			}
			return 0x81 + octave * 12 + semitone;
		}
		Match tone = Regex.Match(name, @"^fTone_([0-9A-Fa-f]{2})$");
		if (tone.Success)
		{
			return int.Parse(tone.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
		}
		Match pitchName = Regex.Match(name, @"^smpsPitch(\d\d)(lo|hi)$");
		if (pitchName.Success)
		{
			int octaves = int.Parse(pitchName.Groups[1].Value, CultureInfo.InvariantCulture);
			return pitchName.Groups[2].Value == "hi" ? octaves * 12 : (0x100 - octaves * 12) & 0xFF;
		}
		if (name == "smpsPitch00")
		{
			return 0;
		}
		int drum = Array.IndexOf(DacNames, name);
		if (drum >= 0)
		{
			return 0x81 + drum;
		}
		throw new FormatException($"Unknown symbol {name}.");
	}
}
