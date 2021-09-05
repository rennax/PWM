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
        static Keyboard.VRKeyboardManager keyboardManager;

        public static Assets assets = new Assets();



        public override void OnApplicationStart()
        {
            //Register custom types for PWM
            ClassInjector.RegisterTypeInIl2Cpp<GameStartTimer>();
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




            //global::Messenger.Default.Register<global::Messages.PlayButtonIsEnabledForIntent>(new Action<global::Messages.PlayButtonIsEnabledForIntent>(OnPlayButtonIsEnabled));
        }

        private void OnGameCompletedScoreEvent(global::Messages.CompletedGameScoreEvent obj)
        {
            MelonLogger.Msg($"OnGameCompletedScoreEvent Event, score: {obj.data.score}, complete: {obj.data.songComplete}");
        }

        private void OnPlayButtonIsEnabled(global::Messages.PlayButtonIsEnabledForIntent obj)
        {
            MelonLogger.Msg($"PlayButtonIsEnabledForIntent Event, IsEnabled: {obj.IsEnabled}, Intent: {obj.Intent}");
        }

        //private void OnGameStart(global::Messages.GameStartEvent obj)
        //{
        //    var orig = typeof(GameMap).GetProperty("apiName").GetGetMethod();
        //    var postfix = typeof(Test).GetMethod("Postfix");
        //    var prefix = typeof(Test).GetMethod("Prefix");
        //    HarmonyInstance.Patch(orig, new HarmonyMethod(prefix), new HarmonyMethod(postfix));

        //}

        public override void OnUpdate()
        {
            UnityTaskScheduler scheduler = (UnityTaskScheduler)UnityTaskScheduler.Default;
            scheduler.Start();
        }



        [HarmonyPatch(typeof(CloudheadGames.CHFramework.Platform.CHPlatformManager), "Start", new Type[0] { })]
        public static class CHPlatformManager_InitializeMod
        {
            public static void Postfix(CloudheadGames.CHFramework.Platform.CHPlatformManager __instance)
            {
                LoadAssets();

            }
        }

        
        public static class Test
        {
            public static void Postfix(ref string __result)
            {
                MelonLogger.Msg($"Orig get_Name value: {__result}");
                __result = "Yeet";
            }

            public static void Prefix()
            {

            }
        }
        

        static void LoadAssets()
        {
            MelonLogger.Msg("Instantiating lobby panel");
            Il2CppAssetBundle bundle = Il2CppAssetBundleManager.LoadFromFile("Mods/PWM/pwm");
            MelonLogger.Msg("Loaded bundle");

            //Lobby list prefab
            var lpe = bundle.Load<GameObject>("LobbyPreviewEntry").GetComponent<LobbyPreviewEntry>();




            MelonLogger.Msg("Loaded LobbyPreviewEntry Prefab");
            if (lpe != null) MelonLogger.Msg("Loaded lobby preview entry");
            else MelonLogger.Msg("Failed to load lobby preview entry");
            //We cannot set the lobbylist yet
            assets.lobbyPreviewEntry = lpe;

            assets.playerEntry = bundle.Load<GameObject>("Player_Entry").GetComponent<PlayerEntry>();

            GameObject lobbyPanelPrefab = bundle.Load<GameObject>("pwm_lobby_board_ui"); 
            SetSPShaderForTMP(lobbyPanelPrefab);
            lobbyPanel = UnityEngine.Object.Instantiate(lobbyPanelPrefab);



            bundle = Il2CppAssetBundleManager.LoadFromFile("Mods/PWM/Keyboard");
            GameObject keyboardPrefab = bundle.Load<GameObject>("Keyboard_Canvas");
            keyboard = UnityEngine.Object.Instantiate(keyboardPrefab);
            keyboardManager = keyboard.GetComponent<Keyboard.VRKeyboardManager>();

            //Fix to single pass render
            SetSPShaderForTMP(assets.lobbyPreviewEntry.gameObject);
            SetSPShaderForTMP(assets.playerEntry.gameObject);
            SetSPShaderForTMP(keyboard);

            //keyboard.SetActive(false);


        }

        //Avoid double render because of multi pass rendering being applied
        static void SetSPShaderForTMP(GameObject go)
        {
            var fixArray = go.transform.GetComponentsInChildren<TextMeshProUGUI>();
            MelonLogger.Msg($"fixing {fixArray.Length} tmp items");
            var spShader = Shader.Find("TextMeshPro/Distance Field_SP_Instanced");
            foreach (var item in fixArray)
            {
                item.fontMaterial.shader = spShader;
            }
        }

        public class Assets
        {
            public LobbyPreviewEntry lobbyPreviewEntry;
            public PlayerEntry playerEntry;
        }
    }


}