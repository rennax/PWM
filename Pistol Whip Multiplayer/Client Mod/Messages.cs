using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


namespace PWM
{

    namespace Messages
    {

        public class Lobby
        {
            public string Id { get; set; }
            public int MaxPlayerCount { get; set; }
            public List<Player> Players { get; set; }
            public Player Host { get; set; }

            public List<string> Modifiers { get; set; }
            public Network.SetLevel Level { get; set; }
        }

        public class DifficultySelected
        {
            public Difficulty Difficulty { get; set; }
        }

        public class CreatedLobby
        {
            public Lobby Lobby { get; set; }
        }

        public class ClosedLobby
        {
            public Lobby Lobby { get; set; }
        }

        public class LobbyList
        {
            public List<Lobby> Lobbies { get; set; }
        }

        public class StartGame
        {
            public int DelayMS { get; set; }
        }

        public class NewModifiers
        {
            public List<string> Modifiers { get; set; }
        }
        public class Player
        {
            public string Name { get; set; }
            public bool Ready { get; set; }
        }

        public class PlayerLeft
        {
            public Player Player { get; set; }
        }

        public class PlayerJoined
        {
            public Player Player { get; set; }
        }

        public class PlayerReady
        {
            public Player Player { get; set; }
        }

        public class JoinedLobby
        {
            public Lobby Lobby { get; set; }
        }

        public class SelectLevel
        {
            public string GroupName { get; set; }
            public int Index { get; set; }
            public int Difficulty { get; set; }

            public override string ToString()
            {
                return $"group {GroupName} with song index {Index}, difficulty int {Difficulty}";
            }
        }

        public class Disconnected
        {

        }

        public class ScoreSync
        {
            public int Score { get; set; }
            public float BeatAccuracy { get; set; }
            public float HitAccuracy { get; set; } 
        }

        public class UpdateScore
        {
            public Player Player { get; set; }
            public ScoreSync Score { get; set; }
        }

        namespace Network
        {
            public class Error
            {
                public string Call { get; set; }
                public string Reason { get; set; }
            }

            public class SetLevel
            {
                public string GroupName { get; set; }
                public string SongName { get; set; }
                public int Difficulty { get; set; }
            }
        }
    }


}
