using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using HexCiv.Audio;
using HexCiv.Core;

namespace HexCiv.UI
{
    /// <summary>文明とユニットの補給状態を表示する独立Canvas UI。</summary>
    public sealed class LogisticsPanel : MonoBehaviour
    {
        const int CivilizationRows = 8;
        const int UnitRows = 10;

        Canvas canvas;
        GameObject panel;
        CanvasGroup panelGroup;
        Text summaryText;
        Text detailText;
        readonly Text[] civilizationRows = new Text[CivilizationRows];
        readonly Text[] unitRows = new Text[UnitRows];
        GameState shownState;
        int shownVersion = -1;
        bool modalNotified;
        Coroutine openAnimation;
        Sprite supplyIcon;
        Texture2D supplyTexture;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoSpawn()
        {
            if (FindFirstObjectByType<LogisticsPanel>() != null) return;
            new GameObject("LogisticsUI").AddComponent<LogisticsPanel>();
        }

        void Start()
        {
            BuildCanvas();
            BuildOpenButton();
            BuildPanel();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F10))
            {
                if (panel != null && panel.activeSelf) Hide();
                else Show();
            }
            else if (panel != null && panel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            {
                Hide();
            }

            if (panel == null || !panel.activeSelf) return;
            GameState current = CultureSystem.CurrentState;
            if (current != shownState || (current != null && current.Version != shownVersion))
                Refresh();
        }

        void OnDestroy()
        {
            SetModalNotified(false);
            if (supplyIcon != null) Destroy(supplyIcon);
            if (supplyTexture != null) Destroy(supplyTexture);
        }

        void BuildCanvas()
        {
            var go = new GameObject("LogisticsCanvas", typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 146;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("LogisticsEventSystem", typeof(EventSystem),
                    typeof(StandaloneInputModule));
                eventSystem.transform.SetParent(transform, false);
            }
        }

        void BuildOpenButton()
        {
            var button = UIStyle.CreateButton(canvas.transform, "LogisticsButton", "兵站  F10", 14, Show);
            UIStyle.SetRect(button.gameObject, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(846f, 176f), new Vector2(126f, 36f));

            supplyIcon = BuildSupplyIcon();
            var iconGo = new GameObject("SupplyIcon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(button.transform, false);
            var image = iconGo.GetComponent<Image>();
            image.sprite = supplyIcon;
            image.preserveAspect = true;
            image.raycastTarget = false;
            UIStyle.SetRect(iconGo, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(6f, 0f), new Vector2(20f, 20f));
            Text label = UIStyle.ButtonLabel(button);
            if (label != null)
            {
                var rt = (RectTransform)label.transform;
                rt.offsetMin = new Vector2(28f, rt.offsetMin.y);
            }

            var nested = button.gameObject.AddComponent<Canvas>();
            nested.overrideSorting = true;
            nested.sortingOrder = -5;
            button.gameObject.AddComponent<GraphicRaycaster>();
        }

        void BuildPanel()
        {
            panel = UIStyle.CreatePanel(canvas.transform, "LogisticsPanel",
                new Color(0.045f, 0.065f, 0.085f, 0.988f));
            panelGroup = panel.AddComponent<CanvasGroup>();
            UIStyle.SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(850f, 610f));

            var header = UIStyle.CreatePanel(panel.transform, "Header",
                new Color(0.08f, 0.16f, 0.19f, 0.96f));
            UIStyle.SetRect(header, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 82f));
            var route = UIStyle.CreateText(header.transform, "RouteDecoration",
                "●━━━━●━━━━◇━━━━!", 17, TextAnchor.LowerCenter,
                new Color(0.42f, 0.78f, 0.72f, 0.72f));
            UIStyle.SetRect(route.gameObject, new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(-100f, 28f));

            var title = UIStyle.CreateText(panel.transform, "Title", "兵站 — 補給線・孤立・消耗", 24,
                TextAnchor.MiddleCenter, UIStyle.Accent);
            UIStyle.SetRect(title.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(-100f, 38f));
            var close = UIStyle.CreateButton(panel.transform, "Close", "×", 18, Hide);
            UIStyle.SetRect(close.gameObject, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-10f, -10f), new Vector2(38f, 38f));

            summaryText = UIStyle.CreateText(panel.transform, "Summary", "", 16,
                TextAnchor.UpperLeft, UIStyle.TextMain);
            UIStyle.SetRect(summaryText.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -88f), new Vector2(-52f, 62f));
            detailText = UIStyle.CreateText(panel.transform, "Detail", "", 13,
                TextAnchor.UpperLeft, UIStyle.TextDim);
            UIStyle.SetRect(detailText.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -151f), new Vector2(-52f, 43f));

            var civHeader = UIStyle.CreatePanel(panel.transform, "CivilizationHeader", UIStyle.ButtonPressed);
            UIStyle.SetRect(civHeader, new Vector2(0f, 1f), new Vector2(0.47f, 1f),
                new Vector2(0.5f, 1f), new Vector2(18f, -202f), new Vector2(-18f, 28f));
            var civHeaderText = UIStyle.CreateText(civHeader.transform, "Text",
                "文明　　　　　　 良好　逼迫　孤立", 13, TextAnchor.MiddleLeft, UIStyle.TextDim);
            UIStyle.StretchFull(civHeaderText.gameObject, 7f);

            var unitHeader = UIStyle.CreatePanel(panel.transform, "UnitHeader", UIStyle.ButtonPressed);
            UIStyle.SetRect(unitHeader, new Vector2(0.47f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(-18f, -202f), new Vector2(-18f, 28f));
            var unitHeaderText = UIStyle.CreateText(unitHeader.transform, "Text",
                "部隊　　　　 HP　補給状態　孤立T", 13, TextAnchor.MiddleLeft, UIStyle.TextDim);
            UIStyle.StretchFull(unitHeaderText.gameObject, 7f);

            for (int i = 0; i < CivilizationRows; i++)
            {
                var row = UIStyle.CreatePanel(panel.transform, "CivilizationRow" + i,
                    i % 2 == 0 ? UIStyle.PanelBgLight : new Color(0.10f, 0.13f, 0.16f, 0.94f));
                UIStyle.SetRect(row, new Vector2(0f, 1f), new Vector2(0.47f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(18f, -234f - i * 32f), new Vector2(-18f, 28f));
                civilizationRows[i] = UIStyle.CreateText(row.transform, "Text", "", 13,
                    TextAnchor.MiddleLeft, UIStyle.TextMain);
                UIStyle.StretchFull(civilizationRows[i].gameObject, 7f);
            }

            for (int i = 0; i < UnitRows; i++)
            {
                var row = UIStyle.CreatePanel(panel.transform, "UnitRow" + i,
                    i % 2 == 0 ? UIStyle.PanelBgLight : new Color(0.10f, 0.13f, 0.16f, 0.94f));
                UIStyle.SetRect(row, new Vector2(0.47f, 1f), new Vector2(1f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(-18f, -234f - i * 32f), new Vector2(-18f, 28f));
                unitRows[i] = UIStyle.CreateText(row.transform, "Text", "", 13,
                    TextAnchor.MiddleLeft, UIStyle.TextMain);
                UIStyle.StretchFull(unitRows[i].gameObject, 7f);
            }

            var help = UIStyle.CreateText(panel.transform, "Help",
                "敵部隊・敵都市は補給を遮断｜穀物庫は拠点強化｜車輪・建築学は到達距離を延長｜F10で開閉",
                12, TextAnchor.MiddleCenter, UIStyle.TextDim);
            UIStyle.SetRect(help.gameObject, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(-40f, 24f));
            panel.SetActive(false);
        }

        public void Show()
        {
            if (panel == null) return;
            bool opening = !panel.activeSelf;
            panel.SetActive(true);
            SetModalNotified(true);
            Refresh();
            if (opening)
            {
                GameAudio.Instance?.PlayPanelOpen();
                if (openAnimation != null) StopCoroutine(openAnimation);
                openAnimation = StartCoroutine(AnimateOpen());
            }
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
            SetModalNotified(false);
        }

        void Refresh()
        {
            shownState = CultureSystem.CurrentState;
            shownVersion = shownState != null ? shownState.Version : -1;
            if (shownState == null)
            {
                summaryText.text = "ゲーム開始後に補給網が表示されます。";
                detailText.text = "新しいゲームを開始してください。";
                ClearRows();
                return;
            }

            Player focus = shownState.HumanPlayer;
            if (focus == null)
            {
                for (int i = 0; i < shownState.Players.Count; i++)
                    if (!shownState.Players[i].IsEliminated) { focus = shownState.Players[i]; break; }
            }
            if (focus == null)
            {
                summaryText.text = "存続している文明がありません。";
                detailText.text = "";
                ClearRows();
                return;
            }

            LogisticsSystem.CountLevels(focus, out int good, out int strained, out int isolated);
            int depots = 0;
            for (int i = 0; i < focus.Cities.Count; i++)
                if (focus.Cities[i].Buildings.Contains("granary")) depots++;
            summaryText.text = $"{focus.NameJa}　補給距離 {LogisticsSystem.SupplyRange(focus)}　補給拠点 {focus.Cities.Count}（強化 {depots}）\n" +
                $"部隊 {focus.Units.Count}　補給良好 {good}　逼迫 {strained}　孤立 {isolated}";
            detailText.text = "逼迫：回復半減・戦闘力90%　｜　孤立：回復不能・移動-1・戦闘力75%・2ターン目からHP-5";

            int row = 0;
            for (int i = 0; i < shownState.Players.Count && row < CivilizationRows; i++)
            {
                Player p = shownState.Players[i];
                if (p.IsEliminated) continue;
                LogisticsSystem.CountLevels(p, out int s, out int t, out int x);
                civilizationRows[row].text = $"{p.NameJa,-16} {s,4}　{t,4}　{x,4}";
                civilizationRows[row].color = p == focus ? UIStyle.Accent : UIStyle.TextMain;
                civilizationRows[row].transform.parent.gameObject.SetActive(true);
                row++;
            }
            for (; row < CivilizationRows; row++)
                civilizationRows[row].transform.parent.gameObject.SetActive(false);

            var units = new List<Unit>();
            for (int i = 0; i < focus.Units.Count; i++)
                if (focus.Units[i] != null && !focus.Units[i].IsDead) units.Add(focus.Units[i]);
            units.Sort((a, b) =>
            {
                int cmp = ((int)b.Supply).CompareTo((int)a.Supply);
                if (cmp != 0) return cmp;
                cmp = b.TurnsOutOfSupply.CompareTo(a.TurnsOutOfSupply);
                return cmp != 0 ? cmp : a.Id.CompareTo(b.Id);
            });
            row = 0;
            for (; row < units.Count && row < UnitRows; row++)
            {
                Unit u = units[row];
                unitRows[row].text = $"{u.Def.NameJa,-10} {u.Hp,3}　{LogisticsSystem.LevelNameJa(u.Supply),-6}　{u.TurnsOutOfSupply,3}";
                unitRows[row].color = u.Supply == SupplyLevel.Isolated
                    ? new Color(1f, 0.40f, 0.30f)
                    : u.Supply == SupplyLevel.Strained
                        ? new Color(1f, 0.75f, 0.28f)
                        : new Color(0.48f, 0.90f, 0.68f);
                unitRows[row].transform.parent.gameObject.SetActive(true);
            }
            for (; row < UnitRows; row++) unitRows[row].transform.parent.gameObject.SetActive(false);
        }

        void ClearRows()
        {
            for (int i = 0; i < CivilizationRows; i++)
                civilizationRows[i].transform.parent.gameObject.SetActive(false);
            for (int i = 0; i < UnitRows; i++)
                unitRows[i].transform.parent.gameObject.SetActive(false);
        }

        IEnumerator AnimateOpen()
        {
            const float duration = 0.18f;
            float started = Time.unscaledTime;
            panelGroup.alpha = 0f;
            panel.transform.localPosition = new Vector3(24f, 0f, 0f);
            while (panel != null && panel.activeSelf)
            {
                float t = Mathf.Clamp01((Time.unscaledTime - started) / duration);
                float eased = 1f - (1f - t) * (1f - t);
                panelGroup.alpha = eased;
                panel.transform.localPosition = new Vector3(Mathf.Lerp(24f, 0f, eased), 0f, 0f);
                if (t >= 1f) break;
                yield return null;
            }
            openAnimation = null;
        }

        void SetModalNotified(bool open)
        {
            if (modalNotified == open) return;
            modalNotified = open;
            UIManager.NotifyExternalPanel(open);
        }

        Sprite BuildSupplyIcon()
        {
            const int size = 32;
            supplyTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "ProceduralSupplyIcon",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            var pixels = new Color32[size * size];
            var clear = new Color32(0, 0, 0, 0);
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;
            var teal = new Color32(94, 220, 194, 255);
            var dark = new Color32(25, 105, 99, 255);
            var gold = new Color32(245, 202, 74, 255);
            Fill(pixels, size, 4, 9, 18, 23, dark);
            Fill(pixels, size, 6, 11, 16, 21, teal);
            Fill(pixels, size, 5, 8, 17, 10, gold);
            Fill(pixels, size, 10, 11, 12, 21, dark);
            Fill(pixels, size, 19, 14, 27, 16, gold);
            Fill(pixels, size, 24, 11, 27, 19, gold);
            Fill(pixels, size, 27, 13, 29, 17, gold);
            supplyTexture.SetPixels32(pixels);
            supplyTexture.Apply(false, true);
            return Sprite.Create(supplyTexture, new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f), size);
        }

        static void Fill(Color32[] pixels, int size, int x0, int y0, int x1, int y1, Color32 color)
        {
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    if (x >= 0 && y >= 0 && x < size && y < size)
                        pixels[y * size + x] = color;
        }
    }
}
