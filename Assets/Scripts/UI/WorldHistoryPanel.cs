using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using HexCiv.Core;

namespace HexCiv.UI
{
    /// <summary>
    /// 文明・指導者・遺跡・偉人・研究史・文化史・作品史・生活技術史・自然地理を横断する独立図鑑UI。
    /// UIManagerとは別Canvasに置き、共同開発中のゲーム設定・セーブUIへ依存しない。
    /// </summary>
    public sealed class WorldHistoryPanel : MonoBehaviour
    {
        const int ItemsPerPage = 6;

        enum CatalogMode
        {
            Overview,
            Civilizations,
            Leaders,
            Sites,
            People,
            Research,
            Culture,
            Works,
            MaterialCulture,
            NaturalGeography
        }

        enum RowIconKind
        {
            None,
            Civilization,
            Leader,
            Heritage,
            GreatPerson,
            Research,
            Culture,
            Book,
            Painting,
            Sculpture,
            Architecture,
            Music,
            Theater,
            Film,
            MaterialCulture,
            NaturalGeography
        }

        static readonly string[] Regions =
        {
            "すべて", "アフリカ", "西・南アジア", "東・東南アジア",
            "ヨーロッパ・地中海", "アメリカ大陸", "オセアニア"
        };

        Canvas canvas;
        GameObject panel;
        RectTransform listRoot;
        Text titleText;
        Text countText;
        Text pageText;
        Text regionButtonText;
        Button overviewTab;
        Button civilizationsTab;
        Button leadersTab;
        Button sitesTab;
        Button peopleTab;
        Button researchTab;
        Button cultureTab;
        Button worksTab;
        Button materialCultureTab;
        Button naturalGeographyTab;
        Button prevButton;
        Button nextButton;

        CatalogMode mode = CatalogMode.Overview;
        int regionIndex;
        int page;
        GameState shownState;
        int shownVersion = -1;
        Texture2D civilizationLeaderEmblems;
        Texture2D heritageGreatPeopleEmblems;
        Texture2D researchCultureEmblems;
        Texture2D masterpieceEmblems;
        Texture2D theaterFilmEmblems;
        Texture2D materialCultureIcon;
        Texture2D naturalGeographyIcon;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoSpawn()
        {
            if (FindFirstObjectByType<WorldHistoryPanel>() != null) return;
            var go = new GameObject("WorldHistoryEncyclopedia");
            go.AddComponent<WorldHistoryPanel>();
        }

        void Start()
        {
            BuildCanvas();
            BuildOpenButton();
            BuildPanel();
        }

        void Update()
        {
            if (panel != null && panel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
                Hide();
            else if (panel != null && panel.activeSelf)
            {
                var state = WorldLegacySystem.CurrentState;
                if (state != shownState || (state != null && state.Version != shownVersion))
                    RefreshList();
            }
        }

        void BuildCanvas()
        {
            var go = new GameObject("WorldHistoryCanvas", typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 130;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("WorldHistoryEventSystem", typeof(EventSystem),
                    typeof(StandaloneInputModule));
                eventSystem.transform.SetParent(transform, false);
            }
        }

        void BuildOpenButton()
        {
            var button = UIStyle.CreateButton(canvas.transform, "WorldHistoryButton", "世界史図鑑", 15, Show);
            UIStyle.SetRect(button.gameObject, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(10f, 176f), new Vector2(160f, 36f));
        }

        void BuildPanel()
        {
            panel = UIStyle.CreatePanel(canvas.transform, "WorldHistoryPanel",
                new Color(0.055f, 0.07f, 0.105f, 0.985f));
            UIStyle.SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 620f));

            civilizationLeaderEmblems = Resources.Load<Texture2D>(
                "History/civilization_leader_emblems");
            heritageGreatPeopleEmblems = Resources.Load<Texture2D>(
                "History/heritage_great_people_emblems");
            researchCultureEmblems = Resources.Load<Texture2D>(
                "History/research_culture_emblems");
            masterpieceEmblems = Resources.Load<Texture2D>("History/masterpiece_emblems");
            theaterFilmEmblems = Resources.Load<Texture2D>("History/theater_film_emblems");
            materialCultureIcon = CreateMaterialCultureIcon();
            naturalGeographyIcon = CreateNaturalGeographyIcon();
            var bannerTexture = Resources.Load<Texture2D>("History/world_history_banner");
            if (bannerTexture != null)
            {
                var bannerGo = new GameObject("HistoryBanner", typeof(RectTransform), typeof(RawImage));
                bannerGo.transform.SetParent(panel.transform, false);
                var banner = bannerGo.GetComponent<RawImage>();
                banner.texture = bannerTexture;
                banner.uvRect = new Rect(0f, 0.28f, 1f, 0.44f);
                banner.color = new Color(1f, 1f, 1f, 0.38f);
                banner.raycastTarget = false;
                UIStyle.SetRect(bannerGo, new Vector2(0f, 1f), new Vector2(1f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(0f, 116f));
            }

            titleText = UIStyle.CreateText(panel.transform, "Title", "世界史図鑑", 24,
                TextAnchor.MiddleCenter, UIStyle.Accent);
            UIStyle.SetRect(titleText.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(-90f, 34f));

            var close = UIStyle.CreateButton(panel.transform, "Close", "×", 18, Hide);
            UIStyle.SetRect(close.gameObject, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-9f, -9f), new Vector2(36f, 36f));

            overviewTab = UIStyle.CreateButton(panel.transform, "OverviewTab", "総合", 12,
                () => SwitchMode(CatalogMode.Overview));
            UIStyle.SetRect(overviewTab.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(28f, -55f), new Vector2(58f, 38f));

            civilizationsTab = UIStyle.CreateButton(panel.transform, "CivilizationsTab", "文明", 12,
                () => SwitchMode(CatalogMode.Civilizations));
            UIStyle.SetRect(civilizationsTab.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(89f, -55f), new Vector2(58f, 38f));

            leadersTab = UIStyle.CreateButton(panel.transform, "LeadersTab", "指導者", 12,
                () => SwitchMode(CatalogMode.Leaders));
            UIStyle.SetRect(leadersTab.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(150f, -55f), new Vector2(58f, 38f));

            sitesTab = UIStyle.CreateButton(panel.transform, "SitesTab", "遺跡", 12,
                () => SwitchMode(CatalogMode.Sites));
            UIStyle.SetRect(sitesTab.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(211f, -55f), new Vector2(58f, 38f));

            peopleTab = UIStyle.CreateButton(panel.transform, "PeopleTab", "偉人", 12,
                () => SwitchMode(CatalogMode.People));
            UIStyle.SetRect(peopleTab.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(272f, -55f), new Vector2(58f, 38f));

            researchTab = UIStyle.CreateButton(panel.transform, "ResearchTab", "研究", 12,
                () => SwitchMode(CatalogMode.Research));
            UIStyle.SetRect(researchTab.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(333f, -55f), new Vector2(58f, 38f));

            cultureTab = UIStyle.CreateButton(panel.transform, "CultureTab", "文化", 12,
                () => SwitchMode(CatalogMode.Culture));
            UIStyle.SetRect(cultureTab.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(394f, -55f), new Vector2(58f, 38f));

            worksTab = UIStyle.CreateButton(panel.transform, "WorksTab", "作品", 12,
                () => SwitchMode(CatalogMode.Works));
            UIStyle.SetRect(worksTab.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(455f, -55f), new Vector2(58f, 38f));

            materialCultureTab = UIStyle.CreateButton(panel.transform, "MaterialCultureTab", "生活技術", 11,
                () => SwitchMode(CatalogMode.MaterialCulture));
            UIStyle.SetRect(materialCultureTab.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(516f, -55f), new Vector2(58f, 38f));

            naturalGeographyTab = UIStyle.CreateButton(panel.transform, "NaturalGeographyTab", "自然地理", 11,
                () => SwitchMode(CatalogMode.NaturalGeography));
            UIStyle.SetRect(naturalGeographyTab.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(577f, -55f), new Vector2(58f, 38f));

            var regionButton = UIStyle.CreateButton(panel.transform, "Region", "", 15, CycleRegion);
            UIStyle.SetRect(regionButton.gameObject, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-28f, -55f), new Vector2(210f, 38f));
            regionButtonText = UIStyle.ButtonLabel(regionButton);

            countText = UIStyle.CreateText(panel.transform, "Count", "", 14,
                TextAnchor.MiddleLeft, UIStyle.TextDim);
            UIStyle.SetRect(countText.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -103f), new Vector2(-56f, 24f));

            var list = UIStyle.CreateContainer(panel.transform, "Entries");
            listRoot = UIStyle.SetRect(list, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -134f), new Vector2(-56f, 414f));

            prevButton = UIStyle.CreateButton(panel.transform, "Prev", "前のページ", 14,
                () => ChangePage(-1));
            UIStyle.SetRect(prevButton.gameObject, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(28f, 16f), new Vector2(150f, 40f));

            pageText = UIStyle.CreateText(panel.transform, "Page", "", 15,
                TextAnchor.MiddleCenter, UIStyle.TextMain);
            UIStyle.SetRect(pageText.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(360f, 36f));

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
            RefreshList();
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }

        void SwitchMode(CatalogMode newMode)
        {
            if (mode == newMode) return;
            mode = newMode;
            page = 0;
            RefreshList();
        }

        void CycleRegion()
        {
            regionIndex = (regionIndex + 1) % Regions.Length;
            page = 0;
            RefreshList();
        }

        void ChangePage(int delta)
        {
            int count = CurrentCount();
            int totalPages = Mathf.Max(1, Mathf.CeilToInt(count / (float)ItemsPerPage));
            page = Mathf.Clamp(page + delta, 0, totalPages - 1);
            RefreshList();
        }

        void RefreshList()
        {
            if (panel == null || !panel.activeSelf || listRoot == null) return;
            ClearChildren(listRoot);
            shownState = WorldLegacySystem.CurrentState;
            shownVersion = shownState != null ? shownState.Version : -1;

            regionButtonText.text = "地域：" + Regions[regionIndex] + "　▶";
            SetTabColor(overviewTab, mode == CatalogMode.Overview);
            SetTabColor(civilizationsTab, mode == CatalogMode.Civilizations);
            SetTabColor(leadersTab, mode == CatalogMode.Leaders);
            SetTabColor(sitesTab, mode == CatalogMode.Sites);
            SetTabColor(peopleTab, mode == CatalogMode.People);
            SetTabColor(researchTab, mode == CatalogMode.Research);
            SetTabColor(cultureTab, mode == CatalogMode.Culture);
            SetTabColor(worksTab, mode == CatalogMode.Works);
            SetTabColor(materialCultureTab, mode == CatalogMode.MaterialCulture);
            SetTabColor(naturalGeographyTab, mode == CatalogMode.NaturalGeography);

            if (mode == CatalogMode.Overview)
            {
                var items = IndexEntries();
                BuildOverviewRows(items);
                UpdateFooter(items.Count, "分類");
                titleText.text = "世界史図鑑　―　総合索引";
            }
            else if (mode == CatalogMode.Civilizations)
            {
                var items = FilteredCivilizations();
                BuildCivilizationRows(items);
                UpdateFooter(items.Count, "文明");
                titleText.text = "世界史図鑑　―　文明";
            }
            else if (mode == CatalogMode.Leaders)
            {
                var items = FilteredLeaders();
                BuildLeaderRows(items);
                UpdateFooter(items.Count, "指導者");
                titleText.text = "世界史図鑑　―　王・君主・指導者";
            }
            else if (mode == CatalogMode.Sites)
            {
                var items = FilteredSites();
                BuildSiteRows(items);
                UpdateFooter(items.Count, "遺跡・史跡");
                titleText.text = "世界史図鑑　―　遺跡・史跡";
            }
            else if (mode == CatalogMode.People)
            {
                var items = FilteredPeople();
                BuildPersonRows(items);
                UpdateFooter(items.Count, "偉人");
                titleText.text = "世界史図鑑　―　偉人";
            }
            else if (mode == CatalogMode.Research)
            {
                var items = FilteredResearch();
                BuildResearchRows(items);
                UpdateFooter(items.Count, "研究史");
                titleText.text = "世界史図鑑　―　研究史";
            }
            else if (mode == CatalogMode.Culture)
            {
                var items = FilteredCulture();
                BuildCultureRows(items);
                UpdateFooter(items.Count, "文化史");
                titleText.text = "世界史図鑑　―　文化史";
            }
            else if (mode == CatalogMode.Works)
            {
                var items = FilteredWorks();
                BuildWorkRows(items);
                UpdateFooter(items.Count, "作品");
                titleText.text = "世界史図鑑　―　作品史";
            }
            else if (mode == CatalogMode.MaterialCulture)
            {
                var items = FilteredMaterialCulture();
                BuildMaterialCultureRows(items);
                UpdateFooter(items.Count, "生活・技術史");
                titleText.text = "世界史図鑑　―　生活・技術史";
            }
            else
            {
                var items = FilteredNaturalGeography();
                BuildNaturalGeographyRows(items);
                UpdateFooter(items.Count, "自然地理");
                titleText.text = "世界史図鑑　―　自然地理";
            }
        }

        int CurrentCount()
        {
            switch (mode)
            {
                case CatalogMode.Overview: return IndexEntries().Count;
                case CatalogMode.Civilizations: return FilteredCivilizations().Count;
                case CatalogMode.Leaders: return FilteredLeaders().Count;
                case CatalogMode.Sites: return FilteredSites().Count;
                case CatalogMode.People: return FilteredPeople().Count;
                case CatalogMode.Research: return FilteredResearch().Count;
                case CatalogMode.Culture: return FilteredCulture().Count;
                case CatalogMode.Works: return FilteredWorks().Count;
                case CatalogMode.MaterialCulture: return FilteredMaterialCulture().Count;
                default: return FilteredNaturalGeography().Count;
            }
        }

        List<GlobalHistoryIndexEntry> IndexEntries()
        {
            return GlobalHistoryIndex.Entries(Regions[regionIndex]);
        }

        List<CivilizationDef> FilteredCivilizations()
        {
            return GlobalHistoryIndex.CivilizationsForRegion(Regions[regionIndex]);
        }

        List<LeaderDef> FilteredLeaders()
        {
            return GlobalHistoryIndex.LeadersForRegion(Regions[regionIndex]);
        }

        List<HeritageSiteDef> FilteredSites()
        {
            return HeritageSiteCatalog.ForRegion(regionIndex == 0 ? null : Regions[regionIndex]);
        }

        List<GreatPersonDef> FilteredPeople()
        {
            return GreatPersonCatalog.ForRegion(regionIndex == 0 ? null : Regions[regionIndex]);
        }

        List<ResearchMilestoneDef> FilteredResearch()
        {
            return ResearchMilestoneCatalog.ForRegion(regionIndex == 0 ? null : Regions[regionIndex]);
        }

        List<CulturalTraditionDef> FilteredCulture()
        {
            return CulturalTraditionCatalog.ForRegion(regionIndex == 0 ? null : Regions[regionIndex]);
        }

        List<MasterpieceDef> FilteredWorks()
        {
            return MasterpieceCatalog.ForRegion(regionIndex == 0 ? null : Regions[regionIndex]);
        }

        List<MaterialCultureDef> FilteredMaterialCulture()
        {
            return MaterialCultureCatalog.ForRegion(regionIndex == 0 ? null : Regions[regionIndex]);
        }

        List<NaturalFeatureDef> FilteredNaturalGeography()
        {
            return NaturalFeatureCatalog.ForRegion(regionIndex == 0 ? null : Regions[regionIndex]);
        }

        void BuildOverviewRows(List<GlobalHistoryIndexEntry> items)
        {
            int start = page * ItemsPerPage;
            int end = Mathf.Min(items.Count, start + ItemsPerPage);
            for (int i = start; i < end; i++)
            {
                var item = items[i];
                string text = item.NameJa + "　【実装済み " + item.Count + "件】　継続増補中\n" +
                    item.DetailJa;
                RowIconKind iconKind = item.Id == "civilizations" ? RowIconKind.Civilization :
                    item.Id == "leaders" ? RowIconKind.Leader :
                    item.Id == "heritage" ? RowIconKind.Heritage :
                    item.Id == "great_people" ? RowIconKind.GreatPerson :
                    item.Id == "research" ? RowIconKind.Research :
                    item.Id == "culture" ? RowIconKind.Culture :
                    item.Id == "theater" ? RowIconKind.Theater :
                    item.Id == "film" ? RowIconKind.Film :
                    IsMaterialIndexId(item.Id) ? RowIconKind.MaterialCulture :
                    IsNaturalIndexId(item.Id) ? RowIconKind.NaturalGeography : RowIconKind.None;
                CreateEntryRow(i - start, "Index_" + item.Id, text, iconKind);
            }
        }

        void BuildCivilizationRows(List<CivilizationDef> items)
        {
            int start = page * ItemsPerPage;
            int end = Mathf.Min(items.Count, start + ItemsPerPage);
            for (int i = start; i < end; i++)
            {
                var item = items[i];
                var leader = LeaderCatalog.DefaultForCivilization(item.Id);
                string cities = item.CityNames.Length > 0 ? string.Join("・", item.CityNames, 0,
                    Mathf.Min(3, item.CityNames.Length)) : "都市名資料なし";
                string text = CivilizationStatus(item.Id) + item.NameJa + "　［" + item.RegionJa +
                    "］　" + item.EraJa + "\n代表指導者：" +
                    (leader != null ? leader.NameJa : "未登録") + "　都市例：" + cities;
                CreateEntryRow(i - start, "Civilization_" + item.Id, text,
                    RowIconKind.Civilization);
            }
        }

        void BuildLeaderRows(List<LeaderDef> items)
        {
            int start = page * ItemsPerPage;
            int end = Mathf.Min(items.Count, start + ItemsPerPage);
            for (int i = start; i < end; i++)
            {
                var item = items[i];
                var civilization = CivilizationCatalog.Find(item.CivilizationId);
                string unknown = item.NameKnown ? "" : "（個人名未詳）";
                string text = HistoricalLeaderStatus(item.Id) + item.NameJa + unknown + "　［" +
                    item.TitleJa + "］　" + item.PeriodJa + " / " +
                    (civilization != null ? civilization.NameJa : item.CivilizationId) +
                    "\n" + item.SummaryJa;
                CreateEntryRow(i - start, "Leader_" + item.Id, text, RowIconKind.Leader);
            }
        }

        void BuildSiteRows(List<HeritageSiteDef> items)
        {
            int start = page * ItemsPerPage;
            int end = Mathf.Min(items.Count, start + ItemsPerPage);
            for (int i = start; i < end; i++)
            {
                var item = items[i];
                string status = HeritageStatus(item.Id);
                string text = status + item.NameJa + "　［" + item.TypeJa + "］　" + item.LocationJa +
                    " / " + item.PeriodJa + "\n" + item.SummaryJa;
                CreateEntryRow(i - start, "Site_" + item.Id, text, RowIconKind.Heritage);
            }
        }

        void BuildPersonRows(List<GreatPersonDef> items)
        {
            int start = page * ItemsPerPage;
            int end = Mathf.Min(items.Count, start + ItemsPerPage);
            for (int i = start; i < end; i++)
            {
                var item = items[i];
                string status = GreatPersonStatus(item.Id);
                string text = status + item.NameJa + "　［" + item.CategoryJa + "］　" + item.PeriodJa +
                    "\n" + WorldLegacySystem.EffectTextJa(item) + "　" + item.SummaryJa;
                CreateEntryRow(i - start, "Person_" + item.Id, text,
                    RowIconKind.GreatPerson);
            }
        }

        void BuildResearchRows(List<ResearchMilestoneDef> items)
        {
            int start = page * ItemsPerPage;
            int end = Mathf.Min(items.Count, start + ItemsPerPage);
            for (int i = start; i < end; i++)
            {
                var item = items[i];
                TechDef tech;
                string cost = TechnologyCatalog.TryGet(
                    TechnologyCatalog.TechIdForMilestone(item.Id), out tech)
                    ? " / 研究コスト" + tech.Cost
                    : "";
                string text = item.NameJa + "　［" + item.DomainJa + "］　" + item.PeriodJa +
                    cost + "\n" + item.SummaryJa;
                CreateEntryRow(i - start, "Research_" + item.Id, text, RowIconKind.Research);
            }
        }

        void BuildCultureRows(List<CulturalTraditionDef> items)
        {
            int start = page * ItemsPerPage;
            int end = Mathf.Min(items.Count, start + ItemsPerPage);
            for (int i = start; i < end; i++)
            {
                var item = items[i];
                CulturePolicyDef policy;
                string policyInfo = CulturePolicyCatalog.TryGet(
                    CulturePolicyCatalog.PolicyIdForTradition(item.Id), out policy)
                    ? " / 文化" + policy.Cost + " / " + policy.EffectTextJa
                    : "";
                string text = item.NameJa + "　［" + item.DomainJa + "］　" + item.PeriodJa +
                    policyInfo + "\n" + item.SummaryJa;
                CreateEntryRow(i - start, "Culture_" + item.Id, text, RowIconKind.Culture);
            }
        }

        void BuildWorkRows(List<MasterpieceDef> items)
        {
            int start = page * ItemsPerPage;
            int end = Mathf.Min(items.Count, start + ItemsPerPage);
            for (int i = start; i < end; i++)
            {
                var item = items[i];
                string text = MasterpieceStatus(item.Id) + item.NameJa + "　［" +
                    item.KindNameJa + "］　" + item.PeriodJa + " / " + item.CreatorJa +
                    "\n" + MasterpieceSystem.EffectTextJa(item.Kind) + "　" + item.SummaryJa;
                RowIconKind iconKind = IconForMasterpiece(item.Kind);
                CreateEntryRow(i - start, "Work_" + item.Id, text, iconKind);
            }
        }

        void BuildMaterialCultureRows(List<MaterialCultureDef> items)
        {
            int start = page * ItemsPerPage;
            int end = Mathf.Min(items.Count, start + ItemsPerPage);
            for (int i = start; i < end; i++)
            {
                var item = items[i];
                string text = item.NameJa + "　［" + item.KindNameJa + "］　" + item.PlaceJa +
                    " / " + item.PeriodJa + "\n" + item.SummaryJa;
                CreateEntryRow(i - start, "MaterialCulture_" + item.Id, text,
                    RowIconKind.MaterialCulture);
            }
        }

        void BuildNaturalGeographyRows(List<NaturalFeatureDef> items)
        {
            int start = page * ItemsPerPage;
            int end = Mathf.Min(items.Count, start + ItemsPerPage);
            for (int i = start; i < end; i++)
            {
                var item = items[i];
                string text = item.NameJa + "　［" + item.KindNameJa + "］　" + item.LocationJa +
                    " / " + item.FormJa + "\n" + item.SummaryJa;
                CreateEntryRow(i - start, "NaturalGeography_" + item.Id, text,
                    RowIconKind.NaturalGeography);
            }
        }

        static bool IsMaterialIndexId(string id)
        {
            return id == "specialty_products" || id == "regional_products" ||
                id == "local_icons" || id == "cuisine" || id == "ships" ||
                id == "vehicles" || id == "aircraft" || id == "rockets" ||
                id == "weapons" || id == "dances" || id == "songs" ||
                id == "martial_arts";
        }

        static bool IsNaturalIndexId(string id)
        {
            return id == "mountains" || id == "rivers" || id == "seas" ||
                id == "lakes" || id == "forests" || id == "deserts";
        }

        static RowIconKind IconForMasterpiece(MasterpieceKind kind)
        {
            switch (kind)
            {
                case MasterpieceKind.Book: return RowIconKind.Book;
                case MasterpieceKind.Painting: return RowIconKind.Painting;
                case MasterpieceKind.Sculpture: return RowIconKind.Sculpture;
                case MasterpieceKind.Architecture: return RowIconKind.Architecture;
                case MasterpieceKind.Music: return RowIconKind.Music;
                case MasterpieceKind.Theater: return RowIconKind.Theater;
                case MasterpieceKind.Film: return RowIconKind.Film;
                default: return RowIconKind.None;
            }
        }

        void CreateEntryRow(int row, string name, string content,
            RowIconKind iconKind = RowIconKind.None)
        {
            var rowPanel = UIStyle.CreatePanel(listRoot, name,
                row % 2 == 0 ? UIStyle.PanelBgLight : new Color(0.105f, 0.13f, 0.18f, 0.96f));
            UIStyle.SetRect(rowPanel, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -row * 68f), new Vector2(0f, 61f));

            var text = UIStyle.CreateText(rowPanel.transform, "Text", content, 14,
                TextAnchor.MiddleLeft, UIStyle.TextMain);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.lineSpacing = 1.05f;
            UIStyle.StretchFull(text.gameObject, 9f);

            Texture2D iconTexture = null;
            Rect iconUv = new Rect(0f, 0f, 1f, 1f);
            if (iconKind == RowIconKind.Civilization || iconKind == RowIconKind.Leader)
            {
                iconTexture = civilizationLeaderEmblems;
                iconUv = iconKind == RowIconKind.Civilization
                    ? new Rect(0f, 0f, 0.5f, 1f)
                    : new Rect(0.5f, 0f, 0.5f, 1f);
            }
            else if (iconKind == RowIconKind.Heritage || iconKind == RowIconKind.GreatPerson)
            {
                iconTexture = heritageGreatPeopleEmblems;
                iconUv = iconKind == RowIconKind.Heritage
                    ? new Rect(0f, 0f, 0.5f, 1f)
                    : new Rect(0.5f, 0f, 0.5f, 1f);
            }
            else if (iconKind == RowIconKind.Research || iconKind == RowIconKind.Culture)
            {
                iconTexture = researchCultureEmblems;
                iconUv = iconKind == RowIconKind.Research
                    ? new Rect(0f, 0f, 0.5f, 1f)
                    : new Rect(0.5f, 0f, 0.5f, 1f);
            }
            else if ((int)iconKind >= (int)RowIconKind.Book &&
                (int)iconKind <= (int)RowIconKind.Film)
            {
                iconTexture = masterpieceEmblems;
                int cell = (int)iconKind - (int)RowIconKind.Book;
                iconUv = new Rect((cell % 4) * 0.25f, cell < 4 ? 0.5f : 0f,
                    0.25f, 0.5f);

                // 旧2分割アトラスを残し、生成画像が欠けた環境でも演劇・映画は表示する。
                if (iconTexture == null &&
                    (iconKind == RowIconKind.Theater || iconKind == RowIconKind.Film))
                {
                    iconTexture = theaterFilmEmblems;
                    iconUv = iconKind == RowIconKind.Theater
                        ? new Rect(0f, 0f, 0.5f, 1f)
                        : new Rect(0.5f, 0f, 0.5f, 1f);
                }
            }
            else if (iconKind == RowIconKind.MaterialCulture)
            {
                iconTexture = materialCultureIcon;
            }
            else if (iconKind == RowIconKind.NaturalGeography)
            {
                iconTexture = naturalGeographyIcon;
            }

            if (iconTexture != null)
            {
                var iconGo = new GameObject(iconKind + "Icon", typeof(RectTransform),
                    typeof(RawImage));
                iconGo.transform.SetParent(rowPanel.transform, false);
                var icon = iconGo.GetComponent<RawImage>();
                icon.texture = iconTexture;
                icon.uvRect = iconUv;
                icon.raycastTarget = false;
                UIStyle.SetRect(iconGo, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(48f, 48f));
                var textRect = (RectTransform)text.transform;
                textRect.offsetMin = new Vector2(64f, textRect.offsetMin.y);
            }
        }

        void UpdateFooter(int count, string unit)
        {
            int totalPages = Mathf.Max(1, Mathf.CeilToInt(count / (float)ItemsPerPage));
            page = Mathf.Clamp(page, 0, totalPages - 1);
            countText.text = Regions[regionIndex] + "：" + count + "件　（段階実装。史料確認後に順次追加）";
            pageText.text = (page + 1) + " / " + totalPages + "　全" + count + unit;
            prevButton.interactable = page > 0;
            nextButton.interactable = page < totalPages - 1;
        }

        string HeritageStatus(string id)
        {
            if (shownState == null) return "";
            for (int i = 0; i < shownState.HeritageSites.Count; i++)
            {
                var placed = shownState.HeritageSites[i];
                if (!string.Equals(placed.SiteId, id, StringComparison.OrdinalIgnoreCase)) continue;
                var human = shownState.HumanPlayer;
                if (human != null && placed.DiscoveredByPlayerId == human.Id) return "【発見済】";
                if (placed.IsDiscovered) return "【他文明発見】";
                return "【この世界に存在】";
            }
            return "";
        }

        string CivilizationStatus(string id)
        {
            if (shownState == null) return "";
            var human = shownState.HumanPlayer;
            if (human != null && string.Equals(human.CivilizationId, id,
                StringComparison.OrdinalIgnoreCase)) return "【自文明】";
            for (int i = 0; i < shownState.Players.Count; i++)
                if (string.Equals(shownState.Players[i].CivilizationId, id,
                    StringComparison.OrdinalIgnoreCase)) return "【この世界に登場】";
            return "";
        }

        string HistoricalLeaderStatus(string id)
        {
            if (shownState == null) return "";
            var human = shownState.HumanPlayer;
            if (human != null && string.Equals(human.LeaderId, id,
                StringComparison.OrdinalIgnoreCase)) return "【現在の指導者】";
            for (int i = 0; i < shownState.Players.Count; i++)
                if (string.Equals(shownState.Players[i].LeaderId, id,
                    StringComparison.OrdinalIgnoreCase)) return "【この世界に登場】";
            return "";
        }

        string GreatPersonStatus(string id)
        {
            if (shownState == null) return "";
            var human = shownState.HumanPlayer;
            if (human != null && human.RecruitedGreatPeople.Contains(id)) return "【登用済】";
            return WorldLegacySystem.IsRecruited(shownState, id) ? "【他文明登用】" : "";
        }

        string MasterpieceStatus(string id)
        {
            if (shownState == null) return "";
            var human = shownState.HumanPlayer;
            if (human != null && human.CollectedMasterpieces.Contains(id)) return "【収蔵済】";
            return MasterpieceSystem.IsCollected(shownState, id) ? "【他文明収蔵】" : "";
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
            if (root == null) return;
            for (int i = root.childCount - 1; i >= 0; i--)
                Destroy(root.GetChild(i).gameObject);
        }

        static Texture2D CreateMaterialCultureIcon()
        {
            const int size = 48;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "MaterialCultureIcon",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[size * size];
            var clear = new Color32(0, 0, 0, 0);
            var gold = new Color32(236, 192, 65, 255);
            var blue = new Color32(85, 174, 207, 255);
            var dark = new Color32(21, 30, 47, 255);
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

            // 船鉢と歯車を組み合わせた、外部素材に依存しない生活・技術アイコン。
            Fill(pixels, size, 7, 25, 31, 29, gold);
            Fill(pixels, size, 11, 30, 27, 34, gold);
            Fill(pixels, size, 15, 35, 23, 38, gold);
            Fill(pixels, size, 18, 10, 20, 24, blue);
            Fill(pixels, size, 20, 13, 29, 15, blue);
            Fill(pixels, size, 25, 16, 31, 18, blue);
            for (int y = 9; y <= 22; y++)
                for (int x = 30; x <= 43; x++)
                {
                    int dx = x - 36, dy = y - 16;
                    int d = dx * dx + dy * dy;
                    if (d >= 25 && d <= 49) pixels[y * size + x] = gold;
                    else if (d < 10) pixels[y * size + x] = dark;
                }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        static Texture2D CreateNaturalGeographyIcon()
        {
            const int size = 48;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "NaturalGeographyIcon",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[size * size];
            var clear = new Color32(0, 0, 0, 0);
            var mountain = new Color32(166, 158, 145, 255);
            var snow = new Color32(235, 240, 241, 255);
            var forest = new Color32(54, 132, 70, 255);
            var river = new Color32(61, 174, 224, 255);
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

            // 山・森林・蛇行河川を組み合わせた外部素材不要の自然地理アイコン。
            for (int y = 12; y <= 36; y++)
                for (int x = 5; x <= 31; x++)
                    if (y <= 36 && y >= 12 + Mathf.Abs(x - 18)) pixels[y * size + x] = mountain;
            for (int y = 12; y <= 22; y++)
                for (int x = 12; x <= 24; x++)
                    if (y >= 12 + Mathf.Abs(x - 18)) pixels[y * size + x] = snow;
            Fill(pixels, size, 3, 35, 25, 40, forest);
            for (int y = 8; y <= 43; y++)
            {
                int x = 34 + Mathf.RoundToInt(Mathf.Sin(y * 0.38f) * 5f);
                Fill(pixels, size, x - 2, y, x + 2, y + 1, river);
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        static void Fill(Color32[] pixels, int size, int x0, int y0, int x1, int y1, Color32 color)
        {
            for (int y = Mathf.Max(0, y0); y <= Mathf.Min(size - 1, y1); y++)
                for (int x = Mathf.Max(0, x0); x <= Mathf.Min(size - 1, x1); x++)
                    pixels[y * size + x] = color;
        }

        void OnDestroy()
        {
            if (materialCultureIcon != null) Destroy(materialCultureIcon);
            if (naturalGeographyIcon != null) Destroy(naturalGeographyIcon);
        }
    }
}
