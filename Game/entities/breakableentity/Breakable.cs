using Godot;

namespace ThousandYearsHome.Entities.BreakableEntity
{
    public class Breakable : Node2D
    {
        // Nodes
        private Sprite _sprite = null!;
        private Tween _tween = null!;
        private Area2D _breakableArea = null!;
        private CollisionShape2D _breakableShape = null!;

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
                _tween.InterpolateProperty(_sprite, "offset", Vector2.Zero, new Vector2(5, 0), 0.2f, Tween.TransitionType.Bounce, Tween.EaseType.OutIn);
                _tween.Start();

                // Take damage
                _hitsRemaining -= 1;

                if (_hitsRemaining <= 0)
                {
                    // If destroyed, remove collision, play animation.
                    _breakableShape.SetDeferred("disabled", true);
                    // TODO: Animation
                    _sprite.Hide();
                }
            }
        }
    }
}


