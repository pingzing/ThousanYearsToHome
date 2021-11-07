using Godot;
using System.Threading.Tasks;
using ThousandYearsHome.Extensions;

namespace ThousandYearsHome.Entities.PlayerEntity
{
    [Tool]
    public class PlayerCamera : Node2D
    {
        // ------ Private variables ------
        // Native resolution, NOT actual physical window sizes.
        private const float ResolutionWidth = 480f;
        private const float ResolutionHeight = 270f;

        // These two group names are used to keep Camera2Ds and ParallaxBackgrounds in sync and SUPER UNDOCUMENTED
        private string _groupName = null!;
        private string _canvasGroupName = null!;

        private PlayerStateKind _currentPlayerState = PlayerStateKind.Idle;
        private Viewport _viewport = null!;
        private float? _xAxisLock = null;
        private float? _yAxisLock = null;
        private float _currentXOffset = 0;
        private float _currentYOffset = 0;
        private float _xPerSecond = 150f;
        private float _yPerSecond = 200f;
        private Rect2 _currentRect = new Rect2(64, ResolutionHeight * .6f, 40, 16);
        private Rect2? _overrideRect = null;

        // ------ Nodes ------
        private Player _player = null!;
        private Timer _panningTimer = null!;
        private Tween _tween = null!;

        // ------ Exports ------
        private int _leftLimit = 0;
        [Export] public int LeftLimit { get => _leftLimit; set { _leftLimit = value; Update(); } }

        private int _rightLimit = 2000;
        [Export] public int RightLimit { get => _rightLimit; set { _rightLimit = value; Update(); } }

        private int _topLimit = -400;
        [Export] public int TopLimit { get => _topLimit; set { _topLimit = value; Update(); } }

        private int _bottomLimit = 200;
        [Export]
        public int BottomLimit { get => _bottomLimit; set { _bottomLimit = value; Update(); } }

        [Export(PropertyHint.Range, "1.0, 2.0, .01")]
        public float MovementAccelerationCoefficient = 1.1f;

        [Export(PropertyHint.Range, "0, 480, 1")]
        public Rect2 DefaultIdleRect = new Rect2(64, ResolutionHeight * .6f, 40, 16);

        [Export(PropertyHint.Range, "0, 480, 1")]
        public Rect2 TargetRect = new Rect2(64, ResolutionHeight * .6f, 40, 16);

        private bool _isCameraCurrent = true;
        [Export]
        public bool Current
        {
            get => _isCameraCurrent;
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
                        _isCameraCurrent = true;
                    }
                    UpdateScroll();
                }
                _isCameraCurrent = value;
                Update();
            }
        }

        // ------ Overrides ------

        public override void _Ready()
        {
            _player = GetParent<Player>();
            _panningTimer = GetNode<Timer>("PanningTimer");
            _tween = GetNode<Tween>("Tween");
            _prevIdleFacingRight = !_player.FlipH;
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

        // ------  Public-facing API ------ 

        public Task LockXAxis(float xCoord)
        {
            _xAxisLock = -_viewport.CanvasTransform.origin.x;
            _tween.InterpolateMethod(this, nameof(AnimateXLock), _xAxisLock, xCoord, 1.0f, Tween.TransitionType.Linear, Tween.EaseType.Out);
            _tween.Start();
            return this.ToSignalWithArg(_tween, "tween_completed", 0, this);
        }

        private void AnimateXLock(float intermediateX)
        {
            _xAxisLock = intermediateX;
        }

        public void UnlockXAxis()
        {
            _tween.Stop(this, nameof(AnimateXLock));
            Vector2 relativePlayerPos = _player.GetGlobalTransformWithCanvas().origin;
            _currentRect = new Rect2(relativePlayerPos.x, _currentRect.Position.y, _currentRect.Size);
            _xAxisLock = null;
        }

        public void OverrideTargetRect(Rect2 overrideRect)
        {
            _overrideRect = overrideRect;
        }

        // ------ Internal state-keeping methods ------

        // Signal handler, listens for signals that can be sent by either this, or any Camera2D
        public void _make_current(Object which)
        {
            if (which == this)
            {
                _isCameraCurrent = true;
            }
            else
            {
                _isCameraCurrent = false;
            }
        }

        public void PlayerStateChanged(PlayerStateKind oldState, PlayerStateKind newState)
        {
            _currentPlayerState = newState;
        }

        private PlayerStateKind _prevState = PlayerStateKind.Idle;
        private bool _prevIdleFacingRight = false;
        private void UpdateCameraRect(PlayerStateKind current)
        {
            bool playerFacingRight = !_player.FlipH;
            bool instantX = false; // Make any update to the _currentRect's Position.x instant instead of interpolated.
            bool instantY = false; // Ditto _currentRect.Position.y
            bool instantWidth = false; // Ditto _currentRect.Size.Width
            bool instantHeight = false; // Ditto _currentRect.Size.Height
            Vector2 relativePlayerPos = _player.GetGlobalTransformWithCanvas().origin;

            if (current == PlayerStateKind.Jumping || current == PlayerStateKind.InAir)
            {
                // 32 pixels from the top of the screen
                TargetRect = new Rect2(TargetRect.Position.x, 32, TargetRect.Size.x, TargetRect.Size.y + TargetRect.Position.y - 32);
                instantHeight = true;
                instantY = true;
            }

            if (current == PlayerStateKind.Idle)
            {
                float targetY = DefaultIdleRect.Position.y;
                float targetHeight = DefaultIdleRect.Size.y;

                // Snap the Top of the rectangle to just above the player head, so that it will begin panning up immediately
                // now that they've landed.
                if (_prevState == PlayerStateKind.Jumping || _prevState == PlayerStateKind.InAir)
                {
                    instantHeight = true;
                    instantY = true;
                    targetY = relativePlayerPos.y;
                    targetHeight = TargetRect.End.y - targetY;
                    targetHeight = Mathf.Max(DefaultIdleRect.Size.y, targetHeight);
                }

                // If the player has just turned around, and is staying idle, hold camera position for one second.
                if (_prevIdleFacingRight != playerFacingRight
                    || !_panningTimer.IsStopped())
                {
                    TargetRect = new Rect2(_currentRect.Position.x, targetY, _currentRect.Size.x, targetHeight);
                    if (_panningTimer.IsStopped())
                    {
                        _panningTimer.Start();
                    }
                }
                else
                {
                    float targetX = playerFacingRight ? DefaultIdleRect.Position.x : ResolutionWidth - DefaultIdleRect.Position.x - DefaultIdleRect.Size.x;
                    TargetRect = new Rect2(targetX, targetY, _currentRect.Size.x, targetHeight);
                }

                _prevIdleFacingRight = playerFacingRight;
            }

            if (current == PlayerStateKind.Running)
            {
                float targetY = DefaultIdleRect.Position.y;
                float targetHeight = DefaultIdleRect.Size.y;
                if (_prevState == PlayerStateKind.Jumping || _prevState == PlayerStateKind.InAir)
                {
                    instantHeight = true;
                    instantY = true;
                    targetY = relativePlayerPos.y;
                    targetHeight = TargetRect.End.y - targetY;
                }

                if (_currentRect.HasPoint(relativePlayerPos)) // If they haven't excdeed the bounds of the box, don't do anything.
                {
                    TargetRect = _currentRect;
                }
                else
                {
                    float targetX = playerFacingRight ? DefaultIdleRect.Position.x : ResolutionWidth - DefaultIdleRect.Position.x - DefaultIdleRect.Size.x;
                    TargetRect = new Rect2(targetX, targetY, DefaultIdleRect.Size.x, targetHeight);
                }
            }

            // Gradually pan _currentRect toward TargetRect
            float delta = GetPhysicsProcessDeltaTime();
            float moveAccel = _player.HorizontalUnit != 0 ? MovementAccelerationCoefficient : 1.0f;

            float newX = instantX ? TargetRect.Position.x : Mathf.MoveToward(_currentRect.Position.x, TargetRect.Position.x, (_xPerSecond * delta) * moveAccel);
            float newWidth = instantWidth ? TargetRect.Size.x : Mathf.MoveToward(_currentRect.Size.x, TargetRect.Size.x, (_xPerSecond * delta) * moveAccel);
            float newY = instantY ? TargetRect.Position.y : Mathf.MoveToward(_currentRect.Position.y, TargetRect.Position.y, _yPerSecond * delta);
            float newHeight = instantHeight ? TargetRect.Size.y : Mathf.MoveToward(_currentRect.Size.y, TargetRect.Size.y, _yPerSecond * delta);

            _prevState = current;
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
            if (newTransform != _viewport.CanvasTransform)
            {
                _viewport.CanvasTransform = newTransform;
                // Method call that ParallaxBackgrounds will hear, and scroll in response to.
                GetTree().CallGroupFlags((int)SceneTree.GroupCallFlags.Realtime, _groupName, "_camera_moved", newTransform, Vector2.Zero);
            }
        }

        private Transform2D GetCameraTransform()
        {
            Transform2D transform = _viewport.CanvasTransform;
            Vector2 relativePlayerPos = _player.GetGlobalTransformWithCanvas().origin;

            if (_xAxisLock == null)
            {
                if (relativePlayerPos.x > _currentRect.End.x)
                {
                    _currentXOffset -= (relativePlayerPos.x - _currentRect.End.x);
                }
                if (relativePlayerPos.x < _currentRect.Position.x)
                {
                    _currentXOffset += (_currentRect.Position.x - relativePlayerPos.x);
                }
                _currentXOffset = Mathf.Clamp(_currentXOffset, -RightLimit + ResolutionWidth, -LeftLimit);
            }
            else
            {
                _currentXOffset = -_xAxisLock.Value;
            }

            if (relativePlayerPos.y > _currentRect.End.y)
            {
                _currentYOffset -= (relativePlayerPos.y - _currentRect.End.y);
            }
            if (relativePlayerPos.y < _currentRect.Position.y)
            {
                _currentYOffset += (_currentRect.Position.y - relativePlayerPos.y);
            }
            _currentYOffset = Mathf.Clamp(_currentYOffset, -BottomLimit + ResolutionHeight, -TopLimit);

            _currentXOffset = Mathf.Round(_currentXOffset);
            _currentYOffset = Mathf.Round(_currentYOffset);

            GD.Print($"Update offset: {_currentXOffset}, {_currentYOffset}");

            return new Transform2D(transform.x, transform.y, new Vector2(_currentXOffset, _currentYOffset));
        }

        // ------ Debug drawing methods ------
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

