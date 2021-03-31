using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using MelonLoader;
using Harmony;
using System.Collections.Concurrent;

namespace PWM
{
    public class Entry : MelonMod
    {
        public static ConcurrentQueue<Task> workQueue = new ConcurrentQueue<Task>();

        Client client;
        public override void OnApplicationStart()
        {
            UnityTaskScheduler.mainThread = Thread.CurrentThread;
            client = new Client();
            client.Initialize();
            
        }

        public override void OnUpdate()
        {
            UnityTaskScheduler scheduler = (UnityTaskScheduler)UnityTaskScheduler.Default;
            scheduler.Start();

            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Alpha1))
            {
                
            }

        }



        [HarmonyPatch(typeof(PlayerActionManager), "OnGameStart", new System.Type[0] { })]
        public static class PlayerActionManagerGameStartMod
        {
            public static void Postfix(PlayerActionManager __instance)
            {
            }
        }
    }


}