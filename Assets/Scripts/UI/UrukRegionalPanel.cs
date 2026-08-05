using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using HexCiv.Core;

namespace HexCiv.UI
{
    /// <summary>
    /// ウルク編の水利・作付け・地域交流・水利外交を操作する簡潔な常設パネル。
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
        Button shareWaterButton;
        Button compensateWaterButton;
        Button rejectWaterButton;
        Button breachWaterButton;
        Button renegotiateWaterButton;
        Button nextWaterDisputeButton;
        Button arbitrateWaterButton;
        Button jointLandButton;
        Button compensateLandButton;
        Button mediateLandButton;
        Button rejectLandButton;
        Button breachLandButton;
        Button renegotiateLandButton;
        Button acceptMigrationButton;
        Button rejectMigrationButton;
        Button nextKinshipButton;
        Button proposeKinshipButton;
        Button nextInformationButton;
        Button nextInformationMediumButton;
        Button sendInformationButton;
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
            var latestDiplomacy =
                UrukRegionalSystem.LatestHumanDiplomaticRecord(progress);
            var dispute = UrukRegionalSystem.SelectedWaterCase(progress);
            var openDispute = dispute != null && dispute.status == "open"
                ? dispute : null;
            int openDisputeCount =
                UrukRegionalSystem.OpenWaterDisputeCount(progress);
            int actionableWaterCount =
                UrukRegionalSystem.ActionableWaterCaseCount(progress);
            int selectedWaterOrdinal =
                UrukRegionalSystem.SelectedWaterCaseOrdinal(progress);
            var activeAgreement =
                UrukRegionalSystem.SelectedActiveWaterAgreement(progress);
            var recoverableDispute =
                UrukRegionalSystem.SelectedRecoverableWaterDispute(progress);
            var migration = UrukRegionalSystem.FirstWaitingMigration(progress);
            var openLand = UrukRegionalSystem.SelectedOpenLandDispute(progress);
            var activeLand = UrukRegionalSystem.FirstActiveLandAgreement(progress);
            var recoverableLand =
                UrukRegionalSystem.LatestRecoverableLandDispute(progress);
            var land = openLand ?? activeLand ?? recoverableLand;
            var kinshipCandidate =
                UrukRegionalSystem.SelectedKinshipCandidate(progress);
            var latestKinship =
                UrukRegionalSystem.LatestHumanKinshipTie(progress);
            var informationPartner =
                UrukRegionalSystem.SelectedInformationPartner(progress);
            var latestInformation =
                UrukRegionalSystem.LatestHumanInformationDispatch(progress);
            var latestTransport =
                UrukRegionalSystem.LatestHumanTransport(progress);
            int enRoute = CountTransports(progress, "en_route");

            string farmText = farm == null
                ? "対象農地なし"
                : $"{CropNameJa(farm.crop)}／水{farm.waterReceived}/{farm.waterDemand}／" +
                  $"塩害{farm.salinity}%／前期収穫{farm.lastYield}／" +
                  $"管理:{FactionName(progress, farm.managerFactionId)}／" +
                  $"利用{farm.userFactionIds?.Length ?? 0}共同体";
            string offerText = offer == null
                ? "交易提案なし"
                : OfferText(progress, offer);
            string obligationText = obligation == null
                ? "履行中の契約なし"
                : $"{ContractNameJa(obligation.kind)}: " +
                  $"{FactionName(progress, obligation.debtorFactionId)}→" +
                  $"{FactionName(progress, obligation.creditorFactionId)} " +
                  $"期限{obligation.dueTurn}期／{ObligationStatusJa(obligation.status)}";
            string diplomacyText = latestDiplomacy == null
                ? "外交履歴なし"
                : $"第{latestDiplomacy.turn}期 " +
                  $"{FactionName(progress, latestDiplomacy.counterpartyFactionId)} " +
                  $"{DiplomaticOutcomeJa(latestDiplomacy.outcome)}" +
                  $"（評判{Signed(latestDiplomacy.reputationDelta)}）" +
                  (latestDiplomacy.category == "kinship_tie" ||
                   latestDiplomacy.category == "information_transfer"
                       ? "" : ": " + latestDiplomacy.summaryJa);
            string disputeText = dispute == null
                ? "水利紛争なし"
                : $"水利{WaterStatusJa(dispute.status)}" +
                   $"（対象{selectedWaterOrdinal}/{actionableWaterCount}" +
                   (openDispute != null
                       ? $"・未決{OpenDisputeOrdinal(progress, dispute)}/" +
                         $"{openDisputeCount}" : "") + "）: " +
                   $"{FactionName(progress, dispute.claimantFactionId)}　" +
                   $"不足{dispute.claimantWaterDeficit}／相手水路" +
                   $"{dispute.claimantCanalCondition}%／ウルク流量" +
                   $"{dispute.respondentFlowAtClaim}　" +
                   $"関係:{WaterRelationJa(progress, dispute)}　" +
                   (openDispute != null ? dispute.causeJa : dispute.resultJa) +
                   (string.IsNullOrWhiteSpace(dispute.arbitratorFactionId)
                       ? "" : "　仲裁:" +
                         FactionName(progress, dispute.arbitratorFactionId)) +
                   (string.IsNullOrWhiteSpace(dispute.retaliationJa)
                       ? "" : "　" + dispute.retaliationJa);
            string landText = land == null
                ? "土地・耕作権紛争なし"
                : $"土地{LandStatusJa(land.status)}: " +
                  $"{FactionName(progress, land.claimantFactionId)}→" +
                  $"{FarmNameJa(land.plotId)}　観測 収穫{land.observedYield}・水{land.observedWater}　" +
                  $"根拠:{land.claimantBasisJa}／{land.respondentBasisJa} " +
                  $"（{ConfidenceJa(land.confidence)}）" +
                  (string.IsNullOrWhiteSpace(land.arbitratorFactionId)
                      ? "" : "　仲裁:" +
                        FactionName(progress, land.arbitratorFactionId)) +
                  (string.IsNullOrWhiteSpace(land.retaliationJa)
                      ? "" : "　" + land.retaliationJa);
            string kinshipText = latestKinship == null
                ? "親族連携なし／候補:" +
                  (kinshipCandidate == null ? "なし" :
                    $"{kinshipCandidate.nameJa} 信頼{kinshipCandidate.diplomaticTrust}")
                : $"親族:{FactionName(progress, latestKinship.partnerFactionId)} " +
                  $"{KinshipStatusJa(latestKinship.status)} " +
                  $"氏名不詳／双方同意前提／{ConfidenceJa(latestKinship.confidence)}／" +
                  $"交易危険-{UrukRegionalSystem.KinshipTransportRiskReduction(progress, "uruk_community", latestKinship.partnerFactionId)}%" +
                  (kinshipCandidate == null ? "" :
                    $"／次候補:{kinshipCandidate.nameJa}");
            string informationText =
                $"伝達先:{(informationPartner == null ? "なし" : informationPartner.nameJa)}／" +
                $"媒体:{UrukRegionalSystem.InformationMediumNameJa(progress.selectedInformationMedium)}／" +
                UrukRegionalSystem.InformationRequirementJa(session) +
                (latestInformation == null ? "／伝達履歴なし" :
                    $"／最新:{FactionName(progress, latestInformation.receiverFactionId)} " +
                    $"{InformationStatusJa(latestInformation.status)} " +
                    $"媒体{ConfidenceJa(latestInformation.mediumConfidence)}・" +
                    $"送受信{ConfidenceJa(latestInformation.scenarioConfidence)}・" +
                    $"危険-{UrukRegionalSystem.CommunicationTransportRiskReduction(progress, "uruk_community", latestInformation.receiverFactionId, session.State.TurnNumber)}%・" +
                    $"照合輸送{latestInformation.linkedTransportCount}件");
            string personnelText = latestInformation == null
                ? $"伝達担当の空き: 交易{UrukRegionalSystem.AvailableInformationMessengerLabor(progress)}%・" +
                  $"工芸{UrukRegionalSystem.AvailableInformationRecordLabor(progress)}%"
                : $"担当:{UrukRegionalSystem.InformationPersonnelSummaryJa(latestInformation)}／" +
                  $"空き 交易{UrukRegionalSystem.AvailableInformationMessengerLabor(progress)}%・" +
                  $"工芸{UrukRegionalSystem.AvailableInformationRecordLabor(progress)}%";
            string transportText = latestTransport == null
                ? "最新輸送なし"
                : $"最新輸送:{GoodNameJa(latestTransport.goodId)}" +
                  $"{latestTransport.shippedAmount} " +
                  $"{FactionName(progress, latestTransport.originFactionId)}→" +
                  $"{FactionName(progress, latestTransport.destinationFactionId)}／" +
                  UrukRegionalSystem.TransportForecastJa(latestTransport);
            statusText.text =
                $"水源 {progress.lastRegionalSourceWater}　農地 {progress.lastRegionalFarmWater}　" +
                $"漏水 {progress.lastRegionalLeakage}　未使用 {progress.lastRegionalUnusedWater}\n" +
                $"選択農地: {farmText}\n" +
                $"水路計画 {planned}／工事中 {active}　予約 粘土{progress.reservedClay}・" +
                $"葦{progress.reservedReeds}　予測{progress.estimatedCanalTurns}期\n" +
                $"{offerText}　輸送中{enRoute}\n{obligationText}　" +
                $"移住{(migration == null ? "なし" : migration.people + "人")}\n" +
                $"{disputeText}\n" +
                $"{landText}\n" +
                $"外交評判 {progress.diplomaticReputation}/100　{diplomacyText}\n" +
                kinshipText + "\n" +
                informationText + "\n" +
                personnelText + "\n" +
                transportText;

            bool enabled = !session.State.IsGameOver;
            planButton.interactable = enabled && farm != null &&
                UrukCampaignSystem.GoodAmount(progress, "alluvial_clay") -
                    progress.reservedClay >= 1 &&
                UrukCampaignSystem.GoodAmount(progress, "reeds") -
                    progress.reservedReeds >= 1;
            cancelButton.interactable = enabled && planned > 0;
            acceptOfferButton.interactable = enabled && offer != null;
            negotiateButton.interactable = enabled && openDispute != null;
            shareWaterButton.interactable = enabled && openDispute != null;
            compensateWaterButton.interactable = enabled && openDispute != null &&
                UrukCampaignSystem.GoodAmount(progress, "barley") >= 2;
            rejectWaterButton.interactable = enabled && openDispute != null;
            breachWaterButton.interactable = enabled && activeAgreement != null;
            renegotiateWaterButton.interactable = enabled &&
                recoverableDispute != null &&
                UrukCampaignSystem.GoodAmount(progress, "barley") >= 1 &&
                UrukCampaignSystem.GoodAmount(progress, "reeds") >= 1;
            nextWaterDisputeButton.interactable = enabled &&
                actionableWaterCount > 1;
            arbitrateWaterButton.interactable = enabled && openDispute != null &&
                openDisputeCount > 1 &&
                progress.diplomaticReputation >= 25 &&
                UrukCampaignSystem.GoodAmount(progress, "barley") >= 1;
            jointLandButton.interactable = enabled && openLand != null &&
                progress.labor.food >= 40;
            compensateLandButton.interactable = enabled && openLand != null &&
                UrukCampaignSystem.GoodAmount(progress, "barley") >= 2;
            mediateLandButton.interactable = enabled && openLand != null &&
                progress.diplomaticReputation >= 30 &&
                UrukCampaignSystem.GoodAmount(progress, "barley") >= 1;
            rejectLandButton.interactable = enabled && openLand != null;
            breachLandButton.interactable = enabled && activeLand != null;
            renegotiateLandButton.interactable = enabled &&
                recoverableLand != null &&
                UrukCampaignSystem.GoodAmount(progress, "barley") >= 1 &&
                UrukCampaignSystem.GoodAmount(progress, "alluvial_clay") >= 1;
            acceptMigrationButton.interactable = enabled && migration != null;
            rejectMigrationButton.interactable = enabled && migration != null;
            nextKinshipButton.interactable = enabled && kinshipCandidate != null;
            proposeKinshipButton.interactable = enabled &&
                UrukRegionalSystem.CanProposeKinshipTie(progress);
            nextInformationButton.interactable = enabled &&
                informationPartner != null;
            nextInformationMediumButton.interactable = enabled;
            sendInformationButton.interactable = enabled &&
                UrukRegionalSystem.CanSendInformation(session);
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
                new Vector2(0f, 0f), new Vector2(18f, 18f), new Vector2(720f, 500f));

            var title = UIStyle.CreateText(body.transform, "Title",
                "南メソポタミア地域管理　—　水利・農業・交易・外交・情報", 17,
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
            statusText.resizeTextMaxSize = 12;
            UIStyle.SetRect(statusText.gameObject, new Vector2(0f, 1f),
                new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -43f), new Vector2(-24f, 240f));

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
                    12f + i * 82f, 228f, 74f,
                    () => Apply(overlayIds[index]));
            }
            CreateButton(body.transform, "NextFarm", "農地を切替", 348f, 228f, 94f,
                NextFarm);
            planButton = CreateButton(body.transform, "PlanCanal", "水路を提案",
                448f, 228f, 112f,
                () => Apply(UrukRegionalSystem.PlanCanalAction));
            cancelButton = CreateButton(body.transform, "CancelCanal", "計画取消",
                566f, 228f, 92f,
                () => Apply(UrukRegionalSystem.CancelCanalPlanAction));

            CreateButton(body.transform, "CropBarley", "大麦", 12f, 184f, 70f,
                () => Apply(UrukRegionalSystem.CropBarleyAction));
            CreateButton(body.transform, "CropEmmer", "エンマー", 88f, 184f, 82f,
                () => Apply(UrukRegionalSystem.CropEmmerAction));
            CreateButton(body.transform, "CropFallow", "休耕", 176f, 184f, 70f,
                () => Apply(UrukRegionalSystem.CropFallowAction));
            acceptOfferButton = CreateButton(body.transform, "AcceptOffer", "提案を受諾",
                252f, 184f, 104f,
                () => Apply(UrukRegionalSystem.AcceptOfferAction));
            CreateButton(body.transform, "Gift", "食料贈与", 362f, 184f, 92f,
                () => Apply(UrukRegionalSystem.SendGiftAction));
            CreateButton(body.transform, "Barter", "物々交換", 460f, 184f, 92f,
                () => Apply(UrukRegionalSystem.OfferBarterAction));

            CreateButton(body.transform, "Loan", "穀物貸付", 12f, 140f, 104f,
                () => Apply(UrukRegionalSystem.RequestLoanAction));
            CreateButton(body.transform, "Labor", "労務契約", 122f, 140f, 104f,
                () => Apply(UrukRegionalSystem.OfferLaborAction));
            CreateButton(body.transform, "Access", "通行権", 232f, 140f, 92f,
                () => Apply(UrukRegionalSystem.AcquireAccessAction));
            CreateButton(body.transform, "Tribute", "朝貢を約す", 330f, 140f, 104f,
                () => Apply(UrukRegionalSystem.OfferTributeAction));
            nextWaterDisputeButton = CreateButton(body.transform,
                "NextWaterDispute", "水利対象切替", 444f, 140f, 106f,
                () => Apply(UrukRegionalSystem.NextWaterDisputeAction));
            arbitrateWaterButton = CreateButton(body.transform,
                "ArbitrateWater", "第三者仲裁", 556f, 140f, 102f,
                () => Apply(UrukRegionalSystem.ArbitrateWaterDisputesAction));

            acceptMigrationButton = CreateButton(body.transform, "AcceptMigration",
                "移住を受入", 12f, 88f, 104f,
                () => Apply(UrukRegionalSystem.AcceptMigrationAction));
            rejectMigrationButton = CreateButton(body.transform, "RejectMigration",
                "移住を拒否", 122f, 88f, 104f,
                () => Apply(UrukRegionalSystem.RejectMigrationAction));
            shareWaterButton = CreateButton(body.transform, "ShareWater",
                "分水", 232f, 88f, 74f,
                () => Apply(UrukRegionalSystem.ShareWaterAction));
            compensateWaterButton = CreateButton(body.transform, "CompensateWater",
                "穀物補償", 312f, 88f, 92f,
                () => Apply(UrukRegionalSystem.CompensateWaterAction));
            negotiateButton = CreateButton(body.transform, "JointMaintenance",
                "共同管理", 410f, 88f, 92f,
                () => Apply(UrukRegionalSystem.NegotiateWaterAction));
            rejectWaterButton = CreateButton(body.transform, "RejectWater", "拒否",
                508f, 88f, 64f,
                () => Apply(UrukRegionalSystem.RejectWaterAction));
            breachWaterButton = CreateButton(body.transform, "BreachWater",
                "破約", 578f, 88f, 58f,
                () => Apply(UrukRegionalSystem.BreachWaterAgreementAction));
            renegotiateWaterButton = CreateButton(body.transform, "RenegotiateWater",
                "再交渉", 642f, 88f, 66f,
                () => Apply(UrukRegionalSystem.RenegotiateWaterAction));

            jointLandButton = CreateButton(body.transform, "JointLand",
                "土地共同耕作", 12f, 46f, 108f,
                () => Apply(UrukRegionalSystem.JointCultivationLandAction));
            compensateLandButton = CreateButton(body.transform, "CompensateLand",
                "土地補償", 126f, 46f, 88f,
                () => Apply(UrukRegionalSystem.CompensateLandAction));
            mediateLandButton = CreateButton(body.transform, "MediateLand",
                "土地仲裁", 220f, 46f, 88f,
                () => Apply(UrukRegionalSystem.MediateLandAction));
            rejectLandButton = CreateButton(body.transform, "RejectLand",
                "土地拒否", 314f, 46f, 80f,
                () => Apply(UrukRegionalSystem.RejectLandAction));
            breachLandButton = CreateButton(body.transform, "BreachLand",
                "土地破約", 400f, 46f, 80f,
                () => Apply(UrukRegionalSystem.BreachLandAgreementAction));
            renegotiateLandButton = CreateButton(body.transform, "RenegotiateLand",
                "土地再交渉", 486f, 46f, 92f,
                () => Apply(UrukRegionalSystem.RenegotiateLandAction));
            nextKinshipButton = CreateButton(body.transform, "NextKinship",
                "親族候補", 584f, 46f, 62f,
                () => Apply(UrukRegionalSystem.NextKinshipPartnerAction));
            proposeKinshipButton = CreateButton(body.transform, "ProposeKinship",
                "連携提案", 652f, 46f, 56f,
                () => Apply(UrukRegionalSystem.ProposeKinshipTieAction));
            nextInformationButton = CreateButton(body.transform,
                "NextInformation", "伝達先", 12f, 2f, 112f,
                () => Apply(UrukRegionalSystem.NextInformationPartnerAction));
            nextInformationMediumButton = CreateButton(body.transform,
                "NextInformationMedium", "媒体切替", 130f, 2f, 112f,
                () => Apply(UrukRegionalSystem.NextInformationMediumAction));
            sendInformationButton = CreateButton(body.transform,
                "SendInformation", "情報を送る", 248f, 2f, 112f,
                () => Apply(UrukRegionalSystem.SendInformationAction));
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

        static string FarmNameJa(string id) => id switch
        {
            "uruk_north_farm" => "ウルク北農地",
            "uruk_west_farm" => "ウルク西農地",
            "eridu_hinterland_farm" => "エリドゥ後背農地",
            "ur_hinterland_farm" => "ウル後背農地",
            "lagash_hinterland_farm" => "ラガシュ後背農地",
            _ => id,
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

        static string DiplomaticOutcomeJa(string outcome) => outcome switch
        {
            "agreed" => "合意",
            "completed" => "履行",
            "defaulted" => "不履行",
            "expired" => "期限満了",
            "negotiated" => "交渉成立",
            "water_shared" => "分水合意",
            "compensated" => "穀物補償",
            "joint_maintenance" => "共同補修",
            "joint_management_started" => "共同管理開始",
            "joint_management_completed" => "共同管理完了",
            "water_share_completed" => "分水履行",
            "water_share_defaulted" => "分水不履行",
            "water_agreement_breached" => "水利合意破約",
            "water_renegotiated" => "水利再交渉",
            "water_arbitration_award" => "第三者仲裁",
            "water_arbitration_completed" => "仲裁分水履行",
            "water_arbitration_defaulted" => "仲裁分水不履行",
            "land_joint_cultivation_started" => "共同耕作開始",
            "land_compensated" => "土地補償",
            "land_mediation_award" => "土地仲裁",
            "land_rejected" => "土地要求拒否",
            "land_agreement_completed" => "耕作権合意履行",
            "land_agreement_defaulted" => "耕作権合意不履行",
            "land_agreement_breached" => "耕作権合意破約",
            "land_renegotiated" => "土地再交渉",
            "kinship_tie_formed" => "親族連携成立",
            "kinship_tie_established" => "親族連携定着",
            "information_received" => "情報到着",
            "information_failed" => "伝達不一致",
            "rejected" => "要求拒否",
            _ => outcome,
        };

        static string KinshipStatusJa(string status) => status switch
        {
            "active" => "履行中",
            "established" => "定着",
            _ => status,
        };

        static string InformationStatusJa(string status) => status switch
        {
            "pending" => "到着待ち",
            "active" => "照合済み",
            "failed" => "伝達不一致",
            "archived" => "有効期間終了",
            _ => status,
        };

        static int OpenDisputeOrdinal(UrukCampaignProgress progress,
            UrukWaterDisputeState selected)
        {
            int ordinal = 0;
            foreach (var dispute in progress.waterDisputes)
            {
                if (dispute == null || dispute.respondentFactionId !=
                        "uruk_community" || dispute.status != "open") continue;
                ordinal++;
                if (dispute == selected) return ordinal;
            }
            return Math.Max(1, ordinal);
        }

        static string WaterRelationJa(UrukCampaignProgress progress,
            UrukWaterDisputeState dispute)
        {
            if (dispute == null) return "不明";
            return $"{FactionName(progress, dispute.upstreamFactionId)}上流→" +
                $"{FactionName(progress, dispute.downstreamFactionId)}下流" +
                $"（{ConfidenceJa(dispute.confidence)}）";
        }

        static string WaterStatusJa(string status) => status switch
        {
            "open" => "要求",
            "shared" => "分水履行中",
            "jointly_managed" => "共同管理中",
            "compensated" => "補償済み",
            "rejected" => "拒否",
            "breached" => "破約",
            "completed" => "合意完了",
            "defaulted" => "不履行",
            _ => status,
        };

        static string LandStatusJa(string status) => status switch
        {
            "open" => "要求",
            "jointly_cultivated" => "共同耕作中",
            "mediated" => "仲裁利用中",
            "compensated" => "補償済み",
            "rejected" => "拒否",
            "breached" => "破約",
            "completed" => "合意完了",
            "defaulted" => "不履行",
            _ => status,
        };

        static string Signed(int value) => value > 0 ? "+" + value : value.ToString();
    }
}
