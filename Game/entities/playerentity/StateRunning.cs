using Godot;

namespace ThousandYearsHome.Entities.PlayerEntity
{
    public class StateRunning : PlayerStateBase
    {
        [Export] private PlayerStateKind _stateKind = PlayerStateKind.Running;
        public override PlayerStateKind StateKind => _stateKind;

        [Export] private string _defaultAnimation = "Walk";
        public override string DefaultAnimation => _defaultAnimation;

        [Export] private float StartSpeed = 100f;
        [Export] private float AccelPerTick = 5f;
        [Export] private float DecelPerTick = 15f;
        [Export] private float StartingAccelPerTick = 25f;
        [Export] private float TurningAccel = 20f;
        [Export] private float MaxSpeed = 200f;

        // Gets called once per physics tick by _PhysicsProcess.
        public override PlayerStateKind? Run(Player player)
        {
            // Player is pressing up or down
            if (player.VerticalUnit > 0)
            {
                // example does some layer shenanigans for 1-way platforms
            }

            // No player input, and we're not moving
            if (player.HorizontalUnit == 0 && player.VelX == 0)
            {
                return PlayerStateKind.Idle;
            }

            UpdateVelocity(player);

            player.ApplyGravity(player.JumpResistance);
            player.Move();

            if (!player.IsOnFloor())
            {
                return PlayerStateKind.InAir;
            }
            if (player.VelY > 0)
            {
                player.VelY = 0;
            }
            if (player.Jumping)
            {
                return PlayerStateKind.Jumping;
            }

            if (player.Crouching)
            {
                return PlayerStateKind.Crouching;
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
            }

            player.VelX = velX;
        }
    }
}
