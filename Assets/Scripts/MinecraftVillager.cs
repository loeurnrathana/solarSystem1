using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SolarSystemScope
{
    public class MinecraftVillager : MonoBehaviour
    {
        public static MinecraftVillager Instance { get; private set; }

        [Header("Target & Orbit Settings")]
        public Transform targetBody;
        public float orbitDistance = 6.5f;
        public float orbitSpeed = 15f;
        public float bobSpeed = 2f;
        public float bobHeight = 0.3f;

        [Header("Villager Visual Transforms")]
        private GameObject villagerRoot;
        private Transform headTransform;
        private Transform noseTransform;
        private Transform armsTransform;
        private Transform leftLegTransform;
        private Transform rightLegTransform;

        // Color Palette (Authentic Minecraft Villager Colors)
        private Color skinColor = new Color(0.78f, 0.55f, 0.42f);     // Villager Skin Tanned Tone (#C78C6B)
        private Color noseColor = new Color(0.72f, 0.48f, 0.36f);     // Darker Nose Tone (#B87A5C)
        private Color robeColor = new Color(0.42f, 0.26f, 0.18f);     // Brown Villager Robe (#6C422E)
        private Color robeDarkColor = new Color(0.32f, 0.19f, 0.13f); // Robe Border Trim (#523021)
        private Color eyeWhiteColor = Color.white;
        private Color eyeGreenColor = new Color(0.12f, 0.65f, 0.28f); // Emerald Green Villager Eyes
        private Color eyebrowColor = new Color(0.25f, 0.15f, 0.10f);  // Dark Unibrow
        private Color bootColor = new Color(0.15f, 0.12f, 0.10f);     // Dark Boots

        // Interactivity & Speech Bubble Variables
        private float speechTimer = 0f;
        private string currentSpeech = "";
        private GUIStyle speechStyle;
        private GUIStyle speechBoxStyle;
        private Texture2D speechBgTex;
        private float currentOrbitAngle = 0f;
        private float walkCycleAngle = 0f;

        private static readonly Dictionary<string, string[]> PlanetFacts = new Dictionary<string, string[]>()
        {
            { "Jupiter", new string[] {
                "Believe me, Jupiter is huge! It is a gas giant 318 times the mass of Earth, tremendous planet!",
                "Jupiter rotates super fast, 9.9 hours for a day, nobody does fast days like Jupiter, nobody!",
                "The Great Red Spot is a giant storm, bigger than Earth! It has been raging for centuries, incredible!",
                "Jupiter acts as a cosmic shield, using its massive gravity to protect us from comets, fantastic job!",
                "Jupiter has 95 confirmed moons, including volcanic Io and icy Europa, big league moons!"
            }},
            { "Saturn", new string[] {
                "Saturn has the most beautiful rings you have ever seen, billions of pieces of ice and rock, spectacular!",
                "Saturn's density is so low, it would float in water! Believe me, it floats, total winner!",
                "A day on Saturn is 10.7 hours and its year is 29.5 Earth years, long fantastic year!",
                "Saturn has 274 moons — the most in the entire solar system! Nobody has more moons than Saturn!",
                "Titan has liquid methane lakes and Enceladus has water geysers shooting into space, unbelievable!"
            }},
            { "Venus", new string[] {
                "Venus is the hottest planet in the solar system, 465 degrees Celsius! Nobody handles heat like Venus!",
                "Venus is named after the goddess of beauty, the brightest natural object in the night sky after the Moon, gorgeous!",
                "Venus rotates backwards! Its day is 243 Earth days long, longer than its year, incredible!",
                "Venus has a 96% carbon dioxide atmosphere with sulfuric acid clouds, total runaway greenhouse effect!",
                "Venus is Earth's twin in size, 12,104 kilometers wide, but has zero moons and 92 times Earth's pressure, tough planet!"
            }},
            { "Mercury", new string[] {
                "Mercury is the closest to the Sun and the fastest planet! 88 days for a full year, very fast, very strong!",
                "Mercury is named after the speedy messenger god, tremendous speed!",
                "Mercury has extreme temperatures, 430 degrees in the day and minus 180 at night, huge temperature swings!",
                "Mercury has an iron core that takes up 85% of its radius, massive iron core, tremendous structure!",
                "Mercury rotates super slowly, one day lasts 59 Earth days, very slow spin!"
            }},
            { "Mars", new string[] {
                "Mars is the Red Planet, covered in iron oxide, total rust! A fantastic world, very special!",
                "Mars has a 24.6 hour day, almost identical to Earth's day, great timing!",
                "Olympus Mons on Mars is the tallest volcano in the solar system — 3 times taller than Everest, huge!",
                "Mars has two potato-shaped moons, Phobos and Deimos, captured asteroids, great moons!",
                "Mars has a thin atmosphere and massive global dust storms that cover the entire planet, unbelievable!"
            }},
            { "Earth", new string[] {
                "Welcome to Earth, the greatest planet! 70% ocean, 30% land, protected by a 5-layer atmosphere!",
                "Earth has a mass of 5.97 times 10 to the 24th power kilograms, big heavy world!",
                "Earth orbits 150 million kilometers from the Sun, with a 24-hour day and 365-day year, perfect balance!",
                "Earth has liquid water and life — nobody does life better than Earth, believe me!"
            }},
            { "Uranus", new string[] {
                "Uranus is an Ice Giant named after the Greek god of the sky, fantastic planet!",
                "Uranus spins on a 97.8 degree tilt — it rolls around the Sun like a ball, very unique!",
                "Uranus is 1.8 billion miles from the Sun with a 17-hour day and 84-year orbit, very far!",
                "Uranus is the coldest planet in the solar system at minus 224 degrees, freezing cold!",
                "Uranus has 28 moons named after Shakespeare and Pope, very educated moons!"
            }},
            { "Neptune", new string[] {
                "Neptune is named after the god of the sea and is invisible to the naked eye, mysterious!",
                "Neptune was discovered by math before anyone even saw it with a telescope, very smart!",
                "Neptune has supersonic winds over 1,200 miles per hour — 9 times stronger than Earth's, powerful winds!",
                "Neptune is 30 astronomical units away, with a 16-hour day and 165-year orbit, huge orbit!",
                "Neptune has 16 moons! Its largest moon Triton circles backwards in a retrograde orbit, rogue moon!"
            }},
            { "Sun", new string[] {
                "The Sun contains 99.86% of all the mass in the entire solar system — massive, powerful star!",
                "The core of the Sun reaches an incredible 15 million degrees Celsius, extremely hot!",
                "Sunlight takes 8 minutes and 20 seconds to reach Earth, fast light!"
            }}
        };

        private static readonly string[] GenericVillagerQuotes = new string[]
        {
            "Welcome to this amazing planet in our solar system, tremendous world!",
            "Did you know? Nobody knows planetary science facts better than me, nobody!",
            "Exploring space is one of humanity's greatest achievements, big league!",
            "Look around at the landscape of this incredible planet, spectacular view!"
        };

        private int currentFactIndex = 0;
        private AudioSource villagerAudioSource;
        private AudioClip villagerHrmmClip;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            try
            {
                if (ttsWriter != null)
                {
                    ttsWriter.WriteLine("QUIT");
                }
                if (ttsServerProcess != null && !ttsServerProcess.HasExited)
                {
                    ttsServerProcess.Kill();
                }
            }
            catch {}
        }

        private static System.Diagnostics.Process ttsServerProcess = null;
        private static System.IO.StreamWriter ttsWriter = null;

        private static void EnsureTtsEngineRunning()
        {
            if (ttsServerProcess != null && !ttsServerProcess.HasExited && ttsWriter != null)
            {
                return;
            }

            try
            {
                string psScript = 
                    "$ErrorActionPreference = 'SilentlyContinue'; " +
                    "Add-Type -AssemblyName System.Speech; " +
                    "$s = New-Object System.Speech.Synthesis.SpeechSynthesizer; " +
                    "$v = $s.GetInstalledVoices() | Where-Object { $_.VoiceInfo.Name -like '*David*' -or $_.VoiceInfo.Gender -eq 'Male' } | Select-Object -First 1; " +
                    "if ($v) { $s.SelectVoice($v.VoiceInfo.Name) }; " +
                    "$s.Rate = 1; " +
                    "$s.Volume = 100; " +
                    "while ($true) { " +
                    "   $line = [Console]::In.ReadLine(); " +
                    "   if ([string]::IsNullOrEmpty($line) -or $line -eq 'QUIT') { break }; " +
                    "   $s.SpeakAsyncCancelAll(); " +
                    "   $s.SpeakAsync($line) | Out-Null; " +
                    "}";

                System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -Command \"{psScript}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                };

                ttsServerProcess = System.Diagnostics.Process.Start(psi);
                if (ttsServerProcess != null)
                {
                    ttsWriter = ttsServerProcess.StandardInput;
                    ttsWriter.AutoFlush = true;
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[TTS Engine Start Error] {ex.Message}");
            }
        }

        public void SpeakTextOutLoud(string textToSpeak)
        {
            if (string.IsNullOrEmpty(textToSpeak)) return;

            string spokenText = textToSpeak
                .Replace("—", ",")
                .Replace("°C", " degrees Celsius")
                .Replace("°F", " degrees Fahrenheit")
                .Replace("AU", " astronomical units")
                .Replace("km/h", " kilometers per hour")
                .Replace("mph", " miles per hour")
                .Replace("CO2", " carbon dioxide")
                .Replace("318x", " 318 times ")
                .Replace("95x", " 95 times ")
                .Replace("3x", " 3 times ");

            System.Threading.Thread ttsThread = new System.Threading.Thread(() =>
            {
                try
                {
                    EnsureTtsEngineRunning();
                    if (ttsWriter != null)
                    {
                        string cleanLine = spokenText.Replace("\r", " ").Replace("\n", " ").Trim();
                        ttsWriter.WriteLine(cleanLine);
                    }
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogError($"[TTS Speak Error] {ex.Message}");
                }
            });
            ttsThread.IsBackground = true;
            ttsThread.Start();
        }

        private bool isSurfaceVillager = false;

        private void Start()
        {
            BuildMinecraftVillagerModel();
            SetupVillagerAudio();
            if (!isSurfaceVillager)
            {
                SetVisible(false); // Start hidden in space mode
            }
        }

        private void SetupVillagerAudio()
        {
            villagerAudioSource = gameObject.AddComponent<AudioSource>();
            villagerAudioSource.spatialBlend = 0.5f;
            villagerAudioSource.playOnAwake = false;
            villagerAudioSource.minDistance = 2f;
            villagerAudioSource.maxDistance = 40f;

            int sampleRate = 44100;
            float duration = 0.45f;
            int sampleCount = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Sin(t / duration * Mathf.PI);
                float freq = Mathf.Lerp(220f, 290f, Mathf.Sin(t / duration * Mathf.PI));
                float voiceWave = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.6f 
                                + Mathf.Sin(4f * Mathf.PI * freq * t) * 0.25f 
                                + Mathf.Sin(6f * Mathf.PI * freq * t) * 0.15f;
                samples[i] = voiceWave * env * 0.5f;
            }

            villagerHrmmClip = AudioClip.Create("HumanSpeechVoice", sampleCount, 1, sampleRate, false);
            villagerHrmmClip.SetData(samples, 0);
        }

        private void PlayVillagerSound()
        {
            if (villagerAudioSource != null && villagerHrmmClip != null)
            {
                villagerAudioSource.pitch = Random.Range(0.88f, 1.08f);
                villagerAudioSource.PlayOneShot(villagerHrmmClip);
            }
        }

        public void SpawnOnSurface(string planetName, Transform playerTransform)
        {
            isSurfaceVillager = true;
            if (villagerRoot == null) BuildMinecraftVillagerModel();

            targetBody = playerTransform;
            if (playerTransform != null)
            {
                Vector3 spawnPos = playerTransform.position + playerTransform.forward * 4.0f + playerTransform.right * 2.0f;
                if (Physics.Raycast(new Vector3(spawnPos.x, spawnPos.y + 4.0f, spawnPos.z), Vector3.down, out RaycastHit hit, 10.0f))
                {
                    spawnPos.y = hit.point.y;
                }
                else
                {
                    spawnPos.y = playerTransform.position.y;
                }
                transform.position = spawnPos;
                transform.rotation = Quaternion.LookRotation(-playerTransform.forward);
            }

            SetVisible(true);
            ShowSpeechBubble($"Hrrrrm! Welcome to {planetName}!");
            Debug.Log($"<color=green>[Minecraft Villager] Landed on {planetName} surface!</color>");
        }

        public void OnPlanetFocused(CelestialBody body)
        {
            // Do not display in space view
            SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            if (villagerRoot != null)
            {
                villagerRoot.SetActive(visible);
            }
        }

        private void BuildMinecraftVillagerModel()
        {
            villagerRoot = new GameObject("VillagerModel");
            villagerRoot.transform.SetParent(transform, false);
            villagerRoot.transform.localPosition = Vector3.zero;
            villagerRoot.transform.localRotation = Quaternion.identity;
            villagerRoot.transform.localScale = Vector3.one; // Normal Villager Model Scale

            // Exact Colors from 1st Reference Image
            Color mcSkinUpper = new Color(0.72f, 0.49f, 0.36f);   // #B77D5C Upper Head Tan
            Color mcSkinLower = new Color(0.59f, 0.39f, 0.27f);   // #966245 Lower Face / Stubbing
            Color mcNose = new Color(0.62f, 0.40f, 0.29f);        // #9E674A Nose Skin
            Color mcRobe = new Color(0.36f, 0.23f, 0.16f);        // #5B3A29 Smooth Robe Brown
            Color mcRobeSeam = new Color(0.24f, 0.15f, 0.10f);    // #3D251A Dark Seam
            Color mcEyeWhite = Color.white;
            Color mcEyeGreen = new Color(0.12f, 0.62f, 0.23f);    // #1F9F3B Vibrant Minecraft Emerald
            Color mcEyebrow = new Color(0.16f, 0.11f, 0.07f);     // #281B10 Dark Unibrow
            Color mcBoots = new Color(0.21f, 0.17f, 0.15f);       // #362C27 Dark Boots

            Material skinUpperMat = CreateSolidColorMaterial(mcSkinUpper);
            Material skinLowerMat = CreateSolidColorMaterial(mcSkinLower);
            Material noseMat = CreateSolidColorMaterial(mcNose);
            Material robeMat = CreateSolidColorMaterial(mcRobe);
            Material robeSeamMat = CreateSolidColorMaterial(mcRobeSeam);
            Material eyeWhiteMat = CreateSolidColorMaterial(mcEyeWhite);
            Material eyeGreenMat = CreateSolidColorMaterial(mcEyeGreen);
            Material eyebrowMat = CreateSolidColorMaterial(mcEyebrow);
            Material bootMat = CreateSolidColorMaterial(mcBoots);

            // 1. TALL MINECRAFT HEAD (0.50f x 0.65f x 0.50f)
            GameObject headObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            headObj.name = "Villager_Head";
            headObj.transform.SetParent(villagerRoot.transform, false);
            headObj.transform.localPosition = new Vector3(0f, 1.625f, 0f);
            headObj.transform.localScale = new Vector3(0.50f, 0.65f, 0.50f);
            headObj.GetComponent<Renderer>().material = skinUpperMat;
            headTransform = headObj.transform;
            RemoveCollider(headObj);

            // Lower Face Stubbing Overlay
            GameObject lowerFaceObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lowerFaceObj.name = "Villager_LowerFaceStubbing";
            lowerFaceObj.transform.SetParent(headObj.transform, false);
            lowerFaceObj.transform.localPosition = new Vector3(0f, -0.25f, 0.001f);
            lowerFaceObj.transform.localScale = new Vector3(1.002f, 0.50f, 1.002f);
            lowerFaceObj.GetComponent<Renderer>().material = skinLowerMat;
            RemoveCollider(lowerFaceObj);

            // 2. UNIBROW BAR (Directly above eyes)
            GameObject eyebrowObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            eyebrowObj.name = "Villager_Unibrow";
            eyebrowObj.transform.SetParent(headObj.transform, false);
            eyebrowObj.transform.localPosition = new Vector3(0f, 0.08f, 0.51f);
            eyebrowObj.transform.localScale = new Vector3(1.02f, 0.12f, 0.05f);
            eyebrowObj.GetComponent<Renderer>().material = eyebrowMat;
            RemoveCollider(eyebrowObj);

            // 3. EYES (Left & Right - White outer, Green inner directly under unibrow)
            // Left Eye
            GameObject leftWhite = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftWhite.transform.SetParent(headObj.transform, false);
            leftWhite.transform.localPosition = new Vector3(-0.32f, -0.06f, 0.51f);
            leftWhite.transform.localScale = new Vector3(0.24f, 0.16f, 0.05f);
            leftWhite.GetComponent<Renderer>().material = eyeWhiteMat;
            RemoveCollider(leftWhite);

            GameObject leftGreen = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftGreen.transform.SetParent(headObj.transform, false);
            leftGreen.transform.localPosition = new Vector3(-0.10f, -0.06f, 0.512f);
            leftGreen.transform.localScale = new Vector3(0.20f, 0.16f, 0.05f);
            leftGreen.GetComponent<Renderer>().material = eyeGreenMat;
            RemoveCollider(leftGreen);

            // Right Eye
            GameObject rightGreen = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightGreen.transform.SetParent(headObj.transform, false);
            rightGreen.transform.localPosition = new Vector3(0.10f, -0.06f, 0.512f);
            rightGreen.transform.localScale = new Vector3(0.20f, 0.16f, 0.05f);
            rightGreen.GetComponent<Renderer>().material = eyeGreenMat;
            RemoveCollider(rightGreen);

            GameObject rightWhite = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightWhite.transform.SetParent(headObj.transform, false);
            rightWhite.transform.localPosition = new Vector3(0.32f, -0.06f, 0.51f);
            rightWhite.transform.localScale = new Vector3(0.24f, 0.16f, 0.05f);
            rightWhite.GetComponent<Renderer>().material = eyeWhiteMat;
            RemoveCollider(rightWhite);

            // 4. ICONIC PROTRUDING VILLAGER NOSE (Hangs down past chin)
            GameObject noseObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            noseObj.name = "Villager_Nose";
            noseObj.transform.SetParent(headObj.transform, false);
            noseObj.transform.localPosition = new Vector3(0f, -0.24f, 0.60f); // Positioned between eyes extending down
            noseObj.transform.localScale = new Vector3(0.26f, 0.48f, 0.32f);
            noseObj.GetComponent<Renderer>().material = noseMat;
            noseTransform = noseObj.transform;
            RemoveCollider(noseObj);

            // 5. TORSO & LONG ROBE COAT (0.52f x 1.15f x 0.40f)
            GameObject robeObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            robeObj.name = "Villager_OuterRobeCoat";
            robeObj.transform.SetParent(villagerRoot.transform, false);
            robeObj.transform.localPosition = new Vector3(0f, 0.70f, 0f);
            robeObj.transform.localScale = new Vector3(0.52f, 1.15f, 0.40f);
            robeObj.GetComponent<Renderer>().material = robeMat;
            RemoveCollider(robeObj);

            // Vertical Center Seam Line on Robe
            GameObject seamObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seamObj.name = "Villager_RobeSeam";
            seamObj.transform.SetParent(robeObj.transform, false);
            seamObj.transform.localPosition = new Vector3(0f, 0f, 0.501f);
            seamObj.transform.localScale = new Vector3(0.08f, 1.0f, 0.02f);
            seamObj.GetComponent<Renderer>().material = robeSeamMat;
            RemoveCollider(seamObj);

            // 6. FOLDED ARMS ACROSS CHEST (0.75f x 0.26f x 0.25f)
            GameObject armsObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            armsObj.name = "Villager_FoldedArms";
            armsObj.transform.SetParent(villagerRoot.transform, false);
            armsObj.transform.localPosition = new Vector3(0f, 0.95f, 0.14f);
            armsObj.transform.localScale = new Vector3(0.75f, 0.26f, 0.25f);
            armsObj.GetComponent<Renderer>().material = robeMat;
            armsTransform = armsObj.transform;
            RemoveCollider(armsObj);

            // Center Seam on Folded Arms
            GameObject armSeam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            armSeam.transform.SetParent(armsObj.transform, false);
            armSeam.transform.localPosition = new Vector3(0f, 0f, 0.501f);
            armSeam.transform.localScale = new Vector3(0.08f, 1.0f, 0.02f);
            armSeam.GetComponent<Renderer>().material = robeSeamMat;
            RemoveCollider(armSeam);

            // Tucked Skin Hands (Positioned underneath sleeve opening flush)
            GameObject handsObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            handsObj.name = "Villager_TuckedHandsUnder";
            handsObj.transform.SetParent(armsObj.transform, false);
            handsObj.transform.localPosition = new Vector3(0f, -0.42f, 0.05f);
            handsObj.transform.localScale = new Vector3(0.32f, 0.22f, 0.70f);
            handsObj.GetComponent<Renderer>().material = skinUpperMat;
            RemoveCollider(handsObj);

            // 7. LEGS & BOOTS
            GameObject leftLeg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftLeg.name = "Villager_LeftLeg";
            leftLeg.transform.SetParent(villagerRoot.transform, false);
            leftLeg.transform.localPosition = new Vector3(-0.13f, 0.18f, 0f);
            leftLeg.transform.localScale = new Vector3(0.22f, 0.36f, 0.24f);
            leftLeg.GetComponent<Renderer>().material = bootMat;
            leftLegTransform = leftLeg.transform;
            RemoveCollider(leftLeg);

            GameObject rightLeg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightLeg.name = "Villager_RightLeg";
            rightLeg.transform.SetParent(villagerRoot.transform, false);
            rightLeg.transform.localPosition = new Vector3(0.13f, 0.18f, 0f);
            rightLeg.transform.localScale = new Vector3(0.22f, 0.36f, 0.24f);
            rightLeg.GetComponent<Renderer>().material = bootMat;
            rightLegTransform = rightLeg.transform;
            RemoveCollider(rightLeg);

            // Add Capsule Collider for interaction
            CapsuleCollider col = gameObject.AddComponent<CapsuleCollider>();
            col.height = 2.0f;
            col.radius = 0.45f;
            col.center = new Vector3(0f, 1.0f, 0f);
        }

        private Texture2D CreateMinecraftVillagerFaceTexture()
        {
            int w = 64;
            int h = 64;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point; // Crisp Minecraft Pixel Art!
            tex.wrapMode = TextureWrapMode.Clamp;

            Color baseSkin = new Color(0.76f, 0.54f, 0.41f);
            Color shadowSkin = new Color(0.68f, 0.46f, 0.33f);
            Color eyebrowCol = new Color(0.16f, 0.10f, 0.07f);
            Color eyeWhite = Color.white;
            Color eyeGreen = new Color(0.15f, 0.62f, 0.25f);
            Color mouthCol = new Color(0.52f, 0.32f, 0.22f);

            Color[] pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = baseSkin;

            // Unibrow Bar (Row 36 to 41 across face)
            for (int x = 8; x <= 55; x++)
            {
                for (int y = 38; y <= 43; y++)
                {
                    pixels[y * w + x] = eyebrowCol;
                }
            }

            // Left Eye (White outer x 12-23, Green inner x 24-31, Row 27 to 37)
            for (int y = 27; y <= 37; y++)
            {
                for (int x = 12; x <= 23; x++) pixels[y * w + x] = eyeWhite;
                for (int x = 24; x <= 31; x++) pixels[y * w + x] = eyeGreen;
            }

            // Right Eye (Green inner x 32-39, White outer x 40-51, Row 27 to 37)
            for (int y = 27; y <= 37; y++)
            {
                for (int x = 32; x <= 39; x++) pixels[y * w + x] = eyeGreen;
                for (int x = 40; x <= 51; x++) pixels[y * w + x] = eyeWhite;
            }

            // Mouth / Chin Shading (Row 12 to 16)
            for (int x = 24; x <= 39; x++)
            {
                for (int y = 12; y <= 16; y++)
                {
                    pixels[y * w + x] = mouthCol;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private Texture2D CreateMinecraftVillagerRobeTexture()
        {
            int w = 32;
            int h = 32;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Repeat;

            Color baseRobe = new Color(0.36f, 0.22f, 0.14f);
            Color darkSeam = new Color(0.26f, 0.15f, 0.09f);

            Color[] pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool isSeam = (x % 8 == 0) || (y % 8 == 0);
                    pixels[y * w + x] = isSeam ? darkSeam : baseRobe;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private Texture2D CreateSolidColorTexture(Color col)
        {
            Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color[] pixels = new Color[16];
            for (int i = 0; i < 16; i++) pixels[i] = col;
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private Material CreateTextureMaterial(Texture2D tex)
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Unlit/Texture");
            Material mat = new Material(litShader);
            mat.mainTexture = tex;
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            return mat;
        }

        private float GetGroundHeightAt(Vector3 pos)
        {
            RaycastHit[] hits = Physics.RaycastAll(new Vector3(pos.x, pos.y + 4.0f, pos.z), Vector3.down, 10.0f);
            float maxHitY = -9999f;
            bool found = false;

            foreach (var h in hits)
            {
                if (h.transform == transform || h.transform.IsChildOf(transform)) continue;
                if (targetBody != null && (h.transform == targetBody || h.transform.IsChildOf(targetBody))) continue;

                if (h.point.y > maxHitY)
                {
                    maxHitY = h.point.y;
                    found = true;
                }
            }

            return found ? maxHitY : (targetBody != null ? targetBody.position.y : pos.y);
        }

        // Active Walking & Wander Variables
        private Vector3 wanderTargetPos;
        private float wanderTimer = 0f;
        private float nextWanderInterval = 3.0f;
        private Vector3 lastPos;
        private float actualSpeed = 0f;

        private void Update()
        {
            if (!isSurfaceVillager || villagerRoot == null || !villagerRoot.activeSelf) return;

            // 0. Fallback: Find astronaut player if targetBody is not set
            if (targetBody == null)
            {
                GameObject player = GameObject.Find("NASA_AstronautPlayer");
                if (player != null) targetBody = player.transform;
            }

            // 1. Calculate Active Follow Movement
            Vector3 moveDir = Vector3.zero;

            if (targetBody != null)
            {
                Vector3 toPlayer = targetBody.position - transform.position;
                toPlayer.y = 0f;
                float distToPlayer = toPlayer.magnitude;

                // Follow player immediately whenever distance > 2.5 meters
                if (distToPlayer > 2.5f)
                {
                    moveDir = toPlayer.normalized;
                    // Catch up faster (5.5 m/s) if player sprints away (> 7m)
                    float speed = (distToPlayer > 7.0f) ? 5.5f : 3.2f;

                    transform.position = Vector3.MoveTowards(transform.position, transform.position + moveDir, speed * Time.deltaTime);

                    if (moveDir.sqrMagnitude > 0.001f)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), Time.deltaTime * 8f);
                    }
                }
                else if (toPlayer.sqrMagnitude > 0.01f)
                {
                    // Stand near player & turn smoothly to face player
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toPlayer), Time.deltaTime * 5f);
                }
            }

            // Calculate actual movement speed for leg walking animation cycle
            actualSpeed = ((transform.position - lastPos).magnitude) / Mathf.Max(Time.deltaTime, 0.001f);
            lastPos = transform.position;

            // Ground Snap Check (Casts ray down to plant boots 100% flat on ground)
            Vector3 currentPos = transform.position;
            currentPos.y = GetGroundHeightAt(currentPos);
            transform.position = currentPos;

            // First Person Click Interaction (Looking directly at Villager & Left Click)
            if (IsLeftClickPressed())
            {
                Camera cam = Camera.main ?? Camera.current;
                if (cam != null)
                {
                    Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
                    if (Physics.Raycast(ray, out RaycastHit hitClick, 7.0f))
                    {
                        if (hitClick.transform == transform || hitClick.transform.IsChildOf(transform))
                        {
                            InteractWithVillager();
                        }
                    }
                }
            }

            // 2. Synchronized Leg & Body Walking Animations
            bool isMoving = actualSpeed > 0.3f;

            if (isMoving)
            {
                walkCycleAngle += Time.deltaTime * 9.5f;
            }
            else
            {
                // Smoothly return leg angle to standing upright
                walkCycleAngle = Mathf.LerpAngle(walkCycleAngle, 0f, Time.deltaTime * 8f);
            }
            
            // Nose Wobble & Head Sway
            if (headTransform != null)
            {
                float headSway = Mathf.Sin(Time.time * 2.5f) * (isMoving ? 8f : 3f);
                headTransform.localRotation = Quaternion.Euler(0f, headSway, 0f);
            }
            if (noseTransform != null)
            {
                float noseWobble = Mathf.Sin(Time.time * 4f) * (isMoving ? 6f : 2f);
                noseTransform.localRotation = Quaternion.Euler(noseWobble, 0f, 0f);
            }

            // Leg Walking Motion (Only swings when actively walking!)
            if (leftLegTransform != null && rightLegTransform != null)
            {
                float legAngle = isMoving ? Mathf.Sin(walkCycleAngle) * 28f : Mathf.LerpAngle(leftLegTransform.localRotation.eulerAngles.x, 0f, Time.deltaTime * 8f);
                leftLegTransform.localRotation = Quaternion.Euler(legAngle, 0f, 0f);
                rightLegTransform.localRotation = Quaternion.Euler(-legAngle, 0f, 0f);
            }

            // Left Click Raycast Detection (Works in both Locked FPS & Free Cursor modes, compatible with New Input System)
            if (IsLeftClickPressed())
            {
                Camera cam = Camera.main ?? Camera.current;
                if (cam != null)
                {
                    Ray ray = (Cursor.lockState == CursorLockMode.Locked) 
                        ? cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)) 
                        : cam.ScreenPointToRay(GetMousePositionSafe());

                    if (Physics.Raycast(ray, out RaycastHit hit, 15.0f))
                    {
                        if (hit.transform == transform || hit.transform.IsChildOf(transform))
                        {
                            InteractWithVillager();
                        }
                    }
                }
            }
        }

        private bool IsLeftClickPressed()
        {
            try
            {
                if (Input.GetMouseButtonDown(0)) return true;
            }
            catch {}
#if ENABLE_INPUT_SYSTEM
            try
            {
                if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
                    return true;
            }
            catch {}
#endif
            return false;
        }

        private Vector3 GetMousePositionSafe()
        {
            try
            {
                return Input.mousePosition;
            }
            catch {}
#if ENABLE_INPUT_SYSTEM
            try
            {
                if (UnityEngine.InputSystem.Mouse.current != null)
                {
                    Vector2 pos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                    return new Vector3(pos.x, pos.y, 0f);
                }
            }
            catch {}
#endif
            return Vector3.zero;
        }

        private float lastInteractTime = 0f;

        public void InteractWithVillager()
        {
            // Debounce: speak EXACTLY ONE TIME per click
            if (Time.time - lastInteractTime < 0.4f) return;
            lastInteractTime = Time.time;

            string planetName = "";
            if (PlanetSurfaceExplorer.Instance != null && !string.IsNullOrEmpty(PlanetSurfaceExplorer.Instance.CurrentPlanetName))
            {
                planetName = PlanetSurfaceExplorer.Instance.CurrentPlanetName;
            }
            else if (targetBody != null)
            {
                planetName = targetBody.name;
            }

            string fact = GetPlanetFact(planetName);
            ShowSpeechBubble(fact, 6.0f);
            SpawnEmeraldParticleEffect();
            PlayVillagerSound();
            SpeakTextOutLoud(fact);
            Debug.Log($"<color=green>[Explorer Guide] Speaking aloud about {planetName}: {fact}</color>");
        }

        private string GetPlanetFact(string planetName)
        {
            if (!string.IsNullOrEmpty(planetName) && PlanetFacts.ContainsKey(planetName))
            {
                string[] facts = PlanetFacts[planetName];
                string fact = facts[currentFactIndex % facts.Length];
                currentFactIndex++;
                return fact;
            }
            return GenericVillagerQuotes[Random.Range(0, GenericVillagerQuotes.Length)];
        }

        public void ShowSpeechBubble(string text, float duration = 99999f)
        {
            currentSpeech = text;
            speechTimer = 99999f;
        }

        private void SpawnEmeraldParticleEffect()
        {
            GameObject fxObj = new GameObject("EmeraldBurstFX");
            fxObj.transform.SetParent(transform, true);
            fxObj.transform.position = transform.position + Vector3.up * 2.0f;

            ParticleSystem ps = fxObj.AddComponent<ParticleSystem>();
            ParticleSystemRenderer psr = fxObj.GetComponent<ParticleSystemRenderer>();
            if (psr != null)
            {
                Shader pShader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                              ?? Shader.Find("Particles/Standard Unlit")
                              ?? Shader.Find("Sprites/Default")
                              ?? Shader.Find("Unlit/Color");
                if (pShader != null)
                {
                    Material pMat = new Material(pShader);
                    Color emeraldCol = new Color(0.1f, 0.95f, 0.35f, 0.95f);
                    if (pMat.HasProperty("_BaseColor")) pMat.SetColor("_BaseColor", emeraldCol);
                    if (pMat.HasProperty("_Color")) pMat.SetColor("_Color", emeraldCol);
                    if (pMat.HasProperty("_TintColor")) pMat.SetColor("_TintColor", emeraldCol);
                    psr.material = pMat;
                }
            }

            var main = ps.main;
            main.startLifetime = 1.2f;
            main.startSpeed = 3.5f;
            main.startSize = 0.35f;
            main.startColor = new Color(0.1f, 0.95f, 0.35f, 1.0f);
            main.maxParticles = 40;

            var emit = ps.emission;
            emit.rateOverTime = 0;
            ps.Emit(30);

            Destroy(fxObj, 1.5f);
        }

        private void OnGUI()
        {
            if (string.IsNullOrEmpty(currentSpeech) || villagerRoot == null || !villagerRoot.activeSelf) return;
            
            Camera cam = Camera.main ?? Camera.current;
#pragma warning disable 0618
            if (cam == null) cam = Object.FindObjectOfType<Camera>();
#pragma warning restore 0618
            if (cam == null) return;

            Vector3 worldPos = transform.position + Vector3.up * 2.5f;
            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

            if (screenPos.z > 0.1f)
            {
                // Dynamic resolution scale factor (scales up for 1080p, 1440p, 4K UHD)
                float resScale = Mathf.Clamp(Screen.height / 720f, 1.2f, 5.0f);

                if (speechStyle == null || speechBgTex == null)
                {
                    speechBgTex = new Texture2D(1, 1);
                    speechBgTex.SetPixel(0, 0, new Color(0.04f, 0.14f, 0.07f, 0.95f)); // Emerald dark tint
                    speechBgTex.Apply();

                    speechBoxStyle = new GUIStyle();
                    speechBoxStyle.normal.background = speechBgTex;

                    speechStyle = new GUIStyle();
                    speechStyle.fontStyle = FontStyle.Bold;
                    speechStyle.normal.textColor = new Color(0.35f, 1.0f, 0.45f); // Emerald Green Text
                    speechStyle.alignment = TextAnchor.MiddleCenter;
                    speechStyle.wordWrap = true;
                }

                speechStyle.fontSize = Mathf.RoundToInt(34f * resScale);

                float guiX = screenPos.x;
                float guiY = Screen.height - screenPos.y;

                float boxW = Mathf.Min(820f * resScale, Screen.width * 0.92f);
                float boxH = 150f * resScale;
                Rect boxRect = new Rect(guiX - (boxW * 0.5f), guiY - boxH, boxW, boxH);

                GUI.Box(boxRect, "", speechBoxStyle);
                GUI.Label(new Rect(boxRect.x + (20f * resScale), boxRect.y + (12f * resScale), boxRect.width - (40f * resScale), boxRect.height - (24f * resScale)), currentSpeech, speechStyle);
            }
        }

        private Material CreateSolidColorMaterial(Color col)
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
            Material mat = new Material(litShader);
            mat.color = col;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", col);
            return mat;
        }

        private void RemoveCollider(GameObject obj)
        {
            Collider col = obj.GetComponent<Collider>();
            if (col != null) Destroy(col);
        }
    }
}
