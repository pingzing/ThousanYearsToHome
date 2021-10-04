using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;
using ThousandYearsHome.Areas;
using ThousandYearsHome.Entities.PlayerEntity;

namespace ThousandYearsHome
{
    public class Title : Node
    {

        private Label _titleLabel = null!;
        private VBoxContainer _menuVBox = null!;
        private VBoxContainer _stageSelectVBox = null!;
        private AnimationPlayer _fadeAnimator = null!;
        private Button _startButton = null!;
        private Button _stageSelectButton = null!;
        private Button _stageSelectBackButton = null!;

        private ResourceInteractiveLoader _newGameLoader = null!;

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            _fadeAnimator = GetNode<AnimationPlayer>("FadeAnimator");
            _startButton = GetNode<Button>("MenuVBox/StartGame");
            _titleLabel = GetNode<Label>("TitleLabel");
            _menuVBox = GetNode<VBoxContainer>("MenuVBox");
            _stageSelectVBox = GetNode<VBoxContainer>("StageSelectVBox");
            _stageSelectBackButton = GetNode<Button>("StageSelectVBox/BackButton");
            _stageSelectButton = GetNode<Button>("MenuVBox/StageSelect");
            _titleLabel.SelfModulate = new Color(_titleLabel.SelfModulate, a: 0);
            _menuVBox.Modulate = new Color(_menuVBox.Modulate, a: 0);

            // Begin loading the "new game" scene immediately on boot
            _newGameLoader = ResourceLoader.LoadInteractive("res://Areas/StartBlizzard.tscn");
        }

        public void ShowTitleTimerTimeout()
        {
            _fadeAnimator.Play("Fade In Title");
        }

        public void ShowMenuTimerTimeout()
        {
            _fadeAnimator.Play("Fade In Menu");
        }

        public async void FadeAnimationFinished(string animationName)
        {
            if (animationName == "Fade In Menu")
            {
                _startButton.GrabFocus();
            }

            if (animationName == "Fade Out Screen")
            {
                _fadeAnimator.Play("Speed Up Snow");
            }

            if (animationName == "Speed Up Snow")
            {
                await Task.Delay(4000); // Linger on the snowy black screen for drama
                LoadNewGame(false);
            }
        }

        public void OnStartGamePressed()
        {
            _menuVBox.SetProcessInput(false);
            _fadeAnimator.Play("Fade Out Screen");
        }

        public void OnContinuePressed()
        {
            // TODO: Load player's save, load level/checkpoint/whatever
            LoadNewGame(true);
        }

        private bool _stageSelectInited = false;
        public void OnStageSelectPressed()
        {
            _menuVBox.Hide();
            _stageSelectVBox.Show();
            _stageSelectBackButton.CallDeferred("grab_focus");

            if (!_stageSelectInited)
            {
                List<string> scenePaths = new List<string>();
                var debugStages = new Directory();
                if (debugStages.Open("res://Areas/Debug") == Error.Ok)
                {
                    debugStages.ListDirBegin(skipNavigational: true);
                    {
                        var fileInDebugfolder = debugStages.GetNext();
                        while (fileInDebugfolder != "")
                        {
                            if (fileInDebugfolder.EndsWith(".tscn"))
                            {
                                scenePaths.Add($"res://Areas/Debug/{fileInDebugfolder}");
                            }
                            fileInDebugfolder = debugStages.GetNext();
                        }
                    }
                    debugStages.ListDirEnd();
                }

                foreach (string path in scenePaths)
                {
                    Button stageButton = new Button();
                    stageButton.Text = path;
                    stageButton.Connect("pressed", this, "OnStagePressed");
                    _stageSelectVBox.AddChild(stageButton);
                }

                _stageSelectInited = true;
            }
        }


        public void OnStageSelectBackPressed()
        {
            _stageSelectVBox.Hide();
            _menuVBox.Show();
            _stageSelectButton.CallDeferred("grab_focus");
        }

        public void OnStagePressed()
        {
            var focusedButton = _stageSelectBackButton.GetFocusOwner() as Button;
            Resource? selectedStageResource = ResourceLoader.Load(focusedButton.Text);
            var selectedStageScene = (selectedStageResource as PackedScene)?.Instance();
            LoadStage(selectedStageScene);
        }

        public void OnQuitGamePressed()
        {
            GetTree().Notification(MainLoop.NotificationWmQuitRequest);
        }

        private void LoadNewGame(bool skipIntro)
        {
            // Force completion of _newGameLoader
            _newGameLoader.Wait();

            // Get and instance the loaded scene
            var newGameLevel = _newGameLoader.GetResource();
            var root = GetTree().Root;
            StartBlizzard? newGameScene = (newGameLevel as PackedScene)?.Instance() as StartBlizzard;
            newGameScene.SkipIntro = skipIntro;

            LoadStage(newGameScene);
        }

        private void LoadStage(Node stage)
        {
            var root = GetTree().Root;

            // Remove the current scene
            var currentSceneRoot = root.GetNode("Title");
            root.RemoveChild(currentSceneRoot);
            currentSceneRoot.CallDeferred("free");

            // Add the new scene
            root.AddChild(stage);
        }
    }

}
