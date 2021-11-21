using Godot;

namespace ThousandYearsHome.Entities.PlayerEntity
{
    [Tool]
    public class StateRunning : PlayerStateBase
    {
        [Export] private PlayerStateKind _stateKind = PlayerStateKind.Running;
        public override PlayerStateKind StateKind => _stateKind;

        [Export] private string _defaultAnimation = "Walk";
        public override string DefaultAnimation => _defaultAnimation;

        [Export] private float StartSpeed = 87.5f;
        [Export] private float AccelPerTick = 5f;
        [Export] private float DecelPerTick = 15f;
        [Export] private float StartingAccelPerTick = 25f;
        [Export] private float TurningAccel = 35f;
        [Export] private float MaxSpeed = 175f;
        // If we want the player to maintain normal speed, they need to move at 2x up our slopes
        // Use 1.5x to slow them down a little
        // For a more dynamic solution, we'd probably want to multiply by the actual slope angle somehow
        // For now, we only have one slope angle, so this is good enough
        [Export] private float SlopeClimbSpeedMultiplier = 1.5f;

        // Gets called once per physics tick by _PhysicsProcess.
        public override PlayerStateKind? Run(Player player)
        {
            // No player input, and we're not moving
            if (player.HorizontalUnit == 0 && player.VelX == 0)
            {
                return PlayerStateKind.Idle;
            }

            UpdateVelocity(player);

            player.ApplyGravity(Player.IdleGravity);
            player.Move();

            if (!player.IsOnFloor())
            {
                return PlayerStateKind.InAir;
            }
            if (player.Jumping)
            {
                return PlayerStateKind.Jumping;
            }

            if (player.Crouching)
            {
                return PlayerStateKind.Crouching;
            }

            if (player.IsKickJustPressed)
            {
                return PlayerStateKind.Kicking;
            }

            return null;
        }

        private void UpdateVelocity(Player player)
        {
            float velX = player.VelX;

            // If there's no player input:
            if (player.HorizontalUnit == 0)
            {
                if (velX < 0) // Going left, go from negative to zero
                {
                    velX += DecelPerTick;
                    velX = Mathf.Clamp(velX, -MaxSpeed, 0);
                }
                else // Going right, go from positive to zero
                {
                    velX -= DecelPerTick;
                    velX = Mathf.Clamp(velX, 0, MaxSpeed);
                }
            }
            else
            {
                // Start with high aceleration if we're below StartSpeed
                if (Mathf.Abs(velX) < StartSpeed)
                {
                    velX += StartingAccelPerTick * player.HorizontalUnit;
                }

                // If changing directions, use TurningAccel
                if ((player.HorizontalUnit ^ (int)velX) < 0) // Binary XOR. If we wind up with something negative, the signs were different, an indication that we're moving in a different direction than our current velocity.
                {
                    velX += TurningAccel * player.HorizontalUnit;
                }
                else
                {
                    velX += AccelPerTick * player.HorizontalUnit;
                }

                velX = Mathf.Clamp(velX, -MaxSpeed, MaxSpeed);

                // Allow player to climb up slopes at normal speed                
                if (player.IsOnSlope)
                {
                    bool isRightSlope = player.GetFloorNormal().x < 0;
                    // ...but only if they're actually climbing a slope
                    if ((player.HorizontalUnit > 0 && isRightSlope && velX > 0)
                        || (player.HorizontalUnit < 0 && !isRightSlope && velX < 0))
                    {
                        velX *= SlopeClimbSpeedMultiplier;
                    }
                }
            }

            player.VelX = velX;
        }
    }
}
