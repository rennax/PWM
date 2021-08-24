using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;

namespace PWM
{
    class PlayerCount : MonoBehaviour
    {
        public PlayerCount(IntPtr ptr) : base(ptr) { }

        private TMP_Text countText;
        private int count = 4;
        public int Count { get => count; }
        private int maxCount = 10;

        void Awake()
        {
            countText = transform.FindChild("Display").GetComponent<TMP_Text>();

            var onClickUpEvent = transform.Find("Up/PF_PWM_Trigger_UnityEvents/PF_UnityEventTrigger_OnClick").GetComponent<UnityEventTrigger>();
            onClickUpEvent.Event.AddListener(new Action(OnUp));
            onClickUpEvent.Event.AddListener(new Action(UpdateCountText));

            var onClickDownEvent = transform.Find("Down/PF_PWM_Trigger_UnityEvents/PF_UnityEventTrigger_OnClick").GetComponent<UnityEventTrigger>();
            onClickDownEvent.Event.AddListener(new Action(OnDown));
            onClickDownEvent.Event.AddListener(new Action(UpdateCountText));
        }

        void Start()
        {
            UpdateCountText();
        }


        private void OnUp()
        {
            count = ( (count + 1) > maxCount ? maxCount : (count + 1) );
        }

        private void OnDown()
        {
            count = ((count - 1) < 1 ? 1 : (count - 1));
        }

        private void UpdateCountText()
        {
            countText.text = $"{count}";
        }

    }
}
