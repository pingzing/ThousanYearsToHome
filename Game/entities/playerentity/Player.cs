using Godot;
using System.Threading.Tasks;

namespace ThousandYearsHome.Entities.PlayerEntity
{
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
        private CollisionShape2D _hornBoxCollisionShape = null!;
        private Timer _jumpTimer = null!;
        private Timer _floorTimer = null!;
        private Timer _jumpHoldTimer = null!;

        private bool _flipH = false;

        public string CurrentAnimationName => _poseAnimator.CurrentAnimation;

        public bool Grounded => !_floorTimer.IsStopped();
        public bool Jumping => !_jumpTimer.IsStopped();
        public bool JumpHolding => !_jumpHoldTimer.IsStopped();
        public bool Crouching => _verticalUnit == -1 && Grounded;
        public bool JumpAvailable { get; set; }

        private int _horizontalUnit = 0;
        public int HorizontalUnit => _horizontalUnit;

        private int _verticalUnit = 0;
        public int VerticalUnit => _verticalUnit;

        private float _velX;
        public float VelX
        {
            get => _velX;
            set
            {
                if (value != 0)
                {
                    bool oldFlip = _flipH;
                    _flipH = value < 0;
                    if (_flipH != oldFlip)
                    {
                        _sprite.Scale = new Vector2(_sprite.Scale.x * -1, 1);
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
            set
            {
                _velY = value;
            }
        }

        [Export] public bool InputLocked = false;
        [Export] public float JumpResistance = 50f;
        [Export] public float FallGravity = 40f;
        [Export] public float MaxFallSpeed = 450f;
        [Signal] public delegate void DebugUpdateState(PlayerStateKind newState, float xVel, float yVel);

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            _poseAnimator = GetNode<AnimationPlayer>("Sprite/PoseAnimator");
            _colorAnimator = GetNode<AnimationPlayer>("Sprite/ColorAnimator");
            _sprite = GetNode<Sprite>("Sprite");
            _stateMachine = GetNode<PlayerStateMachine>("PlayerStateMachine");
            _jumpTimer = GetNode<Timer>("JumpTimer");
            _floorTimer = GetNode<Timer>("FloorTimer");
            _jumpHoldTimer = GetNode<Timer>("JumpHoldTimer");
            _hornBoxCollisionShape = GetNode<CollisionShape2D>("Sprite/HornBox/HornBoxCollisionShape");
            _stateMachine.Init(this);
        }

        public override void _PhysicsProcess(float delta)
        {
            if (InputLocked)
            {
                // Do nothing. Used to prevent player input during cutscenes, etc.
            }
            else
            {
                UpdateFromInput(delta);
                var newState = _stateMachine.Run();
                EmitSignal(nameof(DebugUpdateState), _stateMachine.CurrentState.StateKind, VelX, VelY);
            }
        }

        // Places the player at pos, unhides them, and enables their collision.
        public void Spawn(Vector2 pos)
        {
            GlobalPosition = pos;
            Show();
            GetNode<CollisionShape2D>("BodyCollisionBox").Disabled = false;
        }

        private void UpdateFromInput(float delta)
        {
            _horizontalUnit = (Input.IsActionPressed("ui_right") ? 1 : 0) - (Input.IsActionPressed("ui_left") ? 1 : 0);
            _verticalUnit = (Input.IsActionPressed("ui_up") ? 1 : 0) - (Input.IsActionPressed("ui_down") ? 1 : 0);
            if (Input.IsActionJustPressed("ui_accept"))
            {
                // Initial jump
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
        public void Move()
        {
            //var oldVel = _velocity;
            var velocity = MoveAndSlide(new Vector2(VelX, VelY), Vector2.Up, true);
            VelX = velocity.x;
            VelY = velocity.y;
            //var _collisions = GetSlideCount(); // TODO: Not sure if we need to handle this?
        }

        public void ApplyGravity(float gravity)
        {
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

        public SignalAwaiter WaitForCurrentPoseAnimationAsync()
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
    }
}
