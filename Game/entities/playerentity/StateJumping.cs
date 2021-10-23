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

        private float _inAirSpeed = 175;
        // Enforce a minimum of 5 ticks of jumping, so you're always guaranteed a minimum wage^H^H^H^H jump
        private uint _ticksJumping = 0;
        private uint _minTicks = 5;
        private float _currentReduction = 0f;

        private float _initialSpeed = 550f;
        [Export]
        public float InitialSpeed
        {
            get => _initialSpeed;
            set { _initialSpeed = value; Update(); }
        }

        private float _holdingSpeed = 450f;
        [Export]
        public float HoldingSpeed
        {
            get => _holdingSpeed;
            set { _holdingSpeed = value; Update(); }
        }

        private float _reductionPerTick = 85f;
        [Export]
        public float ReductionPerTick
        {
            get => _reductionPerTick;
            set { _reductionPerTick = value; Update(); }
        }

        private float _releaseSpeed = 90f;
        [Export]
        public float ReleaseSpeed
        {
            get => _releaseSpeed;
            set { _releaseSpeed = value; Update(); }
        }

        public override Task Enter(Player player)
        {
            _ticksJumping = 0;
            _currentReduction = 0f;
            return base.Enter(player);
        }

        public override PlayerStateKind? Run(Player player)
        {
            if (!player.JumpHolding && !player.Jumping && _ticksJumping >= _minTicks)
            {
                player.VelY = -_releaseSpeed;
                return PlayerStateKind.InAir;
            }

            TickJumpState(player);

            if (_ticksJumping < _minTicks)
            {
                return null;
            }

            if ((player.Jumping || player.JumpHolding)
                && player.VelY < 0)
            {
                return null;
            }

            if (player.Jumping && player.IsTouchingWall)
            {
                if ((!player.FlipH && player.RightRaycast.IsColliding() && player.HorizontalUnit > 0)
                    || (player.FlipH && player.LeftRaycast.IsColliding() && player.HorizontalUnit < 0))
                {
                    return PlayerStateKind.WallJumping;
                }
            }

            player.VelY = -_releaseSpeed;
            return PlayerStateKind.InAir;
        }

        // Extracted to a separate method for testability
        private void TickJumpState(Player player)
        {
            player.VelX = player.HorizontalUnit * _inAirSpeed;
            float newVelY;
            if (_ticksJumping < 3)
            {
                newVelY = _initialSpeed;
            }
            else
            {
                newVelY = _holdingSpeed - _currentReduction;
            }

            player.VelY = Mathf.Min(0, -newVelY);

            player.Move();
            player.ApplyGravity(0);

            if (_ticksJumping > 10)
            {
                _currentReduction += _reductionPerTick;
            }
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

            // Simulate vertical + forward jump distance
            Enter(player);
            player.HorizontalUnit = 1;
            TickJumpState(player);
            GD.Print($"PlayerPosY: {player.Position.y}");

            List<Vector2> forwardJumpFrames = new List<Vector2>();
            while (player.VelY < 0)
            {
                forwardJumpFrames.Add(player.Position);
                TickJumpState(player);
                GD.Print($"PlayerPosY: {player.Position.y}");
            }

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
            DrawLine(new Vector2(0, adjustedJumpFrames.First().y), new Vector2(0, adjustedJumpFrames.Last().y), new Color(1, 0, 0), 1, true);
            // Arc for forward jumps
            DrawPolyline(adjustedJumpFrames.ToArray(), new Color(1, 0, 0), 1, true);
        }
    }
}
