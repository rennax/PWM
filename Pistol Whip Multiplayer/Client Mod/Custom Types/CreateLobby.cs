using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MelonLoader;

using UnityEngine.Events;
using UnityEngine;
using TMPro;


namespace PWM
{
    class CreateLobby : MonoBehaviour
    {
        public CreateLobby(IntPtr ptr) : base(ptr) { }

        private PlayerCount playerCount;
        private TMP_InputField lobbyNameInput;
        private const int maxNameLenght = 14;

        public LobbyManager lobbyManager;
        private List<string> currentModifiers = new List<string>();

        private KeyCode[] keyCodesToAvoid = { KeyCode.None, KeyCode.LeftAlt, KeyCode.RightAlt, KeyCode.LeftShift, KeyCode.RightShift};

        void Awake()
        {
            playerCount = transform.FindChild("Player_Count").GetComponent<PlayerCount>();
            lobbyNameInput = transform.FindChild("Input/Field").GetComponent<TMP_InputField>();


            var onClickCreateLobbyEvent = transform.Find("Create/PF_PWM_Trigger_UnityEvents/PF_UnityEventTrigger_OnClick").GetComponent<UnityEventTrigger>();
            onClickCreateLobbyEvent.Event.AddListener(new Action(OnCreateLobby));

            var onClickCancelEvent = transform.Find("Cancel/PF_PWM_Trigger_UnityEvents/PF_UnityEventTrigger_OnClick").GetComponent<UnityEventTrigger>();
            onClickCancelEvent.Event.AddListener(new Action(OnCancel));

            var onClickInputEvent = transform.Find("Input/PF_PWM_Trigger_UnityEvents/PF_UnityEventTrigger_OnClick").GetComponent<UnityEventTrigger>();
            onClickInputEvent.Event.AddListener(new Action(OnInputSelect));

            //Register global event listeners
            global::Messenger.Default.Register<global::Messages.ModifiersSet>(new Action<global::Messages.ModifiersSet>(OnModifiersSet));

        }

        private void OnModifiersSet(global::Messages.ModifiersSet obj)
        {
            currentModifiers = obj.modifiers.ToArray().Select(m => m.Name).ToList();
        }

        void Start()
        {

        }

        void Update()
        {

            Event e = Event.current;
            if (e.isKey)
            {
                string txt = lobbyNameInput.text;
                if (e.keyCode == KeyCode.Backspace)
                {
                    if (txt.Length > 0)
                    {
                        txt = txt.Substring(0, txt.Length - 1);
                    }
                }
                else if (keyCodesToAvoid.Contains<KeyCode>(e.keyCode))
                { }//Skip
                else
                {
                    string c;
                    switch (e.keyCode)
                    {
                        case KeyCode.Space:
                            c = " ";
                            break;
                        case KeyCode.Keypad0:
                        case KeyCode.Alpha0:
                            c = "0";
                            break;
                        case KeyCode.Keypad1:
                        case KeyCode.Alpha1:
                            c = "1";
                            break;
                        case KeyCode.Keypad2:
                        case KeyCode.Alpha2:
                            c = "2";
                            break;
                        case KeyCode.Keypad3:
                        case KeyCode.Alpha3:
                            c = "3";
                            break;
                        case KeyCode.Keypad4:
                        case KeyCode.Alpha4:
                            c = "4";
                            break;
                        case KeyCode.Keypad5:
                        case KeyCode.Alpha5:
                            c = "5";
                            break;
                        case KeyCode.Keypad6:
                        case KeyCode.Alpha6:
                            c = "6";
                            break;
                        case KeyCode.Keypad7:
                        case KeyCode.Alpha7:
                            c = "7";
                            break;
                        case KeyCode.Keypad8:
                        case KeyCode.Alpha8:
                            c = "8";
                            break;
                        case KeyCode.Keypad9:
                        case KeyCode.Alpha9:
                            c = "9";
                            break;
                        case KeyCode.A:
                        case KeyCode.B:
                        case KeyCode.C:
                        case KeyCode.D:
                        case KeyCode.E:
                        case KeyCode.F:
                        case KeyCode.G:
                        case KeyCode.H:
                        case KeyCode.I:
                        case KeyCode.J:
                        case KeyCode.K:
                        case KeyCode.L:
                        case KeyCode.M:
                        case KeyCode.N:
                        case KeyCode.O:
                        case KeyCode.P:
                        case KeyCode.Q:
                        case KeyCode.R:
                        case KeyCode.S:
                        case KeyCode.T:
                        case KeyCode.U:
                        case KeyCode.V:
                        case KeyCode.W:
                        case KeyCode.X:
                        case KeyCode.Y:
                        case KeyCode.Z:
                            c = $"{e.keyCode}".ToLower();
                            break;

                        case KeyCode.Exclaim:
                            c = "!";
                            break;
                        case KeyCode.DoubleQuote:
                            c = "\"";
                            break;
                        case KeyCode.Hash:
                            c = "#";
                            break;
                        case KeyCode.Dollar:
                            c = "$";
                            break;
                        case KeyCode.Percent:
                            c = "%";
                            break;
                        case KeyCode.Ampersand:
                            c = "&";
                            break;
                        case KeyCode.Quote:
                            c = "'";
                            break;
                        case KeyCode.LeftParen:
                            c = "(";
                            break;
                        case KeyCode.RightParen:
                            c = ")";
                            break;
                        case KeyCode.Plus:
                            c = "+";
                            break;
                        case KeyCode.Comma:
                            c = ",";
                            break;
                        case KeyCode.Minus:
                            c = "-";
                            break;
                        case KeyCode.Period:
                            c = ".";
                            break;
                        case KeyCode.Slash:
                            c = "/";
                            break;
                        case KeyCode.Colon:
                            c = ":";
                            break;
                        case KeyCode.Semicolon:
                            c = ";";
                            break;
                        case KeyCode.Less:
                            c = "<";
                            break;
                        case KeyCode.Equals:
                            c = "=";
                            break;
                        case KeyCode.Greater:
                            c = ">";
                            break;
                        case KeyCode.Question:
                            c = "?";
                            break;
                        case KeyCode.At:
                            c = "@";
                            break;
                        case KeyCode.LeftBracket:
                            c = "{";
                            break;
                        case KeyCode.Backslash:
                            c = "\\";
                            break;
                        case KeyCode.RightBracket:
                            c = "}";
                            break;
                        case KeyCode.Underscore:
                            c = "_";
                            break;
                        case KeyCode.BackQuote:
                            c = "`";
                            break;
                        case KeyCode.Caret:
                        case KeyCode.Asterisk:
                        default:
                            c = "";
                            break;
                    }

                    txt = $"{txt}{c}";
                }
                lobbyNameInput.text = txt;
            }

            
        }

 

        //Hack for dealing with input field
        //Host must write the name using keyboard
        private void OnInputSelect()
        {
            MelonLogger.Msg("Selected lobby name input field. Creating VR keyboard");
            Vector3 posAdjust = lobbyManager.transform.position;
            posAdjust.z += 0.1f;
            var keyboard = Keyboard.VRKeyboardManager.Default;
            keyboard.SetPlacement(posAdjust, lobbyManager.transform.rotation);
            keyboard.Listen(LobbyNameUpdated);

            //lobbyNameInput.ActivateInputField();
            //lobbyNameInput.Select();
        }

        private void LobbyNameUpdated(string name)
        {
            MelonLogger.Msg($"Set new lobby name {name}");
            lobbyNameInput.text = name;
        }

        private void OnCreateLobby()
        {
            MelonLogger.Msg("Created lobby");
            string lobbyName = lobbyNameInput.text;

            if (lobbyName.Length > maxNameLenght)
                lobbyName = lobbyName.Substring(0, maxNameLenght);


            var lobby = new PWM.Messages.Lobby {
                Id = lobbyName,
                MaxPlayerCount = playerCount.Count,
                Players = new List<Messages.Player>(),
                Modifiers = currentModifiers
            };
            Client.client.EmitAsync("CreateLobby", lobby, lobbyManager.Player);
            lobbyManager.ShowLobbyOverviewCanvas();
        }

        //TODO in UI
        private void OnCancel()
        {
            lobbyManager.ShowLobbyListCanvas();
        }
    }
}
