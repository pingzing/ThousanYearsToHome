using Godot;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ThousandYearsHome.Controls.DialogueEngine
{
    public class DialogueEngine : ReferenceRect
    {
        // Members

        private static Regex _bbCodeRegex = new Regex(@"\[*.\b[^][]*\]", RegexOptions.Compiled);
        private static Regex _bbCodeOpen = new Regex(@"\[([^\/]*?)\b\]", RegexOptions.Compiled);
        private static Regex _bbCodeClose = new Regex(@"\[\/(.*?)\b\]", RegexOptions.Compiled);

        private List<IDialoguePayload> _buffer = new List<IDialoguePayload>();
        private DialogueEngineState _currState = DialogueEngineState.Waiting;
        private bool _isBreak = false;
        private uint _maxLines = 0;
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

        public override void _Ready()
        {
            SetPhysicsProcess(true);
            SetProcessInput(true);
            _label = GetNode<RichTextLabel>("Label");
            _label.ScrollActive = false;
            _label.ScrollFollowing = false;
            _characterTickTimer = GetNode<Timer>("CharacterTickTimer");

            if (Font != null)
            {
                _label.AddFontOverride("normal_font", Font);
            }
            else
            {
                Font = _label.GetFont("normal_font");
            }

            float fontHeight = Font.GetHeight();
            _maxLines = (uint)Mathf.Floor(RectSize.y / (fontHeight + _label.GetConstant("line_separation")));
            _lineSpaceRemaining = new float[_maxLines];
            _label.RectSize = RectSize;
            for (int i = 0; i < _lineSpaceRemaining.Length; i++)
            {
                _lineSpaceRemaining[i] = _label.RectSize.x;
            }
            _label.BbcodeEnabled = true;
            _parser = new DialogueParser(Font);
        }

        // API-like methods

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
        /// Enqueues a "begin taking input from player" instruction.
        /// </summary>
        public void QueueInput(string tag = "", bool pushFront = false)
        {
            var payload = new DialogueInputPayload
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
        }

        public void Reset()
        {
            ClearText();
            ClearBuffer();
        }

        /// <summary>
        /// Returns the (BBCode) text the label is currently displaying.
        /// </summary>
        public string Text => _label.BbcodeText;

        /// <summary>
        /// Enable or disable turbo mode (i.e. all text gets printed instantly).
        /// </summary>
        /// <param name="enabled"></param>
        public bool Turbo { get; set; } = false;

        public void SetState(DialogueEngineState newState)
        {
            EmitSignal(nameof(StateChanged), newState);
            _currState = newState;
        }

        public KeyList BreakKey { get; set; } = KeyList.Z;

        // Internal methods

        public override void _Input(InputEvent evt)
        {
            if (!(evt is InputEventKey keyEvt))
            {
                return;
            }

            // TODO: If we're on a Break, listen for the break key.
            // When hit, we need to clear the label's contents, reset its state,
            // then resume processing payloads.
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
            if (_printingText || _isBreak)
            {
                // Either printing, or waiting for player input. Do nothing.
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
            if (brk.Tag != null)
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

        public void CharacterTickTimeout()
        {
            _label.VisibleCharacters += 1;
            if (_label.PercentVisible >= 1.0)
            {
                // Once we've processed the payload in its entirety, remove it from the queue, and stop ticking.
                _buffer.RemoveAt(0);
                _characterTickTimer.Stop();
                _printingText = false;
            }
        }

        private bool AddToLabel(string text, float speed)
        {
            List<DialogueSegment> segmentsToShow = new List<DialogueSegment>();
            var segmentsEnumerable = _parser.SegmentText(text);
            foreach (var segment in segmentsEnumerable)
            {
                bool lineAdded = TryIncldeDialogueSegment(segmentsToShow, segment);
                if (!lineAdded)
                {
                    _currentLineIndex++;
                    if (_currentLineIndex >= _maxLines)
                    {
                        _dialogueBoxFull = true;
                        // We're overflowing to the next page.
                        string remainderText = text.Substring(segment.StartIndex);

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
                            if (segment.Tags != null)
                            {
                                StringBuilder openingTags = new StringBuilder();
                                foreach (BBTag tag in segment.Tags)
                                {
                                    openingTags.Insert(0, $"[{tag.FullTagText}]");
                                }
                                remainderText = openingTags.ToString() + remainderText;
                            }
                        }

                        // Insert a break, and then whatever segments we couldn't fit just after the current payload.
                        _buffer.Insert(1, new DialogueTextPayload { PayloadString = remainderText, Speed = speed, Tag = "" });
                        _buffer.Insert(1, new DialogueBreakPayload());

                        break;
                    }
                    else // Move on to the next line
                    {
                        // If we've moved to a new line, but not beyond the last line, 
                        // then glue on a newline, to ensure that the RichTextLabel breaks where *we* want it to.
                        if (segmentsToShow.Any())
                        {
                            // If we're in the middle of cutting up a text payload, glue it on to the previous segment.
                            segmentsToShow.Last().Text += "\n";
                        }
                        else
                        {
                            // Otherwise, if this is the first segment, glue it onto whatever already exists.
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
    }

    public interface IDialoguePayload
    {
        public string Tag { get; set; }
    }

    public class DialogueTextPayload : IDialoguePayload
    {
        public string Tag { get; set; } = "";
        public string PayloadString { get; set; } = null!;
        public float Speed { get; set; }
    }

    public class DialogueSilencePayload : IDialoguePayload
    {
        public string Tag { get; set; } = "";
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


