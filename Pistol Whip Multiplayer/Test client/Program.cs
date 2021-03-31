using System;
using SocketIOSharp.Client;
using SocketIOSharp.Common;
using EngineIOSharp.Common.Enum;
using Newtonsoft.Json.Linq;
using PWM;
using PWM.Network.Messages;

namespace Test_client
{
    class Program
    {
        static SocketIOClient client;
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            ConsoleKeyInfo key;

            client = new SocketIOClient(
                new SocketIOClientOption(EngineIOScheme.http, "localhost", 9001)
            );

            client.On(SocketIOEvent.CONNECTION, () =>
            {
                Console.WriteLine("Connected to server");
            });

            client.On(SocketIOEvent.DISCONNECT, () =>
            {
                Console.WriteLine("Disconnected from server");
            });

            client.Connect();

            do
            {
                key = Console.ReadKey();
                switch (key.Key)
                {
                    case ConsoleKey.Clear:
                        break;
                    case ConsoleKey.Enter:
                        
                        break;
                    case ConsoleKey.D0:
                        
                        break;
                    case ConsoleKey.D1:
                        SelectLevel();
                        break;
                    case ConsoleKey.D2:
                        break;
                    case ConsoleKey.H:
                        
                        WriteHelp();
                        break;
                    case ConsoleKey.F1:
                        StartGame();
                        break;
                    default:
                        break;
                }


            } while (key.Key != ConsoleKey.Escape);
        }

        static void StartGame()
        {
            StartGame startGame = new StartGame
            {
                delayMS = 2000,
            };
            client.Emit("start_game", startGame);
        }

        static void SelectLevel()
        {
            Console.WriteLine("\nPress enter to select default test level");
            Console.WriteLine("Press 1 for classic index 2 diff normal");
            Console.WriteLine("Press 2 for Heartbreaker index 1 diff easy");
            Console.WriteLine("Press 3 for Reloaded index 3 diff hard");
            


            SelectLevel selectLevel = null;

            switch (Console.ReadKey().Key)
            {
                case ConsoleKey.D1:
                    selectLevel = new SelectLevel
                    {
                        groupName = "Classic",
                        index = 2,
                        difficulty = 1
                    };
                    break;
                case ConsoleKey.D2:
                    selectLevel = new SelectLevel
                    {
                        groupName = "Heartbreaker",
                        index = 17,
                        difficulty = 0
                    };
                    break;
                case ConsoleKey.D3:

                default:
                    selectLevel = new SelectLevel
                    {
                        groupName = "Reloaded",
                        index = 12,
                        difficulty = 2
                    };
                    break;
            }

            client.Emit("select_level", selectLevel);
        }

        static void WriteHelp()
        {
            
            Console.Write(helpText);
        }


        static string helpText =
            "Press 1 to select level\n" + 
            "Press Esc to exit application\n" + 
            "Press F1 to start game\n";
    }
}
