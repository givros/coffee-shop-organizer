using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoffeeShop
{
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public sealed class PlaceableObjectTable : MonoBehaviour
    {
        [Serializable]
        public sealed class PlacementRecord
        {
            [SerializeField] private GameObject objectToPlace;
            [SerializeField] private Vector3 savedPosition;
            [SerializeField] private Vector3 savedEulerAngles;
            [SerializeField] private Vector3 savedScale = Vector3.one;

            public GameObject ObjectToPlace => objectToPlace;
            public Vector3 SavedPosition => savedPosition;
            public Quaternion SavedRotation => Quaternion.Euler(savedEulerAngles);
            public Vector3 SavedScale => savedScale;
        }

        [Header("Objects to place")]
        [Tooltip("Each child object is stored with its world-space reference pose.")]
        [SerializeField] private List<PlacementRecord> records = new List<PlacementRecord>();

        private bool runtimeInitialized;

        public int ObjectCount => records.Count;
        public IReadOnlyList<PlacementRecord> Records => records;

        private void Awake()
        {
            InitializeRuntimeObjects();
        }

        private void Start()
        {
            InitializeRuntimeObjects();
        }

        public bool TryGetRecord(GameObject objectToPlace, out PlacementRecord record)
        {
            for (int i = 0; i < records.Count; i++)
            {
                PlacementRecord candidate = records[i];
                if (candidate != null && candidate.ObjectToPlace == objectToPlace)
                {
                    record = candidate;
                    return true;
                }
            }

            record = null;
            return false;
        }

        private void InitializeRuntimeObjects()
        {
            if (runtimeInitialized)
            {
                return;
            }

            int initializedCount = 0;
            for (int i = 0; i < records.Count; i++)
            {
                PlacementRecord record = records[i];
                GameObject objectToPlace = record == null ? null : record.ObjectToPlace;
                if (objectToPlace == null && i < transform.childCount)
                {
                    objectToPlace = transform.GetChild(i).gameObject;
                }

                if (record == null || objectToPlace == null)
                {
                    continue;
                }

                PlaceableObject placeableObject = objectToPlace.GetComponent<PlaceableObject>();
                if (placeableObject == null)
                {
                    placeableObject = objectToPlace.AddComponent<PlaceableObject>();
                }

                placeableObject.ConfigureSavedPlacement(
                    record.SavedPosition,
                    record.SavedRotation,
                    record.SavedScale);
                initializedCount++;
            }

            runtimeInitialized = records.Count == 0 || initializedCount == records.Count;
            PlaceableObject.NotifyProgressChanged();
        }
    }
}
