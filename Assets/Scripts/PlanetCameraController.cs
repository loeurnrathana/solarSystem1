using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MarsExplorer
{
    public class PlanetCameraController : MonoBehaviour
    {
        [Header("Orbit Target & Distance")]
        public Transform target;
        public float distance = 12f;
        public float minDistance = 5f;
        public float maxDistance = 30f;

        [Header("Orbit Speed & Limits")]
        public float xSpeed = 150.0f;
        public float ySpeed = 150.0f;
        public float zoomSpeed = 8.0f;
        public float yMinLimit = -85f;
        public float yMaxLimit = 85f;

        [Header("Flow Motion Damping")]
        public float rotationSmoothness = 12f;
        public float zoomSmoothness = 10f;
        public float targetSmoothness = 6f;

        private float targetX = 0.0f;
        private float targetY = 0.0f;
        private float currentX = 0.0f;
        private float currentY = 0.0f;

        private float targetDistance = 12f;
        private float currentDistance = 12f;

        private Vector3 targetFocusPoint;
        private Vector3 currentFocusPoint;
        private bool isLerpingToTarget = false;

        private void Start()
        {
            Vector3 angles = transform.eulerAngles;
            targetX = currentX = angles.y;
            targetY = currentY = angles.x;
            targetDistance = currentDistance = distance;

            if (target != null)
            {
                targetFocusPoint = currentFocusPoint = target.position;
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            if (IsRightPressed())
            {
                Vector2 delta = GetDelta();
                targetX += delta.x * xSpeed * 0.02f;
                targetY -= delta.y * ySpeed * 0.02f;
                targetY = ClampAngle(targetY, yMinLimit, yMaxLimit);
                isLerpingToTarget = false;
            }

            float scroll = GetScroll();
            if (Mathf.Abs(scroll) > 0.001f)
            {
                targetDistance = Mathf.Clamp(targetDistance - scroll * zoomSpeed, minDistance, maxDistance);
            }
            distance = targetDistance;

            // Apply smooth flow motion damping to angles and distance
            float dt = Time.deltaTime;
            currentX = Mathf.LerpAngle(currentX, targetX, dt * rotationSmoothness);
            currentY = Mathf.Lerp(currentY, targetY, dt * rotationSmoothness);
            currentDistance = Mathf.Lerp(currentDistance, targetDistance, dt * zoomSmoothness);

            Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);

            if (target != null)
            {
                targetFocusPoint = target.position;
            }

            float lerpSpeed = isLerpingToTarget ? targetSmoothness : 20f;
            currentFocusPoint = Vector3.Lerp(currentFocusPoint, targetFocusPoint, dt * lerpSpeed);

            Vector3 negDistance = new Vector3(0.0f, 0.0f, -currentDistance);
            Vector3 position = rotation * negDistance + currentFocusPoint;

            transform.rotation = rotation;
            transform.position = position;
        }

        public void FocusOnTarget(Transform newTarget)
        {
            target = newTarget;
            isLerpingToTarget = true;
        }

        public void FocusOnPosition(Vector3 pos)
        {
            targetFocusPoint = pos;
            isLerpingToTarget = true;
        }

        private float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360F) angle += 360F;
            if (angle > 360F) angle -= 360F;
            return Mathf.Clamp(angle, min, max);
        }

        private bool IsRightPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.rightButton.isPressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
            try { return Input.GetMouseButton(1); } catch { return false; }
#else
            return false;
#endif
        }

        private Vector2 GetDelta()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null ? Mouse.current.delta.ReadValue() * 0.1f : Vector2.zero;
#elif ENABLE_LEGACY_INPUT_MANAGER
            try { return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")); } catch { return Vector2.zero; }
#else
            return Vector2.zero;
#endif
        }

        private float GetScroll()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null ? Mouse.current.scroll.ReadValue().y * 0.001f : 0f;
#elif ENABLE_LEGACY_INPUT_MANAGER
            try { return Input.GetAxis("Mouse ScrollWheel"); } catch { return 0f; }
#else
            return 0f;
#endif
        }
    }
}
