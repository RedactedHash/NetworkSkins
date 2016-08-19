using UnityEngine;
using System.Linq;
using System;
using System.Collections.Generic;

namespace NetworkSkins.Skins
{
    public class SkinLoader : MonoBehaviour
    {
        internal SkinsDefinition.Skin skin;
        internal HashSet<string> log;
        internal string path;
        private bool _isInitialized;

        public void Awake()
        {
            DontDestroyOnLoad(this);
        }

        public void OnLevelWasLoaded(int level)
        {
            if (level == 6)
            {
                _isInitialized = false;
            }
        }

        public void Update()
        {
            if (_isInitialized || skin == null)
            {
                return;
            }

            NetInfo prefab = null;

            try
            {
                var collections = GameObject.FindObjectsOfType<NetCollection>();
                foreach (var collection in collections)
                {
                    foreach (var p in collection.m_prefabs)
                    {
                        if (p.name == skin.NetworkName)
                        {
                            prefab = p;
                        }
                    }
                    if (prefab != null) break;
                }

                if (prefab == null) return;
            }
            catch (Exception)
            {
                return;
            }
            Loading.QueueLoadingAction(() =>
            {
                try
                {
                    skin.CreateMaterials(path, SkinManager.originalSegmentCounts, prefab, log);
                }
                catch (Exception ex)
                {
                    Debug.Log(ex.Message);
                    Debug.Log(ex.ToString());
                }
            });
            _isInitialized = true;
        }
    }
}
