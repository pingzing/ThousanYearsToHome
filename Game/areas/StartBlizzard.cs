using Godot;
using System;
using System.Threading.Tasks;
using ThousandYearsHome.Controls;
using ThousandYearsHome.Controls.Dialogue;
using ThousandYearsHome.Entities;
using ThousandYearsHome.Entities.BreakableEntity;
using ThousandYearsHome.Entities.PlayerEntity;
using ThousandYearsHome.Entities.PowerBallEntity;
using ThousandYearsHome.Entities.WarmthBallEntity;
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
        private CinematicCamera _cinematicCamera = null!;
        private TileMap _midgroundTiles = null!;
        private WarmthBallWatcher _warmthBallWatcher = null!;
        private HornCollectibleSignalBus _collectibleSignalBus = null!;

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

        private Breakable? _doorInRange;

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
            _cinematicCamera = GetNode<CinematicCamera>("CinematicCamera");
            _midgroundTiles = GetNode<TileMap>("MidgroundTiles");
            _warmthBallWatcher = GetNode<WarmthBallWatcher>("UICanvas/WarmthBallWatcher");
            _collectibleSignalBus = GetNode<HornCollectibleSignalBus>("/root/HornCollectibleSignalBus");
            _collectibleSignalBus.Connect(nameof(HornCollectibleSignalBus.PowerBallCollected), this, nameof(PowerBallCollected));
            _collectibleSignalBus.Connect(nameof(HornCollectibleSignalBus.WarmthBallCollected), this, nameof(WarmthBallCollected));

            _firstFallDialogueTrigger = GetNode<Area2D>("FirstFallDialogueTrigger");
            _vistaPointTrigger = GetNode<Area2D>("VistaPointDialogueTrigger");

            _collapseTrigger = GetNode<Area2D>("CollapseTriggerArea");
            _collapseTopLeft = GetNode<Position2D>("CollapsePositions/CollapseTopLeft");
            _collapseBottomRight = GetNode<Position2D>("CollapsePositions/CollapseBottomRight");

            _fallTeleportTop = GetNode<Position2D>("FallTeleportTop");
            _fallTeleportBottomArea = GetNode<Area2D>("FallTeleportBottomArea");

            _collapseLandingTriggerArea = GetNode<Area2D>("CollapseLandingTriggerArea");

            _keeperCutsceneTriggerArea = GetNode<Area2D>("KeeperCutsceneTriggerArea");

            Vector2 startPos = GetNode<Position2D>("StartPosition").Position;
            _player.Spawn(startPos);
            _warmthBallWatcher.Init(_playerCamera);

            // Lock player's input, because we're gonna animate them cutscene style
            if (!SkipIntro)
            {
                _player.InputLocked = true;
            }
            else
            {
                _cinematicCamera.Current = false;
                _playerCamera.Current = true;

                // DEBUG! Continuing forces us into freezing mode
                _player.EnterKickOverride = FreezingKickOverride;
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
                _tweener.InterpolateProperty(_cinematicCamera, "global_position", null, new Vector2(0, -21), 6.5f, Tween.TransitionType.Cubic, Tween.EaseType.InOut);
                _tweener.Start();
                await this.ToSignalWithArg(_tweener, "tween_completed", 0, _cinematicCamera);

                // Blue walks on and talks                
                _player.AnimatePose("Walk");
                _tweener.InterpolateProperty(_player, "position", null, new Vector2(_player.Position.x + 100, _player.Position.y), 3.3f, Tween.TransitionType.Quad, Tween.EaseType.Out);
                _tweener.Start();
                await this.ToSignalWithArg(_tweener, "tween_completed", 0, _player);
                _player.AnimatePose("Idle");

                await _dialogueBox.Open();
                _dialogueBox.QueuePortrait("res://art/PlaceholderPortrait.png")
                    .QueueText("...whew.\n")
                    .QueueBreak()
                    .QueueText("Finally made it up here.\n")
                    .QueueBreak()
                    .QueueText("This is the closest thing to a trail I've found all week. I really hope I'm close.")
                    .QueueBreak().QueueClear()
                    .QueueText(" ...especially because I'm almost out of supplies. If this doesn't lead anywhere, I'll have to turn back.");
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
                    .QueueText("Ow.")
                    .QueueBreak().QueueClear()
                    .QueueText("I am so ").QueueSilence(.3f).QueueText("SICK").QueueSilence(.3f).QueueText(" of these hills.")
                    .QueueBreak().QueueClear()
                    .QueueText("Hopefully this really is the trail...", .015f)
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
                _dialogueBox.QueuePortrait("res://art/PlaceholderPortrait.png")
                    .QueueText("...?\n")
                    .QueueBreak()
                    .QueueText("What is that? I can barely make it out through the snowfall...")
                    .QueueBreak().QueueClear()
                    .QueueText("But...").QueueBreak().QueueText(" it looks...").QueueSilence(1f).QueueText(" artificial...");
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

                // Make the Cinematic Camera take over, and parent it to the player so it follows them
                CinematicCameraFollowPlayer(_player, _cinematicCamera);

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

                _player.InputLocked = true;

                using (_player.DisableStateMachine())
                {
                    _player.AnimatePose("Crouch"); // TODO: Faceplant animation/pose

                    await Task.Delay(1000);

                    await _dialogueBox.Open();
                    _dialogueBox.QueuePortrait("res://art/PlaceholderPortrait.png")
                        .QueueText("Ow.")
                        .QueueSilence(2)
                        .QueueBreak();

                    _player.AnimatePose("Idle"); // TODO: Animate standing up, then animate LOOKING up

                    // TODO: pan camera up to fluttering coat

                    string variantLine = _firstFallTriggered
                        ? "The second time today! Unbelievable.\nNot getting back out that way. Or getting my coat back..."
                        : "Not getting back out that way. Or getting my coat back...";
                    _dialogueBox.QueuePortrait("res://art/PlaceholderPortrait.png")
                        .QueueText(variantLine)
                        .QueueBreak().QueueClear();
                    await _dialogueBox.Run();
                }

                // TODO: Pan camera back down
                // TODO: animate pose back to idle

                _dialogueBox.QueuePortrait("res://art/PlaceholderPortrait.png")
                    .QueueText("Brrr. It is [shake rate=10 level=8]cold[/shake].")
                    .QueueBreak()
                    .QueueText("\nI'll be okay in here for a little while, but if I have to go outside again, that wind is going to slice right through me. I've got to find some shelter, and fast...");
                await _dialogueBox.Run();
                await ToSignal(_dialogueBox, nameof(DialogueBox.DialogueBoxClosed));

                PlayerCameraTakeControl(_player, _playerCamera, _cinematicCamera);

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

                _cinematicCamera.LimitBottom = (int)_playerCamera.LimitBottom;
                _cinematicCamera.LimitTop = (int)_playerCamera.LimitTop;
                _cinematicCamera.LimitRight = (int)_playerCamera.LimitRight;
                _cinematicCamera.LimitLeft = (int)_playerCamera.LimitLeft;
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

                // Return control to player, lock down their ability to kick
                _playerCamera.Current = true;
                _player.EnterKickOverride = FreezingKickOverride;
                _keeperCutsceneTriggerArea.QueueFree();
                _player.InputLocked = false;

                // More snow!
                _snowParticles.Amount = 1600;
                (_snowParticles.ProcessMaterial as ParticlesMaterial).InitialVelocity = 680f;
                _snowParticles.Emitting = true;
            }
        }

        public void PowerBallCollected(PowerBall ball)
        {
            GD.Print("Level sees that player got a PowerBall");
            // Freeze the warmth drain
            // Begin the warmth ball's expiry timer
            // Trigger the "powered up" animation

            // Unlock kicking on the player
            _player.EnterKickOverride = PowerKickOverride;
        }

        public void WarmthBallCollected(WarmthBall ball)
        {
            GD.Print("Level sees that player got a WarmthBall");
            // Increase the current warmth value, and update the warmth bar
        }

        public void PowerWallPlayerEnteredRange(Breakable entity)
        {
            _doorInRange = entity;
            GD.Print($"Player entered kicking range of door: {entity.Name}");
        }

        public void PowerWallPlayerExitedRange(Breakable entity)
        {
            if (_doorInRange == entity)
            {
                _doorInRange = null;
                GD.Print($"Player exited kicking range of door: {entity.Name}");
            }
        }

        /// <summary>
        /// Reparents the cinematic camera to the Player node, and gives it control.
        /// </summary>
        private void CinematicCameraFollowPlayer(Player player, Camera2D cinematicCamera)
        {
            RemoveChild(cinematicCamera);
            player.AddChild(cinematicCamera);
            cinematicCamera.Owner = player;
            var relativePlayerPos = player.GetGlobalTransformWithCanvas().origin;
            cinematicCamera.Position = -(new Vector2(relativePlayerPos.x, relativePlayerPos.y));
            cinematicCamera.Current = true;
        }

        /// <summary>
        /// Gives the PlayerCamera control again, and returns the cinematic camera to the main scene tree.
        /// </summary>
        private void PlayerCameraTakeControl(Player player, PlayerCamera playerCamera, Camera2D cinematicCamera)
        {
            playerCamera.Current = true;
            player.RemoveChild(cinematicCamera);
            AddChild(cinematicCamera);
            cinematicCamera.Owner = this;
        }

        private async Task FreezingKickOverride(Player player, Timer kickAnimationTimer)
        {
            // Player a slower kick animation that doesn't break breakables, then play some lite dialogue
            player.AnimatePose(StateKicking.FrozenKickName);
            float slowKickDuration = player.GetPoseAnimationDuration(StateKicking.FrozenKickName);
            kickAnimationTimer.Start(slowKickDuration);

            if (!_liteDialogueBox.IsOpen)
            {
                // TODO: variant dialogues
                await Task.Delay(TimeSpan.FromSeconds(slowKickDuration));
                _liteDialogueBox.QueuePortrait("res://art/PlaceholderPortrait.png")
                    .QueueText("T-t-t-").QueueSilence(.35f)
                    .QueueText("too c-c-c-").QueueSilence(.35f)
                    .QueueText("cold to k-k-k-").QueueSilence(.35f)
                    .QueueText("kick...")
                    .QueueBreak();
                await _liteDialogueBox.Open();
                await _liteDialogueBox.Run();
                await _liteDialogueBox.Close();
            }
        }


        private async Task PowerKickOverride(Player player, Timer kickAnimationTimer)
        {
            // Performs a regular kick and, if in door range, does a freeze-frame and zoom-in
            player.AnimatePose(StateKicking.AnimationName);
            kickAnimationTimer.Start(player.GetPoseAnimationDuration(StateKicking.AnimationName));

            // TODO: This can be too late by a frame (or two?) so instead of relying on the event, we should just do a hit-check somehow
            if (_doorInRange != null)
            {
                Breakable doorInRange = _doorInRange; // store local copy, for when the player exits range next frame and the class-wide one gets nulled
                player.InputLocked = true;

                // Wait exactly 200ms, as that's the point in the animation at which the sprite has turned around
                await Task.Delay(200); // TODO: May be safer to watch for a specific frame. Maybe make AnimationPlayer have a callback func here?
                GetTree().Paused = true;

                CinematicCameraFollowPlayer(_player, _cinematicCamera);
                Vector2 cinematicStartPos = _cinematicCamera.Position;

                _tweener.InterpolateProperty(_cinematicCamera, "zoom", null, new Vector2(.5f, .5f), 1f, Tween.TransitionType.Cubic, Tween.EaseType.In);
                _tweener.InterpolateProperty(_cinematicCamera, "position", cinematicStartPos, _cinematicCamera.Position * .5f, 1f, Tween.TransitionType.Cubic, Tween.EaseType.In);
                _tweener.InterpolateProperty(_snowParticles, "modulate", null, new Color(_snowParticles.Modulate, 0), .4f);
                _tweener.Start();
                await this.ToSignalWithArg(_tweener, "tween_completed", 0, _cinematicCamera);

                GetTree().Paused = false;

                await Task.Delay(225); // We're now at 0.4225s, which is juuust after when the hooves fly
                // Screen shake, and special stuff based on which door it was
                _cinematicCamera.Shake(.3f, 100, 24);
                if (doorInRange.Name == "BreakableWall3")
                {
                    CollapseSnowDrift();
                }
                if (doorInRange.Name == "BreakableWall7")
                {
                    CollapseSecondSnowDrift();
                }    

                await ToSignal(kickAnimationTimer, "timeout");

                _tweener.InterpolateProperty(_cinematicCamera, "zoom", null, new Vector2(1f, 1f), .5f, Tween.TransitionType.Cubic, Tween.EaseType.In);
                _tweener.InterpolateProperty(_cinematicCamera, "position", _cinematicCamera.Position, cinematicStartPos, .5f, Tween.TransitionType.Cubic, Tween.EaseType.In);
                _tweener.InterpolateProperty(_snowParticles, "modulate", null, new Color(_snowParticles.Modulate, 1), .1f, Tween.TransitionType.Linear, Tween.EaseType.InOut, .4f);
                _tweener.Start();
                await this.ToSignalWithArg(_tweener, "tween_completed", 0, _cinematicCamera);

                PlayerCameraTakeControl(player, _playerCamera, _cinematicCamera);

                player.InputLocked = false;

                // Now that the door has been kicked down, set the kick override back to "oh no I'm freezing"
                _player.EnterKickOverride = FreezingKickOverride;

                // TODO: If this was the first door, play some "oh no cold again" dialogue
            }
        }

        private void CollapseSnowDrift()
        {
            // TODO: Make this actually animate some sprite that falls and spreads out on the ground
            // For now, just fiddle around with tiles
            // Remove the hanging snow tiles
            var topLeft = GetNode<Position2D>("DoorKnockdownCorners/TopLeft");
            var bottomRight = GetNode<Position2D>("DoorKnockdownCorners/BottomRight");

            var topLeftCell = _midgroundTiles.WorldToMap(topLeft.Position);
            var bottomRightCell = _midgroundTiles.WorldToMap(bottomRight.Position);
            for (int x = (int)topLeftCell.x; x <= bottomRightCell.x; x++)
            {
                for (int y = (int)topLeftCell.y; y <= bottomRightCell.y; y++)
                {
                    if (_midgroundTiles.GetCell(x, y) == 10) // Only remove cell index 10, the snowy ones
                    {
                        _midgroundTiles.SetCellv(new Vector2(x, y), TileMap.InvalidCell);
                    }
                }
            }

            _midgroundTiles.UpdateDirtyQuadrants();

        }

        private void CollapseSecondSnowDrift()
        {
            // Make this clear the skinny barrier in the ice wall, so players can go back and charge up
        }
    }
}


