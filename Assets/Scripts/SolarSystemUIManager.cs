using UnityEngine;
using System.Collections.Generic;

namespace SolarSystemScope
{
    public class SolarSystemUIManager : MonoBehaviour
    {
        public static SolarSystemUIManager Instance { get; private set; }

        private List<CelestialBody> celestialBodies = new List<CelestialBody>();
        private CelestialBody focusedBody;
        private SolarSystemCameraController cameraCtrl;
        private PlanetLabelManager labelManager;

        private void Awake()
        {
            Instance = this;
        }

        public void Initialize(List<CelestialBody> bodies, SolarSystemCameraController camController, PlanetLabelManager labelMgr = null)
        {
            celestialBodies = bodies;
            cameraCtrl = camController;
            labelManager = labelMgr;

            BuildUI();

            focusedBody = null;
            if (labelManager != null)
            {
                labelManager.SetFocusedBody(null);
            }
        }

        private void BuildUI()
        {
            // Destroy any existing Info Canvas
            GameObject existing = GameObject.Find("PlanetInfoCanvas");
            if (existing != null) Destroy(existing);
        }

        public void SelectBody(CelestialBody body)
        {
            if (body == null) return;
            focusedBody = body;

            if (cameraCtrl != null)
            {
                float viewDist = Mathf.Max(body.transform.localScale.y * 4.0f, 24f);
                if (body.bodyName == "Sun") viewDist = 160f;
                cameraCtrl.SetTarget(body.transform, viewDist);
            }

            if (labelManager != null)
            {
                labelManager.SetFocusedBody(body);
            }
        }
    }
}
