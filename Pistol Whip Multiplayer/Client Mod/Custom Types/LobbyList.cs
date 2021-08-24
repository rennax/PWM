using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace PWM
{
    public class LobbyList : MonoBehaviour
    {
        public LobbyList(IntPtr ptr) : base(ptr) { }

        public LobbyManager lobbyManager;

        private List<PWM.Messages.Lobby> lobbies = new List<PWM.Messages.Lobby>();
        public LobbyPreviewEntry lobbyUIPrefab;
        public RectTransform lobbyPreviewParent;
        private List<GameObject> lobbyPreviewEntries = new List<GameObject>();



        void Start()
        {
            //Register messages
            PWM.Messenger.Default.Register<PWM.Messages.LobbyList>(OnLobbyList);
            
            lobbyUIPrefab = Entry.assets.lobbyPreviewEntry;
            lobbyUIPrefab.lobbyList = this;
           


            lobbyPreviewParent = this.transform.FindChild("VerticalLayoutPanel").GetComponent<RectTransform>();

            UnityEventTrigger refreshOnClickEvent = transform.FindChild("Refresh_List_Button/PF_PWM_Trigger_UnityEvents/PF_UnityEventTrigger_OnClick").GetComponent<UnityEventTrigger>();
            refreshOnClickEvent.Event.AddListener(new Action(RefreshLobbyList));

            UnityEventTrigger createLobbyOnClickEvent = transform.FindChild("Create_Lobby/PF_PWM_Trigger_UnityEvents/PF_UnityEventTrigger_OnClick").GetComponent<UnityEventTrigger>();
            createLobbyOnClickEvent.Event.AddListener(new Action(OnShowCreateLobby));

            Image refreshImg = transform.FindChild("Refresh_List_Button/Image").GetComponent<Image>();
            CHUI_ElementFormattedBoxCollider EFBC = transform.FindChild("Refresh_List_Button/PF_PWM_Trigger_UnityEvents").GetComponent<CHUI_ElementFormattedBoxCollider>();
            EFBC.target = refreshImg.rectTransform;
        }


        public void RefreshLobbyList()
        {
            MelonLogger.Msg("Refreshed Lobby List");
            Client.client.EmitAsync("GetLobbyList");
        }

        public void OnShowCreateLobby()
        {
            MelonLogger.Msg("Showing Canvas for creating lobby");
            lobbyManager.ShowCreateLobbyCanvas();

        }

        public void JoinLobby(string id)
        {
            MelonLogger.Msg($"LobbyManager.JoinLobby on id: {id}");
            Client.client.EmitAsync("JoinLobby", lobbyManager.Player, id);
        }

        void OnLobbyList(PWM.Messages.LobbyList msg)
        {
            //Clear all current lobby preview entries
            var clearList = lobbyPreviewEntries.ToList();
            lobbyPreviewEntries.Clear();
            foreach (var entry in clearList)
            {
                Destroy(entry);
            }

            //Create new preview entries based on received message
            lobbies = msg.Lobbies;
            foreach (var lobby in lobbies)
            {
                LobbyPreviewEntry entry = Instantiate(lobbyUIPrefab, lobbyPreviewParent);
                entry.id.text = ($"{lobby.Id}");
                entry.playerText.text = $"{lobby.Players.Count}/{lobby.MaxPlayerCount}";
                entry.lobbyList = this;
                lobbyPreviewEntries.Add(entry.gameObject);
            }
        }
    }
}
