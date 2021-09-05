using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

namespace Keyboard
{
    public class VRKeyboardManager : MonoBehaviour
    {
        public VRKeyboardManager(IntPtr ptr) : base(ptr) { }

        public static VRKeyboardManager Default;

        UnityEvent<string> callbackOnOK = new UnityEvent<string>(); 

        string input = "";

        TMP_Text showText;

        void Awake()
        {
            if (!Default)
            {
                Default = this;
                showText = transform.FindChild("TextField").GetComponent<TMP_Text>();
            }

        }

        void Start()
        {
            this.gameObject.SetActive(false);
        }

        public void AddCharacter(string _char)
        {
            input = $"{input}{_char}";
            UpdateText();
        }

        public void Delete()
        {
            if (input.Length > 0)
            {
                input = input.Substring(0, input.Length - 1);
                UpdateText();
            }
        }

        private void UpdateText()
        {
            showText.text = input;
        }

        public void Enter()
        {
            callbackOnOK.Invoke(input);
            callbackOnOK.RemoveAllListeners();
            this.gameObject.SetActive(false);
        }

        public void SetPlacement(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            
            this.transform.position = position;
            this.transform.rotation = rotation;
            this.transform.localScale = scale;
        }

        public void SetPlacement(Vector3 position, Quaternion rotation)
        {
            SetPlacement(position, rotation, new Vector3(1, 1, 1));
        }


        public void Listen(Action<string> action)
        {
            this.gameObject.SetActive(true);
            callbackOnOK.AddListener(action);
        }



    }
}
