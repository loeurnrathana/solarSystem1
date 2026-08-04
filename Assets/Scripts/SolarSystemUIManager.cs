using UnityEngine;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SolarSystemScope
{
    public class SolarSystemUIManager : MonoBehaviour
    {
        public static SolarSystemUIManager Instance { get; private set; }

        private List<CelestialBody> celestialBodies = new List<CelestialBody>();
        private CelestialBody focusedBody;
        private SolarSystemCameraController cameraCtrl;
        private PlanetLabelManager labelManager;

        private readonly string[] bodyOrder = new string[]
        {
            "Mercury", // 1
            "Venus",   // 2
            "Earth",   // 3
            "Mars",    // 4
            "Jupiter", // 5
            "Saturn",  // 6
            "Uranus",  // 7
            "Neptune", // 8
            "Sun"      // 9
        };

        private GUIStyle buttonNormalStyle;
        private GUIStyle buttonActiveStyle;
        private GUIStyle headerStyle;
        private Texture2D btnBgTexNormal;
        private Texture2D btnBgTexActive;
        private Texture2D panelBgTex;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (cameraCtrl == null)
            {
                cameraCtrl = Object.FindAnyObjectByType<SolarSystemCameraController>();
            }
            EnsureBodiesLoaded();
        }

        public void Initialize(List<CelestialBody> bodies, SolarSystemCameraController camController, PlanetLabelManager labelMgr = null)
        {
            celestialBodies = bodies ?? new List<CelestialBody>();
            cameraCtrl = camController;
            labelManager = labelMgr;

            focusedBody = null;
            if (labelManager != null)
            {
                labelManager.SetFocusedBody(null);
            }
        }

        private void EnsureBodiesLoaded()
        {
            if (celestialBodies == null || celestialBodies.Count == 0)
            {
                celestialBodies = new List<CelestialBody>(Object.FindObjectsByType<CelestialBody>(FindObjectsInactive.Exclude));
            }
        }

        private void Update()
        {
            CheckKeyboardShortcuts();
        }

        private void CheckKeyboardShortcuts()
        {
            int selectedNum = -1;

#if ENABLE_INPUT_SYSTEM
            try
            {
                if (Keyboard.current != null)
                {
                    if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame) selectedNum = 1;
                    else if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame) selectedNum = 2;
                    else if (Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame) selectedNum = 3;
                    else if (Keyboard.current.digit4Key.wasPressedThisFrame || Keyboard.current.numpad4Key.wasPressedThisFrame) selectedNum = 4;
                    else if (Keyboard.current.digit5Key.wasPressedThisFrame || Keyboard.current.numpad5Key.wasPressedThisFrame) selectedNum = 5;
                    else if (Keyboard.current.digit6Key.wasPressedThisFrame || Keyboard.current.numpad6Key.wasPressedThisFrame) selectedNum = 6;
                    else if (Keyboard.current.digit7Key.wasPressedThisFrame || Keyboard.current.numpad7Key.wasPressedThisFrame) selectedNum = 7;
                    else if (Keyboard.current.digit8Key.wasPressedThisFrame || Keyboard.current.numpad8Key.wasPressedThisFrame) selectedNum = 8;
                    else if (Keyboard.current.digit9Key.wasPressedThisFrame || Keyboard.current.numpad9Key.wasPressedThisFrame) selectedNum = 9;
                    else if (Keyboard.current.digit0Key.wasPressedThisFrame || Keyboard.current.numpad0Key.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame) selectedNum = 0;
                }
            }
            catch {}
#endif

            if (selectedNum == -1)
            {
                try
                {
                    if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) selectedNum = 1;
                    else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) selectedNum = 2;
                    else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) selectedNum = 3;
                    else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) selectedNum = 4;
                    else if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5)) selectedNum = 5;
                    else if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6)) selectedNum = 6;
                    else if (Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7)) selectedNum = 7;
                    else if (Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8)) selectedNum = 8;
                    else if (Input.GetKeyDown(KeyCode.Alpha9) || Input.GetKeyDown(KeyCode.Keypad9)) selectedNum = 9;
                    else if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0) || Input.GetKeyDown(KeyCode.Escape)) selectedNum = 0;
                }
                catch {}
            }

            if (selectedNum >= 1 && selectedNum <= 9)
            {
                SelectByNumber(selectedNum);
            }
            else if (selectedNum == 0)
            {
                DeselectToOverview();
            }
        }

        public void SelectByNumber(int num)
        {
            if (num < 1 || num > 9) return;
            string targetName = bodyOrder[num - 1];

            EnsureBodiesLoaded();
            CelestialBody targetBody = celestialBodies.Find(b => b != null && b.bodyName == targetName);
            if (targetBody == null)
            {
                GameObject obj = GameObject.Find(targetName);
                if (obj != null) targetBody = obj.GetComponent<CelestialBody>();
            }

            if (targetBody != null)
            {
                focusedBody = targetBody;
                if (cameraCtrl != null)
                {
                    cameraCtrl.SelectCelestialBody(targetBody);
                }
                if (labelManager != null)
                {
                    labelManager.SetFocusedBody(targetBody);
                }
            }
        }

        public void DeselectToOverview()
        {
            focusedBody = null;
            if (cameraCtrl != null)
            {
                cameraCtrl.DeselectPlanet();
            }
            if (labelManager != null)
            {
                labelManager.SetFocusedBody(null);
            }
        }

        public void SelectBody(CelestialBody body)
        {
            if (body == null) return;
            focusedBody = body;

            if (cameraCtrl != null)
            {
                cameraCtrl.SelectCelestialBody(body);
            }

            if (labelManager != null)
            {
                labelManager.SetFocusedBody(body);
            }
        }

        private void InitGUIStyles()
        {
            if (buttonNormalStyle != null && btnBgTexNormal != null) return;

            btnBgTexNormal = MakeSolidTex(new Color(0.06f, 0.12f, 0.22f, 0.85f));
            btnBgTexActive = MakeSolidTex(new Color(0.12f, 0.45f, 0.70f, 0.95f));
            panelBgTex = MakeSolidTex(new Color(0.02f, 0.04f, 0.08f, 0.82f));

            buttonNormalStyle = new GUIStyle(GUI.skin.button);
            buttonNormalStyle.fontSize = 15;
            buttonNormalStyle.fontStyle = FontStyle.Bold;
            buttonNormalStyle.normal.textColor = new Color(0.85f, 0.95f, 1.0f);
            buttonNormalStyle.hover.textColor = Color.yellow;
            buttonNormalStyle.normal.background = btnBgTexNormal;
            buttonNormalStyle.alignment = TextAnchor.MiddleCenter;

            buttonActiveStyle = new GUIStyle(buttonNormalStyle);
            buttonActiveStyle.normal.textColor = Color.yellow;
            buttonActiveStyle.normal.background = btnBgTexActive;

            headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.fontSize = 12;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.normal.textColor = new Color(0.4f, 0.8f, 1.0f, 0.9f);
            headerStyle.alignment = TextAnchor.MiddleCenter;
        }

        private Texture2D MakeSolidTex(Color col)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, col);
            tex.Apply();
            return tex;
        }

        private void OnGUI()
        {
            InitGUIStyles();

            // Calculate responsive top bar layout
            float scale = Mathf.Clamp(Screen.height / 1080f, 0.75f, 1.5f);
            int buttonWidth = Mathf.RoundToInt(115f * scale);
            int buttonHeight = Mathf.RoundToInt(34f * scale);
            int padding = Mathf.RoundToInt(6f * scale);

            int totalCount = 10; // Buttons 1 to 9 + 0 Overview
            int totalWidth = totalCount * buttonWidth + (totalCount - 1) * padding;
            int startX = (Screen.width - totalWidth) / 2;
            int startY = Mathf.RoundToInt(12f * scale);

            // Draw background panel for quick navigation bar
            Rect panelRect = new Rect(startX - 15, startY - 6, totalWidth + 30, buttonHeight + 28);
            GUI.DrawTexture(panelRect, panelBgTex);

            // Header label
            GUI.Label(new Rect(panelRect.x, panelRect.y + 2, panelRect.width, 18 * scale), "CELESTIAL QUICK NAVIGATION [PRESS 1 - 9]", headerStyle);

            int currentY = startY + Mathf.RoundToInt(18f * scale);

            // Draw buttons 1 to 9
            for (int i = 1; i <= 9; i++)
            {
                string bName = bodyOrder[i - 1];
                bool isSelected = (focusedBody != null && focusedBody.bodyName == bName);

                Rect btnRect = new Rect(startX + (i - 1) * (buttonWidth + padding), currentY, buttonWidth, buttonHeight);
                GUIStyle currentStyle = isSelected ? buttonActiveStyle : buttonNormalStyle;

                string labelText = $"{i}. {bName}";
                if (GUI.Button(btnRect, labelText, currentStyle))
                {
                    SelectByNumber(i);
                }
            }

            // Draw Overview Button (0)
            Rect overviewRect = new Rect(startX + 9 * (buttonWidth + padding), currentY, buttonWidth, buttonHeight);
            bool isOverview = (focusedBody == null);
            GUIStyle overviewStyle = isOverview ? buttonActiveStyle : buttonNormalStyle;

            if (GUI.Button(overviewRect, "0. Free Lock", overviewStyle))
            {
                DeselectToOverview();
            }
        }
    }
}
