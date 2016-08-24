using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using UnityEngine;

namespace NetworkSkins.Skins
{
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
                    var mainTex = segmentDef.MainTexPath == null ? null : LoadTexture(Path.Combine(basePath, segmentDef.MainTexPath), false);
                    var xysMap = segmentDef.XysPath == null ? null : LoadTexture(Path.Combine(basePath, segmentDef.XysPath), false);
                    var aprMap = segmentDef.AprPath == null ? null : LoadTexture(Path.Combine(basePath, segmentDef.AprPath), false);

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
                    mainTex = segmentDef.LodMainTexPath == null ? null : LoadTexture(Path.Combine(basePath, segmentDef.LodMainTexPath), true);
                    xysMap = segmentDef.LodXysPath == null ? null : LoadTexture(Path.Combine(basePath, segmentDef.LodXysPath), true);
                    aprMap = segmentDef.LodAprPath == null ? null : LoadTexture(Path.Combine(basePath, segmentDef.LodAprPath), true);

                    if (mainTex != null || xysMap != null || aprMap != null)
                    {
                        if (mainTex != null && xysMap != null && aprMap != null)
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
                        else
                        {
                            log.Add($"Not all LOD textures (lod-main, lod-xys, lod-apr) are defined for Skin {DisplayName} (Index {segmentDef.Index})! That will cause rendering issues!");
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

            private Texture2D LoadTexture(string texturePath, bool lod)
            {
                if (!File.Exists(texturePath)) return null;

                var fileData = File.ReadAllBytes(texturePath);
                var tex = new Texture2D(2, 2, TextureFormat.ARGB32, !lod); // no mipmap for LODs
                tex.name = Path.GetFileNameWithoutExtension(texturePath);
                tex.LoadImage(fileData);
                tex.anisoLevel = 8;
                tex.filterMode = FilterMode.Bilinear;
                tex.Compress(true);
                if (!lod) tex.Apply(true, true); else tex.Apply();

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
