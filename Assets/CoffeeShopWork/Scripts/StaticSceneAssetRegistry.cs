using System.Collections.Generic;
using UnityEngine;

namespace CoffeeShop
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class StaticSceneAssetRegistry : MonoBehaviour
    {
        private static StaticSceneAssetRegistry activeRegistry;

        [Header("Fixed scene assets")]
        [Tooltip("Scene assets listed here are decorative only. They cannot be picked up and never count toward placement progress.")]
        [SerializeField] private List<GameObject> assets = new List<GameObject>();

        public int AssetCount => assets.Count;

        private void Awake()
        {
            activeRegistry = this;
        }

        private void Start()
        {
            EnforceExclusions();
        }

        public static bool IsExcluded(GameObject candidate)
        {
            return activeRegistry != null && activeRegistry.Contains(candidate);
        }

        private bool Contains(GameObject candidate)
        {
            if (candidate == null)
            {
                return false;
            }

            Transform candidateTransform = candidate.transform;
            if (candidateTransform.IsChildOf(transform))
            {
                return true;
            }

            for (int i = 0; i < assets.Count; i++)
            {
                GameObject asset = assets[i];
                if (asset == null)
                {
                    continue;
                }

                Transform assetTransform = asset.transform;
                if (candidateTransform == assetTransform || candidateTransform.IsChildOf(assetTransform))
                {
                    return true;
                }
            }

            return false;
        }

        private void EnforceExclusions()
        {
            PlaceableObject[] childPlaceableObjects = GetComponentsInChildren<PlaceableObject>(true);
            for (int i = 0; i < childPlaceableObjects.Length; i++)
            {
                if (childPlaceableObjects[i] != null)
                {
                    childPlaceableObjects[i].ExcludeFromProgressTracking();
                }
            }

            for (int i = 0; i < assets.Count; i++)
            {
                GameObject asset = assets[i];
                if (asset == null)
                {
                    continue;
                }

                PlaceableObject[] placeableObjects = asset.GetComponentsInChildren<PlaceableObject>(true);
                for (int j = 0; j < placeableObjects.Length; j++)
                {
                    if (placeableObjects[j] != null)
                    {
                        placeableObjects[j].ExcludeFromProgressTracking();
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (activeRegistry == this)
            {
                activeRegistry = null;
            }
        }
    }
}
