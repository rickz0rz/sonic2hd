using System.Collections;
using System.Collections.Generic;
using SonicOrca.Extensions;

namespace SonicOrca.Input;

/// <summary>
/// The up or down state of each key, indexed by SDL scancode. The KEY_ constants are the SDL scancode values.
/// </summary>
public sealed class KeyboardState : IReadOnlyList<bool>, IReadOnlyCollection<bool>, IEnumerable<bool>, IEnumerable
{
	private const int KeyCount = 512;

	public const int KEY_UNKNOWN = 0;

	public const int KEY_A = 4;

	public const int KEY_B = 5;

	public const int KEY_C = 6;

	public const int KEY_D = 7;

	public const int KEY_E = 8;

	public const int KEY_F = 9;

	public const int KEY_G = 10;

	public const int KEY_H = 11;

	public const int KEY_I = 12;

	public const int KEY_J = 13;

	public const int KEY_K = 14;

	public const int KEY_L = 15;

	public const int KEY_M = 16;

	public const int KEY_N = 17;

	public const int KEY_O = 18;

	public const int KEY_P = 19;

	public const int KEY_Q = 20;

	public const int KEY_R = 21;

	public const int KEY_S = 22;

	public const int KEY_T = 23;

	public const int KEY_U = 24;

	public const int KEY_V = 25;

	public const int KEY_W = 26;

	public const int KEY_X = 27;

	public const int KEY_Y = 28;

	public const int KEY_Z = 29;

	public const int KEY_1 = 30;

	public const int KEY_2 = 31;

	public const int KEY_3 = 32;

	public const int KEY_4 = 33;

	public const int KEY_5 = 34;

	public const int KEY_6 = 35;

	public const int KEY_7 = 36;

	public const int KEY_8 = 37;

	public const int KEY_9 = 38;

	public const int KEY_0 = 39;

	public const int KEY_RETURN = 40;

	public const int KEY_ESCAPE = 41;

	public const int KEY_BACKSPACE = 42;

	public const int KEY_TAB = 43;

	public const int KEY_SPACE = 44;

	public const int KEY_MINUS = 45;

	public const int KEY_EQUALS = 46;

	public const int KEY_LEFTBRACKET = 47;

	public const int KEY_RIGHTBRACKET = 48;

	public const int KEY_BACKSLASH = 49;

	public const int KEY_NONUSHASH = 50;

	public const int KEY_SEMICOLON = 51;

	public const int KEY_APOSTROPHE = 52;

	public const int KEY_GRAVE = 53;

	public const int KEY_COMMA = 54;

	public const int KEY_PERIOD = 55;

	public const int KEY_SLASH = 56;

	public const int KEY_CAPSLOCK = 57;

	public const int KEY_F1 = 58;

	public const int KEY_F2 = 59;

	public const int KEY_F3 = 60;

	public const int KEY_F4 = 61;

	public const int KEY_F5 = 62;

	public const int KEY_F6 = 63;

	public const int KEY_F7 = 64;

	public const int KEY_F8 = 65;

	public const int KEY_F9 = 66;

	public const int KEY_F10 = 67;

	public const int KEY_F11 = 68;

	public const int KEY_F12 = 69;

	public const int KEY_PRINTSCREEN = 70;

	public const int KEY_SCROLLLOCK = 71;

	public const int KEY_PAUSE = 72;

	public const int KEY_INSERT = 73;

	public const int KEY_HOME = 74;

	public const int KEY_PAGEUP = 75;

	public const int KEY_DELETE = 76;

	public const int KEY_END = 77;

	public const int KEY_PAGEDOWN = 78;

	public const int KEY_RIGHT = 79;

	public const int KEY_LEFT = 80;

	public const int KEY_DOWN = 81;

	public const int KEY_UP = 82;

	public const int KEY_NUMLOCKCLEAR = 83;

	public const int KEY_KP_DIVIDE = 84;

	public const int KEY_KP_MULTIPLY = 85;

	public const int KEY_KP_MINUS = 86;

	public const int KEY_KP_PLUS = 87;

	public const int KEY_KP_ENTER = 88;

	public const int KEY_KP_1 = 89;

	public const int KEY_KP_2 = 90;

	public const int KEY_KP_3 = 91;

	public const int KEY_KP_4 = 92;

	public const int KEY_KP_5 = 93;

	public const int KEY_KP_6 = 94;

	public const int KEY_KP_7 = 95;

	public const int KEY_KP_8 = 96;

	public const int KEY_KP_9 = 97;

	public const int KEY_KP_0 = 98;

	public const int KEY_KP_PERIOD = 99;

	public const int KEY_NONUSBACKSLASH = 100;

	public const int KEY_APPLICATION = 101;

	public const int KEY_POWER = 102;

	public const int KEY_KP_EQUALS = 103;

	public const int KEY_F13 = 104;

	public const int KEY_F14 = 105;

	public const int KEY_F15 = 106;

	public const int KEY_F16 = 107;

	public const int KEY_F17 = 108;

	public const int KEY_F18 = 109;

	public const int KEY_F19 = 110;

	public const int KEY_F20 = 111;

	public const int KEY_F21 = 112;

	public const int KEY_F22 = 113;

	public const int KEY_F23 = 114;

	public const int KEY_F24 = 115;

	public const int KEY_EXECUTE = 116;

	public const int KEY_HELP = 117;

	public const int KEY_MENU = 118;

	public const int KEY_SELECT = 119;

	public const int KEY_STOP = 120;

	public const int KEY_AGAIN = 121;

	public const int KEY_UNDO = 122;

	public const int KEY_CUT = 123;

	public const int KEY_COPY = 124;

	public const int KEY_PASTE = 125;

	public const int KEY_FIND = 126;

	public const int KEY_MUTE = 127;

	public const int KEY_VOLUMEUP = 128;

	public const int KEY_VOLUMEDOWN = 129;

	public const int KEY_KP_COMMA = 133;

	public const int KEY_KP_EQUALSAS400 = 134;

	public const int KEY_INTERNATIONAL1 = 135;

	public const int KEY_INTERNATIONAL2 = 136;

	public const int KEY_INTERNATIONAL3 = 137;

	public const int KEY_INTERNATIONAL4 = 138;

	public const int KEY_INTERNATIONAL5 = 139;

	public const int KEY_INTERNATIONAL6 = 140;

	public const int KEY_INTERNATIONAL7 = 141;

	public const int KEY_INTERNATIONAL8 = 142;

	public const int KEY_INTERNATIONAL9 = 143;

	public const int KEY_LANG1 = 144;

	public const int KEY_LANG2 = 145;

	public const int KEY_LANG3 = 146;

	public const int KEY_LANG4 = 147;

	public const int KEY_LANG5 = 148;

	public const int KEY_LANG6 = 149;

	public const int KEY_LANG7 = 150;

	public const int KEY_LANG8 = 151;

	public const int KEY_LANG9 = 152;

	public const int KEY_ALTERASE = 153;

	public const int KEY_SYSREQ = 154;

	public const int KEY_CANCEL = 155;

	public const int KEY_CLEAR = 156;

	public const int KEY_PRIOR = 157;

	public const int KEY_RETURN2 = 158;

	public const int KEY_SEPARATOR = 159;

	public const int KEY_OUT = 160;

	public const int KEY_OPER = 161;

	public const int KEY_CLEARAGAIN = 162;

	public const int KEY_CRSEL = 163;

	public const int KEY_EXSEL = 164;

	public const int KEY_KP_00 = 176;

	public const int KEY_KP_000 = 177;

	public const int KEY_THOUSANDSSEPARATOR = 178;

	public const int KEY_DECIMALSEPARATOR = 179;

	public const int KEY_CURRENCYUNIT = 180;

	public const int KEY_CURRENCYSUBUNIT = 181;

	public const int KEY_KP_LEFTPAREN = 182;

	public const int KEY_KP_RIGHTPAREN = 183;

	public const int KEY_KP_LEFTBRACE = 184;

	public const int KEY_KP_RIGHTBRACE = 185;

	public const int KEY_KP_TAB = 186;

	public const int KEY_KP_BACKSPACE = 187;

	public const int KEY_KP_A = 188;

	public const int KEY_KP_B = 189;

	public const int KEY_KP_C = 190;

	public const int KEY_KP_D = 191;

	public const int KEY_KP_E = 192;

	public const int KEY_KP_F = 193;

	public const int KEY_KP_XOR = 194;

	public const int KEY_KP_POWER = 195;

	public const int KEY_KP_PERCENT = 196;

	public const int KEY_KP_LESS = 197;

	public const int KEY_KP_GREATER = 198;

	public const int KEY_KP_AMPERSAND = 199;

	public const int KEY_KP_DBLAMPERSAND = 200;

	public const int KEY_KP_VERTICALBAR = 201;

	public const int KEY_KP_DBLVERTICALBAR = 202;

	public const int KEY_KP_COLON = 203;

	public const int KEY_KP_HASH = 204;

	public const int KEY_KP_SPACE = 205;

	public const int KEY_KP_AT = 206;

	public const int KEY_KP_EXCLAM = 207;

	public const int KEY_KP_MEMSTORE = 208;

	public const int KEY_KP_MEMRECALL = 209;

	public const int KEY_KP_MEMCLEAR = 210;

	public const int KEY_KP_MEMADD = 211;

	public const int KEY_KP_MEMSUBTRACT = 212;

	public const int KEY_KP_MEMMULTIPLY = 213;

	public const int KEY_KP_MEMDIVIDE = 214;

	public const int KEY_KP_PLUSMINUS = 215;

	public const int KEY_KP_CLEAR = 216;

	public const int KEY_KP_CLEARENTRY = 217;

	public const int KEY_KP_BINARY = 218;

	public const int KEY_KP_OCTAL = 219;

	public const int KEY_KP_DECIMAL = 220;

	public const int KEY_KP_HEXADECIMAL = 221;

	public const int KEY_LCTRL = 224;

	public const int KEY_LSHIFT = 225;

	public const int KEY_LALT = 226;

	public const int KEY_LGUI = 227;

	public const int KEY_RCTRL = 228;

	public const int KEY_RSHIFT = 229;

	public const int KEY_RALT = 230;

	public const int KEY_RGUI = 231;

	public const int KEY_MODE = 257;

	public const int KEY_AUDIONEXT = 258;

	public const int KEY_AUDIOPREV = 259;

	public const int KEY_AUDIOSTOP = 260;

	public const int KEY_AUDIOPLAY = 261;

	public const int KEY_AUDIOMUTE = 262;

	public const int KEY_MEDIASELECT = 263;

	public const int KEY_WWW = 264;

	public const int KEY_MAIL = 265;

	public const int KEY_CALCULATOR = 266;

	public const int KEY_COMPUTER = 267;

	public const int KEY_AC_SEARCH = 268;

	public const int KEY_AC_HOME = 269;

	public const int KEY_AC_BACK = 270;

	public const int KEY_AC_FORWARD = 271;

	public const int KEY_AC_STOP = 272;

	public const int KEY_AC_REFRESH = 273;

	public const int KEY_AC_BOOKMARKS = 274;

	public const int KEY_BRIGHTNESSDOWN = 275;

	public const int KEY_BRIGHTNESSUP = 276;

	public const int KEY_DISPLAYSWITCH = 277;

	public const int KEY_KBDILLUMTOGGLE = 278;

	public const int KEY_KBDILLUMDOWN = 279;

	public const int KEY_KBDILLUMUP = 280;

	public const int KEY_EJECT = 281;

	public const int KEY_SLEEP = 282;

	public const int KEY_APP1 = 283;

	public const int KEY_APP2 = 284;

	private readonly bool[] _keys;

	public IReadOnlyList<bool> Keys => _keys;

	public bool this[int index] => _keys[index];

	public IEnumerable<int> ActiveKeys
	{
		get
		{
			for (int i = 0; i < _keys.Length; i++)
			{
				if (_keys[i])
				{
					yield return i;
				}
			}
		}
	}

	public int Count => _keys.Length;

	public KeyboardState()
	{
		_keys = new bool[KeyCount];
	}

	public KeyboardState(bool[] keys)
	{
		_keys = keys;
	}

	public static KeyboardState GetPressed(KeyboardState previousState, KeyboardState nextState)
	{
		KeyboardState pressed = new KeyboardState();
		for (int i = 0; i < previousState._keys.Length; i++)
		{
			pressed._keys[i] = !previousState._keys[i] && nextState._keys[i];
		}
		return pressed;
	}

	public static KeyboardState GetReleased(KeyboardState previousState, KeyboardState nextState)
	{
		KeyboardState released = new KeyboardState();
		for (int i = 0; i < previousState._keys.Length; i++)
		{
			released._keys[i] = previousState._keys[i] && !nextState._keys[i];
		}
		return released;
	}

	public IEnumerator<bool> GetEnumerator()
	{
		return _keys.GetEnumeratorGeneric();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
