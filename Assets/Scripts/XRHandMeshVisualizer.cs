using UnityEngine;

namespace SolarSystemScope
{
    /// <summary>
    /// Generates and renders a 3D Holographic Glowing XR Hand Model with joint nodes and index finger pointer (matching VR hand tracking visualization).
    /// </summary>
    public class XRHandMeshVisualizer : MonoBehaviour
    {
        public bool isLeftHand = false;
        public bool showHandMesh = false; // Default false to prevent giant hand debug meshes from blocking camera view
        public Color handGlowColor = new Color(0.20f, 0.68f, 0.95f, 1.0f); // Vibrant smooth cyan VR hand color from photo
        public Color jointNodeColor = new Color(0.28f, 0.78f, 1.0f, 1.0f);

        private GameObject handMeshRoot;
        private Transform indexTipTransform;

        private void Start()
        {
            Build3DHolographicHandMesh();
            if (handMeshRoot != null)
            {
                handMeshRoot.SetActive(showHandMesh);
            }
        }

        public void SetHandMeshVisible(bool visible)
        {
            showHandMesh = visible;
            if (handMeshRoot != null)
            {
                handMeshRoot.SetActive(visible);
            }
        }

        private void Build3DHolographicHandMesh()
        {
            if (handMeshRoot != null) return;

            handMeshRoot = new GameObject(name + "_3DMeshRoot");
            handMeshRoot.transform.SetParent(transform, false);

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            Material handMat = new Material(unlitShader);
            ConfigureTransparentGlowMaterial(handMat, handGlowColor);

            Material jointMat = new Material(unlitShader);
            ConfigureTransparentGlowMaterial(jointMat, jointNodeColor);

            // 1. Palm Core Mesh
            GameObject palm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            palm.name = "PalmCore";
            palm.transform.SetParent(handMeshRoot.transform, false);
            palm.transform.localScale = new Vector3(0.08f, 0.02f, 0.09f);
            palm.transform.localPosition = new Vector3(0f, 0f, 0f);
            SafeDestroy(palm.GetComponent<Collider>());
            palm.GetComponent<Renderer>().material = handMat;

            // Wrist Base
            GameObject wrist = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wrist.name = "WristBase";
            wrist.transform.SetParent(handMeshRoot.transform, false);
            wrist.transform.localScale = new Vector3(0.07f, 0.025f, 0.07f);
            wrist.transform.localPosition = new Vector3(0f, 0f, -0.06f);
            SafeDestroy(wrist.GetComponent<Collider>());
            wrist.GetComponent<Renderer>().material = handMat;

            // 2. Build 5 3D Fingers with Joint Spheres
            // Index Finger (Extended Pointing)
            indexTipTransform = CreateFinger(handMeshRoot.transform, handMat, jointMat, "IndexFinger", new Vector3(0.025f, 0f, 0.045f), new Vector3(0f, 0f, 1f), 0.08f, true);

            // Middle Finger
            CreateFinger(handMeshRoot.transform, handMat, jointMat, "MiddleFinger", new Vector3(0.008f, 0f, 0.045f), new Vector3(0f, -15f, 0.8f), 0.065f, false);

            // Ring Finger
            CreateFinger(handMeshRoot.transform, handMat, jointMat, "RingFinger", new Vector3(-0.010f, 0f, 0.042f), new Vector3(0f, -25f, 0.7f), 0.06f, false);

            // Pinky Finger
            CreateFinger(handMeshRoot.transform, handMat, jointMat, "PinkyFinger", new Vector3(-0.028f, 0f, 0.038f), new Vector3(0f, -35f, 0.6f), 0.05f, false);

            // Thumb (Angled out)
            float thumbSide = isLeftHand ? -0.04f : 0.04f;
            CreateFinger(handMeshRoot.transform, handMat, jointMat, "Thumb", new Vector3(thumbSide, 0f, 0.01f), new Vector3(0f, isLeftHand ? -45f : 45f, 0.5f), 0.055f, false);

            handMeshRoot.transform.localScale = Vector3.one * 1.0f;

            // 3. Laser Pointer Beam Line from Index Finger Tip to 3D Space
            if (indexTipTransform != null)
            {
                GameObject pointerRayObj = new GameObject("HandPointerRay");
                pointerRayObj.transform.SetParent(indexTipTransform, false);
                LineRenderer lr = pointerRayObj.AddComponent<LineRenderer>();
                lr.useWorldSpace = false;
                lr.startWidth = 0.04f;
                lr.endWidth = 0.01f;
                lr.positionCount = 2;
                lr.SetPosition(0, Vector3.zero);
                lr.SetPosition(1, new Vector3(0f, 0f, 100f));
                lr.material = handMat;
            }
        }

        private Transform CreateFinger(Transform parent, Material bodyMat, Material jointMat, string fingerName, Vector3 rootOffset, Vector3 direction, float length, bool isPointing)
        {
            GameObject fingerRoot = new GameObject(fingerName);
            fingerRoot.transform.SetParent(parent, false);
            fingerRoot.transform.localPosition = rootOffset;

            int segments = 3;
            float segLen = length / segments;
            Vector3 currentPos = Vector3.zero;
            Transform lastJoint = fingerRoot.transform;

            for (int i = 0; i < segments; i++)
            {
                // Joint Node Sphere (Red VR keypoint dot)
                GameObject jointObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                jointObj.name = fingerName + "_Joint_" + i;
                jointObj.transform.SetParent(fingerRoot.transform, false);
                jointObj.transform.localPosition = currentPos;
                jointObj.transform.localScale = Vector3.one * 0.014f;
                SafeDestroy(jointObj.GetComponent<Collider>());
                jointObj.GetComponent<Renderer>().material = jointMat;

                // Bone Capsule Segment
                GameObject boneObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                boneObj.name = fingerName + "_Bone_" + i;
                boneObj.transform.SetParent(fingerRoot.transform, false);
                boneObj.transform.localPosition = currentPos + direction.normalized * (segLen * 0.5f);
                boneObj.transform.localScale = new Vector3(0.010f, segLen * 0.45f, 0.010f);
                boneObj.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
                SafeDestroy(boneObj.GetComponent<Collider>());
                boneObj.GetComponent<Renderer>().material = bodyMat;

                currentPos += direction.normalized * segLen;
                lastJoint = jointObj.transform;
            }

            // Fingertip Node
            GameObject tipObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tipObj.name = fingerName + "_Tip";
            tipObj.transform.SetParent(fingerRoot.transform, false);
            tipObj.transform.localPosition = currentPos;
            tipObj.transform.localScale = Vector3.one * 0.012f;
            SafeDestroy(tipObj.GetComponent<Collider>());
            tipObj.GetComponent<Renderer>().material = jointMat;

            return tipObj.transform;
        }

        private void ConfigureTransparentGlowMaterial(Material mat, Color col)
        {
            mat.color = col;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", col);

            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 0); // Solid opaque visibility
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 1);
            mat.renderQueue = 2000;
        }

        private void SafeDestroy(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }
    }
}
