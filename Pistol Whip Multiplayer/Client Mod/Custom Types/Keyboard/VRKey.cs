using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using MelonLoader;

namespace Keyboard
{
    public class VRKey : MonoBehaviour
    {
        public VRKey(IntPtr ptr) : base(ptr) { }


        TMP_Text text;

        private string Char { get => text.text; }

        protected virtual void Awake()
        {
            text = transform.FindChild("Label").GetComponent<TMP_Text>();

            UnityEventTrigger onClick = transform.FindChild("PF_PWM_Trigger_UnityEvents/PF_UnityEventTrigger_OnClick").GetComponent<UnityEventTrigger>();
            onClick.Event.AddListener(new Action(OnClick));
        }


        public virtual void OnClick()
        {
            MelonLogger.Msg($"Pressed: {Char}");
            VRKeyboardManager.Default.AddCharacter(Char);
        }
    }
}
