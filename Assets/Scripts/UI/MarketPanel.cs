using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using HexCiv.Audio;
using HexCiv.Core;

namespace HexCiv.UI
{
    /// <summary>5財の在庫・価格・交易・地域産業と市場方針を表示する独立Canvas。</summary>
    public sealed class MarketPanel : MonoBehaviour
    {
        readonly Text[] goodRows = new Text[5];
        readonly Image[] goodMarks = new Image[5];
        readonly Button[] policyButtons = new Button[4];
        Canvas canvas;
        GameObject panel;
        CanvasGroup panelGroup;
        Text summaryText;
        Text industryText;
        Text comparisonText;
        GameState shownState;
        int shownVersion = -1;
        bool modalNotified;
        Coroutine openAnimation;
        Sprite marketIcon;
        Texture2D marketTexture;

        static readonly Color[] GoodColors =
        {
            new Color(0.45f, 0.78f, 0.32f, 1f),
            new Color(0.68f, 0.58f, 0.43f, 1f),
            new Color(0.91f, 0.64f, 0.22f, 1f),
            new Color(0.37f, 0.68f, 0.93f, 1f),
            new Color(0.42f, 0.84f, 0.82f, 1f),
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoSpawn()
        {
            if (FindFirstObjectByType<MarketPanel>() != null) return;
            new GameObject("MarketUI").AddComponent<MarketPanel>();
        }

        void Start()
        {
            BuildCanvas();
            BuildOpenButton();
            BuildPanel();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F4))
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
            if (marketIcon != null) Destroy(marketIcon);
            if (marketTexture != null) Destroy(marketTexture);
        }

        void BuildCanvas()
        {
            GameObject go = new GameObject("MarketCanvas", typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 150;
            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("MarketEventSystem", typeof(EventSystem),
                    typeof(StandaloneInputModule));
                eventSystem.transform.SetParent(transform, false);
            }
        }

        void BuildOpenButton()
        {
            Button button = UIStyle.CreateButton(canvas.transform, "MarketButton", "市場  F4", 14, Show);
            UIStyle.SetRect(button.gameObject, Vector2.zero, Vector2.zero, Vector2.zero,
                new Vector2(1110f, 132f), new Vector2(126f, 36f));
            marketIcon = BuildMarketIcon();
            GameObject iconGo = new GameObject("MarketIcon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(button.transform, false);
            Image image = iconGo.GetComponent<Image>();
            image.sprite = marketIcon;
            image.preserveAspect = true;
            image.raycastTarget = false;
            UIStyle.SetRect(iconGo, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(7f, 0f), new Vector2(20f, 20f));
            Text label = UIStyle.ButtonLabel(button);
            if (label != null) ((RectTransform)label.transform).offsetMin = new Vector2(28f, 0f);
            Canvas nested = button.gameObject.AddComponent<Canvas>();
            nested.overrideSorting = true;
            nested.sortingOrder = -5;
            button.gameObject.AddComponent<GraphicRaycaster>();
        }

        void BuildPanel()
        {
            panel = UIStyle.CreatePanel(canvas.transform, "MarketPanel",
                new Color(0.045f, 0.07f, 0.075f, 0.99f));
            panelGroup = panel.AddComponent<CanvasGroup>();
            UIStyle.SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(940f, 630f));

            GameObject header = UIStyle.CreatePanel(panel.transform, "Header",
                new Color(0.08f, 0.19f, 0.18f, 0.98f));
            UIStyle.SetRect(header, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(0f, 72f));
            Text title = UIStyle.CreateText(panel.transform, "Title",
                "市場・交易・地域産業", 24, TextAnchor.MiddleCenter, UIStyle.Accent);
            UIStyle.SetRect(title.gameObject, new Vector2(0f, 1f), Vector2.one,
                new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(-100f, 38f));
            Button close = UIStyle.CreateButton(panel.transform, "Close", "×", 18, Hide);
            UIStyle.SetRect(close.gameObject, Vector2.one, Vector2.one, Vector2.one,
                new Vector2(-10f, -10f), new Vector2(38f, 38f));

            summaryText = UIStyle.CreateText(panel.transform, "Summary", "", 15,
                TextAnchor.UpperLeft, UIStyle.TextMain);
            UIStyle.SetRect(summaryText.gameObject, new Vector2(0f, 1f), Vector2.one,
                new Vector2(0.5f, 1f), new Vector2(0f, -76f), new Vector2(-54f, 58f));

            Text policyTitle = UIStyle.CreateText(panel.transform, "PolicyTitle", "市場方針", 15,
                TextAnchor.MiddleLeft, UIStyle.Accent);
            UIStyle.SetRect(policyTitle.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(28f, -140f), new Vector2(100f, 34f));
            for (int i = 0; i < 4; i++)
            {
                EconomicPolicy policy = (EconomicPolicy)i;
                Button button = UIStyle.CreateButton(panel.transform, "Policy" + i,
                    MarketSystem.PolicyNameJa(policy) + "\n" + MarketSystem.PolicyEffectJa(policy), 12,
                    () => SelectPolicy(policy));
                UIStyle.SetRect(button.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(0f, 1f), new Vector2(132f + i * 194f, -140f), new Vector2(180f, 58f));
                Text label = UIStyle.ButtonLabel(button);
                if (label != null)
                {
                    label.horizontalOverflow = HorizontalWrapMode.Wrap;
                    label.verticalOverflow = VerticalWrapMode.Truncate;
                    label.lineSpacing = 0.92f;
                }
                policyButtons[i] = button;
            }

            GameObject goodsHeader = UIStyle.CreatePanel(panel.transform, "GoodsHeader", UIStyle.ButtonPressed);
            UIStyle.SetRect(goodsHeader, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -210f), new Vector2(-54f, 28f));
            Text goodsHeaderText = UIStyle.CreateText(goodsHeader.transform, "Text",
                "財　　　　在庫　 生産　需要　価格　需給", 13, TextAnchor.MiddleLeft, UIStyle.TextDim);
            UIStyle.StretchFull(goodsHeaderText.gameObject, 34f);
            for (int i = 0; i < 5; i++) BuildGoodRow(i);

            Text industryTitle = UIStyle.CreateText(panel.transform, "IndustryTitle", "地域産業・生活技術", 15,
                TextAnchor.MiddleLeft, UIStyle.Accent);
            UIStyle.SetRect(industryTitle.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(28f, -414f), new Vector2(210f, 26f));
            industryText = UIStyle.CreateText(panel.transform, "Industry", "", 13,
                TextAnchor.UpperLeft, UIStyle.TextMain);
            UIStyle.SetRect(industryText.gameObject, new Vector2(0f, 1f), Vector2.one,
                new Vector2(0.5f, 1f), new Vector2(0f, -442f), new Vector2(-54f, 55f));

            Text comparisonTitle = UIStyle.CreateText(panel.transform, "ComparisonTitle", "世界市場", 15,
                TextAnchor.MiddleLeft, UIStyle.Accent);
            UIStyle.SetRect(comparisonTitle.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(28f, -502f), new Vector2(100f, 26f));
            comparisonText = UIStyle.CreateText(panel.transform, "Comparison", "", 12,
                TextAnchor.UpperLeft, UIStyle.TextDim);
            UIStyle.SetRect(comparisonText.gameObject, new Vector2(0f, 1f), Vector2.one,
                new Vector2(0.5f, 1f), new Vector2(0f, -530f), new Vector2(-54f, 66f));

            Text help = UIStyle.CreateText(panel.transform, "Help",
                "余剰と不足から平時交易を自動計算｜戦争は相互交易を遮断｜F4で開閉", 12,
                TextAnchor.MiddleCenter, UIStyle.TextDim);
            UIStyle.SetRect(help.gameObject, Vector2.zero, new Vector2(1f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(-40f, 22f));
            panel.SetActive(false);
        }

        void BuildGoodRow(int index)
        {
            GameObject row = UIStyle.CreatePanel(panel.transform, "Good" + index,
                index % 2 == 0 ? UIStyle.PanelBgLight : new Color(0.08f, 0.13f, 0.14f, 0.96f));
            UIStyle.SetRect(row, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -241f - index * 33f), new Vector2(-54f, 29f));
            GameObject mark = UIStyle.CreatePanel(row.transform, "Mark", GoodColors[index]);
            UIStyle.SetRect(mark, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(12f, 12f));
            goodMarks[index] = mark.GetComponent<Image>();
            goodRows[index] = UIStyle.CreateText(row.transform, "Text", "", 13,
                TextAnchor.MiddleLeft, UIStyle.TextMain);
            UIStyle.StretchFull(goodRows[index].gameObject, 34f);
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

        void SelectPolicy(EconomicPolicy policy)
        {
            GameState state = CultureSystem.CurrentState;
            Player player = state != null ? state.HumanPlayer : null;
            if (state == null || player == null || state.IsGameOver) return;
            MarketSystem.SetPolicy(state, player, policy, true);
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
                summaryText.text = "ゲーム開始後に市場が表示されます。";
                industryText.text = "地域産業は技術進歩に応じて発展します。";
                comparisonText.text = "新しいゲームを開始してください。";
                SetPolicyInteractable(null, false);
                for (int i = 0; i < goodRows.Length; i++) goodRows[i].text = "";
                return;
            }

            Player partner = shownState.GetPlayer(focus.LastTradePartnerId);
            string partnerName = partner != null ? partner.NameJa : "なし";
            string featured = "なし";
            MaterialCultureDef featuredDef = MaterialCultureCatalog.Find(focus.FeaturedIndustryId);
            if (featuredDef != null) featured = featuredDef.NameJa;
            summaryText.text = $"{focus.NameJa}　市場アクセス {focus.MarketAccess}%　需要充足 {focus.DemandFulfillment}%　" +
                $"方針：{MarketSystem.PolicyNameJa(focus.EconomicPolicy)}\n" +
                $"輸入 {focus.LastImports}　輸出 {focus.LastExports}　交易収支 {Signed(focus.LastTradeBalance)}　" +
                $"主要相手：{partnerName}　注力産業：{featured}";

            for (int i = 0; i < goodRows.Length; i++)
            {
                MarketGood good = (MarketGood)i;
                int production = MarketSystem.Production(focus, good);
                int demand = MarketSystem.Demand(focus, good);
                int stock = MarketSystem.GetStock(focus, good);
                int price = MarketSystem.GetPrice(focus, good);
                string balance = production >= demand ? "余剰" : "不足";
                goodRows[i].text = $"{MarketSystem.GoodNameJa(good),-6}　{stock,4}　+{production,3}　{demand,4}　¤{price,2}　{balance}";
                goodMarks[i].color = GoodColors[i];
            }

            var names = new List<string>();
            for (int i = 0; i < MaterialCultureCatalog.All.Count; i++)
            {
                MaterialCultureDef item = MaterialCultureCatalog.All[i];
                if (focus.DevelopedMaterialCultures.Contains(item.Id)) names.Add(item.NameJa);
            }
            industryText.text = names.Count == 0
                ? "未発展。最初の地域産業は次の市場更新で登録されます。"
                : $"発展済み {names.Count}/12：{string.Join("、", names)}";

            var lines = new List<string>();
            for (int i = 0; i < shownState.Players.Count && lines.Count < 8; i++)
            {
                Player player = shownState.Players[i];
                if (player.IsEliminated) continue;
                lines.Add($"{player.NameJa}  接続{player.MarketAccess}%  充足{player.DemandFulfillment}%  " +
                    $"輸{player.LastExports}/入{player.LastImports}  収支{Signed(player.LastTradeBalance)}");
            }
            comparisonText.text = string.Join("　｜　", lines);
            SetPolicyInteractable(focus, shownState.HumanPlayer == focus && !shownState.IsGameOver);
        }

        void SetPolicyInteractable(Player player, bool editable)
        {
            for (int i = 0; i < policyButtons.Length; i++)
            {
                EconomicPolicy policy = (EconomicPolicy)i;
                bool current = player != null && MarketSystem.NormalizePolicy(player.EconomicPolicy) == policy;
                policyButtons[i].interactable = editable && !current;
                ColorBlock colors = policyButtons[i].colors;
                colors.normalColor = current ? new Color(0.17f, 0.42f, 0.36f, 1f) : UIStyle.ButtonNormal;
                colors.selectedColor = colors.normalColor;
                policyButtons[i].colors = colors;
            }
        }

        IEnumerator AnimateOpen()
        {
            const float duration = 0.18f;
            float started = Time.unscaledTime;
            panelGroup.alpha = 0f;
            panel.transform.localPosition = new Vector3(32f, 0f, 0f);
            while (panel != null && panel.activeSelf)
            {
                float t = Mathf.Clamp01((Time.unscaledTime - started) / duration);
                float eased = 1f - (1f - t) * (1f - t);
                panelGroup.alpha = eased;
                panel.transform.localPosition = new Vector3(Mathf.Lerp(32f, 0f, eased), 0f, 0f);
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

        Sprite BuildMarketIcon()
        {
            const int size = 32;
            marketTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "ProceduralMarketIcon",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            Color32[] pixels = new Color32[size * size];
            Color32 clear = new Color32(0, 0, 0, 0);
            Color32 gold = new Color32(239, 195, 66, 255);
            Color32 teal = new Color32(82, 202, 190, 255);
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;
            Fill(pixels, size, 9, 10, 22, 22, gold);
            Fill(pixels, size, 11, 12, 20, 20, new Color32(82, 77, 51, 255));
            Fill(pixels, size, 4, 6, 16, 8, teal);
            Fill(pixels, size, 4, 6, 7, 11, teal);
            Fill(pixels, size, 15, 5, 18, 9, teal);
            Fill(pixels, size, 15, 24, 27, 26, teal);
            Fill(pixels, size, 24, 21, 27, 26, teal);
            Fill(pixels, size, 13, 23, 16, 27, teal);
            marketTexture.SetPixels32(pixels);
            marketTexture.Apply(false, true);
            return Sprite.Create(marketTexture, new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f), size);
        }

        static string Signed(int value) => value > 0 ? "+" + value : value.ToString();

        static void Fill(Color32[] pixels, int size, int x0, int y0, int x1, int y1, Color32 color)
        {
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    if (x >= 0 && y >= 0 && x < size && y < size) pixels[y * size + x] = color;
        }
    }
}
