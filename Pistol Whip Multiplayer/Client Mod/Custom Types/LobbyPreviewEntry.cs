using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace PWM
{
    public class LobbyPreviewEntry : MonoBehaviour
    {
        public LobbyPreviewEntry(IntPtr ptr) : base(ptr) {}

        public LobbyList lobbyList;
        public TMP_Text id;
        public TMP_Text playerText;

        void Awake()
        {
            var onClickEvent = transform.Find("PF_PWM_Trigger_UnityEvents/PF_UnityEventTrigger_OnClick").GetComponent<UnityEventTrigger>();
            onClickEvent.Event.AddListener(new Action(OnClick));

            id = transform.FindChild("LobbyID").GetComponent<TMP_Text>();
            playerText = transform.FindChild("PlayerCount").GetComponent<TMP_Text>();
        }

        public void OnClick()
        {
            lobbyList.JoinLobby(id.text);
            MelonLogger.Msg("LobbyPreviewEntry.OnClick");
        }
    }
}
