using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using System.IO;
using Newtonsoft.Json;
using MelonLoader;

using SocketIOClient;

using UnityEngine;
using TMPro;


namespace PWM
{

    public class Client
    {
        ServerInfo info;
        public static SocketIO client;
        public async Task InitializeAsync()
        {       

            if (File.Exists(Config.serverInfoPath))
            {
                info = JsonConvert.DeserializeObject<ServerInfo>(File.ReadAllText(Config.serverInfoPath));
                MelonLogger.Msg("Loaded configuration for PW Accuracy");
            }
            else
            {
                info = new ServerInfo();
                MelonLogger.Warning($"No configuration file exists at {Config.serverInfoPath}. Default configuration is loaded");
            }
            MelonLogger.Msg($"{info.Url}:{info.Port}");

            //System.Diagnostics.Trace.Listeners.Add(new MelonListener());         
            client = new SocketIO($"{info.Url}:{info.Port}", new SocketIOOptions
            {
                EIO = 4,
            });            


            client.OnConnected += (sender, e) => 
            {
                MelonLogger.Msg("Connected to server");
                Action OnConnected = () =>
                {
                    MelonLogger.Msg("Connected to server");
                };

                UnityTaskScheduler.Factory.StartNew(OnConnected);
            };

            client.OnDisconnected += (sender, e) =>
            {
                MelonLogger.Msg("Disconnected to server");
                Action OnDisconnected = () =>
                {
                    MelonLogger.Msg("Disconnected from server");
                    Messenger.Default.Send(new Messages.Disconnected());
                };

                UnityTaskScheduler.Factory.StartNew(OnDisconnected);
            };

            client.OnError += (sender, e) =>
            {
                MelonLogger.Msg("Error");
                Action<object> OnError = (object obj) =>
                {
                    MelonLogger.Msg($"Error {(String)obj}");
                };

                UnityTaskScheduler.Factory.StartNew(OnError, e);
            };

            //Called on all players currently in a lobby when a new player joins the lobby
            client.On("PlayerJoinedLobby", data => {
                PWM.Messages.Player msg = data.GetValue<PWM.Messages.Player>();

                Action<object> PlayerJoinedLobby = (object _player) =>
                {
                    Messenger.Default.Send(new PWM.Messages.PlayerJoined {
                        Player = (PWM.Messages.Player)_player
                    });
                };

                UnityTaskScheduler.Factory.StartNew(PlayerJoinedLobby, msg);
            });

            //Called on player itself when succesfully joining a lobby
            client.On("JoinedLobby", data => {
                PWM.Messages.Lobby msg = data.GetValue<PWM.Messages.Lobby>();

                Action<object> JoinedLobby = (object _lobby) =>
                {
                    Messenger.Default.Send(new PWM.Messages.JoinedLobby
                    {
                        Lobby = (PWM.Messages.Lobby)_lobby
                    });
                };

                UnityTaskScheduler.Factory.StartNew(JoinedLobby, msg);
            });

            //Remove player when it leaves
            client.On("PlayerLeftLobby", data => {
                PWM.Messages.Player msg = data.GetValue<PWM.Messages.Player>();

                Action<object> PlayerLeftLobby = (object _player) =>
                {
                    Messenger.Default.Send(new PWM.Messages.PlayerLeft
                    {
                        Player = (PWM.Messages.Player)_player
                    });
                };

                UnityTaskScheduler.Factory.StartNew(PlayerLeftLobby, msg);
            });

            //Handle lobby closure
            client.On("LobbyClosed", data =>
            {
                PWM.Messages.Lobby msg = data.GetValue<PWM.Messages.Lobby>();

                Action<object> LobbyClosed = (object _lobby) =>
                {
                    Messenger.Default.Send(new PWM.Messages.ClosedLobby
                    {
                        Lobby = (PWM.Messages.Lobby)_lobby
                    });
                };

                UnityTaskScheduler.Factory.StartNew(LobbyClosed, msg);
            });

            //Get a list of current active lobbies
            client.On("GetLobbyList", data => 
            {
                List<PWM.Messages.Lobby> msg = data.GetValue<List<PWM.Messages.Lobby>>();
                MelonLogger.Msg($"GetLobbyList returned {msg.Count} lobbies");

                Action<object> GetLobbyList = (object _lobbies) =>
                {
                    Messenger.Default.Send<PWM.Messages.LobbyList>(new PWM.Messages.LobbyList { Lobbies = (List<PWM.Messages.Lobby>)_lobbies });
                };

                UnityTaskScheduler.Factory.StartNew(GetLobbyList, msg);
            });


            client.On("LevelSelected", data =>
            {
                PWM.Messages.Network.SetLevel msg = data.GetValue<PWM.Messages.Network.SetLevel>();
                MelonLogger.Msg($"Got Level Selected in group: {msg.GroupName}, for song: {msg.SongName} and with difficulty {msg.Difficulty}");

                //Create task we can execute in context of unity main thread
                Action<object> SetLevel = (object _setLevel) =>
                {
                    Messages.Network.SetLevel setLevel = (PWM.Messages.Network.SetLevel)_setLevel;
                    Messenger.Default.Send(setLevel);

                };

                UnityTaskScheduler.Factory.StartNew(SetLevel, msg);
            });

            client.On("StartGame", data => {

                PWM.Messages.StartGame msg = data.GetValue<PWM.Messages.StartGame>();
                Action<object> startGameAction = (object startGame) =>
                {
                    PWM.Messages.StartGame sg = (PWM.Messages.StartGame)startGame;
                    Messenger.Default.Send(sg);
                };

                UnityTaskScheduler.Factory.StartNew(startGameAction, msg);
            });

            client.On("OnScoreSync", data => {
                //PWM.Messages.Player player = data.GetValue<PWM.Messages.Player>(0);
                //PWM.Messages.ScoreSync scoreSync = data.GetValue<PWM.Messages.ScoreSync>(1);

                PWM.Messages.UpdateScore score = data.GetValue<Messages.UpdateScore>(0);

                //broadcast scoresync message to messenger and let recipients handle
                Action<object> updateScoreAction = (object _updateScore) =>
                {
                    PWM.Messages.UpdateScore updateScore = (PWM.Messages.UpdateScore)_updateScore;
                    Messenger.Default.Send(updateScore);
                };

                UnityTaskScheduler.Factory.StartNew(updateScoreAction, score);
            });

            client.On("CreatedLobby", data => {
                PWM.Messages.Lobby msg = data.GetValue<PWM.Messages.Lobby>();

                Action<object> CreatedLobby = (object _lobby) =>
                {
                    Messenger.Default.Send<PWM.Messages.CreatedLobby>(new Messages.CreatedLobby { Lobby = (Messages.Lobby)_lobby });
                };

                UnityTaskScheduler.Factory.StartNew(CreatedLobby, msg);
            });

            client.On("CreateLobbyError", data => { 
                PWM.Messages.Network.Error error = data.GetValue<PWM.Messages.Network.Error>();
                MelonLogger.Msg($"Error Location:{error.Call}, Reason: {error.Reason}");
            });

            client.On("SetModifiers", data =>
            {
                MelonLogger.Msg("Setting new modifiers");
                List<string> msg = data.GetValue<List<string>>(0);

                Action<object> SetModifiers = (object _modifiers) =>
                {
                    //MelonLogger.Msg("Disconnected from server");
                    Messages.NewModifiers newModifiers = new Messages.NewModifiers
                    {
                        Modifiers = (List<string>)_modifiers
                    };
                    Messenger.Default.Send(newModifiers);
                };

                UnityTaskScheduler.Factory.StartNew(SetModifiers, msg);
            });

            await client.ConnectAsync();
            MelonLogger.Msg($"Client connection state: {client.Connected}");
        }


        class MelonListener : System.Diagnostics.TraceListener
        {
            public override void Write(string message)
            {
                MelonLogger.Msg(message);
            }

            public override void WriteLine(string message)
            {
                MelonLogger.Msg(message);
            }
        }

        class ServerInfo
        {
            public string Url { get; set; }
            public int Port { get; set; }
        }
    }


}
