using UnityEngine;
using System.Collections.Generic;

namespace SolarSystemScope
{
    public class PlanetLabelManager : MonoBehaviour
    {
        public static PlanetLabelManager Instance { get; private set; }

        private static readonly Dictionary<string, string> Descriptions = new Dictionary<string, string>()
        {
            { "Sun", "The Sun is the star at the center of the Solar System. It is a nearly perfect sphere of hot plasma, heated by nuclear fusion reactions in its core." },
            { "Mercury", "Mercury is the smallest planet and closest to the Sun — only a little bigger than our Moon. If you stood on Mercury, the Sun would look 3 times bigger and shine 7 times brighter than it does on Earth!" },
            { "Venus", "Venus is the second planet from the Sun, and our closest planetary neighbor. It's the hottest planet in our solar system, and is sometimes called Earth's twin." },
            { "Earth", "Earth – our home planet – is the third planet from the Sun, and the fifth largest planet. It's the only place we know of inhabited by living things." },
            { "Moon", "The Moon is Earth's only natural satellite, orbiting at 384,400 km and tidally locked with Earth." },
            { "Mars", "Mars is the fourth planet from the Sun — a cold, dusty desert world often called the \"Red Planet.\" It has seasons, icy poles, old volcanoes, and giant canyons." },
            { "Phobos", "Phobos is the larger inner moon of Mars, orbiting faster than Mars rotates." },
            { "Deimos", "Deimos is the smaller outer moon of Mars, covered in dark reddish dust." },
            { "Jupiter", "Jupiter is the biggest planet — 1,000 Earths could fit inside it! It's also the oldest planet, and it spins faster than any other, finishing one spin in just 9.9 hours." },
            { "Io", "Io is Jupiter's innermost Galilean moon and the most volcanically active body in the Solar System." },
            { "Europa", "Europa is Jupiter's ice-covered moon hiding a vast liquid ocean beneath its surface." },
            { "Ganymede", "Ganymede is Jupiter's moon and the largest natural satellite in the Solar System." },
            { "Callisto", "Callisto is Jupiter's heavily cratered ancient ice moon." },
            { "Saturn", "Saturn is the sixth planet from the Sun, and the second-largest planet in our solar system." },
            { "Titan", "Titan is Saturn's largest moon with a thick nitrogen atmosphere and liquid methane lakes." },
            { "Uranus", "Uranus is the seventh planet from the Sun and the third biggest one. It's a bit weird because it spins on its side, like a rolling ball instead of a spinning top!" },
            { "Neptune", "Neptune is the eighth and most distant planet in our solar system." },
            { "Triton", "Triton is Neptune's largest moon with cryovolcanic nitrogen geysers and a retrograde orbit." }
        };

        private List<CelestialBody> celestialBodies = new List<CelestialBody>();
        private Camera mainCam;
        private CelestialBody focusedBody = null;

        private GUIStyle nameStyle;
        private GUIStyle nameShadowStyle;
        private GUIStyle boxStyle;
        private GUIStyle descStyle;
        private Texture2D boxBackgroundTex;

        private void Awake()
        {
            Instance = this;
        }

        public void SetFocusedBody(CelestialBody body)
        {
            if (focusedBody == body) return;
            focusedBody = body;

            if (MinecraftVillager.Instance != null)
            {
                MinecraftVillager.Instance.OnPlanetFocused(body);
            }
        }

        public void Initialize(List<CelestialBody> bodies)
        {
            Instance = this;
            mainCam = Camera.main;
            celestialBodies = bodies ?? new List<CelestialBody>();
            focusedBody = null; // Start with NO focused planet (full solar system view)
            InitGUIStyles();
        }

        private void InitGUIStyles()
        {
            if (nameStyle != null && boxBackgroundTex != null) return;

            nameStyle = new GUIStyle();
            nameStyle.fontSize = 28;
            nameStyle.fontStyle = FontStyle.Bold;
            nameStyle.normal.textColor = Color.white;
            nameStyle.alignment = TextAnchor.MiddleCenter;

            nameShadowStyle = new GUIStyle(nameStyle);
            nameShadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.95f);

            // Create solid/semi-transparent background texture for description box
            boxBackgroundTex = new Texture2D(1, 1);
            boxBackgroundTex.SetPixel(0, 0, new Color(0.04f, 0.08f, 0.16f, 0.94f));
            boxBackgroundTex.Apply();

            boxStyle = new GUIStyle();
            boxStyle.normal.background = boxBackgroundTex;

            descStyle = new GUIStyle();
            descStyle.fontSize = 24;
            descStyle.normal.textColor = Color.white;
            descStyle.alignment = TextAnchor.UpperLeft;
            descStyle.wordWrap = true;
            descStyle.richText = true;
        }

        private void Update()
        {
            // Instantly clear description card in 0s with zero delay when E is pressed
#if ENABLE_INPUT_SYSTEM
            bool ePressed = (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame);
#else
            bool ePressed = Input.GetKeyDown(KeyCode.E);
#endif
            if (ePressed)
            {
                focusedBody = null;
            }
        }

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint) return;
            if (mainCam == null) mainCam = Camera.main;
            if (mainCam == null) return;

            // Ensure CelestialBodies list is populated if null
            if (celestialBodies == null || celestialBodies.Count == 0)
            {
#pragma warning disable 0618
                CelestialBody[] foundBodies = Object.FindObjectsOfType<CelestialBody>();
#pragma warning restore 0618
                if (foundBodies != null && foundBodies.Length > 0)
                {
                    celestialBodies = new List<CelestialBody>(foundBodies);
                }
                else return;
            }

            InitGUIStyles();

            // Find Sun transform for occlusion check
            Transform sunTransform = null;
            float sunRadius = 18f;
            foreach (var b in celestialBodies)
            {
                if (b != null && b.bodyName == "Sun")
                {
                    sunTransform = b.transform;
                    sunRadius = sunTransform.lossyScale.y * 0.5f;
                    break;
                }
            }

            Vector3 camPos = mainCam.transform.position;

            foreach (var body in celestialBodies)
            {
                if (body == null) continue;

                // Skip Asteroid Belt from displaying floating name labels
                if (body.bodyName.Contains("Asteroid") || body.bodyName.Contains("Belt"))
                {
                    continue;
                }

                bool isFocused = (body == focusedBody);

                // Sub-moons (orbitCenter != Sun) do NOT show floating names in full solar system overview
                if (!isFocused && body.orbitCenter != null && body.orbitCenter.name != "Sun")
                {
                    continue;
                }

                Vector3 bodyPos = body.transform.position;
                float planetScale = body.transform.lossyScale.y;

                // Ray-Sphere Occlusion Check against the Sun (skip check if body is focused or is Sun itself)
                if (!isFocused && body.bodyName != "Sun" && sunTransform != null)
                {
                    Vector3 toBody = bodyPos - camPos;
                    float distToBody = toBody.magnitude;
                    Vector3 rayDir = toBody / distToBody;

                    Vector3 camToSun = sunTransform.position - camPos;
                    float proj = Vector3.Dot(camToSun, rayDir);

                    if (proj > 0.5f && proj < distToBody - 2.0f)
                    {
                        Vector3 closestPoint = camPos + rayDir * proj;
                        float distToSunRay = Vector3.Distance(closestPoint, sunTransform.position);
                        if (distToSunRay < sunRadius * 1.05f)
                        {
                            continue; // Occluded behind Sun
                        }
                    }
                }

                // Calculate Name Position
                Vector3 bodyScreenPos = mainCam.WorldToScreenPoint(bodyPos);

                // Check if in front of camera lens
                if (bodyScreenPos.z > 0.1f)
                {
                    float guiX = bodyScreenPos.x;
                    float guiY = Screen.height - bodyScreenPos.y;

                    float pixelOffsetY = -24f; // Default above planet
                    float pixelOffsetX = 0f;

                    if (body.bodyName == "Sun")
                    {
                        pixelOffsetY = 24f; // Below Sun
                    }
                    else if (body.bodyName == "Earth" || body.bodyName == "Neptune")
                    {
                        pixelOffsetY = 26f; // Below Earth and Neptune
                    }
                    else if (body.bodyName == "Venus")
                    {
                        pixelOffsetX = 35f; // To the right of Venus
                        pixelOffsetY = -5f;
                    }
                    else if (body.bodyName == "Saturn")
                    {
                        pixelOffsetX = -40f; // Left of Saturn
                        pixelOffsetY = -25f;
                    }
                    else if (body.bodyName == "Uranus")
                    {
                        pixelOffsetX = 25f; // Top-Right of Uranus
                        pixelOffsetY = -25f;
                    }

                    // Draw Floating Planet Name Label
                    Rect nameRect = new Rect(guiX + pixelOffsetX - 300f, guiY + pixelOffsetY - 25f, 600f, 50f);
                    
                    // Shadow text for ultra-crisp readability
                    GUI.Label(new Rect(nameRect.x + 2f, nameRect.y + 2f, nameRect.width, nameRect.height), body.bodyName, nameShadowStyle);
                    GUI.Label(nameRect, body.bodyName, nameStyle);

                    // 2. Draw Giant Planet Description Panel Next to Planet if Focused (Matching Red Outline Area)
                    if (isFocused)
                    {
                        Vector3 rightOffset = mainCam.transform.right * (planetScale * 0.65f + 1.2f);
                        Vector3 descWorldPos = bodyPos + rightOffset - mainCam.transform.up * (planetScale * 0.1f);
                        Vector3 descScreenPos = mainCam.WorldToScreenPoint(descWorldPos);

                        float boxWidth = 680f;
                        float boxHeight = 440f;

                        float descGuiX = descScreenPos.x + 30f;
                        float descGuiY = Screen.height - descScreenPos.y - (boxHeight * 0.5f);

                        // Clamp box within screen boundaries
                        descGuiX = Mathf.Clamp(descGuiX, 20f, Screen.width - (boxWidth + 20f));
                        descGuiY = Mathf.Clamp(descGuiY, 20f, Screen.height - (boxHeight + 20f));

                        Rect descBoxRect = new Rect(descGuiX, descGuiY, boxWidth, boxHeight);

                        // Background panel box
                        GUI.Box(descBoxRect, "", boxStyle);

                        // Formatted Text Content
                        string descTextStr = "";
                        if (Descriptions.TryGetValue(body.bodyName, out string dStr))
                        {
                            descTextStr = dStr;
                        }
                        else if (!string.IsNullOrEmpty(body.description))
                        {
                            descTextStr = body.description;
                        }
                        else
                        {
                            descTextStr = $"{body.bodyName} is a celestial body in the Solar System.";
                        }

                        string formattedText = $"<size=40><color=#00E5FF><b>{body.bodyName.ToUpper()}</b></color></size>\n\n";
                        if (body.radiusKm > 0) formattedText += $"<size=23><color=#CCCCCC><b>Diameter:</b></color> {body.radiusKm * 2f:N0} km   |   <color=#CCCCCC><b>Temp:</b></color> {body.surfaceTemp}\n<color=#CCCCCC><b>Orbit Period:</b></color> {body.orbitalPeriod}</size>\n\n";
                        formattedText += $"<size=24>{descTextStr}</size>";

                        Rect textPaddingRect = new Rect(descBoxRect.x + 32f, descBoxRect.y + 30f, descBoxRect.width - 64f, descBoxRect.height - 60f);
                        GUI.Label(textPaddingRect, formattedText, descStyle);
                    }
                }
            }
        }
    }
}
