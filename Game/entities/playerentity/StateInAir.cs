using Godot;
using System;
using System.Threading.Tasks;

namespace ThousandYearsHome.Entities.PlayerEntity
{
    [Tool]
    public class StateInAir : PlayerStateBase
    {

        [Export] private PlayerStateKind _stateKind = PlayerStateKind.InAir;
        public override PlayerStateKind StateKind => _stateKind;

        [Export] private string _defaultAnimation = "InAir";
        public override string DefaultAnimation => _defaultAnimation;

        private float _inAirSpeed = 175;
        private uint _ticksInAir = 0;
        private uint _ticksTillAnimate = 5;

        public override async Task Enter(Player player)
        {
            _ticksInAir = 0;
            // todo: Check to see if this actually works
            if (player.CurrentAnimationName == "Jump")
            {
                await player.WaitForCurrentPoseAnimationFinished();
                await base.Enter(player);
            }
        }

        public override PlayerStateKind? Run(Player player)
        {
            // This will allow short falls to not flicker into the "falling" animation.
            if (_ticksInAir > _ticksTillAnimate)
            {
                player.AnimatePose(DefaultAnimation);
            }

            player.VelX = player.HorizontalUnit * _inAirSpeed;
            player.ApplyGravity(player.FallGravity);
            player.Move();

            if (player.IsOnFloor())
            {
                return player.HorizontalUnit == 0
                    ? PlayerStateKind.Idle
                    : PlayerStateKind.Running;
            }

            if (player.Grounded && player.Jumping) // Allows jumping for a split second after beginning to fall
            {
                return PlayerStateKind.Jumping;
            }

            if (player.Jumping && player.IsTouchingWall)
            {
                if ((!player.FlipH && player.RightRaycast.IsColliding() && player.HorizontalUnit > 0)
                    || (player.FlipH && player.LeftRaycast.IsColliding() && player.HorizontalUnit < 0))
                {
                    return PlayerStateKind.WallJumping;
                }
            }

            _ticksInAir++;
            return null;
        }
    }
}
