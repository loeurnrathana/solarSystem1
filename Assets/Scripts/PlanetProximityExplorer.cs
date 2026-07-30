using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SolarSystemScope
{
    public class PlanetProximityExplorer : MonoBehaviour
    {
        [Header("Planet Settings")]
        public string planetName = "Mars";
        public string explorationSceneName = "MarsExplorationScene";

        [Header("Interaction Settings")]
        public Transform playerSpaceship;
        public float interactionRadius = 45f;
        public KeyCode interactKey = KeyCode.E;

        [Header("UI Prompt")]
        public GameObject uiPromptPanel;
        public UnityEngine.UI.Text promptText;

        [Header("Transition Settings")]
        public bool loadSceneOnInteract = false;

        private bool isPlayerInRange = false;
        private bool isTransitioning = false;

        private void Start()
        {
            if (playerSpaceship == null)
            {
                GameObject playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null)
                {
                    playerSpaceship = playerObj.transform;
                }
                else if (Camera.main != null)
                {
                    playerSpaceship = Camera.main.transform;
                }
            }

            if (uiPromptPanel != null)
            {
                uiPromptPanel.SetActive(false);
            }
        }

        private void Update()
        {
            if (playerSpaceship == null || isTransitioning) return;

            float distance = Vector3.Distance(transform.position, playerSpaceship.position);
            float effectiveRadius = Mathf.Max(transform.localScale.y * 6.0f, interactionRadius);

            if (distance <= effectiveRadius)
            {
                if (!isPlayerInRange)
                {
                    EnterProximityRange();
                }

                if (WasKeyPressedThisFrame())
                {
                    TriggerPlanetExploration();
                }
            }
            else
            {
                if (isPlayerInRange)
                {
                    ExitProximityRange();
                }
            }
        }

        private bool WasKeyPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER || !ENABLE_INPUT_SYSTEM
            try { if (Input.GetKeyDown(interactKey)) return true; } catch {}
#endif
            return false;
        }

        private void EnterProximityRange()
        {
            isPlayerInRange = true;
            if (uiPromptPanel != null)
            {
                uiPromptPanel.SetActive(true);
                if (promptText != null)
                {
                    promptText.text = $"Press [{interactKey}] to Explore {planetName}";
                }
            }
        }

        private void ExitProximityRange()
        {
            isPlayerInRange = false;
            if (uiPromptPanel != null)
            {
                uiPromptPanel.SetActive(false);
            }
        }

        private void TriggerPlanetExploration()
        {
            Debug.Log($"<color=cyan>[Planet Proximity Explorer] Entering {planetName}!</color>");

            if (uiPromptPanel != null)
            {
                uiPromptPanel.SetActive(false);
            }

            CelestialBody body = GetComponent<CelestialBody>();
            if (body != null)
            {
                if (PlanetSurfaceExplorer.Instance == null)
                {
                    GameObject explorerObj = new GameObject("PlanetSurfaceExplorerManager");
                    explorerObj.AddComponent<PlanetSurfaceExplorer>();
                }
                if (PlanetSurfaceExplorer.Instance != null)
                {
                    PlanetSurfaceExplorer.Instance.EnterPlanetSurface(body);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, interactionRadius);
        }
    }
}
