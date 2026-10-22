using System.Collections.Generic;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca.Core.Objects;

public interface ICharacter : IActiveObject
{
	CharacterSpecialState SpecialState { get; set; }

	bool Jumped { get; set; }

	bool IsJumping { get; set; }

	bool IsObjectControlled { get; set; }

	bool ShouldReactToLevel { get; }

	IReadOnlyList<CharacterHistoryItem> History { get; }

	ActiveObject ObjectLink { get; set; }

	object ObjectTag { get; set; }

	int Path { get; set; }

	CharacterState StateFlags { get; set; }

	AnimationInstance Animation { get; }

	Player Player { get; set; }

	bool IsSidekick { get; set; }

	Vector2 Velocity { get; set; }

	double GroundVelocity { get; set; }

	CharacterLookDirection LookDirection { get; set; }

	double ShowAngle { get; set; }

	int LookDelay { get; set; }

	CharacterInputState Input { get; set; }

	int Facing { get; set; }

	bool IsAirborne { get; set; }

	bool IsDeadly { get; }

	bool IsSpinball { get; set; }

	bool IsUnderwater { get; set; }

	bool IsRollJumping { get; set; }

	bool IsHurt { get; set; }

	int BreathTicks { get; set; }

	BarrierType Barrier { get; set; }

	bool HasBarrier { get; }

	bool ForceSpinball { get; set; }

	bool IsCharging { get; }

	bool Respawning { get; }

	bool ExhibitsVirtualPlatform { get; set; }

	bool IsDebug { get; set; }

	double TumbleAngle { get; set; }

	int TumbleTurns { get; set; }

	bool HasSpeedShoes { get; set; }

	bool IsInvincible { get; set; }

	int SlopeLockTicks { get; set; }

	bool IsPushing { get; set; }

	bool IsDead { get; set; }

	bool IsDying { get; set; }

	bool IsWinning { get; set; }

	CameraProperties CameraProperties { get; set; }

	bool CheckCollision { get; set; }

	bool CheckLandscapeCollision { get; set; }

	bool CheckObjectCollision { get; set; }

	int LedgeSensorRadius { get; }

	Vector2i NormalCollisionRadius { get; }

	Vector2i SpinballCollisionRadius { get; }

	Vector2i CollisionRadius { get; }

	CollisionVector GroundVector { get; }

	CollisionMode Mode { get; set; }

	bool CanBeHurt { get; }

	void Hurt(int direction, PlayerDeathCause cause = PlayerDeathCause.Hurt);

	void Kill(PlayerDeathCause cause);

	void InhaleOxygen();

	void LeaveGround();
}
