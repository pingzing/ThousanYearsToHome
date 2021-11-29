using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThousandYearsHome.Controls.Dialogue
{
    /// <summary>
    /// The core of the dialogue engine that wraps the RichTextLabel, that processes
    /// payloads, provides word-wrapping and automatic paging.
    /// </summary>
    public class DialogueBox : Control
    {
        // Members
        private List<IDialoguePayload> _buffer = new List<IDialoguePayload>();
        private DialogueBoxState _currState = DialogueBoxState.Waiting;
        private bool _isBreak = false;
        private uint _maxLineCount = 0;
        private bool _dialogueBoxFull = false;
        private bool _printingText = false;
        private bool _isOpen = false;
        private bool IsPortraitVisible => _portraitMargin.Visible;
        private bool _bufferEmptied = false;
        private int _currentLineIndex = 0;
        private float? _labelWidthNoPortrait = null;
        private float? _labelWidthWithPortrait = null!;
        private float[] _lineSpaceRemaining = null!;
        private DialogueSegmenter _segmenter = null!;

        private TaskCompletionSource<string>? _showCompletionSource;
        private TaskCompletionSource<string>? _hideCompletionSource;

        private TaskCompletionSource<string>? _bufferEndCompletionSource;
        private TaskCompletionSource<string>? _singlePageCompletionSource;

        // Nodes
        private Control _portraitMargin = null!;
        private TextureRect _portrait = null!;
        private Control _labelMargin = null!;
        private RichTextLabel _label = null!;
        private Timer _characterTickTimer = null!;
        private Timer _silenceTimer = null!;

        private TextureRect _nextArrow = null!;
        private AnimationPlayer _animator = null!;
        private AnimationPlayer _nextArrowAnimator = null!;

        // Exported properties

        /// <summary>
        /// The font to use in the backing RichTextLabel.
        /// </summary>
        [Export] public Font? Font = null;

        // Signals

        [Signal] public delegate void DialogueBoxOpened();
        [Signal] public delegate void DialogueBoxClosed();

        /// <summary>
        /// Fired when the buffer encounters a pyaload with a tag.
        /// </summary>
        /// <param name="tag">The tag that was encountered.</param>
        [Signal] public delegate void TagEncountered(string tag);

        /// <summary>
        /// Fired when the buffer is cleared of all text.
        /// </summary>
        [Signal] public delegate void BufferCleared();

        public override void _Ready()
        {
            SetPhysicsProcess(true);
            SetProcessInput(true);
            _portraitMargin = GetNode<Control>("Background/HBoxContainer/PortraitMargin");
            _portrait = GetNode<TextureRect>("Background/HBoxContainer/PortraitMargin/Portrait");
            _labelMargin = GetNode<Control>("Background/HBoxContainer/LabelMargin");
            _label = GetNode<RichTextLabel>("Background/HBoxContainer/LabelMargin/Label");
            _label.ScrollActive = false;
            _label.ScrollFollowing = false;
            _label.Clear();
            _label.VisibleCharacters = 0;

            _characterTickTimer = GetNode<Timer>("CharacterTickTimer");
            _silenceTimer = GetNode<Timer>("SilenceTimer");

            _nextArrow = GetNode<TextureRect>("NextArrow");
            BreakKey = KeyList.Z; // TODO: Make this user-configurable, I guess.
            _animator = GetNode<AnimationPlayer>("DialogueBoxAnimator");
            _nextArrowAnimator = GetNode<AnimationPlayer>("NextArrowAnimator");

            if (Font != null)
            {
                _label.AddFontOverride("normal_font", Font);
            }
            else
            {
                Font = _label.GetFont("normal_font");
            }

            _labelWidthWithPortrait = _label.RectSize.x;

            _portrait.Texture = null;
            // Has to be CallDeferred, because the width doesn't get updated until
            // the HBox has a chance to re-run its layout, which doesn't happen until one frame
            // after the portrait gets hidden.
            CallDeferred(nameof(SetLabelWidthNoPortrait));
            CallDeferred(nameof(HidePortrait));

            _label.BbcodeEnabled = true;
            _segmenter = new DialogueSegmenter(Font);
        }

        private void SetLabelWidthNoPortrait()
        {
            _labelWidthNoPortrait = _label.RectSize.x;
        }

        // API-like methods and properties

        /// <summary>
        /// Returns the (BBCode) text the label is currently displaying.
        /// </summary>
        public string Text => _label.BbcodeText;

        /// <summary>
        /// The key used to resume from a break.
        /// </summary>
        public KeyList BreakKey { get; set; } = KeyList.Z;

        /// <summary>
        /// Enable or disable turbo mode (i.e. all text gets printed instantly).
        /// </summary>
        /// <param name="enabled"></param>
        public bool Turbo { get; set; } = false;

        /// <summary>
        /// Enqueues text to add to the dialogue box. Supports BBCode.
        /// </summary>
        /// <param name="text">The text to add.</param>
        /// <param name="speed">Number of seconds to take to print each character. Defaults to 0.015. If 0, the text appears instantly.</param>        
        public DialogueBox QueueText(string text, float speed = 0.015f, string tag = "", bool pushFront = false)
        {
            _bufferEmptied = false;

            var payload = new TextPayload
            {
                PayloadString = text,
                Speed = speed,
                Tag = tag
            };
            if (pushFront)
            {
                _buffer.Insert(0, payload);
            }
            else
            {
                _buffer.Add(payload);
            }

            return this;
        }

        /// <summary>
        /// Enqueues a "pause output for <paramref name="length"/> seconds" instruction.
        /// </summary>
        /// <param name="length">Number of seconds to pause.</param>
        public DialogueBox QueueSilence(float length, string tag = "", bool pushFront = false)
        {
            _bufferEmptied = false;

            var payload = new SilencePayload
            {
                Length = length,
                Tag = tag
            };
            if (pushFront)
            {
                _buffer.Insert(0, payload);
            }
            else
            {
                _buffer.Add(payload);
            }

            return this;
        }

        /// <summary>
        /// Enqueues a "stop output until the player hits the "next" key" instruction.
        /// </summary>
        public DialogueBox QueueBreak(string tag = "", bool pushFront = false)
        {
            _bufferEmptied = false;

            var payload = new BreakPayload
            {
                Tag = tag
            };
            if (pushFront)
            {
                _buffer.Insert(0, payload);
            }
            else
            {
                _buffer.Add(payload);
            }

            return this;
        }

        /// <summary>
        /// Enqueues a "clear all text" instruction.
        /// </summary>
        public DialogueBox QueueClear(string tag = "", bool pushFront = false)
        {
            _bufferEmptied = false;

            var payload = new ClearPayload
            {
                Tag = tag
            };
            if (pushFront)
            {
                _buffer.Insert(0, payload);
            }
            else
            {
                _buffer.Add(payload);
            }

            return this;
        }

        /// <summary>
        /// Enqueues a portrait change instruction.
        /// </summary>
        /// <param name="portraitPath">The Godot 'res://' path to the portrait.</param>
        public DialogueBox QueuePortrait(string portraitPath, string tag = "", bool pushFront = false)
        {
            _bufferEmptied = false;

            var payload = new PortraitPayload
            {
                PortraitResourcePath = portraitPath,
                Tag = tag,
            };
            if (pushFront)
            {
                _buffer.Insert(0, payload);
            }
            else
            {
                _buffer.Add(payload);
            }

            return this;
        }

        /// <summary>
        /// Deletes <em>ALL</em> text on the label.
        /// </summary>
        public void ClearText()
        {
            _label.BbcodeText = "";
            _label.VisibleCharacters = 0;
            _currentLineIndex = 0;
            UpdateMeasure();
            _dialogueBoxFull = false;
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
            _currState = DialogueBoxState.Outputting;
            return _bufferEndCompletionSource.Task;
        }

        /// <summary>
        /// Clears all pending payloads from the internal buffer.
        /// </summary>
        public void ClearBuffer()
        {
            _isBreak = false;
            _currState = DialogueBoxState.Waiting;
            _buffer.Clear();

            UpdateMeasure();

            Turbo = false;
            EmitSignal(nameof(BufferCleared));
        }

        public void Reset()
        {
            ClearText();
            ClearBuffer();
            HidePortrait();
            _portrait.Texture = null;
        }

        // Internal methods

        public override void _Input(InputEvent evt)
        {
            if (!(evt is InputEventKey keyEvt))
            {
                return;
            }

            if (_currState == DialogueBoxState.Outputting
                && _isBreak
                && keyEvt.Scancode == (uint)BreakKey
                && !keyEvt.Echo)
            {
                BreakExited();
                _buffer.RemoveAt(0);
                _isBreak = false;
            }
        }

        public override void _PhysicsProcess(float delta)
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
                    return;
                }
            }

            if (_printingText || _isBreak || !_silenceTimer.IsStopped())
            {
                // Either printing, waiting for player input, or silent.
                return;
            }
            if (_currState == DialogueBoxState.Outputting)
            {
                if (_buffer.Count == 0)
                {
                    _currState = DialogueBoxState.Waiting;
                    BufferEmptied();
                    return;
                }

                IDialoguePayload payload = _buffer[0];

                switch (payload)
                {
                    case TextPayload text:
                        ProcessTextPayload(text);
                        break;
                    case BreakPayload brk:
                        ProcessBreakPayload(brk);
                        break;
                    case SilencePayload silence:
                        ProcessSilencePayload(silence);
                        break;
                    case ClearPayload clear:
                        ProcessClearPayload(clear);
                        break;
                    case PortraitPayload portrait:
                        ProcessPortraitPayload(portrait);
                        break;
                    default:
                        throw new Exception("Unhandled kind of IDialoguePayload.");
                }
            }
        }

        private void ProcessTextPayload(TextPayload text)
        {
            if (_dialogueBoxFull)
            {
                ClearText();
            }

            if (text.Tag != "")
            {
                EmitSignal(nameof(TagEncountered), text.Tag);
            }

            if (Turbo)
            {
                text.Speed = 0;
            }

            // If speed is 0, display everything all at once
            if (text.Speed == 0)
            {
                AddToLabel(text.PayloadString, 0);
                _label.VisibleCharacters = -1;
                _buffer.RemoveAt(0);
            }
            else
            {
                _label.VisibleCharacters = _label.GetTotalCharacterCount();
                bool payloadAdded = AddToLabel(text.PayloadString, text.Speed);
                if (payloadAdded)
                {
                    _printingText = true;
                    _characterTickTimer.Start(text.Speed); // TODO: Handle speeds faster than per-frame by adding more characters per-frame.
                }
                else
                {
                    // If no part of our payload got printed, remove the payload without waiting for the timer.
                    _buffer.RemoveAt(0);
                }
            }
        }

        private void ProcessBreakPayload(BreakPayload brk)
        {
            if (brk.Tag != "")
            {
                EmitSignal(nameof(TagEncountered), brk.Tag);
            }

            if (Turbo)
            {
                // Ignore the break if we're in turbo.
                _buffer.RemoveAt(0);
            }
            else
            {
                BreakEntered();
                _isBreak = true;
            }
        }

        private void ProcessSilencePayload(SilencePayload silence)
        {
            if (silence.Tag != "")
            {
                EmitSignal(nameof(TagEncountered), silence.Tag);
            }

            if (Turbo)
            {
                // Ignore the silence if we're in turbo.
                _buffer.RemoveAt(0);
            }
            else
            {
                _silenceTimer.Start(silence.Length);
            }
        }

        private void ProcessClearPayload(ClearPayload clear)
        {
            if (clear.Tag != "")
            {
                EmitSignal(nameof(TagEncountered), clear.Tag);
            }

            ClearText();
            _buffer.RemoveAt(0);
        }

        private void ProcessPortraitPayload(PortraitPayload portrait)
        {
            if (portrait.Tag != "")
            {
                EmitSignal(nameof(TagEncountered), portrait.Tag);
            }

            // Changing the portrait forces a text clear, otherwise we'd have to relayout already-drawn text.
            // Screw that noise.
            ClearText();
            if (portrait.PortraitResourcePath == null)
            {
                _portrait.Texture = null;
                HidePortrait();
                _buffer.RemoveAt(0);
                return;
            }

            if (!ResourceLoader.Exists(portrait.PortraitResourcePath, "Texture"))
            {
                _portrait.Texture = null;
                HidePortrait();
                GD.PushWarning($"Failed to load a portrait with the path '{portrait.PortraitResourcePath}'. Hiding portrait instead.");
                _buffer.RemoveAt(0);
                return;
            }

            var portraitTexture = ResourceLoader.Load<Texture>(portrait.PortraitResourcePath);
            _portrait.Texture = portraitTexture;
            ShowPortrait();
            _buffer.RemoveAt(0);
        }

        public void CharacterTickTimeout()
        {
            _label.VisibleCharacters += 1;
            if (_label.PercentVisible >= 1.0)
            {
                // Once we've processed the payload in its entirety, remove it from the queue, and stop ticking.
                _buffer.RemoveAt(0); // TODO: Danger: what if someone has called pushfront since we began processing this payload?
                _characterTickTimer.Stop();
                _printingText = false;
            }
        }

        public void SilenceTimerTimeout()
        {
            _buffer.RemoveAt(0); // TODO: Danger: what if someone has called pushfront since we began processing this payload?
        }

        private bool AddToLabel(string text, float speed)
        {
            List<DialogueSegment> segmentsToShow = new List<DialogueSegment>();
            foreach (var segment in _segmenter.SegmentText(text))
            {
                // This block handles cases where we added a newline on the previous iteration
                // and might need to page break immediately.
                if (_currentLineIndex > _maxLineCount)
                {
                    BreakToNextPage(text, speed, segmentsToShow, segment);
                    break;
                }

                bool segmentIsSoleNewline = segment.Text == "\n";
                bool segmentEndsWithNewline = !segmentIsSoleNewline && segment.Text.EndsWith("\n");
                if (segmentIsSoleNewline)
                {
                    _currentLineIndex++;
                    if (_currentLineIndex > _maxLineCount)
                    {
                        // We can't page break immediately--we need to move to the next segment, first.
                        continue;
                    }
                }

                bool lineAdded = TryIncldeDialogueSegment(segmentsToShow, segment);
                if (lineAdded)
                {
                    if (segmentEndsWithNewline) { _currentLineIndex++; }
                }
                else
                {
                    _currentLineIndex++;
                    if (_currentLineIndex > _maxLineCount)
                    {
                        BreakToNextPage(text, speed, segmentsToShow, segment);
                        break;
                    }
                    else // Couldn't fit on the current line. Wrap to the next.
                    {
                        if (segmentsToShow.Any())
                        {
                            segmentsToShow.Last().Text += "\n";
                        }
                        else
                        {
                            _label.Newline();
                        }

                        lineAdded = TryIncldeDialogueSegment(segmentsToShow, segment);
                        if (!lineAdded)
                        {
                            // If we've still failed, we somehow have a segment that's longer than an entire line. We have nowhere sane to break it. Just blow up.
                            throw new Exception($"Failed to find space for the text segment '{segment.Text}', in the overall dialogue text: '{text}'");
                        }
                    }
                }
            }

            if (!segmentsToShow.Any())
            {
                return false;
            }

            string processedText = string.Join("", segmentsToShow.Select(x => x.Text));
            _label.AppendBbcode(processedText);
            return true;
        }

        private bool TryIncldeDialogueSegment(List<DialogueSegment> toShow, DialogueSegment segment)
        {
            float spaceRemaining = _lineSpaceRemaining[_currentLineIndex];
            if (segment.DisplayWidth > spaceRemaining)
            {
                return false;
            }

            _lineSpaceRemaining[_currentLineIndex] -= segment.DisplayWidth;
            toShow.Add(segment);

            return true;
        }

        private void BreakToNextPage(string text, float speed, List<DialogueSegment> segmentsToShow, DialogueSegment currentSegment)
        {
            _dialogueBoxFull = true;
            string remainderText = text.Substring(currentSegment.StartIndex);

            if (segmentsToShow.Any()) // If we have some text already in the box, make sure we wrap up our tag situation
            {
                // Close up any tags on the last segment to make it into this page.
                var prevSegment = segmentsToShow.Last();
                if (prevSegment.Tags != null)
                {
                    StringBuilder closingTags = new StringBuilder();
                    foreach (BBTag tag in prevSegment.Tags)
                    {
                        closingTags.Append($"[/{tag.TagName}]");
                    }
                    prevSegment.Text += closingTags.ToString();
                }

                // And add opening tags to the segment that DIDN'T make the cut.
                if (currentSegment.Tags != null)
                {
                    StringBuilder openingTags = new StringBuilder();
                    foreach (BBTag tag in currentSegment.Tags)
                    {
                        openingTags.Insert(0, $"[{tag.FullTagText}]");
                    }
                    remainderText = openingTags.ToString() + remainderText;
                }
            }

            // Insert a break, and then whatever segments we couldn't fit just after the current payload.
            _buffer.Insert(1, new TextPayload { PayloadString = remainderText, Speed = speed, Tag = "" });
            _buffer.Insert(1, new BreakPayload());
        }

        private void BufferEmptied()
        {
            _nextArrowAnimator.Play("BlinkArrow");
            _bufferEmptied = true;
            _bufferEndCompletionSource?.SetResult("");
        }

        private void BreakEntered()
        {
            _nextArrowAnimator.Play("BlinkArrow");
        }

        private void BreakExited()
        {
            _nextArrowAnimator.Stop(reset: true);
            _nextArrow.Visible = false;
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
                Reset();
            }
        }

        private void HidePortrait()
        {
            _portraitMargin.Visible = false;
            UpdateMeasure();
        }

        private void ShowPortrait()
        {
            _portraitMargin.Visible = true;
            UpdateMeasure();
        }

        private void UpdateMeasure()
        {
            float fontHeight = Font!.GetHeight();
            float labelWidth = (IsPortraitVisible ? _labelWidthWithPortrait : _labelWidthNoPortrait) ?? _label.RectSize.y;
            _maxLineCount = (uint)Mathf.Floor(_label.RectSize.y / (fontHeight + _label.GetConstant("line_separation"))) - 1;
            _lineSpaceRemaining = new float[_maxLineCount + 1];
            for (int i = 0; i < _lineSpaceRemaining.Length; i++)
            {
                _lineSpaceRemaining[i] = labelWidth;
            }
        }
    }

    public interface IDialoguePayload
    {
        public string Tag { get; set; }
    }

    public class TextPayload : IDialoguePayload
    {
        public string Tag { get; set; } = "";
        public string PayloadString { get; set; } = null!;

        /// <summary>
        /// Text tick speed in seconds. Set to 0 to make text appear instantly.
        /// </summary>
        public float Speed { get; set; }
    }

    public class SilencePayload : IDialoguePayload
    {
        public string Tag { get; set; } = "";

        /// <summary>
        /// Silence duration in seconds.
        /// </summary>
        public float Length { get; set; }
    }

    public class BreakPayload : IDialoguePayload
    {
        public string Tag { get; set; } = "";
    }

    public class ClearPayload : IDialoguePayload
    {
        public string Tag { get; set; } = "";
    }

    public class PortraitPayload : IDialoguePayload
    {
        public string Tag { get; set; } = "";
        public string? PortraitResourcePath { get; set; } // TODO: this is for sure gonna change to something more performant...
    }

    public enum DialogueBoxState
    {
        Waiting = 1,
        Outputting = 2,
    }
}


