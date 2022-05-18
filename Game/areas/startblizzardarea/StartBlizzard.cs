using Godot;
using System;
using System.Threading.Tasks;
using ThousandYearsHome.Controls.Dialogue;
using ThousandYearsHome.Controls;
using ThousandYearsHome.Entities;
using ThousandYearsHome.Entities.BreakableEntity;
using ThousandYearsHome.Entities.PlayerEntity;
using ThousandYearsHome.Entities.PowerBallEntity;
using ThousandYearsHome.Entities.WarmthBallEntity;
using ThousandYearsHome.Extensions;

namespace ThousandYearsHome.Areas.StartBlizzardArea
{
    public class StartBlizzard : Node
    {
        private Node _sceneRoot = null!;
        private Player _player = null!;
        private Particles2D _backgroundSnow = null!;
        private Particles2D _firstAreaSnow = null!;
        private PlayerCamera _playerCamera = null!;
        private AnimationPlayer _animator = null!;
        private AnimationPlayer _fadeAnimator = null!;
        private Tween _tweener = null!;
        private ColorRect _fader = null!;
        private DialogueBox _dialogueBox = null!;
        private DialogueBox _liteDialogueBox = null!;
        private HUD _hud = null!;
        private CinematicCamera _cinematicCamera = null!;
        private TileMap _midgroundTiles = null!;
        private WarmthBallWatcher _warmthBallWatcher = null!;
        private HornCollectibleSignalBus _collectibleSignalBus = null!;

        private TextureRect _backgroundCity = null!;
        private TextureRect _backgroundCityLarge = null!;
        private Area2D _firstFallDialogueTrigger = null!;
        private Area2D _vistaPointTrigger = null!;
        private Area2D _collapseTrigger = null!;
        private Position2D _collapseTopLeft = null!;
        private Position2D _collapseBottomRight = null!;
        private Position2D _fallTeleportTop = null!;
        private Area2D _fallTeleportBottomArea = null!;
        private Area2D _collapseLandingTriggerArea = null!;
        private Area2D _firstCaveChillArea = null!;
        private Node _keeperCutscene = null!;
        private Area2D _keeperCutsceneTriggerArea = null!;
        private Position2D _keeperCutsceneCameraPosition = null!;
        private Sprite _normalKeeperSprite = null!;
        private Sprite _blackenedKeeperSprite = null!;
        private Sprite _keeperMask = null!;
        private SpriteDissolve _keeperDissolver = null!;
        private ColorRect _keeperCutsceneWhiteoutRect = null!;
        private Timer _warmthDrainTimer = null!;
        private ShaderMaterial _silhouetteShader = null!;

        private bool _playerHasPowerBall = false;
        private const float DefaultWarmthDrainPerTick = .3f;
        private float _levelWarmthDrainPerTick = DefaultWarmthDrainPerTick;

        private Breakable? _doorInRange;

        public bool SkipIntro { get; set; }

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            _sceneRoot = GetTree().Root.GetNode<Node>("StartBlizzard");
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
            _backgroundSnow = GetNode<Particles2D>("ParallaxBackground/BackgroundSnowfallParticles");
            _backgroundCity = GetNode<TextureRect>("ParallaxBackground/ParallaxLayer/BackgroundCity");
            _backgroundCityLarge = GetNode<TextureRect>("ParallaxBackground/ParallaxLayer/BackgroundCityLarge");
            _firstAreaSnow = GetNode<Particles2D>("FirstAreaSnowParticles");
            _cinematicCamera = GetNode<CinematicCamera>("CinematicCamera");
            _midgroundTiles = GetNode<TileMap>("MidgroundTiles");
            _warmthBallWatcher = GetNode<WarmthBallWatcher>("UICanvas/WarmthBallWatcher");
            _warmthDrainTimer = GetNode<Timer>("WarmthDrainTimer");            
            _silhouetteShader = new ShaderMaterial { Shader = ResourceLoader.Load<Shader>("res://shaders/Silhouette.gdshader") };

            _collectibleSignalBus = GetNode<HornCollectibleSignalBus>("/root/HornCollectibleSignalBus");
            _collectibleSignalBus.Connect(nameof(HornCollectibleSignalBus.PowerBallCollected), this, nameof(PowerBallCollected));

            _firstFallDialogueTrigger = GetNode<Area2D>("FirstFallDialogueTrigger");
            _vistaPointTrigger = GetNode<Area2D>("VistaPointDialogueTrigger");

            _collapseTrigger = GetNode<Area2D>("CollapseTriggerArea");
            _collapseTopLeft = GetNode<Position2D>("CollapsePositions/CollapseTopLeft");
            _collapseBottomRight = GetNode<Position2D>("CollapsePositions/CollapseBottomRight");

            _fallTeleportTop = GetNode<Position2D>("FallTeleportTop");
            _fallTeleportBottomArea = GetNode<Area2D>("FallTeleportBottomArea");

            _collapseLandingTriggerArea = GetNode<Area2D>("CollapseLandingTriggerArea");

            _firstCaveChillArea = GetNode<Area2D>("FirstCaveChillArea");

            _keeperCutscene = GetNode<Node>("KeeperCutscene");
            _keeperCutsceneTriggerArea = _keeperCutscene.GetNode<Area2D>("KeeperCutsceneTriggerArea");
            _blackenedKeeperSprite = _keeperCutscene.GetNode<Sprite>("Keeper/BackBufferCopy/BlackenedKeeperSprite");
            _normalKeeperSprite = _keeperCutscene.GetNode<Sprite>("Keeper/NormalKeeperSprite");
            _keeperMask = _keeperCutscene.GetNode<Sprite>("Keeper/BackBufferCopy/Mask");
            _keeperDissolver = _keeperCutscene.GetNode<SpriteDissolve>("KeeperDissolver");
            _keeperCutsceneWhiteoutRect = _keeperCutscene.GetNode<ColorRect>("KeeperCutsceneWhiteoutRect");

            Vector2 startPos = GetNode<Position2D>("StartPosition").Position;
            _player.Spawn(startPos);
            _warmthBallWatcher.Init(_playerCamera);

            _player.Connect(nameof(Player.PreKicking), this, nameof(PlayerPreKicking));

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

        public async void WaitStartTimerTimeout()
        {
            if (!SkipIntro)
            {
                // Initial camera Pan
                _tweener.InterpolateProperty(_cinematicCamera, "global_position", null, new Vector2(0, -21), 6.5f, Tween.TransitionType.Cubic, Tween.EaseType.InOut);
                _tweener.Start();
                await this.ToSignalWithArg(_tweener, "tween_completed", 0, _cinematicCamera);

                // Corina walks on and talks                
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

        public void OnHideCityTriggered(Node body)
        {
            if (body is Player)
            {
                _backgroundCity.Visible = false;
            }
        }

        public void OnShowCityTriggered(Node body)
        {
            if (body is Player)
            {
                _backgroundCity.Visible = true;
            }
        }

        public void SwapBackgroundCityAreaTriggered(Node body)
        {
            if (body is Player)
            {
                GetNode<Area2D>("ShowCityTriggerArea2").Monitoring = true;
                _backgroundCity.Visible = false;
                _backgroundCityLarge.Visible = false;
            }
        }

        public void OnHideCityLargeTriggered(Node body)
        {
            if (body is Player)
            {                
                _backgroundCityLarge.Visible = false;
            }
        }
       
        public void OnShowCityLargeTriggered(Node body)
        {
            if (body is Player)
            {
                _backgroundCityLarge.Visible = true;
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

                // Hide the snowfall from the first area, so we don't have indoor snowfall
                _firstAreaSnow.Emitting = false;
                _tweener.InterpolateProperty(_firstAreaSnow, "modulate", null, new Color(_firstAreaSnow.Modulate, 0), .2f);

                // falling dialogue
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
                    // TODO: Play a shiver animation here?
                    .QueueBreak()
                    .QueueText("\nI'll be okay in here for a little while, but if I have to go outside again, that wind is going to slice right through me. I've got to find some shelter, and fast...");
                await _dialogueBox.Run();
                await ToSignal(_dialogueBox, nameof(DialogueBox.DialogueBoxClosed));

                PlayerCameraTakeControl(_player, _playerCamera, _cinematicCamera);

                // Enable chill area, which should cause warmth to start draining
                _firstCaveChillArea.Monitoring = true;
                _warmthDrainTimer.Start();

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

                // Stop draining warmth and hide the warmth bar, and disable the cave chill area--everything is cold now
                _firstCaveChillArea.Monitoring = false;
                _warmthDrainTimer.Stop();
                _hud.HideWarmthBar(); // TODO: Make this a cleaner fade

                await _dialogueBox.Open();
                _dialogueBox.QueuePortrait("res://art/PlaceholderPortrait.png")
                    .QueueText("An exit! But...", 0.01f)
                    .QueueBreak()
                    .QueueClear()
                    .QueueText("...what in seasons' name is THAT?", 0.03f);
                await _dialogueBox.Run();
                await ToSignal(_dialogueBox, nameof(DialogueBox.DialogueBoxClosed));

                // Pan camera over to keeper body
                _cinematicCamera.SmoothingEnabled = true;
                CinematicCameraTakeControl(_cinematicCamera, _playerCamera);                
                _keeperCutsceneCameraPosition = _keeperCutscene.GetNode<Position2D>("KeeperCutsceneCameraPosition");
                _cinematicCamera.Position = _keeperCutsceneCameraPosition.Position; 

                var walkEnd = _keeperCutscene.GetNode<Position2D>("KeeperCutsceneWalkEndPosition");                
                _player.AnimatePose("Walk");
                _tweener.InterpolateProperty(_player, "position", null, new Vector2(walkEnd.Position.x, _player.Position.y), 4.0f, Tween.TransitionType.Quad, Tween.EaseType.Out);
                _tweener.Start();
                await this.ToSignalWithArg(_tweener, "tween_completed", 0, _player);
                _player.AnimatePose("Idle");

                await _dialogueBox.Open();
                _dialogueBox.QueuePortrait("res://art/PlaceholderPortrait.png")
                    .QueueText("It looks like a unicorn...")
                    .QueueBreak()
                    .QueueText("\n...but with wings.");
                await _dialogueBox.Run();
                await ToSignal(_dialogueBox, nameof(DialogueBox.DialogueBoxClosed));

                // Walk forward slightly, crouch
                _player.AnimatePose("Walk");
                _tweener.InterpolateProperty(_player, "position", null, new Vector2(_player.Position.x + 10, _player.Position.y), .25f);
                _tweener.Start();
                await this.ToSignalWithArg(_tweener, "tween_completed", 0, _player);
                _player.AnimatePose("Crouch");

                await _dialogueBox.Open();
                _dialogueBox.QueuePortrait("res://art/PlaceholderPortrait.png")
                    .QueueText("Hm, what's this it's holding?");
                await _dialogueBox.Run();
                await ToSignal(_dialogueBox, nameof(DialogueBox.DialogueBoxClosed));

                _player.AnimatePose("Idle"); // TODO: Make this some kind of holding animation

                // TODO: Reposition relic atop Corina's hoof
                // TODO: Relic flaring animation here

                await _dialogueBox.Open(false);
                _dialogueBox.QueuePortrait("res://art/PlaceholderPortrait.png")
                    .QueueText("W--!")
                    .QueueSilence(.5f);
                await _dialogueBox.Run();
                await _dialogueBox.Close(false);

                // Fade out the normal keeper sprite in favor of the silhouetted one,
                // White out the screen,
                // Blacken the player sprite
                _keeperCutsceneWhiteoutRect.Visible = true;
                _silhouetteShader.SetShaderParam("percent", 0f);
                _player.SetShader(_silhouetteShader);
                _tweener.InterpolateProperty(_keeperCutsceneWhiteoutRect, "modulate", _keeperCutsceneWhiteoutRect.Modulate, new Color(_keeperCutsceneWhiteoutRect.Modulate, 1f), 1f);
                _tweener.InterpolateProperty(_normalKeeperSprite, "modulate", _normalKeeperSprite.Modulate, new Color(_normalKeeperSprite.Modulate, 0), 1f);
                _tweener.InterpolateProperty(_silhouetteShader, "shader_param/percent", 0f, 1f, 1f);
                _tweener.Start();
                await Task.Delay(1000);

                // Dissolve the keeper
                _keeperDissolver.Initialize(_blackenedKeeperSprite.Texture, riseSpeed: 30f);
                _tweener.InterpolateProperty(_keeperMask, "position", _keeperMask.Position, Vector2.Zero, 5f, Tween.TransitionType.Linear);
                _tweener.Start();

                // TODO: Dissolve the relic

                // 5 seconds for the dissolution, + 2 second for dramatic effect
                await Task.Delay(7000);

                _player.AnimatePose("Crouch");

                // Fade back in
                _silhouetteShader.SetShaderParam("reverse", true);
                _silhouetteShader.SetShaderParam("percent", 0);
                _tweener.InterpolateProperty(_keeperCutsceneWhiteoutRect, "modulate", _keeperCutsceneWhiteoutRect.Modulate, new Color(_keeperCutsceneWhiteoutRect.Modulate, 0f), 1f);
                _tweener.InterpolateProperty(_silhouetteShader, "shader_param/percent", 0f, 1f, 1f);
                _tweener.Start();
                await Task.Delay(1000);

                _keeperCutscene.GetNode<Node2D>("Keeper").Hide(); // Hide all the gunk used for the keeper shenanigans

                // Smoothly focus back on the player
                _playerCamera.ForceCameraRectUpdate();
                _cinematicCamera.Position = _player.Position - _playerCamera.TargetRect.Position;
                await Task.Delay(200);
                PlayerCameraTakeControl(_player, _playerCamera, _cinematicCamera);
                _player.ClearShader();                

                await _dialogueBox.Open();
                _dialogueBox.QueuePortrait("res://art/PlaceholderPortrait.png")
                    .QueueText("What...")
                    .QueueBreak()
                    .QueueText("\n...in sweet sun's name was that?", tag: StandardTags.StandUp);
                await _dialogueBox.Run();
                await ToSignal(_dialogueBox, nameof(DialogueBox.DialogueBoxClosed));

                // Return control to player, lock down their ability to kick, begin draining warmth at the level's global drain rate
                _warmthDrainTimer.Start();
                _player.WarmthDrainPerTick = _levelWarmthDrainPerTick;
                _playerCamera.Current = true;
                _player.EnterKickOverride = FreezingKickOverride;
                _keeperCutsceneTriggerArea.QueueFree();
                _hud.ShowWarmthBar(); // TODO: Make this a cleaner fade
                _player.InputLocked = false;

                // TODO: Make more snow happen here!
            }
        }

        public void WarmthDrainTimerTimeout()
        {    
            if (_player.Warmth <= 0 && _player.ExcessWarmth <= 0)
            {
                // TODO: Trigger "death"
            }

            if (_player.Warmth <= 0 && _player.ExcessWarmth > 0)
            {
                _player.ExcessWarmth -= _player.WarmthDrainPerTick * 2;
            }
            else
            {
                _player.Warmth -= _player.WarmthDrainPerTick;            
            }
        }

        public void PowerBallCollected(PowerBall ball)
        {
            GD.Print("Level sees that player got a PowerBall");
            // TODO: Freeze the warmth drain
            //      Begin the warmth ball's expiry timer
            //      Trigger the "powered up" animation
            //      Maybe this part should be handled by the Player, and not the level

            _playerHasPowerBall = true;
            // Unlock kicking on the player
            _player.EnterKickOverride = null;
        }

        /// <summary>
        /// Places the cinematic camera at exactly the point the player camera is at,
        /// then gives it control.
        /// </summary>
        private void CinematicCameraTakeControl(CinematicCamera cinematicCamera, PlayerCamera playerCamera)
        {
            cinematicCamera.Position = playerCamera.CurrentViewportRect.Position;
            cinematicCamera.Current = true;
        }

        /// <summary>
        /// Reparents the cinematic camera to the Player node, and gives it control.
        /// </summary>
        private void CinematicCameraFollowPlayer(Player player, CinematicCamera cinematicCamera)
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
        private void PlayerCameraTakeControl(Player player, PlayerCamera playerCamera, CinematicCamera cinematicCamera)
        {
            playerCamera.Current = true;
            Node? cinematicParent = cinematicCamera.GetParent();
            if (cinematicParent == player)
            {
                player.RemoveChild(cinematicCamera);                
            }
            if (cinematicParent != this)
            {
                AddChild(cinematicCamera);
            }
            cinematicCamera.Owner = this;
        }

        private async Task FreezingKickOverride(Player player, Timer kickTimer)
        {
            // Player a slower kick animation that doesn't break breakables, then play some lite dialogue
            player.AnimatePose(StateKicking.FrozenKickName);
            float slowKickDuration = player.GetPoseAnimationDuration(StateKicking.FrozenKickName);
            kickTimer.Start(slowKickDuration);

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


        private async void PlayerPreKicking(Player player)
        {
            var overlaps = player.KickHurtSentinel.GetOverlappingAreas();
            if (overlaps == null || overlaps.Count <= 0)
            {
                return;
            }

            // Just check the first overlap, this level doesn't have any overlapping doors
            if (!(overlaps[0] is Area2D breakableArea))
            {
                return;
            }            

            if (!_playerHasPowerBall)
            {
                return;
            }

            Breakable doorInRange = breakableArea.GetParent<Breakable>();

            player.InputLocked = true;            
            GetTree().Paused = true;

            CinematicCameraFollowPlayer(_player, _cinematicCamera);
            Vector2 cinematicStartPos = _cinematicCamera.Position;

            _tweener.InterpolateProperty(_cinematicCamera, "zoom", null, new Vector2(.5f, .5f), 1f, Tween.TransitionType.Cubic, Tween.EaseType.In);
            _tweener.InterpolateProperty(_cinematicCamera, "position", cinematicStartPos, _cinematicCamera.Position * .5f, 1f, Tween.TransitionType.Cubic, Tween.EaseType.In);
            // TODO: Fade snow out //_tweener.InterpolateProperty(_snowParticles, "modulate", null, new Color(_snowParticles.Modulate, 0), .4f);
            _tweener.Start();
            await this.ToSignalWithArg(_tweener, "tween_completed", 0, _cinematicCamera);

            GetTree().Paused = false;

            await Task.Delay(225); // Wait until juuust after when the hooves fly
            // Screen shake, and special stuff based on which door it was
            _cinematicCamera.Shake(.3f, 100, 24);
            if (doorInRange.Name == "BreakableWall3")
            {
                CollapseFirstSnowDrift();
            }
            if (doorInRange.Name == "BreakableWall7")
            {
                CollapseSecondSnowDrift();
            }    

            await ToSignal(player, nameof(Player.KickExited));

            _tweener.InterpolateProperty(_cinematicCamera, "zoom", null, new Vector2(1f, 1f), .5f, Tween.TransitionType.Cubic, Tween.EaseType.In);
            _tweener.InterpolateProperty(_cinematicCamera, "position", _cinematicCamera.Position, cinematicStartPos, .5f, Tween.TransitionType.Cubic, Tween.EaseType.In);
            //TODO: Fade snow in  _tweener.InterpolateProperty(_snowParticles, "modulate", null, new Color(_snowParticles.Modulate, 1), .1f, Tween.TransitionType.Linear, Tween.EaseType.InOut, .4f);
            _tweener.Start();
            await this.ToSignalWithArg(_tweener, "tween_completed", 0, _cinematicCamera);

            PlayerCameraTakeControl(player, _playerCamera, _cinematicCamera);

            player.InputLocked = false;

            // Now that the door has been kicked down, set the kick override back to "oh no I'm freezing"
            _player.EnterKickOverride = FreezingKickOverride;

            // TODO: If this was the first door, play some "oh no cold again" dialogue

            // TODO: Clear the player's PowerBall status
            
        }

        private void CollapseFirstSnowDrift()
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

        public void OnDialogueTag(string tag)
        {
            if (tag == StandardTags.StandUp)
            {
                _player.AnimatePose("Idle");
            }
        }
    }
}


