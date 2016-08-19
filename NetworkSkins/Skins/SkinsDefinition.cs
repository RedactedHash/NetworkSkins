using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
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

        public static readonly Dictionary<NetInfo, int> originalSegmentCounts = new Dictionary<NetInfo, int>(); 

        private readonly Dictionary<string, SkinsDefinition.Skin> skinMap = new Dictionary<string, SkinsDefinition.Skin>();

        private static GameObject skinLoaderGO;

        public static SkinManager Instance;

        public override void OnCreated(ILoading loading)
        {
            base.OnCreated(loading);

            Instance = this;

            NetSegmentDetour.Deploy();
            NetNodeDetour.Deploy();

            if (skinLoaderGO == null) skinLoaderGO = new GameObject("NS SkinLoaders");

            ParseSkinDefs();
        }

        public override void OnReleased()
        {
            base.OnReleased();

            NetSegmentDetour.Revert();
            NetNodeDetour.Revert();

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
            originalSegmentCounts.Clear();
        }

        public void ParseSkinDefs()
        {
            try
            {
                skinsDefParseErrors = new HashSet<string>();
                var checkedPaths = new List<string>();

                // TODO save original node/segment count

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

                        if (skin?.NetworkName == null)
                        {
                            skinsDefParseErrors.Add($"Skin '{skin.DisplayName}': Network name not defined");
                            continue;
                        }


                        skin.Identifier = skin.NetworkName + "." + skin.Identifier;

                        var loader = skinLoaderGO.AddComponent<SkinLoader>();
                        loader.skin = skin;
                        loader.path = pluginInfo.modPath;
                        loader.log = skinsDefParseErrors;

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
            public int[] segmentRedirectMap;

            // TODO track materials/textures

            public void CreateMaterials(string basePath, Dictionary<NetInfo, int> originalSegmentCounts, NetInfo net, HashSet<string> log)
            {
                var InitSegmentInfo = typeof (NetInfo).GetMethod("InitSegmentInfo", BindingFlags.NonPublic | BindingFlags.Instance);
                if (InitSegmentInfo == null)
                {
                    log.Add($"InitSegmentInfo not found");
                    return;
                }

                if (net == null)
                {
                    log.Add($"Skin '{DisplayName}': Network '{NetworkName}' not found");
                    return; 
                }

                if(net.m_segments == null)
                {
                    log.Add($"Skin '{DisplayName}': No segments found in network '{NetworkName}'");
                    return;
                }

                var newNetSegmentsArray = new List<NetInfo.Segment>(net.m_segments);

                int originalSegmentCount;
                if (!originalSegmentCounts.TryGetValue(net, out originalSegmentCount))
                {
                    originalSegmentCount = net.m_segments.Length;
                    originalSegmentCounts.Add(net, originalSegmentCount);
                }

                segmentRedirectMap = new int[originalSegmentCount];
                for (var i = 0; i < originalSegmentCount; i++)
                {
                    segmentRedirectMap[i] = i;
                }


                foreach (var segmentDef in Segments)
                {
                    if (segmentDef.Index < 0 || segmentDef.Index >= originalSegmentCount)
                    {
                        log.Add($"Skin '{DisplayName}': Invalid Segment index {segmentDef.Index}");
                        continue;
                    }

                    // DETAIL
                    var mainTex = segmentDef.MainTexPath == null ? null : LoadTexture(Path.Combine(basePath, segmentDef.MainTexPath));
                    var xysMap = segmentDef.XysPath == null ? null : LoadTexture(Path.Combine(basePath, segmentDef.XysPath));
                    var aprMap = segmentDef.AprPath == null ? null : LoadTexture(Path.Combine(basePath, segmentDef.AprPath));

                    var originalSegment = net.m_segments[segmentDef.Index];
                    if(originalSegment == null)
                    {
                        log.Add($"Skin '{DisplayName}': Original Segment {segmentDef.Index} is null");
                        continue;
                    }

                    var material = originalSegment.m_segmentMaterial;

                    if (mainTex != null || xysMap != null || aprMap != null)
                    {
                        material = new Material(material);

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
                    }

                    var lodMaterial = originalSegment.m_lodMaterial;

                    // LOD
                    mainTex = segmentDef.LodMainTexPath == null ? null : LoadTexture(Path.Combine(basePath, segmentDef.LodMainTexPath));
                    xysMap = segmentDef.LodXysPath == null ? null : LoadTexture(Path.Combine(basePath, segmentDef.LodXysPath));
                    aprMap = segmentDef.LodAprPath == null ? null : LoadTexture(Path.Combine(basePath, segmentDef.LodAprPath));

                    if (mainTex != null || xysMap != null || aprMap != null)
                    {
                        lodMaterial = new Material(lodMaterial);

                        if (mainTex != null)
                        {
                            lodMaterial.SetTexture("_MainTex", mainTex);
                        }

                        if (xysMap != null)
                        {
                            lodMaterial.SetTexture("_XYSMap", xysMap);
                        }

                        if (aprMap != null)
                        {
                            lodMaterial.SetTexture("_APRMap", aprMap);
                        }
                    }

                    var newSegment = new NetInfo.Segment
                    {
                        m_mesh = originalSegment.m_mesh,
                        m_lodMesh = originalSegment.m_lodMesh,
                        m_material = material,
                        m_lodMaterial = lodMaterial,
                        m_forwardRequired = originalSegment.m_forwardRequired,
                        m_forwardForbidden = originalSegment.m_forwardForbidden,
                        m_backwardRequired = originalSegment.m_backwardRequired,
                        m_backwardForbidden = originalSegment.m_backwardForbidden,
                        m_emptyTransparent = originalSegment.m_emptyTransparent,
                        m_disableBendNodes = originalSegment.m_disableBendNodes,
                        m_lodRenderDistance = originalSegment.m_lodRenderDistance
                    };
                    InitSegmentInfo.Invoke(net, new object[] {newSegment, net.m_requireSurfaceMaps});

                    segmentRedirectMap[segmentDef.Index] = newNetSegmentsArray.Count;
                    newNetSegmentsArray.Add(newSegment);
                }

                net.m_segments = newNetSegmentsArray.ToArray();
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
