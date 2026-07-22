using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using HexCiv.Audio;
using HexCiv.Core;

namespace HexCiv.UI
{
    /// <summary>人口階層・需要・教育・満足度・移住を表示して社会重点を選ぶ独立Canvas UI。</summary>
    public sealed class PopulationPanel : MonoBehaviour
    {
        const int CityRows = 9;
        readonly Text[] cityRows = new Text[CityRows];
        readonly Button[] focusButtons = new Button[4];
        Canvas canvas;
        GameObject panel;
        CanvasGroup panelGroup;
        Text summaryText;
        Text explanationText;
        GameState shownState;
        int shownVersion = -1;
        bool modalNotified;
        Coroutine openAnimation;
        Sprite populationIcon;
        Texture2D populationTexture;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoSpawn()
        {
            if (FindFirstObjectByType<PopulationPanel>() != null) return;
            new GameObject("PopulationUI").AddComponent<PopulationPanel>();
        }

        void Start()
        {
            BuildCanvas();
            BuildOpenButton();
            BuildPanel();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F7))
            {
                if (panel != null && panel.activeSelf) Hide();
                else Show();
            }
            else if (panel != null && panel.activeSelf && Input.GetKeyDown(KeyCode.Escape)) Hide();

            if (panel == null || !panel.activeSelf) return;
            GameState current = CultureSystem.CurrentState;
            if (current != shownState || (current != null && current.Version != shownVersion)) Refresh();
        }

        void OnDestroy()
        {
            SetModalNotified(false);
            if (populationIcon != null) Destroy(populationIcon);
            if (populationTexture != null) Destroy(populationTexture);
        }

        void BuildCanvas()
        {
            var go = new GameObject("PopulationCanvas", typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 148;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("PopulationEventSystem", typeof(EventSystem),
                    typeof(StandaloneInputModule));
                eventSystem.transform.SetParent(transform, false);
            }
        }

        void BuildOpenButton()
        {
            Button button = UIStyle.CreateButton(canvas.transform, "PopulationButton", "人口社会  F7", 14, Show);
            UIStyle.SetRect(button.gameObject, Vector2.zero, Vector2.zero, Vector2.zero,
                new Vector2(978f, 176f), new Vector2(126f, 36f));
            populationIcon = BuildPopulationIcon();
            var iconGo = new GameObject("PopulationIcon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(button.transform, false);
            Image image = iconGo.GetComponent<Image>();
            image.sprite = populationIcon;
            image.preserveAspect = true;
            image.raycastTarget = false;
            UIStyle.SetRect(iconGo, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(6f, 0f), new Vector2(20f, 20f));
            Text label = UIStyle.ButtonLabel(button);
            if (label != null)
            {
                RectTransform rt = (RectTransform)label.transform;
                rt.offsetMin = new Vector2(28f, rt.offsetMin.y);
            }
            var nested = button.gameObject.AddComponent<Canvas>();
            nested.overrideSorting = true;
            nested.sortingOrder = -5;
            button.gameObject.AddComponent<GraphicRaycaster>();
        }

        void BuildPanel()
        {
            panel = UIStyle.CreatePanel(canvas.transform, "PopulationPanel",
                new Color(0.045f, 0.075f, 0.075f, 0.988f));
            panelGroup = panel.AddComponent<CanvasGroup>();
            UIStyle.SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 620f));

            GameObject header = UIStyle.CreatePanel(panel.transform, "Header",
                new Color(0.09f, 0.18f, 0.14f, 0.96f));
            UIStyle.SetRect(header, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(0f, 78f));
            Text title = UIStyle.CreateText(panel.transform, "Title", "人口と社会 — 階層・需要・教育・移住",
                24, TextAnchor.MiddleCenter, UIStyle.Accent);
            UIStyle.SetRect(title.gameObject, new Vector2(0f, 1f), Vector2.one,
                new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(-100f, 38f));
            Button close = UIStyle.CreateButton(panel.transform, "Close", "×", 18, Hide);
            UIStyle.SetRect(close.gameObject, Vector2.one, Vector2.one, Vector2.one,
                new Vector2(-10f, -10f), new Vector2(38f, 38f));

            summaryText = UIStyle.CreateText(panel.transform, "Summary", "", 16,
                TextAnchor.UpperLeft, UIStyle.TextMain);
            UIStyle.SetRect(summaryText.gameObject, new Vector2(0f, 1f), Vector2.one,
                new Vector2(0.5f, 1f), new Vector2(0f, -82f), new Vector2(-54f, 67f));
            Text focusLabel = UIStyle.CreateText(panel.transform, "FocusLabel", "社会重点", 15,
                TextAnchor.MiddleLeft, UIStyle.Accent);
            UIStyle.SetRect(focusLabel.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(26f, -153f), new Vector2(90f, 34f));
            focusButtons[0] = CreateFocusButton("Balanced", "均衡", 122f, SocialFocus.Balanced);
            focusButtons[1] = CreateFocusButton("Agriculture", "農業", 298f, SocialFocus.Agriculture);
            focusButtons[2] = CreateFocusButton("Crafts", "工芸", 474f, SocialFocus.Crafts);
            focusButtons[3] = CreateFocusButton("Learning", "学問", 650f, SocialFocus.Learning);

            explanationText = UIStyle.CreateText(panel.transform, "Explanation", "", 13,
                TextAnchor.UpperLeft, UIStyle.TextDim);
            UIStyle.SetRect(explanationText.gameObject, new Vector2(0f, 1f), Vector2.one,
                new Vector2(0.5f, 1f), new Vector2(0f, -198f), new Vector2(-54f, 42f));
            GameObject tableHeader = UIStyle.CreatePanel(panel.transform, "TableHeader", UIStyle.ButtonPressed);
            UIStyle.SetRect(tableHeader, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f),
                new Vector2(0f, -244f), new Vector2(-48f, 29f));
            Text headerText = UIStyle.CreateText(tableHeader.transform, "Text",
                "都市　　　 人口　農民　工人　学者　教育　満足　食料　住居　奉仕　移住", 13,
                TextAnchor.MiddleLeft, UIStyle.TextDim);
            UIStyle.StretchFull(headerText.gameObject, 7f);
            for (int i = 0; i < CityRows; i++)
            {
                GameObject row = UIStyle.CreatePanel(panel.transform, "CityRow" + i,
                    i % 2 == 0 ? UIStyle.PanelBgLight : new Color(0.08f, 0.15f, 0.13f, 0.94f));
                UIStyle.SetRect(row, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f),
                    new Vector2(0f, -277f - i * 32f), new Vector2(-48f, 29f));
                cityRows[i] = UIStyle.CreateText(row.transform, "Text", "", 13,
                    TextAnchor.MiddleLeft, UIStyle.TextMain);
                UIStyle.StretchFull(cityRows[i].gameObject, 7f);
            }
            Text help = UIStyle.CreateText(panel.transform, "Help",
                "農民=食料　工人=生産・税源　学者=科学・文化｜需要と安定が満足度を左右｜4ターンごとに魅力の高い都市へ移住｜F7で開閉",
                12, TextAnchor.MiddleCenter, UIStyle.TextDim);
            UIStyle.SetRect(help.gameObject, Vector2.zero, new Vector2(1f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(-38f, 24f));
            panel.SetActive(false);
        }

        Button CreateFocusButton(string name, string label, float x, SocialFocus focus)
        {
            Button button = UIStyle.CreateButton(panel.transform, name, label, 15, () => SelectFocus(focus));
            UIStyle.SetRect(button.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(x, -153f), new Vector2(156f, 36f));
            return button;
        }

        public void Show()
        {
            if (panel == null) return;
            bool opening = !panel.activeSelf;
            panel.SetActive(true);
            SetModalNotified(true);
            Refresh();
            if (!opening) return;
            GameAudio.Instance?.PlayPanelOpen();
            if (openAnimation != null) StopCoroutine(openAnimation);
            openAnimation = StartCoroutine(AnimateOpen());
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
            SetModalNotified(false);
        }

        void SelectFocus(SocialFocus focus)
        {
            GameState state = CultureSystem.CurrentState;
            Player player = state != null ? state.HumanPlayer : null;
            if (state == null || player == null || state.IsGameOver) return;
            PopulationSystem.SetFocus(state, player, focus);
            Refresh();
        }

        void Refresh()
        {
            shownState = CultureSystem.CurrentState;
            shownVersion = shownState != null ? shownState.Version : -1;
            Player focus = shownState != null ? shownState.HumanPlayer : null;
            if (focus == null && shownState != null)
                for (int i = 0; i < shownState.Players.Count; i++)
                    if (!shownState.Players[i].IsEliminated) { focus = shownState.Players[i]; break; }
            if (focus == null)
            {
                summaryText.text = "ゲーム開始後に人口社会が表示されます。";
                explanationText.text = "新しいゲームを開始してください。";
                SetFocusInteractable(false);
                ClearRows();
                return;
            }

            int population = 0, farmers = 0, artisans = 0, scholars = 0;
            for (int i = 0; i < focus.Cities.Count; i++)
            {
                City city = focus.Cities[i];
                population += city.Population;
                farmers += city.Farmers;
                artisans += city.Artisans;
                scholars += city.Scholars;
            }
            summaryText.text = $"{focus.NameJa}　人口 {population}（農民 {farmers} / 工人 {artisans} / 学者 {scholars}）　重点：{PopulationSystem.FocusNameJa(focus.SocialFocus)}\n" +
                $"平均教育 {PopulationSystem.AverageEducation(focus)}/100　平均満足 {PopulationSystem.AverageSatisfaction(focus)}/100　都市 {focus.Cities.Count}";
            explanationText.text = shownState.HumanPlayer == null
                ? "観戦モード：AIが食料・戦争・教育から社会重点を判断します（変更不可）。"
                : "重点変更で職能配分と産出が変化します。都市ごとの需要を整えると満足度と移住魅力が上昇します。";
            SetFocusInteractable(shownState.HumanPlayer == focus && !shownState.IsGameOver);
            for (int i = 0; i < focusButtons.Length; i++)
                SetFocusSelected(focusButtons[i], (int)focus.SocialFocus == i);

            int row = 0;
            for (; row < focus.Cities.Count && row < CityRows; row++)
            {
                City c = focus.Cities[row];
                string migration = c.LastNetMigration > 0 ? "+1" : c.LastNetMigration < 0 ? "-1" : " 0";
                cityRows[row].text = $"{c.NameJa,-11} {c.Population,4}　{c.Farmers,4}　{c.Artisans,4}　{c.Scholars,4}　{c.Education,3}　{c.Satisfaction,3}　{c.FoodNeedFulfillment,3}　{c.HousingNeedFulfillment,3}　{c.ServiceNeedFulfillment,3}　{migration}";
                cityRows[row].color = c.Satisfaction < 35 ? UIStyle.Danger : UIStyle.TextMain;
                cityRows[row].transform.parent.gameObject.SetActive(true);
            }
            for (; row < CityRows; row++) cityRows[row].transform.parent.gameObject.SetActive(false);
        }

        void ClearRows()
        {
            for (int i = 0; i < CityRows; i++) cityRows[i].transform.parent.gameObject.SetActive(false);
        }

        void SetFocusInteractable(bool value)
        {
            for (int i = 0; i < focusButtons.Length; i++) focusButtons[i].interactable = value;
        }

        static void SetFocusSelected(Button button, bool selected)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = selected ? new Color(0.18f, 0.48f, 0.33f, 1f) : UIStyle.ButtonNormal;
            colors.selectedColor = colors.normalColor;
            button.colors = colors;
        }

        IEnumerator AnimateOpen()
        {
            const float duration = 0.18f;
            float started = Time.unscaledTime;
            panelGroup.alpha = 0f;
            panel.transform.localPosition = new Vector3(0f, -28f, 0f);
            while (panel != null && panel.activeSelf)
            {
                float t = Mathf.Clamp01((Time.unscaledTime - started) / duration);
                float eased = 1f - (1f - t) * (1f - t);
                panelGroup.alpha = eased;
                panel.transform.localPosition = new Vector3(0f, Mathf.Lerp(-28f, 0f, eased), 0f);
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

        Sprite BuildPopulationIcon()
        {
            const int size = 32;
            populationTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "ProceduralPopulationIcon",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            var pixels = new Color32[size * size];
            Color32 clear = new Color32(0, 0, 0, 0);
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;
            DrawPerson(pixels, size, 7, new Color32(102, 205, 112, 255));
            DrawPerson(pixels, size, 16, new Color32(235, 178, 65, 255));
            DrawPerson(pixels, size, 25, new Color32(104, 177, 235, 255));
            populationTexture.SetPixels32(pixels);
            populationTexture.Apply(false, true);
            return Sprite.Create(populationTexture, new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f), size);
        }

        static void DrawPerson(Color32[] pixels, int size, int cx, Color32 color)
        {
            Fill(pixels, size, cx - 2, 21, cx + 2, 25, color);
            Fill(pixels, size, cx - 3, 10, cx + 3, 20, color);
            Fill(pixels, size, cx - 5, 8, cx + 5, 12, color);
            Fill(pixels, size, cx - 3, 4, cx - 1, 9, color);
            Fill(pixels, size, cx + 1, 4, cx + 3, 9, color);
        }

        static void Fill(Color32[] pixels, int size, int x0, int y0, int x1, int y1, Color32 color)
        {
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    if (x >= 0 && y >= 0 && x < size && y < size) pixels[y * size + x] = color;
        }
    }
}
