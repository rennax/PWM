using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


namespace PWM
{
    namespace Network
    {
        namespace Messages
        {

            public class StartGame
            {
                public int delayMS;
            }

            public class Player
            {
                Guid guid;
                public string playerName;
            }

            public class SelectLevel
            {
                public string groupName;
                public int index;
                public int difficulty;

                public override string ToString()
                {
                    return $"group {groupName} with song index {index}, difficulty int {difficulty}";
                }
            }
        }
    }

}
