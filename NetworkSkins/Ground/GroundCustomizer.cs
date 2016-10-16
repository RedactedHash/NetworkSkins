using ColossalFramework;
using ICities;
using NetworkSkins.Data;
using System.Collections.Generic;

namespace NetworkSkins.Ground
{
    public class GroundCustomizer : LoadingExtensionBase
    {
        public static GroundCustomizer Instance;

        public string test = null;

        public override void OnCreated(ILoading loading)
        {
            base.OnCreated(loading);
            Instance = this;

            NetSegmentDetour.Deploy();
            NetNodeDetour.Deploy();
        }

        public override void OnReleased()
        {
            Instance = null;

            NetSegmentDetour.Revert();
            NetNodeDetour.Revert();
        }

        public List<GroundType> GetAvailableGroundTypes(NetInfo prefab) // TODO make dependent on skin
        {
            return new List<GroundType> { GroundType.Ruined, GroundType.Gravel, GroundType.Pavement };
        }

        public GroundType GetActiveGroundType(NetInfo prefab)
        {
            var segmentData = SegmentDataManager.Instance.GetActiveOptions(prefab);

            if (segmentData == null || !segmentData.Features.IsFlagSet(SegmentData.FeatureFlags.GroundTexture))
            {
                return GetDefaultGroundType(prefab);
            }
            else
            {
                if (segmentData.Features.IsFlagSet(SegmentData.FeatureFlags.CreatePavement)) return GroundType.Pavement;
                else if (segmentData.Features.IsFlagSet(SegmentData.FeatureFlags.CreateGravel)) return GroundType.Gravel;
                else return GroundType.Ruined;
            }
        }

        public GroundType GetDefaultGroundType(NetInfo prefab)
        {
            if (prefab.m_createPavement) return GroundType.Pavement;
            else if (prefab.m_createGravel) return GroundType.Gravel;
            else return GroundType.Ruined;
        }

        public void SetGroundType(NetInfo prefab, GroundType type)
        {
            var newSegmentData = new SegmentData(SegmentDataManager.Instance.GetActiveOptions(prefab));

            if (type != GetDefaultGroundType(prefab))
            {
                newSegmentData.ToggleFeature(SegmentData.FeatureFlags.GroundTexture, true);
                newSegmentData.ToggleFeature(SegmentData.FeatureFlags.CreateGravel, type == GroundType.Gravel);
                newSegmentData.ToggleFeature(SegmentData.FeatureFlags.CreatePavement, type == GroundType.Pavement);
            }
            else
            {
                newSegmentData.ToggleFeature(SegmentData.FeatureFlags.GroundTexture, false);
                newSegmentData.ToggleFeature(SegmentData.FeatureFlags.CreateGravel, false);
                newSegmentData.ToggleFeature(SegmentData.FeatureFlags.CreatePavement, false);
            }

            SegmentDataManager.Instance.SetActiveOptions(prefab, newSegmentData);
        }
    }
}
