using Godot;
using System;

namespace ThousandYearsHome.Entities.PlayerEntity
{
    [Tool]
    public class Player : KinematicBody2D
    {
        public const int StandFrame = 0;
        public const int WalkFrame1 = 1;
        public const int WalkFrame2 = 2;
        public const int WalkFrame3 = 3;

        private AnimationPlayer _poseAnimator = null!;
        private AnimationPlayer _colorAnimator = null!;
        private Sprite _sprite = null!;
        private PlayerStateMachine _stateMachine = null!;
        private CollisionShape2D _bodyCollisionBox = null!;
        private CollisionShape2D _hornBoxCollisionShape = null!;
        private Timer _jumpTimer = null!;
        private Timer _floorTimer = null!;
        private Timer _jumpHoldTimer = null!;
        private Timer _oneWayPlatformTimer = null!;
        private Timer _wallJumpLockoutTimer = null!;
        private RayCast2D _leftRaycast = null!;
        private RayCast2D _rightRaycast = null!;

        private Vector2 _snapVector = Vector2.Down * 30; // 36 is player's collision box height. Should this be dynamic?

        public string CurrentAnimationName => _poseAnimator.CurrentAnimation;

        public bool FlipH { get; private set; } = false;
        public bool Grounded => !_floorTimer.IsStopped();
        public bool Jumping => !_jumpTimer.IsStopped();
        public bool JumpHolding => !_jumpHoldTimer.IsStopped();
        public bool Crouching => _verticalUnit == -1 && Grounded;
        public bool JumpAvailable { get; set; }
        public bool IsOnOneWayPlatform { get; private set; } = false;
        public bool IsOnSlope { get; private set; } = false;
        public bool IsTouchingWall { get; private set; } = false;
        public bool IsWallJumpLocked => !_wallJumpLockoutTimer.IsStopped();
        public RayCast2D LeftRaycast => _leftRaycast;
        public RayCast2D RightRaycast => _rightRaycast;

        public int HorizontalUnit { get; set; } = 0;

        private int _verticalUnit = 0;
        public int VerticalUnit => _verticalUnit;

        public const float IdleGravity = 30f;

        private float _velX;
        public float VelX
        {
            get => _velX;
            set
            {
                if (value != 0)
                {
                    bool oldFlip = FlipH;
                    FlipH = value < 0;
                    if (FlipH != oldFlip)
                    {
                        _sprite.Scale = new Vector2(_sprite.Scale.x * -1, 1);
                        // We can't flip the entire player object, as collision doesn't function properly with negative scales.
                        // So instead, just negate the collision boxes' x-values
                        _bodyCollisionBox.Position = new Vector2(_bodyCollisionBox.Position.x * -1, _bodyCollisionBox.Position.y);
                        _leftRaycast.Position = new Vector2(_leftRaycast.Position.x * -1, _leftRaycast.Position.y);
                        _rightRaycast.Position = new Vector2(_rightRaycast.Position.x * -1, _rightRaycast.Position.y);

                        // Immediately update animation state if we've changed direction,
                        // so we don't have one frame of the old facing while moving in reverse.
                        _poseAnimator.Advance(0);
                    }
                }
                _velX = value;
            }
        }

        private float _velY;
        public float VelY
        {
            get => _velY;
            set => _velY = value;
        }

        private float _fallGravity = 30f;
        [Export]
        public float FallGravity
        {
            get => _fallGravity;
            set
            {
                _fallGravity = value;
                Update();
            }
        }

        private float _maxFallSpeed = 425f;
        [Export]
        public float MaxFallSpeed
        {
            get => _maxFallSpeed;
            set
            {
                _maxFallSpeed = value;
                Update();
            }
        }

        [Export] public bool InputLocked = false;
        [Signal] public delegate void DebugUpdateState(PlayerStateKind newState, float xVel, float yVel, Vector2 position);

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            _poseAnimator = GetNode<AnimationPlayer>("Sprite/PoseAnimator");
            _colorAnimator = GetNode<AnimationPlayer>("Sprite/ColorAnimator");
            _sprite = GetNode<Sprite>("Sprite");
            _stateMachine = GetNode<PlayerStateMachine>("PlayerStateMachine");
            _bodyCollisionBox = GetNode<CollisionShape2D>("BodyCollisionBox");
            _hornBoxCollisionShape = GetNode<CollisionShape2D>("Sprite/HornBox/HornBoxCollisionShape");
            _jumpTimer = GetNode<Timer>("JumpTimer");
            _floorTimer = GetNode<Timer>("FloorTimer");
            _jumpHoldTimer = GetNode<Timer>("JumpHoldTimer");
            _oneWayPlatformTimer = GetNode<Timer>("OneWayPlatformTimer");
            _wallJumpLockoutTimer = GetNode<Timer>("WallJumpLockoutTimer");
            _leftRaycast = GetNode<RayCast2D>("LeftRaycast");
            _rightRaycast = GetNode<RayCast2D>("RightRaycast");
            _stateMachine.Init(this);
        }

        public override void _PhysicsProcess(float delta)
        {
            if (Engine.EditorHint)
            {
                return;
            }

            if (!InputLocked)
            {
                UpdateFromInput(delta);
            }
            else
            {
                // Zero out any lingering inputs.
                HorizontalUnit = 0;
                _verticalUnit = 0;
                if (IsOnFloor()) { _floorTimer.Start(); }
            }

            var newState = _stateMachine.Run();
            EmitSignal(nameof(DebugUpdateState), _stateMachine.CurrentState.StateKind, VelX, VelY, Position);

        }

        // Places the player at pos, enables their collision, and initializes their state machine.
        public void Spawn(Vector2 pos)
        {
            GlobalPosition = pos;
            Show();
            _bodyCollisionBox.Disabled = false;

            // Make sure the initial state and collisions are taken care of.
            _stateMachine.Run();
            Move();
        }

        private void UpdateFromInput(float delta)
        {
            HorizontalUnit = (Input.IsActionPressed("ui_right") ? 1 : 0) - (Input.IsActionPressed("ui_left") ? 1 : 0);
            _verticalUnit = (Input.IsActionPressed("ui_up") ? 1 : 0) - (Input.IsActionPressed("ui_down") ? 1 : 0);

            if (Input.IsActionJustPressed("ui_accept"))
            {
                _jumpTimer.Start();
            }

            if (Input.IsActionPressed("ui_accept"))
            {
                // Holding the jump button for more air
                _jumpHoldTimer.Start();
            }

            if (IsOnFloor())
            {
                // Makes us be "on the floor" for 0.1s after leaving the ground. Refreshes every frame we're on the ground.
                // Allows for jumping a little bit after running off an edge.
                _floorTimer.Start();
            }
        }

        // Called by the state machine.
        public void Move(bool forceSnap = false)
        {
            bool previousFlipH = FlipH;
            var previousVelocity = new Vector2(VelX, VelY);

            Vector2 velocity = RunMovement(shouldSnap: forceSnap || IsOnSlope);
            VelX = velocity.x;
            VelY = velocity.y;
            IsOnOneWayPlatform = false;
            IsOnSlope = false;
            IsTouchingWall = CheckWallRaycasts();

            int slideCount = GetSlideCount();
            bool isOnFloor = IsOnFloor();
            if (slideCount > 0)
            {
                for (int i = 0; i < slideCount; i++)
                {
                    KinematicCollision2D collision = GetSlideCollision(i);
                    IsOnOneWayPlatform = GetIsOnOneWayPlatform(collision);
                    (float? newVelY, float? newVelX, bool isOnSlope) = GetSlopeAdjustment(previousFlipH, previousVelocity, isOnFloor, collision);
                    if (newVelY != null) { VelY = newVelY.Value; }
                    if (newVelX != null) { VelX = newVelX.Value; }
                    IsOnSlope = isOnSlope;
                }
            }

            if (isOnFloor && GetFloorNormal().y > -1.0)
            {
                IsOnSlope = true;
            }

            if (isOnFloor)
            {
                // Snap position to whole pixel coordinates to avoid jitter
                // but only when on the floor--it's only noticeable when moving slowly,
                // which doesn't really happen in the air.
                Position = new Vector2(Mathf.Round(Position.x), Mathf.Round(Position.y));
            }
        }

        private Vector2 RunMovement(bool shouldSnap)
        {
            // Nail player to the ground when on a slope, but allow jumping
            Vector2 velocity;
            if (shouldSnap)
            {
                Vector2 snapVector = _snapVector;
                if (_stateMachine.CurrentState.StateKind == PlayerStateKind.Jumping)
                {
                    snapVector = Vector2.Zero;
                }
                velocity = MoveAndSlideWithSnap(new Vector2(VelX, VelY), snapVector, Vector2.Up, true, 4, 1.42173f); // Approx 70 degrees
            }
            else
            {
                velocity = MoveAndSlide(new Vector2(VelX, VelY), Vector2.Up, true, 4, 1.42173f);
            }

            return velocity;
        }

        private bool CheckWallRaycasts()
        {
            if (_leftRaycast.IsColliding() || _rightRaycast.IsColliding())
            {
                Vector2 leftCollNormal = _leftRaycast.GetCollisionNormal();
                Vector2 rightCollNormal = _rightRaycast.GetCollisionNormal();
                if (leftCollNormal != Vector2.Zero || rightCollNormal != Vector2.Zero)
                {
                    return true;
                }
            }

            return false;
        }

        private (float? velY, float? velX, bool isOnSlope) GetSlopeAdjustment(bool previousFlipH, Vector2 previousVelocity, bool isOnFloor, KinematicCollision2D collision)
        {
            // Slope stuff
            // Cancel out the downward velocity from sliding on slopes
            float? newVelY = null;
            float? newVelX = null;
            bool newOnSlope = false;
            if (isOnFloor && collision.Normal.y > -1.0 && VelX != 0)
            {
                newVelY = collision.Normal.y * -1;

                // Undo slopes turning players around if they land on them straight down
                if (previousVelocity.x == 0)
                {
                    if (previousFlipH != FlipH)
                    {
                        if (previousFlipH) // Player was facing left
                        {
                            newVelX = -.1f;
                        }
                        else // Player was facing right
                        {
                            newVelX = .1f;
                        }
                    }
                }
                else
                {
                    newVelX = previousVelocity.x;
                }

                newOnSlope = true;
            }
            return (newVelY, newVelX, newOnSlope);
        }

        private bool GetIsOnOneWayPlatform(KinematicCollision2D collision)
        {
            // One-way platform handling
            if (collision.Collider is TileMap tilemap)
            {
                int collidedTileIndex = tilemap.GetCellv(tilemap.WorldToMap(collision.Position));
                if (collidedTileIndex != -1 && (
                    // TODO: Make lookup tables for each tilset and all of its one-way platform tiles instead of this silly hardcoding
                    collidedTileIndex == 12
                    || collidedTileIndex == 13
                    || collidedTileIndex == 14
                ))
                {
                    return true;
                }
            }

            return false;
        }

        // Called by the state machine.
        public void ApplyGravity(float gravity)
        {
            // No gravity on slopes, or there's too much resistance
            if (IsOnSlope)
            {
                return;
            }

            var velocity = new Vector2(VelX, VelY);
            velocity += Vector2.Down * gravity;
            VelX = velocity.x;
            VelY = Mathf.Clamp(velocity.y, -1000f, MaxFallSpeed);
        }

        public void AnimatePose(string animationName, float animationSpeed = 1.0f)
        {
            if (_poseAnimator.CurrentAnimation == animationName)
            {
                return;
            }
            _poseAnimator.Play(animationName, animationSpeed);
        }

        // TODO: Can we make this more robust? Should we?
        public SignalAwaiter WaitForCurrentPoseAnimationFinished()
        {
            return ToSignal(_poseAnimator, "animation_finished");
        }

        public void AnimateColor(string animationName, float animationSpeed = 1.0f)
        {
            if (_colorAnimator.CurrentAnimation == animationName)
            {
                return;
            }
            _colorAnimator.Play(animationName, animationSpeed);
        }

        public void ResetPoseAnimation()
        {
            _poseAnimator.Stop(true);
            _poseAnimator.ClearQueue();
        }

        public void ResetColorAnimation()
        {
            _colorAnimator.Stop(true);
            _colorAnimator.ClearQueue();
        }

        public void SetSprite(int frameIndex)
        {
            _sprite.Frame = frameIndex;
        }

        public void OnHornTouched(PowerBall ball)
        {
            // TODO: Increase stamina meter, make player glow
        }

        /// <summary>
        /// Triggers the "just dropped off a one-way platform" state.
        /// Also immediately ends the jumping state.
        /// </summary>
        public void StartOneWayPlatformTimer()
        {
            _jumpTimer.Stop();
            _oneWayPlatformTimer.Start();
        }

        /// <summary>
        /// Starts the timer that prevents player input momentarily after wall-jumping.
        /// </summary>
        public void StartWallJumpLockoutTimer()
        {
            _wallJumpLockoutTimer.Start();
        }

        public void OnOneWayPlatformTimerTimeout()
        {
            // One-way platforms are only on Layer 2. The player normally occupies 1 AND 2.
            // When passing through one-way platforms, they only occupy Layer 1.
            // Once the timer expires, this puts them back on both layers.
            CollisionLayer = 1 | 2;
        }
    }
}
