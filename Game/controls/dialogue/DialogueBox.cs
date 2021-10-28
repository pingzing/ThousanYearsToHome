using Godot;
using System.Threading.Tasks;

namespace ThousandYearsHome.Controls.Dialogue
{
    /// <summary>
    /// The wrapper that make the DialogueEngine look good.
    /// Provides borders, backgrounds, portraits, and convenience methods.
    /// </summary>
    public class DialogueBox : Control
    {
        [Signal] public delegate void DialogueBoxOpened();
        [Signal] public delegate void DialogueBoxClosed();

        private DialogueEngine _dialogueEngine = null!;
        private TextureRect _nextArrow = null!;
        private AnimationPlayer _animator = null!;
        private AnimationPlayer _nextArrowAnimator = null!;

        private bool _isOpen = false;
        private const string EndSinglePage = "endsinglepage";
        private bool _bufferEmptied = false;

        private TaskCompletionSource<string>? _showCompletionSource;
        private TaskCompletionSource<string>? _hideCompletionSource;

        private TaskCompletionSource<string>? _bufferEndCompletionSource;
        private TaskCompletionSource<string>? _singlePageCompletionSource;

        public override void _Ready()
        {
            _nextArrow = GetNode<TextureRect>("NextArrow");
            _dialogueEngine = GetNode<DialogueEngine>("DialogueEngine");
            _dialogueEngine.BreakKey = KeyList.Z; // TODO: Make this user-configurable, I guess.
            _animator = GetNode<AnimationPlayer>("DialogueBoxAnimator");
            _nextArrowAnimator = GetNode<AnimationPlayer>("NextArrowAnimator");
        }

        public override void _Process(float delta)
        {
            if (!_isOpen)
            {
                return;
            }

            if (_bufferEmptied)
            {
                if (Input.IsActionJustPressed("ui_accept"))
                {
                    _ = Close();
                }
            }
        }

        public void OnAnimationFinished(string name)
        {
            if (name == "Show")
            {
                _showCompletionSource?.SetResult("Show");
                _isOpen = true;
                EmitSignal(nameof(DialogueBoxOpened));
            }
            if (name == "Hide")
            {
                _hideCompletionSource?.SetResult("Hide");
                _isOpen = false;
                EmitSignal(nameof(DialogueBoxClosed));
                _dialogueEngine.Reset();
            }
        }

        public void DialogueEngineBreakEntered()
        {
            _nextArrowAnimator.Play("BlinkArrow");
        }

        public void DialogueEngineBreakExited()
        {
            _nextArrowAnimator.Stop(reset: true);
            _nextArrow.Visible = false;
        }

        public void DialogueEngineTagEncountered(string tag)
        {
            if (tag == EndSinglePage)
            {
                _singlePageCompletionSource?.SetResult(tag);
            }
        }

        public void DialogueEngineBufferEmptied()
        {
            _nextArrowAnimator.Play("BlinkArrow");
            _bufferEmptied = true;
            _bufferEndCompletionSource?.SetResult("");
        }

        public Task Open()
        {
            _showCompletionSource = new TaskCompletionSource<string>();
            _animator.Play("Show");
            return _showCompletionSource.Task;
        }

        public Task Close()
        {
            _hideCompletionSource = new TaskCompletionSource<string>();
            _animator.Play("Hide");
            return _hideCompletionSource.Task;
        }

        /// <summary>
        /// Convenience method for displaying one page of text at a single speed.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes once the given text has finished displaying.</returns>
        public Task ShowSinglePage(string text, float secondsPerChar)
        {
            _singlePageCompletionSource = new TaskCompletionSource<string>();
            _bufferEmptied = false;
            _dialogueEngine.QueueText(text, secondsPerChar);
            _dialogueEngine.QueueBreak(EndSinglePage);
            _dialogueEngine.SetState(DialogueEngineState.Outputting);
            return _singlePageCompletionSource.Task;
        }

        public void QueueText(string text, float velocity = 0, string tag = "", bool pushFront = false)
        {
            _bufferEmptied = false;
            _dialogueEngine.QueueText(text, velocity, tag, pushFront);
        }

        public void QueueSilence(float lengthSeconds, string tag = "", bool pushFront = false)
        {
            _bufferEmptied = false;
            _dialogueEngine.QueueSilence(lengthSeconds, tag, pushFront);
        }

        public void QueueBreak(string tag = "", bool pushFront = false)
        {
            _bufferEmptied = false;
            _dialogueEngine.QueueBreak(tag, pushFront);
        }

        /// <summary>
        /// Begins displaying the buffered contents of the dialogue box. Completes when
        /// we receive a "buffer_end" signal from the underlying text interface engine.
        /// </summary>
        public Task Run()
        {
            if (_bufferEmptied)
            {
                // Return immediately if there's (probably) nothing in the buffer
                return Task.CompletedTask;
            }

            _bufferEndCompletionSource = new TaskCompletionSource<string>();
            _dialogueEngine.SetState(DialogueEngineState.Outputting);
            return _bufferEndCompletionSource.Task;
        }
    }
}

