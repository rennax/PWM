using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using MelonLoader;
using HarmonyLib;
using System.Collections.Concurrent;
using UnhollowerRuntimeLib;
using System.IO;
using UnityEngine;
using TMPro;

namespace PWM
{
    public class Entry : MelonMod
    {
        public static ConcurrentQueue<Task> workQueue = new ConcurrentQueue<Task>();

        Client client;

        static GameObject lobbyPanel;
        static GameObject keyboard;
        static GameObject openPWMButton;
        static Keyboard.VRKeyboardManager keyboardManager;

        //TODO
        //Fix nulling of prefabs
        //Fix host selecting scene while other players are results panel (auto exist results panel?)
        //Fix player "Name" joined game (cant leave cause naming not matching). Likely caused when joined during a game is in progress
        //Fix crash when players press random modifiers etc when not host
        //Disable replay button function in results scene

        //Future work
        //Add kick player option
        //Only disable visual components when starting game - Currently score does not update while game is running cause LobbyOverview is disabled too
        //Remove start button when in lobby?


        public override void OnApplicationStart()
        {
            //Register custom types for PWM
            ClassInjector.RegisterTypeInIl2Cpp<GameStartTimer>();
            ClassInjector.RegisterTypeInIl2Cpp<OpenPWM>();
            ClassInjector.RegisterTypeInIl2Cpp<ScoreDisplay>();
            ClassInjector.RegisterTypeInIl2Cpp<LobbyManager>();
            ClassInjector.RegisterTypeInIl2Cpp<LobbyPreviewEntry>();
            ClassInjector.RegisterTypeInIl2Cpp<LobbyList>();
            ClassInjector.RegisterTypeInIl2Cpp<CreateLobby>();
            ClassInjector.RegisterTypeInIl2Cpp<PlayerCount>();
            ClassInjector.RegisterTypeInIl2Cpp<LobbyOverview>();
            ClassInjector.RegisterTypeInIl2Cpp<PlayerEntry>();

            //Register custom types for VRKeyboard
            ClassInjector.RegisterTypeInIl2Cpp<Keyboard.VRKeyboardManager>();
            ClassInjector.RegisterTypeInIl2Cpp<Keyboard.VRKey>();
            ClassInjector.RegisterTypeInIl2Cpp<Keyboard.VRKeyDelete>();
            ClassInjector.RegisterTypeInIl2Cpp<Keyboard.VRKeyOK>();

            //Init taskscheduler on unity thread
            UnityTaskScheduler.mainThread = Thread.CurrentThread;
            client = new Client();
            client.InitializeAsync();

            MelonLogger.Msg("Initialized client");

            global::Messenger.Default.Register<global::Messages.GameEndEvent>(new Action(OnGameEndEvent));
            global::Messenger.Default.Register<global::Messages.GameAbortEvent>(new Action(OnGameAbortEvent));
            global::Messenger.Default.Register<global::Messages.CompletedGameScoreEvent>(new Action<global::Messages.CompletedGameScoreEvent>(OnCompletedGameScoreEvent));
            global::Messenger.Default.Register<global::Messages.ReturningToMainMenu>(new Action(OnReturningToMainMenu));
            global::Messenger.Default.Register<global::Messages.GameUnpauseEvent>(new Action<global::Messages.GameUnpauseEvent>(OnGameUnpauseEvent));
            global::Messenger.Default.Register<global::Messages.GamePauseEvent>(new Action<global::Messages.GamePauseEvent>(OnGamePauseEvent));
            global::Messenger.Default.Register<global::Messages.PlayerHitDie>(new Action<global::Messages.PlayerHitDie>(OnPlayerHitDie));
            global::Messenger.Default.Register<global::Messages.SongStopEvent>(new Action(OnSongStopEvent));
            global::Messenger.Default.Register<global::Messages.ResetPlayer>(new Action(OnResetPlayer));     
        }

        private void OnPlayerHitDie(global::Messages.PlayerHitDie obj)
        {
            MelonLogger.Msg("PlayerHitDie Called");
        }

        private void OnResetPlayer()
        {
            MelonLogger.Msg("ResetPlayer Called");
        }

        private void OnSongStopEvent()
        {
            MelonLogger.Msg("SongStopEvent Called");
        }

        private void OnGamePauseEvent(global::Messages.GamePauseEvent obj)
        {
            MelonLogger.Msg("GamePauseEvent Called");
        }

        private void OnGameUnpauseEvent(global::Messages.GameUnpauseEvent obj)
        {
            MelonLogger.Msg("GameUnpauseEvent Called");
        }

        private void OnReturningToMainMenu()
        {
            MelonLogger.Msg("ReturningToMainMenu Called");
        }

        private void OnCompletedGameScoreEvent(global::Messages.CompletedGameScoreEvent obj)
        {
            MelonLogger.Msg("CompletedGameScoreEvent Called");
        }

        private void OnGameAbortEvent()
        {
            MelonLogger.Msg("GameAbortEvent Called");
        }

        private void OnGameEndEvent()
        {
            MelonLogger.Msg("GameEndEvent Called");
        }

        public override void OnUpdate()
        {
            UnityTaskScheduler scheduler = (UnityTaskScheduler)UnityTaskScheduler.Default;
            scheduler.Start(); //Executes all scheduled tasks
        }

        //Seems like a good point to initialize our PWM ui and load asset bundles
        [HarmonyPatch(typeof(CloudheadGames.CHFramework.Platform.CHPlatformManager), "Start", new Type[0] { })]
        public static class CHPlatformManager_InitializeMod
        {
            public static void Postfix(CloudheadGames.CHFramework.Platform.CHPlatformManager __instance)
            {
                LoadAssets();
            }
        }

        static void LoadAssets()
        {
            GameObject bankGO = new GameObject();
            bankGO.name = "AssetBundleBank";
            AssetBundleBank bank = bankGO.AddComponent<AssetBundleBank>();

            //Instantiate static gameobjects
            lobbyPanel = UnityEngine.Object.Instantiate(bank.LobbyPanelPrefab);
            keyboard = UnityEngine.Object.Instantiate(bank.KeyboardPrefab);
            keyboardManager = keyboard.GetComponent<Keyboard.VRKeyboardManager>();

            openPWMButton = UnityEngine.Object.Instantiate(bank.OpenPWMPrefab);
            OpenPWM openPWM = openPWMButton.GetComponent<OpenPWM>();
            openPWM.pwmPanel = lobbyPanel;
            lobbyPanel.SetActive(false);
        }
    }


}