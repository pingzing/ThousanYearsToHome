using Godot;

namespace ThousandYearsHome.Entities.PlayerEntity
{
    [Tool]
    public class PlayerCamera : Node2D
    {
        // Native resolution, NOT actual physical window sizes.
        private const float ResolutionWidth = 480f;
        private const float ResolutionHeight = 270f;
        private const string DebugCanvasGroupName = "_PlayerCameraDebugGroup";

        // These two group names are used to keep Camera2Ds and ParallaxBackgrounds in sync and SUPER UNDOCUMENTED
        private string _groupName = null!;
        private string _canvasGroupName = null!;
        private PlayerStateKind _currentPlayerState = PlayerStateKind.Idle;
        private Viewport _viewport = null!;
        private float _currentXOffset = 0;
        private float _currentYOffset = 0;
        private float _xPerSecond = 150f;
        private float _yPerSecond = 200f;
        private Rect2 _currentRect = new Rect2(64, 32, 80, ResolutionHeight * .6f);

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

        [Export(PropertyHint.Range, "0, 480, 1")]
        public Rect2 TargetRect = new Rect2(64, 16, 80, ResolutionHeight * .6f);

        private bool _current = true;
        [Export]
        public bool Current
        {
            get => _current;
            set
            {
                if (value)
                {
                    if (IsInsideTree())
                    {
                        // Tell all other Camera2Ds to stop being Current, and this one to start.
                        GetTree().CallGroupFlags((int)SceneTree.GroupCallFlags.Realtime, _groupName, "_make_current", this);
                    }
                    else
                    {
                        _current = true;
                    }
                    UpdateScroll();
                }
                _current = value;
                Update();
            }
        }

        // Overrides

        public override void _Ready()
        {
            _player = GetParent<Player>();
        }

        public override void _Process(float delta)
        {
            if (Engine.EditorHint || GetTree().DebugCollisionsHint)
            {
                Update(); // Schedule new draw call to draw camera box
            }
        }

        public override void _PhysicsProcess(float delta)
        {
            if (Engine.EditorHint)
            {
                return;
            }

            UpdateCameraRect(_currentPlayerState);
            UpdateScroll();
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
            RID canvas = GetCanvas();

            // Warning: Super undocumented behavior! Beware!
            _groupName = $"__cameras_{_viewport.GetViewportRid().GetId()}";
            _canvasGroupName = $"__cameras_c{canvas.GetId()}";

            AddToGroup(_groupName);
            AddToGroup(_canvasGroupName);
        }


        public override void _ExitTree()
        {
            // Doing this stuff to stop talking to the same group that ParallaxBackgrouns and Camera2Ds are in
            RemoveFromGroup(_groupName);
            RemoveFromGroup(_canvasGroupName);
        }

        // Internal state-keeping methods

        // Signal handler, listens for signals that can be sent by either this, or any Camera2D
        public void _make_current(Object which)
        {
            if (which == this)
            {
                _current = true;
            }
            else
            {
                _current = false;
            }
        }

        public void PlayerStateChanged(PlayerStateKind oldState, PlayerStateKind newState)
        {
            _currentPlayerState = newState;
        }

        private void UpdateCameraRect(PlayerStateKind current)
        {
            bool facingRight = !_player.FlipH;
            bool instantX = false;
            bool instantY = false;
            bool instantWidth = false;
            bool instantHeight = false;
            if (current == PlayerStateKind.Jumping || current == PlayerStateKind.InAir)
            {
                TargetRect = new Rect2(TargetRect.Position.x, 32, TargetRect.Size.x, TargetRect.Size.y + TargetRect.Position.y - 32);
                instantHeight = true;
                instantY = true;
            }
            if (current == PlayerStateKind.Idle || current == PlayerStateKind.Running)
            {
                // TODO: uh, everything
                float targetX = facingRight ? 64 : ResolutionWidth - 64 - 110;
                TargetRect = new Rect2(targetX, 110, 80, 16);
            }

            // TODO: other states

            // Gradually pan _currentRect toward TargetRect
            float delta = GetPhysicsProcessDeltaTime();
            float newX = instantX ? TargetRect.Position.x : Mathf.MoveToward(_currentRect.Position.x, TargetRect.Position.x, _xPerSecond * delta);
            float newY = instantY ? TargetRect.Position.y : Mathf.MoveToward(_currentRect.Position.y, TargetRect.Position.y, _yPerSecond * delta);
            float newWidth = instantWidth ? TargetRect.Size.x : Mathf.MoveToward(_currentRect.Size.x, TargetRect.Size.x, _xPerSecond * delta);
            float newHeight = instantHeight ? TargetRect.Size.y : Mathf.MoveToward(_currentRect.Size.y, TargetRect.Size.y, _yPerSecond * delta);
            _currentRect = new Rect2(newX, newY, newWidth, newHeight);
        }

        private void UpdateScroll()
        {
            if (!IsInsideTree())
            {
                return;
            }

            if (Engine.EditorHint)
            {
                Update();
                return;
            }

            if (!Current)
            {
                return;
            }

            var newTransform = GetCameraTransform();
            _viewport.CanvasTransform = newTransform;

            // Method call that ParallaxBackgrounds will hear, and scroll in response to.
            GetTree().CallGroupFlags((int)SceneTree.GroupCallFlags.Realtime, _groupName, "_camera_moved", Transform, newTransform.origin);
        }

        private Transform2D GetCameraTransform()
        {
            Transform2D transform = _viewport.CanvasTransform;
            Vector2 relativePlayerPos = _player.GetGlobalTransformWithCanvas().origin;

            float delta = GetPhysicsProcessDeltaTime();

            if (relativePlayerPos.x > _currentRect.End.x)
            {
                _currentXOffset -= (relativePlayerPos.x - _currentRect.End.x);
            }
            if (relativePlayerPos.x < _currentRect.Position.x)
            {
                _currentXOffset += (_currentRect.Position.x - relativePlayerPos.x);
            }
            _currentXOffset = Mathf.Clamp(_currentXOffset, -RightLimit, -LeftLimit);

            if (relativePlayerPos.y > _currentRect.End.y)
            {
                _currentYOffset -= (relativePlayerPos.y - _currentRect.End.y);
            }
            if (relativePlayerPos.y < _currentRect.Position.y)
            {
                _currentYOffset += (_currentRect.Position.y - relativePlayerPos.y);
            }
            _currentYOffset = Mathf.Clamp(_currentYOffset, -BottomLimit, -TopLimit);

            return new Transform2D(transform.x, transform.y, new Vector2(_currentXOffset, _currentYOffset));
        }

        // Debug drawing methods
        public override void _Draw()
        {
            if (Engine.EditorHint || GetTree().DebugCollisionsHint)
            {
                var inverseTransform = GetGlobalTransformWithCanvas().Inverse();
                var debugCurrentRect = new Rect2(inverseTransform.origin + _currentRect.Position, _currentRect.Size);
                var debugTargetRect = new Rect2(inverseTransform.origin + TargetRect.Position, TargetRect.Size);
                DrawRect(debugCurrentRect, new Color(1f, 1f, 0), filled: false, width: 2);
                DrawRect(debugTargetRect, new Color(1f, .75f, .55f), filled: false, width: 1);
            }
        }
    }
}

