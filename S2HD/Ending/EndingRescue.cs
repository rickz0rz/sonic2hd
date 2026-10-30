using System;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace S2HD.Ending;

/// <summary>
/// The rescue in the ending: the Tornado (ObjCC) comes with birds (ObjCD), takes Sonic, and flies near. Then the
/// close-up of the plane comes on plane A, with its propeller (ObjCF), and Sonic jumps (ObjCE).
/// </summary>
internal sealed partial class EndingGameState
{
	/// <summary>The colour that ObjCC_Init writes to colour 0 of line 2, the backdrop: $C00, blue.</summary>
	private static readonly Colour BackdropColour = new Colour(1.0, 0.0, 0.0, 12 / 14.0);

	/// <summary>The positions of the Tornado that comes near (word_A656), one each 3 frames.</summary>
	private static readonly (int X, int Y)[] ApproachPositions =
	{
		(0xA0, 0x70), (0xB0, 0x70), (0xB6, 0x71), (0xBC, 0x72), (0xC4, 0x74), (0xC8, 0x75), (0xCA, 0x76), (0xCC, 0x77),
		(0xCE, 0x78), (0xD0, 0x79), (0xD2, 0x7A), (0xD4, 0x7B), (0xD6, 0x7C), (0xD9, 0x7E), (0xDC, 0x81), (0xDE, 0x84),
		(0xE1, 0x87), (0xE4, 0x8B), (0xE7, 0x8F), (0xEC, 0x94), (0xF0, 0x99), (0xF5, 0x9D), (0xF9, 0xA4), (0x100, 0xAC),
		(0x108, 0xB8), (0x112, 0xC4), (0x11F, 0xD3), (0x12C, 0xFA)
	};

	/// <summary>
	/// The frames of ObjCF for the Tornado that comes near: with Sonic on it (byte_A602), without him for Super Sonic
	/// (byte_A61E), and with Tails on it (byte_A63A).
	/// </summary>
	private int[] ApproachFrames => _variant switch
	{
		EndingCharacter.SuperSonic => new[] { 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4 },
		EndingCharacter.Tails => new[] { 0x18, 0x18, 0x18, 0x18, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 0x19, 9, 9, 9, 0xA, 0xA, 0xA, 0xB, 0xB, 0xB, 0xB, 0xB, 0xB, 0xB, 0xB, 0xB, 0xB, 0xB },
		_ => new[] { 7, 7, 7, 7, 8, 8, 8, 8, 8, 8, 8, 9, 9, 9, 0xA, 0xA, 0xA, 0xB, 0xB, 0xB, 0xB, 0xB, 0xB, 0xB, 0xB, 0xB, 0xB, 0xB }
	};

	/// <summary>The positions of Super Sonic who flies past the plane (word_A766), and the frames of ObjCF for them (byte_A748).</summary>
	private static readonly (int X, int Y)[] SuperFlightPositions =
	{
		(0xC0, 0x90), (0xB0, 0x91), (0xA8, 0x92), (0x9B, 0x96), (0x99, 0x98), (0x98, 0x99), (0x99, 0x9A), (0x9B, 0x9C),
		(0x9F, 0x9E), (0xA4, 0xA0), (0xAC, 0xA2), (0xB7, 0xA5), (0xC4, 0xA8), (0xD3, 0xAB), (0xDE, 0xAE), (0xE8, 0xB0),
		(0xEF, 0xB2), (0xF4, 0xB5), (0xF9, 0xB8), (0xFC, 0xBB), (0xFE, 0xBE), (0xFF, 0xC0), (0x100, 0xC2), (0x101, 0xC5),
		(0x102, 0xC8), (0x102, 0xCC), (0x101, 0xD1), (0xFD, 0xD7), (0xF9, 0xDE), (0xF9, 0x118)
	};

	private static readonly int[] SuperFlightFrames =
	{
		0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x13, 0x13, 0x13, 0x13, 0x13, 0x13, 0x14, 0x14, 0x14,
		0x14, 0x15, 0x15, 0x15, 0x16, 0x16, 0x16, 0x16, 0x16, 0x16, 0x16, 0x16, 0x16, 0
	};

	/// <summary>The positions of Super Sonic who flies away from the close-up (word_A874).</summary>
	private static readonly (int X, int Y)[] SuperLeavePositions =
	{
		(0x60, 0x88), (0x50, 0x68), (0x44, 0x46), (0x3C, 0x36), (0x36, 0x2A), (0x33, 0x24), (0x31, 0x20), (0x30, 0x1E)
	};

	/// <summary>The animations of the PLANE group: N is frame N with palette line 0, and this number plus N is frame N with line 1.</summary>
	private const int PlaneFrameCount = 26;

	/// <summary>The scroll of plane A that brings the close-up on the screen (word_A822): x, then y, each 3 frames.</summary>
	private static readonly (int X, int Y)[] CloseUpSteps =
	{
		(-0x3A, 0x88), (-0xC, 0x22), (-8, 0x10), (-4, 8), (-2, 4), (-1, 2), (-1, 2)
	};

	/// <summary>The jump of Sonic from the close-up (byte_A980): the change of the position at each step.</summary>
	private static readonly (int X, int Y)[] JumpSteps = { (-8, 0), (-0x44, -0x38) };

	/// <summary>The frames of the pilot Sonic, as indices of his pilot group (Sonic_pilot_frames).</summary>
	private static readonly int[] SonicPilotSequence = { 0, 1, 2, 3 };

	/// <summary>The frames of the pilot, Tails, as indices of the pilot group (Tails_pilot_frames).</summary>
	private static readonly int[] PilotSequence = { 0, 0, 0, 0, 1, 2, 3, 2, 1, 1, 0, 0, 0, 0, 1, 2, 3, 2, 1, 1, 4, 4, 1, 1 };

	/// <summary>The position of the close-up on plane A, in pixels (planeLoc(64,22,33)). Plane A is 512 x 512 pixels.</summary>
	private const int CloseUpX = 22 * TileSize;

	private const int CloseUpY = 33 * TileSize;

	private const int PlaneSize = 512;

	private const int CloseUpWidth = 23 * TileSize;

	private const int CloseUpHeight = 15 * TileSize;

	/// <summary>CutScene+$34: the Tornado came, so the clouds fly from the right.</summary>
	private bool _cloudsSideways;

	/// <summary>Ending_VInt_Subrout 4: plane B is empty, and plane A has the close-up.</summary>
	private bool _backgroundCleared;

	/// <summary>The scroll of plane A: Horiz_Scroll_Buf and Vscroll_Factor_FG.</summary>
	private int _planeScrollX;

	private int _planeScrollY;

	private EndingSprite _tornado;

	private EndingSprite _pilot;

	/// <summary>ObjCC_Init: the Tornado comes from the left, below the screen.</summary>
	private EndingSprite CreateTornado()
	{
		_backgroundCleared = true;
		_tornado = new EndingSprite(_tornadoGroup)
		{
			Priority = 3,
			Line = 0,
			// ObjB2_SubObjData: make_art_tile(ArtTile_ArtNem_Tornado,0,1)
			HighPriority = true,
			X = -0x10,
			Y = 0xC0,
			XVelocity = 0x100,
			YVelocity = -0x80,
			// objoff_35: the number of birds
			Saved = 0x14
		};
		// ObjCC_Init: when Tails plays alone, the Tornado has Sonic as the pilot (frames 4 to 7, Ani_objB2_a 1)
		if (_variant == EndingCharacter.Tails)
		{
			_tornado.SetScript(0, 4, 5, 6, 7);
		}
		else
		{
			_tornado.SetScript(0, 0, 1, 2, 3);
		}
		_tornado.Update = UpdateTornado;
		_pilot = new EndingSprite(_pilotGroup) { Priority = 3, Line = 0, HighPriority = true };
		AddSprite(_pilot);
		return _tornado;
	}

	/// <summary>ObjCC_Main.</summary>
	private void UpdateTornado(EndingSprite tornado)
	{
		UpdatePilot(tornado);
		switch (tornado.Routine)
		{
			case 0:
				// loc_A474: fly in until x is $A0
				if ((int)Math.Floor(tornado.X) != 0xA0)
				{
					tornado.Move();
					break;
				}
				tornado.Routine = 1;
				tornado.Timer = 0x480;
				tornado.BaseY = 0x40;
				tornado.XVelocity = 0;
				tornado.YVelocity = 0;
				_cloudsSideways = true;
				break;
			case 1:
				// loc_A4B6: the birds come, and the character comes to the plane (sub_A524)
				SpawnBirds(tornado);
				PlaceCharacterOnPlane(tornado);
				if (--tornado.Timer >= 0)
				{
					break;
				}
				// The Tornado comes near: the frames of ObjCF, with the character in them
				tornado.Routine = 2;
				tornado.Timer = 2;
				tornado.Index = 0;
				tornado.Script = null;
				tornado.Group = _planeGroup;
				// make_art_tile(ArtTile_ArtKos_LevelArt,0,0): without the priority bit
				tornado.HighPriority = false;
				tornado.Animation = ApproachFrames[0];
				tornado.X -= 0x14;
				tornado.Y += 0x14;
				_pilot.Visible = false;
				HideCharacterUnlessSuper();
				break;
			case 2:
				// loc_A5A6: a new position and frame each 3 frames
				HideCharacterUnlessSuper();
				if (--tornado.Timer >= 0)
				{
					break;
				}
				tornado.Timer = 2;
				if (tornado.Index < ApproachFrames.Length)
				{
					tornado.Animation = ApproachFrames[tornado.Index];
					(tornado.X, tornado.Y) = ApproachPositions[tornado.Index];
					tornado.Index++;
					break;
				}
				tornado.Routine = 3;
				tornado.Timer = 0x60;
				tornado.Index = 0;
				tornado.Saved = 0;
				break;
			case 3:
				// loc_A6C6: a wait. Super Sonic flies past the plane in this time.
				if (--tornado.Timer >= 0)
				{
					if (_variant == EndingCharacter.SuperSonic)
					{
						UpdateSuperSonicFlight(tornado);
					}
					break;
				}
				// loc_A720: the close-up with the propeller, and the character who jumps from it (not Super Sonic)
				tornado.Routine = 4;
				tornado.Timer = 0;
				tornado.Index = 0;
				AddSprite(CreatePropeller());
				if (_variant != EndingCharacter.SuperSonic)
				{
					AddSprite(CreateJumper());
				}
				break;
			case 4:
				// loc_A7DE: plane A scrolls the close-up on the screen
				_character.Visible = false;
				if (--tornado.Timer >= 0)
				{
					break;
				}
				tornado.Timer = 2;
				if (tornado.Index < CloseUpSteps.Length)
				{
					_planeScrollX += CloseUpSteps[tornado.Index].X;
					_planeScrollY += CloseUpSteps[tornado.Index].Y;
					tornado.Index++;
					break;
				}
				// status.npc.p1_standing: the character jumps
				tornado.Routine = 5;
				tornado.Index = 0;
				tornado.Saved = 0;
				break;
			case 5:
				// loc_A83E: Super Sonic flies away from the close-up
				if (_variant == EndingCharacter.SuperSonic)
				{
					UpdateSuperSonicLeave(tornado);
				}
				break;
		}
	}

	/// <summary>
	/// sub_A524: Sonic and Tails stand on the plane. Super Sonic comes down to the plane for $40 frames, then stays
	/// next to it.
	/// </summary>
	private void PlaceCharacterOnPlane(EndingSprite tornado)
	{
		switch (_variant)
		{
			case EndingCharacter.SuperSonic:
				// loc_A55C: half a pixel to the right and one pixel down on each frame
				if (tornado.BaseY > 0)
				{
					tornado.BaseY--;
					_character.X += 0.5;
					_character.Y += 1;
					break;
				}
				_character.X = 0xC0;
				_character.Y = 0x90;
				break;
			default:
				// loc_A53A and loc_A582: the character waits on the plane
				_character.X = tornado.X;
				_character.Y = tornado.Y - (_variant == EndingCharacter.Tails ? 0x18 : 0x1C);
				_character.Animation = EndingArtAnimations.Wait;
				_character.Frame = 0;
				break;
		}
	}

	/// <summary>sub_A58C: the frames of ObjCF have Sonic and Tails in them, so the character object goes away. Super Sonic stays.</summary>
	private void HideCharacterUnlessSuper()
	{
		if (_variant != EndingCharacter.SuperSonic)
		{
			_character.Visible = false;
		}
	}

	/// <summary>
	/// loc_A6C6 for Super Sonic: each 3 frames, the next of the 30 positions of word_A766. For the first 3, Super Sonic
	/// moves. Then the frames of ObjCF (byte_A748) show him, and the object of the character goes away.
	/// </summary>
	private void UpdateSuperSonicFlight(EndingSprite tornado)
	{
		if (--tornado.Saved >= 0)
		{
			return;
		}
		tornado.Saved += 3;
		int index = tornado.Index++;
		if (index >= SuperFlightPositions.Length)
		{
			return;
		}
		(int x, int y) = SuperFlightPositions[index];
		if (index < 3)
		{
			_character.X = x;
			_character.Y = y;
			return;
		}
		_character.Visible = false;
		tornado.X = x;
		tornado.Y = y;
		tornado.Animation = SuperFlightFrames[index];
	}

	/// <summary>loc_A83E: Super Sonic (frame $17) flies to the top left, each 3 frames to the next position of word_A874.</summary>
	private void UpdateSuperSonicLeave(EndingSprite tornado)
	{
		tornado.Animation = 0x17;
		if (--tornado.Saved >= 0)
		{
			return;
		}
		tornado.Saved += 3;
		if (tornado.Index < SuperLeavePositions.Length)
		{
			(tornado.X, tornado.Y) = SuperLeavePositions[tornado.Index++];
		}
	}

	/// <summary>ObjB2_Animate_Pilot: the next frame of the pilot each 9 frames. The pilot is at the place of the Tornado.</summary>
	private void UpdatePilot(EndingSprite tornado)
	{
		_pilot.X = tornado.X;
		_pilot.Y = tornado.Y;
		if (--_pilot.Timer >= 0)
		{
			return;
		}
		_pilot.Timer = 8;
		// Sonic_pilot_frames has 4 frames, Tails_pilot_frames 24
		int[] sequence = _variant == EndingCharacter.Tails ? SonicPilotSequence : PilotSequence;
		_pilot.Index = (_pilot.Index + 1) % sequence.Length;
		_pilot.Animation = sequence[_pilot.Index];
	}

	/// <summary>sub_ABBA: a bird at random intervals of up to $F frames, $14 birds in all.</summary>
	private void SpawnBirds(EndingSprite tornado)
	{
		if (--tornado.BaseX >= 0 || tornado.Saved == 0)
		{
			return;
		}
		tornado.Saved--;
		tornado.BaseX = (int)(_random.Seed & 0xF);
		AddSprite(CreateBird());
	}

	/// <summary>ObjCD: a bird that flies in from the left, flies up and down with the plane, and flies away.</summary>
	private EndingSprite CreateBird()
	{
		// ObjCD_Init stores the seed after the first ror.l, and turns a copy once more for the height
		uint seed = _random.RotateRight(3);
		int y = (int)((seed >> 3 | seed << 29) & 0xFF);
		EndingSprite bird = new EndingSprite(_birdsGroup)
		{
			// The original uses priority 2 here, not 1, so the birds are in front of the plane but behind Sonic
			Priority = 2,
			Line = 0,
			X = -0xA0 + (int)(seed & 0x7F),
			Y = 8 + y,
			XVelocity = 0x100,
			YVelocity = y < 0x20 ? 0x20 : -0x20,
			Timer = 0xC0
		};
		bird.SetScript(5, 0, 1);
		bird.Update = UpdateBird;
		return bird;
	}

	/// <summary>ObjCD_Main.</summary>
	private static void UpdateBird(EndingSprite bird)
	{
		switch (bird.Routine)
		{
			case 0:
				// loc_AB34: fly in, then stop and fly up and down around this height
				if (--bird.Timer < 0)
				{
					bird.Routine = 1;
					bird.Saved = bird.YVelocity;
					bird.XVelocity = 0;
					bird.BaseY = (int)Math.Floor(bird.Y);
					bird.YVelocity = 0x80;
					bird.Timer = 0x180;
				}
				break;
			case 1:
				// loc_AB5C: the speed turns towards the height of the stop
				if (--bird.Timer < 0)
				{
					bird.Routine = 2;
					bird.XVelocity = -0x100;
					bird.YVelocity = bird.Saved;
					bird.Timer = 0xC0;
					break;
				}
				bird.YVelocity += (ushort)(int)Math.Floor(bird.Y) >= (ushort)bird.BaseY ? -4 : 4;
				break;
			case 2:
				// loc_AB8E: fly away to the left, then go
				if (--bird.Timer < 0)
				{
					bird.Deleted = true;
					return;
				}
				break;
		}
		bird.Move();
	}

	/// <summary>ObjCF: the propeller of the close-up, which moves with plane A.</summary>
	private EndingSprite CreatePropeller()
	{
		EndingSprite propeller = new EndingSprite(_planeGroup) { Priority = 3, Line = 0, HighPriority = true, BaseX = 0x10F, BaseY = 0x15E };
		propeller.SetScript(1, 5, 6);
		propeller.Update = sprite => FollowPlane(sprite);
		FollowPlane(propeller);
		return propeller;
	}

	/// <summary>
	/// ObjCE: the character in the close-up: Sonic (frame $C), or Tails (frame $F with palette line 1). He moves with
	/// plane A, then jumps when the close-up stops.
	/// </summary>
	private EndingSprite CreateJumper()
	{
		int firstFrame = _variant == EndingCharacter.Tails ? PlaneFrameCount + 0xF : 0xC;
		EndingSprite jumper = new EndingSprite(_planeGroup)
		{
			Priority = 1,
			Line = _variant == EndingCharacter.Tails ? 1 : 0,
			HighPriority = true,
			Animation = firstFrame,
			BaseX = 0xE8,
			BaseY = 0x118
		};
		jumper.Update = sprite =>
		{
			switch (sprite.Routine)
			{
				case 0:
					// loc_A902: move with plane A until the Tornado says that the close-up stopped
					if (_tornado.Routine < 5)
					{
						FollowPlane(sprite);
						break;
					}
					sprite.Routine = 1;
					sprite.Timer = 0;
					break;
				case 1:
					// loc_A936: two steps of the jump, each 5 frames
					if (--sprite.Timer >= 0)
					{
						break;
					}
					sprite.Timer = 4;
					if (sprite.Index >= JumpSteps.Length)
					{
						sprite.Routine = 2;
						break;
					}
					sprite.X += JumpSteps[sprite.Index].X;
					sprite.Y += JumpSteps[sprite.Index].Y;
					sprite.Animation++;
					sprite.Index++;
					break;
			}
		};
		FollowPlane(jumper);
		return jumper;
	}

	/// <summary>loc_A90E: the position of an object on plane A, with the scroll of plane A.</summary>
	private void FollowPlane(EndingSprite sprite)
	{
		sprite.X = sprite.BaseX + _planeScrollX;
		sprite.Y = sprite.BaseY - _planeScrollY;
	}

	/// <summary>Draws the close-up on plane A, at its place with the scroll of plane A. Plane A repeats every 512 pixels.</summary>
	private void DrawCloseUp(I2dRenderer g)
	{
		if (!_backgroundCleared || _closeUp == null)
		{
			return;
		}
		int x = ((CloseUpX + _planeScrollX) % PlaneSize + PlaneSize) % PlaneSize;
		int y = ((CloseUpY - _planeScrollY) % PlaneSize + PlaneSize) % PlaneSize;
		if (x >= ScreenWidth || y >= ScreenHeight)
		{
			return;
		}
		g.Colour = Colours.White;
		// The close-up fills its place of 23 x 15 tiles of the original, whatever the size of the image
		g.RenderTexture(_closeUp, new Rectangle(x * ArtScale, y * ArtScale, CloseUpWidth * ArtScale, CloseUpHeight * ArtScale));
	}
}
