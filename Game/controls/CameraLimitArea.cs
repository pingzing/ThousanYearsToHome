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
        private CollisionShape2D _cameraLimitShape = null!;
        private Tween _enterTween = null!;

        // Plain ol' locals
        private float _preEnterLimitLeft;
        private float _preEnterLimitRight;
        private float _preEnterLimitTop;
        private float _preEnterLimitBottom;
        private CancellationTokenSource _enterCts = new CancellationTokenSource();
        private CancellationTokenSource _exitCts = new CancellationTokenSource();

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

            _cameraLimitShape = GetNode<CollisionShape2D>("CameraLimitShape");
            _enterTween = GetNode<Tween>("EnterTween");
        }

        public async void CameraLimitAreaEntered(Node body)
        {
            if (!(body is Player) || _playerCamera == null)
            {
                return;
            }
                       
            if (ActivationDelay != 0f)
            {
                _enterCts.Cancel();
                _enterCts = new CancellationTokenSource();
                await Task.Delay(TimeSpan.FromSeconds(ActivationDelay));
            }
            
            if (_enterCts.Token.IsCancellationRequested)
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

            _enterTween.StopAll();
            _enterTween.InterpolateProperty(_playerCamera, nameof(PlayerCamera.LimitLeft), currentViewport.Position.x, LimitLeft, 1.0f, Tween.TransitionType.Linear, Tween.EaseType.Out);
            _enterTween.InterpolateProperty(_playerCamera, nameof(PlayerCamera.LimitRight), currentViewport.End.x, LimitRight, 1.0f, Tween.TransitionType.Linear, Tween.EaseType.Out);
            _enterTween.InterpolateProperty(_playerCamera, nameof(PlayerCamera.LimitTop), currentViewport.Position.y, LimitTop, 1.0f, Tween.TransitionType.Linear, Tween.EaseType.Out);
            _enterTween.InterpolateProperty(_playerCamera, nameof(PlayerCamera.LimitBottom), currentViewport.End.y, LimitBottom, 1.0f, Tween.TransitionType.Linear, Tween.EaseType.Out);

            _enterTween.Start();
        }

        public async void CameraLimitAreaExited(Node body)
        {
            if (!(body is Player) || _playerCamera == null)
            {
                return;
            }

            _enterTween.StopAll();
            if (ActivationDelay != 0f)
            {
                _enterCts.Cancel();
            }
            _exitCts.Cancel();

            _exitCts = new CancellationTokenSource();              

            _playerCamera.MaxXPerSecond = 325f;

            _playerCamera.LimitLeft = (int)_preEnterLimitLeft;
            _playerCamera.LimitRight = (int)_preEnterLimitRight;
            _playerCamera.LimitTop = (int)_preEnterLimitTop;
            _playerCamera.LimitBottom = (int)_preEnterLimitBottom;

            await Task.Delay(3000);

            if (_exitCts.Token.IsCancellationRequested)
            {
                return;
            }

            _playerCamera.MaxXPerSecond = -1;
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


