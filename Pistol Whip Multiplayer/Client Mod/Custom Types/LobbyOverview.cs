using System;
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
        public PlayerEntry playerEntryPrefab;

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
        private Messages.Network.SetLevel currentLevel = new Messages.Network.SetLevel
        {
            SongName = "TheFall_Data",
            GroupName = "Classic",
            Difficulty = 0
        };

        static bool catchStarting = true;

        float time = 0;
        float tickRate = 1f;
        PlayerActionManager actionManager;

        private bool settingModifiers;
        private bool settingMap;

        public bool IsHost { get => (curLobby != null && lobbyManager.Player.Name == curLobby.Host.Name); }

        private Difficulty CurrentDifficulty
        {
            get
            {
                if (songSelectionUIController == null)
                    songSelectionUIController = GameObject.Find("Managers/UI State Controller/PF_CHUI_AnchorPt_SongSelection/PF_SongSelectionCanvas_UIv2").GetComponent<SongSelectionUIController>();
                return songSelectionUIController.SelectedDifficulty();
            }
        }
        public Messages.Lobby CurrentLobby { get => curLobby; set => curLobby = value; }

        void Awake()
        {
            //Configure UI
            UnityEventTrigger leaveOnClickEvent = transform.FindChild("Leave/PF_PWM_Trigger_UnityEvents/PF_UnityEventTrigger_OnClick").GetComponent<UnityEventTrigger>();
            leaveOnClickEvent.Event.AddListener(new Action(LeaveLobby));

            startButton = transform.FindChild("Start/PF_PWM_Trigger_UnityEvents/PF_UnityEventTrigger_OnClick");
            UnityEventTrigger startGameOnClickEvent = startButton.GetComponent<UnityEventTrigger>();
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

            playerEntryPrefab = Entry.assets.playerEntry;
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
            


            global::Messenger.Default.Register<global::Messages.SelectFeatureSetSO>(new Action<global::Messages.SelectFeatureSetSO>(OnSceneSetSelect));
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
            List<string> mods = obj.modifiers.ToArray().Select(m => m.Name).ToList();
            if (IsHost)
            {
                Client.client.EmitAsync("SetModifiers", CurrentLobby.Id, mods);
            }
            else
            {
                //Since we are only setting modifiers one by one, we have to wait for all modifiers to be set
                //befroe we attempt to correct otherwise we go into infinite loop
                if (settingModifiers == true)
                    return;
                //We are not host
                //To make sure that 
                var firstNotSecond = mods.Except(CurrentLobby.Modifiers).ToList();
                var secondNotFirst = CurrentLobby.Modifiers.Except(mods).ToList();
                
                bool isSame = !firstNotSecond.Any() && !secondNotFirst.Any();
                if (!isSame)
                {
                    lobbyManager.SetModifiers(CurrentLobby.Modifiers);
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

        private void OnSceneSetSelect(global::Messages.SelectFeatureSetSO msg)
        {
            if (!this.gameObject.activeSelf)
                return;
            //MelonLogger.Msg($"{msg.set.CanonicalUIName} : {msg.set.name}");

            //If we are host transmit set the new current group
            if (IsHost)
            {
                currentLevel.GroupName = msg.set.setLabelUIText;
            }
            else 
            {
                if (settingMap)
                    return;

                
                //If non-host selects a map, we want to return him to the lobby map if the map is not the current lobby map
                if (msg.set.setLabelUIText != currentLevel.GroupName)
                {
                    SetLevel(currentLevel);
                }
            }
        }

        public void OnLevelSelect(global::Messages.LevelSelectEvent msg)
        {
            if (!this.gameObject.activeSelf)
                return;

            MelonLogger.Msg($"{msg.level.name} : {msg.level.data.songName} : {msg.level.data.name}");
            if (IsHost)
            {
                MelonLogger.Msg("Selected");
                currentLevel.SongName = msg.level.data.name;

                currentLevel.Difficulty = (int)CurrentDifficulty;

                Client.client.EmitAsync("SetLevel", CurrentLobby.Id, currentLevel);
            }
            else
            {
                if (!settingMap && msg.level.data.name != currentLevel.SongName)
                {
                    SetLevel(currentLevel);
                }
            }
        }

        public void OnDifficultySelect(Messages.DifficultySelected msg)
        {
            if (!this.gameObject.activeSelf)
                return;

            if (IsHost)
            {
                currentLevel.Difficulty = (int)msg.Difficulty;
                Client.client.EmitAsync("SetLevel", CurrentLobby.Id, currentLevel);
            }
            else
            {
                if (currentLevel.Difficulty != (int)msg.Difficulty)
                {
                    SetDifficulty();
                }
            }
        }

        public void OnLobbyModifiersSet(Messages.NewModifiers msg)
        {
            CurrentLobby.Modifiers = msg.Modifiers;
            settingModifiers = true;
            lobbyManager.SetModifiers(msg.Modifiers);
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

            settingModifiers = true;
            lobbyManager.SetModifiers(curLobby.Modifiers);
            settingModifiers = false;


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

        void AddPlayer(PWM.Messages.Player player)
        {
            string playerName = player.Name;
            if (!players.ContainsKey(playerName))
            {
                PlayerEntry playerEntry = Instantiate(playerEntryPrefab, playersParent);
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

            //Make sure we are reset when we get into lobby again
            TriggerIsNotReady();

            //Make sure game is not started if we leave while game start is in progress.
            CancelInvoke("DelayedGameStart");


            curLobby = null;
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
            Client.client.EmitAsync("PlayerReady", CurrentLobby.Id, lobbyManager.Player, false);
        }


        #region SET_GAME
        //Times might depend on the speed of the client. To test
        public void SetLevel(Messages.Network.SetLevel setLevel)
        {
            currentLevel = setLevel;
            settingMap = true;
            Invoke("SetFeatureMenu", 0.0f);
            Invoke("SetFeatureGroup", 0.4f);
            Invoke("SetSong", 0.7f);
            Invoke("SetDifficulty", 0.9f);
            Invoke("DoneSettingMap", 1f);
        }


        //There ought to be a smarter way to do this
        private void DoneSettingMap()
        {
            settingMap = false;
        }

        private void SetFeatureMenu()
        {
            GameObject.Find("Managers/UI State Controller/PF_CHUI_AnchorPt_SongSelection/PF_SongSelectionCanvas_UIv2/SceneInventory/SceneSet-rev4/PW_VerticalLayoutElementPanel/FeatureMenuContainer/ARCADE").GetComponent<FeatureSelectMenuButton>().SelectFeatureType();
        }

        private void SetFeatureGroup()
        {
            MelonLogger.Msg($"Trying to set group: Managers/UI State Controller/PF_CHUI_AnchorPt_SongSelection/PF_SongSelectionCanvas_UIv2/SceneInventory/SceneSet-rev4/PW_VerticalLayoutElementPanel/LowerContent/GroupMenuContainer/{currentLevel.GroupName}");
            GroupSelectMenuButton groupSelect = GameObject.Find($"Managers/UI State Controller/PF_CHUI_AnchorPt_SongSelection/PF_SongSelectionCanvas_UIv2/SceneInventory/SceneSet-rev4/PW_VerticalLayoutElementPanel/LowerContent/GroupMenuContainer/{currentLevel.GroupName}").GetComponent<GroupSelectMenuButton>();
            groupSelect.SelectFeatureGroup();
        }

        //Set song
        private void SetSong()
        {
            MelonLogger.Msg($"Trying to set song: Managers/UI State Controller/PF_CHUI_AnchorPt_SongSelection/PF_SongSelectionCanvas_UIv2/SceneInventory/SceneSet-rev4/PW_VerticalLayoutElementPanel/LowerContent/Content/FreePlayContent(Clone)/Content/{currentLevel.SongName}");
            GameObject songObj = GameObject.Find($"Managers/UI State Controller/PF_CHUI_AnchorPt_SongSelection/PF_SongSelectionCanvas_UIv2/SceneInventory/SceneSet-rev4/PW_VerticalLayoutElementPanel/LowerContent/Content/FreePlayContent(Clone)/Content/{currentLevel.SongName}");
            if (songObj != null)
            {
                SongPanelUIController song = songObj.GetComponent<SongPanelUIController>();
                song.PosterButtonHandler();
                
            }
            else
                MelonLogger.Msg("Failed to get song");
        }

        public void SetDifficulty()
        {
            MelonLogger.Msg($"Trying to set difficulty: Managers/UI State Controller/PF_CHUI_AnchorPt_SongSelection/PF_SongSelectionCanvas_UIv2/ForwardInfoBoard/DiffPlay/DiffButtons/CncrgDifficultyButton-{(Difficulty)currentLevel.Difficulty}/PF_CHUI_Trigger_UnityEvents");
            GameObject diffObj = GameObject.Find($"Managers/UI State Controller/PF_CHUI_AnchorPt_SongSelection/PF_SongSelectionCanvas_UIv2/ForwardInfoBoard/DiffPlay/DiffButtons/CncrgDifficultyButton-{(Difficulty)currentLevel.Difficulty}/PF_CHUI_Trigger_UnityEvents");
            if (diffObj != null)
            {
                CHUI_TriggerEvents diffTriggerEvent = diffObj.GetComponent<CHUI_TriggerEvents>();
                diffTriggerEvent.OnClick();
            }
            else
                MelonLogger.Msg("Failed to set difficulty");
        }

        //Catch selection of difficulty
        [HarmonyPatch(typeof(CncrgDifficultyButtonController), "DifficultyButtonHandler", new Type[0] { })]
        private static class DifficultyButtonController_Mod
        {
            public static void Postfix(CncrgDifficultyButtonController __instance)
            {
                MelonLogger.Msg($"New difficulty selected: {__instance.Difficulty}");
                Messenger.Default.Send<Messages.DifficultySelected>(new Messages.DifficultySelected { Difficulty = __instance.Difficulty });
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
                    return true;
                }
                return false;
            }
        }
        #endregion
    }
}
