using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThousandYearsHome.Controls.Dialogue
{
    /// <summary>
    /// The core of the dialogue engine that wraps the RichTextLabel, that processes
    /// payloads, provides word-wrapping and automatic paging.
    /// </summary>
    public class DialogueEngine : ReferenceRect
    {
        // Members
        private List<IDialoguePayload> _buffer = new List<IDialoguePayload>();
        private DialogueEngineState _currState = DialogueEngineState.Waiting;
        private bool _isBreak = false;
        private uint _maxLineIndex = 0;
        private bool _dialogueBoxFull = false;
        private bool _printingText = false;

        private int _currentLineIndex = 0;
        private float[] _lineSpaceRemaining = null!;
        private DialogueParser _parser = null!;

        // Exported properties

        /// <summary>
        /// If the text output pauses waiting for the user when reaching the maximum number of lines.
        /// </summary>
        [Export] public bool BreakOnMaxLines = true;

        /// <summary>
        /// The font to use in the backing RichTextLabel.
        /// </summary>
        [Export] public Font? Font = null;

        /// <summary>
        /// Whether or not to print input to console.
        /// </summary>
        [Export] public bool PrintInput = true;

        // Signals

        /// <summary>
        /// Fired when the buffer has run out of things to output.
        /// </summary>
        [Signal] public delegate void BufferEmptied();

        /// <summary>
        /// When the engine's state changes.
        /// </summary>
        /// <param name="state">The new state.</param>
        [Signal] public delegate void StateChanged(DialogueEngineState state);

        /// <summary>
        /// Fired when the engine encounters a Break.
        /// </summary>
        [Signal] public delegate void BreakEntered();

        /// <summary>
        /// Fired when the engine resumes after a break.
        /// </summary>
        [Signal] public delegate void BreakExited();

        /// <summary>
        /// Fired when the buffer encounters a pyaload with a tag.
        /// </summary>
        /// <param name="tag">The tag that was encountered.</param>
        [Signal] public delegate void TagEncountered(string tag);

        /// <summary>
        /// Fired when the buffer is cleared of all text.
        /// </summary>
        [Signal] public delegate void BufferCleared();

        // Nodes
        private RichTextLabel _label = null!;
        private Timer _characterTickTimer = null!;
        private Timer _silenceTimer = null!;

        public override void _Ready()
        {
            SetPhysicsProcess(true);
            SetProcessInput(true);
            _label = GetNode<RichTextLabel>("Label");
            _label.ScrollActive = false;
            _label.ScrollFollowing = false;
            _characterTickTimer = GetNode<Timer>("CharacterTickTimer");
            _silenceTimer = GetNode<Timer>("SilenceTimer");

            if (Font != null)
            {
                _label.AddFontOverride("normal_font", Font);
            }
            else
            {
                Font = _label.GetFont("normal_font");
            }

            float fontHeight = Font.GetHeight();
            _maxLineIndex = (uint)Mathf.Floor(RectSize.y / (fontHeight + _label.GetConstant("line_separation")));
            _lineSpaceRemaining = new float[_maxLineIndex + 1];
            _label.RectSize = RectSize;
            for (int i = 0; i < _lineSpaceRemaining.Length; i++)
            {
                _lineSpaceRemaining[i] = _label.RectSize.x;
            }
            _label.BbcodeEnabled = true;
            _parser = new DialogueParser(Font);
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
        /// <param name="speed">Number of seconds to take to print each character.</param>        
        public void QueueText(string text, float speed = 0f, string tag = "", bool pushFront = false)
        {
            var payload = new DialogueTextPayload
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
        }

        /// <summary>
        /// Enqueues a "pause output for <paramref name="length"/> seconds" instruction.
        /// </summary>
        /// <param name="length">Number of seconds to pause.</param>
        public void QueueSilence(float length, string tag = "", bool pushFront = false)
        {
            var payload = new DialogueSilencePayload
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
        }

        /// <summary>
        /// Enqueues a "stop output until the player hits the "next" key" instruction.
        /// </summary>
        public void QueueBreak(string tag = "", bool pushFront = false)
        {
            var payload = new DialogueBreakPayload
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
        }

        /// <summary>
        /// Enqueues a "clear all text" instruction.
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="pushFront"></param>
        public void QueueClear(string tag = "", bool pushFront = false)
        {
            var payload = new DialogueClearPayload
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
        }

        /// <summary>
        /// Deletes <em>ALL</em> text on the label.
        /// </summary>
        public void ClearText()
        {
            _label.BbcodeText = "";
            _label.VisibleCharacters = 0;
            _currentLineIndex = 0;
            for (int i = 0; i < _lineSpaceRemaining.Length; i++)
            {
                _lineSpaceRemaining[i] = _label.RectSize.x;
            }
            _dialogueBoxFull = false;
        }

        /// <summary>
        /// Clears all pending payloads from the internal buffer.
        /// </summary>
        public void ClearBuffer()
        {
            _isBreak = false;
            SetState(DialogueEngineState.Waiting);
            _buffer.Clear();

            for (int i = 0; i < _lineSpaceRemaining.Length; i++)
            {
                _lineSpaceRemaining[i] = _label.RectSize.x;
            }

            Turbo = false;
            EmitSignal(nameof(BufferCleared));
        }

        public void Reset()
        {
            ClearText();
            ClearBuffer();
        }

        public void SetState(DialogueEngineState newState)
        {
            EmitSignal(nameof(StateChanged), newState);
            _currState = newState;
        }

        // Internal methods

        public override void _Input(InputEvent evt)
        {
            if (!(evt is InputEventKey keyEvt))
            {
                return;
            }

            if (_currState == DialogueEngineState.Outputting
                && _isBreak
                && keyEvt.Scancode == (uint)BreakKey)
            {
                EmitSignal(nameof(BreakExited));
                _buffer.RemoveAt(0);
                _isBreak = false;
            }
        }

        public override void _PhysicsProcess(float delta)
        {
            if (_printingText || _isBreak || !_silenceTimer.IsStopped())
            {
                // Either printing, waiting for player input, or silent.
                return;
            }
            if (_currState == DialogueEngineState.Outputting)
            {
                if (_buffer.Count == 0)
                {
                    _currState = DialogueEngineState.Waiting;
                    EmitSignal(nameof(BufferEmptied));
                    return;
                }

                IDialoguePayload payload = _buffer[0];

                switch (payload)
                {
                    case DialogueTextPayload text:
                        ProcessTextPayload(text);
                        break;
                    case DialogueBreakPayload brk:
                        ProcessBreakPayload(brk);
                        break;
                    case DialogueSilencePayload silence:
                        ProcessSilencePayload(silence);
                        break;
                    case DialogueClearPayload clear:
                        ProcessClearPayload(clear);
                        break;
                    default:
                        throw new Exception("Unhandled kind of IDialoguePayload.");
                }
            }
        }

        private void ProcessTextPayload(DialogueTextPayload text)
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

        private void ProcessBreakPayload(DialogueBreakPayload brk)
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
                EmitSignal(nameof(BreakEntered));
                _isBreak = true;
            }
        }

        private void ProcessSilencePayload(DialogueSilencePayload silence)
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

        private void ProcessClearPayload(DialogueClearPayload clear)
        {
            if (clear.Tag != "")
            {
                EmitSignal(nameof(TagEncountered), clear.Tag);
            }

            ClearText();
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
            foreach (var segment in _parser.SegmentText(text))
            {
                // This block handles cases where we added a newline on the previous iteration
                // and might need to page break immediately.
                if (_currentLineIndex > _maxLineIndex)
                {
                    BreakToNextPage(text, speed, segmentsToShow, segment);
                    break;
                }

                bool segmentIsSoleNewline = segment.Text == "\n";
                bool segmentEndsWithNewline = !segmentIsSoleNewline && segment.Text.EndsWith("\n");
                if (segmentIsSoleNewline)
                {
                    _currentLineIndex++;
                    if (_currentLineIndex > _maxLineIndex)
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
                    if (_currentLineIndex > _maxLineIndex)
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
            _buffer.Insert(1, new DialogueTextPayload { PayloadString = remainderText, Speed = speed, Tag = "" });
            _buffer.Insert(1, new DialogueBreakPayload());
        }
    }

    public interface IDialoguePayload
    {
        public string Tag { get; set; }
    }

    public class DialogueTextPayload : IDialoguePayload
    {
        public string Tag { get; set; } = "";
        public string PayloadString { get; set; } = null!;

        /// <summary>
        /// Text tick speed in seconds. Set to 0 to make text appear instantly.
        /// </summary>
        public float Speed { get; set; }
    }

    public class DialogueSilencePayload : IDialoguePayload
    {
        public string Tag { get; set; } = "";

        /// <summary>
        /// Silence duration in seconds.
        /// </summary>
        public float Length { get; set; }
    }

    public class DialogueBreakPayload : IDialoguePayload
    {
        public string Tag { get; set; } = "";
    }

    public class DialogueInputPayload : IDialoguePayload
    {
        public string Tag { get; set; } = "";
    }

    public class DialogueClearPayload : IDialoguePayload
    {
        public string Tag { get; set; } = "";
    }

    public enum DialogueEngineState
    {
        Waiting = 1,
        Outputting = 2,
    }
}


