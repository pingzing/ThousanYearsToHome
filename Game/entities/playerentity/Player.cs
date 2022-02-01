using Godot;
using System;
using System.Threading.Tasks;
using ThousandYearsHome.Entities.PowerBallEntity;
using ThousandYearsHome.Entities.WarmthBallEntity;

namespace ThousandYearsHome.Entities.PlayerEntity
{
    [Tool]
    public class Player : KinematicBody2D
    {
        private AnimationPlayer _poseAnimator = null!;
        private AnimationPlayer _colorAnimator = null!;
        private Sprite _sprite = null!;
        private PlayerStateMachine _stateMachine = null!;
        private PlayerInputService _inputService = null!;
        private CollisionShape2D _bodyCollisionBox = null!;
        private CollisionShape2D _hornBoxCollisionShape = null!;
        private Timer _jumpTimer = null!;
        private Timer _floorTimer = null!;
        private Timer _jumpHoldTimer = null!;
        private Timer _oneWayPlatformTimer = null!;
        private Timer _wallJumpLockoutTimer = null!;
        private Timer _kickTimer = null!;
        private RayCast2D _leftRaycast = null!;
        private RayCast2D _rightRaycast = null!;
        private Area2D _kickHurtSentinel = null!;
        private HornCollectibleSignalBus _collectibleSignalBus = null!;
        private PlayerSignalBus _playerSignalBus = null!;

        private PlayerStateDisableToken? _stateProcessingDisableToken = null;
        private Vector2 _snapVector = Vector2.Down * 30; // 36 is player's collision box height. Should this be dynamic?

        public string CurrentAnimationName => _poseAnimator.CurrentAnimation;

        public bool FlipH { get; private set; } = false;
        public bool Grounded => !_floorTimer.IsStopped();
        public bool Jumping => !_jumpTimer.IsStopped();
        public bool JumpHolding => !_jumpHoldTimer.IsStopped();
        public bool Crouching => VerticalUnit == -1 && Grounded;
        public bool JumpAvailable { get; set; }
        public bool IsOnOneWayPlatform { get; private set; } = false;
        public bool IsOnSlope { get; private set; } = false;
        public bool IsTouchingWall { get; private set; } = false;
        public bool IsWallJumpLocked => !_wallJumpLockoutTimer.IsStopped();
        public bool IsKickJustPressed { get; private set; }
        public bool IsKicking => !_kickTimer.IsStopped();
        public Func<Player, Timer, Task>? EnterKickOverride { get; set; } = null!;
        public float IdleTime { get; private set; }
        public RayCast2D LeftRaycast => _leftRaycast;
        public RayCast2D RightRaycast => _rightRaycast;
        public Area2D KickHurtSentinel => _kickHurtSentinel;

        private float _warmth = 100f;
        /// <summary>
        /// The player's current Warmth. Ranges from 0-100, inclusive at both ends.
        /// </summary>
        public float Warmth
        {
            get => _warmth;
            set
            {
                float oldWarmth = _warmth;
                float newWarmth = Mathf.Clamp(value, 0, 100);
                if (_warmth != newWarmth)
                {
                    _warmth = newWarmth;
                    _playerSignalBus.EmitSignal(nameof(PlayerSignalBus.WarmthChanged), oldWarmth, newWarmth);
                }
            }
        }

        private float _warmthDrainPerTick = 0;
        /// <summary>
        /// Determines how much wamrth is lost each time a warmth drain timer ticks.
        /// </summary>
        public float WarmthDrainPerTick
        {
            get => _warmthDrainPerTick;
            set => _warmthDrainPerTick = value;
        }

        public int HorizontalUnit { get; set; } = 0;

        public int VerticalUnit { get; set; } = 0;

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

        [Export]
        public bool InputLocked
        {
            get => _inputService?.InputLocked ?? false;
            set
            {
                if (_inputService != null)
                {
                    _inputService.InputLocked = value;
                    if (_inputService.InputLocked)
                    {
                        _inputService.ClearInputs();
                        // Zero out any lingering inputs.
                        HorizontalUnit = 0;
                        VerticalUnit = 0;
                    }
                }
            }
        }

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            if (!Engine.EditorHint)
            {
                _inputService = GetNode<PlayerInputService>("PlayerInputService");
            }

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
            _kickTimer = GetNode<Timer>("KickTimer");
            _kickHurtSentinel = GetNode<Area2D>("Sprite/KickHurtSentinel");

            _collectibleSignalBus = GetNode<HornCollectibleSignalBus>("/root/HornCollectibleSignalBus");
            _collectibleSignalBus.Connect(nameof(HornCollectibleSignalBus.WarmthBallCollected), this, nameof(WarmthBallCollected));
            _collectibleSignalBus.Connect(nameof(HornCollectibleSignalBus.PowerBallCollected), this, nameof(PowerBallCollected));

            _playerSignalBus = GetNode<PlayerSignalBus>("/root/PlayerSignalBus");

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

            var oldPos = Position;
            var oldVel = new Vector2(VelX, VelY);

            UpdateFromInput(delta);

            if (IsOnFloor())
            {
                // Makes us be "on the floor" for 0.1s after leaving the ground. Refreshes every frame we're on the ground.
                // Allows for jumping a little bit after running off an edge.
                _floorTimer.Start();
            }

            if (_stateProcessingDisableToken == null)
            {
                PlayerStateKind oldState = _stateMachine.CurrentState.StateKind;
                PlayerStateKind newState = _stateMachine.Run();
                if (oldState != newState)
                {
                    _playerSignalBus.EmitSignal(nameof(PlayerSignalBus.StateChanged), oldState, newState);
                }
            }

            if (Grounded && _stateMachine.CurrentState.StateKind == PlayerStateKind.Idle)
            {
                IdleTime += delta;
            }
            else
            {
                IdleTime = 0;
            }

            var newVel = new Vector2(VelX, VelY);
            if (oldPos != Position) { _playerSignalBus.EmitSignal(nameof(PlayerSignalBus.PositionChanged), oldPos, Position); }
            if (oldVel != newVel) { _playerSignalBus.EmitSignal(nameof(PlayerSignalBus.VelocityChanged), oldVel, newVel); }
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
            HorizontalUnit = (_inputService.IsPressed(PlayerInputAction.Right) ? 1 : 0) - (_inputService.IsPressed(PlayerInputAction.Left) ? 1 : 0);
            VerticalUnit = (_inputService.IsPressed(PlayerInputAction.Up) ? 1 : 0) - (_inputService.IsPressed(PlayerInputAction.Down) ? 1 : 0);

            if (_inputService.IsJustPressed(PlayerInputAction.Accept))
            {
                _jumpTimer.Start();
            }

            IsKickJustPressed = _inputService.IsJustPressed(PlayerInputAction.Cancel)
                && !IsKicking;

            if (_inputService.IsPressed(PlayerInputAction.Accept))
            {
                // Holding the jump button for more air
                _jumpHoldTimer.Start();
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

        public void WarmthBallCollected(WarmthBall warmthBall)
        {
            Warmth += warmthBall.WarmthRestoration;
        }

        public void PowerBallCollected(PowerBall powerBall)
        {

        }

        // Methods primarily meant to allow external callers to manipulate the Player

        public void AnimatePose(string animationName, float animationSpeed = 1.0f)
        {
            if (_poseAnimator.CurrentAnimation == animationName)
            {
                return;
            }
            ResetPoseAnimation();
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

        public float GetPoseAnimationDuration(string animationName)
        {
            var animation = _poseAnimator.GetAnimation(animationName);
            if (animation == null)
            {
                GD.PushWarning($"Failed to get duration of animation named '{animationName}'. No animation with that name found.");
                return 0f;
            }

            return animation.Length;
        }

        /// <summary>
        ///  Disable all state processing until the returned token is disposed.
        /// </summary>
        /// <returns>A token that, when disposed, will reenable state processing.
        /// It can also be manually passed to <see cref="ReenableStateMachine(PlayerStateDisableToken)"/> to
        /// resume state processing.</returns>
        public PlayerStateDisableToken DisableStateMachine()
        {
            var token = new PlayerStateDisableToken(this);
            _stateProcessingDisableToken = token;
            return token;
        }

        public void ReenableStateMachine(PlayerStateDisableToken token)
        {
            if (token == _stateProcessingDisableToken)
            {
                _stateProcessingDisableToken = null;
            }
            else
            {
                GD.PrintErr("Attempted to unforce state with a token other than the one holding the state lock!");
            }
        }
    }
}
