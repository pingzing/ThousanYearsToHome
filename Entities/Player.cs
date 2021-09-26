using Godot;

namespace ThousandYearsHome.Entities
{
    public class Player : Area2D
    {
        public const int StandFrame = 0;
        public const int WalkFrame1 = 1;
        public const int WalkFrame2 = 2;
        public const int WalkFrame3 = 3;

        private Vector2 _screenSize;
        private AnimationPlayer _animationPlayer = null!;
        private Sprite _sprite = null!;

        [Export] private float MaxSpeed = 200;
        [Export] public bool InputLocked = false;

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            _screenSize = GetViewport().Size;
            Hide();
            _animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
            _sprite = GetNode<Sprite>("Sprite");
        }

        // Places the player at pos, unhides them, and enables their collision.
        public void Spawn(Vector2 pos)
        {
            GlobalPosition = pos;
            Show();
            GetNode<CollisionShape2D>("CollisionShape2D").Disabled = false;
        }

        // Called every frame. 'delta' is the elapsed time since the previous frame.
        public override void _Process(float delta)
        {
            if (InputLocked)
            {

            }
            else
            {
                UpdateFromInput(delta);
            }
        }

        private void UpdateFromInput(float delta)
        {
            Vector2 velocity = new Vector2();

            // Player-controlled movement
            if (Input.IsActionPressed("ui_right"))
            {
                velocity.x += 1;
            }

            if (Input.IsActionPressed("ui_left"))
            {
                velocity.x -= 1;
            }

            // Make sure diagonal movement isn't faster than straight movement.
            // And apply the player's current MaxSpeed to their velocity.
            if (velocity.Length() > 0)
            {
                velocity = velocity.Normalized() * MaxSpeed;
            }


            Position += velocity * delta;
            Position = new Vector2(
                x: Mathf.Clamp(Position.x, 0, _screenSize.x),
                y: Mathf.Clamp(Position.y, 0, _screenSize.y)
            );

            if (velocity.x != 0)
            {
                float animationSpeed = Mathf.Abs(velocity.x) / MaxSpeed;

                _animationPlayer.PlaybackSpeed = Mathf.Clamp(animationSpeed, 0.2f, 2.0f);
                _animationPlayer.Play("Walk");
                bool oldFlip = _sprite.FlipH;
                _sprite.FlipH = velocity.x < 0;

                // Immediately update animation state if we've changed direction,
                // so we don't have one frame of the old facing while moving in reverse.
                if (_sprite.FlipH != oldFlip)
                {
                    _animationPlayer.Advance(0);
                }
            }
            else
            {
                _animationPlayer.Play("Stand");

            }
        }

        public void PlayAnimation(string animationName, float animationSpeed = 1.0f)
        {
            _animationPlayer.Play(animationName, animationSpeed);
        }

        public void SetSprite(int frameIndex)
        {
            _sprite.Frame = frameIndex;
        }
    }
}
