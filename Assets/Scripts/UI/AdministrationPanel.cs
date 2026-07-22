using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using HexCiv.Audio;
using HexCiv.Core;

namespace HexCiv.UI
{
    /// <summary>
    /// 国庫・税制・安定度・戦争疲弊を表示、操作する独立Canvas UI。
    /// RuntimeInitializeで自己起動し、既存のUIManager構築コードを変更しない。
    /// </summary>
    public sealed class AdministrationPanel : MonoBehaviour
    {
        const int PlayerRows = 8;

        Canvas canvas;
        GameObject panel;
        CanvasGroup panelGroup;
        Text summaryText;
        Text explanationText;
        readonly Text[] playerRows = new Text[PlayerRows];
        Button lowButton;
        Button balancedButton;
        Button highButton;
        GameState shownState;
        int shownVersion = -1;
        bool modalNotified;
        Coroutine openAnimation;
        Sprite treasuryIcon;
        Texture2D treasuryTexture;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoSpawn()
        {
            if (FindFirstObjectByType<AdministrationPanel>() != null) return;
            new GameObject("AdministrationUI").AddComponent<AdministrationPanel>();
        }

        void Start()
        {
            BuildCanvas();
            BuildOpenButton();
            BuildPanel();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F8))
            {
                if (panel != null && panel.activeSelf) Hide();
                else Show();
            }
            else if (panel != null && panel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            {
                Hide();
            }

            if (panel == null || !panel.activeSelf) return;
            var current = CultureSystem.CurrentState;
            if (current != shownState || (current != null && current.Version != shownVersion))
                Refresh();
        }

        void OnDestroy()
        {
            SetModalNotified(false);
            if (treasuryIcon != null) Destroy(treasuryIcon);
            if (treasuryTexture != null) Destroy(treasuryTexture);
        }

        void BuildCanvas()
        {
            var go = new GameObject("AdministrationCanvas", typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 147;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("AdministrationEventSystem", typeof(EventSystem),
                    typeof(StandaloneInputModule));
                eventSystem.transform.SetParent(transform, false);
            }
        }

        void BuildOpenButton()
        {
            var button = UIStyle.CreateButton(canvas.transform, "AdministrationButton", "国家運営", 14, Show);
            UIStyle.SetRect(button.gameObject, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(714f, 176f), new Vector2(126f, 36f));

            treasuryIcon = BuildTreasuryIcon();
            var iconGo = new GameObject("TreasuryIcon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(button.transform, false);
            var image = iconGo.GetComponent<Image>();
            image.sprite = treasuryIcon;
            image.preserveAspect = true;
            image.raycastTarget = false;
            UIStyle.SetRect(iconGo, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(6f, 0f), new Vector2(20f, 20f));
            var label = UIStyle.ButtonLabel(button);
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
            panel = UIStyle.CreatePanel(canvas.transform, "AdministrationPanel",
                new Color(0.055f, 0.07f, 0.105f, 0.985f));
            panelGroup = panel.AddComponent<CanvasGroup>();
            UIStyle.SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800f, 560f));

            var bannerTexture = Resources.Load<Texture2D>("Administration/administration_banner");
            if (bannerTexture != null)
            {
                var bannerGo = new GameObject("AdministrationBanner", typeof(RectTransform), typeof(RawImage));
                bannerGo.transform.SetParent(panel.transform, false);
                var banner = bannerGo.GetComponent<RawImage>();
                banner.texture = bannerTexture;
                banner.uvRect = new Rect(0f, 0.13f, 1f, 0.48f);
                banner.color = new Color(1f, 1f, 1f, 0.34f);
                banner.raycastTarget = false;
                UIStyle.SetRect(bannerGo, new Vector2(0f, 1f), new Vector2(1f, 1f),
                    new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 115f));
            }

            var title = UIStyle.CreateText(panel.transform, "Title", "国家運営 — 国庫・税制・安定", 24,
                TextAnchor.MiddleCenter, UIStyle.Accent);
            UIStyle.SetRect(title.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(-100f, 38f));

            var close = UIStyle.CreateButton(panel.transform, "Close", "×", 18, Hide);
            UIStyle.SetRect(close.gameObject, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-10f, -10f), new Vector2(38f, 38f));

            summaryText = UIStyle.CreateText(panel.transform, "Summary", "", 17,
                TextAnchor.UpperLeft, UIStyle.TextMain);
            UIStyle.SetRect(summaryText.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -65f), new Vector2(-56f, 76f));

            var policyLabel = UIStyle.CreateText(panel.transform, "PolicyLabel", "税制を選択", 16,
                TextAnchor.MiddleLeft, UIStyle.Accent);
            UIStyle.SetRect(policyLabel.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(28f, -146f), new Vector2(130f, 34f));

            lowButton = CreatePolicyButton("LowTax", "減税 80%", 160f, TaxPolicy.Low);
            balancedButton = CreatePolicyButton("BalancedTax", "均衡 100%", 360f, TaxPolicy.Balanced);
            highButton = CreatePolicyButton("HighTax", "重税 130%", 560f, TaxPolicy.High);

            explanationText = UIStyle.CreateText(panel.transform, "Explanation", "", 13,
                TextAnchor.UpperLeft, UIStyle.TextDim);
            UIStyle.SetRect(explanationText.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -203f), new Vector2(-56f, 47f));

            var tableHeader = UIStyle.CreatePanel(panel.transform, "TableHeader", UIStyle.ButtonPressed);
            UIStyle.SetRect(tableHeader, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -251f), new Vector2(-48f, 28f));
            var headerText = UIStyle.CreateText(tableHeader.transform, "Text",
                "文明　　　　　　　　　国庫　　 収支　 安定　疲弊　税制", 13,
                TextAnchor.MiddleLeft, UIStyle.TextDim);
            UIStyle.StretchFull(headerText.gameObject, 7f);

            for (int i = 0; i < PlayerRows; i++)
            {
                var row = UIStyle.CreatePanel(panel.transform, "PlayerRow" + i,
                    i % 2 == 0 ? UIStyle.PanelBgLight : new Color(0.11f, 0.13f, 0.18f, 0.92f));
                UIStyle.SetRect(row, new Vector2(0f, 1f), new Vector2(1f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(0f, -282f - i * 29f), new Vector2(-48f, 26f));
                playerRows[i] = UIStyle.CreateText(row.transform, "Text", "", 13,
                    TextAnchor.MiddleLeft, UIStyle.TextMain);
                UIStyle.StretchFull(playerRows[i].gameObject, 7f);
            }

            var help = UIStyle.CreateText(panel.transform, "Help",
                "収入 = 人口・都市・建物　｜　支出 = 都市・ユニット・戦争　｜　F8で開閉", 12,
                TextAnchor.MiddleCenter, UIStyle.TextDim);
            UIStyle.SetRect(help.gameObject, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(-40f, 24f));

            panel.SetActive(false);
        }

        Button CreatePolicyButton(string name, string label, float x, TaxPolicy policy)
        {
            var button = UIStyle.CreateButton(panel.transform, name, label, 15,
                () => SelectPolicy(policy));
            UIStyle.SetRect(button.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0.5f, 1f), new Vector2(x, -146f), new Vector2(180f, 38f));
            return button;
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

        void SelectPolicy(TaxPolicy policy)
        {
            var state = CultureSystem.CurrentState;
            var player = state != null ? state.HumanPlayer : null;
            if (state == null || player == null || state.IsGameOver) return;
            AdministrationSystem.SetTaxPolicy(state, player, policy);
            Refresh();
        }

        void Refresh()
        {
            shownState = CultureSystem.CurrentState;
            shownVersion = shownState != null ? shownState.Version : -1;
            if (shownState == null)
            {
                summaryText.text = "ゲーム開始後に国家の財政と安定度が表示されます。";
                explanationText.text = "新しいゲームを開始してください。";
                SetPolicyInteractable(false);
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
                explanationText.text = "";
                SetPolicyInteractable(false);
                ClearRows();
                return;
            }

            int revenue = AdministrationSystem.Revenue(focus);
            int expenses = AdministrationSystem.Expenses(focus);
            int balance = revenue - expenses;
            summaryText.text = $"{focus.NameJa}　国庫 {focus.Treasury:N0}　今期 +{revenue:N0} / -{expenses:N0} = {Signed(balance)}\n" +
                $"安定度 {focus.Stability}/100　戦争疲弊 {focus.WarWeariness}/100　総合産出 {AdministrationSystem.OutputPercent(focus)}%";
            explanationText.text = shownState.HumanPlayer == null
                ? "観戦モード：AIの税制判断を表示しています（変更不可）。"
                : "減税は安定と産出を支え、重税は収入を増やします。赤字と長期戦は安定度を低下させます。";

            bool canEdit = shownState.HumanPlayer == focus && !shownState.IsGameOver;
            SetPolicyInteractable(canEdit);
            SetPolicySelected(lowButton, focus.Taxation == TaxPolicy.Low);
            SetPolicySelected(balancedButton, focus.Taxation == TaxPolicy.Balanced);
            SetPolicySelected(highButton, focus.Taxation == TaxPolicy.High);

            int row = 0;
            for (int i = 0; i < shownState.Players.Count && row < PlayerRows; i++)
            {
                Player p = shownState.Players[i];
                if (p.IsEliminated) continue;
                int currentBalance = AdministrationSystem.Balance(p);
                playerRows[row].text = $"{p.NameJa,-18} {p.Treasury,8:N0}　{Signed(currentBalance),6}　{p.Stability,3}　{p.WarWeariness,3}　{AdministrationSystem.PolicyNameJa(p.Taxation)}";
                playerRows[row].color = p == focus ? UIStyle.Accent : UIStyle.TextMain;
                playerRows[row].transform.parent.gameObject.SetActive(true);
                row++;
            }
            for (; row < PlayerRows; row++) playerRows[row].transform.parent.gameObject.SetActive(false);
        }

        void ClearRows()
        {
            for (int i = 0; i < PlayerRows; i++)
                playerRows[i].transform.parent.gameObject.SetActive(false);
        }

        static string Signed(int value) => value >= 0 ? "+" + value.ToString("N0") : value.ToString("N0");

        void SetPolicyInteractable(bool value)
        {
            lowButton.interactable = value;
            balancedButton.interactable = value;
            highButton.interactable = value;
        }

        static void SetPolicySelected(Button button, bool selected)
        {
            if (button == null) return;
            var colors = button.colors;
            colors.normalColor = selected ? new Color(0.52f, 0.43f, 0.15f, 1f) : UIStyle.ButtonNormal;
            colors.selectedColor = colors.normalColor;
            button.colors = colors;
        }

        IEnumerator AnimateOpen()
        {
            const float duration = 0.16f;
            float started = Time.unscaledTime;
            panelGroup.alpha = 0f;
            panel.transform.localScale = new Vector3(0.96f, 0.96f, 1f);
            while (panel != null && panel.activeSelf)
            {
                float t = Mathf.Clamp01((Time.unscaledTime - started) / duration);
                float eased = 1f - (1f - t) * (1f - t);
                panelGroup.alpha = eased;
                float scale = Mathf.Lerp(0.96f, 1f, eased);
                panel.transform.localScale = new Vector3(scale, scale, 1f);
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

        Sprite BuildTreasuryIcon()
        {
            const int size = 32;
            treasuryTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "ProceduralTreasuryIcon",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            var pixels = new Color32[size * size];
            var clear = new Color32(0, 0, 0, 0);
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;
            var gold = new Color32(246, 211, 75, 255);
            var shadow = new Color32(128, 91, 22, 255);
            Fill(pixels, size, 5, 7, 26, 9, gold);
            Fill(pixels, size, 7, 10, 24, 12, shadow);
            Fill(pixels, size, 8, 13, 11, 24, gold);
            Fill(pixels, size, 14, 13, 17, 24, gold);
            Fill(pixels, size, 21, 13, 24, 24, gold);
            Fill(pixels, size, 5, 25, 26, 28, gold);
            Fill(pixels, size, 8, 4, 23, 6, shadow);
            treasuryTexture.SetPixels32(pixels);
            treasuryTexture.Apply(false, true);
            return Sprite.Create(treasuryTexture, new Rect(0f, 0f, size, size),
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
