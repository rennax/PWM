using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Keyboard
{
    public class VRKeyOK : MonoBehaviour
    {
        public VRKeyOK(IntPtr ptr) : base(ptr) { }


        void Awake()
        {
            UnityEventTrigger onClick = transform.FindChild("PF_PWM_Trigger_UnityEvents/PF_UnityEventTrigger_OnClick").GetComponent<UnityEventTrigger>();
            onClick.Event.AddListener(new Action(OnClick));
        }
        public void OnClick()
        {
            VRKeyboardManager.Default.Enter();
        }
    }
}
