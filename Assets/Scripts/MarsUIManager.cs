using UnityEngine;
using UnityEngine.UI;
using MarsExplorer;

namespace MarsExplorer
{
    public class MarsUIManager : MonoBehaviour
    {
        [Header("References")]
        public PlanetRotator planetRotator;
        public ProceduralMarsTexture textureGenerator;
        public LandmarkManager landmarkManager;
        public PlanetCameraController cameraController;

        [Header("UI Elements")]
        public Text titleText;
        public Text telemetryText;
        public Button playPauseButton;
        public Text playPauseText;
        public Button reverseButton;
        public Text reverseText;
        public Slider speedSlider;
        public Text speedText;

        [Header("Landmark HUD Panel")]
        public GameObject hudPanel;
        public Text hudTitleText;
        public Text hudTypeText;
        public Text hudCoordsText;
        public Text hudElevationText;
        public Text hudDiameterText;
        public Text hudDetailsText;
        public Button hudCloseButton;

        private void Start()
        {
            if (playPauseButton != null) playPauseButton.onClick.AddListener(OnPlayPauseClicked);
            if (reverseButton != null) reverseButton.onClick.AddListener(OnReverseClicked);
            if (speedSlider != null) speedSlider.onValueChanged.AddListener(OnSpeedSliderChanged);
            if (hudCloseButton != null) hudCloseButton.onClick.AddListener(CloseHUD);

            if (hudPanel != null) hudPanel.SetActive(false);
        }

        private void Update()
        {
            if (planetRotator != null && telemetryText != null)
            {
                float angle = planetRotator.GetCurrentYAngle();
                telemetryText.text = $"ROTATION ANGLE: {Mathf.FloorToInt(angle)}°";
            }
        }

        public void OnPlayPauseClicked()
        {
            if (planetRotator == null) return;
            planetRotator.TogglePause();

            if (playPauseText != null)
            {
                playPauseText.text = planetRotator.isRotating ? "Pause" : "Resume";
            }
        }

        public void OnReverseClicked()
        {
            if (planetRotator == null) return;
            planetRotator.ToggleDirection();

            if (reverseText != null)
            {
                reverseText.text = planetRotator.isPrograde ? "Prograde" : "Retrograde";
            }
        }

        public void OnSpeedSliderChanged(float val)
        {
            if (planetRotator == null) return;
            planetRotator.SetSpeedMultiplier(val);

            if (speedText != null)
            {
                speedText.text = $"{val:F1}x";
            }
        }

        public void SetRenderMode(int modeIndex)
        {
            if (textureGenerator == null) return;
            textureGenerator.ApplyMode((ProceduralMarsTexture.RenderMode)modeIndex);
        }

        public void SelectLandmark(LandmarkData lm)
        {
            if (lm == null) return;

            if (cameraController != null && landmarkManager != null)
            {
                Transform pTransform = landmarkManager.planetTransform != null ? landmarkManager.planetTransform : landmarkManager.transform;
                Vector3 localPos = landmarkManager.LatLonToVector3(lm.latitude, lm.longitude, landmarkManager.planetRadius);
                Vector3 worldPos = pTransform.TransformPoint(localPos);
                cameraController.FocusOnPosition(worldPos);
            }

            ShowHUD(lm);
        }

        public void ShowHUD(LandmarkData lm)
        {
            if (hudPanel == null) return;

            if (hudTitleText != null) hudTitleText.text = $"{lm.icon} {lm.name}";
            if (hudTypeText != null) hudTypeText.text = lm.type;
            if (hudCoordsText != null) hudCoordsText.text = $"Lat: {lm.latitude}° | Lon: {lm.longitude}°";
            if (hudElevationText != null) hudElevationText.text = lm.elevation;
            if (hudDiameterText != null) hudDiameterText.text = lm.diameter;
            if (hudDetailsText != null) hudDetailsText.text = lm.details;

            hudPanel.SetActive(true);
        }

        public void CloseHUD()
        {
            if (hudPanel != null) hudPanel.SetActive(false);
        }
    }
}
