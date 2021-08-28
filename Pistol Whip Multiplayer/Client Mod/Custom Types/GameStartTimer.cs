using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using UnhollowerRuntimeLib;

namespace PWM
{
    class GameStartTimer : MonoBehaviour
    {
        public GameStartTimer(IntPtr ptr) : base(ptr) { }

        private TMP_Text timerTxt;
        private float timerEnd;
        private bool timerIsOn;

        void Awake()
        {
            timerTxt = this.GetComponent<TMP_Text>();
            timerEnd = 0;
            timerIsOn = false;
        }

        void Update()
        {
            if (timerIsOn)
            {
                if (timerEnd < Time.time)
                {
                    timerTxt.text = "";
                    timerIsOn = false;
                }
                else
                    timerTxt.text = $"{(timerEnd-Time.time):0.#}s";
            }
        }

        public void SetTimer(float time)
        {
            timerEnd = Time.time + time;
            timerIsOn = true;
        }
    }
}
