using Godot;
using System.Threading.Tasks;
using ThousandYearsHome.Controls;
using ThousandYearsHome.Controls.Dialogue;
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
        private HUD _hud = null!;
        private Camera2D _playerCamera = null!;
        private Camera2D _cinematicCamera = null!;
        private TileMap _midgroundTiles = null!;

        private Area2D _firstFallDialogueTrigger = null!;
        private Area2D _vistaPointTrigger = null!;
        private Area2D _collapseTrigger = null!;
        private Position2D _collapseTopLeft = null!;
        private Position2D _collapseBottomRight = null!;
        private Position2D _fallTeleportTop = null!;
        private Area2D _fallTeleportBottomArea = null!;
        private Area2D _collapseLandingTriggerArea = null!;

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
            _hud = GetNode<HUD>("UICanvas/HUD");
            _playerCamera = GetNode<Camera2D>("Player/CameraTarget/Camera2D");
            _cinematicCamera = GetNode<Camera2D>("CinematicCamera");
            _midgroundTiles = GetNode<TileMap>("MidgroundTiles");

            _firstFallDialogueTrigger = GetNode<Area2D>("FirstFallDialogueTrigger");
            _vistaPointTrigger = GetNode<Area2D>("VistaPointDialogueTrigger");

            _collapseTrigger = GetNode<Area2D>("CollapseTriggerArea");
            _collapseTopLeft = GetNode<Position2D>("CollapsePositions/CollapseTopLeft");
            _collapseBottomRight = GetNode<Position2D>("CollapsePositions/CollapseBottomRight");

            _fallTeleportTop = GetNode<Position2D>("FallTeleportTop");
            _fallTeleportBottomArea = GetNode<Area2D>("FallTeleportBottomArea");

            _collapseLandingTriggerArea = GetNode<Area2D>("CollapseLandingTriggerArea");

            Vector2 startPos = GetNode<Position2D>("StartPosition").Position;
            _player.Spawn(startPos);

            // Lock player's input, because we're gonna animate them cutscene style
            if (!SkipIntro)
            {
                _player.InputLocked = true;
            }
            else
            {
                _cinematicCamera.Current = false;
                _playerCamera.Current = true;
            }
        }

        // Called every frame. 'delta' is the elapsed time since the previous frame.
        public override void _Process(float delta)
        {

        }

        public void OnPlayerDebugUpdateState(PlayerStateKind newState, float xVel, float yVel, Vector2 position)
        {
            _hud.Debug_SetStateLabel(newState);
            _hud.Debug_SetVelocity(new Vector2(xVel, yVel));
            _hud.Debug_SetPosition(position);
        }

        public async void WaitStartTimerTimeout()
        {
            if (!SkipIntro)
            {
                // Initial camera Pan
                _tweener.InterpolateProperty(_cinematicCamera, "global_position", null, new Vector2(190, 82), 6.5f, Tween.TransitionType.Cubic, Tween.EaseType.InOut);
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
                _dialogueBox.QueueText("* I think I'm finally getting close... ", .03f);
                _dialogueBox.QueueBreak();
                _dialogueBox.QueueText("Not long now, one way or the other.", .03f);
                await _dialogueBox.Run();
                await ToSignal(_dialogueBox, "DialogueBoxClosed");

                _cinematicCamera.Current = false;
                _playerCamera.Current = true;

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
                //_dialogueBox.QueueText("* ...o close!\n", 0.2f);
                //_dialogueBox.LoadSilence(0.5f);
                //_dialogueBox.QueueText("Just... ...ittle furth...", 0.1f);
                //await _dialogueBox.Run();

                //// Wait for the dialogue box to close, then restore the player and snow and stuff
                //await ToSignal(_dialogueBox, "DialogueBoxClosed");
                //_snowParticles.SpeedScale = 1.0f;
                //_fader.Color = new Color(_fader.Color, 0f);
                //_player.SetSprite(0);
                _player.InputLocked = false;
            }
        }

        private bool _firstFallTriggered = false;
        public async void OnFirstFallDialogueEntered(Node body)
        {
            if (body.Name == "Player" && !_firstFallTriggered)
            {
                _firstFallTriggered = true;
                _firstFallDialogueTrigger.QueueFree();

                _player.InputLocked = true;

                // TODO: Play faceplant animation
                await _dialogueBox.Open();
                _dialogueBox.QueueText("* ...ow.", .1f);
                _dialogueBox.QueueBreak();
                // TODO: Return to standing animation
                _dialogueBox.QueueText(" Silver lining--no one was around to see that.", 0.05f);
                _dialogueBox.QueueBreak();
                _dialogueBox.QueueText(" Must be getting tired if that gave me trouble, though. Better wrap this up...", 0.05f);
                await _dialogueBox.Run();
                await ToSignal(_dialogueBox, nameof(DialogueBox.DialogueBoxClosed));

                _player.InputLocked = false;
            }
        }

        private bool _vistaPointTriggered = false;
        public async void OnVistaPointEntered(Node body)
        {
            if (body.Name == "Player" && !_vistaPointTriggered)
            {
                _vistaPointTriggered = true;
                _vistaPointTrigger.QueueFree();

                _player.InputLocked = true;
                await _dialogueBox.Open();
                _dialogueBox.QueueText("* I'll talk about that weird thing in the distance here.", 0.01f);
                await _dialogueBox.Run();
                await ToSignal(_dialogueBox, nameof(DialogueBox.DialogueBoxClosed));

                _player.InputLocked = false;
            }
        }

        private bool _collapseTriggered = false;
        public async void CollapseTriggerAreaEntered(Node body)
        {
            if (body.Name == "Player" && !_collapseTriggered)
            {
                _collapseTriggered = true;
                _collapseTrigger.QueueFree();

                _player.InputLocked = true;
                await _dialogueBox.Open();
                _dialogueBox.QueueText("* Oh no, things are collapsing dialogue here!", 0.01f);
                await _dialogueBox.Run();
                await ToSignal(_dialogueBox, nameof(DialogueBox.DialogueBoxClosed));

                // Remove cells so that player falls.
                var topLeftCell = _midgroundTiles.WorldToMap(_collapseTopLeft.Position);
                var bottomRightCell = _midgroundTiles.WorldToMap(_collapseBottomRight.Position);
                for (int x = (int)topLeftCell.x; x <= bottomRightCell.x; x++)
                {
                    for (int y = (int)topLeftCell.y; y <= bottomRightCell.y; y++)
                    {
                        _midgroundTiles.SetCellv(new Vector2(x, y), TileMap.InvalidCell);
                    }
                }

                _midgroundTiles.UpdateDirtyQuadrants();
                _player.MoveAndSlide(new Vector2(0, 0), Vector2.Down); // Force a position update to make the player begin to fall.

                // falling dialogue
                _snowParticles.Emitting = false; // no snow inside caves!
                await _dialogueBox.Open();
                _playerCamera.DragMarginVEnabled = false; // force camera to stay vertically snapped to player to sell the illusion of the infinte fall
                _dialogueBox.QueueText("* Oh even more no! Falling dialogue here!", 0.01f);
                await _dialogueBox.Run();
                await ToSignal(_dialogueBox, nameof(DialogueBox.DialogueBoxClosed));

                _fallTeleportBottomArea.QueueFree();
                _player.InputLocked = false;
            }
        }

        private bool _collapseLanded = false;
        public async void CollapseBaseAreaEntered(Node body)
        {
            if (body.Name == "Player" && !_collapseLanded)
            {
                _collapseLanded = true;
                _playerCamera.DragMarginVEnabled = true; // Stop force-centering the player in the camera vertically

                _player.InputLocked = true;

                await Task.Delay(500); // TODO: testing to see if waiting for the landing state is enough...
                _player.AnimatePose("Crouch"); // TODO: Make this the faceplant animation
                await Task.Delay(2000);

                string secondLine = _firstFallTriggered ? "* Okay, this time it might have been nice to have someone see me. That [i]hurt[/i]." : "* Oh, great, now I've got snow in my saddlebags. Augh, that [i]hurt[/i].";
                await _dialogueBox.Open();
                _dialogueBox.QueueText("* Ow.\n")
                    .QueueSilence(2.0f)
                    .QueueText(secondLine, 0.05f)
                    .QueueBreak();
                await _dialogueBox.Run();

                _player.AnimatePose("Stand"); // TODO: Replace this with crouch once the above is replaced by faceplant.
                _dialogueBox.QueueText("\n* Looks like I fell a pretty long way. I can barely even see the top from here...", 0.05f)
                    .QueueBreak()
                    .QueueText("\n* No choice but to push forward, then. Hopefully there's a way out somewhere ahead.", 0.05f);
                await _dialogueBox.Run();
                await ToSignal(_dialogueBox, nameof(DialogueBox.DialogueBoxClosed));

                _collapseLandingTriggerArea.QueueFree();
                _player.InputLocked = false;
            }
        }

        public void OnFallTeleportBottomAreaEntered(Node body)
        {
            // Continually teleport the player back up to the top of the fall area to simulate an infinite fall
            if (body.Name == "Player")
            {
                _player.Position = new Vector2(_player.Position.x, _fallTeleportTop.Position.y);
            }
        }
    }
}


