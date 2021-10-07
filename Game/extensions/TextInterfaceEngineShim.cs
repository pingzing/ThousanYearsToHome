using Godot;

namespace ThousandYearsHome.Extensions
{
    public enum TextInterfaceEngineState : int
    {
        StateWaiting = 0,
        StateOutput = 1,
        StateInput = 2,
    }

    public class TextInterfaceEngineShim
    {
        private readonly Node _textInterfaceEngine;

        public TextInterfaceEngineShim(Node textInterfaceEngine)
        {
            _textInterfaceEngine = textInterfaceEngine;
        }

        public void BufferText(string text, float velocity = 0, string tag = "", bool pushFront = false)
        {
            _textInterfaceEngine.Call("buff_text", text, velocity, tag, pushFront);
        }

        public void BufferSilence(float length, string tag = "", bool pushFront = false)
        {
            _textInterfaceEngine.Call("buff_silence", length, tag, pushFront);
        }

        public void BufferBreak(string tag = "", bool pushFront = false)
        {
            _textInterfaceEngine.Call("buff_break", tag, pushFront);
        }

        /// <summary>
        /// Buffer a "clear buffer" command.
        /// </summary>
        public void BufferClear(string tag = "", bool pushFront = false)
        {
            _textInterfaceEngine.Call("buff_clear", tag, pushFront);
        }

        public void ClearText()
        {
            _textInterfaceEngine.Call("clear_text");
        }

        /// <summary>
        /// Immediately clears all text in the buffer.
        /// </summary>
        public void ClearBuffer()
        {
            _textInterfaceEngine.Call("clear_buffer");
        }

        /// <summary>
        /// Immediately clear all buffers and clear the label.
        /// </summary>
        public void Reset()
        {
            _textInterfaceEngine.Call("reset");
        }

        /// <summary>
        /// Immediately adds a newline to the printed text.
        /// </summary>
        public void AddNewline()
        {
            _textInterfaceEngine.Call("add_newline");
        }

        /// <summary>
        /// Gets the currently-visible text.
        /// </summary>
        public void GetText()
        {
            _textInterfaceEngine.Call("get_text");
        }

        public void SetColor(Color color)
        {
            _textInterfaceEngine.Call("set_color", color);
        }

        public void SetState(TextInterfaceEngineState state)
        {
            _textInterfaceEngine.Call("set_state", (int)state);
        }

        public void SetBufferSpeed(int velocity)
        {
            _textInterfaceEngine.Call("set_buff_speed", velocity);
        }

        public void SetBreakKeyScancode(KeyList scancode)
        {
            _textInterfaceEngine.Call("set_break_key_by_scancode", scancode);
        }
    }
}
