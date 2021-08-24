using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnhollowerRuntimeLib;
using UnityEngine;
using MelonLoader;

namespace PWM
{
    public class LobbyManager : MonoBehaviour
    {
        public LobbyManager(IntPtr ptr) : base(ptr) { }

        private Messages.Player player;
        public Messages.Player Player { get => player; }

        public ScoreDisplay scoreDisplay;
        private Dictionary<PWM.Messages.Player, PWM.Messages.ScoreSync> scores = new Dictionary<PWM.Messages.Player, PWM.Messages.ScoreSync>();

        private LobbyList lobbyList;
        private LobbyOverview lobbyOverview;
        private CreateLobby createLobby;

        

        //3d back panel
        public GameObject backPanel;

        void Start()
        {
            //PWM.Messenger.Default.Register<PWM.Messages.UpdateScore>(OnUpdateScore);

            player = new PWM.Messages.Player
            {
                Name = CloudheadGames.CHFramework.Platform.CHPlatformManager.UserName()
            };

            //Make sure that we have the back panel visible by using a CH shader
            backPanel = this.transform.FindChild("Mesh/Board_Mesh").gameObject;
            backPanel.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Cloudhead/Universal/HighEnd/PW_Props"));

            //Set correct transform
            transform.position = new Vector3(0, 1.4f, -2.5f);
            transform.rotation = Quaternion.Euler(0, 180, 0);

            lobbyList = this.transform.FindChild("Mesh/PWM_Lobby_List_Canvas").GetComponent<LobbyList>();
            lobbyList.lobbyManager = this;

            createLobby = this.transform.FindChild("Mesh/PWM_Lobby_Create_Canvas").GetComponent<CreateLobby>();
            createLobby.lobbyManager = this;

            lobbyOverview = this.transform.FindChild("Mesh/PWM_Lobby_Overview_Canvas").GetComponent<LobbyOverview>();
            lobbyOverview.lobbyManager = this;

            ShowLobbyListCanvas();

            Messenger.Default.Register(new Action<Messages.JoinedLobby>(OnJoinedLobby));
            Messenger.Default.Register(new Action<Messages.Disconnected>(OnDisconnected));

#if DEBUG
            Invoke("SkipIntro", 4);
            //Invoke("AutoJoin", 5);
#endif
        }

        //If we disconnect, just disable any active UI
        private void OnDisconnected(Messages.Disconnected obj)
        {
            lobbyOverview.CurrentLobby = null;
            ShowLobbyListCanvas();
        }

        //void Update()
        //{

        //    Event e = Event.current;
        //    if (e.isKey && e.keyCode == KeyCode.F1)
        //    {
        //        GameplayDatabase DB = GameplayManager.gameplayDB;
        //        var modifiers = new List<GameModifierEntry>();
        //        modifiers.Add(DB.AllModifiers[3]); //Duals
        //        modifiers.Add(DB.AllModifiers[9]); //Disorder
        //        modifiers.Add(DB.AllModifiers[16]); //Revolver




        //    }
        //}

        void OnJoinedLobby(Messages.JoinedLobby msg)
        {
            ShowLobbyOverviewCanvas();
            lobbyOverview.JoinedLobby(msg.Lobby);

        }

        void SkipIntro()
        {
            GameObject.Find("Staging/Set Dressing/Backlot Gates/StartPanel/Canvas/PWUIButton-Start").GetComponent<IntroPanelButtonController>().TransitionToScenes();
        }

        void AutoJoin()
        {
            ShowLobbyOverviewCanvas();
            Client.client.EmitAsync("JoinLobby", player, "TEST");

        }

        public void SetModifiers(List<string> modifiers)
        {
            StyleTabsManager styleTabsManager = GameObject.Find("Managers/UI State Controller/PF_CHUI_AnchorPt_Styles/PF_StylesCanvas/StylesPanel").GetComponent<StyleTabsManager>();
            styleTabsManager.SelectModChooserButtonHandler();

            //Gather the StyleCustomizerModIcons we need to press
            //TODO: create static container so we only have to do this once
            StyleCustomize styleCustomize = GameObject.Find("Managers/UI State Controller/PF_CHUI_AnchorPt_Styles/PF_StylesCanvas/StylesPanel/Open/StylesCustomize").GetComponent<StyleCustomize>();
            List<StyleCustomizerModIcon> mods = new List<StyleCustomizerModIcon>();

            foreach (var obj in styleCustomize.GunTypeButtons)
            {
                mods.Add(obj.GetComponent<StyleCustomizerModIcon>());
            }

            foreach (var obj in styleCustomize.modButtons)
            {
                mods.Add(obj.GetComponent<StyleCustomizerModIcon>());
            }

            foreach (var mod in mods)
            {
                if (modifiers.Any(m => m == mod.modName) == true)
                {
                    //Mod is not enabled, thus we need to enable it
                    if (!mod.modEntry.setting.Value)
                    {
                        mod.handleToggle();
                    }
                }
                else
                {
                    if (mod.modEntry.setting.Value)
                    {
                        mod.handleToggle();
                    }
                }
            }

            if (modifiers.Any(m => m == "Dual Wield") == true)
            {
                if (!styleCustomize.DualWieldSelector.dualWieldMod.setting.Value)
                {
                    styleCustomize.DualWieldSelector.icon.handleToggle();
                }
            }
            else
            {
                if (styleCustomize.DualWieldSelector.dualWieldMod.setting.Value)
                {
                    styleCustomize.DualWieldSelector.icon.handleToggle();
                }
            }
        }


        void OnUpdateScore(PWM.Messages.UpdateScore score)
        {
            if (scores.ContainsKey(score.Player))
            {
                scores[score.Player] = score.Score;
                scoreDisplay.UpdateScoreDisplay(scores);
            }
            else
            {
                MelonLogger.Msg($"Failed to update score for {score.Player}");
            }
        }

        


        public void ShowLobbyListCanvas()
        {
            createLobby.gameObject.SetActive(false);
            lobbyList.gameObject.SetActive(true);
            lobbyOverview.gameObject.SetActive(false);
            lobbyList.RefreshLobbyList();
        }

        public void ShowCreateLobbyCanvas()
        {
            createLobby.gameObject.SetActive(true);
            lobbyList.gameObject.SetActive(false);
            lobbyOverview.gameObject.SetActive(false);
        }

        public void ShowLobbyOverviewCanvas()
        {
            createLobby.gameObject.SetActive(false);
            lobbyList.gameObject.SetActive(false);
            lobbyOverview.gameObject.SetActive(true);
        }


    }
}
