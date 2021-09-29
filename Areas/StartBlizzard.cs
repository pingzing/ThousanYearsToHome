using Godot;
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
        private Particles2D _snowParticles = null!;
        private ColorRect _fader = null!;
        private DialogueBox _dialogueBox = null!;
        private Timer _ballSpawnTimer = null!;
        private PackedScene _powerBallScene = null!;
        private Position2D _ballSpawnPoint = null!;

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
            _ballSpawnTimer = GetNode<Timer>("BallSpawnTimer");
            _powerBallScene = GD.Load<PackedScene>("res://Entities/PowerBall.tscn");
            _ballSpawnPoint = GetNode<Position2D>("BallSpawnPoint");

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
            GetNode<Label>("UICanvas/DebugStateLabel").Text = newState.ToString();
            GetNode<Label>("UICanvas/VelocityVBox/XVelLabel").Text = $"XVel: {xVel}";
            GetNode<Label>("UICanvas/VelocityVBox/YVelLabel").Text = $"YVel: {yVel}";
        }

        public void StartGameplay()
        {
            _ballSpawnTimer.Start();
        }

        public void WaitStartTimerTimeout()
        {
            if (!SkipIntro)
            {
                _animator.Play("StaggerForward");
            }
            else
            {
                StartGameplay();
            }
        }

        public void OnAnimationStarted(string name)
        {
            if (name == "PassOut")
            {
                _fadeAnimator.Play("FadeScene");
            }
        }

        public void OnAnimationFinished(string name)
        {
            if (name == "StaggerForward")
            {
                _animator.Play("Shiver");
            }
            if (name == "Shiver")
            {
                _animator.Play("PassOut");
            }
        }

        public void OnDialogueBoxClosed()
        {
        }

        public async void OnFadePlayerAnimationFinished(string name)
        {
            if (name == "FadeScene")
            {
                _player.AnimateColor("FlashWhite");
                _fader.Color = new Color(_fader.Color, .3f); // Un-dim MOST of the way. Rest of the way after dialogue.
                _snowParticles.SpeedScale = 0f;
                _player.SetSprite(4);

                await _dialogueBox.Open();
                _dialogueBox.LoadText("* ...o close!\n", 0.2f);
                _dialogueBox.LoadSilence(0.5f);
                _dialogueBox.LoadText("Just... ...ittle furth...", 0.1f);
                await _dialogueBox.Run();

                // Wait for the dialogue box to close, then restore the player and snow and stuff
                await ToSignal(_dialogueBox, "DialogueBoxClosed");
                _snowParticles.SpeedScale = 1.0f;
                _fader.Color = new Color(_fader.Color, 0f);
                _player.SetSprite(0);
                _player.InputLocked = false;
                StartGameplay();
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


