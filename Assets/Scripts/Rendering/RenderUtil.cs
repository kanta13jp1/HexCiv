using System.Collections.Generic;
using HexCiv.Core;
using UnityEngine;

namespace HexCiv.Render
{
    /// <summary>
    /// 描画モジュール内部の共有ヘルパー。
    /// フォント取得・マテリアル生成・ヘクス形状定数・簡易メッシュ構築を提供する。
    /// (UIモジュールには依存しない — 日本語フォントの取得はここで独自に行う)
    /// </summary>
    internal static class RenderUtil
    {
        /// <summary>ポインティトップ六角形の頂点方向(角度 -30°+60°*i、XZ平面、半径1)。</summary>
        public static readonly Vector3[] Corners;

        /// <summary>隣接方向 d(HexCoord.Directions の添字)が共有する辺の頂点添字(A側)。</summary>
        public static readonly int[] EdgeCornerA = { 0, 5, 4, 3, 2, 1 };
        /// <summary>隣接方向 d が共有する辺の頂点添字(B側)。</summary>
        public static readonly int[] EdgeCornerB = { 1, 0, 5, 4, 3, 2 };

        static Font jpFont;

        static RenderUtil()
        {
            Corners = new Vector3[6];
            for (int i = 0; i < 6; i++)
            {
                float a = (-30f + 60f * i) * Mathf.Deg2Rad;
                Corners[i] = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            }
        }

        /// <summary>
        /// OSの日本語フォントを取得する(キャッシュ)。
        /// TextMesh に使う場合は font と font.material の両方を割り当てること。
        /// </summary>
        public static Font JapaneseFont()
        {
            if (jpFont != null) return jpFont;

            string[] candidates = { "Yu Gothic UI", "Meiryo UI", "Meiryo", "MS Gothic" };
            string[] installed = null;
            try { installed = Font.GetOSInstalledFontNames(); } catch { }

            if (installed != null)
            {
                foreach (var name in candidates)
                {
                    bool found = false;
                    for (int i = 0; i < installed.Length; i++)
                        if (installed[i] == name) { found = true; break; }
                    if (!found) continue;
                    var f = Font.CreateDynamicFontFromOSFont(name, 32);
                    if (f != null) { jpFont = f; return jpFont; }
                }
            }

            // インストール一覧から見つからなくても一括指定で試す
            var multi = Font.CreateDynamicFontFromOSFont(candidates, 32);
            if (multi != null) { jpFont = multi; return jpFont; }

            // 最終手段(日本語グリフは出ないが null は返さない)
            jpFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return jpFont;
        }

        /// <summary>ビルドに必ず含まれる Sprites/Default の新規マテリアル。</summary>
        public static Material NewSpriteMaterial()
        {
            return new Material(Shader.Find("Sprites/Default"));
        }

        // ---- 丘陵の立体化(2026-07-21 追加。表示のみ・シミュレーション非干渉) ----

        /// <summary>丘陵タイルの視覚的な持ち上げ量(ワールドY)。</summary>
        public const float HillHeight = 0.14f;

        /// <summary>
        /// タイルの視覚的な持ち上げ高さ(表示専用)。丘陵のある陸地タイルのみ HillHeight、
        /// それ以外は 0。山岳は既存の峰デコレーションが立体感を担うため持ち上げない
        /// (二重に隆起させない)。水タイルも常に 0。
        /// マウス判定(XZ平面の数学的交差)やシミュレーションには一切影響しない。
        /// </summary>
        public static float TileVisualHeight(Tile t)
        {
            if (t == null) return 0f;
            if (t.Terrain == TerrainType.Ocean ||
                t.Terrain == TerrainType.Coast ||
                t.Terrain == TerrainType.Mountain) return 0f;
            return t.HasHill ? HillHeight : 0f;
        }

        /// <summary>座標から決定的な int ハッシュ(表示ゆらぎ用。シミュレーションでは使わない)。</summary>
        public static int HashInt(HexCoord c)
        {
            unchecked
            {
                int h = c.q * 73856093 ^ c.r * 19349663;
                h ^= h >> 13;
                h *= 1274126177;
                h ^= h >> 16;
                return h;
            }
        }

        /// <summary>座標から決定的な 0..1 のハッシュ値。</summary>
        public static float Hash01(HexCoord c)
        {
            return ((HashInt(c) & 0x7FFFFFFF) % 10000) / 10000f;
        }

        /// <summary>MeshFilter+MeshRenderer 付きの子 GameObject を作る。</summary>
        public static MeshRenderer NewMeshChild(Transform parent, string name, Mesh mesh, Material mat, Vector3 localPos, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.sortingOrder = sortingOrder;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            return mr;
        }

        // ---- 共有メッシュの構築(EntityRenderer 用) ----

        /// <summary>XZ平面の円盤(扇形ファン)。</summary>
        public static Mesh BuildDisc(float radius, int segments, Color color)
        {
            var mb = new MeshBuilder();
            for (int i = 0; i < segments; i++)
            {
                float a0 = Mathf.PI * 2f * i / segments;
                float a1 = Mathf.PI * 2f * (i + 1) / segments;
                mb.AddTriangle(
                    Vector3.zero,
                    new Vector3(Mathf.Cos(a1) * radius, 0f, Mathf.Sin(a1) * radius),
                    new Vector3(Mathf.Cos(a0) * radius, 0f, Mathf.Sin(a0) * radius),
                    color);
            }
            return mb.Build(null);
        }

        /// <summary>XZ平面のリング(輪)。</summary>
        public static Mesh BuildRing(float rInner, float rOuter, int segments, Color color)
        {
            var mb = new MeshBuilder();
            for (int i = 0; i < segments; i++)
            {
                float a0 = Mathf.PI * 2f * i / segments;
                float a1 = Mathf.PI * 2f * (i + 1) / segments;
                Vector3 d0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0));
                Vector3 d1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
                mb.AddQuad(d0 * rOuter, d1 * rOuter, d1 * rInner, d0 * rInner, color);
            }
            return mb.Build(null);
        }

        /// <summary>
        /// XZ平面の矩形。leftPivot=false なら中心原点(x: -w/2..w/2)、
        /// true なら左端原点(x: 0..w)。z は -d/2..d/2。
        /// </summary>
        public static Mesh BuildQuadXZ(float width, float depth, Color color, bool leftPivot)
        {
            var mb = new MeshBuilder();
            float x0 = leftPivot ? 0f : -width * 0.5f;
            float x1 = leftPivot ? width : width * 0.5f;
            float z0 = -depth * 0.5f;
            float z1 = depth * 0.5f;
            mb.AddQuad(
                new Vector3(x0, 0f, z1),
                new Vector3(x1, 0f, z1),
                new Vector3(x1, 0f, z0),
                new Vector3(x0, 0f, z0),
                color);
            return mb.Build(null);
        }

        /// <summary>底面 y=0、高さ h の直方体(底面なし)。上面と側面で色を変えて立体感を出す。</summary>
        public static Mesh BuildBox(float width, float height, float depth, Color topColor, Color sideColor)
        {
            var mb = new MeshBuilder();
            float hw = width * 0.5f;
            float hd = depth * 0.5f;

            Vector3 b0 = new Vector3(-hw, 0f, -hd);
            Vector3 b1 = new Vector3(hw, 0f, -hd);
            Vector3 b2 = new Vector3(hw, 0f, hd);
            Vector3 b3 = new Vector3(-hw, 0f, hd);
            Vector3 t0 = b0 + Vector3.up * height;
            Vector3 t1 = b1 + Vector3.up * height;
            Vector3 t2 = b2 + Vector3.up * height;
            Vector3 t3 = b3 + Vector3.up * height;

            Color sideDark = new Color(sideColor.r * 0.85f, sideColor.g * 0.85f, sideColor.b * 0.85f, sideColor.a);

            mb.AddQuad(t3, t2, t1, t0, topColor);       // 上面
            mb.AddQuad(t0, t1, b1, b0, sideColor);      // 南面(手前)
            mb.AddQuad(t2, t3, b3, b2, sideDark);       // 北面
            mb.AddQuad(t1, t2, b2, b1, sideDark);       // 東面
            mb.AddQuad(t3, t0, b0, b3, sideColor);      // 西面
            return mb.Build(null);
        }
    }

    /// <summary>頂点カラー付きメッシュの簡易ビルダー(法線なし・UVなし、Sprites/Default 用)。</summary>
    internal class MeshBuilder
    {
        readonly List<Vector3> verts = new List<Vector3>();
        readonly List<Color32> colors = new List<Color32>();
        readonly List<int> tris = new List<int>();

        public int VertexCount => verts.Count;

        public void Clear()
        {
            verts.Clear();
            colors.Clear();
            tris.Clear();
        }

        public void AddTriangle(Vector3 a, Vector3 b, Vector3 c, Color col)
        {
            int i = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c);
            Color32 c32 = col;
            colors.Add(c32); colors.Add(c32); colors.Add(c32);
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
        }

        /// <summary>外周順の4点で四角形(2三角形)を追加する。</summary>
        public void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color col)
        {
            int i = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
            Color32 c32 = col;
            colors.Add(c32); colors.Add(c32); colors.Add(c32); colors.Add(c32);
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
            tris.Add(i); tris.Add(i + 2); tris.Add(i + 3);
        }

        /// <summary>中心+6頂点(計7頂点)のヘクスを追加する。上向きの面。</summary>
        public void AddHex(Vector3 center, float radius, Color col)
        {
            int ci = verts.Count;
            Color32 c32 = col;
            verts.Add(center);
            colors.Add(c32);
            for (int i = 0; i < 6; i++)
            {
                verts.Add(center + RenderUtil.Corners[i] * radius);
                colors.Add(c32);
            }
            for (int i = 0; i < 6; i++)
            {
                tris.Add(ci);
                tris.Add(ci + 1 + (i + 1) % 6);
                tris.Add(ci + 1 + i);
            }
        }

        /// <summary>小さなひし形(資源・経路マーカー用)。</summary>
        public void AddDiamond(Vector3 center, float radius, Color col)
        {
            AddQuad(
                center + new Vector3(0f, 0f, radius),
                center + new Vector3(radius, 0f, 0f),
                center + new Vector3(0f, 0f, -radius),
                center + new Vector3(-radius, 0f, 0f),
                col);
        }

        /// <summary>
        /// 現在の頂点色を新しい配列へ複製する(構築直後のキャッシュ取得用)。
        /// 構築時に一度だけ呼ぶ想定で、毎フレーム経路では使わない。
        /// </summary>
        public Color32[] ColorsToArray()
        {
            return colors.ToArray();
        }

        /// <summary>リストの内容でメッシュを(再)構築する。65k超対策として常に UInt32 インデックス。</summary>
        public Mesh Build(Mesh mesh)
        {
            if (mesh == null) mesh = new Mesh();
            mesh.Clear();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetColors(colors);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
