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
        //private List<string> currentModifiers = new List<string>();
        private ulong bitPackedModifiers;


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
            bitPackedModifiers = GameplayDatabase.GetBitPackedModifiers(obj.modifiers);
        }

        void Start()
        {

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

            var dest = GameManager.theDestination;

            var lobby = new PWM.Messages.Lobby {
                Id = lobbyName,
                MaxPlayerCount = playerCount.Count,
                Players = new List<Messages.Player>(),
                Level = new Messages.Network.SetLevel
                {
                    BaseName = dest.level.baseName,
                    Difficulty = (int)GameManager.GetDifficulty(),
                    BitPackedModifiers = GameManager.Instance.GetBitPackedModifiers(),
                    PlayIntent = (int)PlayIntent.FREEPLAY
                }
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
