using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using ColossalFramework;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using ICities;
using NetworkSkins.Data;
using NetworkSkins.Detour;
using NetworkSkins.Net;
using UnityEngine;

namespace NetworkSkins.Skins
{
    public class SkinManager : LoadingExtensionBase
    {
        public HashSet<string> skinsDefParseErrors;

        private readonly Dictionary<string, SkinsDefinition.Skin> skinMap = new Dictionary<string, SkinsDefinition.Skin>();

        public static SkinManager Instance;

        public override void OnCreated(ILoading loading)
        {
            base.OnCreated(loading);

            Instance = this;

            NetSegmentDetour.Deploy();

            RenderManagerDetour.EventUpdateDataPre += UpdateData;
        }

        public override void OnReleased()
        {
            base.OnReleased();

            RenderManagerDetour.EventUpdateDataPre -= UpdateData;

            NetSegmentDetour.Revert();

            Instance = null;
        }

        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);

            if (skinsDefParseErrors?.Count > 0)
            {
                var errorMessage = skinsDefParseErrors.Aggregate("Error while parsing network skins definition file(s). Contact the author of the skins. \n" + "List of errors:\n", (current, error) => current + (error + '\n'));

                UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel").SetMessage("Network Skins", errorMessage, true);
            }

            skinsDefParseErrors = null;
        }

        public override void OnLevelUnloading()
        {
            base.OnLevelUnloading();

            skinMap.Clear();
        }

        public void UpdateData(SimulationManager.UpdateMode mode)
        {
            try
            {
                skinsDefParseErrors = new HashSet<string>();
                var checkedPaths = new List<string>();

                foreach (var pluginInfo in PluginManager.instance.GetPluginsInfo().Where(pluginInfo => pluginInfo.isEnabled))
                {
                    // search for SkinsDefinition.xml
                    var skinsDefPath = Path.Combine(pluginInfo.modPath, "SkinsDefinition.xml");

                    // skip files which were already parsed
                    if (checkedPaths.Contains(skinsDefPath)) continue;
                    checkedPaths.Add(skinsDefPath);

                    if (!File.Exists(skinsDefPath)) continue;

                    SkinsDefinition skinsDef = null;

                    var xmlSerializer = new XmlSerializer(typeof(SkinsDefinition));
                    try
                    {
                        using (var streamReader = new System.IO.StreamReader(skinsDefPath))
                        {
                            skinsDef = xmlSerializer.Deserialize(streamReader) as SkinsDefinition;
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        skinsDefParseErrors.Add(pluginInfo.name + " - " + e.Message);
                        continue;
                    }

                    if (skinsDef?.Skins == null || skinsDef.Skins.Count == 0)
                    {
                        skinsDefParseErrors.Add(pluginInfo.name + " - skins is null or empty.");
                        continue;
                    }

                    foreach (var skin in skinsDef.Skins)
                    {
                        if (skin?.Identifier == null || skin?.DisplayName == null)
                        {
                            skinsDefParseErrors.Add(pluginInfo.name + " - Skin Identifier or Display Name missing.");
                            continue;
                        }

                        skin.Identifier = skin.NetworkName + "." + skin.Identifier;

                        skin.CreateMaterials(pluginInfo.modPath, skinsDefParseErrors);

                        skinMap.Add(skin.Identifier, skin);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public SkinsDefinition.Skin GetSkin(string id)
        {
            SkinsDefinition.Skin skin = null;
            skinMap.TryGetValue(id, out skin);

            return skin;
        }

        public List<SkinsDefinition.Skin> GetAvailableSkins(NetInfo prefab)
        {
            return skinMap.Values.Where((skin => prefab.name.Equals(skin.NetworkName))).ToList();
        }

        public SkinsDefinition.Skin GetActiveSkin(NetInfo prefab)
        {
            var segmentData = SegmentDataManager.Instance.GetActiveOptions(prefab);

            if (segmentData == null || !segmentData.Features.IsFlagSet(SegmentData.FeatureFlags.Skin))
            {
                return null;
            }
            else
            {
                return segmentData.SkinPrefab;
            }
        }

        public void SetSkin(NetInfo prefab, SkinsDefinition.Skin skin)
        {
            var newSegmentData = new SegmentData(SegmentDataManager.Instance.GetActiveOptions(prefab));

            if (skin != null)
            {
                newSegmentData.SetSkin(skin);
            }
            else
            {
                newSegmentData.UnsetFeature(SegmentData.FeatureFlags.Skin);
            }

            SegmentDataManager.Instance.SetActiveOptions(prefab, newSegmentData);
        }
    }

    public class SkinsDefinition
    {
        [DefaultValue(null)]
        public List<Skin> Skins { get; set; }

        public class Skin
        {
            [XmlAttribute("name"), DefaultValue(null)]
            public string DisplayName { get; set; }

            [XmlAttribute("id"), DefaultValue(null)]
            public string Identifier { get; set; }

            [XmlAttribute("network-name"), DefaultValue(null)]
            public string NetworkName { get; set; }

            [DefaultValue(null)]
            public List<Segment> Segments { get; set; }

            [XmlIgnore]
            public Material[] SegmentMaterials;

            [XmlIgnore]
            public NetInfo.LodValue[] SegmentCombinedLods;

            public void CreateMaterials(string basePath, HashSet<string> log)
            {
                if (NetworkName == null) return;

                var net = PrefabCollection<NetInfo>.FindLoaded(NetworkName);
                if (net == null) return;

                SegmentMaterials = new Material[net.m_segments.Length];
                SegmentCombinedLods = new NetInfo.LodValue[net.m_segments.Length];

                for (var i = 0; i < net.m_segments.Length; i++)
                {
                    SegmentMaterials[i] = net.m_segments[i].m_segmentMaterial;
                    SegmentCombinedLods[i] = net.m_segments[i].m_combinedLod;
                }

                foreach (var segment in Segments)
                {
                    if (segment.Index < 0 || segment.Index >= net.m_segments.Length)
                    {
                        log.Add($"Skin '{DisplayName}': Invalid Segment index {segment.Index}");
                        continue;
                    }

                    if (segment.Disabled)
                    {
                         SegmentMaterials[segment.Index] = null;
                         SegmentCombinedLods[segment.Index] = null;
                         continue; // TODO meshes
                    }

                    // DETAIL
                    var mainTex = segment.MainTexPath == null ? null : LoadTexture(Path.Combine(basePath, segment.MainTexPath));
                    var xysMap = segment.XysPath == null ? null : LoadTexture(Path.Combine(basePath, segment.XysPath));
                    var aprMap = segment.AprPath == null ? null : LoadTexture(Path.Combine(basePath, segment.AprPath));

                    if (mainTex != null || xysMap != null || aprMap != null)
                    {
                        var material = new Material(net.m_segments[segment.Index].m_segmentMaterial);

                        if (mainTex != null)
                        {
                            material.SetTexture("_MainTex", mainTex);
                        }

                        if (xysMap != null)
                        {
                            material.SetTexture("_XYSMap", xysMap);
                        }

                        if (aprMap != null)
                        {
                            material.SetTexture("_APRMap", aprMap);
                        }

                        SegmentMaterials[segment.Index] = material;
                    }

                    // TODO destroy material on unload

                    /*
                    // LOD
                    mainTex = segment.LodMainTexPath == null ? null : LoadTexture(Path.Combine(basePath, segment.LodMainTexPath));
                    xysMap = segment.LodXysPath == null ? null : LoadTexture(Path.Combine(basePath, segment.LodXysPath));
                    aprMap = segment.LodXysPath == null ? null : LoadTexture(Path.Combine(basePath, segment.LodAprPath));

                    if (mainTex != null || xysMap != null || aprMap != null)
                    {
                        var originalMaterial = net.m_segments[segment.Index].m_lodMaterial;
                        var material = new Material(originalMaterial);

                        if (mainTex != null)
                        {
                            material.SetTexture("_MainTex", mainTex);
                        }

                        if (xysMap != null)
                        {
                            material.SetTexture("_XYSMap", xysMap);
                        }

                        if (aprMap != null)
                        {
                            material.SetTexture("_APRMap", aprMap);
                        }

                        var originalLodValue = net.m_segments[segment.Index].m_combinedLod;

                        net.m_segments[segment.Index].m_lodMaterial = material;
                        net.m_segments[segment.Index].m_combinedLod = null;

                        net.InitMeshData(net.m_segments[segment.Index], new Rect(), 
                            material.GetTexture("_MainTex") as Texture2D, 
                            material.GetTexture("_XYSMap") as Texture2D,
                            material.GetTexture("APRMap") as Texture2D); // TODO

                        SegmentCombinedLods[segment.Index] = net.m_segments[segment.Index].m_combinedLod;

                        net.m_segments[segment.Index].m_lodMaterial = originalMaterial;
                        net.m_segments[segment.Index].m_combinedLod = originalLodValue;

                        // TODO destroy material on unload
                    }
                    */

                // TODO custom LODValue manager with custom atlas
                }
            }

            private Texture2D LoadTexture(string texturePath)
            {
                if (!File.Exists(texturePath)) return null;

                var fileData = File.ReadAllBytes(texturePath);
                var tex = new Texture2D(2, 2);
                tex.LoadImage(fileData);
                tex.anisoLevel = 8;
                tex.filterMode = FilterMode.Trilinear;
                tex.Apply();

                return tex;
            }

            public abstract class TexturedNetElement
            {
                [XmlAttribute("index"), DefaultValue(0)]
                public int Index { get; set; }

                [XmlAttribute("disabled"), DefaultValue(false)]
                public bool Disabled { get; set; }

                [XmlAttribute("main"), DefaultValue(null)]
                public string MainTexPath { get; set; }
                [XmlAttribute("xys"), DefaultValue(null)]
                public string XysPath { get; set; }
                [XmlAttribute("apr"), DefaultValue(null)]
                public string AprPath { get; set; }

                [XmlAttribute("lod-main"), DefaultValue(null)]
                public string LodMainTexPath { get; set; }
                [XmlAttribute("lod-xys"), DefaultValue(null)]
                public string LodXysPath { get; set; }
                [XmlAttribute("lod-apr"), DefaultValue(null)]
                public string LodAprPath { get; set; }
            }

            public class Segment : TexturedNetElement
            {
            }
        }
    }
}
