using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using HexCiv.Audio;
using HexCiv.Core;

namespace HexCiv.UI
{
    /// <summary>政治力・正統性・利害集団・法律を表示する独立Canvas UI。</summary>
    public sealed class PoliticsPanel : MonoBehaviour
    {
        readonly Text[] interestTexts = new Text[4];
        readonly Image[] interestBars = new Image[4];
        readonly Button[] lawButtons = new Button[4];
        Canvas canvas;
        GameObject panel;
        CanvasGroup panelGroup;
        Text summaryText;
        Text guidanceText;
        GameState shownState;
        int shownVersion = -1;
        bool modalNotified;
        Coroutine openAnimation;
        Sprite politicsIcon;
        Texture2D politicsTexture;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoSpawn()
        {
            if (FindFirstObjectByType<PoliticsPanel>() != null) return;
            new GameObject("PoliticsUI").AddComponent<PoliticsPanel>();
        }

        void Start()
        {
            BuildCanvas();
            BuildOpenButton();
            BuildPanel();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F6))
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
            if (politicsIcon != null) Destroy(politicsIcon);
            if (politicsTexture != null) Destroy(politicsTexture);
        }

        void BuildCanvas()
        {
            var go = new GameObject("PoliticsCanvas", typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 149;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("PoliticsEventSystem", typeof(EventSystem),
                    typeof(StandaloneInputModule));
                eventSystem.transform.SetParent(transform, false);
            }
        }

        void BuildOpenButton()
        {
            Button button = UIStyle.CreateButton(canvas.transform, "PoliticsButton", "政治  F6", 14, Show);
            UIStyle.SetRect(button.gameObject, Vector2.zero, Vector2.zero, Vector2.zero,
                new Vector2(1110f, 176f), new Vector2(126f, 36f));
            politicsIcon = BuildPoliticsIcon();
            var iconGo = new GameObject("PoliticsIcon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(button.transform, false);
            Image image = iconGo.GetComponent<Image>();
            image.sprite = politicsIcon;
            image.preserveAspect = true;
            image.raycastTarget = false;
            UIStyle.SetRect(iconGo, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(7f, 0f), new Vector2(20f, 20f));
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
            panel = UIStyle.CreatePanel(canvas.transform, "PoliticsPanel",
                new Color(0.065f, 0.06f, 0.09f, 0.99f));
            panelGroup = panel.AddComponent<CanvasGroup>();
            UIStyle.SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 620f));

            GameObject header = UIStyle.CreatePanel(panel.transform, "Header",
                new Color(0.17f, 0.12f, 0.23f, 0.97f));
            UIStyle.SetRect(header, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(0f, 78f));
            Text title = UIStyle.CreateText(panel.transform, "Title", "政治と法律 — 利害調整と正統性",
                24, TextAnchor.MiddleCenter, UIStyle.Accent);
            UIStyle.SetRect(title.gameObject, new Vector2(0f, 1f), Vector2.one,
                new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(-100f, 38f));
            Button close = UIStyle.CreateButton(panel.transform, "Close", "×", 18, Hide);
            UIStyle.SetRect(close.gameObject, Vector2.one, Vector2.one, Vector2.one,
                new Vector2(-10f, -10f), new Vector2(38f, 38f));

            summaryText = UIStyle.CreateText(panel.transform, "Summary", "", 16,
                TextAnchor.UpperLeft, UIStyle.TextMain);
            UIStyle.SetRect(summaryText.gameObject, new Vector2(0f, 1f), Vector2.one,
                new Vector2(0.5f, 1f), new Vector2(0f, -82f), new Vector2(-54f, 60f));

            Text interestTitle = UIStyle.CreateText(panel.transform, "InterestTitle", "利害集団の支持",
                16, TextAnchor.MiddleLeft, UIStyle.Accent);
            UIStyle.SetRect(interestTitle.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(28f, -145f), new Vector2(180f, 28f));
            for (int i = 0; i < 4; i++) BuildInterestRow(i);

            Text lawTitle = UIStyle.CreateText(panel.transform, "LawTitle",
                "基本法（変更費用：政治力" + PoliticalSystem.ChangeLawCost + "）", 16,
                TextAnchor.MiddleLeft, UIStyle.Accent);
            UIStyle.SetRect(lawTitle.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(28f, -306f), new Vector2(350f, 30f));
            for (int i = 0; i < 4; i++)
            {
                CivicLaw law = (CivicLaw)i;
                Button button = UIStyle.CreateButton(panel.transform, "Law" + i,
                    PoliticalSystem.LawNameJa(law) + "\n" + PoliticalSystem.LawEffectJa(law), 13,
                    () => SelectLaw(law));
                UIStyle.SetRect(button.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(0f, 1f), new Vector2(28f + i * 211f, -345f), new Vector2(196f, 94f));
                Text label = UIStyle.ButtonLabel(button);
                if (label != null)
                {
                    label.horizontalOverflow = HorizontalWrapMode.Wrap;
                    label.verticalOverflow = VerticalWrapMode.Truncate;
                    label.lineSpacing = 1.1f;
                }
                lawButtons[i] = button;
            }

            guidanceText = UIStyle.CreateText(panel.transform, "Guidance", "", 14,
                TextAnchor.UpperLeft, UIStyle.TextDim);
            UIStyle.SetRect(guidanceText.gameObject, new Vector2(0f, 1f), Vector2.one,
                new Vector2(0.5f, 1f), new Vector2(0f, -452f), new Vector2(-54f, 88f));
            Text help = UIStyle.CreateText(panel.transform, "Help",
                "支持は教育・職能・建物・国庫・戦争から毎ターン変化｜法律は文化・満足・税収・補給へ接続｜F6で開閉",
                12, TextAnchor.MiddleCenter, UIStyle.TextDim);
            UIStyle.SetRect(help.gameObject, Vector2.zero, new Vector2(1f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(-38f, 24f));
            panel.SetActive(false);
        }

        void BuildInterestRow(int index)
        {
            float x = index % 2 == 0 ? 28f : 456f;
            float y = index < 2 ? -178f : -238f;
            GameObject row = UIStyle.CreatePanel(panel.transform, "Interest" + index,
                index % 2 == 0 ? UIStyle.PanelBgLight : new Color(0.11f, 0.09f, 0.15f, 0.96f));
            UIStyle.SetRect(row, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(x, y), new Vector2(416f, 50f));
            interestTexts[index] = UIStyle.CreateText(row.transform, "Text", "", 14,
                TextAnchor.MiddleLeft, UIStyle.TextMain);
            UIStyle.SetRect(interestTexts[index].gameObject, Vector2.zero, Vector2.one,
                new Vector2(0.5f, 0.5f), new Vector2(-86f, 0f), new Vector2(-14f, 0f));
            GameObject barBg = UIStyle.CreatePanel(row.transform, "BarBg", new Color(0.03f, 0.04f, 0.06f, 1f));
            UIStyle.SetRect(barBg, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(150f, 14f));
            GameObject fill = UIStyle.CreatePanel(barBg.transform, "Fill", SupportColor(index));
            RectTransform fillRect = UIStyle.SetRect(fill, Vector2.zero, new Vector2(0f, 1f),
                new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(-2f, -2f);
            interestBars[index] = fill.GetComponent<Image>();
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

        void SelectLaw(CivicLaw law)
        {
            GameState state = CultureSystem.CurrentState;
            Player player = state != null ? state.HumanPlayer : null;
            if (state == null || player == null || state.IsGameOver) return;
            PoliticalSystem.SetLaw(state, player, law, true, true);
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
                summaryText.text = "ゲーム開始後に政治制度が表示されます。";
                guidanceText.text = "新しいゲームを開始してください。";
                SetLawInteractable(null, false);
                return;
            }

            summaryText.text = $"{focus.NameJa}　政治力 {focus.PoliticalCapital}/{PoliticalSystem.MaximumPoliticalCapital}　正統性 {focus.Legitimacy}/100\n" +
                $"現行法：{PoliticalSystem.LawNameJa(focus.ActiveLaw)}　効果：{PoliticalSystem.LawEffectJa(focus.ActiveLaw)}";
            int[] supports =
            {
                focus.ScholarSupport, focus.MerchantSupport,
                focus.TraditionalSupport, focus.MilitarySupport
            };
            for (int i = 0; i < supports.Length; i++)
            {
                int value = Mathf.Clamp(supports[i], 0, 100);
                interestTexts[i].text = PoliticalSystem.InterestNameJa(i) + "　" + value + "%";
                RectTransform bar = (RectTransform)interestBars[i].transform;
                bar.anchorMax = new Vector2(value / 100f, 1f);
                interestTexts[i].color = value < 30 ? UIStyle.Danger : UIStyle.TextMain;
            }

            bool editable = shownState.HumanPlayer == focus && !shownState.IsGameOver;
            SetLawInteractable(focus, editable);
            guidanceText.text = shownState.HumanPlayer == null
                ? "観戦モード：AIは戦争・国庫・教育から法律を選びます（変更不可）。"
                : focus.PoliticalCapital < PoliticalSystem.ChangeLawCost
                    ? $"次の法律変更まで政治力があと{PoliticalSystem.ChangeLawCost - focus.PoliticalCapital}必要です。都市と正統性が毎ターンの政治力を生みます。"
                    : "法律を選択できます。利害集団の支持と制度効果の両方を見て決定してください。";
        }

        void SetLawInteractable(Player focus, bool editable)
        {
            for (int i = 0; i < lawButtons.Length; i++)
            {
                CivicLaw law = (CivicLaw)i;
                bool current = focus != null && focus.ActiveLaw == law;
                lawButtons[i].interactable = editable && !current &&
                    focus.PoliticalCapital >= PoliticalSystem.ChangeLawCost;
                ColorBlock colors = lawButtons[i].colors;
                colors.normalColor = current ? new Color(0.42f, 0.31f, 0.12f, 1f) : UIStyle.ButtonNormal;
                colors.selectedColor = colors.normalColor;
                lawButtons[i].colors = colors;
            }
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

        Sprite BuildPoliticsIcon()
        {
            const int size = 32;
            politicsTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "ProceduralPoliticsIcon",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            var pixels = new Color32[size * size];
            Color32 clear = new Color32(0, 0, 0, 0);
            Color32 gold = new Color32(238, 196, 68, 255);
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;
            Fill(pixels, size, 15, 7, 17, 26, gold);
            Fill(pixels, size, 7, 23, 25, 25, gold);
            Fill(pixels, size, 6, 20, 11, 22, gold);
            Fill(pixels, size, 21, 20, 26, 22, gold);
            Fill(pixels, size, 5, 11, 12, 13, gold);
            Fill(pixels, size, 20, 11, 27, 13, gold);
            Fill(pixels, size, 8, 13, 9, 19, gold);
            Fill(pixels, size, 23, 13, 24, 19, gold);
            politicsTexture.SetPixels32(pixels);
            politicsTexture.Apply(false, true);
            return Sprite.Create(politicsTexture, new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f), size);
        }

        static Color SupportColor(int index)
        {
            return index switch
            {
                0 => new Color(0.30f, 0.65f, 0.92f, 1f),
                1 => new Color(0.93f, 0.69f, 0.25f, 1f),
                2 => new Color(0.55f, 0.78f, 0.38f, 1f),
                _ => new Color(0.86f, 0.31f, 0.29f, 1f),
            };
        }

        static void Fill(Color32[] pixels, int size, int x0, int y0, int x1, int y1, Color32 color)
        {
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    if (x >= 0 && y >= 0 && x < size && y < size) pixels[y * size + x] = color;
        }
    }
}
