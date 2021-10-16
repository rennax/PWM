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
        //public PlayerEntry playerEntryPrefab;

        private static Messages.Lobby curLobby;
        private TMP_Text lobbyTitle;

        private Transform readyButton;
        private Transform notReadyButton;
        private Transform startButton;
        private Image readyIconImg;

        private GameStartTimer gameStartTimer;

        GameObject retryDeathBtn = null;

        private bool canStartYet = false;
     
        //Player name is key
        private Dictionary<string, PlayerEntry> players = new Dictionary<string, PlayerEntry>();
        private Transform playersParent;
        
        private Messages.Network.SetLevel CurrentLevel { get => CurrentLobby.Level; set => CurrentLobby.Level = value; }

        float time = 0;
        float tickRate = 1f;
        PlayerActionManager actionManager;

        public bool IsHost { get => (curLobby != null && lobbyManager.Player.Name == curLobby.Host.Name); }

        private Difficulty CurrentDifficulty
        {
            get{ return (Difficulty) CurrentLevel.Difficulty; }
        }

        private static bool preventUIInteraction = false;
        private static bool starting = false;
        private static bool settingModifiers = false;
        private static bool settingMap = false;
        private static bool settingDiff = false;

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
            Messenger.Default.Register(new Action<Messages.UpdateScore>(OnScoreSync));
            Messenger.Default.Register(new Action<Messages.StartGame>(OnStartGame));
            Messenger.Default.Register(new Action<Messages.NewModifiers>(OnLobbyModifiersSet));
            Messenger.Default.Register(new Action<Messages.PlayerReady>(OnPlayerReady));
            

            //Listen to game Messages
            global::Messenger.Default.Register<global::Messages.LevelSelectEvent>(new Action<global::Messages.LevelSelectEvent>(OnLevelSelect));
            global::Messenger.Default.Register<global::Messages.GameStartEvent>(new Action<global::Messages.GameStartEvent>(OnGameStart));
            global::Messenger.Default.Register<global::Messages.CompletedGameScoreEvent>(new Action<global::Messages.CompletedGameScoreEvent>(OnGameCompletedScoreEvent));
            global::Messenger.Default.Register<global::Messages.ModifiersSet>(new Action<global::Messages.ModifiersSet>(OnModifiersSet));
            global::Messenger.Default.Register<global::Messages.PlayerHitDie>(new Action<global::Messages.PlayerHitDie>(OnPlayerHitDie));
            global::Messenger.Default.Register<global::Messages.GameEndEvent>(new Action(OnGameEnd));

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

        #region ModMessages
        private void OnPlayerReady(Messages.PlayerReady obj)
        {
            PlayerEntry player;
            MelonLogger.Msg($"{obj.Player.Name} is ready? {obj.Player.Ready}");
            if (players.TryGetValue(obj.Player.Name, out player))
            {
                player.IsReady(obj.Player.Ready);
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

            preventUIInteraction = true;

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
            preventUIInteraction = false;

            foreach (var player in curLobby.Players)
            {
                AddPlayer(player);
            }
            readyButton.gameObject.SetActive(true);
            Invoke("PreventInteraction", 0.05f);
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
        private void OnStartGame(Messages.StartGame obj)
        {
            MelonLogger.Msg("Start Game");

            gameStartTimer.gameObject.SetActive(true);
            float delay = obj.DelayMS / 1000;
            gameStartTimer.SetTimer(delay);
            CancelInvoke("DelayedGameStart"); //Handle cases where we go from not all ready(30s), to all ready(5s)
            Invoke("DelayedGameStart", delay);
            
        }
        #endregion

        #region GameMessages
        private void OnGameStart(global::Messages.GameStartEvent obj)
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
        private void OnLevelSelect(global::Messages.LevelSelectEvent obj)
        {
            if (!this.gameObject.activeSelf)
                return;

            if (IsHost)
            {
                MelonLogger.Msg($"Host selected: {obj.level.name}_{obj.level.difficulty}:{GameplayDatabase.CachedCurrent.GetBitpackedActiveModifiers()}");
                CurrentLevel.BaseName = obj.level.data.baseName; //Used by GameManager.SetDestination...
                CurrentLevel.BitPackedModifiers = GameplayDatabase.CachedCurrent.GetBitpackedActiveModifiers(); // GameManager.Instance.GetBitPackedModifiers();
                CurrentLevel.PlayIntent = (int)GameManager.Instance.playIntent;
                CurrentLevel.Difficulty = (int)obj.level.difficulty;

                Client.client.EmitAsync("SetLevel", CurrentLobby.Id, CurrentLevel);
                Invoke("PreventInteraction", 0.05f);
            }
            else
            {
                if (!settingMap && obj.level.data.baseName != CurrentLevel.BaseName)
                {
                    SetLevel(CurrentLevel);
                }
            }
        }
        private void OnModifiersSet(global::Messages.ModifiersSet obj)
        {
            //CurrentLobby == null is to catch the message sent at boot of game
            if (!this.gameObject.activeSelf || CurrentLobby == null)
                return;

#if DEBUG
            string mods = "Following modifiers are enabled:";
            foreach (var item in obj.modifiers)
            {
                mods += $" {item.Name}";
            }
            MelonLogger.Msg(mods);
#endif

            ulong bitPacked = GameplayDatabase.GetBitPackedModifiers(obj.modifiers);

            if (IsHost)
            {
                Client.client.EmitAsync("SetModifiers", CurrentLobby.Id, bitPacked);
            }
            else
            {
                if (settingModifiers == false && bitPacked != CurrentLevel.BitPackedModifiers)
                {
                    SetLevel(CurrentLevel);
                }
            }
        }

        private void OnPlayerHitDie(global::Messages.PlayerHitDie obj)
        {
            if (!this.gameObject.activeSelf)
                return;

            //Remove retry if player dies. Allow them only to go back to lobby
            if (CurrentLobby != null)
            {
                retryDeathBtn = GameObject.Find("Managers/UI State Controller/PF_CHUI_AnchorPt_DeathProtips/PF_DeathCanvas_UIv2/PW_VerticalLayoutElementPanel/PW_HorizontalLayoutElementPanel (1)/PF_TextImgLinkButton_UIv2");
                if (retryDeathBtn == null) return;

                retryDeathBtn.SetActive(false);
            }
        }
        private void OnGameEnd()
        {
            if (!this.gameObject.activeSelf)
                return;

            //TODO: Find better solution that this. This is pretty yikes
            Invoke("PreventInteraction", 0.05f);
        }
        #endregion

        

        private void PreventInteraction()
        {
            if (CurrentLobby != null)
            {
                if (!IsHost)
                {
                    settingDiff = true;
                    global::Messenger.Default.Send(global::Messages.DifficultyOptionsAreLocked.Create(StaticUITerms.DiffUnlocked)); //Trigger reset on diff btns to show current diff
                    settingDiff = false;
                    global::Messenger.Default.Send(global::Messages.StylesAreLocked.Create(StaticUITerms.StylesLockedForCampaigns));
                    global::Messenger.Default.Send(global::Messages.StyleResetAndRandomAreLocked.Create(StaticUITerms.StylesResetAndRandomAreLocked));
                    global::Messenger.Default.Send(global::Messages.PlayButtonIsEnabledForIntent.Create(false, PlayIntent.FREEPLAY)); //We do not want the host to start from game menu
                }
                else
                {
                    //We do not want the host to start from game menu, but we still want him to be able to go back from result screen
                    bool allowPlayButton = UIStateController.Instance.endResultsObj.activeSelf;
                    global::Messenger.Default.Send(global::Messages.PlayButtonIsEnabledForIntent.Create(allowPlayButton, PlayIntent.FREEPLAY));
                }
            }
        }

        //Start game if corresponding network event is triggered
        private void DelayedGameStart()
        {
            starting = true;
            PlayButtonManager playBtnManager = GameObject.Find("Managers/UI State Controller/PF_CHUI_AnchorPt_SongSelection/PF_SongSelectionCanvas_UIv2/ForwardInfoBoard/DiffPlay/StartButton").GetComponent<PlayButtonManager>();
            playBtnManager.PlayButtonButtonHander();
            starting = false;
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

            preventUIInteraction = false;
            if(retryDeathBtn != null)
            {
                retryDeathBtn.SetActive(true);
                retryDeathBtn = null;
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

            //Ensure that we are not stuck if we leave after a game has finished
            global::Messenger.Default.Send(global::Messages.PlayButtonIsEnabledForIntent.Create(true, PlayIntent.FREEPLAY));
            global::Messenger.Default.Send(global::Messages.StylesAreLocked.Create(StaticUITerms.StylesUnlocked));
            global::Messenger.Default.Send(global::Messages.StyleResetAndRandomAreLocked.Create(StaticUITerms.StylesResetAndRandomUnlocked));

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

        public void SetLevel(Messages.Network.SetLevel setLevel)
        {
            CurrentLevel = setLevel;
            MelonLogger.Msg($"Setting level: {setLevel.BaseName}_{(Difficulty)setLevel.Difficulty}:{setLevel.BitPackedModifiers}");
            if (setLevel.BaseName == "Lobby")
                return;

            settingMap = true;
            settingModifiers = true;
            settingDiff = true;




            GameManager.Instance.playIntent = (PlayIntent)CurrentLevel.PlayIntent; //We have to make sure playintent is not NONE when calling ShowDiffPlay
            UIStateController.Instance.destHasBeenSetFromPoster = true;

            //Replication of pressing on song panel
            GameplayDatabase.CachedCurrent.SetModifiers(setLevel.BitPackedModifiers);
            string diffString = (CurrentDifficulty != Difficulty.Normal ? $"_{CurrentDifficulty}" : ""); //Normal is not part of destination so we have to remove that
            string dstString = $"{setLevel.BaseName}{diffString}:{setLevel.BitPackedModifiers}";
            GameManager.Instance.currentDifficulty = (Difficulty)CurrentLevel.Difficulty;
            GameManager.Instance.SetDestinationAndUpdateUIFromDestinationString(dstString, true, true);
            DiffPlayHintsController.Instance.ShowDiffPlay();

            global::Messenger.Default.Send(global::Messages.DifficultyOptionsAreLocked.Create(StaticUITerms.DiffUnlocked)); //We need to unlock diff to actually show current selected diff
            global::Messenger.Default.Send(global::Messages.StylesAreLocked.Create(StaticUITerms.StylesLockedForCampaigns));
            global::Messenger.Default.Send(global::Messages.StyleResetAndRandomAreLocked.Create(StaticUITerms.StylesResetAndRandomAreLocked));
            global::Messenger.Default.Send(global::Messages.ToggleSceneDetailPanel.Create(true));

            global::Messenger.Default.Send(global::Messages.PlayButtonIsEnabledForIntent.Create(false, (PlayIntent)CurrentLevel.PlayIntent));

            //"Reset" difficulty buttons by initializing them again. This will show the current selected difficulty
            //GameObject diffResetObj = GameObject.Find("Managers/UI State Controller/PF_CHUI_AnchorPt_SongSelection/PF_SongSelectionCanvas_UIv2/ForwardInfoBoard/DiffPlay/DiffButtons");
            //if (diffResetObj != null)
            //    diffResetObj.GetComponent<DifficultySelector>().InitDifficultyButtons();

            settingDiff = false;
            settingModifiers = false;
            settingMap = false;

        }


        #region Hooks

        #region DifficultyButton
        //Catch selection of difficulty send message we can deal with
        [HarmonyPatch(typeof(CncrgDifficultyButtonController), "DifficultyButtonHandler")]
        private static class DifficultyButtonController_Mod
        {
            public static bool Prefix(CncrgDifficultyButtonController __instance)
            {
                if (preventUIInteraction)
                    return false;

                //Messenger.Default.Send(new Messages.DifficultySelected { Difficulty = __instance.Difficulty });
                return true;
            }
        }

        [HarmonyPatch(typeof(DifficultySelector), "DifficultySelectButtonHandler")]
        private static class DifficultySelectButtonHandler_Hook
        {
            public static bool Prefix(DifficultySelector __instance)
            {
                if (preventUIInteraction)
                    return false;
                return true;
            }
        }


        [HarmonyPatch(typeof(OutlinedCncrgUIButton), "SetAsSelected")]
        private static class SetAsSelected_Hook
        {
            public static bool Prefix(OutlinedCncrgUIButton __instance)
            {
                if (preventUIInteraction && !settingDiff)
                    return false;
                return true;
            }
        }

        #endregion

        //[HarmonyPatch(typeof(DiffPlayHintsController), "SettingIsAvailableMessage")]
        //private static class SettingIsAvailableMessageHook
        //{
        //    public static void Postfix(DiffPlayHintsController __instance, global::Messages.DifficultyOptionsAreLocked e)
        //    {
        //        string s = global::Messages.DifficultyOptionsAreLocked.s_instance.value;
        //        MelonLogger.Msg($"Got call to SettingIsAvailableMessage with '{e.value}', '{s}' as value");
        //    }
        //}

        //We disable the playbutton. But incase someone manages to enable it, prevent anything from happening
        [HarmonyPatch(typeof(PlayButtonManager), "PlayButtonButtonHander", new Type[0] { })]
        private static class PlayButtonManager_Mod
        {
            public static bool Prefix(PlayButtonManager __instance)
            {
                if (preventUIInteraction && !starting)
                    return false;
                return true;
            }
        }

        //Prevent user pressing Page Up when in lobby
        [HarmonyPatch(typeof(SceneDetailManager), "PaginationRightButtonHandler")]
        private static class PaginationRightButtonHandler_Hook
        {
            public static bool Prefix(SceneDetailManager __instance)
            {
                if (preventUIInteraction)
                    return false;
                return true;

            }
        }

        //Prevent user pressing Page Down when in lobby
        [HarmonyPatch(typeof(SceneDetailManager), "PaginationLeftButtonHandler")]
        private static class PaginationLeftButtonHandler_Hook
        {
            public static bool Prefix(SceneDetailManager __instance)
            {
                if (preventUIInteraction)
                    return false;
                return true;

            }
        }

        //Prevent clicking back
        [HarmonyPatch(typeof(SceneDetailManager), "CloseSceneDetails")]
        private static class CloseSceneDetails_Hook
        {
            public static bool Prefix(SceneDetailManager __instance)
            {
                if (preventUIInteraction)
                    return false;
                return true;

            }
        }

        //Prevent clicking styles in song details panel, as this allow user to select modifiers anyway
        [HarmonyPatch(typeof(SceneDetailManager), "TabSelectButtonHandler")]
        private static class TabSelectButtonHandler_Hook
        {
            public static bool Prefix(SceneDetailManager __instance)
            {
                if (preventUIInteraction)
                    return false;
                return true;
            }
        }

        //prevent triggering any of the button in upper part of menu
        [HarmonyPatch(typeof(FeatureSelectMenuButton), "SelectFeatureType")]
        private static class SelectFeatureType_Hook
        {
            public static bool Prefix(FeatureSelectMenuButton __instance)
            {
                if (preventUIInteraction)
                    return false;
                return true;

            }
        }
        #endregion
    }
}
