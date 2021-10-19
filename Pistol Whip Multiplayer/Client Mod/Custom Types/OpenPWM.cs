using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MelonLoader;
using UnhollowerRuntimeLib;

namespace PWM
{
    public class OpenPWM : MonoBehaviour
    {
        public OpenPWM(IntPtr ptr) : base(ptr) { }

        private GameObject tooltip;

        public GameObject pwmPanel;

        Vector3 rotation = new Vector3( 90, 180, 0 );
        Vector3 position = new Vector3( 0, 0.1f, -2);

        private void Awake()
        {
            transform.rotation = Quaternion.Euler(rotation);
            transform.position = position;

            tooltip = transform.FindChild("Canvas/ToolTip").gameObject;
            tooltip.SetActive(false);

            UnityEventTrigger tooltipHoverEnterEvent = transform.FindChild("Canvas/PF_PWM_Trigger_UnityEvents/PF_UnityEventTrigger_HoverEnter").GetComponent<UnityEventTrigger>();
            tooltipHoverEnterEvent.Event.AddListener(new Action(()=> {
                tooltip.SetActive(true);
            }));

            UnityEventTrigger tooltipHoverExitEvent = transform.FindChild("Canvas/PF_PWM_Trigger_UnityEvents/PF_UnityEventTrigger_HoverExit").GetComponent<UnityEventTrigger>();
            tooltipHoverExitEvent.Event.AddListener(new Action(() => {
                tooltip.SetActive(false);
            }));


            UnityEventTrigger toggleLobbyPanelEvent = transform.FindChild("Canvas/PF_PWM_Trigger_UnityEvents/PF_UnityEventTrigger_OnClick").GetComponent<UnityEventTrigger>();
            toggleLobbyPanelEvent.Event.AddListener(new Action(TogglePWMPanel));

            global::Messenger.Default.Register<global::Messages.GameScoreEvent>(new Action<global::Messages.GameScoreEvent>(OnGameEndEvent));
            global::Messenger.Default.Register<global::Messages.GameStartEvent>(new Action<global::Messages.GameStartEvent>(OnGameStartEvent));
        }

        private void OnGameStartEvent(global::Messages.GameStartEvent obj)
        {
            this.gameObject.SetActive(false);
        }

        private void OnGameEndEvent(global::Messages.GameScoreEvent obj)
        {
            this.gameObject.SetActive(true);
        }

        private void TogglePWMPanel()
        {
            if (pwmPanel != null)
                pwmPanel.SetActive(!pwmPanel.activeSelf);
            else
                MelonLogger.Msg("OpenPWM: PWM panel is not assigned and cannot be toggled");
        }
    }
}
