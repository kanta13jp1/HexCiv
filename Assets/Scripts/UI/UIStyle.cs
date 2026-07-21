using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using HexCiv.Audio;

namespace HexCiv.UI
{
    /// <summary>
    /// uGUI共通スタイルと生成ヘルパー。すべてのテキストは JapaneseFont() を使うこと
    /// (組み込みの LegacyRuntime.ttf には日本語グリフが無いため)。
    /// </summary>
    public static class UIStyle
    {
        // ---- 配色 ----
        public static readonly Color PanelBg        = new Color(0.08f, 0.10f, 0.14f, 0.90f);
        public static readonly Color PanelBgLight   = new Color(0.13f, 0.16f, 0.21f, 0.94f);
        public static readonly Color ButtonNormal   = new Color(0.22f, 0.28f, 0.38f, 1f);
        public static readonly Color ButtonHover    = new Color(0.33f, 0.41f, 0.53f, 1f);
        public static readonly Color ButtonPressed  = new Color(0.15f, 0.19f, 0.27f, 1f);
        public static readonly Color ButtonDisabled = new Color(0.20f, 0.22f, 0.26f, 0.55f);
        public static readonly Color TextMain       = new Color(0.93f, 0.94f, 0.96f, 1f);
        public static readonly Color TextDim        = new Color(0.70f, 0.73f, 0.78f, 1f);
        public static readonly Color Accent         = new Color(0.95f, 0.83f, 0.35f, 1f);
        public static readonly Color Danger         = new Color(0.90f, 0.35f, 0.30f, 1f);

        static Font cachedFont;

        /// <summary>
        /// OSフォントから日本語フォントを生成(キャッシュ)。
        /// "Yu Gothic UI" → "Meiryo UI" → "Meiryo" → "MS Gothic" の順に試し、
        /// 最後の手段として LegacyRuntime.ttf を返す。
        /// </summary>
        public static Font JapaneseFont()
        {
            if (cachedFont != null) return cachedFont;

            string[] chain = { "Yu Gothic UI", "Meiryo UI", "Meiryo", "MS Gothic" };

            string[] installed = null;
            try { installed = Font.GetOSInstalledFontNames(); }
            catch { installed = null; }

            // インストール済み一覧に一致する名前を優先
            foreach (var name in chain)
            {
                try
                {
                    if (installed != null)
                    {
                        bool found = false;
                        for (int i = 0; i < installed.Length; i++)
                        {
                            if (string.Equals(installed[i], name, StringComparison.OrdinalIgnoreCase))
                            {
                                found = true;
                                break;
                            }
                        }
                        if (!found) continue;
                    }
                    var f = Font.CreateDynamicFontFromOSFont(name, 16);
                    if (f != null) { cachedFont = f; return cachedFont; }
                }
                catch { }
            }

            // 一覧に無くてもOS側フォールバックで表示できる場合があるため直接生成を試す
            foreach (var name in chain)
            {
                try
                {
                    var f = Font.CreateDynamicFontFromOSFont(name, 16);
                    if (f != null) { cachedFont = f; return cachedFont; }
                }
                catch { }
            }

            cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return cachedFont;
        }

        // ---- RectTransform 配置ヘルパー ----

        /// <summary>アンカー・ピボット・位置・サイズを一括設定する。</summary>
        public static RectTransform SetRect(GameObject go, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
            return rt;
        }

        /// <summary>親いっぱいにストレッチ(margin は内側余白)。</summary>
        public static RectTransform StretchFull(GameObject go, float margin = 0f)
        {
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(margin, margin);
            rt.offsetMax = new Vector2(-margin, -margin);
            return rt;
        }

        // ---- 要素生成ヘルパー ----

        /// <summary>背景Image付きパネルを生成する。</summary>
        public static GameObject CreatePanel(Transform parent, string name, Color bg)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = bg;
            return go;
        }

        /// <summary>RectTransformのみの空コンテナを生成する。</summary>
        public static GameObject CreateContainer(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        /// <summary>日本語フォントを使うTextを生成する(raycastTargetは無効)。</summary>
        public static Text CreateText(Transform parent, string name, string content,
            int fontSize, TextAnchor anchor, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = JapaneseFont();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>色付きImage+Textの単純ボタン(ホバー色は ColorBlock)。</summary>
        public static Button CreateButton(Transform parent, string name, string label,
            int fontSize, UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var img = go.GetComponent<Image>();
            img.color = Color.white;   // 実際の色は ColorBlock 側で乗算される

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            var cb = btn.colors;
            cb.normalColor = ButtonNormal;
            cb.highlightedColor = ButtonHover;
            cb.pressedColor = ButtonPressed;
            cb.selectedColor = ButtonNormal;
            cb.disabledColor = ButtonDisabled;
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0.08f;
            btn.colors = cb;
            btn.onClick.AddListener(() => GameAudio.Instance?.PlayUiClick());
            if (onClick != null) btn.onClick.AddListener(onClick);

            var text = CreateText(go.transform, "Label", label, fontSize, TextAnchor.MiddleCenter, TextMain);
            var trt = (RectTransform)text.transform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(4f, 1f);
            trt.offsetMax = new Vector2(-4f, -1f);
            return btn;
        }

        /// <summary>ボタン内のラベルTextを取得する。</summary>
        public static Text ButtonLabel(Button b)
        {
            return b != null ? b.GetComponentInChildren<Text>() : null;
        }

        // ---- ボタンアイコン(2026-07-21 Claude Code 追加) ----
        //
        // 小さな手続き描画アイコン(24pxテクスチャ・4倍スーパーサンプリングでアンチエイリアス)を
        // 実行時に一度だけ生成してキャッシュし、既存ボタンのラベル左側へ Image として追加する。
        // ボタンのサイズ・onClick・ColorBlock には一切触れない純表示の追加ヘルパー。

        /// <summary>種類別アイコンSpriteのキャッシュ(アプリ実行中は生成済みを使い回す)。</summary>
        static readonly Dictionary<string, Sprite> iconCache = new Dictionary<string, Sprite>();

        /// <summary>アイコンImageの子オブジェクト名(AddButtonIcon の二重付与チェックに使う)。</summary>
        const string ButtonIconName = "ButtonIcon";

        /// <summary>
        /// 種類名からアイコンSpriteを生成(キャッシュ)する。
        /// 種類: "book"(本/図鑑) "flag"(旗/文化・政策) "star"(星/遺産・偉人・作品)
        /// "save"(下向き矢印+受け皿) "load"(上向き矢印+受け皿)。未知の種類は null。
        /// </summary>
        public static Sprite CreateIconSprite(string kind)
        {
            if (string.IsNullOrEmpty(kind)) return null;
            Sprite cached;
            if (iconCache.TryGetValue(kind, out cached) && cached != null) return cached;
            var sprite = BuildIconSprite(kind);
            iconCache[kind] = sprite;
            return sprite;
        }

        /// <summary>
        /// 既存ボタンのラベル左側へアイコンImageを追加する(追加のみ・冪等)。
        /// ボタンのサイズ・位置・ハンドラは変更しない。ラベルの左オフセットだけを
        /// アイコン幅ぶん広げ、テキストとの重なりを防ぐ。既に付与済みなら何もしない。
        /// </summary>
        public static Image AddButtonIcon(Button button, string kind, float size = 20f, float leftPadding = 6f)
        {
            if (button == null) return null;
            var existing = button.transform.Find(ButtonIconName);
            if (existing != null) return existing.GetComponent<Image>();

            var sprite = CreateIconSprite(kind);
            if (sprite == null) return null;

            var go = new GameObject(ButtonIconName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(button.transform, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            img.preserveAspect = true;
            SetRect(go, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(leftPadding, 0f), new Vector2(size, size));

            var label = ButtonLabel(button);
            if (label != null)
            {
                var lrt = (RectTransform)label.transform;
                float minLeft = leftPadding + size + 2f;
                if (lrt.offsetMin.x < minLeft)
                    lrt.offsetMin = new Vector2(minLeft, lrt.offsetMin.y);
            }
            return img;
        }

        /// <summary>正規化座標(0..1、y上向き)のVector2省略記法。</summary>
        static Vector2 V(float x, float y) => new Vector2(x, y);

        /// <summary>アイコンを 24px(内部96pxで描いて4x4平均縮小=アンチエイリアス)で生成する。</summary>
        static Sprite BuildIconSprite(string kind)
        {
            const int size = 24;
            const int ss = 4;
            const int big = size * ss;
            var buf = new Color[big * big];   // 透明で初期化される

            var ivory = new Color(0.93f, 0.91f, 0.83f, 1f);
            var gold = Accent;

            switch (kind)
            {
                case "book":   // 開いた本(左右2ページ、中央の隙間が背)
                    FillPoly(buf, big, new[] { V(0.08f, 0.80f), V(0.48f, 0.68f), V(0.48f, 0.20f), V(0.08f, 0.32f) }, ivory);
                    FillPoly(buf, big, new[] { V(0.92f, 0.80f), V(0.52f, 0.68f), V(0.52f, 0.20f), V(0.92f, 0.32f) }, ivory);
                    break;
                case "flag":   // 旗(白い竿+燕尾の金の旗)
                    FillRect(buf, big, 0.16f, 0.08f, 0.23f, 0.92f, ivory);
                    FillPoly(buf, big, new[] { V(0.23f, 0.88f), V(0.90f, 0.88f), V(0.76f, 0.66f), V(0.90f, 0.44f), V(0.23f, 0.44f) }, gold);
                    break;
                case "star":   // 五芒星
                {
                    var star = new Vector2[10];
                    for (int k = 0; k < 10; k++)
                    {
                        float ang = (90f + k * 36f) * Mathf.Deg2Rad;
                        float r = (k % 2 == 0) ? 0.42f : 0.18f;
                        star[k] = V(0.5f + Mathf.Cos(ang) * r, 0.52f + Mathf.Sin(ang) * r);
                    }
                    FillPoly(buf, big, star, gold);
                    break;
                }
                case "save":   // 受け皿へ下向き矢印(保存)
                    FillRect(buf, big, 0.10f, 0.08f, 0.90f, 0.18f, ivory);
                    FillRect(buf, big, 0.10f, 0.08f, 0.20f, 0.40f, ivory);
                    FillRect(buf, big, 0.80f, 0.08f, 0.90f, 0.40f, ivory);
                    FillRect(buf, big, 0.42f, 0.52f, 0.58f, 0.94f, gold);
                    FillPoly(buf, big, new[] { V(0.26f, 0.54f), V(0.74f, 0.54f), V(0.50f, 0.26f) }, gold);
                    break;
                case "load":   // 受け皿から上向き矢印(読込)
                    FillRect(buf, big, 0.10f, 0.08f, 0.90f, 0.18f, ivory);
                    FillRect(buf, big, 0.10f, 0.08f, 0.20f, 0.40f, ivory);
                    FillRect(buf, big, 0.80f, 0.08f, 0.90f, 0.40f, ivory);
                    FillRect(buf, big, 0.42f, 0.26f, 0.58f, 0.66f, gold);
                    FillPoly(buf, big, new[] { V(0.26f, 0.64f), V(0.74f, 0.64f), V(0.50f, 0.94f) }, gold);
                    break;
                default:
                    return null;
            }

            // 4x4ブロックのアルファ加重平均で 24px へ縮小(縁が滑らかになる)
            var outPx = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float r = 0f, g = 0f, b = 0f, a = 0f;
                    for (int sy = 0; sy < ss; sy++)
                    {
                        int row = (y * ss + sy) * big + x * ss;
                        for (int sx = 0; sx < ss; sx++)
                        {
                            var c = buf[row + sx];
                            r += c.r * c.a; g += c.g * c.a; b += c.b * c.a; a += c.a;
                        }
                    }
                    outPx[y * size + x] = a > 0f
                        ? new Color(r / a, g / a, b / a, a / (ss * ss))
                        : Color.clear;
                }
            }

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = "ui_icon_" + kind;
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.SetPixels(outPx);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>正規化座標(0..1、y上向き)の軸平行矩形を塗る。</summary>
        static void FillRect(Color[] buf, int big, float x0, float y0, float x1, float y1, Color c)
        {
            int px0 = Mathf.Clamp(Mathf.FloorToInt(x0 * big), 0, big);
            int py0 = Mathf.Clamp(Mathf.FloorToInt(y0 * big), 0, big);
            int px1 = Mathf.Clamp(Mathf.CeilToInt(x1 * big), 0, big);
            int py1 = Mathf.Clamp(Mathf.CeilToInt(y1 * big), 0, big);
            for (int y = py0; y < py1; y++)
            {
                float v = (y + 0.5f) / big;
                if (v < y0 || v > y1) continue;
                for (int x = px0; x < px1; x++)
                {
                    float u = (x + 0.5f) / big;
                    if (u < x0 || u > x1) continue;
                    buf[y * big + x] = c;
                }
            }
        }

        /// <summary>正規化座標(0..1、y上向き)の多角形を塗る(交差数判定)。</summary>
        static void FillPoly(Color[] buf, int big, Vector2[] poly, Color c)
        {
            float minX = 1f, minY = 1f, maxX = 0f, maxY = 0f;
            for (int i = 0; i < poly.Length; i++)
            {
                if (poly[i].x < minX) minX = poly[i].x;
                if (poly[i].y < minY) minY = poly[i].y;
                if (poly[i].x > maxX) maxX = poly[i].x;
                if (poly[i].y > maxY) maxY = poly[i].y;
            }
            int px0 = Mathf.Clamp(Mathf.FloorToInt(minX * big), 0, big);
            int py0 = Mathf.Clamp(Mathf.FloorToInt(minY * big), 0, big);
            int px1 = Mathf.Clamp(Mathf.CeilToInt(maxX * big), 0, big);
            int py1 = Mathf.Clamp(Mathf.CeilToInt(maxY * big), 0, big);
            for (int y = py0; y < py1; y++)
            {
                float v = (y + 0.5f) / big;
                for (int x = px0; x < px1; x++)
                {
                    float u = (x + 0.5f) / big;
                    if (PointInPoly(poly, u, v)) buf[y * big + x] = c;
                }
            }
        }

        /// <summary>点が多角形の内側か(半直線との交差数の偶奇)。</summary>
        static bool PointInPoly(Vector2[] poly, float x, float y)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if ((poly[i].y > y) != (poly[j].y > y) &&
                    x < (poly[j].x - poly[i].x) * (y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                {
                    inside = !inside;
                }
            }
            return inside;
        }
    }
}
