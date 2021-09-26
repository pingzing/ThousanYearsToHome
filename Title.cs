using Godot;
using System.Threading.Tasks;

namespace ThousandYearsHome
{
    public class Title : Node
    {

        private Label _titleLabel;
        private VBoxContainer _menuVBox;
        private AnimationPlayer _fadeAnimator;
        private Button _startButton;
        private Button _quitButton;
        private Particles2D _mainParticles;
        private Particles2D _secondaryParticles;

        private ResourceInteractiveLoader _newGameLoader;

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            _fadeAnimator = GetNode<AnimationPlayer>("FadeAnimator");
            _startButton = GetNode<Button>("MenuVBox/StartGame");
            _quitButton = GetNode<Button>("MenuVBox/QuitGame");
            _mainParticles = GetNode<Particles2D>("MainParticles");
            _secondaryParticles = GetNode<Particles2D>("SecondaryParticles");
            _titleLabel = GetNode<Label>("TitleLabel");
            _titleLabel.SelfModulate = new Color(_titleLabel.SelfModulate, a: 0);
            _menuVBox = GetNode<VBoxContainer>("MenuVBox");
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
                LoadNewGame();
            }
        }

        public void OnStartGamePressed()
        {
            _menuVBox.SetProcessInput(false);
            _fadeAnimator.Play("Fade Out Screen");
        }

        public void OnQuitGamePressed()
        {
            GetTree().Notification(MainLoop.NotificationWmQuitRequest);
        }

        private void LoadNewGame()
        {
            // Force completion of _newGameLoader
            _newGameLoader.Wait();

            // Get and instance the loaded scene
            var newGameLevel = _newGameLoader.GetResource();
            var root = GetTree().Root;
            var newGameScene = (newGameLevel as PackedScene).Instance();

            // Remove the current scene
            var currentSceneRoot = root.GetNode("Title");
            root.RemoveChild(currentSceneRoot);
            currentSceneRoot.CallDeferred("free");

            // Add the new scene
            root.AddChild(newGameScene);
        }
    }

}