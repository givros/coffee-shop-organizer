using UnityEngine;
using UnityEngine.InputSystem;

namespace CoffeeShop
{
    public sealed class PlayerObjectInteraction : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform holdAnchor;

        [Header("Raycasts")]
        [SerializeField, Min(0.1f)] private float pickupRayDistance = 6f;
        [SerializeField, Min(0.1f)] private float placementRayDistance = 8f;
        [SerializeField] private LayerMask interactionMask = Physics.DefaultRaycastLayers;
        [SerializeField, Range(0f, 1f)] private float minimumSurfaceUp = 0.35f;

        [Header("Held Object")]
        [SerializeField] private Vector3 heldLocalPosition = Vector3.zero;
        [SerializeField] private Color aimOutlineColor = Color.white;
        [SerializeField] private Color validPlacementColor = new Color(0.2f, 1f, 0.2f, 1f);
        [SerializeField] private Color invalidPlacementColor = new Color(1f, 0.1f, 0.1f, 1f);
        [SerializeField, Range(0.05f, 0.95f)] private float ghostAlpha = 0.3f;
        [SerializeField, Min(0f)] private float correctFeedbackDuration = 2f;

        private PlaceableObject aimedObject;
        private PlaceableObject heldObject;
        private GameObject placementGhost;
        private OutlineVisualizer ghostOutline;
        private Vector3 ghostPosition;
        private Quaternion ghostRotation;
        private bool ghostSurfaceValid;
        private bool interactionEnabled = true;
        private readonly RaycastHit[] raycastBuffer = new RaycastHit[64];
        private uint lastMobileActionVersion;

        public bool InteractionEnabled => interactionEnabled;
        public bool IsHoldingObject => heldObject != null;

        private void Awake()
        {
            ResolveReferences();
            EnsureHoldAnchor();
        }

        private void Update()
        {
            if (!interactionEnabled ||
                (GameSessionManager.Instance != null && GameSessionManager.Instance.IsPaused))
            {
                ClearAimedObject();
                return;
            }

            if (heldObject == null)
            {
                UpdateAimedObject();

                if (aimedObject != null && WasPrimaryActionPressed())
                {
                    PickUp(aimedObject);
                }

                return;
            }

            UpdateHeldObjectPose();
            ClearAimedObject();
            UpdatePlacementGhost();

            if (WasPrimaryActionPressed())
            {
                PlaceOrDropHeldObject();
            }
        }

        public void SetInteractionEnabled(bool enabled)
        {
            interactionEnabled = enabled;

            if (!enabled)
            {
                ClearAimedObject();
            }
        }

        private void UpdateAimedObject()
        {
            PlaceableObject nextTarget = FindAimedObject();
            if (nextTarget != aimedObject)
            {
                if (aimedObject != null)
                {
                    aimedObject.HideOutline();
                }

                aimedObject = nextTarget;
            }

            if (aimedObject != null && !aimedObject.IsFeedbackActive)
            {
                aimedObject.ShowOutline(aimOutlineColor);
            }
        }

        private PlaceableObject FindAimedObject()
        {
            if (playerCamera == null)
            {
                return null;
            }

            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            int hitCount = Physics.RaycastNonAlloc(
                ray,
                raycastBuffer,
                pickupRayDistance,
                interactionMask,
                QueryTriggerInteraction.Ignore);

            SortHitsByDistance(raycastBuffer, hitCount);
            for (int i = 0; i < hitCount; i++)
            {
                PlaceableObject candidate = raycastBuffer[i].collider.GetComponentInParent<PlaceableObject>();
                if (candidate != null &&
                    !StaticSceneAssetRegistry.IsExcluded(candidate.gameObject) &&
                    candidate.CanBePickedUp)
                {
                    return candidate;
                }
            }

            return null;
        }

        private void PickUp(PlaceableObject objectToPickUp)
        {
            if (objectToPickUp == null || !objectToPickUp.CanBePickedUp || holdAnchor == null)
            {
                return;
            }

            objectToPickUp.ClearFeedback();
            objectToPickUp.SetHeldState(true);
            objectToPickUp.transform.SetParent(holdAnchor, false);
            objectToPickUp.transform.localPosition = heldLocalPosition;
            objectToPickUp.transform.localRotation = Quaternion.Inverse(holdAnchor.rotation) * objectToPickUp.SavedRotation;
            objectToPickUp.transform.localScale = objectToPickUp.SavedScale;

            heldObject = objectToPickUp;
            aimedObject = null;
            CreatePlacementGhost();
        }

        private void UpdateHeldObjectPose()
        {
            if (heldObject == null || holdAnchor == null)
            {
                return;
            }

            heldObject.transform.localPosition = heldLocalPosition;
            heldObject.transform.localRotation = Quaternion.Inverse(holdAnchor.rotation) * heldObject.SavedRotation;
        }

        private void CreatePlacementGhost()
        {
            DestroyPlacementGhost();

            placementGhost = Instantiate(heldObject.gameObject);
            placementGhost.name = $"{heldObject.name}__PlacementGhost";
            placementGhost.transform.SetParent(null, false);

            PlaceableObject ghostPlaceable = placementGhost.GetComponent<PlaceableObject>();
            if (ghostPlaceable != null)
            {
                ghostPlaceable.ExcludeFromProgressTracking();
            }

            ghostOutline = placementGhost.GetComponent<OutlineVisualizer>();
            if (ghostOutline == null)
            {
                ghostOutline = placementGhost.AddComponent<OutlineVisualizer>();
            }

            ApplyGhostTransform(heldObject.transform.position);

            Behaviour[] behaviours = placementGhost.GetComponentsInChildren<Behaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] != null && !(behaviours[i] is OutlineVisualizer))
                {
                    behaviours[i].enabled = false;
                }
            }

            Collider[] colliders = placementGhost.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    colliders[i].enabled = false;
                }
            }

            Renderer[] renderers = placementGhost.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                OutlineVisualizer.MakeRendererTransparent(renderers[i], ghostAlpha);
            }

            if (ghostOutline != null)
            {
                ghostOutline.SetState(aimOutlineColor, false);
            }
        }

        private void UpdatePlacementGhost()
        {
            if (placementGhost == null || heldObject == null)
            {
                return;
            }

            if (!TryFindPlacementSurface(out RaycastHit surfaceHit))
            {
                ghostSurfaceValid = false;
                placementGhost.SetActive(false);
                return;
            }

            ghostSurfaceValid = true;
            placementGhost.SetActive(true);
            ghostPosition = heldObject.GetSurfacePlacementPosition(surfaceHit.point, surfaceHit.normal);
            ApplyGhostTransform(ghostPosition);

            bool isCorrectPlacement = heldObject.IsCorrectPlacement(ghostPosition, ghostRotation);
            if (ghostOutline != null)
            {
                ghostOutline.SetState(
                    isCorrectPlacement ? validPlacementColor : aimOutlineColor,
                    true);
            }
        }

        private bool TryFindPlacementSurface(out RaycastHit surfaceHit)
        {
            surfaceHit = default;
            if (playerCamera == null)
            {
                return false;
            }

            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            int hitCount = Physics.RaycastNonAlloc(
                ray,
                raycastBuffer,
                placementRayDistance,
                interactionMask,
                QueryTriggerInteraction.Ignore);

            SortHitsByDistance(raycastBuffer, hitCount);
            for (int i = 0; i < hitCount; i++)
            {
                Collider candidateCollider = raycastBuffer[i].collider;
                if (candidateCollider == null || IsPlayerCollider(candidateCollider))
                {
                    continue;
                }

                if (candidateCollider.GetComponentInParent<PlaceableObject>() == heldObject)
                {
                    continue;
                }

                if (Vector3.Dot(raycastBuffer[i].normal, Vector3.up) < minimumSurfaceUp)
                {
                    continue;
                }

                surfaceHit = raycastBuffer[i];
                return true;
            }

            return false;
        }

        private void PlaceOrDropHeldObject()
        {
            if (heldObject == null)
            {
                return;
            }

            if (ghostSurfaceValid && placementGhost != null && placementGhost.activeSelf)
            {
                bool isCorrectPlacement = heldObject.IsCorrectPlacement(ghostPosition, ghostRotation);
                if (isCorrectPlacement)
                {
                    heldObject.PlaceAt(heldObject.SavedPosition, heldObject.SavedRotation);
                    heldObject.MarkPlacementCompleted();
                    heldObject.ShowPlacementFeedback(validPlacementColor, correctFeedbackDuration);
                }
                else
                {
                    heldObject.PlaceAt(ghostPosition, ghostRotation);
                    heldObject.ShowPlacementFeedback(invalidPlacementColor, 0f);
                }
            }
            else
            {
                heldObject.PlaceAt(FindDropPosition(), heldObject.SavedRotation);
                heldObject.ShowPlacementFeedback(invalidPlacementColor, 0f);
            }

            heldObject = null;
            ghostSurfaceValid = false;
            DestroyPlacementGhost();
        }

        private Vector3 FindDropPosition()
        {
            Vector3 rayStart = transform.position + Vector3.up * 5f;
            int hitCount = Physics.RaycastNonAlloc(
                rayStart,
                Vector3.down,
                raycastBuffer,
                20f,
                interactionMask,
                QueryTriggerInteraction.Ignore);

            SortHitsByDistance(raycastBuffer, hitCount);
            for (int i = 0; i < hitCount; i++)
            {
                if (raycastBuffer[i].collider == null || IsPlayerCollider(raycastBuffer[i].collider))
                {
                    continue;
                }

                return heldObject.GetSurfacePlacementPosition(raycastBuffer[i].point, Vector3.up);
            }

            Vector3 fallback = transform.position + transform.forward * 0.75f;
            fallback.y = 0f;
            return fallback;
        }

        private void ClearAimedObject()
        {
            if (aimedObject != null)
            {
                aimedObject.HideOutline();
                aimedObject = null;
            }
        }

        private Quaternion GetUprightPlacementRotation()
        {
            if (heldObject == null)
            {
                return Quaternion.identity;
            }

            Vector3 forward = Vector3.ProjectOnPlane(
                heldObject.SavedRotation * Vector3.forward,
                Vector3.up);

            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }

            return Quaternion.LookRotation(forward.normalized, Vector3.up);
        }

        private void ApplyGhostTransform(Vector3 position)
        {
            if (placementGhost == null || heldObject == null)
            {
                return;
            }

            ghostRotation = GetUprightPlacementRotation();
            placementGhost.transform.SetParent(null, false);
            placementGhost.transform.position = position;
            placementGhost.transform.rotation = ghostRotation;
            placementGhost.transform.localScale = heldObject.SavedScale;
        }

        private bool IsPlayerCollider(Collider candidate)
        {
            return candidate.GetComponentInParent<FirstPersonPlayerController>() != null;
        }

        private void EnsureHoldAnchor()
        {
            if (holdAnchor != null || playerCamera == null)
            {
                return;
            }

            GameObject anchorObject = new GameObject("HeldObjectAnchor");
            holdAnchor = anchorObject.transform;
            holdAnchor.SetParent(playerCamera.transform, false);
            holdAnchor.localPosition = new Vector3(0.42f, -0.22f, 0.75f);
            holdAnchor.localRotation = Quaternion.identity;
        }

        private void ResolveReferences()
        {
            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>(true);
            }

            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }
        }

        private void DestroyPlacementGhost()
        {
            if (placementGhost != null)
            {
                Destroy(placementGhost);
                placementGhost = null;
                ghostOutline = null;
            }
        }

        private bool WasPrimaryActionPressed()
        {
            if (MobileControlsUI.IsTouchControlsActive)
            {
                return MobileControlsUI.ReadActionPress(ref lastMobileActionVersion);
            }

            Mouse mouse = Mouse.current;
            return mouse != null && mouse.leftButton.wasPressedThisFrame;
        }

        private static void SortHitsByDistance(RaycastHit[] hits, int hitCount)
        {
            for (int index = 1; index < hitCount; index++)
            {
                RaycastHit current = hits[index];
                int previousIndex = index - 1;
                while (previousIndex >= 0 && hits[previousIndex].distance > current.distance)
                {
                    hits[previousIndex + 1] = hits[previousIndex];
                    previousIndex--;
                }

                hits[previousIndex + 1] = current;
            }
        }

        private void OnDisable()
        {
            DestroyPlacementGhost();
        }
    }
}
