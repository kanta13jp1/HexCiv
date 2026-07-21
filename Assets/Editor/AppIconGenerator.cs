using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// アプリケーションアイコンの手続き生成(2026-07-21 Claude Code 追加)。
/// 256x256 の HexCiv ロゴ(濃紺の下地+金の六角形輪郭+内側に緑/黄土のミニヘクスマップ)を
/// Assets/Icon/app_icon.png へ描画し、PlayerSettings(既定アイコン/スタンドアロン全サイズ)へ
/// 割り当てる。PNG が既に存在する場合は再生成せず、インポート設定と割り当てのみ確認する(冪等)。
/// BuildScript.PerformBuild の冒頭から呼ばれる。エディタ専用でシミュレーションには一切影響しない。
/// </summary>
public static class AppIconGenerator
{
    const string IconFolder = "Assets/Icon";
    const string IconPath = "Assets/Icon/app_icon.png";
    const int IconSize = 256;

    /// <summary>アイコンPNGの存在・インポート設定・PlayerSettings割り当てを保証する(冪等)。</summary>
    public static void EnsureIcon()
    {
        if (!File.Exists(IconPath))
        {
            if (!AssetDatabase.IsValidFolder(IconFolder))
                AssetDatabase.CreateFolder("Assets", "Icon");

            var tex = DrawIconTexture();
            File.WriteAllBytes(IconPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(IconPath, ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("AppIconGenerator: generated " + IconPath);
        }

        ConfigureImporter();
        AssignPlayerIcons();
    }

    /// <summary>
    /// アイコンを CPU で手続き描画する。全形状は符号付き距離+約1.5pxの被覆率でアンチエイリアス。
    /// 構図: 縦グラデーションの濃紺地 → ミニヘクス7枚(中心+リング6、緑/黄土) → 金の六角形輪郭。
    /// </summary>
    static Texture2D DrawIconTexture()
    {
        var px = new Color[IconSize * IconSize];

        var navyTop    = new Color(0.10f, 0.14f, 0.25f, 1f);
        var navyBottom = new Color(0.045f, 0.065f, 0.125f, 1f);
        var gold       = new Color(0.93f, 0.78f, 0.32f, 1f);
        var green      = new Color(0.34f, 0.56f, 0.28f, 1f);
        var greenLight = new Color(0.44f, 0.66f, 0.34f, 1f);
        var tan        = new Color(0.78f, 0.65f, 0.40f, 1f);

        var center = new Vector2(IconSize * 0.5f, IconSize * 0.5f);
        const float bigApothem = 97f;   // 外周ヘクス(尖り上)の中心から辺までの距離
        const float strokeHalf = 7f;    // 金の輪郭線の半幅(線幅14px)。頂点方向の外端は半径約120<128
        float miniApothem = 27f * 0.8660254f;   // ミニヘクス(外接半径27)の辺までの距離

        // ミニヘクス7枚: 中心1枚+リング6枚(30°+60°刻み)。中心間距離は 2*apothem+4px で
        // 4pxの濃紺の隙間がヘクスマップらしい区切りになる。最大範囲は半径約78で輪郭の内側に収まる。
        var miniCenters = new Vector2[7];
        var miniColors = new Color[7];
        miniCenters[0] = Vector2.zero;
        miniColors[0] = greenLight;
        float ringDist = miniApothem * 2f + 4f;
        for (int k = 0; k < 6; k++)
        {
            float ang = (30f + 60f * k) * Mathf.Deg2Rad;
            miniCenters[k + 1] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * ringDist;
            miniColors[k + 1] = (k % 2 == 0) ? tan : green;
        }

        for (int y = 0; y < IconSize; y++)
        {
            float ty = (y + 0.5f) / IconSize;
            for (int x = 0; x < IconSize; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f) - center;
                var col = Color.Lerp(navyBottom, navyTop, ty);

                // 内側のミニヘクスマップ
                for (int i = 0; i < miniCenters.Length; i++)
                {
                    float d = HexDistance(p - miniCenters[i]) - miniApothem;
                    float cov = Coverage(-d);
                    if (cov > 0f) col = Color.Lerp(col, miniColors[i], cov);
                }

                // 金の六角形輪郭(距離の絶対値から線を作る)
                float dOutline = Mathf.Abs(HexDistance(p) - bigApothem) - strokeHalf;
                float covOutline = Coverage(-dOutline);
                if (covOutline > 0f) col = Color.Lerp(col, gold, covOutline);

                col.a = 1f;
                px[y * IconSize + x] = col;
            }
        }

        var tex = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false);
        tex.SetPixels(px);
        tex.Apply(false, false);
        return tex;
    }

    /// <summary>
    /// 尖り上(pointy-top)正六角形の中心からの疑似距離(辺法線 0°/60°/120° への最大射影)。
    /// 戻り値が apothem と等しい点がちょうど辺上になる。
    /// </summary>
    static float HexDistance(Vector2 p)
    {
        float ax = Mathf.Abs(p.x);
        float ay = Mathf.Abs(p.y);
        return Mathf.Max(ax, ax * 0.5f + ay * 0.8660254f);
    }

    /// <summary>符号付き距離(内側が正)を約1.5px幅のなだらかな被覆率0..1へ変換する。</summary>
    static float Coverage(float inside)
    {
        return Mathf.Clamp01(inside / 1.5f + 0.5f);
    }

    /// <summary>
    /// TextureImporter を「読み取り可能・Sprite化なし(Default)・非圧縮・ミップマップなし」へ揃える。
    /// 既に一致していれば再インポートしない(冪等)。
    /// </summary>
    static void ConfigureImporter()
    {
        var importer = AssetImporter.GetAtPath(IconPath) as TextureImporter;
        if (importer == null) return;

        bool dirty = false;
        if (importer.textureType != TextureImporterType.Default)
        {
            importer.textureType = TextureImporterType.Default;
            dirty = true;
        }
        if (!importer.isReadable) { importer.isReadable = true; dirty = true; }
        if (importer.mipmapEnabled) { importer.mipmapEnabled = false; dirty = true; }
        if (importer.textureCompression != TextureImporterCompression.Uncompressed)
        {
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            dirty = true;
        }
        if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; dirty = true; }
        if (dirty) importer.SaveAndReimport();
    }

    /// <summary>既定アイコン(Unknown)とスタンドアロンの全サイズへ同じテクスチャを割り当てる。</summary>
    static void AssignPlayerIcons()
    {
        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
        if (icon == null)
        {
            Debug.LogWarning("AppIconGenerator: could not load " + IconPath);
            return;
        }

        // 既定アイコン(Unknown)は1スロット
        PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);

        // スタンドアロンは全サイズスロットへ同じ256pxテクスチャ(Unityが各サイズへ縮小する)
        var kind = IconKind.Application;
        int[] sizes = PlayerSettings.GetIconSizes(NamedBuildTarget.Standalone, kind);
        if (sizes == null || sizes.Length == 0)
        {
            kind = IconKind.Any;
            sizes = PlayerSettings.GetIconSizes(NamedBuildTarget.Standalone, kind);
        }
        if (sizes != null && sizes.Length > 0)
        {
            var icons = new Texture2D[sizes.Length];
            for (int i = 0; i < icons.Length; i++) icons[i] = icon;
            PlayerSettings.SetIcons(NamedBuildTarget.Standalone, icons, kind);
        }

        AssetDatabase.SaveAssets();
    }
}
