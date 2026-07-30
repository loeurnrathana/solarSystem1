using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MarsExplorer
{
    public class PlanetRotator : MonoBehaviour
    {
        [Header("Auto Rotation")]
        public float autoRotationSpeed = 10f;
        public float speedMultiplier = 1.0f;
        public bool isAutoRotating = true;
        public bool isRotating { get => isAutoRotating; set => isAutoRotating = value; }
        public bool isPrograde = true;

        [Header("Axial Tilt")]
        public float axialTilt = 25.19f;

        [Header("Direct Drag Rotation")]
        public bool allowDirectDrag = true;
        public float dragSensitivity = 0.4f;
        public float inertiaDamping = 0.93f;

        private Vector2 lastMousePos;
        private Vector2 dragVelocity;
        private bool isDragging = false;
        private float idleTimer = 0f;
        private float currentAutoSpinSpeed = 0f;

        private void Start()
        {
            transform.rotation = Quaternion.Euler(0, 0, axialTilt);
        }

        private void Update()
        {
            HandleMouseDragInput();

            if (isDragging)
            {
                transform.Rotate(Vector3.up, -dragVelocity.x * dragSensitivity, Space.Self);
                transform.Rotate(Vector3.right, dragVelocity.y * dragSensitivity, Space.World);
                idleTimer = 0f;
                currentAutoSpinSpeed = 0f;
            }
            else
            {
                if (dragVelocity.magnitude > 0.01f)
                {
                    transform.Rotate(Vector3.up, -dragVelocity.x * dragSensitivity, Space.Self);
                    transform.Rotate(Vector3.right, dragVelocity.y * dragSensitivity, Space.World);
                    dragVelocity *= inertiaDamping;
                    idleTimer = 0f;
                    currentAutoSpinSpeed = 0f;
                }
                else
                {
                    idleTimer += Time.deltaTime;
                    if (isAutoRotating && idleTimer > 1.0f)
                    {
                        float dir = isPrograde ? 1.0f : -1.0f;
                        float targetSpeed = autoRotationSpeed * speedMultiplier * dir;
                        currentAutoSpinSpeed = Mathf.Lerp(currentAutoSpinSpeed, targetSpeed, Time.deltaTime * 2.0f);
                        transform.Rotate(Vector3.up, currentAutoSpinSpeed * Time.deltaTime, Space.Self);
                    }
                }
            }
        }

        private void HandleMouseDragInput()
        {
            if (!allowDirectDrag) return;

            Vector2 curMouse = GetMousePos();

            if (IsMouseDown())
            {
                isDragging = true;
                lastMousePos = curMouse;
                dragVelocity = Vector2.zero;
            }
            else if (IsMousePressed() && isDragging)
            {
                Vector2 rawDelta = curMouse - lastMousePos;
                dragVelocity = Vector2.Lerp(dragVelocity, rawDelta, Time.deltaTime * 30f);
                lastMousePos = curMouse;
            }
            else if (IsMouseUp())
            {
                isDragging = false;
            }
        }

        private Vector2 GetMousePos()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#elif ENABLE_LEGACY_INPUT_MANAGER
            try { return Input.mousePosition; } catch { return Vector2.zero; }
#else
            return Vector2.zero;
#endif
        }

        private bool IsMouseDown()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            try { return Input.GetMouseButtonDown(0); } catch { return false; }
#else
            return false;
#endif
        }

        private bool IsMousePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.isPressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
            try { return Input.GetMouseButton(0); } catch { return false; }
#else
            return false;
#endif
        }

        private bool IsMouseUp()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            try { return Input.GetMouseButtonUp(0); } catch { return false; }
#else
            return false;
#endif
        }

        public void TogglePause() { isAutoRotating = !isAutoRotating; }
        public void SetSpeedMultiplier(float speed) { speedMultiplier = Mathf.Max(0.05f, speed); }
        public void ToggleDirection() { isPrograde = !isPrograde; }
        public float GetCurrentYAngle()
        {
            float y = transform.localEulerAngles.y;
            return y < 0 ? y + 360f : y;
        }
    }
}
