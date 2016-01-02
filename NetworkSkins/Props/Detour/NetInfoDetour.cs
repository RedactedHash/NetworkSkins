﻿using System.Reflection;
using NetworkSkins.Detour;
using UnityEngine;

namespace NetworkSkins.Props
{
    // TODO is this actually needed?
    public class NetInfoDetour : NetInfo
    {
        private static bool deployed = false;

        private static RedirectCallsState _NetInfo_LateUpdate_state;
        private static MethodInfo _NetInfo_LateUpdate_original;
        private static MethodInfo _NetInfo_LateUpdate_detour;

        public static void Deploy()
        {
            if (!deployed)
            {
                // NetInfo.LateUpdate - render (x)
                // NetInfo.CheckReferences - init
                // NetInfo.RefreshLevelOfDetail - init

                // NetLane.CalculateGroupData - init/render
                // NetLane.PopulateGroupData - init/render
                // NetLane.RereshInstance - init/render
                // NetLane.RenderInstance - render

                // NetManager.UpdateSegmentRenderer 

                _NetInfo_LateUpdate_original = typeof(NetInfo).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
                _NetInfo_LateUpdate_detour = typeof(NetInfoDetour).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
                _NetInfo_LateUpdate_state = RedirectionHelper.RedirectCalls(_NetInfo_LateUpdate_original, _NetInfo_LateUpdate_detour);

                deployed = true;
            }
        }

        public static void Revert()
        {
            if (deployed)
            {
                RedirectionHelper.RevertRedirect(_NetInfo_LateUpdate_original, _NetInfo_LateUpdate_state);
                _NetInfo_LateUpdate_original = null;
                _NetInfo_LateUpdate_detour = null;

                deployed = false;
            }
        }

        private static MaterialPropertyBlock m_materialPropertyBlock;
        private static Material m_propDecalSceneMaterial;

        protected override void LateUpdate()
        {
            if (this.m_instanceID.IsEmpty)
            {
                var localToWorldMatrix = base.transform.localToWorldMatrix;
                var vScale = 0.05f;
                var vector = new Vector3(-this.m_halfWidth, 0f, -this.m_segmentLength * 0.5f);
                var vector2 = new Vector3(this.m_halfWidth, 0f, -this.m_segmentLength * 0.5f);
                var vector3 = new Vector3(-this.m_halfWidth, 0f, this.m_segmentLength * 0.5f);
                var vector4 = new Vector3(this.m_halfWidth, 0f, this.m_segmentLength * 0.5f);
                var startDir = new Vector3(0f, 0f, 1f);
                var startDir2 = new Vector3(0f, 0f, 1f);
                var endDir = new Vector3(0f, 0f, -1f);
                var endDir2 = new Vector3(0f, 0f, -1f);
                var smoothStart = false;
                var smoothEnd = false;
                Vector3 vector5;
                Vector3 vector6;
                NetSegment.CalculateMiddlePoints(vector, startDir, vector3, endDir, smoothStart, smoothEnd, out vector5, out vector6);
                Vector3 vector7;
                Vector3 vector8;
                NetSegment.CalculateMiddlePoints(vector2, startDir2, vector4, endDir2, smoothStart, smoothEnd, out vector7, out vector8);
                var value = NetSegment.CalculateControlMatrix(vector, vector5, vector6, vector3, vector2, vector7, vector8, vector4, Vector3.zero, vScale);
                var value2 = NetSegment.CalculateControlMatrix(vector2, vector7, vector8, vector4, vector, vector5, vector6, vector3, Vector3.zero, vScale);
                var value3 = new Vector4(0.5f / this.m_halfWidth, 1f / this.m_segmentLength, 1f, 1f);
                if (m_materialPropertyBlock == null)
                {
                    m_materialPropertyBlock = new MaterialPropertyBlock();
                }
                m_materialPropertyBlock.Clear();
                m_materialPropertyBlock.AddMatrix("_LeftMatrix", value);
                m_materialPropertyBlock.AddMatrix("_RightMatrix", value2);
                m_materialPropertyBlock.AddVector("_MeshScale", value3);
                if (this.m_segments != null)
                {
                    for (var i = 0; i < this.m_segments.Length; i++)
                    {
                        var segment = this.m_segments[i];
                        bool flag;
                        if (segment.CheckFlags(NetSegment.Flags.None, out flag))
                        {
                            Graphics.DrawMesh(segment.m_mesh, localToWorldMatrix, segment.m_material, base.gameObject.layer, null, 0, m_materialPropertyBlock);
                        }
                    }
                }
                if (this.m_lanes != null)
                {
                    for (var j = 0; j < this.m_lanes.Length; j++)
                    {
                        var laneProps = this.m_lanes[j].m_laneProps;
                        if (laneProps != null && laneProps.m_props != null)
                        {
                            var num = laneProps.m_props.Length;
                            for (var k = 0; k < num; k++)
                            {
                                var prop = laneProps.m_props[k];
                                var num2 = 2;
                                if (prop.m_repeatDistance > 1f)
                                {
                                    num2 *= Mathf.Max(1, Mathf.RoundToInt(this.m_segmentLength / prop.m_repeatDistance));
                                }
                                var num3 = prop.m_segmentOffset * 0.5f;
                                if (this.m_segmentLength != 0f)
                                {
                                    num3 = Mathf.Clamp(num3 + prop.m_position.z / this.m_segmentLength, -0.5f, 0.5f);
                                }
                                if ((byte)(this.m_lanes[j].m_direction & NetInfo.Direction.Both) == 2)
                                {
                                    num3 = -num3;
                                }
                                if (prop.m_prop != null)
                                {
                                    if (prop.m_prop.m_mesh == null)
                                    {
                                        prop.m_prop.m_mesh = prop.m_prop.GetComponent<MeshFilter>().sharedMesh;
                                    }
                                    if (prop.m_prop.m_material == null)
                                    {
                                        prop.m_prop.m_material = prop.m_prop.GetComponent<Renderer>().sharedMaterial;
                                        prop.m_prop.m_isDecal = (prop.m_prop.m_material.GetTag("RenderType", false) == "Decal");
                                    }
                                    var material = prop.m_prop.m_material;
                                    var one = Vector3.one;
                                    if (prop.m_prop.m_isDecal)
                                    {
                                        if (m_propDecalSceneMaterial == null)
                                        {
                                            m_propDecalSceneMaterial = new Material(Shader.Find("Custom/Props/Decal/Scene"));
                                        }
                                        m_propDecalSceneMaterial.CopyPropertiesFromMaterial(material);
                                        material = m_propDecalSceneMaterial;
                                        one = new Vector3(1f, 0.01f, 1f);
                                    }
                                    for (var l = 1; l <= num2; l += 2)
                                    {
                                        var num4 = num3 + (float)l / (float)num2 - 0.5f;
                                        var pos = new Vector3(this.m_lanes[j].m_position, this.m_lanes[j].m_verticalOffset, this.m_segmentLength * num4);
                                        if ((byte)(this.m_lanes[j].m_direction & NetInfo.Direction.Both) == 2)
                                        {
                                            pos.x -= prop.m_position.x;
                                        }
                                        else
                                        {
                                            pos.x += prop.m_position.x;
                                        }
                                        pos.y += prop.m_position.y;
                                        var q = Quaternion.AngleAxis((((byte)(this.m_lanes[j].m_direction & NetInfo.Direction.Both) != 2) ? 180f : 0f) + prop.m_angle, Vector3.down);
                                        var matrix4x = default(Matrix4x4);
                                        matrix4x.SetTRS(pos, q, one);
                                        matrix4x = localToWorldMatrix * matrix4x;
                                        Graphics.DrawMesh(prop.m_prop.m_mesh, matrix4x, material, prop.m_prop.gameObject.layer);
                                    }
                                }
                                if (prop.m_tree != null)
                                {
                                    if (prop.m_tree.m_mesh == null)
                                    {
                                        prop.m_tree.m_mesh = prop.m_tree.GetComponent<MeshFilter>().sharedMesh;
                                    }
                                    if (prop.m_tree.m_material == null)
                                    {
                                        prop.m_tree.m_material = prop.m_tree.GetComponent<Renderer>().sharedMaterial;
                                    }
                                    for (var m = 1; m <= num2; m += 2)
                                    {
                                        var num5 = num3 + (float)m / (float)num2 - 0.5f;
                                        var pos2 = new Vector3(this.m_lanes[j].m_position, this.m_lanes[j].m_verticalOffset, this.m_segmentLength * num5);
                                        if ((byte)(this.m_lanes[j].m_direction & NetInfo.Direction.Both) == 2)
                                        {
                                            pos2.x -= prop.m_position.x;
                                        }
                                        else
                                        {
                                            pos2.x += prop.m_position.x;
                                        }
                                        pos2.y += prop.m_position.y;
                                        var matrix4x2 = default(Matrix4x4);
                                        matrix4x2.SetTRS(pos2, Quaternion.identity, Vector3.one);
                                        matrix4x2 = localToWorldMatrix * matrix4x2;
                                        Graphics.DrawMesh(prop.m_tree.m_mesh, matrix4x2, prop.m_tree.m_material, prop.m_tree.gameObject.layer);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                // mod begin (code of PrefabInfo.LateUpdate)
                //base.LateUpdate();
                if (this.m_instanceNeeded)
                {
                    this.m_instanceNeeded = false;
                }
                else
                {
                    this.ReleasePrefabInstance();
                }
                // mod end
            }
        }
    }
}
