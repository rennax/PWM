using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using System.IO;
using Newtonsoft.Json;
using MelonLoader;
using SocketIOSharp.Client;
using SocketIOSharp.Common;

using EngineIOSharp.Common.Enum;
using Newtonsoft.Json.Linq;

using UnityEngine;
using TMPro;

using PWM.Network.Messages;


namespace PWM
{

    class Client
    {
        ServerInfo info;
        SocketIOClient client;
        public async void Initialize()
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

            client = new SocketIOClient(
                new SocketIOClientOption(EngineIOScheme.http, info.url, (ushort)info.port)
                );

            client.On(SocketIOEvent.CONNECTION, () =>
            {
                MelonLogger.Msg("Connected to server");
            });

            client.On(SocketIOEvent.DISCONNECT, () =>
            {
                MelonLogger.Msg("Disconnected from server");
            });


            client.On("pong", (Data) => // Argument can be used without type.
            {
                foreach (var token in Data)
                {
                    MelonLogger.Msg($"pong received {token}");
                }
            });

            client.On("select_level", (JToken[] Data) => {
                SelectLevel msg = Data[0].ToObject<SelectLevel>();
                MelonLogger.Msg(msg.ToString());
                
                //SongSelectionUIController.Instance.SelectedDifficulty(msg.difficulty);

                Action<object> setDiffAction = (object level) => {
                    SelectLevel lvl = (SelectLevel)level;
                    SongSelectionUIController.Instance.SelectedDifficulty(lvl.difficulty);
                    MelonLogger.Msg("Pressed difficulty");

                    Messenger.Default.Send(MessengerEvent<Messages.SelectFeatureGroup, string, FeatureType>.Create(lvl.groupName, FeatureType.Freeplay));

                    SongPanelUIController[] panels = GameObject.Find("Managers").GetComponentsInChildren<SongPanelUIController>();

                    AK.Wwise.State selectedSongSwitch = SongSelectionUIController.Instance.levelDB.levelData[msg.index].songSwitch;

                    SongPanelUIController panel = null;

                    foreach (var p in panels)
                    {
                        if (p.songSwitch.ObjectReference.Guid == selectedSongSwitch.ObjectReference.Guid)
                        {
                            panel = p;
                        }
                    }

                    if (panel != null)
                    {
                        panel.OnClick();
                    }

                };

                UnityTaskScheduler.Factory.StartNew(setDiffAction, msg);


            });

            client.On("start_game", (JToken[] Data) => {
                StartGame msg = Data[0].ToObject<StartGame>();
                Action<object> startGameAction = (object startGame) =>
                {
                    StartGame sg = (StartGame)startGame;
                    GameObject playButton = GameObject.Find("Managers/UI State Controller/PF_CHUI_AnchorPt_SongSelection/PF_SongSelectionCanvas_UIv2/PlayButton");
                    if (playButton != null)
                    {
                        PlayButtonManager playBtnManager = playButton.GetComponent<PlayButtonManager>();
                        if (playBtnManager != null)
                        {
                            playBtnManager.Invoke("PlayButtonButtonHander", sg.delayMS/1000f);

                            MelonLogger.Msg($"Starting game in {sg.delayMS} ms");
                        }
                    }
                };

                UnityTaskScheduler.Factory.StartNew(startGameAction, msg);
            });

            client.Connect();
            //client.Emit("ping", info);
            
        }



        class ServerInfo
        {
            public string url;
            public int port;
        }
    }


}
