using ColossalFramework;
using NetworkSkins.Data;
using NetworkSkins.Detour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace NetworkSkins.Skins
{
    public class NetNodeDetour
    {
        private static bool deployed = false;

        private static RedirectCallsState _NetNode_RenderInstance_state;
        private static MethodInfo _NetNode_RenderInstance_original;
        private static MethodInfo _NetNode_RenderInstance_detour;

        private static RedirectCallsState _NetNode_PopulateGroupData_state;
        private static MethodInfo _NetNode_PopulateGroupData_original;
        private static MethodInfo _NetNode_PopulateGroupData_detour;

        public static void Deploy()
        {
            if (!deployed)
            {
                _NetNode_RenderInstance_original = typeof(NetNode).GetMethod("RenderInstance", BindingFlags.Instance | BindingFlags.NonPublic);
                _NetNode_RenderInstance_detour = typeof(NetNodeDetour).GetMethod("RenderInstance", BindingFlags.Instance | BindingFlags.NonPublic);
                _NetNode_RenderInstance_state = RedirectionHelper.RedirectCalls(_NetNode_RenderInstance_original, _NetNode_RenderInstance_detour);

                _NetNode_PopulateGroupData_original = typeof(NetNode).GetMethod("PopulateGroupData", BindingFlags.Instance | BindingFlags.Public);
                _NetNode_PopulateGroupData_detour = typeof(NetNodeDetour).GetMethod("PopulateGroupData", BindingFlags.Static | BindingFlags.Public);
                _NetNode_PopulateGroupData_state = RedirectionHelper.RedirectCalls(_NetNode_PopulateGroupData_original, _NetNode_PopulateGroupData_detour);

                deployed = true;
            }
        }

        public static void Revert()
        {
            if (deployed)
            {
                RedirectionHelper.RevertRedirect(_NetNode_RenderInstance_original, _NetNode_RenderInstance_state);
                _NetNode_RenderInstance_original = null;
                _NetNode_RenderInstance_detour = null;

                RedirectionHelper.RevertRedirect(_NetNode_PopulateGroupData_original, _NetNode_PopulateGroupData_state);
                _NetNode_PopulateGroupData_original = null;
                _NetNode_PopulateGroupData_detour = null;

                deployed = false;
            }
        }

        // NetNode
        private void RenderInstance(RenderManager.CameraInfo cameraInfo, ushort nodeID, NetInfo info, int iter, NetNode.Flags flags, ref uint instanceIndex, ref RenderManager.Instance data)
        {
            var _this = NetManager.instance.m_nodes.m_buffer[nodeID];

            if (data.m_dirty)
            {
                data.m_dirty = false;
                if (iter == 0)
                {
                    if ((flags & NetNode.Flags.Junction) != NetNode.Flags.None)
                    {
                        RefreshJunctionData(_this, nodeID, info, instanceIndex);
                    }
                    else if ((flags & NetNode.Flags.Bend) != NetNode.Flags.None)
                    {
                        RefreshBendData(_this, nodeID, info, instanceIndex, ref data);
                    }
                    else if ((flags & NetNode.Flags.End) != NetNode.Flags.None)
                    {
                        RefreshEndData(_this, nodeID, info, instanceIndex, ref data);
                    }
                }
            }
            if (data.m_initialized)
            {
                if ((flags & NetNode.Flags.Junction) != NetNode.Flags.None)
                {
                    if ((data.m_dataInt0 & 8) != 0)
                    {
                        ushort segment = _this.GetSegment(data.m_dataInt0 & 7);
                        ushort segment2 = _this.GetSegment(data.m_dataInt0 >> 4);
                        if (segment != 0 && segment2 != 0)
                        {
                            NetManager instance = Singleton<NetManager>.instance;
                            info = instance.m_segments.m_buffer[(int)segment].Info;
                            NetInfo info2 = instance.m_segments.m_buffer[(int)segment2].Info;
                            for (int i = 0; i < info.m_nodes.Length; i++)
                            {
                                NetInfo.Node node = info.m_nodes[i];
                                if (node.CheckFlags(flags) && node.m_directConnect && (node.m_connectGroup == NetInfo.ConnectGroup.None || (node.m_connectGroup & info2.m_connectGroup & NetInfo.ConnectGroup.AllGroups) != NetInfo.ConnectGroup.None))
                                {
                                    Vector4 dataVector = data.m_dataVector3;
                                    Vector4 dataVector2 = data.m_dataVector0;
                                    if (node.m_requireWindSpeed)
                                    {
                                        dataVector.w = data.m_dataFloat0;
                                    }
                                    if ((node.m_connectGroup & NetInfo.ConnectGroup.Oneway) != NetInfo.ConnectGroup.None)
                                    {
                                        bool flag = instance.m_segments.m_buffer[(int)segment].m_startNode == nodeID == ((instance.m_segments.m_buffer[(int)segment].m_flags & NetSegment.Flags.Invert) == NetSegment.Flags.None);
                                        if (info2.m_hasBackwardVehicleLanes != info2.m_hasForwardVehicleLanes)
                                        {
                                            bool flag2 = instance.m_segments.m_buffer[(int)segment2].m_startNode == nodeID == ((instance.m_segments.m_buffer[(int)segment2].m_flags & NetSegment.Flags.Invert) == NetSegment.Flags.None);
                                            if (flag == flag2)
                                            {
                                                goto IL_51C;
                                            }
                                        }
                                        if (flag)
                                        {
                                            if ((node.m_connectGroup & NetInfo.ConnectGroup.OnewayStart) == NetInfo.ConnectGroup.None)
                                            {
                                                goto IL_51C;
                                            }
                                        }
                                        else
                                        {
                                            if ((node.m_connectGroup & NetInfo.ConnectGroup.OnewayEnd) == NetInfo.ConnectGroup.None)
                                            {
                                                goto IL_51C;
                                            }
                                            dataVector2.x = -dataVector2.x;
                                            dataVector2.y = -dataVector2.y;
                                        }
                                    }
                                    if (cameraInfo.CheckRenderDistance(data.m_position, node.m_lodRenderDistance))
                                    {
                                        instance.m_materialBlock.Clear();
                                        instance.m_materialBlock.AddMatrix(instance.ID_LeftMatrix, data.m_dataMatrix0);
                                        instance.m_materialBlock.AddMatrix(instance.ID_RightMatrix, data.m_extraData.m_dataMatrix2);
                                        instance.m_materialBlock.AddVector(instance.ID_MeshScale, dataVector2);
                                        instance.m_materialBlock.AddVector(instance.ID_ObjectIndex, dataVector);
                                        instance.m_materialBlock.AddColor(instance.ID_Color, data.m_dataColor0);
                                        if (node.m_requireSurfaceMaps && data.m_dataTexture1 != null)
                                        {
                                            instance.m_materialBlock.AddTexture(instance.ID_SurfaceTexA, data.m_dataTexture0);
                                            instance.m_materialBlock.AddTexture(instance.ID_SurfaceTexB, data.m_dataTexture1);
                                            instance.m_materialBlock.AddVector(instance.ID_SurfaceMapping, data.m_dataVector1);
                                        }
                                        NetManager expr_36F_cp_0 = instance;
                                        expr_36F_cp_0.m_drawCallData.m_defaultCalls = expr_36F_cp_0.m_drawCallData.m_defaultCalls + 1;
                                        Graphics.DrawMesh(node.m_nodeMesh, data.m_position, data.m_rotation, node.m_nodeMaterial, node.m_layer, null, 0, instance.m_materialBlock);
                                    }
                                    else
                                    {
                                        NetInfo.LodValue combinedLod = node.m_combinedLod;
                                        if (combinedLod != null)
                                        {
                                            if (node.m_requireSurfaceMaps && data.m_dataTexture0 != combinedLod.m_surfaceTexA)
                                            {
                                                if (combinedLod.m_lodCount != 0)
                                                {
                                                    NetSegment.RenderLod(cameraInfo, combinedLod);
                                                }
                                                combinedLod.m_surfaceTexA = data.m_dataTexture0;
                                                combinedLod.m_surfaceTexB = data.m_dataTexture1;
                                                combinedLod.m_surfaceMapping = data.m_dataVector1;
                                            }
                                            combinedLod.m_leftMatrices[combinedLod.m_lodCount] = data.m_dataMatrix0;
                                            combinedLod.m_rightMatrices[combinedLod.m_lodCount] = data.m_extraData.m_dataMatrix2;
                                            combinedLod.m_meshScales[combinedLod.m_lodCount] = dataVector2;
                                            combinedLod.m_objectIndices[combinedLod.m_lodCount] = dataVector;
                                            combinedLod.m_meshLocations[combinedLod.m_lodCount] = data.m_position;
                                            combinedLod.m_lodMin = Vector3.Min(combinedLod.m_lodMin, data.m_position);
                                            combinedLod.m_lodMax = Vector3.Max(combinedLod.m_lodMax, data.m_position);
                                            if (++combinedLod.m_lodCount == combinedLod.m_leftMatrices.Length)
                                            {
                                                NetSegment.RenderLod(cameraInfo, combinedLod);
                                            }
                                        }
                                    }
                                }
                            IL_51C:;
                            }
                        }
                    }
                    else
                    {
                        ushort segment3 = _this.GetSegment(data.m_dataInt0 & 7);
                        if (segment3 != 0)
                        {
                            NetManager instance2 = Singleton<NetManager>.instance;
                            info = instance2.m_segments.m_buffer[(int)segment3].Info;
                            for (int j = 0; j < info.m_nodes.Length; j++)
                            {
                                NetInfo.Node node2 = info.m_nodes[j];
                                if (node2.CheckFlags(flags) && !node2.m_directConnect)
                                {
                                    Vector4 dataVector3 = data.m_extraData.m_dataVector4;
                                    if (node2.m_requireWindSpeed)
                                    {
                                        dataVector3.w = data.m_dataFloat0;
                                    }
                                    if (cameraInfo.CheckRenderDistance(data.m_position, node2.m_lodRenderDistance))
                                    {
                                        instance2.m_materialBlock.Clear();
                                        instance2.m_materialBlock.AddMatrix(instance2.ID_LeftMatrix, data.m_dataMatrix0);
                                        instance2.m_materialBlock.AddMatrix(instance2.ID_RightMatrix, data.m_extraData.m_dataMatrix2);
                                        instance2.m_materialBlock.AddMatrix(instance2.ID_LeftMatrixB, data.m_extraData.m_dataMatrix3);
                                        instance2.m_materialBlock.AddMatrix(instance2.ID_RightMatrixB, data.m_dataMatrix1);
                                        instance2.m_materialBlock.AddVector(instance2.ID_MeshScale, data.m_dataVector0);
                                        instance2.m_materialBlock.AddVector(instance2.ID_CenterPos, data.m_dataVector1);
                                        instance2.m_materialBlock.AddVector(instance2.ID_SideScale, data.m_dataVector2);
                                        instance2.m_materialBlock.AddVector(instance2.ID_ObjectIndex, dataVector3);
                                        instance2.m_materialBlock.AddColor(instance2.ID_Color, data.m_dataColor0);
                                        if (node2.m_requireSurfaceMaps && data.m_dataTexture1 != null)
                                        {
                                            instance2.m_materialBlock.AddTexture(instance2.ID_SurfaceTexA, data.m_dataTexture0);
                                            instance2.m_materialBlock.AddTexture(instance2.ID_SurfaceTexB, data.m_dataTexture1);
                                            instance2.m_materialBlock.AddVector(instance2.ID_SurfaceMapping, data.m_dataVector3);
                                        }
                                        NetManager expr_74B_cp_0 = instance2;
                                        expr_74B_cp_0.m_drawCallData.m_defaultCalls = expr_74B_cp_0.m_drawCallData.m_defaultCalls + 1;
                                        Graphics.DrawMesh(node2.m_nodeMesh, data.m_position, data.m_rotation, node2.m_nodeMaterial, node2.m_layer, null, 0, instance2.m_materialBlock);
                                    }
                                    else
                                    {
                                        NetInfo.LodValue combinedLod2 = node2.m_combinedLod;
                                        if (combinedLod2 != null)
                                        {
                                            if (node2.m_requireSurfaceMaps && data.m_dataTexture0 != combinedLod2.m_surfaceTexA)
                                            {
                                                if (combinedLod2.m_lodCount != 0)
                                                {
                                                    NetNode.RenderLod(cameraInfo, combinedLod2);
                                                }
                                                combinedLod2.m_surfaceTexA = data.m_dataTexture0;
                                                combinedLod2.m_surfaceTexB = data.m_dataTexture1;
                                                combinedLod2.m_surfaceMapping = data.m_dataVector3;
                                            }
                                            combinedLod2.m_leftMatrices[combinedLod2.m_lodCount] = data.m_dataMatrix0;
                                            combinedLod2.m_leftMatricesB[combinedLod2.m_lodCount] = data.m_extraData.m_dataMatrix3;
                                            combinedLod2.m_rightMatrices[combinedLod2.m_lodCount] = data.m_extraData.m_dataMatrix2;
                                            combinedLod2.m_rightMatricesB[combinedLod2.m_lodCount] = data.m_dataMatrix1;
                                            combinedLod2.m_meshScales[combinedLod2.m_lodCount] = data.m_dataVector0;
                                            combinedLod2.m_centerPositions[combinedLod2.m_lodCount] = data.m_dataVector1;
                                            combinedLod2.m_sideScales[combinedLod2.m_lodCount] = data.m_dataVector2;
                                            combinedLod2.m_objectIndices[combinedLod2.m_lodCount] = dataVector3;
                                            combinedLod2.m_meshLocations[combinedLod2.m_lodCount] = data.m_position;
                                            combinedLod2.m_lodMin = Vector3.Min(combinedLod2.m_lodMin, data.m_position);
                                            combinedLod2.m_lodMax = Vector3.Max(combinedLod2.m_lodMax, data.m_position);
                                            if (++combinedLod2.m_lodCount == combinedLod2.m_leftMatrices.Length)
                                            {
                                                NetNode.RenderLod(cameraInfo, combinedLod2);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else if ((flags & NetNode.Flags.End) != NetNode.Flags.None)
                {
                    NetManager instance3 = Singleton<NetManager>.instance;
                    for (int k = 0; k < info.m_nodes.Length; k++)
                    {
                        NetInfo.Node node3 = info.m_nodes[k];
                        if (node3.CheckFlags(flags) && !node3.m_directConnect)
                        {
                            Vector4 dataVector4 = data.m_extraData.m_dataVector4;
                            if (node3.m_requireWindSpeed)
                            {
                                dataVector4.w = data.m_dataFloat0;
                            }
                            if (cameraInfo.CheckRenderDistance(data.m_position, node3.m_lodRenderDistance))
                            {
                                instance3.m_materialBlock.Clear();
                                instance3.m_materialBlock.AddMatrix(instance3.ID_LeftMatrix, data.m_dataMatrix0);
                                instance3.m_materialBlock.AddMatrix(instance3.ID_RightMatrix, data.m_extraData.m_dataMatrix2);
                                instance3.m_materialBlock.AddMatrix(instance3.ID_LeftMatrixB, data.m_extraData.m_dataMatrix3);
                                instance3.m_materialBlock.AddMatrix(instance3.ID_RightMatrixB, data.m_dataMatrix1);
                                instance3.m_materialBlock.AddVector(instance3.ID_MeshScale, data.m_dataVector0);
                                instance3.m_materialBlock.AddVector(instance3.ID_CenterPos, data.m_dataVector1);
                                instance3.m_materialBlock.AddVector(instance3.ID_SideScale, data.m_dataVector2);
                                instance3.m_materialBlock.AddVector(instance3.ID_ObjectIndex, dataVector4);
                                instance3.m_materialBlock.AddColor(instance3.ID_Color, data.m_dataColor0);
                                if (node3.m_requireSurfaceMaps && data.m_dataTexture1 != null)
                                {
                                    instance3.m_materialBlock.AddTexture(instance3.ID_SurfaceTexA, data.m_dataTexture0);
                                    instance3.m_materialBlock.AddTexture(instance3.ID_SurfaceTexB, data.m_dataTexture1);
                                    instance3.m_materialBlock.AddVector(instance3.ID_SurfaceMapping, data.m_dataVector3);
                                }
                                NetManager expr_B86_cp_0 = instance3;
                                expr_B86_cp_0.m_drawCallData.m_defaultCalls = expr_B86_cp_0.m_drawCallData.m_defaultCalls + 1;
                                Graphics.DrawMesh(node3.m_nodeMesh, data.m_position, data.m_rotation, node3.m_nodeMaterial, node3.m_layer, null, 0, instance3.m_materialBlock);
                            }
                            else
                            {
                                NetInfo.LodValue combinedLod3 = node3.m_combinedLod;
                                if (combinedLod3 != null)
                                {
                                    if (node3.m_requireSurfaceMaps && data.m_dataTexture0 != combinedLod3.m_surfaceTexA)
                                    {
                                        if (combinedLod3.m_lodCount != 0)
                                        {
                                            NetNode.RenderLod(cameraInfo, combinedLod3);
                                        }
                                        combinedLod3.m_surfaceTexA = data.m_dataTexture0;
                                        combinedLod3.m_surfaceTexB = data.m_dataTexture1;
                                        combinedLod3.m_surfaceMapping = data.m_dataVector3;
                                    }
                                    combinedLod3.m_leftMatrices[combinedLod3.m_lodCount] = data.m_dataMatrix0;
                                    combinedLod3.m_leftMatricesB[combinedLod3.m_lodCount] = data.m_extraData.m_dataMatrix3;
                                    combinedLod3.m_rightMatrices[combinedLod3.m_lodCount] = data.m_extraData.m_dataMatrix2;
                                    combinedLod3.m_rightMatricesB[combinedLod3.m_lodCount] = data.m_dataMatrix1;
                                    combinedLod3.m_meshScales[combinedLod3.m_lodCount] = data.m_dataVector0;
                                    combinedLod3.m_centerPositions[combinedLod3.m_lodCount] = data.m_dataVector1;
                                    combinedLod3.m_sideScales[combinedLod3.m_lodCount] = data.m_dataVector2;
                                    combinedLod3.m_objectIndices[combinedLod3.m_lodCount] = dataVector4;
                                    combinedLod3.m_meshLocations[combinedLod3.m_lodCount] = data.m_position;
                                    combinedLod3.m_lodMin = Vector3.Min(combinedLod3.m_lodMin, data.m_position);
                                    combinedLod3.m_lodMax = Vector3.Max(combinedLod3.m_lodMax, data.m_position);
                                    if (++combinedLod3.m_lodCount == combinedLod3.m_leftMatrices.Length)
                                    {
                                        NetNode.RenderLod(cameraInfo, combinedLod3);
                                    }
                                }
                            }
                        }
                    }
                }
                else if ((flags & NetNode.Flags.Bend) != NetNode.Flags.None)
                {
                    NetManager instance4 = Singleton<NetManager>.instance;

                    // mod begin
                    ushort segmentID = _this.GetSegment(data.m_dataInt0 & 7); // TODO also take into account other segment ID.
                    var skin = SegmentDataManager.Instance.SegmentToSegmentDataMap?[segmentID]?.SkinPrefab;
                    int count;
                    if (!SkinManager.originalSegmentCounts.TryGetValue(info, out count)) count = info.m_segments.Length; // TODO improve performance? array?

                    for (int l = 0; l < count; l++)
                    {
                        NetInfo.Segment segment4 = (skin == null) ? info.m_segments[l] : info.m_segments[skin.segmentRedirectMap[l]];
                        // mod end
                        bool flag3;
                        if (segment4.CheckFlags(info.m_netAI.GetBendFlags(nodeID, ref _this), out flag3) && !segment4.m_disableBendNodes)
                        {
                            Vector4 dataVector5 = data.m_dataVector3;
                            if (segment4.m_requireWindSpeed)
                            {
                                dataVector5.w = data.m_dataFloat0;
                            }
                            if (cameraInfo.CheckRenderDistance(data.m_position, segment4.m_lodRenderDistance))
                            {
                                instance4.m_materialBlock.Clear();
                                instance4.m_materialBlock.AddMatrix(instance4.ID_LeftMatrix, data.m_dataMatrix0);
                                instance4.m_materialBlock.AddMatrix(instance4.ID_RightMatrix, data.m_extraData.m_dataMatrix2);
                                instance4.m_materialBlock.AddVector(instance4.ID_MeshScale, data.m_dataVector0);
                                instance4.m_materialBlock.AddVector(instance4.ID_ObjectIndex, dataVector5);
                                instance4.m_materialBlock.AddColor(instance4.ID_Color, data.m_dataColor0);
                                if (segment4.m_requireSurfaceMaps && data.m_dataTexture1 != null)
                                {
                                    instance4.m_materialBlock.AddTexture(instance4.ID_SurfaceTexA, data.m_dataTexture0);
                                    instance4.m_materialBlock.AddTexture(instance4.ID_SurfaceTexB, data.m_dataTexture1);
                                    instance4.m_materialBlock.AddVector(instance4.ID_SurfaceMapping, data.m_dataVector1);
                                }
                                NetManager expr_F5C_cp_0 = instance4;
                                expr_F5C_cp_0.m_drawCallData.m_defaultCalls = expr_F5C_cp_0.m_drawCallData.m_defaultCalls + 1;
                                Graphics.DrawMesh(segment4.m_segmentMesh, data.m_position, data.m_rotation, segment4.m_segmentMaterial, segment4.m_layer, null, 0, instance4.m_materialBlock);
                            }
                            else
                            {
                                NetInfo.LodValue combinedLod4 = segment4.m_combinedLod;
                                if (combinedLod4 != null)
                                {
                                    if (segment4.m_requireSurfaceMaps && data.m_dataTexture0 != combinedLod4.m_surfaceTexA)
                                    {
                                        if (combinedLod4.m_lodCount != 0)
                                        {
                                            NetSegment.RenderLod(cameraInfo, combinedLod4);
                                        }
                                        combinedLod4.m_surfaceTexA = data.m_dataTexture0;
                                        combinedLod4.m_surfaceTexB = data.m_dataTexture1;
                                        combinedLod4.m_surfaceMapping = data.m_dataVector1;
                                    }
                                    combinedLod4.m_leftMatrices[combinedLod4.m_lodCount] = data.m_dataMatrix0;
                                    combinedLod4.m_rightMatrices[combinedLod4.m_lodCount] = data.m_extraData.m_dataMatrix2;
                                    combinedLod4.m_meshScales[combinedLod4.m_lodCount] = data.m_dataVector0;
                                    combinedLod4.m_objectIndices[combinedLod4.m_lodCount] = dataVector5;
                                    combinedLod4.m_meshLocations[combinedLod4.m_lodCount] = data.m_position;
                                    combinedLod4.m_lodMin = Vector3.Min(combinedLod4.m_lodMin, data.m_position);
                                    combinedLod4.m_lodMax = Vector3.Max(combinedLod4.m_lodMax, data.m_position);
                                    if (++combinedLod4.m_lodCount == combinedLod4.m_leftMatrices.Length)
                                    {
                                        NetSegment.RenderLod(cameraInfo, combinedLod4);
                                    }
                                }
                            }
                        }
                    }
                    for (int m = 0; m < info.m_nodes.Length; m++)
                    {
                        NetInfo.Node node4 = info.m_nodes[m];
                        if (node4.CheckFlags(flags) && node4.m_directConnect && (node4.m_connectGroup == NetInfo.ConnectGroup.None || (node4.m_connectGroup & info.m_connectGroup & NetInfo.ConnectGroup.AllGroups) != NetInfo.ConnectGroup.None))
                        {
                            Vector4 dataVector6 = data.m_dataVector3;
                            Vector4 dataVector7 = data.m_dataVector0;
                            if (node4.m_requireWindSpeed)
                            {
                                dataVector6.w = data.m_dataFloat0;
                            }
                            if ((node4.m_connectGroup & NetInfo.ConnectGroup.Oneway) != NetInfo.ConnectGroup.None)
                            {
                                ushort segment5 = _this.GetSegment(data.m_dataInt0 & 7);
                                ushort segment6 = _this.GetSegment(data.m_dataInt0 >> 4);
                                bool flag4 = instance4.m_segments.m_buffer[(int)segment5].m_startNode == nodeID == ((instance4.m_segments.m_buffer[(int)segment5].m_flags & NetSegment.Flags.Invert) == NetSegment.Flags.None);
                                bool flag5 = instance4.m_segments.m_buffer[(int)segment6].m_startNode == nodeID == ((instance4.m_segments.m_buffer[(int)segment6].m_flags & NetSegment.Flags.Invert) == NetSegment.Flags.None);
                                if (flag4 == flag5)
                                {
                                    goto IL_1579;
                                }
                                if (flag4)
                                {
                                    if ((node4.m_connectGroup & NetInfo.ConnectGroup.OnewayStart) == NetInfo.ConnectGroup.None)
                                    {
                                        goto IL_1579;
                                    }
                                }
                                else
                                {
                                    if ((node4.m_connectGroup & NetInfo.ConnectGroup.OnewayEnd) == NetInfo.ConnectGroup.None)
                                    {
                                        goto IL_1579;
                                    }
                                    dataVector7.x = -dataVector7.x;
                                    dataVector7.y = -dataVector7.y;
                                }
                            }
                            if (cameraInfo.CheckRenderDistance(data.m_position, node4.m_lodRenderDistance))
                            {
                                instance4.m_materialBlock.Clear();
                                instance4.m_materialBlock.AddMatrix(instance4.ID_LeftMatrix, data.m_dataMatrix0);
                                instance4.m_materialBlock.AddMatrix(instance4.ID_RightMatrix, data.m_extraData.m_dataMatrix2);
                                instance4.m_materialBlock.AddVector(instance4.ID_MeshScale, dataVector7);
                                instance4.m_materialBlock.AddVector(instance4.ID_ObjectIndex, dataVector6);
                                instance4.m_materialBlock.AddColor(instance4.ID_Color, data.m_dataColor0);
                                if (node4.m_requireSurfaceMaps && data.m_dataTexture1 != null)
                                {
                                    instance4.m_materialBlock.AddTexture(instance4.ID_SurfaceTexA, data.m_dataTexture0);
                                    instance4.m_materialBlock.AddTexture(instance4.ID_SurfaceTexB, data.m_dataTexture1);
                                    instance4.m_materialBlock.AddVector(instance4.ID_SurfaceMapping, data.m_dataVector1);
                                }
                                NetManager expr_13CB_cp_0 = instance4;
                                expr_13CB_cp_0.m_drawCallData.m_defaultCalls = expr_13CB_cp_0.m_drawCallData.m_defaultCalls + 1;
                                Graphics.DrawMesh(node4.m_nodeMesh, data.m_position, data.m_rotation, node4.m_nodeMaterial, node4.m_layer, null, 0, instance4.m_materialBlock);
                            }
                            else
                            {
                                NetInfo.LodValue combinedLod5 = node4.m_combinedLod;
                                if (combinedLod5 != null)
                                {
                                    if (node4.m_requireSurfaceMaps && data.m_dataTexture0 != combinedLod5.m_surfaceTexA)
                                    {
                                        if (combinedLod5.m_lodCount != 0)
                                        {
                                            NetSegment.RenderLod(cameraInfo, combinedLod5);
                                        }
                                        combinedLod5.m_surfaceTexA = data.m_dataTexture0;
                                        combinedLod5.m_surfaceTexB = data.m_dataTexture1;
                                        combinedLod5.m_surfaceMapping = data.m_dataVector1;
                                    }
                                    combinedLod5.m_leftMatrices[combinedLod5.m_lodCount] = data.m_dataMatrix0;
                                    combinedLod5.m_rightMatrices[combinedLod5.m_lodCount] = data.m_extraData.m_dataMatrix2;
                                    combinedLod5.m_meshScales[combinedLod5.m_lodCount] = dataVector7;
                                    combinedLod5.m_objectIndices[combinedLod5.m_lodCount] = dataVector6;
                                    combinedLod5.m_meshLocations[combinedLod5.m_lodCount] = data.m_position;
                                    combinedLod5.m_lodMin = Vector3.Min(combinedLod5.m_lodMin, data.m_position);
                                    combinedLod5.m_lodMax = Vector3.Max(combinedLod5.m_lodMax, data.m_position);
                                    if (++combinedLod5.m_lodCount == combinedLod5.m_leftMatrices.Length)
                                    {
                                        NetSegment.RenderLod(cameraInfo, combinedLod5);
                                    }
                                }
                            }
                        }
                    IL_1579:;
                    }
                }
            }
            instanceIndex = (uint)data.m_nextInstance;
        }
        
        // NetNode
        public static void PopulateGroupData(ref NetNode _this, ushort nodeID, int groupX, int groupZ, int layer, ref int vertexIndex, ref int triangleIndex, Vector3 groupPosition, RenderGroup.MeshData data, ref Vector3 min, ref Vector3 max, ref float maxRenderDistance, ref float maxInstanceDistance, ref bool requireSurfaceMaps)
        {


            NetInfo info = _this.Info;
            if (_this.m_problems != Notification.Problem.None && layer == Singleton<NotificationManager>.instance.m_notificationLayer)
            {
                Vector3 position = _this.m_position;
                position.y += info.m_maxHeight;
                Notification.PopulateGroupData(_this.m_problems, position, 1f, groupX, groupZ, ref vertexIndex, ref triangleIndex, groupPosition, data, ref min, ref max, ref maxRenderDistance, ref maxInstanceDistance);
            }
            bool flag = false;
            if ((_this.m_flags & NetNode.Flags.Junction) != NetNode.Flags.None)
            {
                NetManager instance = Singleton<NetManager>.instance;
                Vector3 a = _this.m_position;
                for (int i = 0; i < 8; i++)
                {
                    ushort segment = _this.GetSegment(i);
                    if (segment != 0)
                    {
                        NetInfo info2 = instance.m_segments.m_buffer[(int)segment].Info;
                        ItemClass connectionClass = info2.GetConnectionClass();
                        Vector3 a2 = (nodeID != instance.m_segments.m_buffer[(int)segment].m_startNode) ? instance.m_segments.m_buffer[(int)segment].m_endDirection : instance.m_segments.m_buffer[(int)segment].m_startDirection;
                        float num = -1f;
                        for (int j = 0; j < 8; j++)
                        {
                            ushort segment2 = _this.GetSegment(j);
                            if (segment2 != 0 && segment2 != segment)
                            {
                                NetInfo info3 = instance.m_segments.m_buffer[(int)segment2].Info;
                                ItemClass connectionClass2 = info3.GetConnectionClass();
                                if (((info.m_netLayers | info2.m_netLayers | info3.m_netLayers) & 1 << layer) != 0 && connectionClass.m_service == connectionClass2.m_service)
                                {
                                    Vector3 vector = (nodeID != instance.m_segments.m_buffer[(int)segment2].m_startNode) ? instance.m_segments.m_buffer[(int)segment2].m_endDirection : instance.m_segments.m_buffer[(int)segment2].m_startDirection;
                                    float num2 = a2.x * vector.x + a2.z * vector.z;
                                    num = Mathf.Max(num, num2);
                                    bool flag2 = info2.m_requireDirectRenderers && (info2.m_nodeConnectGroups == NetInfo.ConnectGroup.None || (info2.m_nodeConnectGroups & info3.m_connectGroup) != NetInfo.ConnectGroup.None);
                                    bool flag3 = info3.m_requireDirectRenderers && (info3.m_nodeConnectGroups == NetInfo.ConnectGroup.None || (info3.m_nodeConnectGroups & info2.m_connectGroup) != NetInfo.ConnectGroup.None);
                                    if (j > i && (flag2 || flag3))
                                    {
                                        float num3 = 0.01f - Mathf.Min(info2.m_maxTurnAngleCos, info3.m_maxTurnAngleCos);
                                        if (num2 < num3)
                                        {
                                            float num4;
                                            if (flag2)
                                            {
                                                num4 = info2.m_netAI.GetNodeInfoPriority(segment, ref instance.m_segments.m_buffer[(int)segment]);
                                            }
                                            else
                                            {
                                                num4 = -1E+08f;
                                            }
                                            float num5;
                                            if (flag3)
                                            {
                                                num5 = info3.m_netAI.GetNodeInfoPriority(segment2, ref instance.m_segments.m_buffer[(int)segment2]);
                                            }
                                            else
                                            {
                                                num5 = -1E+08f;
                                            }
                                            if (num4 >= num5)
                                            {
                                                if (info2.m_nodes != null && info2.m_nodes.Length != 0)
                                                {
                                                    flag = true;
                                                    float vScale = info2.m_netAI.GetVScale();
                                                    Vector3 zero = Vector3.zero;
                                                    Vector3 zero2 = Vector3.zero;
                                                    Vector3 vector2 = Vector3.zero;
                                                    Vector3 vector3 = Vector3.zero;
                                                    Vector3 zero3 = Vector3.zero;
                                                    Vector3 zero4 = Vector3.zero;
                                                    Vector3 zero5 = Vector3.zero;
                                                    Vector3 zero6 = Vector3.zero;
                                                    bool start = Singleton<NetManager>.instance.m_segments.m_buffer[(int)segment].m_startNode == nodeID;
                                                    bool flag4;
                                                    Singleton<NetManager>.instance.m_segments.m_buffer[(int)segment].CalculateCorner(segment, true, start, false, out zero, out zero3, out flag4);
                                                    Singleton<NetManager>.instance.m_segments.m_buffer[(int)segment].CalculateCorner(segment, true, start, true, out zero2, out zero4, out flag4);
                                                    start = (Singleton<NetManager>.instance.m_segments.m_buffer[(int)segment2].m_startNode == nodeID);
                                                    Singleton<NetManager>.instance.m_segments.m_buffer[(int)segment2].CalculateCorner(segment2, true, start, true, out vector2, out zero5, out flag4);
                                                    Singleton<NetManager>.instance.m_segments.m_buffer[(int)segment2].CalculateCorner(segment2, true, start, false, out vector3, out zero6, out flag4);
                                                    Vector3 b = (vector3 - vector2) * (info2.m_halfWidth / info3.m_halfWidth * 0.5f - 0.5f);
                                                    vector2 -= b;
                                                    vector3 += b;
                                                    Vector3 vector4;
                                                    Vector3 vector5;
                                                    NetSegment.CalculateMiddlePoints(zero, -zero3, vector2, -zero5, true, true, out vector4, out vector5);
                                                    Vector3 vector6;
                                                    Vector3 vector7;
                                                    NetSegment.CalculateMiddlePoints(zero2, -zero4, vector3, -zero6, true, true, out vector6, out vector7);
                                                    Matrix4x4 leftMatrix = NetSegment.CalculateControlMatrix(zero, vector4, vector5, vector2, zero2, vector6, vector7, vector3, groupPosition, vScale);
                                                    Matrix4x4 rightMatrix = NetSegment.CalculateControlMatrix(zero2, vector6, vector7, vector3, zero, vector4, vector5, vector2, groupPosition, vScale);
                                                    Vector4 vector8 = new Vector4(0.5f / info2.m_halfWidth, 1f / info2.m_segmentLength, 1f, 1f);
                                                    Vector4 colorLocation;
                                                    Vector4 vector9;
                                                    if (NetNode.BlendJunction(nodeID))
                                                    {
                                                        colorLocation = RenderManager.GetColorLocation(86016u + (uint)nodeID);
                                                        vector9 = colorLocation;
                                                    }
                                                    else
                                                    {
                                                        colorLocation = RenderManager.GetColorLocation((uint)(49152 + segment));
                                                        vector9 = RenderManager.GetColorLocation((uint)(49152 + segment2));
                                                    }
                                                    Vector4 vector10 = new Vector4(colorLocation.x, colorLocation.y, vector9.x, vector9.y);
                                                    for (int k = 0; k < info2.m_nodes.Length; k++)
                                                    {
                                                        NetInfo.Node node = info2.m_nodes[k];
                                                        if ((node.m_connectGroup == NetInfo.ConnectGroup.None || (node.m_connectGroup & info3.m_connectGroup & NetInfo.ConnectGroup.AllGroups) != NetInfo.ConnectGroup.None) && node.m_layer == layer && node.CheckFlags(_this.m_flags) && node.m_combinedLod != null && node.m_directConnect)
                                                        {
                                                            Vector4 objectIndex = vector10;
                                                            Vector4 meshScale = vector8;
                                                            if (node.m_requireWindSpeed)
                                                            {
                                                                objectIndex.w = Singleton<WeatherManager>.instance.GetWindSpeed(_this.m_position);
                                                            }
                                                            if ((node.m_connectGroup & NetInfo.ConnectGroup.Oneway) != NetInfo.ConnectGroup.None)
                                                            {
                                                                bool flag5 = instance.m_segments.m_buffer[(int)segment].m_startNode == nodeID == ((instance.m_segments.m_buffer[(int)segment].m_flags & NetSegment.Flags.Invert) == NetSegment.Flags.None);
                                                                if (info3.m_hasBackwardVehicleLanes != info3.m_hasForwardVehicleLanes)
                                                                {
                                                                    bool flag6 = instance.m_segments.m_buffer[(int)segment2].m_startNode == nodeID == ((instance.m_segments.m_buffer[(int)segment2].m_flags & NetSegment.Flags.Invert) == NetSegment.Flags.None);
                                                                    if (flag5 == flag6)
                                                                    {
                                                                        goto IL_75A;
                                                                    }
                                                                }
                                                                if (flag5)
                                                                {
                                                                    if ((node.m_connectGroup & NetInfo.ConnectGroup.OnewayStart) == NetInfo.ConnectGroup.None)
                                                                    {
                                                                        goto IL_75A;
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    if ((node.m_connectGroup & NetInfo.ConnectGroup.OnewayEnd) == NetInfo.ConnectGroup.None)
                                                                    {
                                                                        goto IL_75A;
                                                                    }
                                                                    meshScale.x = -meshScale.x;
                                                                    meshScale.y = -meshScale.y;
                                                                }
                                                            }
                                                            NetNode.PopulateGroupData(info2, node, leftMatrix, rightMatrix, meshScale, objectIndex, ref vertexIndex, ref triangleIndex, data, ref requireSurfaceMaps);
                                                        }
                                                    IL_75A:;
                                                    }
                                                }
                                            }
                                            else if (info3.m_nodes != null && info3.m_nodes.Length != 0)
                                            {
                                                flag = true;
                                                float vScale2 = info3.m_netAI.GetVScale();
                                                Vector3 vector11 = Vector3.zero;
                                                Vector3 vector12 = Vector3.zero;
                                                Vector3 zero7 = Vector3.zero;
                                                Vector3 zero8 = Vector3.zero;
                                                Vector3 zero9 = Vector3.zero;
                                                Vector3 zero10 = Vector3.zero;
                                                Vector3 zero11 = Vector3.zero;
                                                Vector3 zero12 = Vector3.zero;
                                                bool start2 = Singleton<NetManager>.instance.m_segments.m_buffer[(int)segment].m_startNode == nodeID;
                                                bool flag7;
                                                Singleton<NetManager>.instance.m_segments.m_buffer[(int)segment].CalculateCorner(segment, true, start2, false, out vector11, out zero9, out flag7);
                                                Singleton<NetManager>.instance.m_segments.m_buffer[(int)segment].CalculateCorner(segment, true, start2, true, out vector12, out zero10, out flag7);
                                                start2 = (Singleton<NetManager>.instance.m_segments.m_buffer[(int)segment2].m_startNode == nodeID);
                                                Singleton<NetManager>.instance.m_segments.m_buffer[(int)segment2].CalculateCorner(segment2, true, start2, true, out zero7, out zero11, out flag7);
                                                Singleton<NetManager>.instance.m_segments.m_buffer[(int)segment2].CalculateCorner(segment2, true, start2, false, out zero8, out zero12, out flag7);
                                                Vector3 b2 = (vector12 - vector11) * (info3.m_halfWidth / info2.m_halfWidth * 0.5f - 0.5f);
                                                vector11 -= b2;
                                                vector12 += b2;
                                                Vector3 vector13;
                                                Vector3 vector14;
                                                NetSegment.CalculateMiddlePoints(vector11, -zero9, zero7, -zero11, true, true, out vector13, out vector14);
                                                Vector3 vector15;
                                                Vector3 vector16;
                                                NetSegment.CalculateMiddlePoints(vector12, -zero10, zero8, -zero12, true, true, out vector15, out vector16);
                                                Matrix4x4 leftMatrix2 = NetSegment.CalculateControlMatrix(vector11, vector13, vector14, zero7, vector12, vector15, vector16, zero8, groupPosition, vScale2);
                                                Matrix4x4 rightMatrix2 = NetSegment.CalculateControlMatrix(vector12, vector15, vector16, zero8, vector11, vector13, vector14, zero7, groupPosition, vScale2);
                                                Vector4 vector17 = new Vector4(0.5f / info3.m_halfWidth, 1f / info3.m_segmentLength, 1f, 1f);
                                                Vector4 colorLocation2;
                                                Vector4 vector18;
                                                if (NetNode.BlendJunction(nodeID))
                                                {
                                                    colorLocation2 = RenderManager.GetColorLocation(86016u + (uint)nodeID);
                                                    vector18 = colorLocation2;
                                                }
                                                else
                                                {
                                                    colorLocation2 = RenderManager.GetColorLocation((uint)(49152 + segment));
                                                    vector18 = RenderManager.GetColorLocation((uint)(49152 + segment2));
                                                }
                                                Vector4 vector19 = new Vector4(colorLocation2.x, colorLocation2.y, vector18.x, vector18.y);
                                                for (int l = 0; l < info3.m_nodes.Length; l++)
                                                {
                                                    NetInfo.Node node2 = info3.m_nodes[l];
                                                    if ((node2.m_connectGroup == NetInfo.ConnectGroup.None || (node2.m_connectGroup & info2.m_connectGroup & NetInfo.ConnectGroup.AllGroups) != NetInfo.ConnectGroup.None) && node2.m_layer == layer && node2.CheckFlags(_this.m_flags) && node2.m_combinedLod != null && node2.m_directConnect)
                                                    {
                                                        Vector4 objectIndex2 = vector19;
                                                        Vector4 meshScale2 = vector17;
                                                        if (node2.m_requireWindSpeed)
                                                        {
                                                            objectIndex2.w = Singleton<WeatherManager>.instance.GetWindSpeed(_this.m_position);
                                                        }
                                                        if ((node2.m_connectGroup & NetInfo.ConnectGroup.Oneway) != NetInfo.ConnectGroup.None)
                                                        {
                                                            bool flag8 = instance.m_segments.m_buffer[(int)segment2].m_startNode == nodeID == ((instance.m_segments.m_buffer[(int)segment2].m_flags & NetSegment.Flags.Invert) == NetSegment.Flags.None);
                                                            if (info2.m_hasBackwardVehicleLanes != info2.m_hasForwardVehicleLanes)
                                                            {
                                                                bool flag9 = instance.m_segments.m_buffer[(int)segment].m_startNode == nodeID == ((instance.m_segments.m_buffer[(int)segment].m_flags & NetSegment.Flags.Invert) == NetSegment.Flags.None);
                                                                if (flag9 == flag8)
                                                                {
                                                                    goto IL_BA9;
                                                                }
                                                            }
                                                            if (flag8)
                                                            {
                                                                if ((node2.m_connectGroup & NetInfo.ConnectGroup.OnewayStart) == NetInfo.ConnectGroup.None)
                                                                {
                                                                    goto IL_BA9;
                                                                }
                                                                meshScale2.x = -meshScale2.x;
                                                                meshScale2.y = -meshScale2.y;
                                                            }
                                                            else if ((node2.m_connectGroup & NetInfo.ConnectGroup.OnewayEnd) == NetInfo.ConnectGroup.None)
                                                            {
                                                                goto IL_BA9;
                                                            }
                                                        }
                                                        NetNode.PopulateGroupData(info3, node2, leftMatrix2, rightMatrix2, meshScale2, objectIndex2, ref vertexIndex, ref triangleIndex, data, ref requireSurfaceMaps);
                                                    }
                                                IL_BA9:;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        a += a2 * (2f + num * 2f);
                    }
                }
                a.y = _this.m_position.y + (float)_this.m_heightOffset * 0.015625f;
                if ((info.m_netLayers & 1 << layer) != 0 && info.m_requireSegmentRenderers)
                {
                    for (int m = 0; m < 8; m++)
                    {
                        ushort segment3 = _this.GetSegment(m);
                        if (segment3 != 0)
                        {
                            NetInfo info4 = instance.m_segments.m_buffer[(int)segment3].Info;
                            if (info4.m_nodes != null && info4.m_nodes.Length != 0)
                            {
                                flag = true;
                                float vScale3 = info4.m_netAI.GetVScale();
                                Vector3 zero13 = Vector3.zero;
                                Vector3 zero14 = Vector3.zero;
                                Vector3 zero15 = Vector3.zero;
                                Vector3 zero16 = Vector3.zero;
                                Vector3 vector20 = Vector3.zero;
                                Vector3 vector21 = Vector3.zero;
                                Vector3 a3 = Vector3.zero;
                                Vector3 a4 = Vector3.zero;
                                Vector3 zero17 = Vector3.zero;
                                Vector3 zero18 = Vector3.zero;
                                Vector3 zero19 = Vector3.zero;
                                Vector3 zero20 = Vector3.zero;
                                NetSegment netSegment = instance.m_segments.m_buffer[(int)segment3];
                                ItemClass connectionClass3 = info4.GetConnectionClass();
                                Vector3 vector22 = (nodeID != netSegment.m_startNode) ? netSegment.m_endDirection : netSegment.m_startDirection;
                                float num6 = -4f;
                                float num7 = -4f;
                                ushort num8 = 0;
                                ushort num9 = 0;
                                for (int n = 0; n < 8; n++)
                                {
                                    ushort segment4 = _this.GetSegment(n);
                                    if (segment4 != 0 && segment4 != segment3)
                                    {
                                        NetInfo info5 = instance.m_segments.m_buffer[(int)segment4].Info;
                                        ItemClass connectionClass4 = info5.GetConnectionClass();
                                        if (connectionClass3.m_service == connectionClass4.m_service)
                                        {
                                            NetSegment netSegment2 = instance.m_segments.m_buffer[(int)segment4];
                                            Vector3 vector23 = (nodeID != netSegment2.m_startNode) ? netSegment2.m_endDirection : netSegment2.m_startDirection;
                                            float num10 = vector22.x * vector23.x + vector22.z * vector23.z;
                                            if (vector23.z * vector22.x - vector23.x * vector22.z < 0f)
                                            {
                                                if (num10 > num6)
                                                {
                                                    num6 = num10;
                                                    num8 = segment4;
                                                }
                                                num10 = -2f - num10;
                                                if (num10 > num7)
                                                {
                                                    num7 = num10;
                                                    num9 = segment4;
                                                }
                                            }
                                            else
                                            {
                                                if (num10 > num7)
                                                {
                                                    num7 = num10;
                                                    num9 = segment4;
                                                }
                                                num10 = -2f - num10;
                                                if (num10 > num6)
                                                {
                                                    num6 = num10;
                                                    num8 = segment4;
                                                }
                                            }
                                        }
                                    }
                                }
                                bool start3 = netSegment.m_startNode == nodeID;
                                bool flag10;
                                netSegment.CalculateCorner(segment3, true, start3, false, out zero13, out zero15, out flag10);
                                netSegment.CalculateCorner(segment3, true, start3, true, out zero14, out zero16, out flag10);
                                Matrix4x4 leftMatrix3;
                                Matrix4x4 rightMatrix3;
                                Matrix4x4 leftMatrixB;
                                Matrix4x4 rightMatrixB;
                                Vector4 meshScale3;
                                Vector4 centerPos;
                                Vector4 sideScale;
                                if (num8 != 0 && num9 != 0)
                                {
                                    float num11 = info4.m_pavementWidth / info4.m_halfWidth * 0.5f;
                                    float y = 1f;
                                    if (num8 != 0)
                                    {
                                        NetSegment netSegment3 = instance.m_segments.m_buffer[(int)num8];
                                        NetInfo info6 = netSegment3.Info;
                                        start3 = (netSegment3.m_startNode == nodeID);
                                        netSegment3.CalculateCorner(num8, true, start3, true, out vector20, out a3, out flag10);
                                        netSegment3.CalculateCorner(num8, true, start3, false, out vector21, out a4, out flag10);
                                        float num12 = info6.m_pavementWidth / info6.m_halfWidth * 0.5f;
                                        num11 = (num11 + num12) * 0.5f;
                                        y = 2f * info4.m_halfWidth / (info4.m_halfWidth + info6.m_halfWidth);
                                    }
                                    float num13 = info4.m_pavementWidth / info4.m_halfWidth * 0.5f;
                                    float w = 1f;
                                    if (num9 != 0)
                                    {
                                        NetSegment netSegment4 = instance.m_segments.m_buffer[(int)num9];
                                        NetInfo info7 = netSegment4.Info;
                                        start3 = (netSegment4.m_startNode == nodeID);
                                        netSegment4.CalculateCorner(num9, true, start3, true, out zero17, out zero19, out flag10);
                                        netSegment4.CalculateCorner(num9, true, start3, false, out zero18, out zero20, out flag10);
                                        float num14 = info7.m_pavementWidth / info7.m_halfWidth * 0.5f;
                                        num13 = (num13 + num14) * 0.5f;
                                        w = 2f * info4.m_halfWidth / (info4.m_halfWidth + info7.m_halfWidth);
                                    }
                                    Vector3 vector24;
                                    Vector3 vector25;
                                    NetSegment.CalculateMiddlePoints(zero13, -zero15, vector20, -a3, true, true, out vector24, out vector25);
                                    Vector3 vector26;
                                    Vector3 vector27;
                                    NetSegment.CalculateMiddlePoints(zero14, -zero16, vector21, -a4, true, true, out vector26, out vector27);
                                    Vector3 vector28;
                                    Vector3 vector29;
                                    NetSegment.CalculateMiddlePoints(zero13, -zero15, zero17, -zero19, true, true, out vector28, out vector29);
                                    Vector3 vector30;
                                    Vector3 vector31;
                                    NetSegment.CalculateMiddlePoints(zero14, -zero16, zero18, -zero20, true, true, out vector30, out vector31);
                                    leftMatrix3 = NetSegment.CalculateControlMatrix(zero13, vector24, vector25, vector20, zero13, vector24, vector25, vector20, groupPosition, vScale3);
                                    rightMatrix3 = NetSegment.CalculateControlMatrix(zero14, vector26, vector27, vector21, zero14, vector26, vector27, vector21, groupPosition, vScale3);
                                    leftMatrixB = NetSegment.CalculateControlMatrix(zero13, vector28, vector29, zero17, zero13, vector28, vector29, zero17, groupPosition, vScale3);
                                    rightMatrixB = NetSegment.CalculateControlMatrix(zero14, vector30, vector31, zero18, zero14, vector30, vector31, zero18, groupPosition, vScale3);
                                    meshScale3 = new Vector4(0.5f / info4.m_halfWidth, 1f / info4.m_segmentLength, 0.5f - info4.m_pavementWidth / info4.m_halfWidth * 0.5f, info4.m_pavementWidth / info4.m_halfWidth * 0.5f);
                                    centerPos = a - groupPosition;
                                    centerPos.w = (leftMatrix3.m33 + rightMatrix3.m33 + leftMatrixB.m33 + rightMatrixB.m33) * 0.25f;
                                    sideScale = new Vector4(num11, y, num13, w);
                                }
                                else
                                {
                                    a.x = (zero13.x + zero14.x) * 0.5f;
                                    a.z = (zero13.z + zero14.z) * 0.5f;
                                    vector20 = zero14;
                                    vector21 = zero13;
                                    a3 = zero16;
                                    a4 = zero15;
                                    float d = Mathf.Min(info4.m_halfWidth * 1.33333337f, 16f);
                                    Vector3 vector32 = zero13 - zero15 * d;
                                    Vector3 vector33 = vector20 - a3 * d;
                                    Vector3 vector34 = zero14 - zero16 * d;
                                    Vector3 vector35 = vector21 - a4 * d;
                                    Vector3 vector36 = zero13 + zero15 * d;
                                    Vector3 vector37 = vector20 + a3 * d;
                                    Vector3 vector38 = zero14 + zero16 * d;
                                    Vector3 vector39 = vector21 + a4 * d;
                                    leftMatrix3 = NetSegment.CalculateControlMatrix(zero13, vector32, vector33, vector20, zero13, vector32, vector33, vector20, groupPosition, vScale3);
                                    rightMatrix3 = NetSegment.CalculateControlMatrix(zero14, vector38, vector39, vector21, zero14, vector38, vector39, vector21, groupPosition, vScale3);
                                    leftMatrixB = NetSegment.CalculateControlMatrix(zero13, vector36, vector37, vector20, zero13, vector36, vector37, vector20, groupPosition, vScale3);
                                    rightMatrixB = NetSegment.CalculateControlMatrix(zero14, vector34, vector35, vector21, zero14, vector34, vector35, vector21, groupPosition, vScale3);
                                    leftMatrix3.SetRow(3, leftMatrix3.GetRow(3) + new Vector4(0.2f, 0.2f, 0.2f, 0.2f));
                                    rightMatrix3.SetRow(3, rightMatrix3.GetRow(3) + new Vector4(0.2f, 0.2f, 0.2f, 0.2f));
                                    leftMatrixB.SetRow(3, leftMatrixB.GetRow(3) + new Vector4(0.2f, 0.2f, 0.2f, 0.2f));
                                    rightMatrixB.SetRow(3, rightMatrixB.GetRow(3) + new Vector4(0.2f, 0.2f, 0.2f, 0.2f));
                                    meshScale3 = new Vector4(0.5f / info4.m_halfWidth, 1f / info4.m_segmentLength, 0.5f - info4.m_pavementWidth / info4.m_halfWidth * 0.5f, info4.m_pavementWidth / info4.m_halfWidth * 0.5f);
                                    centerPos = a - groupPosition;
                                    centerPos.w = (leftMatrix3.m33 + rightMatrix3.m33 + leftMatrixB.m33 + rightMatrixB.m33) * 0.25f;
                                    sideScale = new Vector4(info4.m_pavementWidth / info4.m_halfWidth * 0.5f, 1f, info4.m_pavementWidth / info4.m_halfWidth * 0.5f, 1f);
                                }
                                Vector4 colorLocation3;
                                Vector4 vector40;
                                if (NetNode.BlendJunction(nodeID))
                                {
                                    colorLocation3 = RenderManager.GetColorLocation(86016u + (uint)nodeID);
                                    vector40 = colorLocation3;
                                }
                                else
                                {
                                    colorLocation3 = RenderManager.GetColorLocation((uint)(49152 + segment3));
                                    vector40 = RenderManager.GetColorLocation(86016u + (uint)nodeID);
                                }
                                Vector4 vector41 = new Vector4(colorLocation3.x, colorLocation3.y, vector40.x, vector40.y);
                                for (int num15 = 0; num15 < info4.m_nodes.Length; num15++)
                                {
                                    NetInfo.Node node3 = info4.m_nodes[num15];
                                    if (node3.m_layer == layer && node3.CheckFlags(_this.m_flags) && node3.m_combinedLod != null && !node3.m_directConnect)
                                    {
                                        Vector4 objectIndex3 = vector41;
                                        if (node3.m_requireWindSpeed)
                                        {
                                            objectIndex3.w = Singleton<WeatherManager>.instance.GetWindSpeed(_this.m_position);
                                        }
                                        NetNode.PopulateGroupData(info4, node3, leftMatrix3, rightMatrix3, leftMatrixB, rightMatrixB, meshScale3, centerPos, sideScale, objectIndex3, ref vertexIndex, ref triangleIndex, data, ref requireSurfaceMaps);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else if ((info.m_netLayers & 1 << layer) != 0)
            {
                if ((_this.m_flags & NetNode.Flags.End) != NetNode.Flags.None)
                {
                    if (info.m_nodes != null && info.m_nodes.Length != 0)
                    {
                        flag = true;
                        float vScale4 = info.m_netAI.GetVScale() / 1.5f;
                        Vector3 zero21 = Vector3.zero;
                        Vector3 zero22 = Vector3.zero;
                        Vector3 vector42 = Vector3.zero;
                        Vector3 vector43 = Vector3.zero;
                        Vector3 zero23 = Vector3.zero;
                        Vector3 zero24 = Vector3.zero;
                        Vector3 a5 = Vector3.zero;
                        Vector3 a6 = Vector3.zero;
                        ushort num16 = 0;
                        for (int num17 = 0; num17 < 8; num17++)
                        {
                            ushort segment5 = _this.GetSegment(num17);
                            if (segment5 != 0)
                            {
                                NetSegment netSegment5 = Singleton<NetManager>.instance.m_segments.m_buffer[(int)segment5];
                                bool start4 = netSegment5.m_startNode == nodeID;
                                bool flag11;
                                netSegment5.CalculateCorner(segment5, true, start4, false, out zero21, out zero23, out flag11);
                                netSegment5.CalculateCorner(segment5, true, start4, true, out zero22, out zero24, out flag11);
                                vector42 = zero22;
                                vector43 = zero21;
                                a5 = zero24;
                                a6 = zero23;
                                num16 = segment5;
                            }
                        }
                        float d2 = info.m_netAI.GetEndRadius() * 1.33333337f;
                        Vector3 vector44 = zero21 - zero23 * d2;
                        Vector3 vector45 = vector42 - a5 * d2;
                        Vector3 vector46 = zero22 - zero24 * d2;
                        Vector3 vector47 = vector43 - a6 * d2;
                        Vector3 vector48 = zero21 + zero23 * d2;
                        Vector3 vector49 = vector42 + a5 * d2;
                        Vector3 vector50 = zero22 + zero24 * d2;
                        Vector3 vector51 = vector43 + a6 * d2;
                        Matrix4x4 leftMatrix4 = NetSegment.CalculateControlMatrix(zero21, vector44, vector45, vector42, zero21, vector44, vector45, vector42, groupPosition, vScale4);
                        Matrix4x4 rightMatrix4 = NetSegment.CalculateControlMatrix(zero22, vector50, vector51, vector43, zero22, vector50, vector51, vector43, groupPosition, vScale4);
                        Matrix4x4 leftMatrixB2 = NetSegment.CalculateControlMatrix(zero21, vector48, vector49, vector42, zero21, vector48, vector49, vector42, groupPosition, vScale4);
                        Matrix4x4 rightMatrixB2 = NetSegment.CalculateControlMatrix(zero22, vector46, vector47, vector43, zero22, vector46, vector47, vector43, groupPosition, vScale4);
                        leftMatrix4.SetRow(3, leftMatrix4.GetRow(3) + new Vector4(0.2f, 0.2f, 0.2f, 0.2f));
                        rightMatrix4.SetRow(3, rightMatrix4.GetRow(3) + new Vector4(0.2f, 0.2f, 0.2f, 0.2f));
                        leftMatrixB2.SetRow(3, leftMatrixB2.GetRow(3) + new Vector4(0.2f, 0.2f, 0.2f, 0.2f));
                        rightMatrixB2.SetRow(3, rightMatrixB2.GetRow(3) + new Vector4(0.2f, 0.2f, 0.2f, 0.2f));
                        Vector4 meshScale4 = new Vector4(0.5f / info.m_halfWidth, 1f / info.m_segmentLength, 0.5f - info.m_pavementWidth / info.m_halfWidth * 0.5f, info.m_pavementWidth / info.m_halfWidth * 0.5f);
                        Vector4 centerPos2 = new Vector4(_this.m_position.x - groupPosition.x, _this.m_position.y - groupPosition.y + (float)_this.m_heightOffset * 0.015625f, _this.m_position.z - groupPosition.z, 0f);
                        centerPos2.w = (leftMatrix4.m33 + rightMatrix4.m33 + leftMatrixB2.m33 + rightMatrixB2.m33) * 0.25f;
                        Vector4 sideScale2 = new Vector4(info.m_pavementWidth / info.m_halfWidth * 0.5f, 1f, info.m_pavementWidth / info.m_halfWidth * 0.5f, 1f);
                        Vector4 colorLocation4 = RenderManager.GetColorLocation((uint)(49152 + num16));
                        Vector4 vector52 = new Vector4(colorLocation4.x, colorLocation4.y, colorLocation4.x, colorLocation4.y);
                        for (int num18 = 0; num18 < info.m_nodes.Length; num18++)
                        {
                            NetInfo.Node node4 = info.m_nodes[num18];
                            if (node4.m_layer == layer && node4.CheckFlags(_this.m_flags) && node4.m_combinedLod != null && !node4.m_directConnect)
                            {
                                Vector4 objectIndex4 = vector52;
                                if (node4.m_requireWindSpeed)
                                {
                                    objectIndex4.w = Singleton<WeatherManager>.instance.GetWindSpeed(_this.m_position);
                                }
                                NetNode.PopulateGroupData(info, node4, leftMatrix4, rightMatrix4, leftMatrixB2, rightMatrixB2, meshScale4, centerPos2, sideScale2, objectIndex4, ref vertexIndex, ref triangleIndex, data, ref requireSurfaceMaps);
                            }
                        }
                    }
                }
                else if ((_this.m_flags & NetNode.Flags.Bend) != NetNode.Flags.None && ((info.m_segments != null && info.m_segments.Length != 0) || (info.m_nodes != null && info.m_nodes.Length != 0)))
                {
                    float vScale5 = info.m_netAI.GetVScale();
                    Vector3 zero25 = Vector3.zero;
                    Vector3 zero26 = Vector3.zero;
                    Vector3 zero27 = Vector3.zero;
                    Vector3 zero28 = Vector3.zero;
                    Vector3 zero29 = Vector3.zero;
                    Vector3 zero30 = Vector3.zero;
                    Vector3 zero31 = Vector3.zero;
                    Vector3 zero32 = Vector3.zero;
                    ushort num19 = 0;
                    ushort num20 = 0;
                    bool flag12 = false;
                    int num21 = 0;
                    for (int num22 = 0; num22 < 8; num22++)
                    {
                        ushort segment6 = _this.GetSegment(num22);
                        if (segment6 != 0)
                        {
                            NetSegment netSegment6 = Singleton<NetManager>.instance.m_segments.m_buffer[(int)segment6];
                            bool flag13 = ++num21 == 1;
                            bool flag14 = netSegment6.m_startNode == nodeID;
                            if ((!flag13 && !flag12) || (flag13 && !flag14))
                            {
                                bool flag15;
                                netSegment6.CalculateCorner(segment6, true, flag14, false, out zero25, out zero29, out flag15);
                                netSegment6.CalculateCorner(segment6, true, flag14, true, out zero26, out zero30, out flag15);
                                flag12 = true;
                                num19 = segment6;
                            }
                            else
                            {
                                bool flag15;
                                netSegment6.CalculateCorner(segment6, true, flag14, true, out zero27, out zero31, out flag15);
                                netSegment6.CalculateCorner(segment6, true, flag14, false, out zero28, out zero32, out flag15);
                                num20 = segment6;
                            }
                        }
                    }
                    Vector3 vector53;
                    Vector3 vector54;
                    NetSegment.CalculateMiddlePoints(zero25, -zero29, zero27, -zero31, true, true, out vector53, out vector54);
                    Vector3 vector55;
                    Vector3 vector56;
                    NetSegment.CalculateMiddlePoints(zero26, -zero30, zero28, -zero32, true, true, out vector55, out vector56);
                    Matrix4x4 leftMatrix5 = NetSegment.CalculateControlMatrix(zero25, vector53, vector54, zero27, zero26, vector55, vector56, zero28, groupPosition, vScale5);
                    Matrix4x4 rightMatrix5 = NetSegment.CalculateControlMatrix(zero26, vector55, vector56, zero28, zero25, vector53, vector54, zero27, groupPosition, vScale5);
                    Vector4 vector57 = new Vector4(0.5f / info.m_halfWidth, 1f / info.m_segmentLength, 1f, 1f);
                    Vector4 colorLocation5 = RenderManager.GetColorLocation(86016u + (uint)nodeID);
                    Vector4 vector58 = new Vector4(colorLocation5.x, colorLocation5.y, colorLocation5.x, colorLocation5.y);
                    if (info.m_segments != null && info.m_segments.Length != 0)
                    {

                        // mod begin
                        var skin = SegmentDataManager.Instance.SegmentToSegmentDataMap?[num19]?.SkinPrefab; // TODO num19 vs num20?
                        int count;
                        if (!SkinManager.originalSegmentCounts.TryGetValue(info, out count)) count = info.m_segments.Length; // TODO improve performance? array?

                        for (int num23 = 0; num23 < count; num23++)
                        {
                            NetInfo.Segment segment7 = (skin == null) ? info.m_segments[num23] : info.m_segments[skin.segmentRedirectMap[num23]];
                            // mod end

                            bool flag16;
                            if (segment7.m_layer == layer && segment7.CheckFlags(info.m_netAI.GetBendFlags(nodeID, ref _this), out flag16) && segment7.m_combinedLod != null && !segment7.m_disableBendNodes)
                            {
                                Vector4 objectIndex5 = vector58;
                                if (segment7.m_requireWindSpeed)
                                {
                                    objectIndex5.w = Singleton<WeatherManager>.instance.GetWindSpeed(_this.m_position);
                                }
                                flag = true;
                                NetSegment.PopulateGroupData(info, segment7, leftMatrix5, rightMatrix5, vector57, objectIndex5, ref vertexIndex, ref triangleIndex, data, ref requireSurfaceMaps);
                            }
                        }
                    }
                    if (info.m_nodes != null && info.m_nodes.Length != 0)
                    {
                        for (int num24 = 0; num24 < info.m_nodes.Length; num24++)
                        {
                            NetInfo.Node node5 = info.m_nodes[num24];
                            if ((node5.m_connectGroup == NetInfo.ConnectGroup.None || (node5.m_connectGroup & info.m_connectGroup & NetInfo.ConnectGroup.AllGroups) != NetInfo.ConnectGroup.None) && node5.m_layer == layer && node5.CheckFlags(_this.m_flags) && node5.m_combinedLod != null && node5.m_directConnect)
                            {
                                Vector4 objectIndex6 = vector58;
                                Vector4 meshScale5 = vector57;
                                if (node5.m_requireWindSpeed)
                                {
                                    objectIndex6.w = Singleton<WeatherManager>.instance.GetWindSpeed(_this.m_position);
                                }
                                if ((node5.m_connectGroup & NetInfo.ConnectGroup.Oneway) != NetInfo.ConnectGroup.None)
                                {
                                    NetManager instance2 = Singleton<NetManager>.instance;
                                    bool flag17 = instance2.m_segments.m_buffer[(int)num19].m_startNode == nodeID == ((instance2.m_segments.m_buffer[(int)num19].m_flags & NetSegment.Flags.Invert) == NetSegment.Flags.None);
                                    bool flag18 = instance2.m_segments.m_buffer[(int)num20].m_startNode == nodeID == ((instance2.m_segments.m_buffer[(int)num20].m_flags & NetSegment.Flags.Invert) == NetSegment.Flags.None);
                                    if (flag17 == flag18)
                                    {
                                        goto IL_1F29;
                                    }
                                    if (flag17)
                                    {
                                        if ((node5.m_connectGroup & NetInfo.ConnectGroup.OnewayStart) == NetInfo.ConnectGroup.None)
                                        {
                                            goto IL_1F29;
                                        }
                                    }
                                    else
                                    {
                                        if ((node5.m_connectGroup & NetInfo.ConnectGroup.OnewayEnd) == NetInfo.ConnectGroup.None)
                                        {
                                            goto IL_1F29;
                                        }
                                        meshScale5.x = -meshScale5.x;
                                        meshScale5.y = -meshScale5.y;
                                    }
                                }
                                flag = true;
                                NetNode.PopulateGroupData(info, node5, leftMatrix5, rightMatrix5, meshScale5, objectIndex6, ref vertexIndex, ref triangleIndex, data, ref requireSurfaceMaps);
                            }
                        IL_1F29:;
                        }
                    }
                }
            }
            if (flag)
            {
                min = Vector3.Min(min, _this.m_bounds.min);
                max = Vector3.Max(max, _this.m_bounds.max);
                maxRenderDistance = Mathf.Max(maxRenderDistance, 30000f);
                maxInstanceDistance = Mathf.Max(maxInstanceDistance, 1000f);
            }
        }



        // NetNode
        private static void RefreshBendData(NetNode _this, ushort nodeID, NetInfo info, uint instanceIndex, ref RenderManager.Instance data)
        {
            data.m_position = _this.m_position;
            data.m_rotation = Quaternion.identity;
            data.m_initialized = true;
            float vScale = info.m_netAI.GetVScale();
            Vector3 zero = Vector3.zero;
            Vector3 zero2 = Vector3.zero;
            Vector3 zero3 = Vector3.zero;
            Vector3 zero4 = Vector3.zero;
            Vector3 zero5 = Vector3.zero;
            Vector3 zero6 = Vector3.zero;
            Vector3 zero7 = Vector3.zero;
            Vector3 zero8 = Vector3.zero;
            int num = 0;
            int num2 = 0;
            bool flag = false;
            int num3 = 0;
            for (int i = 0; i < 8; i++)
            {
                ushort segment = _this.GetSegment(i);
                if (segment != 0)
                {
                    NetSegment netSegment = Singleton<NetManager>.instance.m_segments.m_buffer[(int)segment];
                    bool flag2 = ++num3 == 1;
                    bool flag3 = netSegment.m_startNode == nodeID;
                    if ((!flag2 && !flag) || (flag2 && !flag3))
                    {
                        bool flag4;
                        netSegment.CalculateCorner(segment, true, flag3, false, out zero, out zero5, out flag4);
                        netSegment.CalculateCorner(segment, true, flag3, true, out zero2, out zero6, out flag4);
                        flag = true;
                        num = i;
                    }
                    else
                    {
                        bool flag4;
                        netSegment.CalculateCorner(segment, true, flag3, true, out zero3, out zero7, out flag4);
                        netSegment.CalculateCorner(segment, true, flag3, false, out zero4, out zero8, out flag4);
                        num2 = i;
                    }
                }
            }
            Vector3 vector;
            Vector3 vector2;
            NetSegment.CalculateMiddlePoints(zero, -zero5, zero3, -zero7, true, true, out vector, out vector2);
            Vector3 vector3;
            Vector3 vector4;
            NetSegment.CalculateMiddlePoints(zero2, -zero6, zero4, -zero8, true, true, out vector3, out vector4);
            data.m_dataMatrix0 = NetSegment.CalculateControlMatrix(zero, vector, vector2, zero3, zero2, vector3, vector4, zero4, _this.m_position, vScale);
            data.m_extraData.m_dataMatrix2 = NetSegment.CalculateControlMatrix(zero2, vector3, vector4, zero4, zero, vector, vector2, zero3, _this.m_position, vScale);
            data.m_dataVector0 = new Vector4(0.5f / info.m_halfWidth, 1f / info.m_segmentLength, 1f, 1f);
            Vector4 colorLocation = RenderManager.GetColorLocation(86016u + (uint)nodeID);
            data.m_dataVector3 = new Vector4(colorLocation.x, colorLocation.y, colorLocation.x, colorLocation.y);
            data.m_dataColor0 = info.m_color;
            data.m_dataColor0.a = 0f;
            data.m_dataFloat0 = Singleton<WeatherManager>.instance.GetWindSpeed(data.m_position);
            data.m_dataInt0 = (num | num2 << 4);
            if (info.m_requireSurfaceMaps)
            {
                Singleton<TerrainManager>.instance.GetSurfaceMapping(data.m_position, out data.m_dataTexture0, out data.m_dataTexture1, out data.m_dataVector1);
            }
        }
        // NetNode
        private static void RefreshEndData(NetNode _this, ushort nodeID, NetInfo info, uint instanceIndex, ref RenderManager.Instance data)
        {
            data.m_position = _this.m_position;
            data.m_rotation = Quaternion.identity;
            data.m_initialized = true;
            float vScale = info.m_netAI.GetVScale() / 1.5f;
            Vector3 zero = Vector3.zero;
            Vector3 zero2 = Vector3.zero;
            Vector3 vector = Vector3.zero;
            Vector3 vector2 = Vector3.zero;
            Vector3 zero3 = Vector3.zero;
            Vector3 zero4 = Vector3.zero;
            Vector3 a = Vector3.zero;
            Vector3 a2 = Vector3.zero;
            ushort num = 0;
            for (int i = 0; i < 8; i++)
            {
                ushort segment = _this.GetSegment(i);
                if (segment != 0)
                {
                    NetSegment netSegment = Singleton<NetManager>.instance.m_segments.m_buffer[(int)segment];
                    bool start = netSegment.m_startNode == nodeID;
                    bool flag;
                    netSegment.CalculateCorner(segment, true, start, false, out zero, out zero3, out flag);
                    netSegment.CalculateCorner(segment, true, start, true, out zero2, out zero4, out flag);
                    vector = zero2;
                    vector2 = zero;
                    a = zero4;
                    a2 = zero3;
                    num = segment;
                }
            }
            float d = info.m_netAI.GetEndRadius() * 1.33333337f;
            Vector3 vector3 = zero - zero3 * d;
            Vector3 vector4 = vector - a * d;
            Vector3 vector5 = zero2 - zero4 * d;
            Vector3 vector6 = vector2 - a2 * d;
            Vector3 vector7 = zero + zero3 * d;
            Vector3 vector8 = vector + a * d;
            Vector3 vector9 = zero2 + zero4 * d;
            Vector3 vector10 = vector2 + a2 * d;
            data.m_dataMatrix0 = NetSegment.CalculateControlMatrix(zero, vector3, vector4, vector, zero, vector3, vector4, vector, _this.m_position, vScale);
            data.m_extraData.m_dataMatrix2 = NetSegment.CalculateControlMatrix(zero2, vector9, vector10, vector2, zero2, vector9, vector10, vector2, _this.m_position, vScale);
            data.m_extraData.m_dataMatrix3 = NetSegment.CalculateControlMatrix(zero, vector7, vector8, vector, zero, vector7, vector8, vector, _this.m_position, vScale);
            data.m_dataMatrix1 = NetSegment.CalculateControlMatrix(zero2, vector5, vector6, vector2, zero2, vector5, vector6, vector2, _this.m_position, vScale);
            data.m_dataMatrix0.SetRow(3, data.m_dataMatrix0.GetRow(3) + new Vector4(0.2f, 0.2f, 0.2f, 0.2f));
            data.m_extraData.m_dataMatrix2.SetRow(3, data.m_extraData.m_dataMatrix2.GetRow(3) + new Vector4(0.2f, 0.2f, 0.2f, 0.2f));
            data.m_extraData.m_dataMatrix3.SetRow(3, data.m_extraData.m_dataMatrix3.GetRow(3) + new Vector4(0.2f, 0.2f, 0.2f, 0.2f));
            data.m_dataMatrix1.SetRow(3, data.m_dataMatrix1.GetRow(3) + new Vector4(0.2f, 0.2f, 0.2f, 0.2f));
            data.m_dataVector0 = new Vector4(0.5f / info.m_halfWidth, 1f / info.m_segmentLength, 0.5f - info.m_pavementWidth / info.m_halfWidth * 0.5f, info.m_pavementWidth / info.m_halfWidth * 0.5f);
            data.m_dataVector1 = new Vector4(0f, (float)_this.m_heightOffset * 0.015625f, 0f, 0f);
            data.m_dataVector1.w = (data.m_dataMatrix0.m33 + data.m_extraData.m_dataMatrix2.m33 + data.m_extraData.m_dataMatrix3.m33 + data.m_dataMatrix1.m33) * 0.25f;
            data.m_dataVector2 = new Vector4(info.m_pavementWidth / info.m_halfWidth * 0.5f, 1f, info.m_pavementWidth / info.m_halfWidth * 0.5f, 1f);
            Vector4 colorLocation = RenderManager.GetColorLocation((uint)(49152 + num));
            data.m_extraData.m_dataVector4 = new Vector4(colorLocation.x, colorLocation.y, colorLocation.x, colorLocation.y);
            data.m_dataColor0 = info.m_color;
            data.m_dataColor0.a = 0f;
            data.m_dataFloat0 = Singleton<WeatherManager>.instance.GetWindSpeed(data.m_position);
            if (info.m_requireSurfaceMaps)
            {
                Singleton<TerrainManager>.instance.GetSurfaceMapping(data.m_position, out data.m_dataTexture0, out data.m_dataTexture1, out data.m_dataVector3);
            }
        }
        // NetNode
        private static void RefreshJunctionData(NetNode _this, ushort nodeID, NetInfo info, uint instanceIndex)
        {
            NetManager instance = Singleton<NetManager>.instance;
            Vector3 vector = _this.m_position;
            for (int i = 0; i < 8; i++)
            {
                ushort segment = _this.GetSegment(i);
                if (segment != 0)
                {
                    NetInfo info2 = instance.m_segments.m_buffer[(int)segment].Info;
                    ItemClass connectionClass = info2.GetConnectionClass();
                    Vector3 a = (nodeID != instance.m_segments.m_buffer[(int)segment].m_startNode) ? instance.m_segments.m_buffer[(int)segment].m_endDirection : instance.m_segments.m_buffer[(int)segment].m_startDirection;
                    float num = -1f;
                    for (int j = 0; j < 8; j++)
                    {
                        ushort segment2 = _this.GetSegment(j);
                        if (segment2 != 0 && segment2 != segment)
                        {
                            NetInfo info3 = instance.m_segments.m_buffer[(int)segment2].Info;
                            ItemClass connectionClass2 = info3.GetConnectionClass();
                            if (connectionClass.m_service == connectionClass2.m_service)
                            {
                                Vector3 vector2 = (nodeID != instance.m_segments.m_buffer[(int)segment2].m_startNode) ? instance.m_segments.m_buffer[(int)segment2].m_endDirection : instance.m_segments.m_buffer[(int)segment2].m_startDirection;
                                float num2 = a.x * vector2.x + a.z * vector2.z;
                                num = Mathf.Max(num, num2);
                                bool flag = info2.m_requireDirectRenderers && (info2.m_nodeConnectGroups == NetInfo.ConnectGroup.None || (info2.m_nodeConnectGroups & info3.m_connectGroup) != NetInfo.ConnectGroup.None);
                                bool flag2 = info3.m_requireDirectRenderers && (info3.m_nodeConnectGroups == NetInfo.ConnectGroup.None || (info3.m_nodeConnectGroups & info2.m_connectGroup) != NetInfo.ConnectGroup.None);
                                if (j > i && (flag || flag2))
                                {
                                    float num3 = 0.01f - Mathf.Min(info2.m_maxTurnAngleCos, info3.m_maxTurnAngleCos);
                                    if (num2 < num3 && instanceIndex != 65535u)
                                    {
                                        float num4;
                                        if (flag)
                                        {
                                            num4 = info2.m_netAI.GetNodeInfoPriority(segment, ref instance.m_segments.m_buffer[(int)segment]);
                                        }
                                        else
                                        {
                                            num4 = -1E+08f;
                                        }
                                        float num5;
                                        if (flag2)
                                        {
                                            num5 = info3.m_netAI.GetNodeInfoPriority(segment2, ref instance.m_segments.m_buffer[(int)segment2]);
                                        }
                                        else
                                        {
                                            num5 = -1E+08f;
                                        }
                                        if (num4 >= num5)
                                        {
                                            RefreshJunctionData(_this, nodeID, i, j, info2, info3, segment, segment2, ref instanceIndex, ref Singleton<RenderManager>.instance.m_instances[(int)((UIntPtr)instanceIndex)]);
                                        }
                                        else
                                        {
                                            RefreshJunctionData(_this, nodeID, j, i, info3, info2, segment2, segment, ref instanceIndex, ref Singleton<RenderManager>.instance.m_instances[(int)((UIntPtr)instanceIndex)]);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    vector += a * (2f + num * 2f);
                }
            }
            vector.y = _this.m_position.y + (float)_this.m_heightOffset * 0.015625f;
            if (info.m_requireSegmentRenderers)
            {
                for (int k = 0; k < 8; k++)
                {
                    ushort segment3 = _this.GetSegment(k);
                    if (segment3 != 0 && instanceIndex != 65535u)
                    {
                        RefreshJunctionData(_this, nodeID, k, segment3, vector, ref instanceIndex, ref Singleton<RenderManager>.instance.m_instances[(int)((UIntPtr)instanceIndex)]);
                    }
                }
            }
        }
        // NetNode
        private static void RefreshJunctionData(NetNode _this, ushort nodeID, int segmentIndex, int segmentIndex2, NetInfo info, NetInfo info2, ushort nodeSegment, ushort nodeSegment2, ref uint instanceIndex, ref RenderManager.Instance data)
        {
            data.m_position = _this.m_position;
            data.m_rotation = Quaternion.identity;
            data.m_initialized = true;
            float vScale = info.m_netAI.GetVScale();
            Vector3 zero = Vector3.zero;
            Vector3 zero2 = Vector3.zero;
            Vector3 vector = Vector3.zero;
            Vector3 vector2 = Vector3.zero;
            Vector3 zero3 = Vector3.zero;
            Vector3 zero4 = Vector3.zero;
            Vector3 zero5 = Vector3.zero;
            Vector3 zero6 = Vector3.zero;
            bool start = Singleton<NetManager>.instance.m_segments.m_buffer[(int)nodeSegment].m_startNode == nodeID;
            bool flag;
            Singleton<NetManager>.instance.m_segments.m_buffer[(int)nodeSegment].CalculateCorner(nodeSegment, true, start, false, out zero, out zero3, out flag);
            Singleton<NetManager>.instance.m_segments.m_buffer[(int)nodeSegment].CalculateCorner(nodeSegment, true, start, true, out zero2, out zero4, out flag);
            start = (Singleton<NetManager>.instance.m_segments.m_buffer[(int)nodeSegment2].m_startNode == nodeID);
            Singleton<NetManager>.instance.m_segments.m_buffer[(int)nodeSegment2].CalculateCorner(nodeSegment2, true, start, true, out vector, out zero5, out flag);
            Singleton<NetManager>.instance.m_segments.m_buffer[(int)nodeSegment2].CalculateCorner(nodeSegment2, true, start, false, out vector2, out zero6, out flag);
            Vector3 b = (vector2 - vector) * (info.m_halfWidth / info2.m_halfWidth * 0.5f - 0.5f);
            vector -= b;
            vector2 += b;
            Vector3 vector3;
            Vector3 vector4;
            NetSegment.CalculateMiddlePoints(zero, -zero3, vector, -zero5, true, true, out vector3, out vector4);
            Vector3 vector5;
            Vector3 vector6;
            NetSegment.CalculateMiddlePoints(zero2, -zero4, vector2, -zero6, true, true, out vector5, out vector6);
            data.m_dataMatrix0 = NetSegment.CalculateControlMatrix(zero, vector3, vector4, vector, zero2, vector5, vector6, vector2, _this.m_position, vScale);
            data.m_extraData.m_dataMatrix2 = NetSegment.CalculateControlMatrix(zero2, vector5, vector6, vector2, zero, vector3, vector4, vector, _this.m_position, vScale);
            data.m_dataVector0 = new Vector4(0.5f / info.m_halfWidth, 1f / info.m_segmentLength, 1f, 1f);
            Vector4 colorLocation;
            Vector4 vector7;
            if (NetNode.BlendJunction(nodeID))
            {
                colorLocation = RenderManager.GetColorLocation(86016u + (uint)nodeID);
                vector7 = colorLocation;
            }
            else
            {
                colorLocation = RenderManager.GetColorLocation((uint)(49152 + nodeSegment));
                vector7 = RenderManager.GetColorLocation((uint)(49152 + nodeSegment2));
            }
            data.m_dataVector3 = new Vector4(colorLocation.x, colorLocation.y, vector7.x, vector7.y);
            data.m_dataInt0 = (8 | segmentIndex | segmentIndex2 << 4);
            data.m_dataColor0 = info.m_color;
            data.m_dataColor0.a = 0f;
            data.m_dataFloat0 = Singleton<WeatherManager>.instance.GetWindSpeed(data.m_position);
            if (info.m_requireSurfaceMaps)
            {
                Singleton<TerrainManager>.instance.GetSurfaceMapping(data.m_position, out data.m_dataTexture0, out data.m_dataTexture1, out data.m_dataVector1);
            }
            instanceIndex = (uint)data.m_nextInstance;
        }
        // NetNode
        private static void RefreshJunctionData(NetNode _this, ushort nodeID, int segmentIndex, ushort nodeSegment, Vector3 centerPos, ref uint instanceIndex, ref RenderManager.Instance data)
        {
            NetManager instance = Singleton<NetManager>.instance;
            data.m_position = _this.m_position;
            data.m_rotation = Quaternion.identity;
            data.m_initialized = true;
            Vector3 zero = Vector3.zero;
            Vector3 zero2 = Vector3.zero;
            Vector3 zero3 = Vector3.zero;
            Vector3 zero4 = Vector3.zero;
            Vector3 vector = Vector3.zero;
            Vector3 vector2 = Vector3.zero;
            Vector3 a = Vector3.zero;
            Vector3 a2 = Vector3.zero;
            Vector3 zero5 = Vector3.zero;
            Vector3 zero6 = Vector3.zero;
            Vector3 zero7 = Vector3.zero;
            Vector3 zero8 = Vector3.zero;
            NetSegment netSegment = instance.m_segments.m_buffer[(int)nodeSegment];
            NetInfo info = netSegment.Info;
            float vScale = info.m_netAI.GetVScale();
            ItemClass connectionClass = info.GetConnectionClass();
            Vector3 vector3 = (nodeID != netSegment.m_startNode) ? netSegment.m_endDirection : netSegment.m_startDirection;
            float num = -4f;
            float num2 = -4f;
            ushort num3 = 0;
            ushort num4 = 0;
            for (int i = 0; i < 8; i++)
            {
                ushort segment = _this.GetSegment(i);
                if (segment != 0 && segment != nodeSegment)
                {
                    NetInfo info2 = instance.m_segments.m_buffer[(int)segment].Info;
                    ItemClass connectionClass2 = info2.GetConnectionClass();
                    if (connectionClass.m_service == connectionClass2.m_service)
                    {
                        NetSegment netSegment2 = instance.m_segments.m_buffer[(int)segment];
                        Vector3 vector4 = (nodeID != netSegment2.m_startNode) ? netSegment2.m_endDirection : netSegment2.m_startDirection;
                        float num5 = vector3.x * vector4.x + vector3.z * vector4.z;
                        if (vector4.z * vector3.x - vector4.x * vector3.z < 0f)
                        {
                            if (num5 > num)
                            {
                                num = num5;
                                num3 = segment;
                            }
                            num5 = -2f - num5;
                            if (num5 > num2)
                            {
                                num2 = num5;
                                num4 = segment;
                            }
                        }
                        else
                        {
                            if (num5 > num2)
                            {
                                num2 = num5;
                                num4 = segment;
                            }
                            num5 = -2f - num5;
                            if (num5 > num)
                            {
                                num = num5;
                                num3 = segment;
                            }
                        }
                    }
                }
            }
            bool start = netSegment.m_startNode == nodeID;
            bool flag;
            netSegment.CalculateCorner(nodeSegment, true, start, false, out zero, out zero3, out flag);
            netSegment.CalculateCorner(nodeSegment, true, start, true, out zero2, out zero4, out flag);
            if (num3 != 0 && num4 != 0)
            {
                float num6 = info.m_pavementWidth / info.m_halfWidth * 0.5f;
                float y = 1f;
                if (num3 != 0)
                {
                    NetSegment netSegment3 = instance.m_segments.m_buffer[(int)num3];
                    NetInfo info3 = netSegment3.Info;
                    start = (netSegment3.m_startNode == nodeID);
                    netSegment3.CalculateCorner(num3, true, start, true, out vector, out a, out flag);
                    netSegment3.CalculateCorner(num3, true, start, false, out vector2, out a2, out flag);
                    float num7 = info3.m_pavementWidth / info3.m_halfWidth * 0.5f;
                    num6 = (num6 + num7) * 0.5f;
                    y = 2f * info.m_halfWidth / (info.m_halfWidth + info3.m_halfWidth);
                }
                float num8 = info.m_pavementWidth / info.m_halfWidth * 0.5f;
                float w = 1f;
                if (num4 != 0)
                {
                    NetSegment netSegment4 = instance.m_segments.m_buffer[(int)num4];
                    NetInfo info4 = netSegment4.Info;
                    start = (netSegment4.m_startNode == nodeID);
                    netSegment4.CalculateCorner(num4, true, start, true, out zero5, out zero7, out flag);
                    netSegment4.CalculateCorner(num4, true, start, false, out zero6, out zero8, out flag);
                    float num9 = info4.m_pavementWidth / info4.m_halfWidth * 0.5f;
                    num8 = (num8 + num9) * 0.5f;
                    w = 2f * info.m_halfWidth / (info.m_halfWidth + info4.m_halfWidth);
                }
                Vector3 vector5;
                Vector3 vector6;
                NetSegment.CalculateMiddlePoints(zero, -zero3, vector, -a, true, true, out vector5, out vector6);
                Vector3 vector7;
                Vector3 vector8;
                NetSegment.CalculateMiddlePoints(zero2, -zero4, vector2, -a2, true, true, out vector7, out vector8);
                Vector3 vector9;
                Vector3 vector10;
                NetSegment.CalculateMiddlePoints(zero, -zero3, zero5, -zero7, true, true, out vector9, out vector10);
                Vector3 vector11;
                Vector3 vector12;
                NetSegment.CalculateMiddlePoints(zero2, -zero4, zero6, -zero8, true, true, out vector11, out vector12);
                data.m_dataMatrix0 = NetSegment.CalculateControlMatrix(zero, vector5, vector6, vector, zero, vector5, vector6, vector, _this.m_position, vScale);
                data.m_extraData.m_dataMatrix2 = NetSegment.CalculateControlMatrix(zero2, vector7, vector8, vector2, zero2, vector7, vector8, vector2, _this.m_position, vScale);
                data.m_extraData.m_dataMatrix3 = NetSegment.CalculateControlMatrix(zero, vector9, vector10, zero5, zero, vector9, vector10, zero5, _this.m_position, vScale);
                data.m_dataMatrix1 = NetSegment.CalculateControlMatrix(zero2, vector11, vector12, zero6, zero2, vector11, vector12, zero6, _this.m_position, vScale);
                data.m_dataVector0 = new Vector4(0.5f / info.m_halfWidth, 1f / info.m_segmentLength, 0.5f - info.m_pavementWidth / info.m_halfWidth * 0.5f, info.m_pavementWidth / info.m_halfWidth * 0.5f);
                data.m_dataVector1 = centerPos - data.m_position;
                data.m_dataVector1.w = (data.m_dataMatrix0.m33 + data.m_extraData.m_dataMatrix2.m33 + data.m_extraData.m_dataMatrix3.m33 + data.m_dataMatrix1.m33) * 0.25f;
                data.m_dataVector2 = new Vector4(num6, y, num8, w);
            }
            else
            {
                centerPos.x = (zero.x + zero2.x) * 0.5f;
                centerPos.z = (zero.z + zero2.z) * 0.5f;
                vector = zero2;
                vector2 = zero;
                a = zero4;
                a2 = zero3;
                float d = Mathf.Min(info.m_halfWidth * 1.33333337f, 16f);
                Vector3 vector13 = zero - zero3 * d;
                Vector3 vector14 = vector - a * d;
                Vector3 vector15 = zero2 - zero4 * d;
                Vector3 vector16 = vector2 - a2 * d;
                Vector3 vector17 = zero + zero3 * d;
                Vector3 vector18 = vector + a * d;
                Vector3 vector19 = zero2 + zero4 * d;
                Vector3 vector20 = vector2 + a2 * d;
                data.m_dataMatrix0 = NetSegment.CalculateControlMatrix(zero, vector13, vector14, vector, zero, vector13, vector14, vector, _this.m_position, vScale);
                data.m_extraData.m_dataMatrix2 = NetSegment.CalculateControlMatrix(zero2, vector19, vector20, vector2, zero2, vector19, vector20, vector2, _this.m_position, vScale);
                data.m_extraData.m_dataMatrix3 = NetSegment.CalculateControlMatrix(zero, vector17, vector18, vector, zero, vector17, vector18, vector, _this.m_position, vScale);
                data.m_dataMatrix1 = NetSegment.CalculateControlMatrix(zero2, vector15, vector16, vector2, zero2, vector15, vector16, vector2, _this.m_position, vScale);
                data.m_dataMatrix0.SetRow(3, data.m_dataMatrix0.GetRow(3) + new Vector4(0.2f, 0.2f, 0.2f, 0.2f));
                data.m_extraData.m_dataMatrix2.SetRow(3, data.m_extraData.m_dataMatrix2.GetRow(3) + new Vector4(0.2f, 0.2f, 0.2f, 0.2f));
                data.m_extraData.m_dataMatrix3.SetRow(3, data.m_extraData.m_dataMatrix3.GetRow(3) + new Vector4(0.2f, 0.2f, 0.2f, 0.2f));
                data.m_dataMatrix1.SetRow(3, data.m_dataMatrix1.GetRow(3) + new Vector4(0.2f, 0.2f, 0.2f, 0.2f));
                data.m_dataVector0 = new Vector4(0.5f / info.m_halfWidth, 1f / info.m_segmentLength, 0.5f - info.m_pavementWidth / info.m_halfWidth * 0.5f, info.m_pavementWidth / info.m_halfWidth * 0.5f);
                data.m_dataVector1 = centerPos - data.m_position;
                data.m_dataVector1.w = (data.m_dataMatrix0.m33 + data.m_extraData.m_dataMatrix2.m33 + data.m_extraData.m_dataMatrix3.m33 + data.m_dataMatrix1.m33) * 0.25f;
                data.m_dataVector2 = new Vector4(info.m_pavementWidth / info.m_halfWidth * 0.5f, 1f, info.m_pavementWidth / info.m_halfWidth * 0.5f, 1f);
            }
            Vector4 colorLocation;
            Vector4 vector21;
            if (NetNode.BlendJunction(nodeID))
            {
                colorLocation = RenderManager.GetColorLocation(86016u + (uint)nodeID);
                vector21 = colorLocation;
            }
            else
            {
                colorLocation = RenderManager.GetColorLocation((uint)(49152 + nodeSegment));
                vector21 = RenderManager.GetColorLocation(86016u + (uint)nodeID);
            }
            data.m_extraData.m_dataVector4 = new Vector4(colorLocation.x, colorLocation.y, vector21.x, vector21.y);
            data.m_dataInt0 = segmentIndex;
            data.m_dataColor0 = info.m_color;
            data.m_dataColor0.a = 0f;
            data.m_dataFloat0 = Singleton<WeatherManager>.instance.GetWindSpeed(data.m_position);
            if (info.m_requireSurfaceMaps)
            {
                Singleton<TerrainManager>.instance.GetSurfaceMapping(data.m_position, out data.m_dataTexture0, out data.m_dataTexture1, out data.m_dataVector3);
            }
            instanceIndex = (uint)data.m_nextInstance;
        }

    }
}
