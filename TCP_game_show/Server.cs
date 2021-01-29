
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Transactions;

namespace TCP_Server
{
    class Server
    {
        static void Main()
        {
            Console.Title = "Game Server";
            SetupServer();
            Console.ReadLine(); 
            CloseAllSockets();
        }

        private static void SetupServer()
        {
            Console.WriteLine("Setting up server...");
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, 888));
            serverSocket.Listen(0);
            serverSocket.BeginAccept(AcceptCallback, null);
            Console.WriteLine("Server setup complete");
        }

        private static void CloseAllSockets()
        {
            foreach (Socket socket in clientSockets)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }

            serverSocket.Close();
        }

        private static void AcceptCallback(IAsyncResult ar)
        {
            Socket socket;

            try
            {
                socket = serverSocket.EndAccept(ar);
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            clientSockets.Add(socket);
            socket.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, socket);
            Console.WriteLine("Client connected, waiting for request...");
            serverSocket.BeginAccept(AcceptCallback, null);
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            Socket current = (Socket)ar.AsyncState;
            int received;

            try
            {
                received = current.EndReceive(ar);
            }
            catch (SocketException)
            {
                Console.WriteLine("Client disconnected");
                current.Close();
                clientSockets.Remove(current);
                return;
            }

            byte[] recBuf = new byte[received];
            Array.Copy(buffer, recBuf, received);
            string text = Encoding.ASCII.GetString(recBuf);
            Console.WriteLine("Received Text: " + text);

            var player = Players.SingleOrDefault(x => x.Id == current.Handle.ToInt32());

             if (text.ToLower() == "hello")
             {
                Players.Add(new Player(current.Handle.ToInt32()));
                Message message;
                if (clientSockets[0] == current)
                    message = new Message("Your ID: " + current.Handle +
                                              "\nYou have connected first, start the game by typing 'start'", false);
                else
                    message = new Message("Your ID: " + current.Handle + 
                                              "\nConnected to server", true);

                SendMessage(message, current);
             }
             else if (clientSockets[0] == current && text.ToLower() == "start" && game.IsStarted == false)
             {
                 serverSocket.Close();
                 GameStart();
                 SelectQuestion();
                 game.NumberOfQuestions++;
             }
             else if (game.NumberOfQuestions == 11 && game.IsStarted)
             {
                 game.IsStarted = false;
                 SendToAll(new Message( "Game has ended" +
                                            "\ntype 'results' to see the results" +
                                            "\nyou can exit by typing 'exit'" +
                                            "\nPlayer 1 can start the game again", false));
             }
             else if (text.ToLower() == "results")
             { 
                SendMessage(new Message("Your score: "+ player.Points, false), current);
             }
             else if (game.CurrentQuestion != null && game.IsStarted)
             {
                 
                 if (text.ToLower() == "true")
                     answerId = true;
                 else if (text.ToLower() == "false")
                     answerId = false;
                 else
                 {
                     answerId = null;
                    Console.WriteLine("ERROR");
                    SendMessage(new Message("ERROR", false), current);
                 }
                if (game.CurrentQuestion.Answer == answerId && answerId != null)
                {
                     player.Points++;
                     SendMessage(new Message("You have earned 1 point", true), current);
                     SelectQuestion();
                     game.NumberOfQuestions++;
                     player.WrongAnswers = 0;
                }
                else if (game.CurrentQuestion.Answer != answerId && answerId != null) 
                {
                     player.Points -= 2;
                     SendMessage(new Message("You have lost 2 points", true), current);
                     player.WrongAnswers++;
                     player.IsWaiting = true;
                     if (Players.All(x => x.IsWaiting))
                     {
                         SendToAll(new Message("All players answered incorrectly, generating new question...", true));
                         SelectQuestion();
                     }
                }
             }
             else if (text.ToLower() == "exit") 
             {
                 current.Shutdown(SocketShutdown.Both);
                 current.Close();
                 clientSockets.Remove(current);
                 Console.WriteLine("Client disconnected");
                 return;
             }
             else
             {
                 Console.WriteLine("ERROR");
                 SendMessage(new Message("ERROR", false), current);
             }
             current.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, current);
        }

        private static void SelectQuestion()
        {
            var rnd = new Random();
            var r = rnd.Next(Questions.Count);
            var question = Questions[r];
            foreach (var player in Players)
            {
                player.IsWaiting = false;
            }
            game.CurrentQuestion = question;
            IsBlocked(new Message(question.Text, false));
        }
        private static void GameStart()
        {
            game.IsStarted = true;
            SendToAll(new Message("\nGame has been started" +
                                      "\nType 'true' or 'false' to answer the questions", true));
        }
        private static void SendToAll(Message message)
        {
            foreach (Socket socket in clientSockets)
            {
                SendMessage(message, socket);
            }
        }
        private static void IsBlocked(Message message)
        {
            foreach (Socket socket in clientSockets)
            {
                var player = Players.SingleOrDefault(x => x.Id == socket.Handle.ToInt32());
                if (player.WrongAnswers == 3)
                {
                    player.IsWaiting = true;
                    player.WrongAnswers = 0;
                    SendMessage(new Message("You have answered wrong 3 times in a row, wait a turn", true), socket);
                }
                else SendMessage(message, socket);
            }
        }

        private static void SendMessage(Message message, Socket socket)
        {
            var serializedMessage = JsonSerializer.Serialize(message);
            socket.Send(Encoding.ASCII.GetBytes(serializedMessage));
        }
        private static readonly Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static readonly List<Socket> clientSockets = new List<Socket>();
        private const int BUFFER_SIZE = 2048;
        private static readonly byte[] buffer = new byte[BUFFER_SIZE];
        private static Game game = new Game();
        private static bool? answerId;
        private static List<Player> Players = new List<Player>();
        private static readonly List<Question> Questions = new List<Question>()
        {
            new Question("Marrakesh is the capital of Morocco", false),
           new Question("M&M stands for Mars and Moordale", false),
           new Question("There are five different blood groups", false),
           new Question("Canis lupus is the scientific name for a wolf", true),
           new Question("Australia is wider than the moon", true),
           new Question("A woman has walked on the Moon", false),
           new Question("An emu can fly", true),
           new Question("Darth Vader famously says the line “Luke, I am your father” in The Empire Strikes Back", false ),
           new Question("Nicolas Cage and Michael Jackson both married the same woman", true  ),
           new Question("A meter is further than a yard", true),
           new Question("Alliumphobia is a fear of garlic", true),
           new Question("Michael Jackson had a pet python called ‘Crusher’", true),
           new Question("Virtually all Las Vegas gambling casinos ensure that they have no clocks", true),
           new Question("A heptagon has eight sides", false ),
           new Question("The star sign Capricorn is represented by a goat", true),
           new Question("Fish cannot blink", true),
           new Question("YouTube was founded on Valentine’s Day", true),
           new Question("Chewing gum can boost your concentration", true),
           new Question("A regular internet user has three social media accounts", false),
           new Question("The ostrich has the largest eye in the world", false ),

        };
    }
}