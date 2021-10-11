using Godot;
using System.Threading.Tasks;
using ThousandYearsHome.Controls;
using ThousandYearsHome.Entities;
using ThousandYearsHome.Entities.PlayerEntity;
using ThousandYearsHome.Extensions;

namespace ThousandYearsHome.Areas
{
    public class StartBlizzard : Node
    {
        private Node _sceneRoot = null!;
        private Player _player = null!;
        private AnimationPlayer _animator = null!;
        private AnimationPlayer _fadeAnimator = null!;
        private Tween _tweener = null!;
        private Particles2D _snowParticles = null!;
        private ColorRect _fader = null!;
        private DialogueBox _dialogueBox = null!;
        private PackedScene _powerBallScene = null!;
        private Position2D _ballSpawnPoint = null!;
        private HUD _hud = null!;
        private Camera2D _playerCamera = null!;
        private Camera2D _cinematicCamera = null!;
        private CollisionShape2D _leftEdge = null!;

        public bool SkipIntro { get; set; }

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            _sceneRoot = GetTree().Root.GetNode<Node>("StartBlizzard");
            _snowParticles = GetNode<Particles2D>("UICanvas/Particles2D");
            _fader = GetNode<ColorRect>("UICanvas/Fader");
            _dialogueBox = GetNode<DialogueBox>("UICanvas/DialogueBox");
            _player = GetNode<Player>("Player");
            _animator = GetNode<AnimationPlayer>("AnimationPlayer");
            _fadeAnimator = GetNode<AnimationPlayer>("UICanvas/FadePlayer");
            _tweener = GetNode<Tween>("Tweener");
            _powerBallScene = GD.Load<PackedScene>("res://entities/PowerBall.tscn");
            _ballSpawnPoint = GetNode<Position2D>("BallSpawnPoint");
            _hud = GetNode<HUD>("UICanvas/HUD");
            _playerCamera = GetNode<Camera2D>("Player/CameraTarget/Camera2D");
            _cinematicCamera = GetNode<Camera2D>("CinematicCamera");
            _leftEdge = GetNode<CollisionShape2D>("Terrain/LeftEdge");

            Vector2 startPos = GetNode<Position2D>("StartPosition").Position;
            _player.Spawn(startPos);
            // Lock player's input, because we're gonna animate them cutscene style
            if (!SkipIntro)
            {
                _player.InputLocked = true;
            }
        }

        // Called every frame. 'delta' is the elapsed time since the previous frame.
        public override void _Process(float delta)
        {

        }

        public void OnPlayerDebugUpdateState(PlayerStateKind newState, float xVel, float yVel)
        {
            _hud.Debug_SetStateLabel(newState);
            _hud.Debug_SetVelocity(new Vector2(xVel, yVel));
        }

        public async void WaitStartTimerTimeout()
        {
            if (!SkipIntro)
            {
                float playerCamCenterY = (int)_playerCamera.GetViewportRect().End.y / 2 + 1;
                // Initial camera Pan
                _tweener.InterpolateProperty(_cinematicCamera, "global_position", null, new Vector2(_cinematicCamera.GlobalPosition.x, playerCamCenterY), 6.5f, Tween.TransitionType.Cubic, Tween.EaseType.InOut);
                _tweener.Start();
                await this.ToSignalWithArgs(_tweener, "tween_completed", 0, _cinematicCamera);

                // Blue walks on and talks
                _player.ResetPoseAnimation();
                _player.AnimatePose("Walk");
                _tweener.InterpolateProperty(_player, "position", null, new Vector2(_player.Position.x + 100, _player.Position.y), 3.3f, Tween.TransitionType.Quad, Tween.EaseType.Out);
                _tweener.Start();
                await this.ToSignalWithArgs(_tweener, "tween_completed", 0, _player);
                _player.AnimatePose("Idle");

                await _dialogueBox.Open();
                _dialogueBox.LoadText("* ...good. ", .08f);
                _dialogueBox.LoadBreak();
                _dialogueBox.LoadText("Nearing the source. Not far now.", .03f);
                await _dialogueBox.Run();
                await ToSignal(_dialogueBox, "DialogueBoxClosed");

                _cinematicCamera.Current = false;
                _playerCamera.Current = true;
                _leftEdge.Disabled = false;

                // A lot of this can probably be reused for the cutscene after we find the frozen keeper

                //_animator.Play("StaggerForward");
                //await this.ToSignalWithArgs(_animator, "animation_finished", 0, "StaggerForward");

                //_animator.Play("Shiver");
                //await this.ToSignalWithArgs(_animator, "animation_finished", 0, "Shiver");

                //_animator.Play("PassOut");

                //_fadeAnimator.Play("FadeScene");
                //await this.ToSignalWithArgs(_fadeAnimator, "animation_finished", 0, "FadeScene");

                //_player.AnimateColor("FlashWhite");
                //_fader.Color = new Color(_fader.Color, .3f); // Un-dim MOST of the way. Rest of the way after dialogue.
                //_snowParticles.SpeedScale = 0f;
                //_player.SetSprite(4);

                //// Dialogue
                //await _dialogueBox.Open();
                //_dialogueBox.LoadText("* ...o close!\n", 0.2f);
                //_dialogueBox.LoadSilence(0.5f);
                //_dialogueBox.LoadText("Just... ...ittle furth...", 0.1f);
                //await _dialogueBox.Run();

                //// Wait for the dialogue box to close, then restore the player and snow and stuff
                //await ToSignal(_dialogueBox, "DialogueBoxClosed");
                //_snowParticles.SpeedScale = 1.0f;
                //_fader.Color = new Color(_fader.Color, 0f);
                //_player.SetSprite(0);
                _player.InputLocked = false;
            }
        }

        public void OnBallSpawnTimerTimeout()
        {
            GD.Print("Spawning ball...");
            PowerBall ball = _powerBallScene.Instance<PowerBall>();
            float randomY = Numerology.RandRange(_ballSpawnPoint.Position.y - 60, _ballSpawnPoint.Position.y);
            ball.Position = new Vector2(_ballSpawnPoint.Position.x, randomY);
            AddChild(ball);
        }
    }
}


