using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using HexCiv.Core;

namespace HexCiv.UI
{
    /// <summary>
    /// 文化ポイント、政策選択、文明間の影響力を表示する独立UI。
    /// UIManager/GameBootstrapを変更せず、CultureSystemが公開する現行状態を参照する。
    /// </summary>
    public sealed class CulturePanel : MonoBehaviour
    {
        const int ItemsPerPage = 6;

        enum PanelMode
        {
            Available,
            Adopted,
            Influence,
        }

        Canvas canvas;
        GameObject panel;
        RectTransform listRoot;
        Text titleText;
        Text summaryText;
        Text pageText;
        Button availableTab;
        Button adoptedTab;
        Button influenceTab;
        Button prevButton;
        Button nextButton;

        PanelMode mode;
        int page;
        GameState shownState;
        int shownVersion = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoSpawn()
        {
            if (FindFirstObjectByType<CulturePanel>() != null) return;
            new GameObject("CulturePolicyUI").AddComponent<CulturePanel>();
        }

        void Start()
        {
            BuildCanvas();
            BuildOpenButton();
            BuildPanel();
        }

        void Update()
        {
            if (panel == null || !panel.activeSelf) return;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Hide();
                return;
            }

            var current = CultureSystem.CurrentState;
            if (current != shownState || (current != null && current.Version != shownVersion))
                Refresh();
        }

        void BuildCanvas()
        {
            var go = new GameObject("CultureCanvas", typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 135;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("CultureEventSystem", typeof(EventSystem),
                    typeof(StandaloneInputModule));
                eventSystem.transform.SetParent(transform, false);
            }
        }

        void BuildOpenButton()
        {
            var button = UIStyle.CreateButton(canvas.transform, "CultureButton", "文化・政策", 15, Show);
            UIStyle.SetRect(button.gameObject, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(178f, 176f), new Vector2(160f, 36f));
        }

        void BuildPanel()
        {
            panel = UIStyle.CreatePanel(canvas.transform, "CulturePanel",
                new Color(0.055f, 0.07f, 0.105f, 0.985f));
            UIStyle.SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(920f, 620f));

            titleText = UIStyle.CreateText(panel.transform, "Title", "文化と政策", 24,
                TextAnchor.MiddleCenter, UIStyle.Accent);
            UIStyle.SetRect(titleText.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(-90f, 34f));

            var close = UIStyle.CreateButton(panel.transform, "Close", "×", 18, Hide);
            UIStyle.SetRect(close.gameObject, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-9f, -9f), new Vector2(36f, 36f));

            summaryText = UIStyle.CreateText(panel.transform, "Summary", "", 14,
                TextAnchor.MiddleLeft, UIStyle.TextMain);
            summaryText.horizontalOverflow = HorizontalWrapMode.Wrap;
            summaryText.verticalOverflow = VerticalWrapMode.Truncate;
            UIStyle.SetRect(summaryText.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -52f), new Vector2(-56f, 54f));

            availableTab = UIStyle.CreateButton(panel.transform, "AvailableTab", "採用可能", 15,
                () => SwitchMode(PanelMode.Available));
            UIStyle.SetRect(availableTab.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(28f, -116f), new Vector2(150f, 38f));

            adoptedTab = UIStyle.CreateButton(panel.transform, "AdoptedTab", "採用済み", 15,
                () => SwitchMode(PanelMode.Adopted));
            UIStyle.SetRect(adoptedTab.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(186f, -116f), new Vector2(150f, 38f));

            influenceTab = UIStyle.CreateButton(panel.transform, "InfluenceTab", "文化交流", 15,
                () => SwitchMode(PanelMode.Influence));
            UIStyle.SetRect(influenceTab.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(344f, -116f), new Vector2(150f, 38f));

            var list = UIStyle.CreateContainer(panel.transform, "Entries");
            listRoot = UIStyle.SetRect(list, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -166f), new Vector2(-56f, 386f));

            prevButton = UIStyle.CreateButton(panel.transform, "Prev", "前のページ", 14,
                () => ChangePage(-1));
            UIStyle.SetRect(prevButton.gameObject, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(28f, 16f), new Vector2(150f, 40f));

            pageText = UIStyle.CreateText(panel.transform, "Page", "", 15,
                TextAnchor.MiddleCenter, UIStyle.TextMain);
            UIStyle.SetRect(pageText.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(420f, 36f));

            nextButton = UIStyle.CreateButton(panel.transform, "Next", "次のページ", 14,
                () => ChangePage(1));
            UIStyle.SetRect(nextButton.gameObject, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 0f), new Vector2(-28f, 16f), new Vector2(150f, 40f));

            panel.SetActive(false);
        }

        public void Show()
        {
            if (panel == null) return;
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            Refresh();
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }

        void SwitchMode(PanelMode newMode)
        {
            mode = newMode;
            page = 0;
            Refresh();
        }

        void ChangePage(int delta)
        {
            page += delta;
            Refresh();
        }

        void Refresh()
        {
            if (panel == null || !panel.activeSelf || listRoot == null) return;
            ClearChildren(listRoot);

            shownState = CultureSystem.CurrentState;
            shownVersion = shownState != null ? shownState.Version : -1;
            SetTabColor(availableTab, mode == PanelMode.Available);
            SetTabColor(adoptedTab, mode == PanelMode.Adopted);
            SetTabColor(influenceTab, mode == PanelMode.Influence);

            var player = DisplayPlayer(shownState);
            if (shownState == null || player == null)
            {
                titleText.text = "文化と政策";
                summaryText.text = "ゲーム状態を待っています。";
                pageText.text = "0 / 0";
                prevButton.interactable = false;
                nextButton.interactable = false;
                return;
            }

            titleText.text = "文化と政策　―　" + player.NameJa;
            int output = CultureSystem.CulturePerTurn(shownState, player);
            string current = "未選択";
            CulturePolicyDef currentPolicy;
            if (CulturePolicyCatalog.TryGet(player.CurrentCulturePolicyId, out currentPolicy))
                current = currentPolicy.NameJa + " " + player.CultureStored + "/" + currentPolicy.Cost;
            int victoryPercent = Mathf.RoundToInt(CultureSystem.VictoryProgress(shownState, player) * 100f);
            summaryText.text = "文化 " + player.CultureStored + "（累計" + player.TotalCulture +
                "、+" + output + "/ターン）　採用済み " + player.KnownCulturePolicies.Count +
                "/" + CulturePolicyCatalog.All.Count + "\n進行中：" + current +
                "　文化勝利への影響力 " + victoryPercent + "%";

            if (mode == PanelMode.Available) BuildAvailable(player);
            else if (mode == PanelMode.Adopted) BuildAdopted(player);
            else BuildInfluence(shownState, player);
        }

        void BuildAvailable(Player player)
        {
            var items = player.AvailableCulturePolicies();
            int totalPages = PageCount(items.Count);
            page = Mathf.Clamp(page, 0, totalPages - 1);
            int start = page * ItemsPerPage;
            int end = Mathf.Min(items.Count, start + ItemsPerPage);
            bool canChoose = shownState.HumanPlayer == player && !shownState.IsGameOver;

            for (int i = start; i < end; i++)
            {
                var policy = items[i];
                bool selected = player.CurrentCulturePolicyId == policy.Id;
                CreatePolicyRow(i - start, policy,
                    selected ? "進行中" : "選択", canChoose && !selected,
                    () => SelectPolicy(player, policy.Id));
            }
            UpdateFooter(items.Count, totalPages, "採用可能な政策");
        }

        void BuildAdopted(Player player)
        {
            var items = new List<CulturePolicyDef>();
            for (int i = 0; i < CulturePolicyCatalog.All.Count; i++)
                if (player.KnownCulturePolicies.Contains(CulturePolicyCatalog.All[i].Id))
                    items.Add(CulturePolicyCatalog.All[i]);

            int totalPages = PageCount(items.Count);
            page = Mathf.Clamp(page, 0, totalPages - 1);
            int start = page * ItemsPerPage;
            int end = Mathf.Min(items.Count, start + ItemsPerPage);
            for (int i = start; i < end; i++)
                CreatePolicyRow(i - start, items[i], "採用済み", false, null);
            UpdateFooter(items.Count, totalPages, "採用済み政策");
        }

        void BuildInfluence(GameState state, Player player)
        {
            var rivals = new List<Player>();
            for (int i = 0; i < state.Players.Count; i++)
                if (state.Players[i] != player && !state.Players[i].IsEliminated)
                    rivals.Add(state.Players[i]);

            int totalPages = PageCount(rivals.Count);
            page = Mathf.Clamp(page, 0, totalPages - 1);
            int start = page * ItemsPerPage;
            int end = Mathf.Min(rivals.Count, start + ItemsPerPage);
            for (int i = start; i < end; i++)
            {
                var rival = rivals[i];
                int influence = CultureSystem.InfluenceOn(player, rival);
                int threshold = CultureSystem.VictoryThreshold(rival);
                string relation = player.IsAtWarWith(rival.Id) ? "戦争中：交流停止" : "平和交流中";
                string text = rival.NameJa + "　文化的影響 " + influence + "/" + threshold +
                    "　［" + relation + "］\n相手の累計文化 " + rival.TotalCulture;
                CreateTextRow(i - start, text);
            }
            UpdateFooter(rivals.Count, totalPages, "交流文明");
        }

        void SelectPolicy(Player player, string policyId)
        {
            if (shownState == null || shownState.HumanPlayer != player || shownState.IsGameOver) return;
            if (!CultureSystem.CanSelectPolicy(player, policyId)) return;
            player.SetCulturePolicy(policyId);
            shownState.Bump();
            Refresh();
        }

        void CreatePolicyRow(int row, CulturePolicyDef policy, string buttonText,
            bool interactable, UnityEngine.Events.UnityAction onClick)
        {
            var rowPanel = CreateRowPanel(row, "Policy_" + policy.Id);
            string content = policy.NameJa + "　［" + policy.Tradition.RegionJa + " / " +
                policy.Tradition.DomainJa + "］　文化" + policy.Cost +
                "\n効果：" + policy.EffectTextJa;
            var text = UIStyle.CreateText(rowPanel.transform, "Text", content, 14,
                TextAnchor.MiddleLeft, UIStyle.TextMain);
            UIStyle.SetRect(text.gameObject, new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(0.5f, 0.5f), new Vector2(-76f, 0f), new Vector2(-152f, 0f));

            var button = UIStyle.CreateButton(rowPanel.transform, "Action", buttonText, 13,
                onClick ?? (() => { }));
            UIStyle.SetRect(button.gameObject, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f), new Vector2(-8f, 0f), new Vector2(126f, 38f));
            button.interactable = interactable;
        }

        void CreateTextRow(int row, string content)
        {
            var rowPanel = CreateRowPanel(row, "Influence_" + row);
            var text = UIStyle.CreateText(rowPanel.transform, "Text", content, 14,
                TextAnchor.MiddleLeft, UIStyle.TextMain);
            UIStyle.StretchFull(text.gameObject, 9f);
        }

        GameObject CreateRowPanel(int row, string name)
        {
            var rowPanel = UIStyle.CreatePanel(listRoot, name,
                row % 2 == 0 ? UIStyle.PanelBgLight : new Color(0.105f, 0.13f, 0.18f, 0.96f));
            UIStyle.SetRect(rowPanel, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -row * 62f), new Vector2(0f, 56f));
            return rowPanel;
        }

        void UpdateFooter(int count, int totalPages, string label)
        {
            pageText.text = (page + 1) + " / " + totalPages + "　全" + count + label;
            prevButton.interactable = page > 0;
            nextButton.interactable = page < totalPages - 1;
        }

        static int PageCount(int count)
        {
            return Mathf.Max(1, Mathf.CeilToInt(count / (float)ItemsPerPage));
        }

        static Player DisplayPlayer(GameState state)
        {
            if (state == null) return null;
            if (state.HumanPlayer != null) return state.HumanPlayer;
            Player best = null;
            for (int i = 0; i < state.Players.Count; i++)
            {
                var candidate = state.Players[i];
                if (candidate.IsEliminated) continue;
                if (best == null || candidate.TotalCulture > best.TotalCulture ||
                    (candidate.TotalCulture == best.TotalCulture && candidate.Id < best.Id))
                    best = candidate;
            }
            return best;
        }

        static void SetTabColor(Button button, bool selected)
        {
            if (button == null) return;
            var colors = button.colors;
            colors.normalColor = selected
                ? Color.Lerp(UIStyle.ButtonNormal, UIStyle.Accent, 0.42f)
                : UIStyle.ButtonNormal;
            colors.selectedColor = colors.normalColor;
            button.colors = colors;
        }

        static void ClearChildren(RectTransform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
                Destroy(root.GetChild(i).gameObject);
        }
    }
}
