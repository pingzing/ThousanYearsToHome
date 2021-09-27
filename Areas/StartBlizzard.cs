using Godot;
using System;
using System.Threading.Tasks;
using ThousandYearsHome.Controls;
using ThousandYearsHome.Entities.Player;
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

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            _sceneRoot = GetTree().Root.GetNode<Node>("StartBlizzard");
            _snowParticles = GetNode<Particles2D>("SceneCanvas/Particles2D");
            _fader = GetNode<ColorRect>("SceneCanvas/Fader");
            _dialogueBox = GetNode<DialogueBox>("UICanvas/DialogueBox");
            var startPos = GetNode<Position2D>("SceneCanvas/StartPosition").Position;
            _player = GetNode<Player>("SceneCanvas/Player");
            _player.Spawn(startPos);

            // Lock player's input, because we're gonna animate them cutscene style
            _player.InputLocked = true;
            _animator = GetNode<AnimationPlayer>("SceneCanvas/AnimationPlayer");
            _fadeAnimator = GetNode<AnimationPlayer>("SceneCanvas/FadePlayer");
        }

        public void WaitStartTimerTimeout()
        {
            _animator.Play("StaggerForward");
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
                _dialogueBox.LoadText("* ...o close!\n", 0.1f);
                _dialogueBox.LoadSilence(0.5f);
                _dialogueBox.LoadText("...ttle fur...", 0.2f);
                await _dialogueBox.Run();

                // Wait for the dialogue box to close, then restore the player and snow and stuff
                await ToSignal(_dialogueBox, "DialogueBoxClosed");
                _snowParticles.SpeedScale = 1.0f;
                _fader.Color = new Color(_fader.Color, 0f);
                _player.SetSprite(0);
                _player.InputLocked = false;
            }
        }

        // Called every frame. 'delta' is the elapsed time since the previous frame.
        public override void _Process(float delta)
        {

        }
    }
}


