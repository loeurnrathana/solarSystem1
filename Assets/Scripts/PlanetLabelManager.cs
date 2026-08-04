using UnityEngine;
using System.Collections.Generic;

namespace SolarSystemScope
{
    public class PlanetLabelManager : MonoBehaviour
    {
        public static PlanetLabelManager Instance { get; private set; }

        private static readonly Dictionary<string, string> Descriptions = new Dictionary<string, string>()
        {
            { "Sun", "The Sun is the star at the center of the Solar System. It is a nearly perfect sphere of hot plasma, heated by nuclear fusion reactions in its core, containing 99.86% of the total mass of the Solar System." },
            { "Mercury", "Mercury is the smallest planet and closest to the Sun — only a little bigger than our Moon. If you stood on Mercury, the Sun would look 3 times bigger and shine 7 times brighter than it does on Earth!" },
            { "Venus", "Venus is the second planet from the Sun, and our closest planetary neighbor. It's the hottest planet in our solar system, with a crushing atmosphere of carbon dioxide and sulfuric acid clouds." },
            { "Earth", "Earth – our home planet – is the third planet from the Sun, and the fifth largest planet. It's the only place we know of inhabited by living things, covered by 71% liquid water oceans." },
            { "Moon", "The Moon is Earth's only natural satellite, orbiting at 384,400 km and tidally locked with Earth. It causes ocean tides and stabilizes Earth's axial tilt." },
            { "Mars", "Mars is the fourth planet from the Sun — a cold, dusty desert world often called the \"Red Planet.\" It has seasons, icy poles, old volcanoes like Olympus Mons, and giant canyons like Valles Marineris." },
            { "Phobos", "Phobos is the larger inner moon of Mars. It orbits Mars in just 7 hours and 39 minutes, closer to its parent planet than any other moon in the solar system, doomed to crash into Mars in 30-50 million years." },
            { "Deimos", "Deimos is the smaller outer moon of Mars. Covered in a thick layer of reddish dust, it takes 30 hours to orbit Mars and resembles a lumpy, dark asteroid captured from the asteroid belt." },
            { "Jupiter", "Jupiter is the biggest planet — 1,000 Earths could fit inside it! It's also the oldest planet, spinning faster than any other and harboring the iconic Great Red Spot storm." },
            { "Io", "Io is Jupiter's innermost Galilean moon and the most volcanically active body in the entire Solar System, with hundreds of active volcanoes spewing sulfur plumes up to 500 km high." },
            { "Europa", "Europa is Jupiter's ice-covered moon hiding a vast global liquid ocean beneath its frozen crust containing twice as much water as all of Earth's oceans combined. It is a prime candidate in the search for alien life." },
            { "Ganymede", "Ganymede is the largest moon in the Solar System — even bigger than Mercury and Pluto! It is the only moon known to possess its own internally generated magnetic field." },
            { "Callisto", "Callisto is Jupiter's second-largest moon and the most heavily cratered object in the Solar System. Its ancient surface of ice and rock has remained virtually unchanged for 4 billion years." },
            { "Saturn", "Saturn is the sixth planet from the Sun, famous for its magnificent system of icy rings spanning up to 282,000 km across." },
            { "Titan", "Titan is Saturn's largest moon and the only moon in the solar system with a dense atmosphere (rich in nitrogen and methane) and stable liquid lakes and seas of liquid ethane and methane." },
            { "Enceladus", "Enceladus is Saturn's brilliant white ice moon with giant cryovolcanic geysers erupting water ice and organic compounds hundreds of kilometers into space from subsurface oceans." },
            { "Rhea", "Rhea is Saturn's second-largest moon, an icy, heavily cratered body that may possess a faint ring system of its own." },
            { "Mimas", "Mimas is Saturn's icy moon famous for its giant 130-km Herschel impact crater, giving it an uncanny resemblance to the Death Star!" },
            { "Uranus", "Uranus is the seventh planet from the Sun and spins on its side at a 98-degree tilt like a rolling ball, causing extreme 42-year long seasons!" },
            { "Miranda", "Miranda is Uranus's bizarre moon featuring extreme chaotic terrain with 20-km deep canyons and giant chevron-shaped ice cliffs." },
            { "Ariel", "Ariel is Uranus's brightest moon, crisscrossed by giant fault canyons and smooth valleys smoothed by ancient ice volcanism." },
            { "Umbriel", "Umbriel is Uranus's darkest moon, covered in ancient dark impact craters with a mysterious bright ring of ice called Wunda crater." },
            { "Titania", "Titania is Uranus's largest moon, dramatic for its massive fault valleys and giant ice chasms scaling over 1,500 km long." },
            { "Oberon", "Oberon is the outermost major moon of Uranus, heavily cratered with dark carbon-rich floors inside its giant impact basins." },
            { "Neptune", "Neptune is the eighth and most distant major planet, a deep azure ice giant featuring the fastest winds in the solar system reaching 2,100 km/h!" },
            { "Triton", "Triton is Neptune's largest moon, orbiting backwards (retrograde) relative to Neptune's rotation. It features active cryovolcanic nitrogen geysers and a thin nitrogen atmosphere." },
            { "Pluto", "Pluto is a dwarf planet in the Kuiper belt featuring the heart-shaped Tombaugh Regio glacier of nitrogen ice, dark tholin plains, and methane ice mountains." },
            { "Charon", "Charon is Pluto's massive moon, so large relative to Pluto that the two orbit a common center of gravity outside Pluto, forming a true double dwarf planet system." }
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
                if (focusedBody == null && body.orbitCenter != null && body.orbitCenter.name != "Sun")
                {
                    continue;
                }

                // Sub-moons only show when their parent planet is focused
                if (focusedBody != null && body.orbitCenter != null && body.orbitCenter.name != "Sun" && body.orbitCenter != focusedBody.transform)
                {
                    continue;
                }

                Vector3 bodyPos = body.transform.position;
                float planetScale = body.transform.lossyScale.y;

                // Ray-Sphere Occlusion Check against the Focused Planet
                if (focusedBody != null && body != focusedBody)
                {
                    Vector3 toBody = bodyPos - camPos;
                    float distToBody = toBody.magnitude;
                    Vector3 rayDir = toBody / distToBody;

                    Vector3 focusedPos = focusedBody.transform.position;
                    float distToFocused = Vector3.Distance(camPos, focusedPos);

                    // If body is behind the focused planet
                    if (distToBody > distToFocused)
                    {
                        Vector3 camToFocused = focusedPos - camPos;
                        float proj = Vector3.Dot(camToFocused, rayDir);

                        if (proj > 0.1f)
                        {
                            Vector3 closestPoint = camPos + rayDir * proj;
                            float distRay = Vector3.Distance(closestPoint, focusedPos);
                            float effectiveRadius = focusedBody.transform.lossyScale.y * 1.25f;
                            if (distRay < effectiveRadius)
                            {
                                continue; // Occluded behind focused planet!
                            }
                        }
                    }
                }

                // Ray-Sphere Occlusion Check against the Sun
                if (body.bodyName != "Sun" && sunTransform != null)
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

                    // Hide floating text labels when they fall inside the top UI navigation bar region (< 65px from top)
                    if (guiY < 65f)
                    {
                        continue;
                    }

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

                    // Draw Floating Planet Name Label (when not currently focused)
                    if (!isFocused)
                    {
                        Rect nameRect = new Rect(guiX + pixelOffsetX - 300f, guiY + pixelOffsetY - 25f, 600f, 50f);
                        
                        // Shadow text for ultra-crisp readability
                        GUI.Label(new Rect(nameRect.x + 2f, nameRect.y + 2f, nameRect.width, nameRect.height), body.bodyName, nameShadowStyle);
                        GUI.Label(nameRect, body.bodyName, nameStyle);
                    }
                }
            }

            // 2. Render Floating Info Card Panel right next to the focused planet or moon in screen space!
            if (focusedBody != null)
            {
                Vector3 bodyPos = focusedBody.transform.position;
                Vector3 bodyScreenPos = mainCam.WorldToScreenPoint(bodyPos);

                if (bodyScreenPos.z > 0.1f)
                {
                    float distToCam = Vector3.Distance(mainCam.transform.position, bodyPos);
                    float scaleY = focusedBody.transform.lossyScale.y;

                    // Calculate on-screen radius of planet sphere
                    float planetRadiusPx = (scaleY * 0.5f / Mathf.Max(distToCam, 1f)) * Screen.height * 0.85f;
                    planetRadiusPx = Mathf.Clamp(planetRadiusPx, 45f, 250f);

                    float cardWidth = 460f;
                    float cardHeight = 360f;

                    // Anchor panel right next to planet's right edge
                    float cardX = bodyScreenPos.x + planetRadiusPx + 25f;

                    // If panel exceeds right screen edge, anchor to the left edge of planet instead!
                    if (cardX + cardWidth > Screen.width - 20f)
                    {
                        cardX = bodyScreenPos.x - planetRadiusPx - cardWidth - 25f;
                    }

                    // Keep panel within screen bounds
                    cardX = Mathf.Clamp(cardX, 20f, Screen.width - cardWidth - 20f);

                    float cardY = (Screen.height - bodyScreenPos.y) - (cardHeight * 0.5f);
                    cardY = Mathf.Clamp(cardY, 70f, Screen.height - cardHeight - 20f);

                    Rect cardRect = new Rect(cardX, cardY, cardWidth, cardHeight);

                    // Draw background dark glass panel
                    GUI.DrawTexture(cardRect, boxBackgroundTex);

                    // Header & Body Info
                    string descTextStr = "";
                    if (Descriptions.TryGetValue(focusedBody.bodyName, out string dStr))
                    {
                        descTextStr = dStr;
                    }
                    else if (!string.IsNullOrEmpty(focusedBody.description))
                    {
                        descTextStr = focusedBody.description;
                    }
                    else
                    {
                        descTextStr = $"{focusedBody.bodyName} is a celestial body in the Solar System.";
                    }

                    string formattedText = $"<size=32><color=#00E5FF><b>{focusedBody.bodyName.ToUpper()}</b></color></size>\n";
                    if (focusedBody.orbitCenter != null && focusedBody.orbitCenter.name != "Sun")
                    {
                        formattedText += $"<size=18><color=#FFD700><b>Natural Satellite of {focusedBody.orbitCenter.name}</b></color></size>\n";
                    }
                    formattedText += "\n";
                    if (focusedBody.radiusKm > 0)
                    {
                        formattedText += $"<size=17><color=#AAAAAA><b>Diameter:</b></color> {focusedBody.radiusKm * 2f:N0} km\n";
                        formattedText += $"<color=#AAAAAA><b>Surface Temp:</b></color> {focusedBody.surfaceTemp}\n";
                        formattedText += $"<color=#AAAAAA><b>Orbital Period:</b></color> {focusedBody.orbitalPeriod}</size>\n\n";
                    }
                    formattedText += $"<size=18>{descTextStr}</size>";

                    Rect paddingRect = new Rect(cardRect.x + 24f, cardRect.y + 20f, cardRect.width - 48f, cardRect.height - 40f);
                    GUI.Label(paddingRect, formattedText, descStyle);
                }
            }
        }
    }
}
