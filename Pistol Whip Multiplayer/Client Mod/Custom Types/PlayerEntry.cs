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
        public string Name { set 
                {
                _name = value;
                name = value;
                pName.SetText(value);
                }
            get => _name;
        }

        
        private string _name = "";
        private TMP_Text hitAcc;
        private TMP_Text beatAcc;
        private TMP_Text score;
        private Transform readyIcon;

        private Messages.ScoreSync scoreSync;

        void Awake()
        {
            pName = transform.FindChild("Name").GetComponent<TMP_Text>();
            score = transform.FindChild("Score").GetComponent<TMP_Text>();
            beatAcc = transform.FindChild("BeatAcc").GetComponent<TMP_Text>();
            hitAcc = transform.FindChild("HitAcc").GetComponent<TMP_Text>();
            readyIcon = transform.FindChild("Ready_Icon");
            IsReady(false);

            UpdateEntry(new Messages.ScoreSync { Score = 0, HitAccuracy = 0, BeatAccuracy = 0 }); 
        }

        //OnEnable handle cases where updates are made while the lobby ui is disabled
        void OnEnable()
        {
            pName.text = _name;
            UpdateUI();
        }

        public void UpdateEntry(PWM.Messages.ScoreSync scoreSync)
        {
            this.scoreSync = scoreSync;
            UpdateUI();
        }

        private void UpdateUI()
        {
            score.text = $"{scoreSync.Score}";
            beatAcc.text = $"{scoreSync.BeatAccuracy * 100:0.00} %";
            hitAcc.text = $"{scoreSync.HitAccuracy * 100:0.00} %";
        }

        public void IsReady(bool ready)
        {
            readyIcon.gameObject.SetActive(ready);
        }
    }
}
