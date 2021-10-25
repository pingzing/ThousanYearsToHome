using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ThousandYearsHome.Controls.DialogueEngine
{
    /// <summary>
    /// Class in charge of parsing Dialogue with game directives stripped from it,
    /// but still containing BBCode.
    /// </summary>
    public class DialogueParser
    {
        private static Regex _bbCodeRegex = new Regex(@"\[*.\b[^][]*\]", RegexOptions.Compiled);
        private static Regex _bbCodeOpen = new Regex(@"\[([^\/].*?)\]", RegexOptions.Compiled);
        private static Regex _bbCodeClose = new Regex(@"\[\/(.*?)\b\]", RegexOptions.Compiled);

        private readonly Font _dialogueBoxFont;
        private Stack<BBTag> _tagStack = new Stack<BBTag>();

        public DialogueParser(Font dialogueBoxFont)
        {
            _dialogueBoxFont = dialogueBoxFont;
        }

        private bool _insideOpeningTag = false;
        private bool _insideClosingTag = false;
        private StringBuilder _textBuilder = new StringBuilder();
        private StringBuilder _tagBuilder = new StringBuilder();
        public IEnumerable<DialogueSubstring> GetStringWidths(string text)
        {
            List<DialogueSubstring> stringSegments = new List<DialogueSubstring>();

            int startIndex = -1;
            int endIndex = -1;
            string? previousChar = null;

            // Iterate through the string saving, each chunk of non-tag text into a separate DialogueSubstring.
            var stringEnumerator = StringInfo.GetTextElementEnumerator(text);
            while (stringEnumerator.MoveNext())
            {
                string curr = stringEnumerator.GetTextElement();
                if (curr == "[")
                {
                    endIndex = stringEnumerator.ElementIndex - 1;
                    // This could be either an opening tag or a closing tag, we don't know yet.

                    // Save in-progress string, if any
                    if (_textBuilder.Length > 0)
                    {
                        startIndex = -1;
                        endIndex = -1;
                        stringSegments.Add(new DialogueSubstring
                        {
                            StartIndex = startIndex,
                            EndIndex = endIndex,
                            Text = _textBuilder.ToString(),
                            Tags = _tagStack.Count > 0 ? _tagStack.ToList() : null,
                        });
                        _textBuilder.Clear();
                    }

                    previousChar = curr;
                    continue;
                }

                if (previousChar == "[")
                {
                    // We're currently in *some* kind of tag, let's find out which kind:
                    if (curr == "/")
                    {
                        _insideClosingTag = true;
                    }
                    else
                    {
                        _insideOpeningTag = true;
                    }

                    previousChar = curr;
                    continue;
                }

                if (curr == "]")
                {
                    if (_insideOpeningTag)
                    {
                        string tagText = _tagBuilder.ToString();
                        BBTag tag = new BBTag
                        {
                            FullTagText = tagText,
                            TagName = tagText.Split(' ')[0]
                        };
                        _tagStack.Push(tag);
                        _insideOpeningTag = false;
                    }
                    if (_insideClosingTag)
                    {
                        _tagStack.Pop();
                        _insideClosingTag = false;
                    }

                    previousChar = curr;
                    continue;
                }

                if (_insideClosingTag || _insideClosingTag)
                {
                    _tagBuilder.Append(curr);
                    previousChar = curr;
                    continue;
                }

                if (previousChar == "]")
                {
                    startIndex = stringEnumerator.ElementIndex;
                }

                _textBuilder.Append(curr);
                previousChar = curr;
            }

            foreach (var segment in stringSegments)
            {
                float width = _dialogueBoxFont.GetStringSize(segment.Text).x;
                segment.DisplayWidth = width;
            }

            _tagBuilder.Clear();
            _textBuilder.Clear();
            _tagStack.Clear();
            return stringSegments;
        }
    }

    public class DialogueSubstring
    {
        public float DisplayWidth { get; set; }
        public string Text { get; set; } = null!;
        public int StartIndex { get; set; }
        public int EndIndex { get; set; }

        /// <summary>
        /// The list of tags that apply to this substring, in order from innermost to outermost.
        /// </summary>
        public List<BBTag>? Tags { get; set; }

        public DialogueSubstring() { }

        public DialogueSubstring(int startIndex, int endIndex, string text)
        {
            StartIndex = startIndex;
            EndIndex = endIndex;
            Text = text;
        }
    }

    public class BBTag
    {
        public string TagName { get; set; } = null!;
        public string FullTagText { get; set; } = null!;
    }
}
