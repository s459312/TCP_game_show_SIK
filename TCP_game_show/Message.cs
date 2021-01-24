using System;
using System.Collections.Generic;
using System.Text;

namespace TCP_Server
{
    public class Message
    {
        public Message(string text, bool wait)
        {
            Text = text;
            Wait = wait;
        }

        public Message()
        {
        }

        public string Text { get; set; }
        public bool Wait { get; set; }
    }
}
