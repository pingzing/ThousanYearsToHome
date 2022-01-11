using Godot;
using System;
using System.Threading;
using System.Threading.Tasks;
using ThousandYearsHome.Entities.PlayerEntity;
using ThousandYearsHome.Extensions;

namespace ThousandYearsHome.Controls
{
    [Tool]
    public class CameraLimitArea : Area2D
    {
        // Exports
        private NodePath? _playerCameraPath = null;
        [Export]
        public NodePath? PlayerCameraPath
        {
            get => _playerCameraPath;
            set => _playerCameraPath = value;
        }
        private PlayerCamera? _playerCamera = null;

        private float _limitLeft;
        [Export(PropertyHint.Range, "-9999999, 9999999, 1")]
        public float LimitLeft
        {
            get => _limitLeft;
            set { _limitLeft = value; Update(); }
        }

        private float _limitRight;
        [Export(PropertyHint.Range, "-9999999, 9999999, 1")]
        public float LimitRight
        {
            get => _limitRight;
            set { _limitRight = value; Update(); }
        }

        private float _limitTop;
        [Export(PropertyHint.Range, "-9999999, 9999999, 1")]
        public float LimitTop
        {
            get => _limitTop;
            set { _limitTop = value; Update(); }
        }

        private float _limitBottom;
        [Export(PropertyHint.Range, "-9999999, 9999999, 1")]
        public float LimitBottom
        {
            get => _limitBottom;
            set { _limitBottom = value; Update(); }
        }

        private float _activationDelay = 0f;
        [Export(PropertyHint.Range, "0, 10.0, 0.1")]
        public float ActivationDelay
        {
            get => _activationDelay;
            set => _activationDelay = value;
        }

        // Local nodes
        private Area2D _cameraLimitArea = null!;
        private CollisionShape2D _cameraLimitShape = null!;
        private Tween _enterTween = null!;
        private Tween _exitTween = null!;

        // Plain ol' locals
        private float _preEnterLimitLeft;
        private float _preEnterLimitRight;
        private float _preEnterLimitTop;
        private float _preEnterLimitBottom;
        private CancellationTokenSource? _cts = null;

        public override void _Ready()
        {
            if (_playerCameraPath != null)
            {
                _playerCamera = GetNode<PlayerCamera>(_playerCameraPath);

                // Cache these values as early as possible, so we don't have to worry about
                // jumping in an out of a LimitArea winding up with weird in-between values
                _preEnterLimitLeft = _playerCamera.LimitLeft;
                _preEnterLimitRight = _playerCamera.LimitRight;
                _preEnterLimitTop = _playerCamera.LimitTop;
                _preEnterLimitBottom = _playerCamera.LimitBottom;
            }

            _cameraLimitArea = GetNode<Area2D>("CameraLimitArea");
            _cameraLimitShape = GetNode<CollisionShape2D>("CameraLimitShape");
            _enterTween = GetNode<Tween>("EnterTween");
            _exitTween = GetNode<Tween>("ExitTween");
        }

        public async void CameraLimitAreaEntered(Node body)
        {
            if (!(body is Player) || _playerCamera == null)
            {
                return;
            }

            if (ActivationDelay != 0f)
            {
                _cts = new CancellationTokenSource();
                await Task.Delay(TimeSpan.FromSeconds(ActivationDelay));
            }
            
            if (_cts != null && _cts.Token.IsCancellationRequested)
            {
                return;
            }

            // First teleport the current limits to the current viewport, then animate them into
            // position, to ensure a smooth transition no matter what the difference in limits is.
            var currentViewport = _playerCamera.CurrentViewportRect;
            _playerCamera.LimitLeft = (int)currentViewport.Position.x;                        
            _playerCamera.LimitRight = (int)currentViewport.End.x;                        
            _playerCamera.LimitTop = (int)currentViewport.Position.y;                        
            _playerCamera.LimitBottom = (int)currentViewport.End.y;

            _exitTween.StopAll();
            _enterTween.StopAll();
            _enterTween.InterpolateProperty(_playerCamera, nameof(PlayerCamera.LimitLeft), currentViewport.Position.x, LimitLeft, 1.0f, Tween.TransitionType.Linear, Tween.EaseType.Out);
            _enterTween.InterpolateProperty(_playerCamera, nameof(PlayerCamera.LimitRight), currentViewport.End.x, LimitRight, 1.0f, Tween.TransitionType.Linear, Tween.EaseType.Out);
            _enterTween.InterpolateProperty(_playerCamera, nameof(PlayerCamera.LimitTop), currentViewport.Position.y, LimitTop, 1.0f, Tween.TransitionType.Linear, Tween.EaseType.Out);
            _enterTween.InterpolateProperty(_playerCamera, nameof(PlayerCamera.LimitBottom), currentViewport.End.y, LimitBottom, 1.0f, Tween.TransitionType.Linear, Tween.EaseType.Out);

            _enterTween.Start();
        }

        public void CameraLimitAreaExited(Node body)
        {
            if (!(body is Player) || _playerCamera == null)
            {
                return;
            }

            _cts?.Cancel();

            var currentViewport = _playerCamera.CurrentViewportRect;

            float twiceViewportLeft = currentViewport.Position.x - PlayerCamera.ResolutionWidth;
            float twiceViewportRight = currentViewport.End.x + PlayerCamera.ResolutionWidth;
            float twiceViewportTop = currentViewport.Position.y - PlayerCamera.ResolutionHeight;
            float twiceViewportBottom = currentViewport.End.y + PlayerCamera.ResolutionHeight;

            _enterTween.StopAll();
            _exitTween.StopAll();
            // First, slowly animate out to twice the camera's current location
            _exitTween.InterpolateProperty(_playerCamera, nameof(PlayerCamera.LimitLeft), currentViewport.Position.x, twiceViewportLeft, 1.0f, Tween.TransitionType.Linear, Tween.EaseType.In);
            _exitTween.InterpolateProperty(_playerCamera, nameof(PlayerCamera.LimitRight), currentViewport.End.x, twiceViewportRight, 1.0f, Tween.TransitionType.Linear, Tween.EaseType.In);
            _exitTween.InterpolateProperty(_playerCamera, nameof(PlayerCamera.LimitTop), currentViewport.Position.y, twiceViewportTop, 1.0f, Tween.TransitionType.Linear, Tween.EaseType.In);
            _exitTween.InterpolateProperty(_playerCamera, nameof(PlayerCamera.LimitBottom), currentViewport.End.y, twiceViewportBottom, 1.0f, Tween.TransitionType.Linear, Tween.EaseType.In);

            // Then, animate out to the original cached locations
            _exitTween.InterpolateProperty(_playerCamera, nameof(PlayerCamera.LimitLeft), twiceViewportLeft, _preEnterLimitLeft, 1.0f, Tween.TransitionType.Linear, Tween.EaseType.In, 1.0f);
            _exitTween.InterpolateProperty(_playerCamera, nameof(PlayerCamera.LimitRight), twiceViewportRight, _preEnterLimitRight, 1.0f, Tween.TransitionType.Linear, Tween.EaseType.In, 1.0f);
            _exitTween.InterpolateProperty(_playerCamera, nameof(PlayerCamera.LimitTop), twiceViewportTop, _preEnterLimitTop, 1.0f, Tween.TransitionType.Linear, Tween.EaseType.In, 1.0f);
            _exitTween.InterpolateProperty(_playerCamera, nameof(PlayerCamera.LimitBottom), twiceViewportBottom, _preEnterLimitBottom, 1.0f, Tween.TransitionType.Linear, Tween.EaseType.In, 1.0f);

            _exitTween.Start();
        }

        public override void _Notification(int what)
        {
            // Make dragging the node around force the limits to redraw, because they're not relative to the node
            if (what == NotificationTransformChanged)
            {
                Update();
            }
        }

        public override void _Draw()
        {
            if (!Engine.EditorHint && !GetTree().DebugCollisionsHint)
            {
                return;
            }

            var vertMiddle = (LimitTop + LimitBottom) / 2;
            var horizMiddle = (LimitLeft + LimitRight) / 2;

            //Left
            DrawLine(
                new Vector2(LimitLeft, vertMiddle - 10) - GlobalPosition,
                new Vector2(LimitLeft, vertMiddle + 10) - GlobalPosition,
                Colors.GreenYellow,
                5);

            // Right
            DrawLine(
                new Vector2(LimitRight, vertMiddle - 10) - GlobalPosition,
                new Vector2(LimitRight, vertMiddle + 10) - GlobalPosition,
                Colors.GreenYellow,
                5);

            // Top
            DrawLine(
                new Vector2(horizMiddle - 10, LimitTop) - GlobalPosition,
                new Vector2(horizMiddle + 10, LimitTop) - GlobalPosition,
                Colors.GreenYellow,
                5);

            // Bottom
            DrawLine(
                new Vector2(horizMiddle - 10, LimitBottom) - GlobalPosition,
                new Vector2(horizMiddle + 10, LimitBottom) - GlobalPosition,
                Colors.GreenYellow,
                5);
        }

    }
}


