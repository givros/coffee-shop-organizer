using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CoffeeShop
{
    [DefaultExecutionOrder(100)]
    public sealed class PlaceableObject : MonoBehaviour
    {
        private static readonly HashSet<PlaceableObject> RegisteredObjects = new HashSet<PlaceableObject>();

        public static event System.Action ProgressChanged;

        public static int TotalObjectCount => RegisteredObjects.Count;

        public static int CompletedObjectCount
        {
            get
            {
                int completedCount = 0;
                foreach (PlaceableObject placeableObject in RegisteredObjects)
                {
                    if (placeableObject != null && placeableObject.placementCompleted)
                    {
                        completedCount++;
                    }
                }

                return completedCount;
            }
        }

        public static void NotifyProgressChanged()
        {
            ProgressChanged?.Invoke();
        }

        [Header("Saved Placement")]
        [SerializeField] private bool hasSavedPlacement;
        [SerializeField] private Vector3 savedPosition;
        [SerializeField] private Vector3 savedEulerAngles;
        [SerializeField] private Vector3 savedScale = Vector3.one;
        [SerializeField, Min(0.01f)] private float positionTolerance = 0.3f;
        [SerializeField, Min(0f)] private float rotationTolerance = 25f;

        [Header("Interaction")]
        [SerializeField] private bool canBePickedUp = true;
        [SerializeField] private OutlineVisualizer outlineVisualizer;

        private Collider[] colliders;
        private Rigidbody[] rigidbodies;
        private Coroutine feedbackRoutine;
        private bool isHeld;
        private bool feedbackActive;
        private bool placementCompleted;
        private bool progressTracked;

        public bool IsHeld => isHeld;
        public bool IsFeedbackActive => feedbackActive;
        public bool IsPlacementCompleted => placementCompleted;
        public bool CanBePickedUp => canBePickedUp && !isHeld && !placementCompleted;
        public Vector3 SavedPosition => savedPosition;
        public Quaternion SavedRotation => Quaternion.Euler(savedEulerAngles);
        public Vector3 SavedScale => savedScale;

        private void Awake()
        {
            if (outlineVisualizer == null)
            {
                outlineVisualizer = GetComponent<OutlineVisualizer>();
            }

            colliders = GetComponentsInChildren<Collider>(true);
            rigidbodies = GetComponentsInChildren<Rigidbody>(true);

            if (savedScale == Vector3.zero)
            {
                savedScale = transform.localScale;
            }

            if (!hasSavedPlacement)
            {
                savedPosition = transform.position;
                savedEulerAngles = transform.eulerAngles;
                savedScale = transform.localScale;
                hasSavedPlacement = true;
            }

            if (StaticSceneAssetRegistry.IsExcluded(gameObject))
            {
                progressTracked = false;
                return;
            }

            progressTracked = true;
            RegisteredObjects.Add(this);
            ProgressChanged?.Invoke();
        }

        public void ConfigureSavedPlacement(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            hasSavedPlacement = true;
            savedPosition = position;
            savedEulerAngles = rotation.eulerAngles;
            savedScale = scale == Vector3.zero ? Vector3.one : scale;
        }

        public bool MarkPlacementCompleted()
        {
            if (!progressTracked || placementCompleted)
            {
                return false;
            }

            placementCompleted = true;
            ProgressChanged?.Invoke();
            return true;
        }

        public void ExcludeFromProgressTracking()
        {
            if (!progressTracked)
            {
                return;
            }

            progressTracked = false;
            if (RegisteredObjects.Remove(this))
            {
                ProgressChanged?.Invoke();
            }
        }

        public void ShowOutline(Color color)
        {
            OutlineVisualizer visualizer = GetOrCreateOutlineVisualizer();
            if (visualizer == null)
            {
                return;
            }

            visualizer.SetState(color, true);
        }

        public void HideOutline()
        {
            if (feedbackActive || outlineVisualizer == null)
            {
                return;
            }

            outlineVisualizer.SetVisible(false);
        }

        public void ClearFeedback()
        {
            if (feedbackRoutine != null)
            {
                StopCoroutine(feedbackRoutine);
                feedbackRoutine = null;
            }

            feedbackActive = false;
            HideOutline();
        }

        public void ShowPlacementFeedback(Color color, float duration)
        {
            if (feedbackRoutine != null)
            {
                StopCoroutine(feedbackRoutine);
                feedbackRoutine = null;
            }

            feedbackActive = true;
            ShowOutline(color);

            if (duration > 0f)
            {
                feedbackRoutine = StartCoroutine(ClearFeedbackAfter(duration));
            }
        }

        public void SetHeldState(bool held)
        {
            isHeld = held;
            SetCollidersEnabled(!held);

            for (int i = 0; i < rigidbodies.Length; i++)
            {
                Rigidbody rigidbody = rigidbodies[i];
                if (rigidbody == null)
                {
                    continue;
                }

                rigidbody.isKinematic = true;
                rigidbody.useGravity = false;
                rigidbody.linearVelocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
            }

            if (held)
            {
                ClearFeedback();
            }
        }

        public void PlaceAt(Vector3 position, Quaternion rotation)
        {
            transform.SetParent(null, true);
            transform.SetPositionAndRotation(position, rotation);
            transform.localScale = savedScale;
            SetHeldState(false);
        }

        public bool IsCorrectPlacement(Vector3 position, Quaternion rotation)
        {
            if (!hasSavedPlacement)
            {
                return false;
            }

            bool positionMatches = Vector3.Distance(position, savedPosition) <= positionTolerance;
            bool rotationMatches = Quaternion.Angle(rotation, SavedRotation) <= rotationTolerance;
            return positionMatches && rotationMatches;
        }

        public Vector3 GetSurfacePlacementPosition(Vector3 surfacePoint, Vector3 surfaceNormal)
        {
            Vector3 normal = surfaceNormal.sqrMagnitude > 0.0001f
                ? surfaceNormal.normalized
                : Vector3.up;

            Vector3 origin = transform.position;
            float minimumProjection = float.MaxValue;
            bool foundBounds = false;

            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || OutlineVisualizer.IsGeneratedShell(renderer))
                {
                    continue;
                }

                Bounds bounds = renderer.bounds;
                Vector3 center = bounds.center;
                Vector3 extents = bounds.extents;

                for (int x = -1; x <= 1; x += 2)
                {
                    for (int y = -1; y <= 1; y += 2)
                    {
                        for (int z = -1; z <= 1; z += 2)
                        {
                            Vector3 corner = center + new Vector3(
                                extents.x * x,
                                extents.y * y,
                                extents.z * z);

                            minimumProjection = Mathf.Min(
                                minimumProjection,
                                Vector3.Dot(corner - origin, normal));
                            foundBounds = true;
                        }
                    }
                }
            }

            if (!foundBounds)
            {
                return surfacePoint;
            }

            return surfacePoint - normal * minimumProjection;
        }

        private IEnumerator ClearFeedbackAfter(float duration)
        {
            yield return new WaitForSeconds(duration);
            feedbackRoutine = null;
            feedbackActive = false;
            HideOutline();
        }

        private void SetCollidersEnabled(bool enabled)
        {
            if (colliders == null)
            {
                return;
            }

            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    colliders[i].enabled = enabled;
                }
            }
        }

        private OutlineVisualizer GetOrCreateOutlineVisualizer()
        {
            if (outlineVisualizer == null)
            {
                outlineVisualizer = GetComponent<OutlineVisualizer>();
            }

            if (outlineVisualizer == null)
            {
                outlineVisualizer = gameObject.AddComponent<OutlineVisualizer>();
            }

            return outlineVisualizer;
        }

        private void OnDestroy()
        {
            if (progressTracked && RegisteredObjects.Remove(this))
            {
                ProgressChanged?.Invoke();
            }
        }
    }
}
