using Godot;
using System.Threading.Tasks;

namespace ThousandYearsHome.Entities.PlayerEntity
{
    public class StateJumping : PlayerStateBase
    {
        [Export] private PlayerStateKind _stateKind = PlayerStateKind.Jumping;
        public override PlayerStateKind StateKind => _stateKind;

        [Export] private string _defaultAnimation = "Jump";
        public override string DefaultAnimation => _defaultAnimation;

        [Export] private float _initialImpulse = 440;
        [Export] private float _reductionPerTick = 15f;
        [Export] private float _inAirSpeed = 200f;

        private float _currentReduction = 0f;

        public override Task Enter(Player player)
        {
            _currentReduction = 0f;
            return base.Enter(player);
        }

        public override PlayerStateKind? Run(Player player)
        {
            if (!player.JumpHolding && !player.Jumping)
            {
                return PlayerStateKind.InAir;
            }

            player.VelX = player.HorizontalUnit * _inAirSpeed;
            player.VelY = Mathf.Min(0, -_initialImpulse + _currentReduction);
            player.Move();
            player.ApplyGravity(player.JumpResistance);
            _currentReduction += _reductionPerTick;

            if (player.Jumping
                || player.JumpHolding
                && player.VelY < 0)
            {
                return null;
            }

            return PlayerStateKind.InAir;
        }
    }
}
