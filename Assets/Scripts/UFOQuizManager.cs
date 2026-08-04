using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace SolarSystemScope
{
    public class UFOQuizManager : MonoBehaviour
    {
        public static UFOQuizManager Instance { get; private set; }

        public class QuestionData
        {
            public string Question;
            public string[] Options;
            public int CorrectIndex;

            public QuestionData(string question, string[] options, int correctIndex)
            {
                Question = question;
                Options = options;
                CorrectIndex = correctIndex;
            }
        }

        public class ActiveQuestion
        {
            public string Question;
            public string[] ShuffledOptions;
            public int CorrectIndex;
        }

        private Dictionary<string, List<QuestionData>> planetQuestionBanks = new Dictionary<string, List<QuestionData>>(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> passedPlanets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Active Quiz State
        private bool isQuizActive = false;
        private bool isRevisitChoiceActive = false;
        public bool IsQuizActive => isQuizActive || isRevisitChoiceActive;

        private string activePlanetName = "";
        private List<ActiveQuestion> currentQuizQuestions = new List<ActiveQuestion>();
        private int currentQuestionIndex = 0;
        private int currentScore = 0;
        private int selectedOptionIndex = -1;
        private bool answerSubmitted = false;
        private bool isQuizFinished = false;
        private bool isPassed = false;

        // UI Styles
        private GUIStyle panelStyle;
        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle questionStyle;
        private GUIStyle optionButtonStyle;
        private GUIStyle correctOptionStyle;
        private GUIStyle wrongOptionStyle;
        private GUIStyle nextButtonStyle;
        private GUIStyle scoreBadgeStyle;
        private GUIStyle progressTextStyle;

        // Audio Clips
        private AudioSource audioSource;
        private AudioClip clickClip;
        private AudioClip correctClip;
        private AudioClip wrongClip;
        private AudioClip victoryClip;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeQuestionBanks();
                InitializeAudio();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeAudio()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D UI sound

            clickClip = CreateClickAudioClip();
            correctClip = CreateCorrectAudioClip();
            wrongClip = CreateWrongAudioClip();
            victoryClip = PlanetSurfaceExplorer.CreateMinecraftLevelUpAudioClip();
        }

        public bool IsPlanetQuizPassed(string planetName)
        {
            if (string.IsNullOrEmpty(planetName)) return false;
            return passedPlanets.Contains(planetName);
        }

        public void ResetPlanetQuizPassed(string planetName)
        {
            if (passedPlanets.Contains(planetName))
            {
                passedPlanets.Remove(planetName);
            }
        }

        public void StartQuiz(string planetName)
        {
            if (string.IsNullOrEmpty(planetName)) planetName = "Earth";
            activePlanetName = planetName;

            List<QuestionData> pool;
            if (!planetQuestionBanks.TryGetValue(planetName, out pool) || pool == null || pool.Count == 0)
            {
                pool = GetGenericFallbackQuestions(planetName);
            }

            // Randomly select 5 questions from the pool
            List<QuestionData> poolCopy = new List<QuestionData>(pool);
            System.Random rng = new System.Random();
            
            // Shuffle poolCopy
            int n = poolCopy.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                QuestionData temp = poolCopy[k];
                poolCopy[k] = poolCopy[n];
                poolCopy[n] = temp;
            }

            int sampleCount = Mathf.Min(5, poolCopy.Count);
            currentQuizQuestions.Clear();

            for (int i = 0; i < sampleCount; i++)
            {
                QuestionData q = poolCopy[i];
                
                // Shuffle options & update correct index
                ActiveQuestion aq = new ActiveQuestion();
                aq.Question = q.Question;

                List<KeyValuePair<string, bool>> optionPairs = new List<KeyValuePair<string, bool>>();
                for (int optIdx = 0; optIdx < q.Options.Length; optIdx++)
                {
                    optionPairs.Add(new KeyValuePair<string, bool>(q.Options[optIdx], optIdx == q.CorrectIndex));
                }

                // Shuffle optionPairs
                int optN = optionPairs.Count;
                while (optN > 1)
                {
                    optN--;
                    int k = rng.Next(optN + 1);
                    var tPair = optionPairs[k];
                    optionPairs[k] = optionPairs[optN];
                    optionPairs[optN] = tPair;
                }

                aq.ShuffledOptions = new string[optionPairs.Count];
                for (int optIdx = 0; optIdx < optionPairs.Count; optIdx++)
                {
                    aq.ShuffledOptions[optIdx] = optionPairs[optIdx].Key;
                    if (optionPairs[optIdx].Value)
                    {
                        aq.CorrectIndex = optIdx;
                    }
                }

                currentQuizQuestions.Add(aq);
            }

            currentQuestionIndex = 0;
            currentScore = 0;
            selectedOptionIndex = -1;
            answerSubmitted = false;
            isQuizFinished = false;
            isPassed = false;
            isRevisitChoiceActive = false;
            isQuizActive = true;

            // Unlock mouse cursor for UI
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            PlaySound(clickClip);
        }

        public void ShowRevisitPrompt(string planetName)
        {
            if (string.IsNullOrEmpty(planetName)) planetName = "Earth";
            activePlanetName = planetName;
            isQuizActive = false;
            isRevisitChoiceActive = true;

            // Unlock mouse cursor for UI
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            PlaySound(clickClip);
        }

        private void Update()
        {
            if (!IsQuizActive) return;

#if ENABLE_INPUT_SYSTEM
            bool escPressed = (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame);
#else
            bool escPressed = Input.GetKeyDown(KeyCode.Escape);
#endif
            if (escPressed)
            {
                CloseQuiz();
            }
        }

        public void CloseQuiz()
        {
            isQuizActive = false;
            isRevisitChoiceActive = false;

            // Re-lock cursor if in surface explorer
            if (PlanetSurfaceExplorer.Instance != null && PlanetSurfaceExplorer.Instance.CurrentPlanetName != null)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void OnGUI()
        {
            if (!IsQuizActive) return;

            InitStyles();

            // Screen Dim Backdrop
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "", GetBackdropStyle());

            if (isRevisitChoiceActive)
            {
                float revWidth = Mathf.Min(740f, Screen.width * 0.90f);
                float revHeight = Mathf.Min(420f, Screen.height * 0.85f);
                float revX = (Screen.width - revWidth) * 0.5f;
                float revY = (Screen.height - revHeight) * 0.5f;

                Rect revRect = new Rect(revX, revY, revWidth, revHeight);
                GUI.Box(revRect, "", panelStyle);

                GUILayout.BeginArea(new Rect(revX + 25f, revY + 20f, revWidth - 50f, revHeight - 40f));

                GUILayout.Label($"SECURITY CLEARANCE VERIFIED ({activePlanetName.ToUpper()})", titleStyle);
                GUILayout.Label("You have already answered and passed the QCM quiz for this planet!", subtitleStyle);
                GUILayout.Space(20f);

                GUILayout.Box("What would you like to do?", questionStyle, GUILayout.MinHeight(55f));
                GUILayout.Space(20f);

                // Retake Test Button
                if (GUILayout.Button("🔄 RETAKE THE TEST (NEW 5 RANDOM QUESTIONS)", optionButtonStyle, GUILayout.Height(48f)))
                {
                    isRevisitChoiceActive = false;
                    StartQuiz(activePlanetName);
                }

                GUILayout.Space(10f);

                // Leave Planet Button
                if (GUILayout.Button("🚀 LEAVE THE PLANET TO SOLAR SYSTEM", nextButtonStyle, GUILayout.Height(50f)))
                {
                    CloseQuiz();
                    if (PlanetSurfaceExplorer.Instance != null)
                    {
                        PlanetSurfaceExplorer.Instance.ExitPlanetSurface(forceExit: true);
                    }
                }

                GUILayout.Space(8f);

                // Cancel / Explore Further
                if (GUILayout.Button("CANCEL / EXPLORE SURFACE FURTHER", optionButtonStyle, GUILayout.Height(38f)))
                {
                    CloseQuiz();
                }

                GUILayout.EndArea();
                return;
            }

            float panelWidth = Mathf.Min(820f, Screen.width * 0.92f);
            float panelHeight = Mathf.Min(640f, Screen.height * 0.90f);
            float panelX = (Screen.width - panelWidth) * 0.5f;
            float panelY = (Screen.height - panelHeight) * 0.5f;

            Rect panelRect = new Rect(panelX, panelY, panelWidth, panelHeight);
            GUI.Box(panelRect, "", panelStyle);

            GUILayout.BeginArea(new Rect(panelX + 25f, panelY + 20f, panelWidth - 50f, panelHeight - 40f));

            // Header Title
            GUILayout.Label($"UFO SECURITY CLEARANCE: {activePlanetName.ToUpper()} QCM QUIZ", titleStyle);
            GUILayout.Label("Pass 4/5 questions to unlock spaceship launch to orbit!", subtitleStyle);
            GUILayout.Space(12f);

            if (!isQuizFinished && currentQuizQuestions.Count > 0)
            {
                ActiveQuestion currentQ = currentQuizQuestions[currentQuestionIndex];

                // Progress Bar & Question Counter
                string progressStr = $"QUESTION {currentQuestionIndex + 1} OF {currentQuizQuestions.Count}";
                GUILayout.Label(progressStr, progressTextStyle);

                // Progress Bar Track
                Rect progressTrackRect = GUILayoutUtility.GetRect(panelWidth - 50f, 10f);
                GUI.Box(progressTrackRect, "", GetProgressBarTrackStyle());
                float progressPercent = (float)(currentQuestionIndex + 1) / currentQuizQuestions.Count;
                Rect progressFillRect = new Rect(progressTrackRect.x, progressTrackRect.y, progressTrackRect.width * progressPercent, progressTrackRect.height);
                GUI.Box(progressFillRect, "", GetProgressBarFillStyle());

                GUILayout.Space(16f);

                // Question Text Box
                GUILayout.Box(currentQ.Question, questionStyle, GUILayout.MinHeight(75f));
                GUILayout.Space(15f);

                // Options (A, B, C, D)
                char[] optionPrefixes = new char[] { 'A', 'B', 'C', 'D' };
                for (int i = 0; i < currentQ.ShuffledOptions.Length; i++)
                {
                    char prefix = (i < optionPrefixes.Length) ? optionPrefixes[i] : (char)('A' + i);
                    string optText = $"{prefix})  {currentQ.ShuffledOptions[i]}";

                    GUIStyle currentOptStyle = optionButtonStyle;
                    if (answerSubmitted)
                    {
                        if (i == currentQ.CorrectIndex)
                        {
                            currentOptStyle = correctOptionStyle;
                        }
                        else if (i == selectedOptionIndex)
                        {
                            currentOptStyle = wrongOptionStyle;
                        }
                    }

                    if (GUILayout.Button(optText, currentOptStyle, GUILayout.MinHeight(44f)))
                    {
                        if (!answerSubmitted)
                        {
                            selectedOptionIndex = i;
                            answerSubmitted = true;

                            if (i == currentQ.CorrectIndex)
                            {
                                currentScore++;
                                PlaySound(correctClip);
                            }
                            else
                            {
                                PlaySound(wrongClip);
                            }
                        }
                    }
                    GUILayout.Space(6f);
                }

                GUILayout.Space(10f);

                // Next Question / Finish Button
                if (answerSubmitted)
                {
                    string btnText = (currentQuestionIndex + 1 < currentQuizQuestions.Count) ? "NEXT QUESTION  ➜" : "SEE RESULT  ➜";
                    if (GUILayout.Button(btnText, nextButtonStyle, GUILayout.Height(46f)))
                    {
                        PlaySound(clickClip);
                        if (currentQuestionIndex + 1 < currentQuizQuestions.Count)
                        {
                            currentQuestionIndex++;
                            selectedOptionIndex = -1;
                            answerSubmitted = false;
                        }
                        else
                        {
                            // Finish Quiz
                            isQuizFinished = true;
                            isPassed = (currentScore >= 4);

                            if (isPassed)
                            {
                                passedPlanets.Add(activePlanetName);
                                PlaySound(victoryClip);
                            }
                        }
                    }
                }
            }
            else if (isQuizFinished)
            {
                // Result Summary Screen
                GUILayout.Space(20f);
                string scoreText = $"QUIZ COMPLETED! SCORE: {currentScore} / {currentQuizQuestions.Count}";
                GUILayout.Label(scoreText, titleStyle);

                GUILayout.Space(15f);

                if (isPassed)
                {
                    GUILayout.Box("✔ SECURITY CLEARANCE GRANTED!\nYou have passed the QCM quiz! You can now board the UFO and return to space orbit.", scoreBadgeStyle);
                }
                else
                {
                    GUILayout.Box("✖ SECURITY CLEARANCE DENIED!\nYou scored less than 4/5. Answer 4 or more correctly to unlock space launch.", wrongOptionStyle);
                }

                GUILayout.Space(25f);

                if (isPassed)
                {
                    if (GUILayout.Button("🚀 BOARD UFO & LAUNCH TO ORBIT", nextButtonStyle, GUILayout.Height(50f)))
                    {
                        CloseQuiz();
                        if (PlanetSurfaceExplorer.Instance != null)
                        {
                            PlanetSurfaceExplorer.Instance.ExitPlanetSurface();
                        }
                    }

                    GUILayout.Space(10f);

                    if (GUILayout.Button("EXPLORE SURFACE FURTHER", optionButtonStyle, GUILayout.Height(42f)))
                    {
                        CloseQuiz();
                    }
                }
                else
                {
                    if (GUILayout.Button("🔄 TRY AGAIN (NEW 5 RANDOM QUESTIONS)", nextButtonStyle, GUILayout.Height(50f)))
                    {
                        StartQuiz(activePlanetName);
                    }

                    GUILayout.Space(10f);

                    if (GUILayout.Button("CLOSE", optionButtonStyle, GUILayout.Height(42f)))
                    {
                        CloseQuiz();
                    }
                }
            }

            GUILayout.EndArea();
        }

        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip, 0.85f);
            }
        }

        private void InitStyles()
        {
            if (panelStyle != null) return;

            Texture2D darkTex = MakeTex(2, 2, new Color(0.06f, 0.08f, 0.14f, 0.95f));
            Texture2D optionTex = MakeTex(2, 2, new Color(0.12f, 0.16f, 0.26f, 0.90f));
            Texture2D optionHoverTex = MakeTex(2, 2, new Color(0.20f, 0.28f, 0.44f, 0.95f));
            Texture2D correctTex = MakeTex(2, 2, new Color(0.08f, 0.65f, 0.28f, 0.95f));
            Texture2D wrongTex = MakeTex(2, 2, new Color(0.75f, 0.15f, 0.20f, 0.95f));
            Texture2D nextTex = MakeTex(2, 2, new Color(0.0f, 0.70f, 0.95f, 1.0f));

            panelStyle = new GUIStyle(GUI.skin.box);
            panelStyle.normal.background = darkTex;
            panelStyle.border = new RectOffset(4, 4, 4, 4);

            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 24;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.normal.textColor = new Color(0.2f, 0.95f, 1.0f);

            subtitleStyle = new GUIStyle(GUI.skin.label);
            subtitleStyle.fontSize = 14;
            subtitleStyle.alignment = TextAnchor.MiddleCenter;
            subtitleStyle.normal.textColor = new Color(0.85f, 0.90f, 0.95f);

            progressTextStyle = new GUIStyle(GUI.skin.label);
            progressTextStyle.fontSize = 13;
            progressTextStyle.fontStyle = FontStyle.Bold;
            progressTextStyle.alignment = TextAnchor.MiddleLeft;
            progressTextStyle.normal.textColor = new Color(1.0f, 0.85f, 0.3f);

            questionStyle = new GUIStyle(GUI.skin.box);
            questionStyle.fontSize = 17;
            questionStyle.fontStyle = FontStyle.Bold;
            questionStyle.alignment = TextAnchor.MiddleCenter;
            questionStyle.wordWrap = true;
            questionStyle.normal.background = MakeTex(2, 2, new Color(0.10f, 0.13f, 0.22f, 0.90f));
            questionStyle.normal.textColor = Color.white;
            questionStyle.padding = new RectOffset(15, 15, 10, 10);

            optionButtonStyle = new GUIStyle(GUI.skin.button);
            optionButtonStyle.fontSize = 15;
            optionButtonStyle.alignment = TextAnchor.MiddleLeft;
            optionButtonStyle.wordWrap = true;
            optionButtonStyle.normal.background = optionTex;
            optionButtonStyle.normal.textColor = new Color(0.92f, 0.95f, 1.0f);
            optionButtonStyle.hover.background = optionHoverTex;
            optionButtonStyle.hover.textColor = Color.white;
            optionButtonStyle.padding = new RectOffset(16, 16, 8, 8);

            correctOptionStyle = new GUIStyle(optionButtonStyle);
            correctOptionStyle.normal.background = correctTex;
            correctOptionStyle.normal.textColor = Color.white;
            correctOptionStyle.hover.background = correctTex;

            wrongOptionStyle = new GUIStyle(optionButtonStyle);
            wrongOptionStyle.normal.background = wrongTex;
            wrongOptionStyle.normal.textColor = Color.white;
            wrongOptionStyle.hover.background = wrongTex;

            nextButtonStyle = new GUIStyle(GUI.skin.button);
            nextButtonStyle.fontSize = 17;
            nextButtonStyle.fontStyle = FontStyle.Bold;
            nextButtonStyle.alignment = TextAnchor.MiddleCenter;
            nextButtonStyle.normal.background = nextTex;
            nextButtonStyle.normal.textColor = Color.white;
            nextButtonStyle.hover.background = MakeTex(2, 2, new Color(0.2f, 0.85f, 1.0f, 1.0f));

            scoreBadgeStyle = new GUIStyle(GUI.skin.box);
            scoreBadgeStyle.fontSize = 18;
            scoreBadgeStyle.fontStyle = FontStyle.Bold;
            scoreBadgeStyle.alignment = TextAnchor.MiddleCenter;
            scoreBadgeStyle.wordWrap = true;
            scoreBadgeStyle.normal.background = correctTex;
            scoreBadgeStyle.normal.textColor = Color.white;
            scoreBadgeStyle.padding = new RectOffset(20, 20, 15, 15);
        }

        private GUIStyle GetBackdropStyle()
        {
            GUIStyle s = new GUIStyle();
            s.normal.background = MakeTex(2, 2, new Color(0.03f, 0.06f, 0.12f, 0.65f));
            return s;
        }

        private GUIStyle GetProgressBarTrackStyle()
        {
            GUIStyle s = new GUIStyle();
            s.normal.background = MakeTex(2, 2, new Color(0.15f, 0.20f, 0.30f, 0.9f));
            return s;
        }

        private GUIStyle GetProgressBarFillStyle()
        {
            GUIStyle s = new GUIStyle();
            s.normal.background = MakeTex(2, 2, new Color(0.1f, 0.90f, 1.0f, 1.0f));
            return s;
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i) pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private AudioClip CreateClickAudioClip()
        {
            int sampleRate = 44100;
            float duration = 0.05f;
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                samples[i] = Mathf.Sin(2f * Mathf.PI * 800f * t) * Mathf.Exp(-t * 80f);
            }
            AudioClip clip = AudioClip.Create("QCM_Click", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateCorrectAudioClip()
        {
            int sampleRate = 44100;
            float duration = 0.22f;
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float freq = (t < 0.11f) ? 523.25f : 659.25f; // C5 to E5 chord
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * Mathf.Exp(-t * 12f) * 0.6f;
            }
            AudioClip clip = AudioClip.Create("QCM_Correct", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateWrongAudioClip()
        {
            int sampleRate = 44100;
            float duration = 0.25f;
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float freq = (t < 0.12f) ? 220f : 185f; // Low buzzing tone
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * Mathf.Exp(-t * 8f) * 0.7f;
            }
            AudioClip clip = AudioClip.Create("QCM_Wrong", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void InitializeQuestionBanks()
        {
            // 1. SATURN (12 Questions)
            planetQuestionBanks["Saturn"] = new List<QuestionData>
            {
                new QuestionData("What type of planet is Saturn, and what are its two main elements?", new string[] { "Terrestrial planet made mostly of rock and iron", "Ice giant made mostly of water and methane", "Gas giant made mostly of hydrogen and helium" }, 2),
                new QuestionData("Who is Saturn named after in Roman mythology?", new string[] { "The Roman god of the sea", "The Roman god of agriculture, wealth, and time", "The Roman god of war" }, 1),
                new QuestionData("How does Saturn's density compare to the other planets in the Solar System?", new string[] { "It has the lowest density of any planet, even less dense than water", "It has roughly the same density as Earth", "It is the densest planet in the Solar System" }, 0),
                new QuestionData("How far away is Saturn from the Sun compared to Earth?", new string[] { "About 2 times farther from the Sun than Earth", "About 9.5 times farther from the Sun than Earth", "About 30 times farther from the Sun than Earth" }, 1),
                new QuestionData("How long does it take for Saturn to complete one year?", new string[] { "About 10.7 Earth hours", "About 12 Earth years", "About 29.5 Earth years" }, 2),
                new QuestionData("What causes Saturn to have a pale yellow and golden appearance?", new string[] { "Ammonia crystals in its upper atmosphere", "High amounts of sulphur near its core", "Dust and rock particles from its rings reflecting light" }, 0),
                new QuestionData("What do scientists believe lies at the very centre of Saturn?", new string[] { "A completely hollow gas centre", "A large, solid core made entirely of iron", "A small rocky core surrounded by metallic hydrogen, liquid hydrogen, and gas" }, 2),
                new QuestionData("Why doesn't Saturn have geological periods like Earth does?", new string[] { "It rotates too quickly to have geological time", "It has no solid surface", "It does not experience any seasonal changes" }, 1),
                new QuestionData("What is the most abundant gas in Saturn's atmosphere?", new string[] { "Helium (~96%)", "Hydrogen (~96%)", "Methane (~96%)" }, 1),
                new QuestionData("What is the \"Great White Spot\" and how often does it occur?", new string[] { "A giant storm that appears in its extremely cold upper atmosphere about once every 30 Earth years", "A permanent, raging hurricane near its equator that is visible at all times", "A large impact crater visible on its rocky core" }, 0),
                new QuestionData("Which of Saturn's moons has a thick atmosphere and lakes of liquid methane and ethane?", new string[] { "Enceladus", "Rhea", "Titan" }, 2),
                new QuestionData("As of 2026, how many confirmed moons does Saturn have?", new string[] { "79", "146", "274" }, 2)
            };

            // 2. VENUS (10 Questions)
            planetQuestionBanks["Venus"] = new List<QuestionData>
            {
                new QuestionData("Venus is the ______ planet from the Sun.", new string[] { "First", "Third", "Fourth", "Second" }, 3),
                new QuestionData("How is name \"Venus\" come from?", new string[] { "Roman goddess of love and beauty", "Ancient Greeks", "Ancient Egyptians", "Old English" }, 0),
                new QuestionData("What is Mass of Venus?", new string[] { "6.41 × 10²⁴ kg", "4.87 × 10²⁴ kg", "5.72 × 10²⁴ kg", "7.57 × 10²⁴ kg" }, 1),
                new QuestionData("How much is the distance from Venus to the Sun?", new string[] { "100 million km", "102 million km", "108 million km", "109 million km" }, 2),
                new QuestionData("What is the Diameter of Venus ?", new string[] { "13,569 km", "12,104 km", "14,756 km", "11,502 km" }, 1),
                new QuestionData("What is the Day length of Venus?", new string[] { "242 earth days", "250 earth days", "244 earth days", "243 earth days" }, 3),
                new QuestionData("What is the Year length of Venus?", new string[] { "220 earth days", "225 earth days", "230 earth days", "229 earth days" }, 1),
                new QuestionData("How many Geological Periods of Venus are?", new string[] { "2", "3", "4", "1" }, 1),
                new QuestionData("How many Venus's layers?", new string[] { "2", "1", "6", "3" }, 3),
                new QuestionData("How many Venus's moon is?", new string[] { "1", "3", "0", "2" }, 2),
                new QuestionData("What is the climate in Venus?", new string[] { "the hottest planet", "the coldest planet", "the warmth planet", "the foggy planet" }, 0)
            };

            // 3. MERCURY (10 Questions)
            planetQuestionBanks["Mercury"] = new List<QuestionData>
            {
                new QuestionData("Mercury is the ______ planet from the Sun.", new string[] { "First", "Third", "Fourth", "Seventh" }, 0),
                new QuestionData("How is name \"Mercury\" come from?", new string[] { "Ancient Greeks", "Roman messenger god", "Ancient Egyptians", "Old English" }, 1),
                new QuestionData("What is Mass of Mercury?", new string[] { "3.3011 × 10²³ kg", "3.9755 × 10²³ kg", "4.7267 × 10²³ kg", "2.5716 × 10²³ kg" }, 0),
                new QuestionData("How much is the distance from Mercury to the Sun?", new string[] { "57 million km", "56 million km", "58 million km", "59 million km" }, 2),
                new QuestionData("What is the Diameter of Mercury ?", new string[] { "2,614 km", "3,783 km", "4,561 km", "4,879 km" }, 3),
                new QuestionData("What is the Day length of Mercury?", new string[] { "57 earth days", "56 earth days", "59 earth days", "55 earth days" }, 2),
                new QuestionData("What is the Year length of Mercury?", new string[] { "88 earth days", "87 earth days", "86 earth days", "89 earth days" }, 0),
                new QuestionData("How many Geological Periods of Mercury are?", new string[] { "2", "3", "4", "5" }, 3),
                new QuestionData("How many Mercury's moon is?", new string[] { "1", "3", "2", "0" }, 3),
                new QuestionData("How many layers does Mercury have?", new string[] { "1", "3", "2", "6" }, 1)
            };

            // 4. NEPTUNE (10 Questions)
            planetQuestionBanks["Neptune"] = new List<QuestionData>
            {
                new QuestionData("What is Neptune's position in terms of its distance from the Sun within our solar system?", new string[] { "Seventh", "Nineth", "Eighth", "Sixth" }, 2),
                new QuestionData("How does Neptune's visibility from Earth compare to the other planets in our solar system?", new string[] { "It is the only planet not visible to the naked eye.", "It is the brightest planet in the night sky.", "It can only be seen during a solar eclipse.", "It is visible only from the Southern Hemisphere." }, 0),
                new QuestionData("What was unique about the method used to discover Neptune in 1846?", new string[] { "It was predicted using mathematical calculations.", "It was discovered by ancient astronomers using simple tools.", "It was found by accident during a comet search.", "It was first identified by a space probe." }, 0),
                new QuestionData("If Earth were the size of a nickel, approximately what size would Neptune be in comparison?", new string[] { "A baseball", "A basketball", "A marble", "A golf ball" }, 0),
                new QuestionData("How long does it take for Neptune to complete one single orbit around the Sun?", new string[] { "248 Earth Years", "165 Earth Years", "65 Earth Years", "16 Earth Years" }, 1),
                new QuestionData("Which of Neptune's moons is notable for its 'retrograde' orbit, circling the planet in the opposite direction of the planet's rotation?", new string[] { "Nereid", "Adams", "Galatea", "Triton" }, 3),
                new QuestionData("What unusual feature is found in Neptune's outermost ring, known as Adams?", new string[] { "Clumps of dust called arcs.", "Solid bands of ice and rock.", "bright glowing gases.", "A permanent hexagonal storm." }, 0),
                new QuestionData("Which gas in Neptune's atmosphere is responsible for giving the planet its blue appearance?", new string[] { "Oxygen", "Nitrogen", "Methane", "Carbon dioxide" }, 2),
                new QuestionData("Neptune is known for having the fastest winds in the solar system. How fast can these winds reach?", new string[] { "More than 1,200 miles per hour", "250 miles per hour", "500 miles per hour", "10,000 miles per hour" }, 0),
                new QuestionData("What defines Neptune as an 'ice giant' rather than a 'gas giant' like Jupiter?", new string[] { "It is composed mostly of a hot, dense fluid of water, methane, and ammonia.", "It is a solid ball of frozen nitrogen.", "It has no internal heat source.", "It is the only planet made entirely of liquid oxygen." }, 0)
            };

            // 5. JUPITER (12 Questions)
            planetQuestionBanks["Jupiter"] = new List<QuestionData>
            {
                new QuestionData("What two elements primarily make up Jupiter?", new string[] { "Oxygen and Nitrogen", "Hydrogen and Helium", "Methane and Ammonia", "Carbon Dioxide and Water Vapor" }, 1),
                new QuestionData("How long is one day on Jupiter?", new string[] { "24 hours", "11.86 Earth years", "9.9 hours", "4,333 Earth days" }, 2),
                new QuestionData("What causes the distinct orange, brown, and white bands in Jupiter's atmosphere?", new string[] { "Liquid hydrogen oceans", "Volcanic ash from its moons", "Ammonia crystals, water vapor, and sulfur compounds", "Reflection of the Sun's rays off metallic hydrogen" }, 2),
                new QuestionData("Which of Jupiter's moons is the most volcanically active world in the Solar System?", new string[] { "Europa", "Ganymede", "Callisto", "Io" }, 3),
                new QuestionData("Why do scientists study Jupiter's atmospheric changes instead of its geological periods?", new string[] { "Its atmosphere is easier to see", "It has no solid surface", "Its rings block geological features", "It is completely covered in ice" }, 1),
                new QuestionData("What is the \"Great Red Spot\"?", new string[] { "A giant crater on the surface", "A massive volcano", "A giant storm larger than Earth", "A concentration of metallic hydrogen" }, 2),
                new QuestionData("Where did Jupiter get its name?", new string[] { "An ancient Greek astronomer", "The Roman king of the gods", "The Latin word for \"giant\"", "The Roman god of the sea" }, 1),
                new QuestionData("How long does it take Jupiter to complete one orbit around the Sun (a year on Jupiter)?", new string[] { "365 Earth days", "9.9 hours", "11.86 Earth years", "84 Earth years" }, 2),
                new QuestionData("Which of Jupiter's moons is the largest in the Solar System?", new string[] { "Io", "Europa", "Ganymede", "Callisto" }, 2),
                new QuestionData("As of 2026, how many confirmed moons does Jupiter have?", new string[] { "4", "50", "79", "95" }, 3),
                new QuestionData("What unique feature is believed to exist on the moon Europa?", new string[] { "A vast ocean beneath its icy surface that may support life", "The most active volcanoes in the Solar System", "The most heavily cratered surface in the Solar System", "A giant storm made of metallic hydrogen" }, 0),
                new QuestionData("Who discovered Jupiter's four largest moons in 1610?", new string[] { "Isaac Newton", "Galileo Galilei", "Johannes Kepler", "Ancient Romans" }, 1)
            };

            // 6. EARTH (10 Questions)
            planetQuestionBanks["Earth"] = new List<QuestionData>
            {
                new QuestionData("Earth is the ______ planet from the Sun.", new string[] { "First", "Third", "Fourth", "Seventh" }, 1),
                new QuestionData("How is name \"Earth\" come from?", new string[] { "Ancient Greeks", "Ancient Romans", "Ancient Egyptians", "Old English" }, 3),
                new QuestionData("What is Mass of Earth?", new string[] { "6.41 × 10²⁴ kg", "5.97 × 10²⁴ kg", "5.72 × 10²⁴ kg", "6.57 × 10²⁴ kg" }, 1),
                new QuestionData("How much is the distance from Earth to the Sun?", new string[] { "230,503,755 km", "228,455,669 km", "150,196,428 km", "155,651,803 km" }, 2),
                new QuestionData("What is the Diameter of Earth ?", new string[] { "13,569 km", "15,846 km", "12,756 km", "11,502 km" }, 2),
                new QuestionData("What is the Day length of Earth?", new string[] { "24 hours", "12 hours", "8 hours", "23 hours" }, 0),
                new QuestionData("What is the Year length of Earth?", new string[] { "365.25 days", "360 days", "290 days", "333 days" }, 0),
                new QuestionData("How many Geological Periods of Earth are?", new string[] { "2", "3", "4", "1" }, 1),
                new QuestionData("How many Earth's layers?", new string[] { "2", "1", "6", "4" }, 3),
                new QuestionData("What percentage of Earth's surface is covered by water?", new string[] { "77%", "60%", "50%", "70%" }, 3)
            };

            // 7. MARS (10 Questions)
            planetQuestionBanks["Mars"] = new List<QuestionData>
            {
                new QuestionData("Mars is the ______ planet from the Sun.", new string[] { "First", "Third", "Fourth", "Seventh" }, 2),
                new QuestionData("What's the color of its appearance ?", new string[] { "red-orange", "black", "white", "blue" }, 0),
                new QuestionData("How is name \"Mars\" come from?", new string[] { "Ancient Greeks", "Ancient Romans", "Ancient Egyptians", "the Mayans" }, 1),
                new QuestionData("What is Mass of Mars?", new string[] { "6.4171 × 10²³ kg", "6.7350 × 10²³ kg", "5.7172 × 10²³ kg", "6.5073 × 10²³ kg" }, 0),
                new QuestionData("How much is the distance from Mars to the Sun?", new string[] { "230 million km", "228 million km", "229 million km", "227 million km" }, 1),
                new QuestionData("What is red-orange appearance made of ?", new string[] { "Iron(III) oxide, dust, rock", "Iron(II) oxide, dust, rock" }, 0),
                new QuestionData("How many Geological Periods of Mars are?", new string[] { "2", "3", "4", "1" }, 1),
                new QuestionData("What is Mars's climate like?", new string[] { "Cold and dry", "foggy", "Hot and sweat", "Rainy" }, 0),
                new QuestionData("How many Mars's moon are?", new string[] { "1", "3", "2", "4" }, 2),
                new QuestionData("What is another name of Mars?", new string[] { "Blue planet", "Hot planet", "Orange planet", "Red planet" }, 3)
            };
        }

        private List<QuestionData> GetGenericFallbackQuestions(string planetName)
        {
            return new List<QuestionData>
            {
                new QuestionData($"What type of celestial object is {planetName}?", new string[] { "Planet / Celestial Body", "Star", "Comet", "Black Hole" }, 0),
                new QuestionData($"Does {planetName} orbit around the Sun?", new string[] { "Yes, it orbits the Sun", "No, it orbits Earth", "No, it stays stationary", "It orbits Alpha Centauri" }, 0),
                new QuestionData($"What force keeps {planetName} in its orbital trajectory?", new string[] { "Gravity", "Magnetism", "Atmospheric Pressure", "Solar Wind" }, 0),
                new QuestionData($"Which space agency explores {planetName} using telescopes & probes?", new string[] { "NASA / International Space Agencies", "Local Weather Bureau", "Aviation Authorities", "Maritime Coastguard" }, 0),
                new QuestionData($"What is required before boarding the alien UFO to leave {planetName}?", new string[] { "Passing this QCM Quiz!", "Paying 100 Gold Coins", "Waiting 24 Hours", "Building a Rocket Ship" }, 0)
            };
        }
    }
}
