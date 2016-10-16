using System.Reflection;
using ColossalFramework;
using NetworkSkins.Detour;
using UnityEngine;
using System;
using NetworkSkins.Data;
using ColossalFramework.Math;

namespace NetworkSkins.Ground
{
    public class NetNodeDetour
    {
        private static bool deployed = false;

        private static RedirectCallsState _NetNode_TerrainUpdated_state;
        private static MethodInfo _NetNode_TerrainUpdated_original;
        private static MethodInfo _NetNode_TerrainUpdated_detour;

        public static void Deploy()
        {
            if (!deployed)
            {
                _NetNode_TerrainUpdated_original = typeof(NetNode).GetMethod("TerrainUpdated", BindingFlags.Instance | BindingFlags.Public);
                _NetNode_TerrainUpdated_detour = typeof(NetNodeDetour).GetMethod("TerrainUpdated", BindingFlags.Instance | BindingFlags.Public);
                _NetNode_TerrainUpdated_state = RedirectionHelper.RedirectCalls(_NetNode_TerrainUpdated_original, _NetNode_TerrainUpdated_detour);

                deployed = true;
            }
        }

        public static void Revert()
        {
            if (deployed)
            {
                RedirectionHelper.RevertRedirect(_NetNode_TerrainUpdated_original, _NetNode_TerrainUpdated_state);
                _NetNode_TerrainUpdated_original = null;
                _NetNode_TerrainUpdated_detour = null;

                deployed = false;
            }
        }

        // NetNode
        public void TerrainUpdated(ushort nodeID, float minX, float minZ, float maxX, float maxZ)
        {
            NetNode _this = NetManager.instance.m_nodes.m_buffer[nodeID];

            if ((_this.m_flags & (NetNode.Flags.Created | NetNode.Flags.Deleted)) != NetNode.Flags.Created)
            {
                return;
            }
            NetInfo info = _this.Info;
            if (info == null)
            {
                return;
            }
            byte b = (!Singleton<TerrainManager>.instance.HasDetailMapping(_this.m_position) && info.m_requireSurfaceMaps) ? (byte)64 : (byte)0;
            if (b != _this.m_heightOffset)
            {
                CheckHeightOffset(ref _this, nodeID);
                NetManager instance = Singleton<NetManager>.instance;
                for (int i = 0; i < 8; i++)
                {
                    ushort segment = _this.GetSegment(i);
                    if (segment != 0)
                    {
                        ushort startNode = instance.m_segments.m_buffer[(int)segment].m_startNode;
                        ushort endNode = instance.m_segments.m_buffer[(int)segment].m_endNode;
                        if (startNode == nodeID)
                        {
                            CheckHeightOffset(ref instance.m_nodes.m_buffer[(int)endNode], endNode);
                        }
                        else
                        {
                            CheckHeightOffset(ref instance.m_nodes.m_buffer[(int)startNode], startNode);
                        }
                    }
                }
            }
            bool flag;
            bool flag2;
            bool flag3;
            bool flag4;
            bool flag5;
            bool flag6;
            if ((_this.m_flags & NetNode.Flags.Underground) != NetNode.Flags.None)
            {
                flag = false;
                flag2 = false;
                flag3 = false;
                flag4 = false;
                flag5 = false;
                flag6 = info.m_netAI.RaiseTerrain();
            }
            else
            {
                flag = (info.m_createPavement && (!info.m_lowerTerrain || (_this.m_flags & NetNode.Flags.OnGround) != NetNode.Flags.None));
                flag2 = (info.m_createGravel && (!info.m_lowerTerrain || (_this.m_flags & NetNode.Flags.OnGround) != NetNode.Flags.None));
                flag3 = (info.m_createRuining && (!info.m_lowerTerrain || (_this.m_flags & NetNode.Flags.OnGround) != NetNode.Flags.None));
                flag4 = (info.m_clipTerrain && (!info.m_lowerTerrain || (_this.m_flags & NetNode.Flags.OnGround) != NetNode.Flags.None) && info.m_netAI.CanClipNodes());
                flag5 = (info.m_flattenTerrain || (info.m_netAI.FlattenGroundNodes() && (_this.m_flags & NetNode.Flags.OnGround) != NetNode.Flags.None));
                flag6 = false;
            }
            if (flag5 || info.m_lowerTerrain || flag6 || flag || flag2 || flag3 || flag4)
            {
                if ((_this.m_flags & NetNode.Flags.Junction) != NetNode.Flags.None)
                {
                    Vector3 vector = _this.m_position;
                    int num = 0;
                    for (int j = 0; j < 8; j++)
                    {
                        ushort segment2 = _this.GetSegment(j);
                        if (segment2 != 0)
                        {
                            NetSegment netSegment = Singleton<NetManager>.instance.m_segments.m_buffer[(int)segment2];
                            NetInfo info2 = netSegment.Info;
                            if (info2 != null)
                            {
                                ItemClass connectionClass = info2.GetConnectionClass();
                                Vector3 a = (nodeID != netSegment.m_startNode) ? netSegment.m_endDirection : netSegment.m_startDirection;
                                float num2 = -1f;
                                for (int k = 0; k < 8; k++)
                                {
                                    ushort segment3 = _this.GetSegment(k);
                                    if (segment3 != 0 && segment3 != segment2)
                                    {
                                        NetSegment netSegment2 = Singleton<NetManager>.instance.m_segments.m_buffer[(int)segment3];
                                        NetInfo info3 = netSegment2.Info;
                                        if (info3 != null)
                                        {
                                            ItemClass connectionClass2 = info3.GetConnectionClass();
                                            if (connectionClass.m_service == connectionClass2.m_service)
                                            {
                                                Vector3 vector2 = (nodeID != netSegment2.m_startNode) ? netSegment2.m_endDirection : netSegment2.m_startDirection;
                                                num2 = Mathf.Max(num2, a.x * vector2.x + a.z * vector2.z);
                                            }
                                        }
                                    }
                                }
                                vector += a * (2f + num2 * 2f);
                                num++;
                            }
                        }
                    }
                    vector.y = _this.m_position.y;
                    if (num > 1)
                    {
                        for (int l = 0; l < 8; l++)
                        {
                            ushort segment4 = _this.GetSegment(l);
                            if (segment4 != 0)
                            {
                                NetSegment netSegment3 = Singleton<NetManager>.instance.m_segments.m_buffer[(int)segment4];
                                NetInfo info4 = netSegment3.Info;
                                if (info4 != null)
                                {
                                    Bezier3 bezier = default(Bezier3);
                                    Segment3 segment5 = default(Segment3);
                                    Vector3 zero = Vector3.zero;
                                    Vector3 zero2 = Vector3.zero;
                                    Vector3 a2 = Vector3.zero;
                                    Vector3 a3 = Vector3.zero;
                                    ItemClass connectionClass3 = info4.GetConnectionClass();
                                    Vector3 vector3 = (nodeID != netSegment3.m_startNode) ? netSegment3.m_endDirection : netSegment3.m_startDirection;
                                    float num3 = -4f;
                                    ushort num4 = 0;
                                    for (int m = 0; m < 8; m++)
                                    {
                                        ushort segment6 = _this.GetSegment(m);
                                        if (segment6 != 0 && segment6 != segment4)
                                        {
                                            NetSegment netSegment4 = Singleton<NetManager>.instance.m_segments.m_buffer[(int)segment6];
                                            NetInfo info5 = netSegment4.Info;
                                            if (info5 != null)
                                            {
                                                ItemClass connectionClass4 = info5.GetConnectionClass();
                                                if (connectionClass3.m_service == connectionClass4.m_service)
                                                {
                                                    Vector3 vector4 = (nodeID != netSegment4.m_startNode) ? netSegment4.m_endDirection : netSegment4.m_startDirection;
                                                    float num5 = vector3.x * vector4.x + vector3.z * vector4.z;
                                                    if (vector4.z * vector3.x - vector4.x * vector3.z < 0f)
                                                    {
                                                        if (num5 > num3)
                                                        {
                                                            num3 = num5;
                                                            num4 = segment6;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        num5 = -2f - num5;
                                                        if (num5 > num3)
                                                        {
                                                            num3 = num5;
                                                            num4 = segment6;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    bool start = netSegment3.m_startNode == nodeID;
                                    bool flag7;
                                    netSegment3.CalculateCorner(segment4, false, start, false, out bezier.a, out zero, out flag7);
                                    netSegment3.CalculateCorner(segment4, false, start, true, out segment5.a, out zero2, out flag7);
                                    if (num4 != 0)
                                    {
                                        NetSegment netSegment5 = Singleton<NetManager>.instance.m_segments.m_buffer[(int)num4];
                                        NetInfo info6 = netSegment5.Info;
                                        start = (netSegment5.m_startNode == nodeID);
                                        netSegment5.CalculateCorner(num4, false, start, true, out bezier.d, out a2, out flag7);
                                        netSegment5.CalculateCorner(num4, false, start, false, out segment5.b, out a3, out flag7);
                                        NetSegment.CalculateMiddlePoints(bezier.a, -zero, bezier.d, -a2, true, true, out bezier.b, out bezier.c);
                                        segment5.a = (bezier.a + segment5.a) * 0.5f;
                                        segment5.b = (bezier.d + segment5.b) * 0.5f;
                                        Vector3 vector5 = Vector3.Min(vector, Vector3.Min(bezier.Min(), segment5.Min()));
                                        Vector3 vector6 = Vector3.Max(vector, Vector3.Max(bezier.Max(), segment5.Max()));
                                        if (vector5.x <= maxX && vector5.z <= maxZ && minX <= vector6.x && minZ <= vector6.z)
                                        {
                                            float num6 = Vector3.Distance(bezier.a, bezier.b);
                                            float num7 = Vector3.Distance(bezier.b, bezier.c);
                                            float num8 = Vector3.Distance(bezier.c, bezier.d);
                                            Vector3 lhs = (bezier.a - bezier.b) * (1f / Mathf.Max(0.1f, num6));
                                            Vector3 vector7 = (bezier.c - bezier.b) * (1f / Mathf.Max(0.1f, num7));
                                            Vector3 rhs = (bezier.d - bezier.c) * (1f / Mathf.Max(0.1f, num8));
                                            float num9 = Mathf.Min(Vector3.Dot(lhs, vector7), Vector3.Dot(vector7, rhs));
                                            num6 += num7 + num8;
                                            int num10 = Mathf.Clamp(Mathf.CeilToInt(Mathf.Min(num6 * 0.125f, 50f - num9 * 50f)) * 2, 2, 16);
                                            Vector3 vector8 = bezier.a;
                                            Vector3 vector9 = segment5.a;
                                            for (int n = 1; n <= num10; n++)
                                            {
                                                NetInfo netInfo = (n > num10 >> 1) ? info6 : info4;
                                                Vector3 vector10 = bezier.Position((float)n / (float)num10);
                                                Vector3 vector11;
                                                if (n <= num10 >> 1)
                                                {
                                                    vector11 = segment5.a + (vector - segment5.a) * ((float)n / (float)num10 * 2f);
                                                }
                                                else
                                                {
                                                    vector11 = vector + (segment5.b - vector) * ((float)n / (float)num10 * 2f - 1f);
                                                }
                                                bool flag8 = netInfo.m_createPavement && (!netInfo.m_lowerTerrain || (_this.m_flags & NetNode.Flags.OnGround) != NetNode.Flags.None);
                                                bool flag9 = netInfo.m_createGravel && (!netInfo.m_lowerTerrain || (_this.m_flags & NetNode.Flags.OnGround) != NetNode.Flags.None);
                                                bool flag10 = netInfo.m_createRuining && (!netInfo.m_lowerTerrain || (_this.m_flags & NetNode.Flags.OnGround) != NetNode.Flags.None);
                                                bool flag11 = netInfo.m_clipTerrain && (!netInfo.m_lowerTerrain || (_this.m_flags & NetNode.Flags.OnGround) != NetNode.Flags.None) && netInfo.m_netAI.CanClipNodes();
                                                bool flag12 = netInfo.m_flattenTerrain || (netInfo.m_netAI.FlattenGroundNodes() && (_this.m_flags & NetNode.Flags.OnGround) != NetNode.Flags.None);
                                                Vector3 vector12 = vector8;
                                                Vector3 vector13 = vector10;
                                                Vector3 vector14 = vector11;
                                                Vector3 vector15 = vector9;
                                                TerrainModify.Heights heights = TerrainModify.Heights.None;
                                                TerrainModify.Surface surface = TerrainModify.Surface.None;
                                                if (flag6)
                                                {
                                                    heights = TerrainModify.Heights.SecondaryMin;
                                                }
                                                else
                                                {
                                                    if (flag5 || flag12)
                                                    {
                                                        heights |= TerrainModify.Heights.PrimaryLevel;
                                                    }
                                                    if (info.m_lowerTerrain || netInfo.m_lowerTerrain)
                                                    {
                                                        heights |= TerrainModify.Heights.PrimaryMax;
                                                    }
                                                    if (info.m_blockWater || netInfo.m_blockWater)
                                                    {
                                                        heights |= TerrainModify.Heights.BlockHeight;
                                                    }
                                                    if (flag8)
                                                    {
                                                        surface |= TerrainModify.Surface.PavementA;
                                                    }
                                                    if (flag2 || flag9)
                                                    {
                                                        surface |= TerrainModify.Surface.Gravel;
                                                    }
                                                    if (flag3 || flag10)
                                                    {
                                                        surface |= TerrainModify.Surface.Ruined;
                                                    }
                                                    if (flag4 || flag11)
                                                    {
                                                        surface |= TerrainModify.Surface.Clip;
                                                    }
                                                }
                                                TerrainModify.Edges edges = TerrainModify.Edges.All;
                                                float num11 = 0f;
                                                float num12 = 1f;
                                                float num13 = 0f;
                                                float num14 = 0f;
                                                int num15 = 0;
                                                while (netInfo.m_netAI.NodeModifyMask(nodeID, ref _this, segment4, num4, num15, ref surface, ref heights, ref edges, ref num11, ref num12, ref num13, ref num14))
                                                {
                                                    if (num11 < 0.5f)
                                                    {
                                                        TerrainModify.Edges edges2 = TerrainModify.Edges.AB;
                                                        if (num11 != 0f || num12 != 1f || num15 != 0)
                                                        {
                                                            if (num11 != 0f)
                                                            {
                                                                float t = 2f * num11 * netInfo.m_halfWidth / Vector3.Distance(vector12, vector15);
                                                                float t2 = 2f * num11 * netInfo.m_halfWidth / Vector3.Distance(vector13, vector14);
                                                                vector8 = Vector3.Lerp(vector12, vector15, t);
                                                                vector10 = Vector3.Lerp(vector13, vector14, t2);
                                                            }
                                                            else
                                                            {
                                                                vector8 = vector12;
                                                                vector10 = vector13;
                                                            }
                                                            if (num12 < 0.5f)
                                                            {
                                                                edges2 |= TerrainModify.Edges.CD;
                                                                float t3 = 2f * num12 * netInfo.m_halfWidth / Vector3.Distance(vector12, vector15);
                                                                float t4 = 2f * num12 * netInfo.m_halfWidth / Vector3.Distance(vector13, vector14);
                                                                vector9 = Vector3.Lerp(vector12, vector15, t3);
                                                                vector11 = Vector3.Lerp(vector13, vector14, t4);
                                                            }
                                                            else
                                                            {
                                                                vector9 = vector15;
                                                                vector11 = vector14;
                                                            }
                                                        }
                                                        vector8.y += num13;
                                                        vector9.y += num14;
                                                        vector10.y += num13;
                                                        vector11.y += num14;
                                                        Vector3 zero3 = Vector3.zero;
                                                        Vector3 zero4 = Vector3.zero;
                                                        if (flag6)
                                                        {
                                                            zero3.y += info.m_maxHeight;
                                                            zero4.y += info.m_maxHeight;
                                                        }
                                                        else if (netInfo.m_lowerTerrain)
                                                        {
                                                            if (!info.m_lowerTerrain)
                                                            {
                                                                if (n == 1)
                                                                {
                                                                    TerrainModify.Edges edges3 = edges2 | TerrainModify.Edges.DA;
                                                                    TerrainModify.ApplyQuad(vector8, vector10, vector11, vector9, edges3, TerrainModify.Heights.None, surface);
                                                                    surface = TerrainModify.Surface.None;
                                                                }
                                                                else if (n == num10)
                                                                {
                                                                    TerrainModify.Edges edges4 = edges2 | TerrainModify.Edges.BC;
                                                                    TerrainModify.ApplyQuad(vector8, vector10, vector11, vector9, edges4, TerrainModify.Heights.None, surface);
                                                                    surface = TerrainModify.Surface.None;
                                                                }
                                                                zero3.y += (float)Mathf.Abs(n - 1 - (num10 >> 1)) * (1f / (float)num10) * netInfo.m_netAI.GetTerrainLowerOffset();
                                                                zero4.y += (float)Mathf.Abs(n - (num10 >> 1)) * (1f / (float)num10) * netInfo.m_netAI.GetTerrainLowerOffset();
                                                            }
                                                            else
                                                            {
                                                                if ((_this.m_flags & NetNode.Flags.OnGround) != NetNode.Flags.None)
                                                                {
                                                                    if (n == 1)
                                                                    {
                                                                        edges2 |= TerrainModify.Edges.DA;
                                                                    }
                                                                    else if (n == num10)
                                                                    {
                                                                        edges2 |= TerrainModify.Edges.BC;
                                                                    }
                                                                }
                                                                zero3.y += netInfo.m_netAI.GetTerrainLowerOffset();
                                                                zero4.y += netInfo.m_netAI.GetTerrainLowerOffset();
                                                            }
                                                        }
                                                        edges2 &= edges;
                                                        TerrainModify.Surface surface2 = surface;
                                                        if ((surface2 & TerrainModify.Surface.PavementA) != TerrainModify.Surface.None)
                                                        {
                                                            surface2 |= TerrainModify.Surface.Gravel;
                                                        }
                                                        TerrainModify.ApplyQuad(vector8 + zero3, vector10 + zero4, vector11 + zero4, vector9 + zero3, edges2, heights, surface2);
                                                    }
                                                    num15++;
                                                }
                                                vector8 = vector13;
                                                vector9 = vector14;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Vector3 vector16 = bezier.a;
                                        Vector3 vector17 = segment5.a;
                                        Vector3 vector18 = Vector3.zero;
                                        Vector3 vector19 = Vector3.zero;
                                        Vector3 from = vector16;
                                        Vector3 to = vector17;
                                        Vector3 from2 = vector18;
                                        Vector3 to2 = vector19;
                                        bool flag13 = info4.m_createPavement && (!info4.m_lowerTerrain || (_this.m_flags & NetNode.Flags.OnGround) != NetNode.Flags.None);
                                        bool flag14 = info4.m_createGravel && (!info4.m_lowerTerrain || (_this.m_flags & NetNode.Flags.OnGround) != NetNode.Flags.None);
                                        bool flag15 = info4.m_createRuining && (!info4.m_lowerTerrain || (_this.m_flags & NetNode.Flags.OnGround) != NetNode.Flags.None);
                                        bool flag16 = info4.m_clipTerrain && (!info4.m_lowerTerrain || (_this.m_flags & NetNode.Flags.OnGround) != NetNode.Flags.None) && info4.m_netAI.CanClipNodes();
                                        bool flag17 = info4.m_flattenTerrain || (info4.m_netAI.FlattenGroundNodes() && (_this.m_flags & NetNode.Flags.OnGround) != NetNode.Flags.None);
                                        TerrainModify.Heights heights2 = TerrainModify.Heights.None;
                                        TerrainModify.Surface surface3 = TerrainModify.Surface.None;
                                        if (flag6)
                                        {
                                            heights2 = TerrainModify.Heights.SecondaryMin;
                                        }
                                        else
                                        {
                                            if (flag17)
                                            {
                                                heights2 |= TerrainModify.Heights.PrimaryLevel;
                                            }
                                            if (info4.m_lowerTerrain)
                                            {
                                                heights2 |= TerrainModify.Heights.PrimaryMax;
                                            }
                                            if (info4.m_blockWater)
                                            {
                                                heights2 |= TerrainModify.Heights.BlockHeight;
                                            }
                                            if (flag13)
                                            {
                                                surface3 |= TerrainModify.Surface.PavementA;
                                            }
                                            if (flag14)
                                            {
                                                surface3 |= TerrainModify.Surface.Gravel;
                                            }
                                            if (flag15)
                                            {
                                                surface3 |= TerrainModify.Surface.Ruined;
                                            }
                                            if (flag16)
                                            {
                                                surface3 |= TerrainModify.Surface.Clip;
                                            }
                                        }
                                        TerrainModify.Edges edges5 = TerrainModify.Edges.All;
                                        float num16 = 0f;
                                        float num17 = 1f;
                                        float num18 = 0f;
                                        float num19 = 0f;
                                        int num20 = 0;
                                        while (info4.m_netAI.NodeModifyMask(nodeID, ref _this, segment4, segment4, num20, ref surface3, ref heights2, ref edges5, ref num16, ref num17, ref num18, ref num19))
                                        {
                                            if (num16 != 0f || num17 != 1f || num20 != 0)
                                            {
                                                vector16 = Vector3.Lerp(from, to, num16);
                                                vector17 = Vector3.Lerp(from, to, num17);
                                                vector18 = Vector3.Lerp(from2, to2, num16);
                                                vector19 = Vector3.Lerp(from2, to2, num17);
                                            }
                                            vector16.y += num18;
                                            vector17.y += num19;
                                            vector18.y += num18;
                                            vector19.y += num19;
                                            if (info4.m_halfWidth < 3.999f)
                                            {
                                                vector18 = vector16 - zero * (info4.m_halfWidth + 2f);
                                                vector19 = vector17 - zero2 * (info4.m_halfWidth + 2f);
                                                float num21 = Mathf.Min(new float[]
                                                {
                                            Mathf.Min(Mathf.Min(vector16.x, vector17.x), Mathf.Min(vector18.x, vector19.x))
                                                });
                                                float num22 = Mathf.Max(new float[]
                                                {
                                            Mathf.Max(Mathf.Max(vector16.x, vector17.x), Mathf.Max(vector18.x, vector19.x))
                                                });
                                                float num23 = Mathf.Min(new float[]
                                                {
                                            Mathf.Min(Mathf.Min(vector16.z, vector17.z), Mathf.Min(vector18.z, vector19.z))
                                                });
                                                float num24 = Mathf.Max(new float[]
                                                {
                                            Mathf.Max(Mathf.Max(vector16.z, vector17.z), Mathf.Max(vector18.z, vector19.z))
                                                });
                                                if (num21 <= maxX && num23 <= maxZ && minX <= num22 && minZ <= num24)
                                                {
                                                    TerrainModify.Edges edges6 = TerrainModify.Edges.AB | TerrainModify.Edges.BC | TerrainModify.Edges.CD;
                                                    if (info4.m_lowerTerrain && (_this.m_flags & NetNode.Flags.OnGround) != NetNode.Flags.None)
                                                    {
                                                        edges6 |= TerrainModify.Edges.DA;
                                                    }
                                                    edges6 &= edges5;
                                                    TerrainModify.Surface surface4 = surface3;
                                                    if ((surface4 & TerrainModify.Surface.PavementA) != TerrainModify.Surface.None)
                                                    {
                                                        surface4 |= TerrainModify.Surface.Gravel;
                                                    }
                                                    Vector3 zero5 = Vector3.zero;
                                                    if (flag6)
                                                    {
                                                        zero5.y += info4.m_maxHeight;
                                                    }
                                                    else if (info4.m_lowerTerrain)
                                                    {
                                                        zero5.y += info4.m_netAI.GetTerrainLowerOffset();
                                                    }
                                                    TerrainModify.ApplyQuad(vector16 + zero5, vector18 + zero5, vector19 + zero5, vector17 + zero5, edges6, heights2, surface4);
                                                }
                                            }
                                            else
                                            {
                                                vector18 = vector17;
                                                vector19 = vector16;
                                                a2 = zero2;
                                                a3 = zero;
                                                float d = info4.m_netAI.GetEndRadius() * 1.33333337f * 1.1f;
                                                Vector3 b2 = vector16 - zero * d;
                                                Vector3 c = vector18 - a2 * d;
                                                Vector3 vector20 = vector17 + zero2 * d;
                                                Vector3 vector21 = vector19 + a3 * d;
                                                float num25 = Mathf.Min(Mathf.Min(Mathf.Min(vector16.x, vector17.x), Mathf.Min(b2.x, vector20.x)), Mathf.Min(Mathf.Min(c.x, vector21.x), Mathf.Min(vector18.x, vector19.x)));
                                                float num26 = Mathf.Max(Mathf.Max(Mathf.Max(vector16.x, vector17.x), Mathf.Max(b2.x, vector20.x)), Mathf.Max(Mathf.Max(c.x, vector21.x), Mathf.Max(vector18.x, vector19.x)));
                                                float num27 = Mathf.Min(Mathf.Min(Mathf.Min(vector16.z, vector17.z), Mathf.Min(b2.z, vector20.z)), Mathf.Min(Mathf.Min(c.z, vector21.z), Mathf.Min(vector18.z, vector19.z)));
                                                float num28 = Mathf.Max(Mathf.Max(Mathf.Max(vector16.z, vector17.z), Mathf.Max(b2.z, vector20.z)), Mathf.Max(Mathf.Max(c.z, vector21.z), Mathf.Max(vector18.z, vector19.z)));
                                                if (num25 <= maxX && num27 <= maxZ && minX <= num26 && minZ <= num28)
                                                {
                                                    int num29 = Mathf.Clamp(Mathf.CeilToInt(info4.m_halfWidth * 0.4f), 2, 8);
                                                    Vector3 a4 = vector16;
                                                    Vector3 a5 = (vector16 + vector17) * 0.5f;
                                                    for (int num30 = 1; num30 <= num29; num30++)
                                                    {
                                                        Vector3 a6 = Bezier3.Position(vector16, b2, c, vector18, ((float)num30 - 0.5f) / (float)num29);
                                                        Vector3 vector22 = Bezier3.Position(vector16, b2, c, vector18, (float)num30 / (float)num29);
                                                        TerrainModify.Edges edges7 = TerrainModify.Edges.AB | TerrainModify.Edges.BC;
                                                        edges7 &= edges5;
                                                        TerrainModify.Surface surface5 = surface3;
                                                        if ((surface5 & TerrainModify.Surface.PavementA) != TerrainModify.Surface.None)
                                                        {
                                                            surface5 |= TerrainModify.Surface.Gravel;
                                                        }
                                                        Vector3 zero6 = Vector3.zero;
                                                        if (flag6)
                                                        {
                                                            zero6.y += info4.m_maxHeight;
                                                        }
                                                        else if (info4.m_lowerTerrain)
                                                        {
                                                            zero6.y += info4.m_netAI.GetTerrainLowerOffset();
                                                        }
                                                        TerrainModify.ApplyQuad(a4 + zero6, a6 + zero6, vector22 + zero6, a5 + zero6, edges7, heights2, surface5);
                                                        a4 = vector22;
                                                    }
                                                }
                                            }
                                            num20++;
                                        }
                                    }
                                }
                            }
                        }
                        if (num == 8)
                        {
                            Vector3 vector23 = vector + Vector3.left * 8f;
                            Vector3 vector24 = vector + Vector3.back * 8f;
                            Vector3 vector25 = vector + Vector3.right * 8f;
                            Vector3 vector26 = vector + Vector3.forward * 8f;
                            Vector3 vector27 = vector23;
                            Vector3 vector28 = vector24;
                            Vector3 vector29 = vector25;
                            Vector3 vector30 = vector26;
                            TerrainModify.Heights heights3 = TerrainModify.Heights.None;
                            TerrainModify.Surface surface6 = TerrainModify.Surface.None;
                            if (flag6)
                            {
                                heights3 = TerrainModify.Heights.SecondaryMin;
                            }
                            else
                            {
                                if (flag5)
                                {
                                    heights3 |= TerrainModify.Heights.PrimaryLevel;
                                }
                                if (info.m_lowerTerrain)
                                {
                                    heights3 |= TerrainModify.Heights.PrimaryMax;
                                }
                                if (info.m_blockWater)
                                {
                                    heights3 |= TerrainModify.Heights.BlockHeight;
                                }
                                if (flag)
                                {
                                    surface6 |= TerrainModify.Surface.PavementA;
                                }
                                if (flag2)
                                {
                                    surface6 |= TerrainModify.Surface.Gravel;
                                }
                                if (flag3)
                                {
                                    surface6 |= TerrainModify.Surface.Ruined;
                                }
                                if (flag4)
                                {
                                    surface6 |= TerrainModify.Surface.Clip;
                                }
                            }
                            TerrainModify.Edges edges8 = TerrainModify.Edges.All;
                            float num31 = 0f;
                            float num32 = 1f;
                            float num33 = 0f;
                            float num34 = 0f;
                            int num35 = 0;
                            while (info.m_netAI.NodeModifyMask(nodeID, ref _this, 0, 0, num35, ref surface6, ref heights3, ref edges8, ref num31, ref num32, ref num33, ref num34))
                            {
                                if (num35 != 0)
                                {
                                    vector23 = vector27;
                                    vector24 = vector28;
                                    vector25 = vector29;
                                    vector26 = vector30;
                                }
                                vector23.y += (num33 + num34) * 0.5f;
                                vector24.y += (num33 + num34) * 0.5f;
                                vector25.y += (num33 + num34) * 0.5f;
                                vector26.y += (num33 + num34) * 0.5f;
                                TerrainModify.Edges edges9 = TerrainModify.Edges.All;
                                edges9 &= edges8;
                                TerrainModify.Surface surface7 = surface6;
                                if ((surface7 & TerrainModify.Surface.PavementA) != TerrainModify.Surface.None)
                                {
                                    surface7 |= TerrainModify.Surface.Gravel;
                                }
                                Vector3 zero7 = Vector3.zero;
                                if (flag6)
                                {
                                    zero7.y += info.m_maxHeight;
                                }
                                else if (info.m_lowerTerrain)
                                {
                                    zero7.y += info.m_netAI.GetTerrainLowerOffset();
                                }
                                TerrainModify.ApplyQuad(vector23 + zero7, vector24 + zero7, vector25 + zero7, vector26 + zero7, edges9, heights3, surface7);
                                num35++;
                            }
                        }
                    }
                }
                else if ((_this.m_flags & NetNode.Flags.Bend) != NetNode.Flags.None)
                {
                    Bezier3 bezier2 = default(Bezier3);
                    Bezier3 bezier3 = default(Bezier3);
                    Vector3 zero8 = Vector3.zero;
                    Vector3 zero9 = Vector3.zero;
                    Vector3 zero10 = Vector3.zero;
                    Vector3 zero11 = Vector3.zero;
                    ushort segment7 = 0;
                    ushort segment8 = 0;
                    int num36 = 0;
                    for (int num37 = 0; num37 < 8; num37++)
                    {
                        ushort segment9 = _this.GetSegment(num37);
                        if (segment9 != 0)
                        {
                            NetSegment netSegment6 = Singleton<NetManager>.instance.m_segments.m_buffer[(int)segment9];
                            bool start2 = netSegment6.m_startNode == nodeID;
                            if (++num36 == 1)
                            {
                                segment7 = segment9;
                                bool flag18;
                                netSegment6.CalculateCorner(segment9, false, start2, false, out bezier2.a, out zero8, out flag18);
                                netSegment6.CalculateCorner(segment9, false, start2, true, out bezier3.a, out zero9, out flag18);
                            }
                            else
                            {
                                segment8 = segment9;
                                bool flag18;
                                netSegment6.CalculateCorner(segment9, false, start2, true, out bezier2.d, out zero10, out flag18);
                                netSegment6.CalculateCorner(segment9, false, start2, false, out bezier3.d, out zero11, out flag18);
                            }
                        }
                    }
                    Vector3 a7 = bezier2.a;
                    Vector3 a8 = bezier3.a;
                    Vector3 d2 = bezier2.d;
                    Vector3 d3 = bezier3.d;
                    TerrainModify.Heights heights4 = TerrainModify.Heights.None;
                    TerrainModify.Surface surface8 = TerrainModify.Surface.None;
                    if (flag6)
                    {
                        heights4 = TerrainModify.Heights.SecondaryMin;
                    }
                    else
                    {
                        if (flag5)
                        {
                            heights4 |= TerrainModify.Heights.PrimaryLevel;
                        }
                        if (info.m_lowerTerrain)
                        {
                            heights4 |= TerrainModify.Heights.PrimaryMax;
                        }
                        if (info.m_blockWater)
                        {
                            heights4 |= TerrainModify.Heights.BlockHeight;
                        }
                        if (flag)
                        {
                            surface8 |= TerrainModify.Surface.PavementA;
                        }
                        if (flag2)
                        {
                            surface8 |= TerrainModify.Surface.Gravel;
                        }
                        if (flag3)
                        {
                            surface8 |= TerrainModify.Surface.Ruined;
                        }
                        if (flag4)
                        {
                            surface8 |= TerrainModify.Surface.Clip;
                        }
                    }
                    TerrainModify.Edges edges10 = TerrainModify.Edges.All;
                    float num38 = 0f;
                    float num39 = 1f;
                    float num40 = 0f;
                    float num41 = 0f;
                    int num42 = 0;
                    while (info.m_netAI.NodeModifyMask(nodeID, ref _this, segment7, segment8, num42, ref surface8, ref heights4, ref edges10, ref num38, ref num39, ref num40, ref num41))
                    {
                        if (num38 != 0f || num39 != 1f || num42 != 0)
                        {
                            bezier2.a = Vector3.Lerp(a7, a8, num38);
                            bezier3.a = Vector3.Lerp(a7, a8, num39);
                            bezier2.d = Vector3.Lerp(d2, d3, num38);
                            bezier3.d = Vector3.Lerp(d2, d3, num39);
                        }
                        bezier2.a.y = bezier2.a.y + num40;
                        bezier3.a.y = bezier3.a.y + num41;
                        bezier2.d.y = bezier2.d.y + num40;
                        bezier3.d.y = bezier3.d.y + num41;
                        NetSegment.CalculateMiddlePoints(bezier2.a, -zero8, bezier2.d, -zero10, true, true, out bezier2.b, out bezier2.c);
                        NetSegment.CalculateMiddlePoints(bezier3.a, -zero9, bezier3.d, -zero11, true, true, out bezier3.b, out bezier3.c);
                        Vector3 vector31 = Vector3.Min(bezier2.Min(), bezier3.Min());
                        Vector3 vector32 = Vector3.Max(bezier2.Max(), bezier3.Max());
                        if (vector31.x <= maxX && vector31.z <= maxZ && minX <= vector32.x && minZ <= vector32.z)
                        {
                            float num43 = Vector3.Distance(bezier2.a, bezier2.b);
                            float num44 = Vector3.Distance(bezier2.b, bezier2.c);
                            float num45 = Vector3.Distance(bezier2.c, bezier2.d);
                            float num46 = Vector3.Distance(bezier3.a, bezier3.b);
                            float num47 = Vector3.Distance(bezier3.b, bezier3.c);
                            float num48 = Vector3.Distance(bezier3.c, bezier3.d);
                            Vector3 lhs2 = (bezier2.a - bezier2.b) * (1f / Mathf.Max(0.1f, num43));
                            Vector3 vector33 = (bezier2.c - bezier2.b) * (1f / Mathf.Max(0.1f, num44));
                            Vector3 rhs2 = (bezier2.d - bezier2.c) * (1f / Mathf.Max(0.1f, num45));
                            float num49 = Mathf.Min(Vector3.Dot(lhs2, vector33), Vector3.Dot(vector33, rhs2));
                            num43 += num44 + num45;
                            num46 += num47 + num48;
                            int num50 = Mathf.Clamp(Mathf.CeilToInt(Mathf.Min(Mathf.Max(num43, num46) * 0.25f, 100f - num49 * 100f)), 1, 16);
                            Vector3 a9 = bezier2.a;
                            Vector3 a10 = bezier3.a;
                            for (int num51 = 1; num51 <= num50; num51++)
                            {
                                Vector3 vector34 = bezier2.Position((float)num51 / (float)num50);
                                Vector3 vector35 = bezier3.Position((float)num51 / (float)num50);
                                TerrainModify.Edges edges11 = TerrainModify.Edges.AB | TerrainModify.Edges.CD;
                                if (info.m_lowerTerrain && (_this.m_flags & NetNode.Flags.OnGround) != NetNode.Flags.None)
                                {
                                    if (num51 == 1)
                                    {
                                        edges11 |= TerrainModify.Edges.DA;
                                    }
                                    else if (num51 == num50)
                                    {
                                        edges11 |= TerrainModify.Edges.BC;
                                    }
                                }
                                edges11 &= edges10;
                                TerrainModify.Surface surface9 = surface8;
                                if ((surface9 & TerrainModify.Surface.PavementA) != TerrainModify.Surface.None)
                                {
                                    surface9 |= TerrainModify.Surface.Gravel;
                                }
                                Vector3 zero12 = Vector3.zero;
                                if (flag6)
                                {
                                    zero12.y += info.m_maxHeight;
                                }
                                else if (info.m_lowerTerrain)
                                {
                                    zero12.y += info.m_netAI.GetTerrainLowerOffset();
                                }
                                TerrainModify.ApplyQuad(a9 + zero12, vector34 + zero12, vector35 + zero12, a10 + zero12, edges11, heights4, surface9);
                                a9 = vector34;
                                a10 = vector35;
                            }
                        }
                        num42++;
                    }
                }
                else if ((_this.m_flags & NetNode.Flags.End) != NetNode.Flags.None)
                {
                    Vector3 vector36 = Vector3.zero;
                    Vector3 vector37 = Vector3.zero;
                    Vector3 vector38 = Vector3.zero;
                    Vector3 vector39 = Vector3.zero;
                    Vector3 zero13 = Vector3.zero;
                    Vector3 zero14 = Vector3.zero;
                    Vector3 a11 = Vector3.zero;
                    Vector3 a12 = Vector3.zero;
                    ushort num52 = 0;
                    for (int num53 = 0; num53 < 8; num53++)
                    {
                        ushort segment10 = _this.GetSegment(num53);
                        if (segment10 != 0)
                        {
                            num52 = segment10;
                            NetSegment netSegment7 = Singleton<NetManager>.instance.m_segments.m_buffer[(int)segment10];
                            bool start3 = netSegment7.m_startNode == nodeID;
                            bool flag19;
                            netSegment7.CalculateCorner(segment10, false, start3, false, out vector36, out zero13, out flag19);
                            netSegment7.CalculateCorner(segment10, false, start3, true, out vector37, out zero14, out flag19);
                        }
                    }
                    Vector3 from3 = vector36;
                    Vector3 to3 = vector37;
                    Vector3 from4 = vector38;
                    Vector3 to4 = vector39;
                    TerrainModify.Heights heights5 = TerrainModify.Heights.None;
                    TerrainModify.Surface surface10 = TerrainModify.Surface.None;
                    if (flag6)
                    {
                        heights5 = TerrainModify.Heights.SecondaryMin;
                    }
                    else
                    {
                        if (flag5)
                        {
                            heights5 |= TerrainModify.Heights.PrimaryLevel;
                        }
                        if (info.m_lowerTerrain)
                        {
                            heights5 |= TerrainModify.Heights.PrimaryMax;
                        }
                        if (info.m_blockWater)
                        {
                            heights5 |= TerrainModify.Heights.BlockHeight;
                        }
                        if (flag)
                        {
                            surface10 |= TerrainModify.Surface.PavementA;
                        }
                        if (flag2)
                        {
                            surface10 |= TerrainModify.Surface.Gravel;
                        }
                        if (flag3)
                        {
                            surface10 |= TerrainModify.Surface.Ruined;
                        }
                        if (flag4)
                        {
                            surface10 |= TerrainModify.Surface.Clip;
                        }
                    }
                    TerrainModify.Edges edges12 = TerrainModify.Edges.All;
                    float num54 = 0f;
                    float num55 = 1f;
                    float num56 = 0f;
                    float num57 = 0f;
                    int num58 = 0;
                    while (info.m_netAI.NodeModifyMask(nodeID, ref _this, num52, num52, num58, ref surface10, ref heights5, ref edges12, ref num54, ref num55, ref num56, ref num57))
                    {
                        if (num54 != 0f || num55 != 1f || num58 != 0)
                        {
                            vector36 = Vector3.Lerp(from3, to3, num54);
                            vector37 = Vector3.Lerp(from3, to3, num55);
                            vector38 = Vector3.Lerp(from4, to4, num54);
                            vector39 = Vector3.Lerp(from4, to4, num55);
                        }
                        vector36.y += num56;
                        vector37.y += num57;
                        vector38.y += num56;
                        vector39.y += num57;
                        if (info.m_halfWidth < 3.999f)
                        {
                            vector38 = vector36 - zero13 * (info.m_halfWidth + 2f);
                            vector39 = vector37 - zero14 * (info.m_halfWidth + 2f);
                            float num59 = Mathf.Min(new float[]
                            {
                        Mathf.Min(Mathf.Min(vector36.x, vector37.x), Mathf.Min(vector38.x, vector39.x))
                            });
                            float num60 = Mathf.Max(new float[]
                            {
                        Mathf.Max(Mathf.Max(vector36.x, vector37.x), Mathf.Max(vector38.x, vector39.x))
                            });
                            float num61 = Mathf.Min(new float[]
                            {
                        Mathf.Min(Mathf.Min(vector36.z, vector37.z), Mathf.Min(vector38.z, vector39.z))
                            });
                            float num62 = Mathf.Max(new float[]
                            {
                        Mathf.Max(Mathf.Max(vector36.z, vector37.z), Mathf.Max(vector38.z, vector39.z))
                            });
                            if (num59 <= maxX && num61 <= maxZ && minX <= num60 && minZ <= num62)
                            {
                                TerrainModify.Edges edges13 = TerrainModify.Edges.AB | TerrainModify.Edges.BC | TerrainModify.Edges.CD;
                                if (info.m_lowerTerrain && (_this.m_flags & NetNode.Flags.OnGround) != NetNode.Flags.None)
                                {
                                    edges13 |= TerrainModify.Edges.DA;
                                }
                                edges13 &= edges12;
                                TerrainModify.Surface surface11 = surface10;
                                if ((surface11 & TerrainModify.Surface.PavementA) != TerrainModify.Surface.None)
                                {
                                    surface11 |= TerrainModify.Surface.Gravel;
                                }
                                Vector3 zero15 = Vector3.zero;
                                if (flag6)
                                {
                                    zero15.y += info.m_maxHeight;
                                }
                                else if (info.m_lowerTerrain)
                                {
                                    zero15.y += info.m_netAI.GetTerrainLowerOffset();
                                }
                                TerrainModify.ApplyQuad(vector36 + zero15, vector38 + zero15, vector39 + zero15, vector37 + zero15, edges13, heights5, surface11);
                            }
                        }
                        else
                        {
                            vector38 = vector37;
                            vector39 = vector36;
                            a11 = zero14;
                            a12 = zero13;
                            float d4 = info.m_netAI.GetEndRadius() * 1.33333337f * 1.1f;
                            Vector3 b3 = vector36 - zero13 * d4;
                            Vector3 c2 = vector38 - a11 * d4;
                            Vector3 vector40 = vector37 + zero14 * d4;
                            Vector3 vector41 = vector39 + a12 * d4;
                            float num63 = Mathf.Min(Mathf.Min(Mathf.Min(vector36.x, vector37.x), Mathf.Min(b3.x, vector40.x)), Mathf.Min(Mathf.Min(c2.x, vector41.x), Mathf.Min(vector38.x, vector39.x)));
                            float num64 = Mathf.Max(Mathf.Max(Mathf.Max(vector36.x, vector37.x), Mathf.Max(b3.x, vector40.x)), Mathf.Max(Mathf.Max(c2.x, vector41.x), Mathf.Max(vector38.x, vector39.x)));
                            float num65 = Mathf.Min(Mathf.Min(Mathf.Min(vector36.z, vector37.z), Mathf.Min(b3.z, vector40.z)), Mathf.Min(Mathf.Min(c2.z, vector41.z), Mathf.Min(vector38.z, vector39.z)));
                            float num66 = Mathf.Max(Mathf.Max(Mathf.Max(vector36.z, vector37.z), Mathf.Max(b3.z, vector40.z)), Mathf.Max(Mathf.Max(c2.z, vector41.z), Mathf.Max(vector38.z, vector39.z)));
                            if (num63 <= maxX && num65 <= maxZ && minX <= num64 && minZ <= num66)
                            {
                                int num67 = Mathf.Clamp(Mathf.CeilToInt(info.m_halfWidth * 0.4f), 2, 8);
                                Vector3 a13 = vector36;
                                Vector3 a14 = (vector36 + vector37) * 0.5f;
                                for (int num68 = 1; num68 <= num67; num68++)
                                {
                                    Vector3 a15 = Bezier3.Position(vector36, b3, c2, vector38, ((float)num68 - 0.5f) / (float)num67);
                                    Vector3 vector42 = Bezier3.Position(vector36, b3, c2, vector38, (float)num68 / (float)num67);
                                    TerrainModify.Edges edges14 = TerrainModify.Edges.AB | TerrainModify.Edges.BC;
                                    edges14 &= edges12;
                                    TerrainModify.Surface surface12 = surface10;
                                    if ((surface12 & TerrainModify.Surface.PavementA) != TerrainModify.Surface.None)
                                    {
                                        surface12 |= TerrainModify.Surface.Gravel;
                                    }
                                    Vector3 zero16 = Vector3.zero;
                                    if (flag6)
                                    {
                                        zero16.y += info.m_maxHeight;
                                    }
                                    else if (info.m_lowerTerrain)
                                    {
                                        zero16.y += info.m_netAI.GetTerrainLowerOffset();
                                    }
                                    TerrainModify.ApplyQuad(a13 + zero16, a15 + zero16, vector42 + zero16, a14 + zero16, edges14, heights5, surface12);
                                    a13 = vector42;
                                }
                            }
                        }
                        num58++;
                    }
                }
                if (_this.m_lane != 0u && info.m_halfWidth < 3.999f)
                {
                    Vector3 a16 = Singleton<NetManager>.instance.m_lanes.m_buffer[(int)((UIntPtr)_this.m_lane)].CalculatePosition((float)_this.m_laneOffset * 0.003921569f);
                    float num69 = 0f;
                    Vector3 vector43 = VectorUtils.NormalizeXZ(a16 - _this.m_position, out num69);
                    if (num69 > 1f)
                    {
                        Vector3 a17 = _this.m_position - new Vector3(vector43.x + vector43.z * info.m_halfWidth, 0f, vector43.z - vector43.x * info.m_halfWidth);
                        Vector3 a18 = _this.m_position - new Vector3(vector43.x - vector43.z * info.m_halfWidth, 0f, vector43.z + vector43.x * info.m_halfWidth);
                        Vector3 a19 = a16 + new Vector3(vector43.x - vector43.z * info.m_halfWidth, 0f, vector43.z + vector43.x * info.m_halfWidth);
                        Vector3 a20 = a16 + new Vector3(vector43.x + vector43.z * info.m_halfWidth, 0f, vector43.z - vector43.x * info.m_halfWidth);
                        float num70 = Mathf.Min(new float[]
                        {
                    Mathf.Min(Mathf.Min(a17.x, a18.x), Mathf.Min(a19.x, a20.x))
                        });
                        float num71 = Mathf.Max(new float[]
                        {
                    Mathf.Max(Mathf.Max(a17.x, a18.x), Mathf.Max(a19.x, a20.x))
                        });
                        float num72 = Mathf.Min(new float[]
                        {
                    Mathf.Min(Mathf.Min(a17.z, a18.z), Mathf.Min(a19.z, a20.z))
                        });
                        float num73 = Mathf.Max(new float[]
                        {
                    Mathf.Max(Mathf.Max(a17.z, a18.z), Mathf.Max(a19.z, a20.z))
                        });
                        if (num70 <= maxX && num72 <= maxZ && minX <= num71 && minZ <= num73)
                        {
                            TerrainModify.Edges edges15 = TerrainModify.Edges.All;
                            TerrainModify.Heights heights6 = TerrainModify.Heights.None;
                            TerrainModify.Surface surface13 = TerrainModify.Surface.None;
                            if (flag)
                            {
                                surface13 |= (TerrainModify.Surface.PavementA | TerrainModify.Surface.Gravel);
                            }
                            if (flag2)
                            {
                                surface13 |= TerrainModify.Surface.Gravel;
                            }
                            if (flag3)
                            {
                                surface13 |= TerrainModify.Surface.Ruined;
                            }
                            Vector3 zero17 = Vector3.zero;
                            TerrainModify.ApplyQuad(a17 + zero17, a19 + zero17, a20 + zero17, a18 + zero17, edges15, heights6, surface13);
                        }
                    }
                }
            }
        }
        private static void CheckHeightOffset(ref NetNode _this, ushort nodeID)
        {
            NetManager instance = Singleton<NetManager>.instance;
            bool flag = Singleton<TerrainManager>.instance.HasDetailMapping(_this.m_position);
            for (int i = 0; i < 8; i++)
            {
                ushort segment = _this.GetSegment(i);
                if (segment != 0)
                {
                    ushort startNode = instance.m_segments.m_buffer[(int)segment].m_startNode;
                    ushort endNode = instance.m_segments.m_buffer[(int)segment].m_endNode;
                    if (startNode == nodeID)
                    {
                        Vector3 position = instance.m_nodes.m_buffer[(int)endNode].m_position;
                        flag = (flag && Singleton<TerrainManager>.instance.HasDetailMapping(position));
                    }
                    else
                    {
                        Vector3 position2 = instance.m_nodes.m_buffer[(int)startNode].m_position;
                        flag = (flag && Singleton<TerrainManager>.instance.HasDetailMapping(position2));
                    }
                }
            }
            NetInfo info = _this.Info;
            byte b = (!flag && info.m_requireSurfaceMaps) ? (byte)64 : (byte)0;
            if (b != _this.m_heightOffset)
            {
                _this.m_heightOffset = b;
                BuildingInfo newBuilding;
                float heightOffset;
                info.m_netAI.GetNodeBuilding(nodeID, ref _this, out newBuilding, out heightOffset);
                _this.UpdateBuilding(nodeID, newBuilding, heightOffset);
                instance.UpdateNodeFlags(nodeID);
                instance.UpdateNodeRenderer(nodeID, true);
                for (int j = 0; j < 8; j++)
                {
                    ushort segment2 = _this.GetSegment(j);
                    if (segment2 != 0)
                    {
                        instance.m_segments.m_buffer[(int)segment2].UpdateLanes(segment2, false);
                        instance.UpdateSegmentFlags(segment2);
                        instance.UpdateSegmentRenderer(segment2, true);
                    }
                }
            }
        }

    }
}
