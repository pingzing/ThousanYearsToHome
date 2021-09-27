using Godot;
using System;
using System.Security.Policy;
using System.Threading.Tasks;
using ThousandYearsHome.Extensions;

namespace ThousandYearsHome.Controls
{
    public class DialogueBox : Control
    {
        [Signal] public delegate void DialogueBoxOpened();
        [Signal] public delegate void DialogueBoxClosed();

        private Node _textInterfaceEngine = null!;
        private TextInterfaceEngineShim _dialgoueBoxWrapper = null!;
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
            _textInterfaceEngine = GetNode("TextInterfaceEngine");
            _nextArrow = GetNode<TextureRect>("NextArrow");
            _dialgoueBoxWrapper = new TextInterfaceEngineShim(_textInterfaceEngine);
            _dialgoueBoxWrapper.SetBreakKeyScancode(KeyList.Z); // Unconditonally set the break key to "Z", because that's our confirm button.
            _animator = GetNode<AnimationPlayer>("DialogueBoxAnimator");
            _nextArrowAnimator = GetNode<AnimationPlayer>("NextArrowAnimator");
        }

        public void OnAnimationFinished(string name)
        {
            if (name == "Show")
            {
                _showCompletionSource?.SetResult("Show");
                EmitSignal(nameof(DialogueBoxOpened));
            }
            if (name == "Hide")
            {
                _hideCompletionSource?.SetResult("Hide");
                EmitSignal(nameof(DialogueBoxClosed));
                _dialgoueBoxWrapper.Reset();
            }
        }

        public void OnTextInterfaceEngineBreak()
        {
            _nextArrowAnimator.Play("BlinkArrow");
        }

        public void OnTextInterfaceEngineBreakResume()
        {
            _nextArrowAnimator.Stop(reset: true);
        }

        public void OnTextInterfaceEngineTagFound(string tag)
        {
            if (tag == EndSinglePage)
            {
                _singlePageCompletionSource?.SetResult(tag);
            }
        }

        public void OnTextInterfaceEngineBufferEnd()
        {
            _nextArrowAnimator.Play("BlinkArrow");
            _bufferEmptied = true;
            _bufferEndCompletionSource?.SetResult("");
        }

        public Task Open()
        {
            _isOpen = true;
            _showCompletionSource = new TaskCompletionSource<string>();
            _animator.Play("Show");
            return _showCompletionSource.Task;
        }

        public Task Close()
        {
            _isOpen = false;
            _hideCompletionSource = new TaskCompletionSource<string>();
            _animator.Play("Hide");
            return _hideCompletionSource.Task;
        }

        // Called every frame. 'delta' is the elapsed time since the previous frame.
        public override void _Process(float delta)
        {
            if (!_isOpen)
            {
                return;
            }

            if (_bufferEmptied)
            {
                if (Input.IsActionPressed("ui_accept"))
                {
                    _ = Close();
                }
            }
        }

        /// <summary>
        /// Convenience method for displaying one page of text at a single speed.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes once the given text has finished displaying.</returns>
        public Task ShowSinglePage(string text, float secondsPerChar)
        {
            _singlePageCompletionSource = new TaskCompletionSource<string>();
            _bufferEmptied = false;
            _dialgoueBoxWrapper.BufferText(text, secondsPerChar);
            _dialgoueBoxWrapper.BufferBreak(EndSinglePage);
            _dialgoueBoxWrapper.SetState(TextInterfaceEngineState.StateOutput);
            return _singlePageCompletionSource.Task;
        }

        public void LoadText(string text, float velocity = 0, string tag = "", bool pushFront = false)
        {
            _bufferEmptied = false;
            _dialgoueBoxWrapper.BufferText(text, velocity, tag, pushFront);
        }

        public void LoadSilence(float lengthSeconds, string tag = "", bool pushFront = false)
        {
            _bufferEmptied = false;
            _dialgoueBoxWrapper.BufferSilence(lengthSeconds, tag, pushFront);
        }

        public void LoadBreak(string tag = "", bool pushFront = false)
        {
            _bufferEmptied = false;
            _dialgoueBoxWrapper.BufferBreak(tag, pushFront);
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
            _dialgoueBoxWrapper.SetState(TextInterfaceEngineState.StateOutput);
            return _bufferEndCompletionSource.Task;
        }
    }
}

