using Godot;
using System.Collections.Generic;
using System.Linq;
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

        private float _inAirSpeed = 200f;

        private float _initialImpulse = 175;
        [Export]
        public float InitialImpulse
        {
            get => _initialImpulse;
            set
            {
                _initialImpulse = value;
                Update();
            }
        }

        // Enforce a minimum of 5 ticks of jumping, so you're always guaranteed a minimum wage^H^H^H^H jump
        private uint _minTicks = 5;
        private uint _ticksJumping = 0;

        private float _reductionPerTick = 27;
        [Export]
        public float ReductionPerTick
        {
            get => _reductionPerTick;
            set
            {
                _reductionPerTick = value;
                Update();
            }
        }

        private float _jumpResistance = 30f;
        [Export]
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

            TickJumpState(player);

            if ((player.Jumping || player.JumpHolding || _ticksJumping < _minTicks)
                && player.VelY < 0)
            {
                return null;
            }

            return PlayerStateKind.InAir;
        }

        // Extracted to a separate method for testability
        private void TickJumpState(Player player)
        {
            player.VelX = player.HorizontalUnit * _inAirSpeed;
            float velYDelta = _initialImpulse - _currentReduction;
            player.VelY -= Mathf.Max(0, velYDelta);

            player.Move();
            player.ApplyGravity(_jumpResistance);

            _currentReduction += _reductionPerTick;
            _ticksJumping++;
        }

        // --- Tool stuff ---

        public override void _Draw()
        {
            // Draw jump guidelines in editor mode, or if debug Display Collision Shapes is enabled.
            if (!Engine.EditorHint && !GetTree().DebugCollisionsHint)
            {
                return;
            }

            var player = GetParent().GetParent<Player>();
            var collisionBox = player.GetNode<CollisionShape2D>("BodyCollisionBox");
            Vector2 startPos = player.Position;

            // Simulate vertical jump height
            Enter(player);
            TickJumpState(player);

            List<Vector2> upFrames = new List<Vector2>();
            while (player.VelY < 0)
            {
                upFrames.Add(player.Position);
                TickJumpState(player);
            }

            // Back to start before simulating forward
            Exit(player);
            player.VelY = 0f;
            player.Position = startPos;

            // Simulate vertical + forward jump distance
            Enter(player);
            player.HorizontalUnit = 1;
            TickJumpState(player);

            List<Vector2> forwardJumpFrames = new List<Vector2>();
            while (player.Position.y < startPos.y)
            {
                forwardJumpFrames.Add(player.Position);
                TickJumpState(player);
            }

            // One last frame to get them back to the ground
            forwardJumpFrames.Add(player.Position);

            // Back to idle and where we started
            Exit(player);
            player.HorizontalUnit = 0;
            player.VelY = 0f;
            player.VelX = 0f;
            player.Position = startPos;

            float collisionBottomY = (collisionBox.Shape as RectangleShape2D)!.Extents.y + collisionBox.Position.y;

            // Translate arc from global pos to local pos
            List<Vector2> adjustedJumpFrames = new List<Vector2>();
            adjustedJumpFrames.Add(new Vector2(0, collisionBottomY));
            for (int i = 0; i < forwardJumpFrames.Count; i++)
            {
                Vector2 diff;
                if (i == 0)
                {
                    diff = forwardJumpFrames[i] - player.Position;
                }
                else
                {
                    diff = forwardJumpFrames[i] - forwardJumpFrames[i - 1];
                }
                adjustedJumpFrames.Add(adjustedJumpFrames[i] + diff);
            }

            // Line for vertical jumps            
            Vector2 upStart = new Vector2(0, collisionBottomY);
            Vector2 upDiff = new Vector2(upFrames.Last() - upFrames.First());
            DrawLine(upStart, upStart + upDiff, new Color(1, 0, 0), 1, true);
            // Arc for forward jumps
            DrawPolyline(adjustedJumpFrames.ToArray(), new Color(1, 0, 0), 1, true);
        }
    }
}
