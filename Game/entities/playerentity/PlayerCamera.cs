using Godot;

namespace ThousandYearsHome.Entities.PlayerEntity
{
    [Tool]
    public class PlayerCamera : Node2D
    {

        /**
         * Facing left:                 60% | 40%       = 0.6 coeffeicient
         * Facing right:                40% | 60%       = 0.4 coefficient
         * Facing left, extreme view:   100% | 0%       = 1.0 coefficient
         * Facing right extreme view:   0% | 100%       = 0.0 coeffecicient
         */
        private float _leftFacingXOffset = .6f;
        private float _rightFacingXOffset = .4f;

        // Amount visible in front of player
        private float _horizontalVisible = .6f;
        [Export(PropertyHint.Range, "0.0, 1.0, 0.01")] 
        public float HorizontalVisible
        {
            get => _horizontalVisible;
            set
            {
                _horizontalVisible = value;
                _leftFacingXOffset = value;
                _rightFacingXOffset = 1.0f - value;
            }
        }

        // Amount visible above player

        private float _verticalVisible = .4f;
        private float VerticalVisible
        {
            get => _verticalVisible;
            set
            {
                _verticalVisible = value;
            }
        }

        private float _xPerTick = .20f;
        private float _targetXOffset;
        private float _currentXOffset;

        // Nodes
        private Player _player = null!;

        public override void _Ready()
        {
            if (Engine.EditorHint)
            {
                return;
            }

            var parentNode = GetParent() as Player;
            if (parentNode == null)
            {
                UpdateConfigurationWarning();
                return;
            }
            _player = GetParent<Player>();
        }

        /* TODOS:
         * Tween between current/desired x positions
         * Only does so if:
         *      - Stationary for at least 1 second
         *      - Currently moving in the direction of the target offset
         * X tween speed increases if player begins moving in direction of tween
         * Tweens between current/desired y position
         * Only does so if:
         *      - On the ground, and the difference is at least (???)
         *      - In the air, and the difference is at least (???)
         * Hard limits (i.e. level boundaries)         
         */

        public override void _PhysicsProcess(float delta)
        {
            if (Engine.EditorHint)
            {
                return;
            }

            // TODO: Only get viewport size if we know it's changed
            Viewport viewport = GetViewport();
            Rect2 viewportRect = GetViewportRect();
            Vector2 screenSize = viewportRect.Size;
            Vector2 playerPos = _player.Position;
            var transform = viewport.CanvasTransform;
            _targetXOffset = _player.FlipH ? _leftFacingXOffset : _rightFacingXOffset;
            if (_currentXOffset != _targetXOffset)
            {
                if (_currentXOffset > _targetXOffset)
                {
                    _currentXOffset -= _xPerTick * delta;
                }
                if (_currentXOffset < _targetXOffset)
                {
                    _currentXOffset += _xPerTick * delta;
                }
            }            

            // TODO: Don't scroll Y if we're in the air unless we a) hit about the 70% mark (i.e. below), b) hit about the 20% mark (i.e. above) or c) land on something above/below us.
            var newPosition = -playerPos + new Vector2(screenSize.x * _currentXOffset, screenSize.y * _verticalVisible);

            viewport.CanvasTransform = new Transform2D(transform.x, transform.y, newPosition);
        }

        public override string _GetConfigurationWarning()
        {
            var parentNode = GetParent() as Player;
            if (parentNode == null)
            {
                return "This node must be parented to a Player node.";
            }

            return "";
        }

        public override void _EnterTree()
        {
            base._EnterTree();
            UpdateConfigurationWarning();
        }

        // Tool drawing stuff


    }
}

