using UnityEngine;
using UnityEngine.InputSystem;

namespace CoffeeShop
{
    public sealed class DoorInteractable : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform player;
        [SerializeField] private Renderer[] frameRenderers;

        [Header("Interaction")]
        [SerializeField, Min(0.1f)] private float interactionDistance = 3.0f;
        [SerializeField, Min(0.1f)] private float raycastDistance = 8f;
        [SerializeField] private LayerMask raycastMask = Physics.DefaultRaycastLayers;

        [Header("Door Motion")]
        [SerializeField] private float openAngle = 100f;
        [SerializeField, Min(0.05f)] private float openDuration = 0.65f;

        [Header("Highlight")]
        [SerializeField] private Color frameHighlightColor = new Color(1f, 1f, 1f, 1f);
        [SerializeField, Min(0f)] private float emissionIntensity = 1.5f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private Quaternion closedRotation;
        private Quaternion openRotation;
        private MaterialPropertyBlock propertyBlock;
        private float animationProgress;
        private bool isOpening;
        private bool isOpen;
        private bool isFrameHighlighted;
        private uint lastMobileActionVersion;

        private void Awake()
        {
            closedRotation = transform.localRotation;
            openRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);
            propertyBlock = new MaterialPropertyBlock();

            ResolveReferences();
        }

private void Update()
        {
            if (isOpening)
            {
                UpdateDoorAnimation();
            }

            if (GameSessionManager.Instance != null && GameSessionManager.Instance.IsPaused)
            {
                SetFrameHighlighted(false);
                return;
            }

            bool canInteract = IsPlayerClose();
            bool isTargeted = canInteract && IsTargeted();

            SetFrameHighlighted(isTargeted);

            if (isTargeted && WasPrimaryActionPressed())
            {
                StartOpening();
            }
        }

private void UpdateDoorAnimation()
        {
            float direction = isOpen ? 1f : -1f;
            animationProgress = Mathf.Clamp01(
                animationProgress + direction * Time.deltaTime / openDuration);

            float easedProgress = animationProgress * animationProgress * (3f - 2f * animationProgress);
            transform.localRotation = Quaternion.Slerp(closedRotation, openRotation, easedProgress);

            bool animationFinished = isOpen
                ? animationProgress >= 1f
                : animationProgress <= 0f;

            if (animationFinished)
            {
                isOpening = false;
                SetFrameHighlighted(false);
            }
        }

private void StartOpening()
        {
            if (isOpening)
            {
                return;
            }

            if (!isOpen)
            {
                openRotation = GetOpenRotationForPlayerSide();
            }

            isOpen = !isOpen;
            animationProgress = isOpen ? 0f : 1f;
            isOpening = true;
        }

private Quaternion GetOpenRotationForPlayerSide()
        {
            if (player == null)
            {
                return closedRotation * Quaternion.Euler(0f, openAngle, 0f);
            }

            Vector3 toPlayer = player.position - transform.position;
            float playerSide = Vector3.Dot(toPlayer, transform.forward);
            float swingAngle = playerSide >= 0f ? openAngle : -openAngle;

            return closedRotation * Quaternion.Euler(0f, swingAngle, 0f);
        }


        private bool IsPlayerClose()
        {
            if (player == null)
            {
                return false;
            }

            Vector3 playerPosition = player.position;
            Vector3 doorPosition = transform.position;
            playerPosition.y = 0f;
            doorPosition.y = 0f;

            return Vector3.Distance(playerPosition, doorPosition) <= interactionDistance;
        }

        private bool IsTargeted()
        {
            if (playerCamera == null)
            {
                return false;
            }

            Ray centerRay = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (!Physics.Raycast(
                    centerRay,
                    out RaycastHit hit,
                    raycastDistance,
                    raycastMask,
                    QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            DoorInteractable hitDoor = hit.collider.GetComponentInParent<DoorInteractable>();
            if (hitDoor == this)
            {
                return true;
            }

            Renderer hitRenderer = hit.collider.GetComponent<Renderer>();
            return IsFrameRenderer(hitRenderer);
        }

private void ResolveReferences()
        {
            if (player == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    player = playerObject.transform;

                    if (playerCamera == null)
                    {
                        playerCamera = playerObject.GetComponentInChildren<Camera>(true);
                    }
                }
            }

            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }

            bool hasMissingFrameRenderer = frameRenderers == null || frameRenderers.Length == 0;
            if (!hasMissingFrameRenderer)
            {
                for (int i = 0; i < frameRenderers.Length; i++)
                {
                    if (frameRenderers[i] == null)
                    {
                        hasMissingFrameRenderer = true;
                        break;
                    }
                }
            }

            if (hasMissingFrameRenderer)
            {
                frameRenderers = new[]
                {
                    FindRenderer("CoffeeShopBuilding/Exterior/EntranceFrameLeft"),
                    FindRenderer("CoffeeShopBuilding/Exterior/EntranceFrameRight"),
                    FindRenderer("CoffeeShopBuilding/Exterior/EntranceHeader")
                };
            }
        }

        private static Renderer FindRenderer(string path)
        {
            GameObject frameObject = GameObject.Find(path);
            return frameObject != null ? frameObject.GetComponent<Renderer>() : null;
        }

        private bool IsFrameRenderer(Renderer candidate)
        {
            if (candidate == null || frameRenderers == null)
            {
                return false;
            }

            for (int i = 0; i < frameRenderers.Length; i++)
            {
                if (frameRenderers[i] == candidate)
                {
                    return true;
                }
            }

            return false;
        }

        private void SetFrameHighlighted(bool highlighted)
        {
            if (isFrameHighlighted == highlighted)
            {
                return;
            }

            isFrameHighlighted = highlighted;

            if (frameRenderers == null)
            {
                return;
            }

            for (int i = 0; i < frameRenderers.Length; i++)
            {
                Renderer frameRenderer = frameRenderers[i];
                if (frameRenderer == null)
                {
                    continue;
                }

                if (!highlighted)
                {
                    frameRenderer.SetPropertyBlock(null);
                    continue;
                }

                frameRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(BaseColorId, frameHighlightColor);
                propertyBlock.SetColor(ColorId, frameHighlightColor);
                propertyBlock.SetColor(EmissionColorId, frameHighlightColor * emissionIntensity);
                frameRenderer.SetPropertyBlock(propertyBlock);
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

        private void OnDisable()
        {
            SetFrameHighlighted(false);
        }
    }
}
