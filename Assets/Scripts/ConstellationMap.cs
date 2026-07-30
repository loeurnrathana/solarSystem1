using UnityEngine;
using System.Collections.Generic;

namespace SolarSystemScope
{
    public class ConstellationMap : MonoBehaviour
    {
        public float skyRadius = 350f;
        public Color constellationColor = new Color(0.20f, 0.65f, 1.0f, 0.55f);

        private void Start()
        {
            GameObject mapRoot = new GameObject("ConstellationMapRoot");
            mapRoot.transform.SetParent(transform, false);

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            Material lineMat = new Material(unlitShader);
            lineMat.color = constellationColor;
            if (lineMat.HasProperty("_BaseColor")) lineMat.SetColor("_BaseColor", constellationColor);
            if (lineMat.HasProperty("_Color")) lineMat.SetColor("_Color", constellationColor);

            if (lineMat.HasProperty("_Surface")) lineMat.SetFloat("_Surface", 1);
            if (lineMat.HasProperty("_Blend")) lineMat.SetFloat("_Blend", 1);
            lineMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            lineMat.SetInt("_ZWrite", 0);
            lineMat.renderQueue = 3000;

            List<Vector3[]> constellations = GenerateConstellationsData();

            int index = 0;
            foreach (var lines in constellations)
            {
                index++;
                GameObject constObj = new GameObject("Constellation_" + index);
                constObj.transform.SetParent(mapRoot.transform, false);
                LineRenderer lr = constObj.AddComponent<LineRenderer>();
                lr.useWorldSpace = false;
                lr.startWidth = 0.12f;
                lr.endWidth = 0.12f;
                lr.material = lineMat;
                lr.positionCount = lines.Length;

                for (int i = 0; i < lines.Length; i++)
                {
                    lr.SetPosition(i, lines[i] * skyRadius);
                }
            }
        }

        private List<Vector3[]> GenerateConstellationsData()
        {
            List<Vector3[]> list = new List<Vector3[]>();

            // Big Dipper (Ursa Major)
            list.Add(new Vector3[] {
                SphericalToVector(60, 45), SphericalToVector(55, 52), SphericalToVector(48, 54),
                SphericalToVector(42, 48), SphericalToVector(44, 40), SphericalToVector(52, 42),
                SphericalToVector(42, 48), SphericalToVector(35, 50)
            });

            // Orion
            list.Add(new Vector3[] {
                SphericalToVector(-15, 10), SphericalToVector(-12, -5), SphericalToVector(-5, -8),
                SphericalToVector(-2, 8), SphericalToVector(-15, 10), SphericalToVector(-8, 2),
                SphericalToVector(-6, 1), SphericalToVector(-4, 0), SphericalToVector(-2, 8)
            });

            // Cassiopeia
            list.Add(new Vector3[] {
                SphericalToVector(120, 60), SphericalToVector(130, 65), SphericalToVector(140, 58),
                SphericalToVector(150, 64), SphericalToVector(160, 59)
            });

            // Cygnus
            list.Add(new Vector3[] {
                SphericalToVector(-80, 40), SphericalToVector(-85, 30), SphericalToVector(-90, 20),
                SphericalToVector(-85, 30), SphericalToVector(-75, 32), SphericalToVector(-85, 30),
                SphericalToVector(-95, 28)
            });

            // Leo
            list.Add(new Vector3[] {
                SphericalToVector(200, 15), SphericalToVector(210, 25), SphericalToVector(218, 20),
                SphericalToVector(215, 10), SphericalToVector(200, 15), SphericalToVector(190, 12),
                SphericalToVector(185, 22), SphericalToVector(195, 26)
            });

            // Scorpius
            list.Add(new Vector3[] {
                SphericalToVector(300, -20), SphericalToVector(308, -25), SphericalToVector(315, -30),
                SphericalToVector(320, -28), SphericalToVector(322, -20), SphericalToVector(318, -15)
            });

            // Taurus
            list.Add(new Vector3[] {
                SphericalToVector(80, 15), SphericalToVector(88, 22), SphericalToVector(95, 18),
                SphericalToVector(102, 25), SphericalToVector(95, 18), SphericalToVector(85, 8)
            });

            // Pegasus
            list.Add(new Vector3[] {
                SphericalToVector(350, 15), SphericalToVector(10, 20), SphericalToVector(15, 35),
                SphericalToVector(355, 30), SphericalToVector(350, 15)
            });

            return list;
        }

        private Vector3 SphericalToVector(float longitudeDeg, float latitudeDeg)
        {
            float lonRad = longitudeDeg * Mathf.Deg2Rad;
            float latRad = latitudeDeg * Mathf.Deg2Rad;

            float x = Mathf.Cos(latRad) * Mathf.Cos(lonRad);
            float y = Mathf.Sin(latRad);
            float z = Mathf.Cos(latRad) * Mathf.Sin(lonRad);

            return new Vector3(x, y, z).normalized;
        }
    }
}
