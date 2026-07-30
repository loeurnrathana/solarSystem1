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

        private static readonly string[] VillagerQuotes = new string[]
        {
            "Hrrrrm! (Trade: 1 Emerald = 1 Earth)",
            "Hrmm... Is Mars made of Redstone?",
            "Hrrr! The Sun is shining 7 times brighter!",
            "Hrmm... 64 Emeralds for a Rocket!",
            "Hrrrrm! Beautiful Solar System!",
            "Hrmm? Where is the Village?",
            "Hrrr! Saturn's rings are made of ice!"
        };

        private void Awake()
        {
            Instance = this;
        }

        private bool isSurfaceVillager = false;

        private void Start()
        {
            BuildMinecraftVillagerModel();
            if (!isSurfaceVillager)
            {
                SetVisible(false); // Start hidden in space mode
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
            if (Input.GetMouseButtonDown(0))
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

            // Speech Bubble Timer
            if (speechTimer > 0f)
            {
                speechTimer -= Time.deltaTime;
            }
        }

        private void OnMouseDown()
        {
            InteractWithVillager();
        }

        public void InteractWithVillager()
        {
            string randomQuote = VillagerQuotes[Random.Range(0, VillagerQuotes.Length)];
            ShowSpeechBubble(randomQuote);
            SpawnEmeraldParticleEffect();
            Debug.Log("<color=green>[Minecraft Villager] HRRRRM!</color>");
        }

        public void ShowSpeechBubble(string text, float duration = 4.5f)
        {
            currentSpeech = text;
            speechTimer = duration;
        }

        private void SpawnEmeraldParticleEffect()
        {
            GameObject fxObj = new GameObject("EmeraldBurstFX");
            fxObj.transform.position = transform.position + Vector3.up * 2.0f;

            ParticleSystem ps = fxObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 1.2f;
            main.startSpeed = 3.5f;
            main.startSize = 0.35f;
            main.startColor = new Color(0.1f, 0.9f, 0.3f);
            main.maxParticles = 40;

            var emit = ps.emission;
            emit.rateOverTime = 0;
            ps.Emit(30);

            Destroy(fxObj, 1.5f);
        }

        private void OnGUI()
        {
            if (speechTimer <= 0f || string.IsNullOrEmpty(currentSpeech) || villagerRoot == null || !villagerRoot.activeSelf) return;
            
            Camera cam = Camera.main ?? Camera.current;
#pragma warning disable 0618
            if (cam == null) cam = Object.FindObjectOfType<Camera>();
#pragma warning restore 0618
            if (cam == null) return;

            Vector3 worldPos = transform.position + Vector3.up * 2.4f;
            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

            if (screenPos.z > 0.1f)
            {
                if (speechStyle == null || speechBgTex == null)
                {
                    speechBgTex = new Texture2D(1, 1);
                    speechBgTex.SetPixel(0, 0, new Color(0.06f, 0.12f, 0.08f, 0.92f)); // Emerald dark tint
                    speechBgTex.Apply();

                    speechBoxStyle = new GUIStyle();
                    speechBoxStyle.normal.background = speechBgTex;

                    speechStyle = new GUIStyle();
                    speechStyle.fontSize = 20;
                    speechStyle.fontStyle = FontStyle.Bold;
                    speechStyle.normal.textColor = new Color(0.3f, 1.0f, 0.4f); // Emerald Green Text
                    speechStyle.alignment = TextAnchor.MiddleCenter;
                    speechStyle.wordWrap = true;
                }

                float guiX = screenPos.x;
                float guiY = Screen.height - screenPos.y;

                float boxW = 380f;
                float boxH = 65f;
                Rect boxRect = new Rect(guiX - (boxW * 0.5f), guiY - boxH, boxW, boxH);

                GUI.Box(boxRect, "", speechBoxStyle);
                GUI.Label(new Rect(boxRect.x + 10f, boxRect.y + 8f, boxRect.width - 20f, boxRect.height - 16f), currentSpeech, speechStyle);
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
