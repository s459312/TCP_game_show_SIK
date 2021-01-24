using System;
using System.Collections.Generic;
using System.Text;

namespace TCP_Server
{
    public class Question
    {
        public Question(string text, bool answer)
        {
            Text = text;
            Answer = answer;
        }
        public string Text { get; set; }
        public bool Answer { get; set; }
    }
}
