using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;
using UnhollowerRuntimeLib;
using UnityEngine;
using TMPro;


namespace PWM
{
    [RegisterTypeInIl2Cpp]
    public class AssetBundleBank : MonoBehaviour
    {
        public AssetBundleBank(IntPtr ptr) : base(ptr) { }

        public static AssetBundleBank Instance = null;

        //For some reason loaded assets get yeeted so we have to do this..
        public LobbyPreviewEntry LobbyPreviewEntryPrefab
        {
            get
            {
                if (uiBundle == null)
                    return null;
                var asset = uiBundle.Load<GameObject>("LobbyPreviewEntry").GetComponent<LobbyPreviewEntry>();
                SetSPShaderForTMP(asset.gameObject);
                return asset;
            }
        }
        public PlayerEntry PlayerEntryPrefab
        {
            get
            {
                if (uiBundle == null)
                    return null;
                var asset = uiBundle.Load<GameObject>("Player_Entry").GetComponent<PlayerEntry>();
                SetSPShaderForTMP(asset.gameObject);
                return asset;
            }
        }

        public GameObject LobbyPanelPrefab
        {
            get
            {
                if (uiBundle == null)
                    return null;
                var asset = uiBundle.Load<GameObject>("pwm_lobby_board_ui");
                SetSPShaderForTMP(asset);
                return asset;
            }
        }

        public GameObject KeyboardPrefab
        {
            get
            {
                if (uiBundle == null)
                    return null;
                var asset = keyboardBundle.Load<GameObject>("Keyboard_Canvas");
                SetSPShaderForTMP(asset);
                return asset;
            }
        }

        public Il2CppAssetBundle keyboardBundle;
        public Il2CppAssetBundle uiBundle;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                LoadAssets();
            }
        }


        void LoadAssets()
        {
            MelonLogger.Msg("Loading asset bundles");
            uiBundle = Il2CppAssetBundleManager.LoadFromFile("Mods/PWM/pwm");
            keyboardBundle = Il2CppAssetBundleManager.LoadFromFile("Mods/PWM/Keyboard");

            //Fix to single pass render
            //MelonLogger.Msg("Setting single-pass shader TMPro assets");
            //SetSPShaderForTMP(LobbyPanelPrefab);
            //SetSPShaderForTMP(LobbyPreviewEntryPrefab.gameObject);
            //SetSPShaderForTMP(PlayerEntryPrefab.gameObject);
            //SetSPShaderForTMP(KeyboardPrefab);
        }

        //Avoid double render because of multi pass rendering being applied
        public static void SetSPShaderForTMP(GameObject go)
        {
            var fixArray = go.transform.GetComponentsInChildren<TextMeshProUGUI>();
            MelonLogger.Msg($"fixing {fixArray.Length} tmp items");
            var spShader = Shader.Find("TextMeshPro/Distance Field_SP_Instanced");
            foreach (var item in fixArray)
            {
                item.fontMaterial.shader = spShader;
            }
        }
    }
}
