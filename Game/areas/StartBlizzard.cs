using Godot;
using System;
using System.Threading.Tasks;
using ThousandYearsHome.Controls;
using ThousandYearsHome.Controls.Dialogue;
using ThousandYearsHome.Entities;
using ThousandYearsHome.Entities.PlayerEntity;
using ThousandYearsHome.Extensions;

namespace ThousandYearsHome.Areas
{
    public class StartBlizzard : Node
    {
        private Node _sceneRoot = null!;
        private Player _player = null!;
        private PlayerCamera _playerCamera = null!;
        private AnimationPlayer _animator = null!;
        private AnimationPlayer _fadeAnimator = null!;
        private Tween _tweener = null!;
        private Particles2D _snowParticles = null!;
        private ColorRect _fader = null!;
        private DialogueBox _dialogueBox = null!;
        private DialogueBox _liteDialogueBox = null!;
        private HUD _hud = null!;
        private Camera2D _cinematicCamera = null!;
        private TileMap _midgroundTiles = null!;
        private PowerBallWatcher _powerBallWatcher = null!;

        private Area2D _firstFallDialogueTrigger = null!;
        private Area2D _vistaPointTrigger = null!;
        private Area2D _collapseTrigger = null!;
        private Position2D _collapseTopLeft = null!;
        private Position2D _collapseBottomRight = null!;
        private Position2D _fallTeleportTop = null!;
        private Area2D _fallTeleportBottomArea = null!;
        private Area2D _collapseLandingTriggerArea = null!;
        private Area2D _keeperCutsceneTriggerArea = null!;
        private Position2D _keeperCutsceneCameraPosition = null!;
        private Area2D _cameraHoldArea = null!;
        private CollisionShape2D _cameraHoldShape = null!;
        private RectangleShape2D _cameraHoldRect = null!;
        private Position2D _cameraHoldPosition = null!;

        public bool SkipIntro { get; set; }

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            _sceneRoot = GetTree().Root.GetNode<Node>("StartBlizzard");
            _snowParticles = GetNode<Particles2D>("UICanvas/Particles2D");
            _fader = GetNode<ColorRect>("UICanvas/Fader");
            _dialogueBox = GetNode<DialogueBox>("UICanvas/DialogueBox");
            _liteDialogueBox = GetNode<DialogueBox>("UICanvas/LiteDialogueBox");
            _liteDialogueBox.BreakKey = KeyList.Shift;
            _player = GetNode<Player>("Player");
            _animator = GetNode<AnimationPlayer>("AnimationPlayer");
            _fadeAnimator = GetNode<AnimationPlayer>("UICanvas/FadePlayer");
            _tweener = GetNode<Tween>("Tweener");
            _hud = GetNode<HUD>("UICanvas/HUD");
            _playerCamera = GetNode<PlayerCamera>("Player/PlayerCamera");
            _cinematicCamera = GetNode<Camera2D>("CinematicCamera");
            _midgroundTiles = GetNode<TileMap>("MidgroundTiles");
            _powerBallWatcher = GetNode<PowerBallWatcher>("UICanvas/PowerBallWatcher");

            _firstFallDialogueTrigger = GetNode<Area2D>("FirstFallDialogueTrigger");
            _vistaPointTrigger = GetNode<Area2D>("VistaPointDialogueTrigger");

            _collapseTrigger = GetNode<Area2D>("CollapseTriggerArea");
            _collapseTopLeft = GetNode<Position2D>("CollapsePositions/CollapseTopLeft");
            _collapseBottomRight = GetNode<Position2D>("CollapsePositions/CollapseBottomRight");

            _fallTeleportTop = GetNode<Position2D>("FallTeleportTop");
            _fallTeleportBottomArea = GetNode<Area2D>("FallTeleportBottomArea");

            _collapseLandingTriggerArea = GetNode<Area2D>("CollapseLandingTriggerArea");

            _keeperCutsceneTriggerArea = GetNode<Area2D>("KeeperCutsceneTriggerArea");

            _cameraHoldArea = GetNode<Area2D>("CameraHoldArea");
            _cameraHoldShape = _cameraHoldArea.GetNode<CollisionShape2D>("CameraHoldShape");
            _cameraHoldRect = (RectangleShape2D)_cameraHoldShape.Shape;
            _cameraHoldPosition = GetNode<Position2D>("CameraHoldPosition");

            Vector2 startPos = GetNode<Position2D>("StartPosition").Position;
            _player.Spawn(startPos);
            _powerBallWatcher.Init(_playerCamera);

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
                await this.ToSignalWithArg(_tweener, "tween_completed", 0, _cinematicCamera);

                // Blue walks on and talks                
                _player.AnimatePose("Walk");
                _tweener.InterpolateProperty(_player, "position", null, new Vector2(_player.Position.x + 100, _player.Position.y), 3.3f, Tween.TransitionType.Quad, Tween.EaseType.Out);
                _tweener.Start();
                await this.ToSignalWithArg(_tweener, "tween_completed", 0, _player);
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

                await _liteDialogueBox.Open();
                _liteDialogueBox.QueuePortrait("res://art/PlaceholderPortrait.png")
                    .QueueText("* Ow.")
                    .QueueBreak()
                    .QueueClear()
                    .QueueText("* I am so ", .01f).QueueSilence(.3f).QueueText("SICK").QueueSilence(.3f).QueueText(" of these hills.", .01f)
                    .QueueBreak()
                    .QueueClear()
                    .QueueText("* Hopefully this really is the trail...", .01f)
                    .QueueBreak();
                await _liteDialogueBox.Run();
                await _liteDialogueBox.Close();

                //_player.InputLocked = true;

                //using (_player.DisableStateMachine())
                //{
                //    _player.AnimatePose("Crouch"); // TODO: Replace with faceplant animation.                    
                //    await _dialogueBox.Open();
                //    _dialogueBox.QueueText("You become intimately acquainted with the densely-packed snow of this gulch's floor.", .02f)
                //        .QueueBreak()
                //        .QueueClear()
                //        .QueuePortrait("res://art/PlaceholderPortrait.png")
                //        .QueueText("* Ow. ", .02f)
                //        .QueueBreak();
                //    await _dialogueBox.Run();

                //    _player.AnimatePose("Idle");
                //    _dialogueBox.QueuePortrait("res://art/PlaceholderPortrait.png")
                //        .QueueText("* Come on, ", 0.02f).QueueSilence(0.1f).QueueText("just a little further...", 0.02f);
                //    await _dialogueBox.Run();
                //    await ToSignal(_dialogueBox, nameof(DialogueBox.DialogueBoxClosed));
                //}

                //_player.InputLocked = false;
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
                _dialogueBox.QueueText("In the distance, something clearly artificial gleams amongst the drifts. It's difficult to make out through the snowfall.", 0.01f)
                    .QueueBreak()
                    .QueueClear()
                    .QueuePortrait("res://art/PlaceholderPortrait.png")
                    .QueueText("* Could that be it?", 0.02f);
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

                var prevRect = _playerCamera.TargetRect;

                _playerCamera.TargetRect = new Rect2(prevRect.Position, prevRect.Size.x, 0f);
                _dialogueBox.QueueText("You have exactly enough time to regret the choices that led you here, and to mourn your imminent death.", 0.01f)
                    .QueueBreak()
                    .QueueText("\nBizarrely, you mostly just find yourself hoping you don't die in a stupid-looking posiiton.", 0.01f);
                await _dialogueBox.Run();
                await ToSignal(_dialogueBox, nameof(DialogueBox.DialogueBoxClosed));

                _fallTeleportBottomArea.QueueFree();
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

        private bool _collapseLanded = false;
        public async void CollapseBaseAreaEntered(Node body)
        {
            if (body is Player && !_collapseLanded)
            {
                _collapseLanded = true;
                _playerCamera.TargetRect = new Rect2(64, 16, 80, 160); // Stop force-centering the player in the camera vertically

                _player.InputLocked = true;

                using (_player.DisableStateMachine())
                {
                    _player.AnimatePose("Crouch");

                    await Task.Delay(1000);

                    await _dialogueBox.Open();
                    string firstLine = _firstFallTriggered
                        ? "You find yourself contemplating the floor of an icy pit for the second time today. You hope it isn't going to become a habit."
                        : "You find yourself contemplating an icy floor at the bottom of a deep pit.";
                    _dialogueBox.QueueText(firstLine, 0.01f)
                            .QueueBreak()
                            .QueueClear();
                    await _dialogueBox.Run();

                    _dialogueBox.QueuePortrait("res://art/PlaceholderPortrait.png")
                        .QueueText("* Ow.", 0.03f)
                        .QueueBreak()
                        .QueueClear()
                        .QueueText("Miraculously, you seem to have escaped without major injury. Though there's fast-melting snow in places you'd rather not contemplate.", 0.01f)
                        .QueueBreak()
                        .QueueClear();
                    await _dialogueBox.Run();
                }

                _player.AnimatePose("Idle"); // TODO: Replace this with crouch once the above is replaced by faceplant.

                // TODO: Animate pose into "looking up"
                _dialogueBox.QueueText("You can barely see the light at the top of the hole. You're probably not getting back out the way you came in.", 0.01f)
                    .QueueBreak()
                    .QueueClear();
                await _dialogueBox.Run();

                // TODO: animate pose back to idle
                _dialogueBox.QueuePortrait("res://art/PlaceholderPortrait.png")
                    .QueueText("* Just as my luck was turning, too...", 0.03f)
                    .QueueBreak()
                    .QueueText("\n* All right, come on soldier, buck up.", 0.03f);
                await _dialogueBox.Run();
                await ToSignal(_dialogueBox, nameof(DialogueBox.DialogueBoxClosed));

                _collapseLandingTriggerArea.QueueFree();
                _player.InputLocked = false;
            }
        }

        private bool _keeperCutscenePlayed = false;
        public async void KeeperCutsceneAreaEntered(Node body)
        {
            if (body is Player && !_keeperCutscenePlayed)
            {
                _keeperCutscenePlayed = true;

                _player.InputLocked = true;

                await _dialogueBox.Open();
                _dialogueBox.QueueText("You can see light coming from an exit ahead. Except...", 0.01f)
                    .QueueBreak()
                    .QueueClear()
                    .QueuePortrait("res://art/PlaceholderPortrait.png")
                    .QueueText("* What in seasons' name is THAT?", 0.03f);
                await _dialogueBox.Run();
                await ToSignal(_dialogueBox, nameof(DialogueBox.DialogueBoxClosed));

                _cinematicCamera.LimitBottom = _playerCamera.LimitBottom;
                _cinematicCamera.LimitTop = _playerCamera.LimitTop;
                _cinematicCamera.LimitRight = _playerCamera.LimitRight;
                _cinematicCamera.LimitLeft = _playerCamera.LimitLeft;
                _cinematicCamera.Position = _player.Position + _playerCamera.Position;

                _cinematicCamera.SmoothingEnabled = true;
                _cinematicCamera.Current = true;
                _keeperCutsceneCameraPosition = GetNode<Position2D>("KeeperCutsceneCameraPosition");
                _cinematicCamera.Position = _keeperCutsceneCameraPosition.Position; // let Smoothing do the panning work here

                var walkEnd = GetNode<Position2D>("KeeperCutsceneWalkEndPosition");

                using (_player.DisableStateMachine())
                {
                    _player.AnimatePose("Walk");
                    _tweener.InterpolateProperty(_player, "position", null, new Vector2(walkEnd.Position.x, _player.Position.y), 4.0f, Tween.TransitionType.Quad, Tween.EaseType.Out);
                    _tweener.Start();
                    await this.ToSignalWithArg(_tweener, "tween_completed", 0, _player);
                    _player.AnimatePose("Idle");
                }

                await _dialogueBox.Open();
                _dialogueBox.QueueText("This is where the keeper cutscene will happen! Zappy horns, big scary flashy, glowy balls, stamia meter!");
                await _dialogueBox.Run();
                await ToSignal(_dialogueBox, nameof(DialogueBox.DialogueBoxClosed));

                var playerCameraPos = _player.Position + _playerCamera.GetParent<Node2D>().Position; // TODO: PlayerCamera doesn't use position anymore.
                _cinematicCamera.SmoothingEnabled = false;
                _tweener.InterpolateProperty(_cinematicCamera, "position", null, playerCameraPos, 1.0f, Tween.TransitionType.Quad, Tween.EaseType.Out);
                _tweener.Start();
                await this.ToSignalWithArg(_tweener, "tween_completed", 0, _cinematicCamera);

                _playerCamera.Current = true;
                _keeperCutsceneTriggerArea.QueueFree();
                _player.InputLocked = false;

                _snowParticles.Amount = 1600;
                (_snowParticles.ProcessMaterial as ParticlesMaterial).InitialVelocity = 680f;
                _snowParticles.Emitting = true;
            }
        }

        public async void CameraHoldAreaEntered(Node body)
        {
            if (body is Player)
            {
                await _playerCamera.LockXAxis(_cameraHoldPosition.Position.x);
                _playerCamera.IdleRect = new Rect2(64, PlayerCamera.ResolutionHeight * .7f, 40, 16);
            }
        }

        public void CameraHoldAreaExited(Node body)
        {
            if (body is Player)
            {
                _playerCamera.UnlockXAxis();
                _playerCamera.IdleRect = PlayerCamera.DefaultIdleRect;
            }
        }

        public void ClimaxCameraLimitAdjustAreaBodyEntered(Node body)
        {
            if (body is Player)
            {
                // For the final segment of the level, prevent the camera from going too far into the leftmost wall.
                _playerCamera.LimitLeft = 5408;
            }
        }
    }
}


