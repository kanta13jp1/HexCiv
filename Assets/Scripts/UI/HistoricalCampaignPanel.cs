using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using HexCiv.Core;
using HexCiv.Render;

namespace HexCiv.UI
{
    /// <summary>
    /// ウルク史実キャンペーン専用HUD。通常ゲームのUIManagerへ史実固有の人口縮尺や
    /// 物資欄を混ぜず、独立Canvasとして重ねる。
    /// </summary>
    public sealed class HistoricalCampaignPanel : MonoBehaviour
    {
        HistoricalCampaignSession session;
        Action<string> onAction;
        Action onQuickSave;
        Action onQuickLoad;
        Canvas canvas;
        GameObject detailPanel;
        GameObject laborPanel;
        Text heading;
        Text summary;
        Text advisorTitle;
        Text advisorBody;
        Text detailText;
        Text laborTotalText;
        Text reportText;
        Button canalButton;
        Button foodButton;
        Button templeButton;
        Button administrationButton;
        readonly Text[] laborValues = new Text[6];
        readonly Button[] laborDownButtons = new Button[6];
        readonly Button[] laborUpButtons = new Button[6];
        static readonly string[] LaborKinds =
        {
            "food", "canal", "construction", "crafts", "trade", "militia",
        };
        bool detailed;
        HistoricalCampaignWorldVisuals worldVisuals;

        public void Init(HistoricalCampaignSession campaignSession,
            Action<string> campaignAction, Action quickSave, Action quickLoad)
        {
            session = campaignSession ?? throw new ArgumentNullException(nameof(campaignSession));
            onAction = campaignAction;
            onQuickSave = quickSave;
            onQuickLoad = quickLoad;
            if (canvas == null) BuildUi();
            if (worldVisuals == null)
            {
                var visuals = new GameObject("HistoricalCampaignWorldVisuals");
                visuals.transform.SetParent(transform, false);
                worldVisuals = visuals.AddComponent<HistoricalCampaignWorldVisuals>();
            }
            worldVisuals.Init(session);
            Refresh();
        }

        public void Refresh()
        {
            if (session == null || heading == null) return;
            var progress = session.Progress;
            heading.text = $"史実キャンペーン　{session.Definition.title.ja}　" +
                $"{session.CurrentIntervalJa}　ターン{session.State.TurnNumber}/{session.Definition.maxTurns}";
            int irrigated = UrukCampaignSystem.IrrigatedFarmCount(progress);
            summary.text =
                $"人口 {PopulationDisplay(progress)}（{Signed(progress.lastPopulationChange)}）　" +
                $"食料 {FoodDisplay(progress)}　農地 {irrigated}/{UrukCampaignSystem.FarmCount(progress)} 灌漑　" +
                $"運河 {UrukCampaignSystem.CanalCondition(progress)}%　" +
                $"神殿 {progress.templeStage}/5・{progress.templeProgress}%　安定 {progress.stability}";
            advisorTitle.text = "顧問　" + UrukCampaignSystem.TutorialTitleJa(session);
            advisorBody.text = BuildAdvisorBody(progress);
            reportText.text = string.IsNullOrWhiteSpace(progress.lastReportJa)
                ? "" : "前期間: " + progress.lastReportJa;
            detailText.text = BuildDetailText(progress);

            int turn = session.State.TurnNumber;
            canalButton.interactable = !session.State.IsGameOver &&
                progress.lastCanalActionTurn != turn &&
                UrukCampaignSystem.GoodAmount(progress, "reeds") >= 1;
            foodButton.interactable = !session.State.IsGameOver &&
                progress.lastFoodPriorityTurn != turn;
            templeButton.interactable = !session.State.IsGameOver &&
                !progress.templePlanned &&
                UrukCampaignSystem.GoodAmount(progress, "alluvial_clay") >= 1 &&
                UrukCampaignSystem.GoodAmount(progress, "reeds") >= 1;
            administrationButton.interactable = !session.State.IsGameOver &&
                !progress.administrationAdopted &&
                progress.templeStage >= 5 &&
                progress.templeProgress >= UrukCampaignSystem.AdministrationUnlockProgress &&
                UrukCampaignSystem.GoodAmount(progress, "alluvial_clay") >= 2;
            detailPanel.SetActive(detailed);
            laborPanel.SetActive(detailed);
            RefreshLabor(progress);
            worldVisuals?.Refresh();
        }

        void BuildUi()
        {
            var canvasGo = new GameObject("HistoricalCampaignCanvas", typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 122;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            if (FindFirstObjectByType<EventSystem>() == null)
                new GameObject("HistoricalCampaignEventSystem", typeof(EventSystem),
                    typeof(StandaloneInputModule)).transform.SetParent(transform, false);

            var top = UIStyle.CreatePanel(canvasGo.transform, "CampaignTop",
                new Color(0.035f, 0.055f, 0.09f, 0.94f));
            UIStyle.SetRect(top, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(1180f, 82f));
            heading = UIStyle.CreateText(top.transform, "Heading", "", 21,
                TextAnchor.MiddleCenter, UIStyle.Accent);
            heading.fontStyle = FontStyle.Bold;
            UIStyle.SetRect(heading.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -5f), new Vector2(-20f, 34f));
            summary = UIStyle.CreateText(top.transform, "Summary", "", 16,
                TextAnchor.MiddleCenter, UIStyle.TextMain);
            UIStyle.SetRect(summary.gameObject, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(-20f, 35f));

            var advisor = UIStyle.CreatePanel(canvasGo.transform, "CampaignAdvisor",
                new Color(0.035f, 0.055f, 0.09f, 0.95f));
            UIStyle.SetRect(advisor, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f), new Vector2(-18f, 20f), new Vector2(390f, 500f));
            advisorTitle = UIStyle.CreateText(advisor.transform, "AdvisorTitle", "", 20,
                TextAnchor.UpperLeft, UIStyle.Accent);
            advisorTitle.fontStyle = FontStyle.Bold;
            advisorTitle.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIStyle.SetRect(advisorTitle.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(-28f, 48f));
            advisorBody = UIStyle.CreateText(advisor.transform, "AdvisorBody", "", 15,
                TextAnchor.UpperLeft, UIStyle.TextMain);
            advisorBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIStyle.SetRect(advisorBody.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(-28f, 58f));

            canalButton = CreateActionButton(advisor.transform, "CanalButton",
                "運河を整備（葦1）", 320f,
                () => ApplyAction(UrukCampaignSystem.MaintainCanalAction));
            foodButton = CreateActionButton(advisor.transform, "FoodButton",
                "食料を優先", 268f,
                () => ApplyAction(UrukCampaignSystem.PrioritizeFoodAction));
            templeButton = CreateActionButton(advisor.transform, "TempleButton",
                "初期神殿を着工（粘土1・葦1）", 216f,
                () => ApplyAction(UrukCampaignSystem.PlanTempleAction));
            administrationButton = CreateActionButton(advisor.transform, "AdministrationButton",
                "数量記録行政を採用（粘土2）", 164f,
                () => ApplyAction(UrukCampaignSystem.AdoptAdministrationAction));

            reportText = UIStyle.CreateText(advisor.transform, "Report", "", 13,
                TextAnchor.UpperLeft, UIStyle.TextDim);
            reportText.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIStyle.SetRect(reportText.gameObject, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 106f), new Vector2(-28f, 48f));

            var detailButton = UIStyle.CreateButton(advisor.transform, "DetailButton",
                "簡潔／詳細", 13, () =>
                {
                    detailed = !detailed;
                    Refresh();
                });
            UIStyle.SetRect(detailButton.gameObject, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(14f, 14f), new Vector2(112f, 38f));
            var saveButton = UIStyle.CreateButton(advisor.transform, "QuickSaveButton",
                "史実セーブ", 13, () => onQuickSave?.Invoke());
            UIStyle.SetRect(saveButton.gameObject, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(136f, 14f), new Vector2(112f, 38f));
            var loadButton = UIStyle.CreateButton(advisor.transform, "QuickLoadButton",
                "史実ロード", 13, () => onQuickLoad?.Invoke());
            UIStyle.SetRect(loadButton.gameObject, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(258f, 14f), new Vector2(112f, 38f));

            detailPanel = UIStyle.CreatePanel(canvasGo.transform, "CampaignDetail",
                new Color(0.025f, 0.04f, 0.07f, 0.97f));
            UIStyle.SetRect(detailPanel, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f), new Vector2(-418f, 20f), new Vector2(470f, 660f));
            detailText = UIStyle.CreateText(detailPanel.transform, "DetailText", "", 12,
                TextAnchor.UpperLeft, UIStyle.TextMain);
            detailText.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIStyle.StretchFull(detailText.gameObject, 18f);
            detailPanel.SetActive(false);

            laborPanel = UIStyle.CreatePanel(canvasGo.transform, "CampaignLabor",
                new Color(0.025f, 0.04f, 0.07f, 0.97f));
            UIStyle.SetRect(laborPanel, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f), new Vector2(-888f, 20f), new Vector2(450f, 500f));
            var laborTitle = UIStyle.CreateText(laborPanel.transform, "LaborTitle",
                "公共労働の配分（5%刻み）", 18,
                TextAnchor.UpperLeft, UIStyle.Accent);
            laborTitle.fontStyle = FontStyle.Bold;
            UIStyle.SetRect(laborTitle.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(-28f, 30f));
            laborTotalText = UIStyle.CreateText(laborPanel.transform, "LaborTotal", "", 13,
                TextAnchor.UpperLeft, UIStyle.TextDim);
            laborTotalText.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIStyle.SetRect(laborTotalText.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(-28f, 54f));
            for (int i = 0; i < LaborKinds.Length; i++)
                CreateLaborRow(laborPanel.transform, i, 354f - i * 54f);
            var laborNote = UIStyle.CreateText(laborPanel.transform, "LaborNote",
                "世帯維持を除く動員可能人口のみ。食料30%を保護。\n" +
                "自動管理は強制・債務・捕虜・権利変更を行いません。",
                12, TextAnchor.LowerLeft, UIStyle.TextDim);
            laborNote.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIStyle.SetRect(laborNote.gameObject, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(-28f, 56f));
            laborPanel.SetActive(false);
        }

        void CreateLaborRow(Transform parent, int index, float y)
        {
            string kind = LaborKinds[index];
            laborValues[index] = UIStyle.CreateText(parent, "Labor_" + kind, "", 14,
                TextAnchor.MiddleLeft, UIStyle.TextMain);
            UIStyle.SetRect(laborValues[index].gameObject,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(16f, y), new Vector2(280f, 40f));
            laborDownButtons[index] = UIStyle.CreateButton(parent, "LaborDown_" + kind,
                "−5%", 12, () => ApplyAction("labor_" + kind + "_down"));
            UIStyle.SetRect(laborDownButtons[index].gameObject,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(300f, y + 3f), new Vector2(58f, 34f));
            laborUpButtons[index] = UIStyle.CreateButton(parent, "LaborUp_" + kind,
                "+5%", 12, () => ApplyAction("labor_" + kind + "_up"));
            UIStyle.SetRect(laborUpButtons[index].gameObject,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(366f, y + 3f), new Vector2(58f, 34f));
        }

        Button CreateActionButton(Transform parent, string name, string label, float y,
            UnityEngine.Events.UnityAction onClick)
        {
            var button = UIStyle.CreateButton(parent, name, label, 14, onClick);
            UIStyle.SetRect(button.gameObject, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, y), new Vector2(-28f, 42f));
            return button;
        }

        void ApplyAction(string actionId)
        {
            onAction?.Invoke(actionId);
            Refresh();
        }

        void RefreshLabor(UrukCampaignProgress progress)
        {
            if (laborTotalText == null || progress.labor == null) return;
            int mobilizable = UrukSubsistenceSystem.MobilizablePopulation(progress);
            laborTotalText.text =
                $"動員可能 {KnownPeople(progress, mobilizable)}／実人口 {PopulationDisplay(progress)}\n" +
                $"配分合計 {progress.labor.Total}%　自動管理 " +
                $"{(progress.populationAutomation ? "ON（食料安全）" : "OFF")}";
            bool enabled = !session.State.IsGameOver;
            for (int i = 0; i < LaborKinds.Length; i++)
            {
                string kind = LaborKinds[i];
                int percent = LaborPercent(progress.labor, kind);
                int people = UrukSubsistenceSystem.AssignedPeople(progress, kind);
                laborValues[i].text =
                    $"{UrukSubsistenceSystem.LaborNameJa(kind)}　{percent}%　" +
                    $"{KnownPeople(progress, people)}";
                laborDownButtons[i].interactable = enabled &&
                    (kind != "food" || percent > 30) && percent > 0;
                laborUpButtons[i].interactable = enabled;
            }
        }

        string BuildDetailText(UrukCampaignProgress progress)
        {
            var social = progress.social;
            return
                "人口収支（学術的推定）\n" +
                $"総人口 {PopulationDisplay(progress)}　出生 {KnownPeople(progress, progress.lastBirths)}\n" +
                $"通常死亡 {KnownPeople(progress, progress.lastNormalDeaths)}　" +
                $"危機死亡 {KnownPeople(progress, progress.lastCrisisDeaths)}\n" +
                $"流入 {KnownPeople(progress, progress.lastMigrationIn)}　" +
                $"流出 {KnownPeople(progress, progress.lastMigrationOut)}\n\n" +
                "長期的役割（現在の公共労働とは別）\n" +
                $"農耕民 {KnownPeople(progress, progress.roles.farmers)}　" +
                $"牧畜民 {KnownPeople(progress, progress.roles.pastoralists)}\n" +
                $"漁民 {KnownPeople(progress, progress.roles.fishers)}　" +
                $"職人 {KnownPeople(progress, progress.roles.artisans)}　" +
                $"神官 {KnownPeople(progress, progress.roles.priests)}　" +
                $"戦士 {KnownPeople(progress, progress.roles.warriors)}\n\n" +
                "食料台帳（1=推定物流単位）\n" +
                $"期首+産出 {KnownUnits(progress, progress.lastFoodProduced)}　" +
                $"消費 {KnownUnits(progress, progress.lastFoodConsumed)}　" +
                $"不足 {KnownUnits(progress, progress.lastFoodShortage)}\n" +
                $"配給損失 {KnownUnits(progress, progress.lastDistributionLoss)}　" +
                $"腐敗・溢出 {KnownUnits(progress, progress.lastFoodSpoiled)}　" +
                $"種籾 {KnownUnits(progress, progress.seedGrain)}\n" +
                $"期末備蓄 {FoodDisplay(progress)}／容量 約{progress.storageCapacityMonths}か月\n\n" +
                "次期収穫予測\n" +
                $"{KnownUnits(progress, progress.lastForecastFood)}単位　" +
                $"労働{progress.lastFoodLaborFactor}% 灌漑{progress.lastIrrigationFactor}% " +
                $"土壌{progress.lastSoilFactor}% 氾濫{progress.lastFloodFactor}% " +
                $"種籾・道具{progress.lastSeedToolFactor}%\n\n" +
                "地位（変化は原因イベントとして記録）\n" +
                $"自由 {KnownPeople(progress, progress.statuses.free)}　" +
                $"従属 {KnownPeople(progress, progress.statuses.dependent)}　" +
                $"債務拘束 {KnownPeople(progress, progress.statuses.debtBound)}\n" +
                $"捕虜 {KnownPeople(progress, progress.statuses.captive)}　" +
                $"奴隷化状態 {KnownPeople(progress, progress.statuses.enslaved)}\n" +
                $"{(string.IsNullOrWhiteSpace(progress.lastStatusEventJa) ? "直近の地位変化なし" : progress.lastStatusEventJa)}\n\n" +
                "社会状態（高いほど良好）\n" +
                $"食料{social?.foodSecurity ?? 0} 労働{social?.laborBurden ?? 0} " +
                $"配給{social?.distributionFairness ?? 0} 格差{social?.inequality ?? 0}\n" +
                $"信頼{social?.leadershipTrust ?? 0} 祭祀{social?.ritualSupport ?? 0} " +
                $"外部{social?.externalSecurity ?? 0} 水利{social?.waterRelations ?? 0} " +
                $"衝撃{social?.recentShock ?? 0}\n" +
                $"重大不安 {progress.consecutiveCriticalUnrest}/3　" +
                $"{progress.lastCriticalUnrestReasonJa}\n\n" +
                $"神殿 {progress.templeStage}/5 " +
                $"{UrukSubsistenceSystem.TempleStageNameJa(progress.templeStage)}　" +
                $"{progress.templeProgress}%\n{progress.lastConstructionReportJa}\n\n" +
                "自動管理\n" +
                $"人口配置 {(progress.populationAutomation ? "ON" : "OFF")}／" +
                $"生産 {(progress.productionAutomation ? "ON" : "OFF")}／" +
                $"交易 {(progress.tradeAutomation ? "ON" : "OFF")}\n\n" +
                "史料表示\n地図・施設配置: 復元推定\n" +
                "人口量: 学術的推定をゲーム用に丸めた値\n" +
                "音響・3Dモデル: 現代制作の暫定復元";
        }

        string BuildAdvisorBody(UrukCampaignProgress progress)
        {
            string risk;
            string actions;
            if (UrukCampaignSystem.CanalCondition(progress) < 60)
            {
                risk = "灌漑不足。運河の堆積で農地の収量が低下。";
                actions = "①運河を整備 ②運河労働を増加 ③食料30%以上を維持";
            }
            else if (UrukSubsistenceSystem.FoodReserveTenthsMonths(progress) < 30)
            {
                risk = "食料備蓄不足。腐敗・配給損失と人口増が備蓄を圧迫。";
                actions = "①食料を優先 ②食料労働を増加 ③種籾を保護";
            }
            else if (progress.stability < 35)
            {
                risk = "共同体の分裂。食料・負担・配給公平の不満が蓄積。";
                actions = "①不足を解消 ②公共労働を軽減 ③危機を2期間避ける";
            }
            else if (progress.templePlanned && progress.templeStage < 5)
            {
                risk = "神殿建設の停滞。建設労働または粘土・葦が必要。";
                actions = "①建設労働を確保 ②必要物資を保護 ③食料安全を維持";
            }
            else
            {
                risk = "都市国家成立条件。人口・灌漑・備蓄・行政を同時に維持。";
                actions = "①食料安全を継続 ②完全灌漑を維持 ③数量記録行政を整備";
            }
            int forecast = UrukSubsistenceSystem.ForecastFoodProduction(progress);
            string forecastText = progress.administrationAdopted
                ? $"次期産出予測 {forecast}単位"
                : $"次期産出予測 約{Math.Max(0, forecast - 2)}～{forecast + 2}単位";
            return $"最大リスク: {risk}\n{actions}\n{forecastText}";
        }

        static string PopulationDisplay(UrukCampaignProgress progress)
        {
            if (progress.administrationAdopted)
                return $"{progress.actualPopulation:N0}人（推定）";
            int lower = Math.Max(0, progress.actualPopulation / 100 * 100 - 100);
            int upper = (progress.actualPopulation / 100 + 1) * 100;
            return $"{lower:N0}～{upper:N0}人";
        }

        static string FoodDisplay(UrukCampaignProgress progress)
        {
            int tenths = UrukSubsistenceSystem.FoodReserveTenthsMonths(progress);
            if (progress.administrationAdopted)
                return $"{tenths / 10}.{tenths % 10}か月分（推定）";
            int lower = tenths / 10;
            int upper = Math.Max(lower + 1, (tenths + 9) / 10);
            return $"約{lower}～{upper}か月分";
        }

        static string KnownPeople(UrukCampaignProgress progress, int value)
        {
            if (progress.administrationAdopted) return $"{Math.Max(0, value):N0}人";
            if (value <= 0) return "0人前後";
            int step = value >= 500 ? 100 : value >= 100 ? 50 : 10;
            int lower = Math.Max(0, value / step * step - step);
            int upper = (value / step + 1) * step;
            return $"{lower:N0}～{upper:N0}人";
        }

        static string KnownUnits(UrukCampaignProgress progress, int value)
        {
            if (progress.administrationAdopted) return Math.Max(0, value).ToString();
            return $"{Math.Max(0, value - 1)}～{Math.Max(1, value + 1)}";
        }

        static int LaborPercent(UrukLaborAllocation labor, string kind)
        {
            return kind switch
            {
                "food" => labor.food,
                "canal" => labor.canal,
                "construction" => labor.construction,
                "crafts" => labor.crafts,
                "trade" => labor.trade,
                "militia" => labor.militia,
                _ => 0,
            };
        }

        static int Good(UrukCampaignProgress progress, string id)
        {
            return UrukCampaignSystem.GoodAmount(progress, id);
        }

        static string Signed(int value)
        {
            if (value == 0) return "±0";
            return value > 0 ? "+" + value : value.ToString();
        }
    }

    /// <summary>農地・運河・神殿基壇の軽量な暫定復元ローポリ表示。</summary>
    public sealed class HistoricalCampaignWorldVisuals : MonoBehaviour
    {
        HistoricalCampaignSession session;
        readonly List<GameObject> generated = new List<GameObject>();
        readonly List<Material> materials = new List<Material>();
        int lastCanalCondition = -1;
        int lastTempleProgress = -1;

        public void Init(HistoricalCampaignSession campaignSession)
        {
            session = campaignSession;
            Rebuild();
        }

        public void Refresh()
        {
            if (session == null) return;
            int canal = UrukCampaignSystem.CanalCondition(session.Progress);
            int temple = session.Progress.templeProgress;
            if (canal == lastCanalCondition && temple == lastTempleProgress) return;
            Rebuild();
        }

        void Rebuild()
        {
            ClearGenerated();
            if (session == null) return;
            var progress = session.Progress;
            foreach (var improvement in progress.improvements)
            {
                var coord = HexCoord.FromOffset(improvement.col, improvement.row);
                var tile = session.State.Map.Get(coord);
                if (tile == null) continue;
                float y = RenderUtil.TileVisualHeight(tile) + 0.09f;
                if (improvement.kind == "farm")
                    BuildFarm(coord.ToWorld(), y);
                else if (improvement.kind == "canal")
                    BuildCanal(coord.ToWorld(), y, improvement.condition);
            }
            if (progress.templePlanned)
            {
                var uruk = session.State.HumanPlayer?.Cities.Count > 0
                    ? session.State.HumanPlayer.Cities[0].Coord
                    : HexCoord.FromOffset(16, 10);
                var tile = session.State.Map.Get(uruk);
                BuildTemple(uruk.ToWorld(),
                    (tile != null ? RenderUtil.TileVisualHeight(tile) : 0f) + 0.11f,
                    progress.templeProgress);
            }
            lastCanalCondition = UrukCampaignSystem.CanalCondition(progress);
            lastTempleProgress = progress.templeProgress;
        }

        void BuildFarm(Vector3 center, float y)
        {
            CreateCube("復元推定_農地", new Vector3(center.x, y, center.z),
                new Vector3(0.78f, 0.035f, 0.62f), new Color(0.54f, 0.46f, 0.18f));
            for (int i = -2; i <= 2; i++)
                CreateCube("畝", new Vector3(center.x + i * 0.13f, y + 0.03f, center.z),
                    new Vector3(0.035f, 0.025f, 0.56f), new Color(0.72f, 0.66f, 0.28f));
        }

        void BuildCanal(Vector3 center, float y, int condition)
        {
            float normalized = Mathf.Clamp01(condition / 100f);
            Color water = Color.Lerp(new Color(0.18f, 0.20f, 0.18f),
                new Color(0.15f, 0.52f, 0.68f), normalized);
            CreateCube("復元推定_運河", new Vector3(center.x, y, center.z),
                new Vector3(0.16f, 0.025f, 0.92f), water);
            CreateCube("運河堤防_左", new Vector3(center.x - 0.12f, y + 0.025f, center.z),
                new Vector3(0.07f, 0.06f, 0.92f), new Color(0.52f, 0.38f, 0.18f));
            CreateCube("運河堤防_右", new Vector3(center.x + 0.12f, y + 0.025f, center.z),
                new Vector3(0.07f, 0.06f, 0.92f), new Color(0.52f, 0.38f, 0.18f));
        }

        void BuildTemple(Vector3 center, float y, int progress)
        {
            float height = 0.08f + 0.28f * Mathf.Clamp01(progress / 100f);
            CreateCube("暫定復元_神殿基壇", new Vector3(center.x, y + height * 0.5f, center.z),
                new Vector3(0.56f, height, 0.48f), new Color(0.70f, 0.54f, 0.27f));
            if (progress >= 60)
                CreateCube("暫定復元_神殿上層",
                    new Vector3(center.x, y + height + 0.09f, center.z),
                    new Vector3(0.34f, 0.16f, 0.29f), new Color(0.82f, 0.67f, 0.36f));
        }

        void CreateCube(string objectName, Vector3 position, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = objectName;
            go.transform.SetParent(transform, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            var collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            var shader = Shader.Find("Sprites/Default");
            var material = new Material(shader) { color = color };
            materials.Add(material);
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            generated.Add(go);
        }

        void ClearGenerated()
        {
            foreach (var go in generated)
                if (go != null) Destroy(go);
            generated.Clear();
            foreach (var material in materials)
                if (material != null) Destroy(material);
            materials.Clear();
        }

        void OnDestroy()
        {
            ClearGenerated();
        }
    }
}
