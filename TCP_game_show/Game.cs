using System;
using System.Collections.Generic;
using System.Text;

namespace TCP_Server
{
    public class Game
    {
        public bool IsStarted { get; set; }
        public Question CurrentQuestion { get; set; }
        public int NumberOfQuestions { get; set; }
    }
}
