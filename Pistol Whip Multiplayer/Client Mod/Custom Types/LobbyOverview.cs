﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

using MelonLoader;
using TMPro;
using UnityEngine.UI;
using HarmonyLib;

namespace PWM
{
    class LobbyOverview : MonoBehaviour
    {
        public LobbyOverview(IntPtr ptr) : base(ptr) { }

        public LobbyManager lobbyManager;
        //public PlayerEntry playerEntryPrefab;

        private static Messages.Lobby curLobby;
        private TMP_Text lobbyTitle;

        private Transform readyButton;
        private Transform notReadyButton;
        private Transform startButton;
        private Image readyIconImg;

        private GameStartTimer gameStartTimer;
     
        //Player name is key
        private Dictionary<string, PlayerEntry> players = new Dictionary<string, PlayerEntry>();
        private Transform playersParent;
        
        static SongSelectionUIController songSelectionUIController = null;
        private Messages.Network.SetLevel CurrentLevel { get => CurrentLobby.Level; set => CurrentLobby.Level = value; }

        static bool catchStarting = true;

        float time = 0;
        float tickRate = 1f;
        PlayerActionManager actionManager;

        private bool settingModifiers;
        private bool settingMap;

        public bool IsHost { get => (curLobby != null && lobbyManager.Player.Name == curLobby.Host.Name); }

        private Difficulty CurrentDifficulty
        {
            get{ return GameManager.Instance.currentDifficulty; }
        }

        public Messages.Lobby CurrentLobby { get => curLobby; set => curLobby = value; }

        void Awake()
        {
            //Configure UI
            UnityEventTrigger leaveOnClickEvent = transform.FindChild("Leave/PF_PWM_Trigger_UnityEvents/PF_UnityEventTrigger_OnClick").GetComponent<UnityEventTrigger>();
            leaveOnClickEvent.Event.AddListener(new Action(LeaveLobby));

            startButton = transform.FindChild("Start");
            UnityEventTrigger startGameOnClickEvent = startButton.FindChild("PF_PWM_Trigger_UnityEvents/PF_UnityEventTrigger_OnClick").GetComponent<UnityEventTrigger>();
            startGameOnClickEvent.Event.AddListener(new Action(TriggerStartGame));
            startButton.gameObject.SetActive(false);

            readyButton = transform.FindChild("Ready");
            UnityEventTrigger isReadyOnClickEvent = readyButton.FindChild("PF_PWM_Trigger_UnityEvents/PF_UnityEventTrigger_OnClick").GetComponent<UnityEventTrigger>();
            isReadyOnClickEvent.Event.AddListener(new Action(TriggerIsReady));
            readyButton.gameObject.SetActive(false);

            notReadyButton = transform.FindChild("Not_Ready");
            UnityEventTrigger isNotReadyOnClickEvent = notReadyButton.FindChild("PF_PWM_Trigger_UnityEvents/PF_UnityEventTrigger_OnClick").GetComponent<UnityEventTrigger>();
            isNotReadyOnClickEvent.Event.AddListener(new Action(TriggerIsNotReady));
            notReadyButton.gameObject.SetActive(false);

            readyIconImg = transform.FindChild("Ready_Icon/Image").GetComponent<Image>();
            readyIconImg.gameObject.SetActive(false);

            gameStartTimer = transform.FindChild("Timer").GetComponent<GameStartTimer>();
            gameStartTimer.gameObject.SetActive(false);

            //playerEntryPrefab = Entry.assets.playerEntry;
            playersParent = transform.FindChild("Player_List").transform;
            lobbyTitle = transform.FindChild("Lobby_Title").GetComponent<TMP_Text>();

            actionManager = GameObject.Find("Managers/PlayerActionManager").GetComponent<PlayerActionManager>();


            //Setup message listeners
            Messenger.Default.Register(new Action<Messages.PlayerJoined>(OnPlayerJoinedLobby));
            Messenger.Default.Register(new Action<Messages.PlayerLeft>(OnPlayerLeftLobby));
            Messenger.Default.Register(new Action<Messages.CreatedLobby>(OnCreatedLobby));
            Messenger.Default.Register(new Action<Messages.ClosedLobby>(OnLobbyClosed));
            Messenger.Default.Register(new Action<Messages.Network.SetLevel>(SetLevel));
            Messenger.Default.Register(new Action<Messages.DifficultySelected>(OnDifficultySelect));
            Messenger.Default.Register(new Action<Messages.UpdateScore>(OnScoreSync));
            Messenger.Default.Register(new Action<Messages.StartGame>(OnStartGame));
            Messenger.Default.Register(new Action<Messages.NewModifiers>(OnLobbyModifiersSet));
            Messenger.Default.Register(new Action<Messages.PlayerReady>(OnPlayerReady));
            


            //global::Messenger.Default.Register<global::Messages.SelectFeatureSetSO>(new Action<global::Messages.SelectFeatureSetSO>(OnSceneSetSelect));
            global::Messenger.Default.Register<global::Messages.LevelSelectEvent>(new Action<global::Messages.LevelSelectEvent>(OnLevelSelect));
            global::Messenger.Default.Register<global::Messages.GameStartEvent>(new Action<global::Messages.GameStartEvent>(OnGameStart));
            global::Messenger.Default.Register<global::Messages.CompletedGameScoreEvent>(new Action<global::Messages.CompletedGameScoreEvent>(OnGameCompletedScoreEvent));
            global::Messenger.Default.Register<global::Messages.ModifiersSet>(new Action<global::Messages.ModifiersSet>(OnModifiersSet));
        }


        void Update()
        {
            if (actionManager.playing)
            {
                if (time + (1f/tickRate) <= Time.time)
                {
                    time = Time.time;

                    GameData gd = actionManager.gameData;

                    Messages.UpdateScore msg = new Messages.UpdateScore
                    {
                        Player = lobbyManager.Player,
                        Score = new Messages.ScoreSync
                        {
                            Score = gd.score,
                            BeatAccuracy = gd.onBeat,
                            HitAccuracy = gd.accuracy
                        }
                    };

                    Client.client.EmitAsync("ScoreUpdate", CurrentLobby.Id, msg);
                }
            }
        }
        private void OnPlayerReady(Messages.PlayerReady obj)
        {
            PlayerEntry player;
            MelonLogger.Msg($"{obj.Player.Name} is ready? {obj.Player.Ready}");
            if (players.TryGetValue(obj.Player.Name, out player))
            {
                player.IsReady(obj.Player.Ready);
            }
        }

        private void OnModifiersSet(global::Messages.ModifiersSet obj)
        {
            //CurrentLobby == null is to catch the message sent at boot of game
            if (!this.gameObject.activeSelf || CurrentLobby == null)
                return;

#if DEBUG
            MelonLogger.Msg("Following modifiers are enabled:");
            foreach (var item in obj.modifiers)
            {
                MelonLogger.Msg($"{item.safeName} : {item.name} : {item.Name}");
            }
#endif

            ulong bitPacked = GameplayDatabase.GetBitPackedModifiers(obj.modifiers);

            if (IsHost)
            {
                Client.client.EmitAsync("SetModifiers", CurrentLobby.Id, bitPacked);
            }
            else
            {
                if (bitPacked != CurrentLevel.BitPackedModifiers)
                {
                    settingModifiers = true;
                    GameplayDatabase.Current.SetModifiers(bitPacked);
                    settingModifiers = false;
                }
            }

        }

        void OnGameStart(global::Messages.GameStartEvent msg)
        {
            if(curLobby != null)
            {
                time = Time.time;
                TriggerIsNotReady(); // Reset player ready status when game starts
            }
        }

        private void OnGameCompletedScoreEvent(global::Messages.CompletedGameScoreEvent obj)
        {
            if (!this.gameObject.activeSelf)
                return;

            GameData gd = obj.data;

            Messages.UpdateScore msg = new Messages.UpdateScore
            {
                Player = lobbyManager.Player,
                Score = new Messages.ScoreSync
                {
                    Score = gd.score,
                    BeatAccuracy = gd.onBeat,
                    HitAccuracy = gd.accuracy
                }
            };

            Client.client.EmitAsync("ScoreUpdate", CurrentLobby.Id, msg);
        }


        public void OnLevelSelect(global::Messages.LevelSelectEvent msg)
        {
            if (!this.gameObject.activeSelf)
                return;

            MelonLogger.Msg($"{msg.level.name} : {msg.level.data.songName} : {msg.level.data.name}");
            if (IsHost)
            {
                MelonLogger.Msg("Selected");
                CurrentLevel.BaseName = msg.level.data.baseName; //Used by GameManager.SetDestination...
                CurrentLevel.BitPackedModifiers = GameManager.Instance.GetBitPackedModifiers();
                CurrentLevel.PlayIntent = (int)GameManager.Instance.playIntent;
                CurrentLevel.Difficulty = (int)GameManager.GetDifficulty();

                Client.client.EmitAsync("SetLevel", CurrentLobby.Id, CurrentLevel);
            }
            else
            {
                if (!settingMap && msg.level.data.baseName != CurrentLevel.BaseName)
                {
                    SetLevel(CurrentLevel);
                }
            }
        }

        public void OnDifficultySelect(Messages.DifficultySelected msg)
        {
            if (!this.gameObject.activeSelf)
                return;

            if (IsHost)
            {
                CurrentLevel.Difficulty = (int)msg.Difficulty;
                Client.client.EmitAsync("SetLevel", CurrentLobby.Id, CurrentLevel);
            }
            else
            {
                if (CurrentLevel.Difficulty != (int)msg.Difficulty)
                {
                    SetLevel(CurrentLevel);
                }
            }
        }

        public void OnLobbyModifiersSet(Messages.NewModifiers msg)
        {
            MelonLogger.Msg("LobbyOverview: OnLobbyModifiersSet");
            CurrentLobby.Level.BitPackedModifiers = msg.BitPackedModifiers;
            settingModifiers = true;
            SetLevel(CurrentLevel);
            settingModifiers = false;
        }

        public void JoinedLobby(Messages.Lobby lobby)
        {
            curLobby = lobby;
            lobbyTitle.text = lobby.Id;
            foreach (var player in lobby.Players)
            {
                AddPlayer(player);
            }

            //settingModifiers = true;
            //lobbyManager.SetModifiers(curLobby.Modifiers);
            //settingModifiers = false;


            if (curLobby.Level != null)
            {
                SetLevel(curLobby.Level);
            }

            readyButton.gameObject.SetActive(true);

            MelonLogger.Msg("JoinedLobby called");
        }

        private void OnLobbyClosed(Messages.ClosedLobby msg)
        {
            LeaveLobby();

            lobbyManager.ShowLobbyListCanvas();
        }

        void AddPlayer(Messages.Player player)
        {
            string playerName = player.Name;
            if (!players.ContainsKey(playerName))
            {
                PlayerEntry playerEntry = Instantiate(AssetBundleBank.Instance.PlayerEntryPrefab, playersParent);
                playerEntry.Name = playerName;
                playerEntry.IsReady(false);
                players.Add(playerName, playerEntry);
                playerEntry.IsReady(player.Ready);
            }
            else
            {
                MelonLogger.Msg($"Player: {playerName}, is already in the lobby");
            }
        }
 
        void OnCreatedLobby(PWM.Messages.CreatedLobby msg)
        {
            curLobby = msg.Lobby;
            lobbyTitle.text = curLobby.Id;
            foreach (var player in curLobby.Players)
            {
                AddPlayer(player);
            }
            readyButton.gameObject.SetActive(true);
            MelonLogger.Msg("ONCreatedLobby called");
        }

        void OnPlayerJoinedLobby(PWM.Messages.PlayerJoined msg)
        {
            AddPlayer(msg.Player);
        }

        void OnPlayerLeftLobby(PWM.Messages.PlayerLeft msg)
        {
            string playerName = msg.Player.Name;

            PlayerEntry pe;
            if (players.TryGetValue(playerName, out pe))
            {
                players.Remove(playerName);
                Destroy(pe.gameObject);
                MelonLogger.Msg($"Player: {playerName} left the lobby");
            }
            else
            {
                MelonLogger.Msg($"Player: {playerName}, is not in the lobby");
            }
        }

        void OnScoreSync(Messages.UpdateScore msg)
        {
            PlayerEntry player;
            if (players.TryGetValue(msg.Player.Name, out player))
            {
                player.UpdateEntry(msg.Score); 
            }
        }

        //Start game if corresponding network event is triggered
        private void OnStartGame(Messages.StartGame obj)
        {
            MelonLogger.Msg("Start Game");

            gameStartTimer.gameObject.SetActive(true);
            float delay = obj.DelayMS / 1000;
            gameStartTimer.SetTimer(delay);
            CancelInvoke("DelayedGameStart"); //Handle cases where we go from not all ready(30s), to all ready(5s)
            Invoke("DelayedGameStart", delay);
            
        }

        private void DelayedGameStart()
        {
            catchStarting = false;
            PlayButtonManager playBtnManager = GameObject.Find("Managers/UI State Controller/PF_CHUI_AnchorPt_SongSelection/PF_SongSelectionCanvas_UIv2/ForwardInfoBoard/DiffPlay/StartButton").GetComponent<PlayButtonManager>();
            playBtnManager.PlayButtonButtonHander();
        }

        //Leave lobby. If host leaves lobby is closed.
        void LeaveLobby()
        {
            if (IsHost)
            {
                //TODO Handle properly. Only socket creating the lobby can delete it
                Client.client.EmitAsync("DeleteLobby", curLobby.Id);
                MelonLogger.Msg($"You deleted the lobby");
            }
            else
            {
                MelonLogger.Msg($"You left the lobby");
                Client.client.EmitAsync("LeaveLobby", lobbyManager.Player, curLobby.Id);
            }

            var toClean = players.Values.ToList();
            players.Clear();
            foreach (var item in toClean)
            {
                Destroy(item.gameObject);
            }

            curLobby = null;

            //Make sure we are reset when we get into lobby again
            TriggerIsNotReady();

            //Make sure game is not started if we leave while game start is in progress.
            CancelInvoke("DelayedGameStart");


            lobbyManager.ShowLobbyListCanvas();
        }

        //Trigger network event to start game across clients
        void TriggerStartGame()
        {
            if (IsHost)
            {

                Client.client.EmitAsync("StartGame", curLobby.Id);
            }
            else
            {
                Client.client.EmitAsync("PlayerIsReady", curLobby.Id, lobbyManager.Player);
            }

        }

        void TriggerIsReady()
        {
            MelonLogger.Msg("TriggerIsReady called");
            readyButton.gameObject.SetActive(false);
            //Once host is ready, he can only start game
            //TODO reset host ready status when he/she selects new map
            if (IsHost)
                startButton.gameObject.SetActive(true);
            else
                notReadyButton.gameObject.SetActive(true);
            readyIconImg.gameObject.SetActive(true);
            Client.client.EmitAsync("PlayerReady", CurrentLobby.Id, lobbyManager.Player, true);
        }

        void TriggerIsNotReady()
        {
            MelonLogger.Msg("TriggerIsNotReady called");
            readyButton.gameObject.SetActive(true);
            notReadyButton.gameObject.SetActive(false);
            readyIconImg.gameObject.SetActive(false);
            startButton.gameObject.SetActive(false);

            //If we are leaving lobby, the lobby will be null and we do not wish to send this
            if(CurrentLobby != null)
                Client.client.EmitAsync("PlayerReady", CurrentLobby.Id, lobbyManager.Player, false);
        }


        #region SET_GAME

        public void SetLevel(Messages.Network.SetLevel setLevel)
        {
            MelonLogger.Msg($"{setLevel.BaseName}_{CurrentDifficulty}:{setLevel.BitPackedModifiers}");
            CurrentLevel = setLevel;

            settingMap = true;
            settingModifiers = true;
            GameManager.Instance.playIntent = (PlayIntent)setLevel.PlayIntent;
            GameManager.Instance.SetDestinationAndUpdateUIFromDestinationString($"{setLevel.BaseName}_{CurrentDifficulty}:{setLevel.BitPackedModifiers}", true, true);
            GameObject.Find("Managers/UI State Controller/PF_CHUI_AnchorPt_SongSelection/PF_SongSelectionCanvas_UIv2/ForwardInfoBoard/DiffPlay/DiffButtons").GetComponent<DifficultySelector>().InitDifficultyButtons();
            settingMap = false;
            settingModifiers = false;
        }

        //Catch selection of difficulty
        [HarmonyPatch(typeof(CncrgDifficultyButtonController), "DifficultyButtonHandler", new Type[0] { })]
        private static class DifficultyButtonController_Mod
        {
            public static void Postfix(CncrgDifficultyButtonController __instance)
            {
                //MelonLogger.Msg($"New difficulty selected: {__instance.Difficulty}");
                Messenger.Default.Send(new Messages.DifficultySelected { Difficulty = __instance.Difficulty });
            }
        }

        //We want to avoid the user starting manually while in lobby. 
        //To do this, we prefix hook the start button function and
        //only if the game is actually starting, do we pass the call down
        //to original.
        //If we are not currently in a lobby, we should just allow the original to be called,
        //to not disrupt singleplayer gameplay
        [HarmonyPatch(typeof(PlayButtonManager), "PlayButtonButtonHander", new Type[0] { })]
        private static class PlayButtonManager_Mod
        {
            public static bool Prefix(PlayButtonManager __instance)
            {
                if (catchStarting == false || curLobby == null)
                {
                    catchStarting = true;
                    return true;
                }
                return false;
            }
        }
        #endregion
    }
}
