using Godot;
using System.Threading.Tasks;

namespace ThousandYearsHome.Entities.PlayerEntity
{
    [Tool]
    public class StateJumping : PlayerStateBase
    {
        [Export] private PlayerStateKind _stateKind = PlayerStateKind.Jumping;
        public override PlayerStateKind StateKind => _stateKind;

        [Export] private string _defaultAnimation = "Jump";
        public override string DefaultAnimation => _defaultAnimation;

        private float _initialImpulse = 175;
        private float _reductionPerTick = 27;
        private float _inAirSpeed = 200f;

        // Enforce a minimum of 5 ticks of jumping, so that teeny tiny taps won't 
        // squirt you off the floor by two pixels.
        private uint _minTicks = 5;
        private uint _ticksJumping = 0;

        private float _jumpResistance = 30f;
        //[Export]
        public float JumpResistance
        {
            get => _jumpResistance;
            set
            {
                _jumpResistance = value;
                Update();
            }
        }

        private float _currentReduction = 0f;

        public override Task Enter(Player player)
        {
            _ticksJumping = 0;
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
            float velYDelta = _initialImpulse - _currentReduction;
            player.VelY -= Mathf.Max(0, velYDelta);

            player.Move();
            player.ApplyGravity(_jumpResistance);

            _currentReduction += _reductionPerTick;
            _ticksJumping++;

            if ((player.Jumping || player.JumpHolding || _ticksJumping < _minTicks)
                && player.VelY < 0)
            {
                return null;
            }

            return PlayerStateKind.InAir;
        }
    }
}
