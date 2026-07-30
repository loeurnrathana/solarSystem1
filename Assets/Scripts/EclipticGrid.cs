using UnityEngine;

namespace SolarSystemScope
{
    public class EclipticGrid : MonoBehaviour
    {
        public int circleCount = 10;
        public float maxRadius = 185f;
        public int radialSpokes = 36;
        public Color gridColor = new Color(0.10f, 0.45f, 0.75f, 0.35f);

        private void Start()
        {
            GameObject gridRoot = new GameObject("EclipticGridRoot");
            gridRoot.transform.SetParent(transform, false);

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            Material gridMat = new Material(unlitShader);
            gridMat.color = gridColor;
            if (gridMat.HasProperty("_BaseColor")) gridMat.SetColor("_BaseColor", gridColor);
            if (gridMat.HasProperty("_Color")) gridMat.SetColor("_Color", gridColor);
            
            if (gridMat.HasProperty("_Surface")) gridMat.SetFloat("_Surface", 1);
            if (gridMat.HasProperty("_Blend")) gridMat.SetFloat("_Blend", 1);
            gridMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            gridMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            gridMat.SetInt("_ZWrite", 0);
            gridMat.renderQueue = 3000;

            // 1. Concentric Grid Circles
            float[] radii = new float[] { 20f, 28f, 38f, 50f, 64f, 80f, 98f, 126f, 152f, 178f };
            foreach (float r in radii)
            {
                GameObject circleObj = new GameObject("GridCircle_" + r);
                circleObj.transform.SetParent(gridRoot.transform, false);
                LineRenderer lr = circleObj.AddComponent<LineRenderer>();
                lr.useWorldSpace = false;
                lr.startWidth = 0.05f;
                lr.endWidth = 0.05f;
                lr.material = gridMat;

                int segments = 160;
                lr.positionCount = segments + 1;
                for (int i = 0; i <= segments; i++)
                {
                    float rad = (i / (float)segments) * Mathf.PI * 2f;
                    Vector3 pos = new Vector3(Mathf.Cos(rad) * r, 0f, Mathf.Sin(rad) * r);
                    lr.SetPosition(i, pos);
                }
            }

            // 2. Radial Grid Spokes
            for (int s = 0; s < radialSpokes; s++)
            {
                float angleRad = (s / (float)radialSpokes) * Mathf.PI * 2f;
                GameObject spokeObj = new GameObject("GridSpoke_" + s);
                spokeObj.transform.SetParent(gridRoot.transform, false);
                LineRenderer lr = spokeObj.AddComponent<LineRenderer>();
                lr.useWorldSpace = false;
                lr.startWidth = 0.04f;
                lr.endWidth = 0.04f;
                lr.material = gridMat;
                lr.positionCount = 2;

                Vector3 innerPos = new Vector3(Mathf.Cos(angleRad) * 12f, 0f, Mathf.Sin(angleRad) * 12f);
                Vector3 outerPos = new Vector3(Mathf.Cos(angleRad) * maxRadius, 0f, Mathf.Sin(angleRad) * maxRadius);

                lr.SetPosition(0, innerPos);
                lr.SetPosition(1, outerPos);
            }
        }
    }
}
