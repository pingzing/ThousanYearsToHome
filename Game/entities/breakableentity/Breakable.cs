using Godot;

namespace ThousandYearsHome.Entities.BreakableEntity
{
    public class Breakable : Node2D
    {
        [Signal] public delegate void PlayerEnteredKickRange(Breakable entity);
        [Signal] public delegate void PlayerExitedKickRange(Breakable entity);

        // Nodes
        private Sprite _sprite = null!;
        private Tween _tween = null!;
        private Area2D _breakableArea = null!;
        private CollisionShape2D _breakableShape = null!;
        private StaticBody2D _collisionBody = null!;
        private CollisionShape2D _collisionShape = null!;

        private uint _hitsToDestroy = 3;
        [Export]
        public uint HitsToDestroy
        {
            get => _hitsToDestroy;
            set => _hitsToDestroy = value;
        }

        private uint _hitsRemaining = 1;

        public override void _Ready()
        {
            _sprite = GetNode<Sprite>("Sprite");
            _tween = GetNode<Tween>("Tween");
            _breakableArea = GetNode<Area2D>("BreakableArea");
            _breakableShape = _breakableArea.GetNode<CollisionShape2D>("BreakableShape");
            _collisionBody = GetNode<StaticBody2D>("CollisionBody");
            _collisionShape = _collisionBody.GetNode<CollisionShape2D>("CollisionShape");
            _hitsRemaining = HitsToDestroy;
        }

        // To be called on player respawn to reset state
        public void Spawn()
        {
            _sprite.Show();
            _breakableShape.SetDeferred("disabled", false);
            _hitsRemaining = HitsToDestroy;
        }

        public void BreakableAreaEntered(Area2D area)
        {
            if (area.Name == "KickHurtBox")
            {
                // Animate sprite, take some damage
                // Animate
                _tween.Stop(_sprite);
                _tween.InterpolateProperty(_sprite, "offset:x", 0, 5, 0.1f, Tween.TransitionType.Bounce, Tween.EaseType.OutIn);
                _tween.InterpolateProperty(_sprite, "offset:x", 5, 0, 0.1f, Tween.TransitionType.Bounce, Tween.EaseType.OutIn, 0.1f);
                _tween.Start();

                // Take damage
                _hitsRemaining -= 1;

                if (_hitsRemaining <= 0)
                {
                    // If destroyed, remove collision, play animation.
                    _breakableShape.SetDeferred("disabled", true);
                    _collisionShape.SetDeferred("disabled", true);
                    // TODO: Animation
                    _sprite.Hide();
                }
            }

            if (area.Name == "KickHurtSentinel")
            {
                EmitSignal(nameof(PlayerEnteredKickRange), this);
            }
        }

        public void BreakableAreaExited(Area2D area)
        {
            if (area.Name == "KickHurtSentinel")
            {
                EmitSignal(nameof(PlayerExitedKickRange), this);
            }
        }
    }
}


