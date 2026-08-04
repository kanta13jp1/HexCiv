using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using HexCiv.Core;

namespace HexCiv.UI
{
    /// <summary>
    /// ウルク編 Stage 4A の水利・作付け・地域交流を操作する簡潔な常設パネル。
    /// 史実確度と予測値を同じ場所に示し、詳細計算は Core に委譲する。
    /// </summary>
    public sealed class UrukRegionalPanel : MonoBehaviour
    {
        HistoricalCampaignSession session;
        Action<string> onAction;
        Canvas canvas;
        GameObject body;
        Text statusText;
        Button planButton;
        Button cancelButton;
        Button acceptOfferButton;
        Button negotiateButton;
        Button acceptMigrationButton;
        Button rejectMigrationButton;
        readonly Button[] overlayButtons = new Button[4];

        public void Init(HistoricalCampaignSession campaignSession,
            Action<string> campaignAction)
        {
            session = campaignSession ?? throw new ArgumentNullException(
                nameof(campaignSession));
            onAction = campaignAction;
            if (canvas == null) BuildUi();
            Refresh();
        }

        public void Refresh()
        {
            if (session == null || statusText == null) return;
            var progress = session.Progress;
            var farm = SelectedFarm(progress);
            int planned = CountProjects(progress, "planned");
            int active = CountProjects(progress, "active");
            var offer = UrukRegionalSystem.FirstOpenOffer(progress);
            var obligation = UrukRegionalSystem.FirstHumanObligation(progress);
            var dispute = UrukRegionalSystem.FirstOpenDispute(progress);
            var migration = UrukRegionalSystem.FirstWaitingMigration(progress);
            int enRoute = CountTransports(progress, "en_route");

            string farmText = farm == null
                ? "対象農地なし"
                : $"{CropNameJa(farm.crop)}／水{farm.waterReceived}/{farm.waterDemand}／" +
                  $"塩害{farm.salinity}%／前期収穫{farm.lastYield}";
            string offerText = offer == null
                ? "交易提案なし"
                : OfferText(progress, offer);
            string obligationText = obligation == null
                ? "履行中の契約なし"
                : $"{ContractNameJa(obligation.kind)}: " +
                  $"{FactionName(progress, obligation.debtorFactionId)}→" +
                  $"{FactionName(progress, obligation.creditorFactionId)} " +
                  $"期限{obligation.dueTurn}期／{ObligationStatusJa(obligation.status)}";
            statusText.text =
                $"水源 {progress.lastRegionalSourceWater}　農地 {progress.lastRegionalFarmWater}　" +
                $"漏水 {progress.lastRegionalLeakage}　未使用 {progress.lastRegionalUnusedWater}\n" +
                $"選択農地: {farmText}\n" +
                $"水路計画 {planned}／工事中 {active}　予約 粘土{progress.reservedClay}・" +
                $"葦{progress.reservedReeds}　予測{progress.estimatedCanalTurns}期\n" +
                $"{offerText}　輸送中{enRoute}\n{obligationText}　" +
                $"水利紛争{(dispute == null ? "なし" : "交渉可")}　" +
                $"移住{(migration == null ? "なし" : migration.people + "人")}";

            bool enabled = !session.State.IsGameOver;
            planButton.interactable = enabled && farm != null &&
                UrukCampaignSystem.GoodAmount(progress, "alluvial_clay") -
                    progress.reservedClay >= 1 &&
                UrukCampaignSystem.GoodAmount(progress, "reeds") -
                    progress.reservedReeds >= 1;
            cancelButton.interactable = enabled && planned > 0;
            acceptOfferButton.interactable = enabled && offer != null;
            negotiateButton.interactable = enabled && dispute != null;
            acceptMigrationButton.interactable = enabled && migration != null;
            rejectMigrationButton.interactable = enabled && migration != null;
            for (int i = 0; i < overlayButtons.Length; i++)
            {
                var colors = overlayButtons[i].colors;
                colors.normalColor = i == progress.regionalOverlayMode
                    ? new Color(0.75f, 0.60f, 0.20f)
                    : new Color(0.30f, 0.38f, 0.52f);
                overlayButtons[i].colors = colors;
            }
        }

        void BuildUi()
        {
            var canvasGo = new GameObject("UrukRegionalCanvas", typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 123;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            if (FindFirstObjectByType<EventSystem>() == null)
                new GameObject("UrukRegionalEventSystem", typeof(EventSystem),
                    typeof(StandaloneInputModule)).transform.SetParent(transform, false);

            body = UIStyle.CreatePanel(canvasGo.transform, "RegionalBody",
                new Color(0.025f, 0.04f, 0.07f, 0.95f));
            UIStyle.SetRect(body, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(18f, 18f), new Vector2(720f, 346f));

            var title = UIStyle.CreateText(body.transform, "Title",
                "南メソポタミア地域管理　—　水利・農業・移動", 17,
                TextAnchor.MiddleLeft, UIStyle.Accent);
            title.fontStyle = FontStyle.Bold;
            UIStyle.SetRect(title.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(-38f, -8f),
                new Vector2(-92f, 32f));
            var collapse = UIStyle.CreateButton(body.transform, "Collapse",
                "更新", 12, Refresh);
            UIStyle.SetRect(collapse.gameObject, new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-12f, -8f), new Vector2(72f, 30f));

            statusText = UIStyle.CreateText(body.transform, "RegionalStatus", "", 12,
                TextAnchor.UpperLeft, UIStyle.TextMain);
            statusText.horizontalOverflow = HorizontalWrapMode.Wrap;
            statusText.resizeTextForBestFit = true;
            statusText.resizeTextMinSize = 10;
            UIStyle.SetRect(statusText.gameObject, new Vector2(0f, 1f),
                new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -43f), new Vector2(-24f, 92f));

            string[] overlayNames = { "通常", "水利", "農地", "物流" };
            string[] overlayIds =
            {
                "regional_overlay_none", "regional_overlay_water",
                "regional_overlay_farm", "regional_overlay_logistics",
            };
            for (int i = 0; i < overlayButtons.Length; i++)
            {
                int index = i;
                overlayButtons[i] = CreateButton(body.transform,
                    "Overlay_" + overlayNames[i], overlayNames[i],
                    12f + i * 82f, 184f, 74f,
                    () => Apply(overlayIds[index]));
            }
            CreateButton(body.transform, "NextFarm", "農地を切替", 348f, 184f, 94f,
                NextFarm);
            planButton = CreateButton(body.transform, "PlanCanal", "水路を提案",
                448f, 184f, 112f,
                () => Apply(UrukRegionalSystem.PlanCanalAction));
            cancelButton = CreateButton(body.transform, "CancelCanal", "計画取消",
                566f, 184f, 92f,
                () => Apply(UrukRegionalSystem.CancelCanalPlanAction));

            CreateButton(body.transform, "CropBarley", "大麦", 12f, 140f, 70f,
                () => Apply(UrukRegionalSystem.CropBarleyAction));
            CreateButton(body.transform, "CropEmmer", "エンマー", 88f, 140f, 82f,
                () => Apply(UrukRegionalSystem.CropEmmerAction));
            CreateButton(body.transform, "CropFallow", "休耕", 176f, 140f, 70f,
                () => Apply(UrukRegionalSystem.CropFallowAction));
            acceptOfferButton = CreateButton(body.transform, "AcceptOffer", "提案を受諾",
                252f, 140f, 104f,
                () => Apply(UrukRegionalSystem.AcceptOfferAction));
            CreateButton(body.transform, "Gift", "食料贈与", 362f, 140f, 92f,
                () => Apply(UrukRegionalSystem.SendGiftAction));
            CreateButton(body.transform, "Barter", "物々交換", 460f, 140f, 92f,
                () => Apply(UrukRegionalSystem.OfferBarterAction));
            negotiateButton = CreateButton(body.transform, "Negotiate", "水利交渉",
                558f, 140f, 92f,
                () => Apply(UrukRegionalSystem.NegotiateWaterAction));

            CreateButton(body.transform, "Loan", "穀物貸付", 12f, 96f, 104f,
                () => Apply(UrukRegionalSystem.RequestLoanAction));
            CreateButton(body.transform, "Labor", "労務契約", 122f, 96f, 104f,
                () => Apply(UrukRegionalSystem.OfferLaborAction));
            CreateButton(body.transform, "Access", "通行権", 232f, 96f, 92f,
                () => Apply(UrukRegionalSystem.AcquireAccessAction));
            CreateButton(body.transform, "Tribute", "朝貢を約す", 330f, 96f, 104f,
                () => Apply(UrukRegionalSystem.OfferTributeAction));
            var contractNote = UIStyle.CreateText(body.transform, "ContractNote",
                "貨幣なし：現物・労務・期限・輸送損失を契約ごとに記録", 10,
                TextAnchor.MiddleLeft, UIStyle.TextDim);
            UIStyle.SetRect(contractNote.gameObject, new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(444f, 96f), new Vector2(214f, 36f));

            acceptMigrationButton = CreateButton(body.transform, "AcceptMigration",
                "移住を受入", 12f, 44f, 104f,
                () => Apply(UrukRegionalSystem.AcceptMigrationAction));
            rejectMigrationButton = CreateButton(body.transform, "RejectMigration",
                "移住を拒否", 122f, 44f, 104f,
                () => Apply(UrukRegionalSystem.RejectMigrationAction));
            var note = UIStyle.CreateText(body.transform, "RegionalNote",
                "計画はターン終了まで取消可能。資源は予約され、確定後に消費されます。",
                11, TextAnchor.MiddleLeft, UIStyle.TextDim);
            UIStyle.SetRect(note.gameObject, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0.5f, 0f), new Vector2(232f, 10f),
                new Vector2(-250f, 28f));
        }

        Button CreateButton(Transform parent, string name, string label, float x,
            float y, float width, UnityEngine.Events.UnityAction onClick)
        {
            var button = UIStyle.CreateButton(parent, name, label, 12, onClick);
            UIStyle.SetRect(button.gameObject, new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(x, y), new Vector2(width, 36f));
            return button;
        }

        void Apply(string actionId)
        {
            onAction?.Invoke(actionId);
            Refresh();
        }

        void NextFarm()
        {
            var progress = session.Progress;
            int current = -1;
            int first = -1;
            for (int i = 0; i < progress.farmPlots.Length; i++)
            {
                if (progress.farmPlots[i].ownerFactionId != "uruk_community") continue;
                if (first < 0) first = i;
                if (progress.farmPlots[i].id == progress.selectedFarmId)
                    current = i;
            }
            int next = first;
            for (int offset = 1; offset <= progress.farmPlots.Length; offset++)
            {
                int index = (Math.Max(current, first) + offset) %
                    progress.farmPlots.Length;
                if (progress.farmPlots[index].ownerFactionId == "uruk_community")
                {
                    next = index;
                    break;
                }
            }
            if (next >= 0)
                Apply("regional_select_farm_" + progress.farmPlots[next].id);
        }

        static UrukFarmPlotState SelectedFarm(UrukCampaignProgress progress)
        {
            UrukFarmPlotState first = null;
            foreach (var farm in progress.farmPlots)
            {
                if (farm.ownerFactionId != "uruk_community") continue;
                first ??= farm;
                if (farm.id == progress.selectedFarmId) return farm;
            }
            return first;
        }

        static int CountProjects(UrukCampaignProgress progress, string status)
        {
            int count = 0;
            foreach (var project in progress.constructionProjects)
                if (project.status == status) count++;
            return count;
        }

        static int CountTransports(UrukCampaignProgress progress, string status)
        {
            int count = 0;
            foreach (var transport in progress.transports)
                if (transport.status == status) count++;
            return count;
        }

        static string FactionName(UrukCampaignProgress progress, string factionId)
        {
            var faction = UrukRegionalSystem.FindFaction(progress, factionId);
            return faction?.nameJa ?? factionId;
        }

        static string CropNameJa(string crop) => crop switch
        {
            "barley" => "大麦",
            "emmer" => "エンマー小麦",
            "fallow" => "休耕",
            _ => crop,
        };

        static string GoodNameJa(string good) => good switch
        {
            "barley" => "大麦",
            "emmer_wheat" => "エンマー小麦",
            "fish" => "魚",
            "reeds" => "葦",
            "alluvial_clay" => "沖積粘土",
            "copper" => "銅",
            "labor_service" => "労務",
            "access_right" => "通行権",
            _ => string.IsNullOrWhiteSpace(good) ? "なし" : good,
        };

        static string ConfidenceJa(string confidence) => confidence switch
        {
            "certain" => "確実",
            "probable" => "有力説",
            "inferred" => "推定",
            _ => confidence,
        };

        static string OfferText(UrukCampaignProgress progress,
            UrukTradeOfferState offer)
        {
            string parties = $"{FactionName(progress, offer.proposerFactionId)}→" +
                FactionName(progress, offer.receiverFactionId);
            string terms = offer.contractKind switch
            {
                "loan" => $"{GoodNameJa(offer.offeredGoodId)}{offer.offeredAmount}貸付／" +
                    $"{offer.requestedAmount}返済",
                "tribute" => $"{GoodNameJa(offer.offeredGoodId)}{offer.offeredAmount}を" +
                    $"{Math.Max(1, offer.installmentCount)}回",
                _ => $"{GoodNameJa(offer.offeredGoodId)}{offer.offeredAmount} ↔ " +
                    $"{GoodNameJa(offer.requestedGoodId)}{offer.requestedAmount}",
            };
            return $"{ContractNameJa(offer.contractKind)} {parties}: {terms} " +
                $"（{ConfidenceJa(offer.confidence)}）";
        }

        static string ContractNameJa(string kind) => kind switch
        {
            "gift" => "贈与",
            "barter" => "物々交換",
            "loan" => "貸付",
            "loan_repayment" => "貸付返済",
            "labor" => "労務",
            "labor_service" => "労務履行",
            "access" => "通行権",
            "access_right" => "通行権",
            "tribute" => "朝貢",
            _ => kind,
        };

        static string ObligationStatusJa(string status) => status switch
        {
            "active" => "履行待ち",
            "in_transit" => "輸送中",
            "completed" => "完了",
            "defaulted" => "不履行",
            "expired" => "期限満了",
            _ => status,
        };
    }
}
