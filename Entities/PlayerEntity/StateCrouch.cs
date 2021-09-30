using Godot;

namespace ThousandYearsHome.Entities.PlayerEntity
{
    public class StateCrouch : PlayerStateBase
    {
        [Export] private PlayerStateKind _stateKind = PlayerStateKind.Crouching;
        public override PlayerStateKind StateKind => _stateKind;

        [Export] private string _defaultAnimation = "Crouch";
        public override string DefaultAnimation => _defaultAnimation;

        [Export] private float DecelPerTick = 20f;
        [Export] private float MaxSpeed = 200f;

        public override PlayerStateKind? Run(Player player)
        {
            if (Mathf.Abs(player.VelX) > 0) // Allow sliding crouch
            {
                float velX = player.VelX;
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
                player.VelX = velX;
            }

            player.ApplyGravity(player.Gravity);
            player.Move();

            if (!player.IsOnFloor())
            {
                return PlayerStateKind.InAir;
            }

            if (player.Jumping)
            {
                return PlayerStateKind.Jumping;
            }

            if (player.VerticalUnit != -1)
            {
                return PlayerStateKind.Idle;
            }

            return null;
        }
    }
}
