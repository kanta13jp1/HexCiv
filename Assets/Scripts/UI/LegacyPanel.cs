using System;
using System.Collections.Generic;
using HexCiv.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HexCiv.UI
{
    /// <summary>マップ遺産の探索、偉人登用、歴史作品の収蔵を扱う独立UI。</summary>
    public sealed class LegacyPanel : MonoBehaviour
    {
        const int ItemsPerPage = 5;

        enum PanelMode
        {
            Heritage,
            Candidates,
            Recruited,
            Masterpieces,
        }

        Canvas canvas;
        GameObject panel;
        RectTransform listRoot;
        Text titleText;
        Text summaryText;
        Text pageText;
        Button heritageTab;
        Button candidatesTab;
        Button recruitedTab;
        Button masterpiecesTab;
        Button prevButton;
        Button nextButton;
        PanelMode mode;
        int page;
        GameState shownState;
        int shownVersion = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoSpawn()
        {
            if (FindFirstObjectByType<LegacyPanel>() != null) return;
            new GameObject("WorldLegacyUI").AddComponent<LegacyPanel>();
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
            var state = WorldLegacySystem.CurrentState;
            if (state != shownState || (state != null && state.Version != shownVersion)) Refresh();
        }

        void BuildCanvas()
        {
            var go = new GameObject("WorldLegacyCanvas", typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 140;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("WorldLegacyEventSystem", typeof(EventSystem),
                    typeof(StandaloneInputModule));
                eventSystem.transform.SetParent(transform, false);
            }
        }

        void BuildOpenButton()
        {
            var button = UIStyle.CreateButton(canvas.transform, "WorldLegacyButton", "遺産・偉人・作品", 14, Show);
            UIStyle.SetRect(button.gameObject, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(346f, 176f), new Vector2(160f, 36f));
        }

        void BuildPanel()
        {
            panel = UIStyle.CreatePanel(canvas.transform, "WorldLegacyPanel",
                new Color(0.055f, 0.07f, 0.105f, 0.985f));
            UIStyle.SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(940f, 620f));

            titleText = UIStyle.CreateText(panel.transform, "Title", "世界遺産・偉人・作品", 24,
                TextAnchor.MiddleCenter, UIStyle.Accent);
            UIStyle.SetRect(titleText.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(-90f, 34f));
            var close = UIStyle.CreateButton(panel.transform, "Close", "×", 18, Hide);
            UIStyle.SetRect(close.gameObject, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-9f, -9f), new Vector2(36f, 36f));

            summaryText = UIStyle.CreateText(panel.transform, "Summary", "", 14,
                TextAnchor.MiddleLeft, UIStyle.TextMain);
            summaryText.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIStyle.SetRect(summaryText.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -52f), new Vector2(-56f, 46f));

            heritageTab = MakeTab("HeritageTab", "遺産探索", 28f, PanelMode.Heritage);
            candidatesTab = MakeTab("CandidatesTab", "偉人候補", 188f, PanelMode.Candidates);
            recruitedTab = MakeTab("RecruitedTab", "登用済み", 348f, PanelMode.Recruited);
            masterpiecesTab = MakeTab("MasterpiecesTab", "作品収蔵", 508f, PanelMode.Masterpieces);

            var list = UIStyle.CreateContainer(panel.transform, "Entries");
            listRoot = UIStyle.SetRect(list, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -158f), new Vector2(-56f, 398f));

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

        Button MakeTab(string name, string label, float x, PanelMode tabMode)
        {
            var button = UIStyle.CreateButton(panel.transform, name, label, 15,
                () => SwitchMode(tabMode));
            UIStyle.SetRect(button.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(x, -105f), new Vector2(150f, 38f));
            return button;
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

        void SwitchMode(PanelMode next)
        {
            mode = next;
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
            shownState = WorldLegacySystem.CurrentState;
            shownVersion = shownState != null ? shownState.Version : -1;
            SetTabColor(heritageTab, mode == PanelMode.Heritage);
            SetTabColor(candidatesTab, mode == PanelMode.Candidates);
            SetTabColor(recruitedTab, mode == PanelMode.Recruited);
            SetTabColor(masterpiecesTab, mode == PanelMode.Masterpieces);

            var player = DisplayPlayer(shownState);
            if (shownState == null || player == null)
            {
                summaryText.text = "ゲーム状態を待っています。";
                pageText.text = "0 / 0";
                return;
            }

            titleText.text = "世界遺産・偉人・作品　―　" + player.NameJa;
            summaryText.text = "偉人ポイント " + player.GreatPersonPoints +
                "（累計" + player.TotalGreatPersonPoints + "、+" +
                WorldLegacySystem.GreatPersonPointsPerTurn(player) + "/ターン）　作品ポイント " +
                player.MasterpiecePoints + "（+" + MasterpieceSystem.PointsPerTurn(player) +
                "/ターン）\n遺産 " +
                player.DiscoveredHeritageSites.Count + "/" + shownState.HeritageSites.Count +
                "　偉人 " + player.RecruitedGreatPeople.Count + "/" +
                GreatPersonCatalog.All.Count + "　作品 " +
                player.CollectedMasterpieces.Count + "/" + MasterpieceCatalog.All.Count;

            int total;
            if (mode == PanelMode.Heritage) total = BuildHeritageRows(shownState, player);
            else if (mode == PanelMode.Candidates) total = BuildCandidateRows(shownState, player);
            else if (mode == PanelMode.Recruited) total = BuildRecruitedRows(player);
            else total = BuildMasterpieceRows(shownState, player);
            SetPaging(total);
        }

        int BuildHeritageRows(GameState state, Player player)
        {
            var items = new List<HeritageSiteInstance>(state.HeritageSites);
            items.Sort((a, b) =>
            {
                bool aOwn = a.DiscoveredByPlayerId == player.Id;
                bool bOwn = b.DiscoveredByPlayerId == player.Id;
                if (aOwn != bOwn) return aOwn ? -1 : 1;
                if (a.IsDiscovered != b.IsDiscovered) return a.IsDiscovered ? -1 : 1;
                return string.CompareOrdinal(a.SiteId, b.SiteId);
            });
            ClampPage(items.Count);
            int start = page * ItemsPerPage;
            int end = Math.Min(items.Count, start + ItemsPerPage);
            for (int i = start; i < end; i++)
            {
                var placed = items[i];
                var def = placed.Def;
                bool explored = player.Explored.Contains(placed.Coord);
                string content;
                Color color;
                if (placed.DiscoveredByPlayerId == player.Id)
                {
                    content = "発見済み　" + def.NameJa + "　" + def.LocationJa + " / " + def.PeriodJa +
                        "\n" + def.SummaryJa + "（ターン" + placed.DiscoveredTurn + "）";
                    color = UIStyle.Accent;
                }
                else if (placed.IsDiscovered)
                {
                    var owner = state.GetPlayer(placed.DiscoveredByPlayerId);
                    content = "他文明が発見　" + def.NameJa + "　" + def.RegionJa +
                        "\n発見文明：" + (owner != null ? owner.NameJa : "不明");
                    color = UIStyle.TextDim;
                }
                else if (explored)
                {
                    content = "未発見の史跡　座標 " + placed.Coord +
                        "\n地図上の金色マーカーへユニットを移動すると調査できます。";
                    color = UIStyle.TextMain;
                }
                else
                {
                    content = "未探索の遺産\n地図を探索して手掛かりを見つけてください。";
                    color = UIStyle.TextDim;
                }
                CreateRow(i - start, "Heritage_" + placed.SiteId, content, color, null, null, false);
            }
            return items.Count;
        }

        int BuildCandidateRows(GameState state, Player player)
        {
            var items = WorldLegacySystem.AvailableGreatPeople(state, player);
            ClampPage(items.Count);
            int start = page * ItemsPerPage;
            int end = Math.Min(items.Count, start + ItemsPerPage);
            for (int i = start; i < end; i++)
            {
                var person = items[i];
                int cost = WorldLegacySystem.RecruitmentCost(player, person);
                int affinity = WorldLegacySystem.AffinityPercent(player,
                    person.RelatedCivilizationId, person.RegionJa);
                string affinityText = affinity > 0 ? "　親和性" + affinity + "%" : "";
                string content = person.NameJa + "　" + person.PeriodJa + " / " + person.CategoryJa +
                    affinityText + "\n" + WorldLegacySystem.EffectTextJa(person) + "　" + person.SummaryJa;
                string personId = person.Id;
                CreateRow(i - start, "Candidate_" + person.Id, content, UIStyle.TextMain,
                    "登用 " + cost + "P", () =>
                    {
                        if (WorldLegacySystem.TryRecruit(state, player, personId)) Refresh();
                    }, player.GreatPersonPoints >= cost);
            }
            return items.Count;
        }

        int BuildRecruitedRows(Player player)
        {
            var items = new List<GreatPersonDef>();
            foreach (var id in player.RecruitedGreatPeople)
            {
                var person = GreatPersonCatalog.Find(id);
                if (person != null) items.Add(person);
            }
            items.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            ClampPage(items.Count);
            int start = page * ItemsPerPage;
            int end = Math.Min(items.Count, start + ItemsPerPage);
            for (int i = start; i < end; i++)
            {
                var person = items[i];
                string content = "登用済み　" + person.NameJa + "　" + person.RegionJa + " / " +
                    person.CategoryJa + "\n" + WorldLegacySystem.EffectTextJa(person) +
                    "　" + person.SummaryJa;
                CreateRow(i - start, "Recruited_" + person.Id, content, UIStyle.Accent,
                    null, null, false);
            }
            if (items.Count == 0)
                CreateRow(0, "None", "まだ偉人を登用していません。\nポイントを貯め、偉人候補タブから登用できます。",
                    UIStyle.TextDim, null, null, false);
            return items.Count;
        }

        int BuildMasterpieceRows(GameState state, Player player)
        {
            var items = new List<MasterpieceDef>();
            foreach (var id in player.CollectedMasterpieces)
            {
                var work = MasterpieceCatalog.Find(id);
                if (work != null) items.Add(work);
            }
            items.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            items.AddRange(MasterpieceSystem.AvailableMasterpieces(state, player));

            ClampPage(items.Count);
            int start = page * ItemsPerPage;
            int end = Math.Min(items.Count, start + ItemsPerPage);
            for (int i = start; i < end; i++)
            {
                var work = items[i];
                bool owned = player.CollectedMasterpieces.Contains(work.Id);
                int affinity = MasterpieceSystem.AffinityPercent(player, work);
                string affinityText = affinity > 0 ? "　親和性" + affinity + "%" : "";
                string content = (owned ? "収蔵済み　" : "") + work.NameJa + "　[" +
                    work.KindNameJa + "] " + work.PeriodJa + " / " + work.CreatorJa + affinityText +
                    "\n" + MasterpieceSystem.EffectTextJa(work.Kind) + "　" + work.SummaryJa;
                if (owned)
                {
                    CreateRow(i - start, "OwnedWork_" + work.Id, content, UIStyle.Accent,
                        null, null, false);
                }
                else
                {
                    int cost = MasterpieceSystem.CollectionCost(player, work);
                    string workId = work.Id;
                    CreateRow(i - start, "Work_" + work.Id, content, UIStyle.TextMain,
                        "収蔵 " + cost + "P", () =>
                        {
                            if (MasterpieceSystem.TryCollect(state, player, workId)) Refresh();
                        }, player.MasterpiecePoints >= cost);
                }
            }
            if (items.Count == 0)
                CreateRow(0, "NoWorks", "収蔵できる作品は残っていません。",
                    UIStyle.TextDim, null, null, false);
            return items.Count;
        }

        void CreateRow(int row, string name, string content, Color color, string buttonLabel,
            UnityEngine.Events.UnityAction onClick, bool interactable)
        {
            var bg = UIStyle.CreatePanel(listRoot, name,
                row % 2 == 0 ? UIStyle.PanelBgLight : new Color(0.10f, 0.13f, 0.18f, 0.94f));
            UIStyle.SetRect(bg, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -row * 76f), new Vector2(0f, 70f));
            var text = UIStyle.CreateText(bg.transform, "Text", content, 13, TextAnchor.MiddleLeft, color);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            UIStyle.SetRect(text.gameObject, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                buttonLabel != null ? new Vector2(-84f, 0f) : Vector2.zero,
                buttonLabel != null ? new Vector2(-188f, -8f) : new Vector2(-20f, -8f));
            if (buttonLabel != null)
            {
                var button = UIStyle.CreateButton(bg.transform, "Action", buttonLabel, 13, onClick);
                UIStyle.SetRect(button.gameObject, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                    new Vector2(1f, 0.5f), new Vector2(-10f, 0f), new Vector2(158f, 40f));
                button.interactable = interactable;
            }
        }

        void ClampPage(int count)
        {
            int pages = Math.Max(1, (count + ItemsPerPage - 1) / ItemsPerPage);
            page = Mathf.Clamp(page, 0, pages - 1);
        }

        void SetPaging(int count)
        {
            int pages = Math.Max(1, (count + ItemsPerPage - 1) / ItemsPerPage);
            page = Mathf.Clamp(page, 0, pages - 1);
            pageText.text = count == 0 ? "0件" : (page + 1) + " / " + pages + "　全" + count + "件";
            prevButton.interactable = page > 0;
            nextButton.interactable = page + 1 < pages;
        }

        static Player DisplayPlayer(GameState state)
        {
            if (state == null) return null;
            if (state.HumanPlayer != null) return state.HumanPlayer;
            for (int i = 0; i < state.Players.Count; i++)
                if (!state.Players[i].IsEliminated) return state.Players[i];
            return null;
        }

        static void SetTabColor(Button button, bool selected)
        {
            if (button == null) return;
            var colors = button.colors;
            colors.normalColor = selected ? new Color(0.50f, 0.39f, 0.12f, 1f) : UIStyle.ButtonNormal;
            colors.selectedColor = colors.normalColor;
            button.colors = colors;
        }

        static void ClearChildren(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
                Destroy(root.GetChild(i).gameObject);
        }
    }
}
