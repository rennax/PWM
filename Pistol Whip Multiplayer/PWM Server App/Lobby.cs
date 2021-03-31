using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketIOSharp.Server.Client;
using SocketIOSharp.Common;
using EngineIOSharp.Common.Enum;
using SocketIOSharp.Client;


namespace PWM
{
    class Lobby
    {
        private List<SocketIOSocket> clients = new List<SocketIOSocket>();
        private List<Player> players = new List<Player>();
        private Dictionary<Player, Score> playerScores = new Dictionary<Player, Score>();

        private bool mapRunning = false;

        public void UpdateScore(Player player, Score score)
        {
            Score oldScore;
            if (playerScores.TryGetValue(player, out oldScore))
            {
                playerScores[player] = score;
            }
        }

        public void JoinLobby(Player player)
        {
            players.Add(player);
            playerScores.Add(player, new Score());
        }


        
        
    }


    class Player
    {
        string name;
    }

    struct Score
    {
        public int score;
        public float accuracy;
        public float onBeat;
    }

    struct SongInfo
    {
        public string songName;
        public float songLength;
        public float timeElapsed;
    }
}
