﻿using System;
using ColossalFramework;
using ColossalFramework.IO;
using NetworkSkins.Net;
using UnityEngine;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace NetworkSkins.Data
{
    public class SegmentData : IDataContainer
    {
        [Flags]
        public enum FeatureFlags
        {
            None = 0,
            TreeLeft = 1,
            TreeMiddle = 2,
            TreeRight = 4,
            StreetLight = 8,
            NoDecals = 16
        }

        // After setting these fields once, they should only be read!
        public FeatureFlags Features = FeatureFlags.None;
        public string TreeLeft;
        public string TreeMiddle;
        public string TreeRight;
        public string StreetLight;

        [NonSerialized]
        public TreeInfo TreeLeftPrefab;
        [NonSerialized]
        public TreeInfo TreeMiddlePrefab;
        [NonSerialized]
        public TreeInfo TreeRightPrefab;
        [NonSerialized]
        public PropInfo StreetLightPrefab;
        [NonSerialized]
        public int UsedCount = 0;

        private SegmentData() {}

        public SegmentData(SegmentData segmentData)
        {
            if (segmentData == null) return;

            Features = segmentData.Features;
            TreeLeft = segmentData.TreeLeft;
            TreeMiddle = segmentData.TreeMiddle;
            TreeRight = segmentData.TreeRight;
            StreetLight = segmentData.StreetLight;

            TreeLeftPrefab = segmentData.TreeLeftPrefab;
            TreeMiddlePrefab = segmentData.TreeMiddlePrefab;
            TreeRightPrefab = segmentData.TreeRightPrefab;
            StreetLightPrefab = segmentData.StreetLightPrefab;
        }

        public void SetFeature<P>(FeatureFlags feature, P prefab = null) where P : PrefabInfo
        {
            Features = Features.SetFlags(feature);

            var flagName = feature.ToString();

            var nameField = GetType().GetField(flagName);
            var prefabField = GetType().GetField(flagName + "Prefab");

            nameField?.SetValue(this, prefab?.name);
            prefabField?.SetValue(this, prefab);
        }

        public void UnsetFeature(FeatureFlags feature)
        {
            Features = Features.ClearFlags(feature);

            var flagName = feature.ToString();

            var nameField = GetType().GetField(flagName);
            var prefabField = GetType().GetField(flagName + "Prefab");

            nameField?.SetValue(this, null);
            prefabField?.SetValue(this, null);
        }

        public void Serialize(DataSerializer s)
        {
            s.WriteInt32((int)Features);

            if (Features.IsFlagSet(FeatureFlags.TreeLeft))
                s.WriteSharedString(TreeLeft);
            if (Features.IsFlagSet(FeatureFlags.TreeMiddle))
                s.WriteSharedString(TreeMiddle);
            if (Features.IsFlagSet(FeatureFlags.TreeRight))
                s.WriteSharedString(TreeRight);
            if (Features.IsFlagSet(FeatureFlags.StreetLight))
                s.WriteSharedString(StreetLight);
        }

        public void Deserialize(DataSerializer s)
        {
            Features = (FeatureFlags)s.ReadInt32();

            if (Features.IsFlagSet(FeatureFlags.TreeLeft))
                TreeLeft = s.ReadSharedString();
            if (Features.IsFlagSet(FeatureFlags.TreeMiddle))
                TreeMiddle = s.ReadSharedString();
            if (Features.IsFlagSet(FeatureFlags.TreeRight))
                TreeRight = s.ReadSharedString();
            if (Features.IsFlagSet(FeatureFlags.StreetLight))
                StreetLight = s.ReadSharedString();
        }

        public void AfterDeserialize(DataSerializer s) {}

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == this.GetType() && Equals((SegmentData) obj);
        }

        protected bool Equals(SegmentData other)
        {
            return Features == other.Features 
                && string.Equals(TreeLeft, other.TreeLeft) 
                && string.Equals(TreeMiddle, other.TreeMiddle) 
                && string.Equals(TreeRight, other.TreeRight) 
                && string.Equals(StreetLight, other.StreetLight);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int) Features;
                hashCode = (hashCode*397) ^ (TreeLeft?.GetHashCode() ?? 0);
                hashCode = (hashCode*397) ^ (TreeMiddle?.GetHashCode() ?? 0);
                hashCode = (hashCode*397) ^ (TreeRight?.GetHashCode() ?? 0);
                hashCode = (hashCode*397) ^ (StreetLight?.GetHashCode() ?? 0);
                return hashCode;
            }
        }

        public override string ToString() 
        {
            return "[SegmentData] features: " + Features
                + ", treeLeftPrefab: " + (TreeLeftPrefab == null ? "null" : TreeLeftPrefab.name)
                + ", treeMiddlePrefab: " + (TreeMiddlePrefab == null ? "null" : TreeMiddlePrefab.name)
                + ", treeRightPrefab: " + (TreeRightPrefab == null ? "null" : TreeRightPrefab.name)
                + ", streetLightPrefab: " + (StreetLightPrefab == null ? "null" : StreetLightPrefab.name)
                + ", usedCount: " + UsedCount;
        }

        public void FindPrefabs()
        {
            FindPrefab(TreeLeft, out TreeLeftPrefab);
            FindPrefab(TreeMiddle, out TreeMiddlePrefab);
            FindPrefab(TreeRight, out TreeRightPrefab);
            FindPrefab(StreetLight, out StreetLightPrefab);
        }

        private static void FindPrefab<T>(string prefabName, out T prefab) where T : PrefabInfo
        {
            prefab = prefabName != null ? PrefabCollection<T>.FindLoaded(prefabName) : null;
        }
    }
}