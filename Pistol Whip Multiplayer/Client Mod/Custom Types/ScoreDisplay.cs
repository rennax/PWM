using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MelonLoader;

namespace PWM
{
    public class ScoreDisplay : MonoBehaviour
    {
        public ScoreDisplay(IntPtr ptr) : base(ptr) { }

        private List<TMP_Text> scoreText = new List<TMP_Text>();

        void Start()
        {

        }


        public void UpdateScoreDisplay(Dictionary<PWM.Messages.Player, PWM.Messages.ScoreSync> scores)
        {
            int index = 0;
            var _scores = scores.ToList();
            //TODO add fix if scoreText is not large enough

            //Sort so largest score is on top of leader board
            _scores.Sort((pair1, pair2) => pair1.Value.Score.CompareTo(pair2.Value.Score));

            foreach (var score in _scores)
            {
                if (scoreText[index] != null)
                {
                    scoreText[index].text = $"{score.Key}: {score.Value.Score}";
                }
                else
                {
                    MelonLogger.Msg($"Either no text or value for key {score.Key}");
                }
                index++;
            }
        }

    }
}
