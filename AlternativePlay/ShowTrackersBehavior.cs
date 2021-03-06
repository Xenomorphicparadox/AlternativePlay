﻿using AlternativePlay.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.XR;

namespace AlternativePlay
{
    public class ShowTrackersBehavior : MonoBehaviour
    {
        private bool showTrackers;
        private TrackerConfigData selectedTracker;

        private GameObject trackerPrefab;
        private GameObject saberPrefab;

        private List<TrackerInstance> trackerInstances;
        private GameObject saberInstance;

        /// <summary>
        /// Begins showing the tracked devices
        /// </summary>
        public void EnableShowTrackers()
        {
            RemoveAllInstances();

            this.trackerInstances = TrackedDeviceManager.instance.TrackedDevices.Select((t) => new TrackerInstance
            {
                Instance = GameObject.Instantiate(this.trackerPrefab),
                InputDevice = t,
                Serial = t.serialNumber,
            }).ToList();

            this.trackerInstances.ForEach(t => t.Instance.SetActive(true));
            this.saberInstance = GameObject.Instantiate(this.saberPrefab);
            this.showTrackers = true;
            this.enabled = true;
        }

        /// <summary>
        /// Hides all trackers and prevents rendering
        /// </summary>
        public void DisableShowTrackers()
        {
            this.showTrackers = false;
            RemoveAllInstances();

            this.enabled = false;
        }

        /// <summary>
        /// Sets the given serial of the tracker as the currently selected tracker.
        /// The tracker will be drawn with a saber.  Send null or a non-existant
        /// serial to set nothing to be selected.
        /// </summary>
        /// <param name="serial">The serial of the tracker to set as selected</param>
        public void SetSelectedSerial(TrackerConfigData tracker)
        {
            this.selectedTracker = tracker;
        }

        private void Awake()
        {
            AssetBundle assetBundle = AssetBundle.LoadFromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("AlternativePlay.Resources.alternativeplaymodels"));
            this.trackerPrefab = assetBundle.LoadAsset<GameObject>("APTracker");
            this.saberPrefab = assetBundle.LoadAsset<GameObject>("APSaber");
            assetBundle.Unload(false);
        }

        private void Update()
        {
            if (!this.showTrackers || this.trackerInstances == null || this.trackerInstances.Count == 0) return;

            foreach (var tracker in this.trackerInstances)
            {
                // Update all the tracker poses
                bool positionSuccess = tracker.InputDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position);
                if (positionSuccess) tracker.Instance.transform.position = position;

                bool rotationSuccess = tracker.InputDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation);
                if (rotationSuccess) tracker.Instance.transform.rotation = rotation;
            }

            var selectedTrackerInstance = this.trackerInstances.Find(t => t.Serial == this.selectedTracker.Serial);
            if (this.selectedTracker == null || String.IsNullOrWhiteSpace(this.selectedTracker.Serial))
            {
                // No tracker so disable the saber
                this.saberInstance.SetActive(false);
                return;
            }

            // Transform the Saber according to the Tracker Config Data
            Pose selectedTrackerPose = new Pose(
                selectedTrackerInstance.Instance.transform.position,
                selectedTrackerInstance.Instance.transform.rotation);
            Utilities.TransformSaberFromTrackerData(this.saberInstance.transform, this.selectedTracker, selectedTrackerPose);

            this.saberInstance.SetActive(true);
        }

        // Deletes all the instances and all the locally stored tracker instances data
        private void RemoveAllInstances()
        {
            if (this.trackerInstances != null) this.trackerInstances.ForEach(t => GameObject.Destroy(t.Instance));
            this.trackerInstances = null;

            if (this.saberInstance != null) GameObject.Destroy(this.saberInstance);
            this.saberInstance = null;
        }

        private class TrackerInstance
        {
            public GameObject Instance { get; set; }
            public bool Selected { get; set; }
            public string Serial { get; set; }
            public InputDevice InputDevice { get; internal set; }
        }

    }
}
