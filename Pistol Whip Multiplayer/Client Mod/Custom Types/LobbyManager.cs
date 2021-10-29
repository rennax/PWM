using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnhollowerRuntimeLib;
using UnityEngine;
using MelonLoader;
using HarmonyLib;
using PWM.Messages.Network;

namespace PWM
{
    public class LobbyManager : MonoBehaviour
    {
        public LobbyManager(IntPtr ptr) : base(ptr) { }

        private Messages.Player player;
        public Messages.Player Player { get => player; }

        public ScoreDisplay scoreDisplay;

        //Submenus
        private LobbyList lobbyList;
        private LobbyOverview lobbyOverview;
        private CreateLobby createLobby;


        //Lobby specific
        private static Messages.Lobby currentLobby;
        private bool canStartYet = false;

        float time = 0;
        float tickRate = 1f;
        PlayerActionManager actionManager;

        private static bool preventUIInteraction = false;
        private static bool starting = false;
        private static bool settingModifiers = false;
        private static bool settingMap = false;
        private static bool settingDiff = false;


        public SetLevel CurrentLevel { get => currentLobby.Level; set => currentLobby.Level = value; }

        public bool IsHost { get => (currentLobby != null && this.Player.Name == currentLobby.Host.Name); }

        private Difficulty CurrentDifficulty
        {
            get { return (Difficulty)CurrentLevel.Difficulty; }
        }

        public static Messages.Lobby CurrentLobby { get => currentLobby; }



        //3d back panel
        public GameObject backPanel;

        void Awake()
        {
            //PWM.Messenger.Default.Register<PWM.Messages.UpdateScore>(OnUpdateScore);

            player = new PWM.Messages.Player
            {
                Name = CloudheadGames.CHFramework.Platform.CHPlatformManager.UserName()
            };

            //Make sure that we have the back panel visible by using a CH shader
            backPanel = this.transform.FindChild("Mesh/Board_Mesh").gameObject;
            backPanel.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Cloudhead/Universal/HighEnd/PW_Props"));

            //Set correct transform
            transform.position = new Vector3(0, 1.4f, -2.5f);
            transform.rotation = Quaternion.Euler(0, 180, 0);


            lobbyList = this.transform.FindChild("Mesh/PWM_Lobby_List_Canvas").GetComponent<LobbyList>();
            lobbyList.lobbyManager = this;

            createLobby = this.transform.FindChild("Mesh/PWM_Lobby_Create_Canvas").GetComponent<CreateLobby>();
            createLobby.lobbyManager = this;

            lobbyOverview = this.transform.FindChild("Mesh/PWM_Lobby_Overview_Canvas").GetComponent<LobbyOverview>();
            lobbyOverview.lobbyManager = this;

            ShowLobbyListCanvas();

            actionManager = GameObject.Find("Managers/PlayerActionManager").GetComponent<PlayerActionManager>();

            //Setup message listeners
            Messenger.Default.Register(new Action<Messages.JoinedLobby>(OnJoinedLobby));
            Messenger.Default.Register(new Action<Messages.Disconnected>(OnDisconnected));

            Messenger.Default.Register(new Action<Messages.CreatedLobby>(OnCreatedLobby));
            Messenger.Default.Register(new Action<Messages.ClosedLobby>(OnLobbyClosed));
            Messenger.Default.Register(new Action<Messages.Network.SetLevel>(SetLevel));
            Messenger.Default.Register(new Action<Messages.StartGame>(OnStartGame));
            Messenger.Default.Register(new Action<Messages.NewModifiers>(OnLobbyModifiersSet));



            //Listen to game Messages
            global::Messenger.Default.Register<global::Messages.LevelSelectEvent>(new Action<global::Messages.LevelSelectEvent>(OnLevelSelect));
            global::Messenger.Default.Register<global::Messages.GameStartEvent>(new Action<global::Messages.GameStartEvent>(OnGameStart));
            global::Messenger.Default.Register<global::Messages.CompletedGameScoreEvent>(new Action<global::Messages.CompletedGameScoreEvent>(OnGameCompletedScoreEvent));
            global::Messenger.Default.Register<global::Messages.ModifiersSet>(new Action<global::Messages.ModifiersSet>(OnModifiersSet));
            global::Messenger.Default.Register<global::Messages.GameEndEvent>(new Action(OnGameEnd));

#if DEBUG
            Invoke("SkipIntro", 4);
            //Invoke("AutoJoin", 5);
#endif
        }
        void SkipIntro()
        {
            GameObject.Find("Staging/Set Dressing/Backlot Gates/StartPanel/Canvas/PWUIButton-Start").GetComponent<IntroPanelButtonController>().TransitionToScenes();
        }

        void Update()
        {
            if (actionManager.playing)
            {
                if (time + (1f / tickRate) <= Time.time)
                {
                    time = Time.time;

                    GameData gd = actionManager.gameData;

                    Messages.UpdateScore msg = new Messages.UpdateScore
                    {
                        Player = this.Player,
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

        #region Global_Messages
        private void OnGameStart(global::Messages.GameStartEvent obj)
        {
            if (currentLobby != null)
            {
                time = Time.time;
                lobbyOverview.TriggerIsNotReady(); // Reset player ready status when game starts
            }
        }
        private void OnGameCompletedScoreEvent(global::Messages.CompletedGameScoreEvent obj)
        {
            if (!this.gameObject.activeSelf || currentLobby == null)
                return;

            GameData gd = obj.data;

            Messages.UpdateScore msg = new Messages.UpdateScore
            {
                Player = this.Player,
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
            if (!this.gameObject.activeSelf || currentLobby == null)
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

        private void OnGameEnd()
        {
            if (!this.gameObject.activeSelf || currentLobby == null)
                return;

            //TODO: Find better solution that this. This is pretty yikes
            Invoke("PreventInteraction", 0.05f);
        }
        #endregion

        #region PWM_Messages
        //If we disconnect, just disable any active UI
        private void OnDisconnected(Messages.Disconnected obj)
        {
            LobbyManager.currentLobby = null;
            ShowLobbyListCanvas();
        }

        void OnJoinedLobby(Messages.JoinedLobby msg)
        {
            ShowLobbyOverviewCanvas();
            currentLobby = msg.Lobby;
            
            lobbyOverview.JoinedLobby(msg.Lobby);
            preventUIInteraction = true;

            if (currentLobby.Level != null)
            {
                SetLevel(currentLobby.Level);
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

        private void OnLobbyClosed(Messages.ClosedLobby msg)
        {
            LeaveLobby();

            this.ShowLobbyListCanvas();
        }

        void OnCreatedLobby(PWM.Messages.CreatedLobby msg)
        {
            MelonLogger.Msg("LobbyManager: OnCreatedLobby called");
            currentLobby = msg.Lobby;
            preventUIInteraction = false;

            Invoke("PreventInteraction", 0.05f);
        }



        private void OnStartGame(Messages.StartGame obj)
        {
            MelonLogger.Msg("LobbyManager: OnStartGame");

            float delay = obj.DelayMS / 1000;
            CancelInvoke("DelayedGameStart"); //Handle cases where we go from not all ready(30s), to all ready(5s)
            Invoke("DelayedGameStart", delay);

        }
        #endregion


        private void PreventInteraction()
        {
            if (CurrentLobby != null)
            {
                MelonLogger.Msg("We are currently in an lobby, so we prevent interaction");
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

        public void LeaveLobby()
        {
            if (IsHost)
            {
                //TODO Handle properly. Only socket creating the lobby can delete it
                Client.client.EmitAsync("DeleteLobby", currentLobby.Id);
                MelonLogger.Msg($"You deleted the lobby");
            }
            else
            {
                MelonLogger.Msg($"You left the lobby");
                Client.client.EmitAsync("LeaveLobby", Player, currentLobby.Id);
            }

            currentLobby = null;

            //Make sure game is not started if we leave while game start is in progress.
            CancelInvoke("DelayedGameStart");

            lobbyOverview.LeaveLobby();

            //Ensure that we are not stuck if we leave after a game has finished
            global::Messenger.Default.Send(global::Messages.PlayButtonIsEnabledForIntent.Create(true, PlayIntent.FREEPLAY));
            global::Messenger.Default.Send(global::Messages.StylesAreLocked.Create(StaticUITerms.StylesUnlocked));
            global::Messenger.Default.Send(global::Messages.StyleResetAndRandomAreLocked.Create(StaticUITerms.StylesResetAndRandomUnlocked));

            ShowLobbyListCanvas();
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

            //Are we in end result screen, we must go back to main menu before proceeding
            if (UIStateController.Instance.endResultsObj.activeSelf)
                UIStateController.Instance.OnReturnToMainMenu();


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


        #region Manage_Submenus
        public void ShowLobbyListCanvas()
        {
            createLobby.gameObject.SetActive(false);
            lobbyList.gameObject.SetActive(true);
            lobbyOverview.gameObject.SetActive(false);
            lobbyList.RefreshLobbyList();
        }

        public void ShowCreateLobbyCanvas()
        {
            createLobby.gameObject.SetActive(true);
            lobbyList.gameObject.SetActive(false);
            lobbyOverview.gameObject.SetActive(false);
        }

        public void ShowLobbyOverviewCanvas()
        {
            createLobby.gameObject.SetActive(false);
            lobbyList.gameObject.SetActive(false);
            lobbyOverview.gameObject.SetActive(true);
        }
        #endregion

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

        //Prevent host from pressing replay in end results menu.
        [HarmonyPatch(typeof(UIStateController), "OnSelectReplaySongUIButton")]
        private static class OnSelectReplaySongUIButton_Hook
        {
            public static bool Prefix(UIStateController __instance)
            {
                if (currentLobby != null)
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
