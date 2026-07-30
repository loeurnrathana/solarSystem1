using System;
using System.Collections.Generic;
using UnityEngine;

namespace MarsExplorer
{
    [Serializable]
    public class LandmarkData
    {
        public string id;
        public string name;
        public string agency; // "NASA", "ESA", etc.
        public string type;
        public float latitude;
        public float longitude;
        public string elevation;
        public string diameter;
        [TextArea(2, 5)]
        public string details;
        public string icon;
        public string year;
    }

    public class LandmarkManager : MonoBehaviour
    {
        public Transform planetTransform;
        public float planetRadius = 2.5f;
        public GameObject pinPrefab;

        public List<LandmarkData> landmarks = new List<LandmarkData>();
        private List<GameObject> spawnedPins = new List<GameObject>();

        private void Awake()
        {
            InitializeNASALandmarks();
        }

        private void Start()
        {
            if (planetTransform == null) planetTransform = transform;
            SpawnLandmarkPins();
        }

        public void InitializeNASALandmarks()
        {
            if (landmarks.Count > 0) return;

            landmarks.Add(new LandmarkData
            {
                id = "perseverance",
                name = "Jezero Crater (Perseverance)",
                agency = "NASA Mars 2020",
                type = "Rover & Ingenuity Helicopter Site",
                latitude = 18.38f,
                longitude = 77.58f,
                elevation = "-2.5 km",
                diameter = "45 km",
                year = "2021",
                details = "NASA Perseverance Rover search site for signs of ancient microbial life and Martian rock sample collection.",
                icon = "🛸"
            });

            landmarks.Add(new LandmarkData
            {
                id = "curiosity",
                name = "Gale Crater (Curiosity)",
                agency = "NASA Science Lab",
                type = "Rover Mission Site",
                latitude = -5.4f,
                longitude = 137.8f,
                elevation = "-4.5 km",
                diameter = "154 km",
                year = "2012",
                details = "NASA Curiosity Rover investigating Mount Sharp and liquid water history on ancient Mars.",
                icon = "🧪"
            });

            landmarks.Add(new LandmarkData
            {
                id = "insight",
                name = "Elysium Planitia (InSight)",
                agency = "NASA Discovery",
                type = "Seismometer Lander Site",
                latitude = 4.5f,
                longitude = 135.6f,
                elevation = "-3.0 km",
                diameter = "Volcanic Plain",
                year = "2018",
                details = "NASA InSight lander measured interior seismic Marsquakes and heat flow from the Martian core.",
                icon = "🛰️"
            });

            landmarks.Add(new LandmarkData
            {
                id = "olympus",
                name = "Olympus Mons",
                agency = "NASA Orbiter Survey",
                type = "Shield Volcano Peak",
                latitude = 18.65f,
                longitude = 226.2f,
                elevation = "21.9 km (72,000 ft)",
                diameter = "624 km",
                year = "Discovered 1971",
                details = "The largest known volcano in the Solar System. 2.5x the height of Mount Everest.",
                icon = "🌋"
            });

            landmarks.Add(new LandmarkData
            {
                id = "valles",
                name = "Valles Marineris",
                agency = "NASA Mariner 9 Site",
                type = "Grand Canyon System",
                latitude = -13.9f,
                longitude = 300.8f,
                elevation = "-7.0 km depth",
                diameter = "4,000 km length",
                year = "Discovered 1971",
                details = "Vast tectonic canyon rift system spanning nearly a quarter of Mars' circumference.",
                icon = "🏜️"
            });

            landmarks.Add(new LandmarkData
            {
                id = "southpole",
                name = "Planum Australe",
                agency = "NASA Polar Reconnaissance",
                type = "South Polar Ice Sheet",
                latitude = -83.9f,
                longitude = 160.0f,
                elevation = "3.0 km ice thickness",
                diameter = "400 km ice cap",
                year = "Permanent Cap",
                details = "Layered deposits of solid CO2 dry ice and frozen water ice.",
                icon = "❄️"
            });
        }

        public void SpawnLandmarkPins()
        {
            foreach (var pin in spawnedPins)
            {
                if (pin != null) SafeDestroy(pin);
            }
            spawnedPins.Clear();

            foreach (var lm in landmarks)
            {
                Vector3 localPos = LatLonToVector3(lm.latitude, lm.longitude, planetRadius * 1.015f);

                GameObject pinObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pinObj.name = "Pin_" + lm.id;
                pinObj.transform.SetParent(planetTransform, false);
                pinObj.transform.localPosition = localPos;
                pinObj.transform.localScale = Vector3.one * 0.12f;

                Renderer r = pinObj.GetComponent<Renderer>();
                if (r != null)
                {
                    r.material = new Material(Shader.Find("Unlit/Color"));
                    r.material.color = lm.agency.Contains("NASA") ? new Color(1.0f, 0.4f, 0.1f) : new Color(0.2f, 0.8f, 1.0f);
                }

                spawnedPins.Add(pinObj);
            }
        }

        public Vector3 LatLonToVector3(float lat, float lon, float radius)
        {
            float phi = (90f - lat) * Mathf.Deg2Rad;
            float theta = (lon + 180f) * Mathf.Deg2Rad;

            float x = radius * Mathf.Sin(phi) * Mathf.Cos(theta);
            float z = radius * Mathf.Sin(phi) * Mathf.Sin(theta);
            float y = radius * Mathf.Cos(phi);

            return new Vector3(x, y, z);
        }

        private void SafeDestroy(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }
    }
}
