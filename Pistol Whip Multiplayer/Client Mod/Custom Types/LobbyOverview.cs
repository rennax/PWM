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

        //private static Messages.Lobby curLobby;
        private TMP_Text lobbyTitle;

        private Transform readyButton;
        private Transform notReadyButton;
        private Transform startButton;
        private Image readyIconImg;

        private GameStartTimer gameStartTimer;

        GameObject retryDeathBtn = null;

        //private bool canStartYet = false;
     
        //Player name is key
        private Dictionary<string, PlayerEntry> players = new Dictionary<string, PlayerEntry>();
        private Transform playersParent;
        
        //private Messages.Network.SetLevel CurrentLevel { get => CurrentLobby.Level; set => CurrentLobby.Level = value; }

        //float time = 0;
        //float tickRate = 1f;
        //PlayerActionManager actionManager;

        //public bool IsHost { get => (curLobby != null && lobbyManager.Player.Name == curLobby.Host.Name); }

        //private Difficulty CurrentDifficulty
        //{
        //    get{ return (Difficulty) CurrentLevel.Difficulty; }
        //}

        //private static bool preventUIInteraction = false;
        //private static bool starting = false;
        //private static bool settingModifiers = false;
        //private static bool settingMap = false;
        //private static bool settingDiff = false;

        //public Messages.Lobby CurrentLobby { get => curLobby; set => curLobby = value; }

        void Awake()
        {
            //Configure UI
            UnityEventTrigger leaveOnClickEvent = transform.FindChild("Leave/PF_PWM_Trigger_UnityEvents/PF_UnityEventTrigger_OnClick").GetComponent<UnityEventTrigger>();
            leaveOnClickEvent.Event.AddListener(new Action(lobbyManager.LeaveLobby));

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


            Messenger.Default.Register(new Action<Messages.PlayerReady>(OnPlayerReady));
            Messenger.Default.Register(new Action<Messages.CreatedLobby>(OnCreatedLobby));
            Messenger.Default.Register(new Action<Messages.PlayerJoined>(OnPlayerJoinedLobby));
            Messenger.Default.Register(new Action<Messages.PlayerLeft>(OnPlayerLeftLobby));
            Messenger.Default.Register(new Action<Messages.UpdateScore>(OnScoreSync));
            Messenger.Default.Register(new Action<Messages.StartGame>(OnStartGame));


            global::Messenger.Default.Register<global::Messages.PlayerHitDie>(new Action<global::Messages.PlayerHitDie>(OnPlayerHitDie));
        }

        #region ModMessages
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

        private void OnPlayerReady(Messages.PlayerReady obj)
        {
            PlayerEntry player;
            MelonLogger.Msg($"{obj.Player.Name} is ready? {obj.Player.Ready}");
            if (players.TryGetValue(obj.Player.Name, out player))
            {
                player.IsReady(obj.Player.Ready);
            }
        }

        public void JoinedLobby(Messages.Lobby lobby)
        {
            MelonLogger.Msg("JoinedLobby called");

            lobbyTitle.text = lobby.Id;
            foreach (var player in lobby.Players)
            {
                AddPlayer(player);
            }

            readyButton.gameObject.SetActive(true);
        }

        void OnScoreSync(Messages.UpdateScore msg)
        {
            PlayerEntry player;
            if (players.TryGetValue(msg.Player.Name, out player))
            {
                player.UpdateEntry(msg.Score);
            }
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
        void OnPlayerJoinedLobby(PWM.Messages.PlayerJoined msg)
        {
            AddPlayer(msg.Player);
        }

        void OnCreatedLobby(PWM.Messages.CreatedLobby msg)
        {
            MelonLogger.Msg("LobbyOverview: OnCreatedLobby called");
            lobbyTitle.text = LobbyManager.CurrentLobby.Id;

            foreach (var player in LobbyManager.CurrentLobby.Players)
            {
                AddPlayer(player);
            }
            readyButton.gameObject.SetActive(true);

        }

        private void OnStartGame(Messages.StartGame obj)
        {
            MelonLogger.Msg("LobbyOverview: OnStartGame");

            gameStartTimer.gameObject.SetActive(true);
            float delay = obj.DelayMS / 1000;
            gameStartTimer.SetTimer(delay);

        }

        #endregion

        #region GameMessages
        private void OnPlayerHitDie(global::Messages.PlayerHitDie obj)
        {
            if (!this.gameObject.activeSelf)
                return;

            //Remove retry if player dies. Allow them only to go back to lobby
            if (LobbyManager.CurrentLobby != null)
            {
                retryDeathBtn = GameObject.Find("Managers/UI State Controller/PF_CHUI_AnchorPt_DeathProtips/PF_DeathCanvas_UIv2/PW_VerticalLayoutElementPanel/PW_HorizontalLayoutElementPanel (1)/PF_TextImgLinkButton_UIv2");
                if (retryDeathBtn == null) return;

                retryDeathBtn.SetActive(false);
            }
        }
        #endregion


        //Leave lobby. If host leaves lobby is closed.
        public void LeaveLobby()
        {
            if (retryDeathBtn != null)
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

            //Make sure we are reset when we get into lobby again
            TriggerIsNotReady();
        }

        //Trigger network event to start game across clients
        public void TriggerStartGame()
        {
            if (lobbyManager.IsHost)
            {
                Client.client.EmitAsync("StartGame", LobbyManager.CurrentLobby.Id);
            }
            else
            {
                Client.client.EmitAsync("PlayerIsReady", LobbyManager.CurrentLobby.Id, lobbyManager.Player);
            }

        }

        public void TriggerIsReady()
        {
            MelonLogger.Msg("TriggerIsReady called");
            readyButton.gameObject.SetActive(false);
            //Once host is ready, he can only start game
            //TODO reset host ready status when he/she selects new map
            if (lobbyManager.IsHost)
                startButton.gameObject.SetActive(true);
            else
                notReadyButton.gameObject.SetActive(true);
            readyIconImg.gameObject.SetActive(true);
            Client.client.EmitAsync("PlayerReady", LobbyManager.CurrentLobby.Id, lobbyManager.Player, true);
        }

        public void TriggerIsNotReady()
        {
            MelonLogger.Msg("TriggerIsNotReady called");
            readyButton.gameObject.SetActive(true);
            notReadyButton.gameObject.SetActive(false);
            readyIconImg.gameObject.SetActive(false);
            startButton.gameObject.SetActive(false);

            //If we are leaving lobby, the lobby will be null and we do not wish to send this
            if (LobbyManager.CurrentLobby != null)
                Client.client.EmitAsync("PlayerReady", LobbyManager.CurrentLobby.Id, lobbyManager.Player, false);
        }

    }
}
