using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;

namespace PWM
{
    public class PlayerEntry : MonoBehaviour
    {
        public PlayerEntry(IntPtr ptr) : base(ptr) { }


        private TMP_Text pName;
        public string Name { set => pName.text = value; }
        private TMP_Text hitAcc;
        private TMP_Text beatAcc;
        private TMP_Text score;

        void Awake()
        {
            pName = transform.FindChild("Name").GetComponent<TMP_Text>();
            score = transform.FindChild("Score").GetComponent<TMP_Text>();
            beatAcc = transform.FindChild("BeatAcc").GetComponent<TMP_Text>();
            hitAcc = transform.FindChild("HitAcc").GetComponent<TMP_Text>();

            UpdateEntry(new Messages.ScoreSync { Score = 0, HitAccuracy = 0, BeatAccuracy = 0 }); 
        }

        public void UpdateEntry(PWM.Messages.ScoreSync scoreSync)
        {
            score.text = $"{scoreSync.Score}";
            beatAcc.text = $"{scoreSync.BeatAccuracy*100:0.00} %";
            hitAcc.text = $"{scoreSync.HitAccuracy*100:0.00} %";
        }
    }
}
