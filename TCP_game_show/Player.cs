using System;
using System.Collections.Generic;
using System.Text;

namespace TCP_Server
{
    public class Player
    {
        public Player(int id)
        {
            Id = id;
            Points = 0;
            WrongAnswers = 0;
        }
        public int Id { get; set; }
        public int Points { get; set; }
        public int WrongAnswers { get; set; }
    }
}
