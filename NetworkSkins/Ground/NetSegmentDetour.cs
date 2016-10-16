using System.Reflection;
using ColossalFramework;
using NetworkSkins.Detour;
using UnityEngine;
using System;
using NetworkSkins.Data;
using ColossalFramework.Math;

namespace NetworkSkins.Ground
{
    public struct NetSegmentDetour
    {
        private static bool deployed = false;

        private static RedirectCallsState _NetSegment_TerrainUpdated_state;
        private static MethodInfo _NetSegment_TerrainUpdated_original;
        private static MethodInfo _NetSegment_TerrainUpdated_detour;

        public static void Deploy()
        {
            if (!deployed)
            {
                _NetSegment_TerrainUpdated_original = typeof(NetSegment).GetMethod("TerrainUpdated", BindingFlags.Instance | BindingFlags.Public);
                _NetSegment_TerrainUpdated_detour = typeof(NetSegmentDetour).GetMethod("TerrainUpdated", BindingFlags.Instance | BindingFlags.Public);
                _NetSegment_TerrainUpdated_state = RedirectionHelper.RedirectCalls(_NetSegment_TerrainUpdated_original, _NetSegment_TerrainUpdated_detour);

                deployed = true;
            }
        }

        public static void Revert()
        {
            if (deployed)
            {
                RedirectionHelper.RevertRedirect(_NetSegment_TerrainUpdated_original, _NetSegment_TerrainUpdated_state);
                _NetSegment_TerrainUpdated_original = null;
                _NetSegment_TerrainUpdated_detour = null;

                deployed = false;
            }
        }

        // NetSegment
        public void TerrainUpdated(ushort segmentID, float minX, float minZ, float maxX, float maxZ)
        {
            NetSegment _this = NetManager.instance.m_segments.m_buffer[segmentID];

            if ((_this.m_flags & (NetSegment.Flags.Created | NetSegment.Flags.Deleted)) != NetSegment.Flags.Created)
            {
                return;
            }
            NetInfo info = _this.Info;
            if (info == null)
            {
                return;
            }

            // mod begin
            var segmentData = SegmentDataManager.Instance.SegmentToSegmentDataMap?[segmentID];
            var customGround = segmentData != null && (segmentData.Features & SegmentData.FeatureFlags.GroundTexture) != 0;
            var customGravel = customGround && (segmentData.Features & SegmentData.FeatureFlags.CreateGravel) != 0;
            var customPavement = customGround && (segmentData.Features & SegmentData.FeatureFlags.CreatePavement) != 0;

            if (GroundCustomizer.Instance.test == null && segmentData != null && customGround)
                GroundCustomizer.Instance.test = $"TerrainUpdated called on segment with data. {_this.Info.name} + {customGround} + {customGravel} + {customPavement}";
            // mod end

            bool flag = (Singleton<NetManager>.instance.m_nodes.m_buffer[(int)_this.m_startNode].m_flags & NetNode.Flags.OnGround) != NetNode.Flags.None;
            bool flag2 = (Singleton<NetManager>.instance.m_nodes.m_buffer[(int)_this.m_endNode].m_flags & NetNode.Flags.OnGround) != NetNode.Flags.None;
            // mod begin
            bool flag3 = ((customGround && customPavement) || info.m_createPavement) && (!info.m_lowerTerrain || flag || flag2);
            bool flag4 = ((customGround && customGravel) || info.m_createGravel) && !info.m_lowerTerrain;
            bool flag5 = ((customGround && !customPavement && !customGravel) || info.m_createRuining) && !info.m_lowerTerrain;
            // mod end
            bool flag6 = info.m_clipTerrain && !info.m_lowerTerrain;
            bool flag7 = !info.m_flattenTerrain && !info.m_lowerTerrain && info.m_netAI.RaiseTerrain();
            if (info.m_flattenTerrain || info.m_lowerTerrain || flag7 || info.m_blockWater || flag3 || flag4 || flag5 || flag6)
            {
                Bezier3 bezier = default(Bezier3);
                Bezier3 bezier2 = default(Bezier3);
                Vector3 startDir;
                bool smoothStart;
                _this.CalculateCorner(segmentID, false, true, true, out bezier.a, out startDir, out smoothStart);
                Vector3 endDir;
                bool smoothEnd;
                _this.CalculateCorner(segmentID, false, false, true, out bezier2.d, out endDir, out smoothEnd);
                Vector3 startDir2;
                _this.CalculateCorner(segmentID, false, true, false, out bezier2.a, out startDir2, out smoothStart);
                Vector3 endDir2;
                _this.CalculateCorner(segmentID, false, false, false, out bezier.d, out endDir2, out smoothEnd);
                Vector3 a = bezier.a;
                Vector3 a2 = bezier2.a;
                Vector3 d = bezier.d;
                Vector3 d2 = bezier2.d;
                TerrainModify.Heights heights = TerrainModify.Heights.None;
                if (info.m_flattenTerrain)
                {
                    heights |= TerrainModify.Heights.PrimaryLevel;
                }
                if (info.m_lowerTerrain)
                {
                    heights |= TerrainModify.Heights.PrimaryMax;
                }
                if (info.m_blockWater)
                {
                    heights |= TerrainModify.Heights.BlockHeight;
                }
                if (flag7)
                {
                    heights = TerrainModify.Heights.SecondaryMin;
                }
                TerrainModify.Surface surface = TerrainModify.Surface.None;
                if (flag3)
                {
                    surface |= TerrainModify.Surface.PavementA;
                }
                if (flag4)
                {
                    surface |= TerrainModify.Surface.Gravel;
                }
                if (flag5)
                {
                    surface |= TerrainModify.Surface.Ruined;
                }
                if (flag6)
                {
                    surface |= TerrainModify.Surface.Clip;
                }
                TerrainModify.Edges edges = TerrainModify.Edges.All;
                float num = 0f;
                float num2 = 1f;
                float num3 = 0f;
                float num4 = 0f;
                float num5 = 0f;
                float num6 = 0f;
                int num7 = 0;
                while (info.m_netAI.SegmentModifyMask(segmentID, ref _this, num7, ref surface, ref heights, ref edges, ref num, ref num2, ref num3, ref num4, ref num5, ref num6))
                {
                    if (num != 0f || num2 != 1f || num7 != 0)
                    {
                        bezier.a = Vector3.Lerp(a, a2, num);
                        bezier2.a = Vector3.Lerp(a, a2, num2);
                        bezier.d = Vector3.Lerp(d, d2, num);
                        bezier2.d = Vector3.Lerp(d, d2, num2);
                    }
                    bezier.a.y = bezier.a.y + num3;
                    bezier2.a.y = bezier2.a.y + num4;
                    bezier.d.y = bezier.d.y + num5;
                    bezier2.d.y = bezier2.d.y + num6;
                    NetSegment.CalculateMiddlePoints(bezier.a, startDir, bezier.d, endDir2, smoothStart, smoothEnd, out bezier.b, out bezier.c);
                    NetSegment.CalculateMiddlePoints(bezier2.a, startDir2, bezier2.d, endDir, smoothStart, smoothEnd, out bezier2.b, out bezier2.c);
                    Vector3 vector = Vector3.Min(bezier.Min(), bezier2.Min());
                    Vector3 vector2 = Vector3.Max(bezier.Max(), bezier2.Max());
                    if (vector.x <= maxX && vector.z <= maxZ && minX <= vector2.x && minZ <= vector2.z)
                    {
                        float num8 = Vector3.Distance(bezier.a, bezier.b);
                        float num9 = Vector3.Distance(bezier.b, bezier.c);
                        float num10 = Vector3.Distance(bezier.c, bezier.d);
                        float num11 = Vector3.Distance(bezier2.a, bezier2.b);
                        float num12 = Vector3.Distance(bezier2.b, bezier2.c);
                        float num13 = Vector3.Distance(bezier2.c, bezier2.d);
                        Vector3 lhs = (bezier.a - bezier.b) * (1f / Mathf.Max(0.1f, num8));
                        Vector3 vector3 = (bezier.c - bezier.b) * (1f / Mathf.Max(0.1f, num9));
                        Vector3 rhs = (bezier.d - bezier.c) * (1f / Mathf.Max(0.1f, num10));
                        float num14 = Mathf.Min(Vector3.Dot(lhs, vector3), Vector3.Dot(vector3, rhs));
                        num8 += num9 + num10;
                        num11 += num12 + num13;
                        float num15;
                        float num16;
                        info.m_netAI.GetTerrainModifyRange(out num15, out num16);
                        int num17 = Mathf.Clamp(Mathf.CeilToInt(Mathf.Min(Mathf.Max(num8, num11) * 0.25f * (num16 - num15), 100f - num14 * 100f)), 1, 16);
                        float num18 = num15;
                        Vector3 a3 = bezier.Position(num18);
                        Vector3 d3 = bezier2.Position(num18);
                        num18 = 3f * num18 * num18 - 2f * num18 * num18 * num18;
                        float num19 = info.m_terrainStartOffset + (info.m_terrainEndOffset - info.m_terrainStartOffset) * num18;
                        if (info.m_lowerTerrain)
                        {
                            num19 += info.m_netAI.GetTerrainLowerOffset();
                        }
                        if (flag7)
                        {
                            num19 += info.m_maxHeight;
                        }
                        a3.y += num19;
                        d3.y += num19;
                        for (int i = 1; i <= num17; i++)
                        {
                            num18 = num15 + (num16 - num15) * (float)i / (float)num17;
                            Vector3 vector4 = bezier.Position(num18);
                            Vector3 vector5 = bezier2.Position(num18);
                            num18 = 3f * num18 * num18 - 2f * num18 * num18 * num18;
                            num19 = info.m_terrainStartOffset + (info.m_terrainEndOffset - info.m_terrainStartOffset) * num18;
                            if (info.m_lowerTerrain)
                            {
                                num19 += info.m_netAI.GetTerrainLowerOffset();
                            }
                            if (flag7)
                            {
                                num19 += info.m_maxHeight;
                            }
                            vector4.y += num19;
                            vector5.y += num19;
                            TerrainModify.Surface surface2 = surface;
                            // mod
                            if ((customGround ? !customPavement : !info.m_createPavement) || (info.m_lowerTerrain && (!flag || i != 1) && (!flag2 || i != num17)))
                            {
                                surface2 &= ~TerrainModify.Surface.PavementA;
                            }
                            if ((surface2 & TerrainModify.Surface.PavementA) != TerrainModify.Surface.None)
                            {
                                surface2 |= TerrainModify.Surface.Gravel;
                            }
                            TerrainModify.Edges edges2 = TerrainModify.Edges.AB | TerrainModify.Edges.CD;
                            if (num15 != 0f && i == 1)
                            {
                                edges2 |= TerrainModify.Edges.DA;
                            }
                            if (num16 != 1f && i == num17)
                            {
                                edges2 |= TerrainModify.Edges.BC;
                            }
                            edges2 &= edges;
                            TerrainModify.ApplyQuad(a3, vector4, vector5, d3, edges2, heights, surface2);
                            a3 = vector4;
                            d3 = vector5;
                        }
                        if (num7 == 0 && num15 != 0f && !flag7 && info.m_netAI.RaiseTerrain())
                        {
                            num17 = Mathf.Clamp(Mathf.CeilToInt(Mathf.Min(Mathf.Max(num8, num11) * 0.25f * num15, 100f - num14 * 100f)), 1, 16);
                            a3 = bezier.a;
                            d3 = bezier2.a;
                            num19 = info.m_terrainStartOffset * 0.75f;
                            a3.y += num19;
                            d3.y += num19;
                            for (int j = 1; j <= num17; j++)
                            {
                                num18 = num15 * (float)j / (float)num17;
                                Vector3 vector6 = bezier.Position(num18);
                                Vector3 vector7 = bezier2.Position(num18);
                                num18 = 3f * num18 * num18 - 2f * num18 * num18 * num18;
                                vector6.y += num19;
                                vector7.y += num19;
                                TerrainModify.Edges edges3 = TerrainModify.Edges.AB | TerrainModify.Edges.CD;
                                if (j == 1)
                                {
                                    edges3 |= TerrainModify.Edges.DA;
                                }
                                edges3 &= edges;
                                TerrainModify.ApplyQuad(a3, vector6, vector7, d3, edges3, TerrainModify.Heights.SecondaryMin, TerrainModify.Surface.None);
                                a3 = vector6;
                                d3 = vector7;
                            }
                        }
                    }
                    num7++;
                }
            }
        }

    }
}
