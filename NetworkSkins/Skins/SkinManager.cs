using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using ColossalFramework;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using ICities;
using NetworkSkins.Data;
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

        /*
        public override void OnCreated(ILoading loading)
        {
            base.OnCreated(loading);

            Instance = this;

            NetSegmentDetour.Deploy();
            NetNodeDetour.Deploy();

            if (skinLoaderGO == null) skinLoaderGO = new GameObject("NS SkinLoaders");

            ParseSkinDefs();
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

        public override void OnReleased()
        {
            base.OnReleased();

            NetSegmentDetour.Revert();
            NetNodeDetour.Revert();

            Instance = null;
        }
        */

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

}
