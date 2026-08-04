using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SolarSystemScope
{
    public class SolarSystemCameraController : MonoBehaviour
    {
        [Header("Targeting")]
        public Transform targetTarget;
        public float smoothTime = 0.04f;

        [Header("Distance & Zoom")]
        public float distance = 160f;
        public float minDistance = 2f;
        public float maxDistance = 900f;
        public float zoomSensitivity = 80f;

        [Header("Rotation Settings")]
        public float rotationSensitivity = 0.65f;
        public float minVerticalAngle = 5f;
        public float maxVerticalAngle = 88f;

        private float currentYaw = -15f;
        private float currentPitch = 25f;

        private Vector3 currentVelocity = Vector3.zero;
        private Vector3 panOffset = Vector3.zero;

        private Vector2 mouseDownPosition;
        private bool isMouseDown = false;

        private Camera cam;
        private CelestialBody nearbyBody = null;
        private GameObject proximityUIPrompt = null;
        private UnityEngine.UI.Text promptTextComponent = null;

        private Transform cachedXRHand = null;
        private float nextHandSearchTime = 0f;

        private void Start()
        {
            currentYaw = -15f;
            distance = 140f;
            panOffset = Vector3.zero;

            if (targetTarget == null)
            {
                GameObject sunObj = GameObject.Find("Sun");
                if (sunObj != null) targetTarget = sunObj.transform;
            }

            // Immediately snap camera position & angle to exact screenshot overview on Play frame 0!
            if (targetTarget != null)
            {
                Vector3 focalPoint = targetTarget.position + panOffset;
                Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
                transform.position = focalPoint - (rotation * Vector3.forward * distance);
                transform.LookAt(focalPoint);
                currentVelocity = Vector3.zero;
            }

            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            CreateProximityUI();
        }

        private void CreateProximityUI()
        {
            if (proximityUIPrompt != null) return;

            // Find existing or create lightweight runtime UI Canvas for prompt
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("ProximityUICanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }

            GameObject panel = new GameObject("PlanetExplorePrompt");
            panel.transform.SetParent(canvas.transform, false);
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.15f);
            rect.anchorMax = new Vector2(0.5f, 0.15f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(400f, 60f);

            UnityEngine.UI.Image bg = panel.AddComponent<UnityEngine.UI.Image>();
            bg.color = new Color(0.05f, 0.08f, 0.15f, 0.85f);

            GameObject textObj = new GameObject("PromptText");
            textObj.transform.SetParent(panel.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            promptTextComponent = textObj.AddComponent<UnityEngine.UI.Text>();
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            promptTextComponent.font = font;
            promptTextComponent.fontSize = 22;
            promptTextComponent.alignment = TextAnchor.MiddleCenter;
            promptTextComponent.color = new Color(0.3f, 0.9f, 1.0f);
            promptTextComponent.fontStyle = FontStyle.Bold;

            proximityUIPrompt = panel;
            proximityUIPrompt.SetActive(false);
        }

        private void CheckProximityAndInput()
        {
            CelestialBody targetBody = null;

            if (targetTarget != null)
            {
                CelestialBody b = targetTarget.GetComponent<CelestialBody>();
                if (b != null && b.bodyName != "Sun")
                {
                    targetBody = b;
                }
            }

            if (targetBody == null)
            {
                float closestDist = float.MaxValue;
                Vector3 camPos = transform.position;

                CelestialBody[] bodies = Object.FindObjectsByType<CelestialBody>(FindObjectsInactive.Exclude);
                foreach (var body in bodies)
                {
                    if (body == null || !IsSelectablePlanet(body) || body.bodyName == "Sun") continue;
                    if (body.orbitCenter != null && body.orbitCenter.name != "Sun") continue;

                    float dist = Vector3.Distance(camPos, body.transform.position);
                    float radiusThreshold = Mathf.Max(body.transform.localScale.y * 6.0f, 65f);

                    if (dist <= radiusThreshold && dist < closestDist)
                    {
                        closestDist = dist;
                        targetBody = body;
                    }
                }
            }

            nearbyBody = targetBody;

            // Detect E key press to enter planet surface ONLY if a planet is selected or nearby!
            if (WasEKeyPressedThisFrame())
            {
                CelestialBody bodyToEnter = targetBody;

                if (bodyToEnter != null)
                {
                    float viewDist = Mathf.Max(bodyToEnter.transform.localScale.y * 3.5f, 25f);
                    SetTarget(bodyToEnter.transform, viewDist);
                    if (proximityUIPrompt != null) proximityUIPrompt.SetActive(false);
                    Debug.Log($"<color=cyan>[Planet Explorer] Entering surface of {bodyToEnter.bodyName}!</color>");

                    if (PlanetLabelManager.Instance != null)
                    {
                        PlanetLabelManager.Instance.SetFocusedBody(null);
                    }

                    if (PlanetSurfaceExplorer.Instance == null)
                    {
                        GameObject explorerObj = new GameObject("PlanetSurfaceExplorerManager");
                        explorerObj.AddComponent<PlanetSurfaceExplorer>();
                    }
                    if (PlanetSurfaceExplorer.Instance != null)
                    {
                        PlanetSurfaceExplorer.Instance.EnterPlanetSurface(bodyToEnter);
                    }
                }
            }
        }

        private void LateUpdate()
        {
            if (targetTarget == null) return;

            // 0. Automatic Proximity Check & Press E to Explore
            CheckProximityAndInput();

            // 1. Mouse Click vs Drag Selection
            if (WasMouseDownThisFrame())
            {
                mouseDownPosition = GetMousePos();
                isMouseDown = true;
            }

            if (WasMouseReleasedThisFrame() && isMouseDown)
            {
                isMouseDown = false;
                Vector2 mouseUpPos = GetMousePos();
                float dragDist = Vector2.Distance(mouseUpPos, mouseDownPosition);
                
                // Only select planet if click released within 8 pixels (not a drag action)
                if (dragDist < 8.0f)
                {
                    Check3DPlanetClick(mouseUpPos);
                }
            }

            // 1b. XR Hand Pointer & Poke Raycast Interactivity
            CheckXRHandInteraction();

            // 2. Drag Orbit (Left Mouse Drag, Right Mouse Drag)
            if (IsDragActive())
            {
                Vector2 mouseDelta = GetMouseDelta();
                currentYaw += mouseDelta.x * rotationSensitivity;
                currentPitch -= mouseDelta.y * rotationSensitivity;
                currentPitch = Mathf.Clamp(currentPitch, minVerticalAngle, maxVerticalAngle);
            }

            // 2b. Arrow Key Turning & Rotation
            Vector2 arrowRot = GetArrowRotation();
            if (arrowRot.sqrMagnitude > 0.001f)
            {
                currentYaw += arrowRot.x * rotationSensitivity * 60f * Time.deltaTime;
                currentPitch += arrowRot.y * rotationSensitivity * 60f * Time.deltaTime;
                currentPitch = Mathf.Clamp(currentPitch, minVerticalAngle, maxVerticalAngle);
            }

            // 3. Scroll Zoom
            float scroll = GetScrollDelta();
            if (Mathf.Abs(scroll) > 0.0001f)
            {
                distance -= scroll * zoomSensitivity * (distance * 0.12f + 1f);
                distance = Mathf.Clamp(distance, minDistance, maxDistance);
            }

            // 4. Keyboard 3D Flying (W for forward flight, S for backward, Space for up, Shift for Turbo Flight)
            Vector3 move = GetKeyboardMove();
            if (move.sqrMagnitude > 0.001f)
            {
                bool isTurbo = IsTurboPressed();
                float flySpeedMult = isTurbo ? 3.5f : 1.0f;
                float moveSpeed = (distance * 0.85f + 18f) * flySpeedMult * Time.deltaTime;

                Vector3 fwd = transform.forward;
                Vector3 right = transform.right;
                Vector3 up = transform.up;

                Vector3 moveDir = (right * move.x + up * move.y + fwd * move.z) * moveSpeed;

                // Move focal point & zoom distance forward smoothly towards looking direction!
                panOffset += moveDir;

                if (move.z > 0f && distance > minDistance)
                {
                    distance = Mathf.Max(minDistance, distance - move.z * moveSpeed * 0.5f);
                }
                else if (move.z < 0f && distance < maxDistance)
                {
                    distance = Mathf.Min(maxDistance, distance - move.z * moveSpeed * 0.5f);
                }
            }

            // Calculate Position & LookAt relative to focal point
            Vector3 focalPoint = targetTarget.position + panOffset;
            Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
            Vector3 desiredPosition = focalPoint - (rotation * Vector3.forward * distance);

            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, smoothTime);
            transform.LookAt(focalPoint);
        }

        public void SetTarget(Transform newTarget, float newDistance = -1f, bool instantSnap = false)
        {
            targetTarget = newTarget;
            panOffset = Vector3.zero;
            if (newDistance > 0f)
            {
                distance = newDistance;
            }
            if (instantSnap && targetTarget != null)
            {
                Vector3 focalPoint = targetTarget.position + panOffset;
                Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
                transform.position = focalPoint - (rotation * Vector3.forward * distance);
                transform.LookAt(focalPoint);
                currentVelocity = Vector3.zero;
            }
        }

        private bool IsSelectablePlanet(CelestialBody body)
        {
            if (body == null) return false;
            if (string.IsNullOrEmpty(body.bodyName)) return false;
            string nameLower = body.bodyName.ToLower();
            if (nameLower.Contains("asteroid") || nameLower.Contains("belt")) return false;
            return true;
        }

        public void SelectCelestialBody(CelestialBody body)
        {
            if (!IsSelectablePlanet(body) && (body == null || body.bodyName != "Sun")) return;

            bool isSameTarget = (targetTarget == body.transform);

            targetTarget = body.transform;
            panOffset = Vector3.zero;

            if (!isSameTarget)
            {
                float worldScaleY = body.transform.lossyScale.y;
                float viewDist = 18f;
                if (body.bodyName == "Sun") viewDist = 100f;
                else if (body.bodyName == "Jupiter" || body.bodyName == "Saturn") viewDist = 26f;
                else if (body.bodyName == "Earth" || body.bodyName == "Mars" || body.bodyName == "Venus" || body.bodyName == "Mercury") viewDist = 16f;
                else if (body.orbitCenter != null && body.orbitCenter.name != "Sun")
                {
                    viewDist = Mathf.Max(worldScaleY * 3.0f, 4.5f);
                }
                else
                {
                    viewDist = Mathf.Max(worldScaleY * 2.2f, 15f);
                }

                distance = viewDist;
            }

            if (PlanetLabelManager.Instance != null)
            {
                PlanetLabelManager.Instance.SetFocusedBody(body);
            }

            Debug.Log($"<color=cyan>[Planet Explorer] Smoothly zooming in to {body.bodyName} (Distance: {distance})!</color>");
        }

        public void DeselectPlanet()
        {
            Transform sunTransform = null;
            GameObject sunObj = GameObject.Find("Sun");
            if (sunObj != null) sunTransform = sunObj.transform;

            if (sunTransform != null && targetTarget != null)
            {
                // Lock current camera focal point in space without changing distance or zooming to Sun!
                Vector3 currentFocalPoint = targetTarget.position + panOffset;
                targetTarget = sunTransform;
                panOffset = currentFocalPoint - sunTransform.position;
                // Keep current distance unchanged!
            }

            if (PlanetLabelManager.Instance != null)
            {
                PlanetLabelManager.Instance.SetFocusedBody(null);
            }
            else
            {
#pragma warning disable 0618
                PlanetLabelManager labelMgr = Object.FindObjectOfType<PlanetLabelManager>();
#pragma warning restore 0618
                if (labelMgr != null) labelMgr.SetFocusedBody(null);
            }
        }

        private void Check3DPlanetClick(Vector2 mousePos)
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            // Prevent clicking through UI elements
            if (UnityEngine.EventSystems.EventSystem.current != null && 
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            Ray ray = cam.ScreenPointToRay(mousePos);

            // 1. Direct 3D Raycast against all celestial body sphere colliders
            RaycastHit[] hits = Physics.RaycastAll(ray, 5000f);
            CelestialBody hitNonSunBody = null;
            CelestialBody hitSunBody = null;
            float minNonSunDist = float.MaxValue;

            foreach (var hit in hits)
            {
                CelestialBody body = hit.collider.GetComponentInParent<CelestialBody>();
                if (body != null && IsSelectablePlanet(body))
                {
                    if (body.bodyName == "Sun")
                    {
                        hitSunBody = body;
                    }
                    else if (hit.distance < minNonSunDist)
                    {
                        minNonSunDist = hit.distance;
                        hitNonSunBody = body;
                    }
                }
            }

            // 2. Accurate Screen-Space Proximity Detection (For clicking floating text labels or near planet spheres)
            CelestialBody[] allBodies = Object.FindObjectsByType<CelestialBody>(FindObjectsInactive.Exclude);
            CelestialBody bestScreenNonSunBody = null;
            float minScreenNonSunDist = float.MaxValue;
            float maxClickPixelRadius = 85f; // Max pixel distance on screen allowed for selection click

            CelestialBody bestScreenSunBody = null;
            float minScreenSunDist = float.MaxValue;

            foreach (var body in allBodies)
            {
                if (body == null || !IsSelectablePlanet(body)) continue;

                Vector3 bodyWorldPos = body.transform.position;
                float baseRadius = body.transform.lossyScale.y * 0.5f;
                Vector3 labelWorldPos = bodyWorldPos + Vector3.up * (baseRadius + 1.5f);

                Vector3 screenPosPlanet = cam.WorldToScreenPoint(bodyWorldPos);
                Vector3 screenPosLabel = cam.WorldToScreenPoint(labelWorldPos);

                // Planet must be in front of the camera frustum (screenPos.z > 0)
                if (screenPosPlanet.z > 0f)
                {
                    float distPlanetPx = Vector2.Distance(mousePos, new Vector2(screenPosPlanet.x, screenPosPlanet.y));
                    float distLabelPx = screenPosLabel.z > 0f ? Vector2.Distance(mousePos, new Vector2(screenPosLabel.x, screenPosLabel.y)) : float.MaxValue;

                    float bestDistPx = Mathf.Min(distPlanetPx, distLabelPx);

                    if (body.bodyName == "Sun")
                    {
                        if (bestDistPx <= 30f && bestDistPx < minScreenSunDist)
                        {
                            minScreenSunDist = bestDistPx;
                            bestScreenSunBody = body;
                        }
                    }
                    else
                    {
                        if (bestDistPx <= maxClickPixelRadius && bestDistPx < minScreenNonSunDist)
                        {
                            minScreenNonSunDist = bestDistPx;
                            bestScreenNonSunBody = body;
                        }
                    }
                }
            }

            // Priority 1: Direct 3D Raycast hit on a non-Sun planet
            if (hitNonSunBody != null)
            {
                SelectCelestialBody(hitNonSunBody);
                return;
            }

            // Priority 2: Screen-space proximity to a non-Sun planet or its label
            if (bestScreenNonSunBody != null)
            {
                SelectCelestialBody(bestScreenNonSunBody);
                return;
            }

            // Priority 3: Direct hit or screen proximity to Sun (ONLY if no non-Sun planet was targeted)
            CelestialBody sunToSelect = (hitSunBody != null) ? hitSunBody : bestScreenSunBody;
            if (sunToSelect != null)
            {
                SelectCelestialBody(sunToSelect);
            }
        }

        private void CheckXRHandInteraction()
        {
            Transform handTransform = cachedXRHand;
            if (handTransform == null && Time.time >= nextHandSearchTime)
            {
                nextHandSearchTime = Time.time + 1.5f;
                GameObject rHand = GameObject.Find("RightHand") ?? GameObject.Find("RightHand Controller") ?? GameObject.Find("RightHandDirectInteractor");
                GameObject lHand = GameObject.Find("LeftHand") ?? GameObject.Find("LeftHand Controller") ?? GameObject.Find("LeftHandDirectInteractor");
                cachedXRHand = rHand != null ? rHand.transform : (lHand != null ? lHand.transform : null);
                handTransform = cachedXRHand;
            }

            if (handTransform == null && cam == null) return;

            Ray ray = (handTransform != null) ? new Ray(handTransform.position, handTransform.forward) : cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit[] hits = Physics.RaycastAll(ray, 5000f);
            CelestialBody hitNonSunBody = null;
            CelestialBody hitSunBody = null;
            float minNonSunDist = float.MaxValue;

            foreach (var hit in hits)
            {
                CelestialBody body = hit.collider.GetComponentInParent<CelestialBody>();
                if (body != null && IsSelectablePlanet(body))
                {
                    if (body.bodyName == "Sun")
                    {
                        hitSunBody = body;
                    }
                    else if (hit.distance < minNonSunDist)
                    {
                        minNonSunDist = hit.distance;
                        hitNonSunBody = body;
                    }
                }
            }

            CelestialBody targetBody = (hitNonSunBody != null) ? hitNonSunBody : hitSunBody;
            if (targetBody != null && (WasMouseDownThisFrame() || WasXRTriggerPressedThisFrame()))
            {
                SelectCelestialBody(targetBody);
            }
        }

        private bool WasXRTriggerPressedThisFrame()
        {
            try
            {
                var rDevice = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);
                if (rDevice.isValid && rDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out bool pressed) && pressed) return true;
                var lDevice = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand);
                if (lDevice.isValid && lDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out bool lPressed) && lPressed) return true;
            }
            catch {}
            return false;
        }

        private bool WasEKeyPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            try { if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) return true; } catch {}
#endif
            try { if (Input.GetKeyDown(KeyCode.E)) return true; } catch { }
            return false;
        }

        private bool WasMouseDownThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            try { if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true; } catch {}
#endif
            try { return Input.GetMouseButtonDown(0); } catch { return false; }
        }

        private bool WasMouseReleasedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            try { if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame) return true; } catch {}
#endif
            try { return Input.GetMouseButtonUp(0); } catch { return false; }
        }

        private bool IsDragActive()
        {
#if ENABLE_INPUT_SYSTEM
            try { if (Mouse.current != null && (Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed)) return true; } catch {}
#endif
            try { return Input.GetMouseButton(0) || Input.GetMouseButton(1); } catch { return false; }
        }

        private Vector2 GetMousePos()
        {
#if ENABLE_INPUT_SYSTEM
            try { if (Mouse.current != null) return Mouse.current.position.ReadValue(); } catch {}
#endif
            try { return Input.mousePosition; } catch { return Vector2.zero; }
        }

        private Vector2 GetMouseDelta()
        {
#if ENABLE_INPUT_SYSTEM
            try { if (Mouse.current != null) return Mouse.current.delta.ReadValue() * 0.45f; } catch {}
#endif
            try { return new Vector2(Input.GetAxis("Mouse X") * 15f, Input.GetAxis("Mouse Y") * 15f); } catch { return Vector2.zero; }
        }

        private float GetScrollDelta()
        {
#if ENABLE_INPUT_SYSTEM
            try { if (Mouse.current != null) return Mouse.current.scroll.ReadValue().y * 0.008f; } catch {}
#endif
            try { return Input.GetAxis("Mouse ScrollWheel"); } catch { return 0f; }
        }

        private Vector3 GetKeyboardMove()
        {
            float x = 0f;
            float y = 0f;
            float z = 0f;

#if ENABLE_INPUT_SYSTEM
            try
            {
                if (Keyboard.current != null)
                {
                    if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) z += 1f;
                    if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) z -= 1f;
                    if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
                    if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;
                    if (Keyboard.current.spaceKey.isPressed) y += 1f;
                    if (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.cKey.isPressed) y -= 1f;
                }
            }
            catch {}
#endif

            try
            {
                if (z == 0f && x == 0f && y == 0f)
                {
                    if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) z += 1f;
                    if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) z -= 1f;
                    if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
                    if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1f;
                    if (Input.GetKey(KeyCode.Space)) y += 1f;
                    if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.C)) y -= 1f;
                }
            }
            catch { }

            return new Vector3(x, y, z).normalized;
        }

        private bool IsTurboPressed()
        {
#if ENABLE_INPUT_SYSTEM
            try { if (Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed)) return true; } catch {}
#endif
            try { if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) return true; } catch { }
            return false;
        }

        private Vector2 GetArrowRotation()
        {
            float yawDelta = 0f;
            float pitchDelta = 0f;

#if ENABLE_INPUT_SYSTEM
            try
            {
                if (Keyboard.current != null)
                {
                    if (Keyboard.current.leftArrowKey.isPressed) yawDelta -= 1f;
                    if (Keyboard.current.rightArrowKey.isPressed) yawDelta += 1f;
                    if (Keyboard.current.upArrowKey.isPressed) pitchDelta -= 1f;
                    if (Keyboard.current.downArrowKey.isPressed) pitchDelta += 1f;
                }
            }
            catch {}
#endif

            try
            {
                if (yawDelta == 0f && pitchDelta == 0f)
                {
                    if (Input.GetKey(KeyCode.LeftArrow)) yawDelta -= 1f;
                    if (Input.GetKey(KeyCode.RightArrow)) yawDelta += 1f;
                    if (Input.GetKey(KeyCode.UpArrow)) pitchDelta -= 1f;
                    if (Input.GetKey(KeyCode.DownArrow)) pitchDelta += 1f;
                }
            }
            catch { }

            return new Vector2(yawDelta, pitchDelta);
        }
    }
}
