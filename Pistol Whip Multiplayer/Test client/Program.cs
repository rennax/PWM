using System;
using SocketIOClient;
using PWM;
using PWM.Network.Messages;
using System.Collections.Generic;

namespace Test_client
{
    class Program
    {
        static SocketIO client;

        static string helpText =
            "Press 1 to select level\n" +
            "Press Esc to exit application\n" +
            "Press F1 to start game\n";

        static async System.Threading.Tasks.Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            ConsoleKeyInfo key;
            #region Client

            System.Diagnostics.Trace.Listeners.Add(new MelonListener());


            client = new SocketIO("http://localhost:3000", new SocketIOOptions
            {
                EIO = 4
            });

            client.OnConnected += (sender, e) => {
                Console.WriteLine("Connected to server");
            };

            client.OnDisconnected += (sender, e) =>
            {
                Console.WriteLine("Disconnected from server");
            };


            client.On("PlayerJoinedLobby", data => {
                Player player = data.GetValue<Player>();
                Console.WriteLine($"Player with name: {player.Name}, joined lobby");
            });

            client.On("PlayerLeftLobby", data => {
                Player player = data.GetValue<Player>();
                Console.WriteLine($"Player with name: {player.Name}, left lobby");
            });

            client.On("OnLevelSelected", data =>
            {
                SelectLevel sl = data.GetValue<SelectLevel>();
                Console.WriteLine($"New level selected, group: {sl.GroupName}, index: {sl.Index}, with difficulty: {sl.Difficulty}");
            });

            client.On("OnGameStart", data => {
                StartGame sg = data.GetValue<StartGame>();
                Console.WriteLine("Game started");

            });

            client.On("OnScoreSync", data => {

                Player player = data.GetValue<Player>(0);
                ScoreSync scoreSync = data.GetValue<ScoreSync>(1);

            });

            await client.ConnectAsync();
            #endregion client

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
                        CreateLobby();
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

        static void CreateLobby()
        {
            Console.WriteLine(client.Connected);
            Lobby lobby = new Lobby
            {
                Id = "test"
            };
            client.EmitAsync("CreateLobby", lobby);
        }



        static void StartGame()
        {
            StartGame startGame = new StartGame
            {
                DelayMS = 2000,
            };
            client.EmitAsync("OnStartGame", startGame);
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
                        GroupName = "Classic",
                        Index = 2,
                        Difficulty = 1
                    };
                    break;
                case ConsoleKey.D2:
                    selectLevel = new SelectLevel
                    {
                        GroupName = "Heartbreaker",
                        Index = 17,
                        Difficulty = 0
                    };
                    break;
                case ConsoleKey.D3:

                default:
                    selectLevel = new SelectLevel
                    {
                        GroupName = "Reloaded",
                        Index = 12,
                        Difficulty = 2
                    };
                    break;
            }

            client.EmitAsync("OnLevelSelected", selectLevel);
        }

        static void WriteHelp()
        {
            Console.Write(helpText);
        }

        class MelonListener : System.Diagnostics.TraceListener
        {
            public override void Write(string message)
            {
                Console.WriteLine(message);
            }

            public override void WriteLine(string message)
            {
                Console.WriteLine(message);
            }
        }
    }
}
