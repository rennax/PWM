using System;
using PWM;
using PWM.Messages;
using PWM.Messages.Network;
using System.Collections.Generic;
using SocketIOClient;

namespace Test_client
{
    class Program
    {
        static SocketIO client;

        static string helpText =
            "Press 1 to join lobby with ID 'test'\n" + 
            "Press 2 to select level\n" +
            "Press 3 to Select Difficulty\n" +
            "Press 4 to Set Modifier\n" +
            "Press 5 to Set Ready\n" +
            "Press 9 to Leave Lobby\n" +
            "Press 0 to Create Lobby with ID 'TEST'\n" + 
            "Press Esc to exit application\n" +
            "Press F1 to start game\n";



        static SetLevel setLevel = null;
        public static Player player = new Player { Name = "Test" };
        static Lobby lobby; 

        static async System.Threading.Tasks.Task Main(string[] args)
        {
            ConsoleKeyInfo key;
            #region Client

            System.Diagnostics.Trace.Listeners.Add(new MelonListener());


            client = new SocketIO("http://86.52.115.166:3000", new SocketIOOptions
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

            client.On("JoinedLobby", data => {
                lobby = data.GetValue<Lobby>();
                Console.WriteLine($"successfully joined lobby {lobby.Id} with {lobby.Players.Count} players and host is {lobby.Host.Name}");
            });

            client.On("CreatedLobby", data => {
                lobby = data.GetValue<Lobby>(0);
                Console.WriteLine("Created Lobby");
            });

            client.On("LobbyClosed", data => {
                Lobby lobby = data.GetValue<Lobby>();
                Console.WriteLine($"lobby {lobby.Id} closed");
                LeaveLobby();
            });

            client.On("PlayerLeftLobby", data => {
                Player player = data.GetValue<Player>();
                Console.WriteLine($"Player with name: {player.Name}, left lobby");
            });

            client.On("LevelSelected", data =>
            {
                SetLevel sl = data.GetValue<SetLevel>();
                Console.WriteLine($"New level selected song: {sl.BaseName}, with difficulty: {sl.Difficulty}");
            });

            client.On("GetLobbyList", data =>
            {
                Console.WriteLine("Got lobbies:");
                List<Lobby> lobbies = data.GetValue<List<Lobby>>(0);
                foreach (var item in lobbies)
                {
                    Console.WriteLine($"Lobby: {item.Id} with: {item.Players.Count} players");
                }
            });

            client.On("StartGame", data => {
                StartGame sg = data.GetValue<StartGame>();
                Console.WriteLine("Game started");

            });

            client.On("OnScoreSync", data => {
                UpdateScore score = data.GetValue<UpdateScore>(0);
                Console.WriteLine($"{score.Player.Name}: updated score: {score.Score.Score}, acc: {score.Score.HitAccuracy}, beat: {score.Score.BeatAccuracy}");
            });

            client.On("PlayerReady", data => {
                Player _player = data.GetValue<Player>(0);
                Console.WriteLine($"Player: {_player.Name}, is ready? {_player.Ready}");
            });

            client.ConnectAsync();
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

                    case ConsoleKey.D1:
                        JoinLobby();
                        break;
                    case ConsoleKey.D2:
                        SelectLevel();
                        break;
                    case ConsoleKey.D3:
                        SelectDifficulty();
                        break;
                    case ConsoleKey.D4:
                        SetModifier();
                        break;
                    case ConsoleKey.D5:
                        SetReady();
                        break;
                    case ConsoleKey.D9:
                        LeaveLobby();
                        break;
                    case ConsoleKey.D0:
                        CreateLobby();
                        break;
                    case ConsoleKey.H:
                        WriteHelp();
                        break;
                    case ConsoleKey.F1:
                        StartGame();
                        break;
                    case ConsoleKey.F2:
                        client.EmitAsync("GetLobbyList");
                        Console.WriteLine();
                        break;

                    default:
                        break;
                }

                Console.WriteLine("");
            } while (key.Key != ConsoleKey.Escape);
        }

        private static void SetReady()
        {
            Console.WriteLine("\nPress enter to select default test level");
            Console.WriteLine("Press 1 for Not Ready");
            Console.WriteLine("Press 2 for Ready");
            bool ready = false;
            switch(Console.ReadKey().Key)
            {
                case ConsoleKey.D1:
                    ready = false;
                    break;
                case ConsoleKey.D2:
                    ready = true;
                    break;
                default:
                    break;
            }

            Console.WriteLine();
            client.EmitAsync("PlayerReady", lobby.Id, player, ready);
        }

        static void CreateLobby()
        {
            Console.WriteLine(client.Connected);
            lobby = new Lobby
            {
                Id = "TEST",
                Players = new List<Player>(),
                MaxPlayerCount = 4,
                Level = new SetLevel
                {
                    BaseName = "Lobby",
                    PlayIntent = 0,
                    Difficulty = 0,
                    BitPackedModifiers = 1125899906842624
                }
            };
            client.EmitAsync("CreateLobby", lobby, player);
        }

        static void JoinLobby()
        {
            client.EmitAsync("JoinLobby", player, "TEST");
        }

        static void LeaveLobby()
        {
            client.EmitAsync("LeaveLobby", player, "TEST");
        }

        static void StartGame()
        {
            client.EmitAsync("StartGame", lobby.Id);
        }

        static void SelectLevel()
        {
            Console.WriteLine("\nPress enter to select default test level");
            Console.WriteLine("Press 1 for Classic The Fall diff normal");
            Console.WriteLine("Press 2 for Heartbreaker .. diff easy");
            Console.WriteLine("Press 3 for Reloaded Priestess diff hard");

            switch (Console.ReadKey().Key)
            {
                case ConsoleKey.D1:
                    setLevel = new SetLevel
                    {
                        BaseName = "TheFall",
                        Difficulty = 1,
                        PlayIntent = 0,
                        BitPackedModifiers = 1125899906842624
                    };
                    break;
                case ConsoleKey.D2:
                    setLevel = new SetLevel
                    {
                        BaseName = "Embers",
                        Difficulty = 0,
                        PlayIntent = 0, 
                        BitPackedModifiers = 1125899906842624
                    };
                    break;
                case ConsoleKey.D3:

                default:
                    setLevel = new SetLevel
                    {
                        BaseName = "TheHighPriestess",
                        Difficulty = 2,
                        PlayIntent = 0,
                        BitPackedModifiers = 1125899906842624,
                    };
                    break;
            }

            client.EmitAsync("SetLevel", lobby.Id, setLevel);
        }

        static void SelectDifficulty()
        {
            Console.WriteLine("\nPress enter to select default test level");
            Console.WriteLine("Press 1 for Easy Difficulty");
            Console.WriteLine("Press 2 for Normal Difficulty");
            Console.WriteLine("Press 3 for Hard Difficulty");

            switch (Console.ReadKey().Key)
            {
                case ConsoleKey.D1:
                    setLevel.Difficulty = 0;
                    break;
                case ConsoleKey.D2:
                    setLevel.Difficulty = 1;
                    break;
                case ConsoleKey.D3:
                    setLevel.Difficulty = 2;
                    break;
                default:
                    break;
            }

            Console.WriteLine("");
            client.EmitAsync("SetLevel", lobby.Id, setLevel);
        }

        static void SetModifier()
        {
            Console.WriteLine("\nPress enter to select default test level");
            Console.WriteLine("Press 1 for Pistol + Dual");
            Console.WriteLine("Press 2 for Revolver + Deadeye");
            Console.WriteLine("Press 3 for dual burst bottomless heavies bullet hell");

            switch (Console.ReadKey().Key)
            {
                case ConsoleKey.D1:
                    lobby.Level.BitPackedModifiers = 1125899906842632; //new List<string>() { "Pistol", "Dual Wield" };
                    break;
                case ConsoleKey.D2:
                    lobby.Level.BitPackedModifiers = 2251799813685250; //new List<string>() { "Revolver", "Deadeye" };
                    break;
                case ConsoleKey.D3:
                    lobby.Level.BitPackedModifiers = 9007199254741000; //new List<string>() { "Boomstick", "Dual Wield" };
                    break;
                default:
                    break;
            }


            client.EmitAsync("SetModifiers", lobby.Id, lobby.Level.BitPackedModifiers);
            Console.WriteLine();
        }
        static void WriteHelp()
        {
            Console.WriteLine();
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
    //class Program
    //{
    //    static SocketIOClient client;

    //    static string helpText =
    //        "Press 1 to select level\n" +
    //        "Press Esc to exit application\n" +
    //        "Press F1 to start game\n";

    //    static void Main(string[] args)
    //    {
    //        Console.WriteLine("Hello World!");
    //        ConsoleKeyInfo key;

    //        #region Client
    //        client = new SocketIOClient(new SocketIOClientOption(EngineIOScheme.http, "localhost", 3000, ));

    //        client.On(SocketIOEvent.CONNECTION, () => {
    //            Console.WriteLine("Connected to server");
    //        });

    //        client.On(SocketIOEvent.CONNECTION, () =>
    //        {
    //            Console.WriteLine("Disconnected from server");
    //        });

    //        client.On(SocketIOEvent.ERROR, data =>
    //        {
    //            Console.Write("Error");
    //            foreach (var item in data)
    //            {

    //            Console.WriteLine($"error {item.ToObject<String>()}");
    //            }
    //        });


    //        client.On("PlayerJoinedLobby", data =>
    //        {
    //            Player player = data[0].ToObject<Player>();
    //            Console.WriteLine($"Player with name: {player.Name}, joined lobby");
    //        });

    //        client.On("PlayerLeftLobby", data =>
    //        {
    //            Player player = data[0].ToObject<Player>();
    //            Console.WriteLine($"Player with name: {player.Name}, left lobby");
    //        });

    //        client.On("OnLevelSelected", data =>
    //        {
    //            SelectLevel sl = data[0].ToObject<SelectLevel>();
    //            Console.WriteLine($"New level selected, group: {sl.GroupName}, index: {sl.Index}, with difficulty: {sl.Difficulty}");
    //        });

    //        client.On("OnGameStart", data =>
    //        {
    //            StartGame sg = data[0].ToObject<StartGame>();
    //            Console.WriteLine("Game started");

    //        });

    //        client.On("OnScoreSync", data =>
    //        {

    //            Player player = data[0].ToObject<Player>();
    //            ScoreSync scoreSync = data[1].ToObject<ScoreSync>();

    //        });
    //        try
    //        {
    //            client.Connect();
    //        }
    //        catch (Exception e)
    //        {

    //            Console.WriteLine(e.Message);
    //        }

    //        #endregion client

    //        do
    //        {
    //            key = Console.ReadKey();
    //            switch (key.Key)
    //            {
    //                case ConsoleKey.Clear:
    //                    break;
    //                case ConsoleKey.Enter:

    //                    break;
    //                case ConsoleKey.D0:
    //                    CreateLobby();
    //                    break;
    //                case ConsoleKey.D1:
    //                    SelectLevel();
    //                    break;
    //                case ConsoleKey.D2:
    //                    break;
    //                case ConsoleKey.H:

    //                    WriteHelp();
    //                    break;
    //                case ConsoleKey.F1:
    //                    StartGame();
    //                    break;
    //                default:
    //                    break;
    //            }


    //        } while (key.Key != ConsoleKey.Escape);
    //    }

    //    static void CreateLobby()
    //    {
    //        Console.WriteLine();
    //        Lobby lobby = new Lobby
    //        {
    //            Id = "test"
    //        };
    //        client.Emit("CreateLobby", lobby);
    //    }



    //    static void StartGame()
    //    {
    //        Console.WriteLine();
    //        StartGame startGame = new StartGame
    //        {
    //            DelayMS = 2000,
    //        };
    //        client.Emit("OnStartGame", startGame);
    //    }

    //    static void SelectLevel()
    //    {
    //        Console.WriteLine();
    //        Console.WriteLine("\nPress enter to select default test level");
    //        Console.WriteLine("Press 1 for classic index 2 diff normal");
    //        Console.WriteLine("Press 2 for Heartbreaker index 1 diff easy");
    //        Console.WriteLine("Press 3 for Reloaded index 3 diff hard");



    //        SelectLevel selectLevel = null;

    //        switch (Console.ReadKey().Key)
    //        {
    //            case ConsoleKey.D1:
    //                selectLevel = new SelectLevel
    //                {
    //                    GroupName = "Classic",
    //                    Index = 2,
    //                    Difficulty = 1
    //                };
    //                break;
    //            case ConsoleKey.D2:
    //                selectLevel = new SelectLevel
    //                {
    //                    GroupName = "Heartbreaker",
    //                    Index = 17,
    //                    Difficulty = 0
    //                };
    //                break;
    //            case ConsoleKey.D3:

    //            default:
    //                selectLevel = new SelectLevel
    //                {
    //                    GroupName = "Reloaded",
    //                    Index = 12,
    //                    Difficulty = 2
    //                };
    //                break;
    //        }

    //        client.Emit("OnLevelSelected", selectLevel);
    //    }

    //    static void WriteHelp()
    //    {
    //        Console.Write(helpText);
    //    }
    //}
}
