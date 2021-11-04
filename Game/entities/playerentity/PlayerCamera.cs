using Godot;

namespace ThousandYearsHome.Entities.PlayerEntity
{
    [Tool]
    public class PlayerCamera : Node2D
    {
        // Native resolution, NOT actual physical window sizes.
        private const float ResolutionWidth = 480f;
        private const float ResolutionHeight = 270f;

        /**
         * Facing left:                 60% | 40%       = 0.6 coeffeicient
         * Facing right:                40% | 60%       = 0.4 coefficient
         * Facing left, extreme view:   100% | 0%       = 1.0 coefficient
         * Facing right extreme view:   0% | 100%       = 0.0 coeffecicient
         */

        private float _xPerTick = 10f;

        // Nodes
        private Player _player = null!;

        private int _leftLimit = 0;
        [Export] public int LeftLimit { get => _leftLimit; set { _leftLimit = value; Update(); } }

        private int _rightLimit = 2000;
        [Export] public int RightLimit { get => _rightLimit; set { _rightLimit = value; Update(); } }

        private int _topLimit = -400;
        [Export] public int TopLimit { get => _topLimit; set { _topLimit = value; Update(); } }

        private int _bottomLimit = 200;
        [Export]
        public int BottomLimit { get => _bottomLimit; set { _bottomLimit = value; Update(); } }

        [Export]
        public Rect2 TargetRect = new Rect2(144, 0, 160, ResolutionHeight);

        /* TODOS:
         * Tween between current/desired x positions
         * Only does so if:
         *      - Stationary for at least 1 second OR
         *      - Currently moving in the direction of the target offset
         * X tween speed increases if player begins moving in direction of tween
         * Tweens between current/desired y position
         * Only does so if:
         *      - On the ground, and the difference is at least (???)
         *      - In the air, and the difference is at least (???)
         *  Also, handle ParallaxBackground scroll offset stuff (maybe just with signals or something)
         */

        private string _groupName = null!;
        private string _canvasGroupName = null!;
        private Viewport _viewport = null!;
        private RID _canvas = null!;
        private float _currentXOffset = 0;
        private float _currentYOffset = 0;

        public override void _Ready()
        {
            _player = GetParent<Player>();
        }

        public override void _PhysicsProcess(float delta)
        {
            if (Engine.EditorHint)
            {
                return;
            }

            Transform2D transform = _viewport.CanvasTransform;
            Rect2 viewportRect = GetViewportRect();
            Vector2 screenSize = viewportRect.Size;
            Vector2 playerPos = _player.Position;
            Vector2 relativePlayerPos = _player.GetGlobalTransformWithCanvas()[2];

            if (relativePlayerPos.x > TargetRect.End.x)
            {
                _currentXOffset -= (relativePlayerPos.x - TargetRect.End.x);
            }
            if (relativePlayerPos.x < TargetRect.Position.x)
            {
                _currentXOffset += (TargetRect.Position.x - relativePlayerPos.x);
            }
            _currentXOffset = Mathf.Clamp(_currentXOffset, -RightLimit, -LeftLimit);

            _currentYOffset = -playerPos.y + screenSize.y / 2;
            _currentYOffset = Mathf.Clamp(_currentYOffset, -BottomLimit, -TopLimit);

            // Clamp to limits
            var newPosition = new Vector2(_currentXOffset, _currentYOffset);

            _viewport.CanvasTransform = new Transform2D(transform.x, transform.y, newPosition);

            // TODO: Make this call so that ParallaxBackgrounds will get the memo
            //GetTree().CallGroupFlags((int)SceneTree.GroupCallFlags.Realtime, _groupName, "_camera_moved", null, null); // cameraTransform, screenOffset
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
            UpdateConfigurationWarning();

            // Doing this stuff to talk to the same group that ParallaxBackgrouns and Camera2Ds are in
            _viewport = GetViewport();
            _canvas = GetCanvas();
            var vpRid = _viewport.GetViewportRid();

            _groupName = $"__cameras_{_viewport.GetInstanceId()}";
            _canvasGroupName = $"__cameras_c{_canvas.GetId()}";

            AddToGroup(_groupName);
            AddToGroup(_canvasGroupName);
        }

        public override void _ExitTree()
        {
            // Doing this stuff to talk to the same group that ParallaxBackgrouns and Camera2Ds are in
            RemoveFromGroup(_groupName);
            RemoveFromGroup(_canvasGroupName);
        }

        // Tool drawing stuff
        public override void _Draw()
        {
            if (!Engine.EditorHint && !GetTree().DebugCollisionsHint)
            {
                return;
            }

            // TODO: Draw TargetRect in screen space

        }

    }
}

