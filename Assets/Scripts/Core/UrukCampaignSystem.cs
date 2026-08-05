using System;
using System.Collections.Generic;

namespace HexCiv.Core
{
    public enum UrukFloodTrend
    {
        Drought = 0,
        Stable = 1,
        Beneficial = 2,
        Severe = 3,
    }

    [Serializable]
    public sealed class HistoricalImprovementState
    {
        public string id;
        public string kind;
        public int col;
        public int row;
        public int condition;
    }

    /// <summary>
    /// 既存4Xの抽象人口とは分離した、ウルク縦切り版の実人数・物資・導入進捗。
    /// JsonUtilityで決定的に保存するため、辞書ではなく定義順の配列を使う。
    /// </summary>
    [Serializable]
    public sealed class UrukCampaignProgress
    {
        public int version = 9;
        public int actualPopulation;
        public HistoricalPopulationRoles roles;
        public HistoricalPopulationStatuses statuses;
        public HistoricalGoodAmount[] stockpiles;
        public HistoricalImprovementState[] improvements;
        public UrukLaborAllocation labor;
        public string laborAutomationPolicy;
        public int seedGrain;
        public int storageCapacityMonths;
        public UrukSocialFactors social;
        public UrukPeriodEvent[] events;
        public UrukRegionalFactionState[] regionalFactions;
        public UrukCanalSegmentState[] canalSegments;
        public UrukFarmPlotState[] farmPlots;
        public UrukConstructionProjectState[] constructionProjects;
        public UrukTradeOfferState[] tradeOffers;
        public UrukTransportState[] transports;
        public UrukObligationState[] obligations;
        public UrukDiplomaticRecordState[] diplomaticRecords;
        public UrukMigrationGroupState[] migrationGroups;
        public UrukWaterDisputeState[] waterDisputes;
        public UrukLandDisputeState[] landDisputes;
        public int diplomaticReputation;
        public int nextRegionalId = 1;
        public int regionalRevision;
        public int regionalOverlayMode;
        public int selectedIntakeCol = -1;
        public int selectedIntakeRow = -1;
        public string selectedFarmId;
        public string selectedWaterDisputeId;
        public string selectedLandDisputeId;
        public int reservedClay;
        public int reservedReeds;
        public int estimatedCanalTurns;
        public int lastRegionalSourceWater;
        public int lastRegionalFarmWater;
        public int lastRegionalLeakage;
        public int lastRegionalUnusedWater;
        public int lastRegionalHumanYield;
        public int stability;
        public int consecutiveCriticalUnrest;
        public int currentFloodTrend = (int)UrukFloodTrend.Stable;
        public int lastFoodProduced;
        public int lastFoodConsumed;
        public int lastFoodShortage;
        public int lastDistributionLoss;
        public int lastFoodSpoiled;
        public int lastFoodCarryover;
        public int lastSeedUsed;
        public int lastFoodLaborFactor;
        public int lastIrrigationFactor;
        public int lastFloodFactor;
        public int lastSoilFactor;
        public int lastSeedToolFactor;
        public int lastForecastFood;
        public int lastPopulationChange;
        public int lastBirths;
        public int lastNormalDeaths;
        public int lastCrisisDeaths;
        public int lastMigrationIn;
        public int lastMigrationOut;
        public int lastCanalActionTurn;
        public int lastFoodPriorityTurn;
        public bool populationAutomation;
        public bool productionAutomation;
        public bool tradeAutomation;
        public bool templePlanned;
        public int templeProgress;
        public int templeStage;
        public int recordKnowledgeElements;
        public bool administrationAdopted;
        public bool foodSecureThisPeriod;
        public int consecutiveFoodSecurePeriods;
        public bool isCityState;
        public int cityStateFoundedTurn;
        public int tributaryCount;
        public int permanentTradeRoutes;
        public int importedGoodKinds;
        public int culturalReach;
        public int knowledgeMilestones;
        public bool introTutorialCompleted;
        public string lastStatusEventJa;
        public string lastConstructionReportJa;
        public string lastCriticalUnrestReasonJa;
        public string lastReportJa;
    }

    public static class UrukCampaignSystem
    {
        public const string MaintainCanalAction = "maintain_canal";
        public const string PrioritizeFoodAction = "prioritize_food";
        public const string PlanTempleAction = "plan_temple";
        public const string AdoptAdministrationAction = "adopt_administration";

        public const int CityStatePopulation = 2500;
        public const int CityStateTempleProgress = 100;
        public const int TempleCompletionProgress = 100;
        public const int AdministrationUnlockProgress = 100;
        public const int MinimumCommunityPopulation = 500;
        public const int CriticalStability = 20;
        public const int CriticalUnrestPeriodsForDefeat = 3;

        public static UrukCampaignProgress CreateInitialProgress(
            HistoricalCampaignDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            HistoricalCampaignValidator.ThrowIfInvalid(definition);
            var starting = definition.startingScenario;
            var progress = new UrukCampaignProgress
            {
                version = 9,
                actualPopulation = starting.actualPopulation,
                roles = CopyRoles(starting.roles),
                statuses = CopyStatuses(starting.statuses),
                stockpiles = CopyStockpiles(starting.stockpiles),
                improvements = CopyImprovements(starting.improvements),
                labor = new UrukLaborAllocation(),
                laborAutomationPolicy = "food_security",
                seedGrain = 2,
                storageCapacityMonths = UrukSubsistenceSystem.InitialStorageMonths,
                social = null,
                events = Array.Empty<UrukPeriodEvent>(),
                stability = starting.stability,
                populationAutomation = starting.populationAutomation,
                productionAutomation = starting.productionAutomation,
                tradeAutomation = starting.tradeAutomation,
                currentFloodTrend = (int)UrukFloodTrend.Stable,
                recordKnowledgeElements = 3,
                diplomaticReputation = 50,
                lastReportJa = "小集落ウルク。食料備蓄はわずかで、運河は堆積により十分に機能していない。",
            };
            UrukSubsistenceSystem.EnsureDefaults(progress);
            UrukRegionalSystem.EnsureInitialized(definition, progress);
            ValidateProgress(definition, progress);
            return progress;
        }

        /// <summary>
        /// 旧セーブを、推定値を明示した現行地域運営段階へ補完する。
        /// 旧JSONに存在しない値だけを設定し、既存人口・備蓄・施設進捗は保持する。
        /// </summary>
        public static void MigrateProgress(HistoricalCampaignDefinition definition,
            UrukCampaignProgress progress)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            if (progress.version == 1)
            {
                progress.labor = new UrukLaborAllocation();
                progress.laborAutomationPolicy = "food_security";
                progress.seedGrain = 2;
                progress.storageCapacityMonths =
                    UrukSubsistenceSystem.InitialStorageMonths;
                progress.events = Array.Empty<UrukPeriodEvent>();
                progress.recordKnowledgeElements = 3;
                if (progress.templePlanned)
                {
                    progress.templeStage = progress.templeProgress >= 100 ? 5 :
                        progress.templeProgress >= 75 ? 4 :
                        progress.templeProgress >= 45 ? 3 :
                        progress.templeProgress >= 20 ? 2 : 1;
                }
            }
            if (progress.version == 1 || progress.version == 2)
                progress.version = 3;
            if (progress.version == 3)
            {
                progress.obligations = Array.Empty<UrukObligationState>();
                progress.version = 4;
            }
            if (progress.version == 4)
            {
                progress.diplomaticRecords =
                    Array.Empty<UrukDiplomaticRecordState>();
                progress.diplomaticReputation = 50;
                progress.version = 5;
            }
            if (progress.version == 5)
            {
                UrukRegionalSystem.MigrateWaterDisputesV6(progress);
                progress.version = 6;
            }
            if (progress.version == 6)
            {
                UrukRegionalSystem.MigrateWaterDisputesV7(progress);
                progress.version = 7;
            }
            if (progress.version == 7)
            {
                UrukRegionalSystem.MigrateWaterDisputesV8(progress);
                progress.version = 8;
            }
            if (progress.version == 8)
            {
                UrukRegionalSystem.MigrateLandRightsV9(progress);
                progress.version = 9;
            }
            UrukSubsistenceSystem.EnsureDefaults(progress);
            UrukRegionalSystem.EnsureInitialized(definition, progress);
        }

        public static void ValidateProgress(HistoricalCampaignDefinition definition,
            UrukCampaignProgress progress)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            if (progress.version != 9)
                throw new InvalidOperationException("ウルク進捗versionが不正");
            if (progress.actualPopulation < 0)
                throw new InvalidOperationException("実人口が負数");
            if (progress.roles == null || progress.roles.Total != progress.actualPopulation)
                throw new InvalidOperationException("役割別人口の合計が実人口と一致しない");
            if (progress.statuses == null || progress.statuses.Total != progress.actualPopulation)
                throw new InvalidOperationException("地位別人口の合計が実人口と一致しない");
            if (progress.statuses.free < 0 || progress.statuses.dependent < 0 ||
                progress.statuses.debtBound < 0 || progress.statuses.captive < 0 ||
                progress.statuses.enslaved < 0)
                throw new InvalidOperationException("地位別人口が負数");
            if (progress.stockpiles == null ||
                progress.stockpiles.Length != definition.goods.Length)
                throw new InvalidOperationException("物資備蓄数が台帳と一致しない");
            var ids = new HashSet<string>();
            for (int i = 0; i < progress.stockpiles.Length; i++)
            {
                var stock = progress.stockpiles[i];
                if (stock == null || string.IsNullOrWhiteSpace(stock.id) ||
                    stock.amount < 0 || !ids.Add(stock.id))
                    throw new InvalidOperationException("物資備蓄が不正");
                if (FindGood(definition, stock.id) == null)
                    throw new InvalidOperationException("台帳にない物資備蓄: " + stock.id);
            }
            if (progress.improvements == null || progress.improvements.Length < 3)
                throw new InvalidOperationException("開始施設進捗が不足");
            for (int i = 0; i < progress.improvements.Length; i++)
            {
                var improvement = progress.improvements[i];
                if (improvement == null || improvement.condition < 0 ||
                    improvement.condition > 100)
                    throw new InvalidOperationException("施設進捗が不正");
            }
            UrukSubsistenceSystem.EnsureDefaults(progress);
            if (progress.labor.Total != 100 ||
                progress.labor.food < 30 ||
                progress.labor.food % UrukSubsistenceSystem.LaborStep != 0 ||
                progress.labor.canal % UrukSubsistenceSystem.LaborStep != 0 ||
                progress.labor.construction % UrukSubsistenceSystem.LaborStep != 0 ||
                progress.labor.crafts % UrukSubsistenceSystem.LaborStep != 0 ||
                progress.labor.trade % UrukSubsistenceSystem.LaborStep != 0 ||
                progress.labor.militia % UrukSubsistenceSystem.LaborStep != 0)
                throw new InvalidOperationException("労働配分が5%刻み・合計100%ではない");
            if (progress.seedGrain < 0 || progress.storageCapacityMonths <= 0 ||
                progress.consecutiveFoodSecurePeriods < 0)
                throw new InvalidOperationException("食料台帳の値が不正");
            if (progress.lastBirths < 0 || progress.lastNormalDeaths < 0 ||
                progress.lastCrisisDeaths < 0 || progress.lastMigrationIn < 0 ||
                progress.lastMigrationOut < 0)
                throw new InvalidOperationException("人口変動の内訳が不正");
            if (progress.social == null ||
                !Percent(progress.social.foodSecurity) ||
                !Percent(progress.social.laborBurden) ||
                !Percent(progress.social.distributionFairness) ||
                !Percent(progress.social.inequality) ||
                !Percent(progress.social.leadershipTrust) ||
                !Percent(progress.social.ritualSupport) ||
                !Percent(progress.social.externalSecurity) ||
                !Percent(progress.social.waterRelations) ||
                !Percent(progress.social.recentShock))
                throw new InvalidOperationException("社会状態の内訳が不正");
            if (progress.events == null)
                throw new InvalidOperationException("期間イベント履歴がnull");
            foreach (var periodEvent in progress.events)
                if (periodEvent == null || periodEvent.turn < 0 ||
                    periodEvent.people < 0 ||
                    string.IsNullOrWhiteSpace(periodEvent.kind))
                    throw new InvalidOperationException("期間イベント履歴が不正");
            if (progress.templeStage < 0 || progress.templeStage > 5)
                throw new InvalidOperationException("神殿建設段階が不正");
            if (!progress.templePlanned &&
                (progress.templeStage != 0 || progress.templeProgress != 0))
                throw new InvalidOperationException("未着工の神殿進捗が不正");
            if (progress.administrationAdopted &&
                (progress.templeStage < 5 ||
                 progress.templeProgress < AdministrationUnlockProgress))
                throw new InvalidOperationException("数量記録行政の前提が不足");
            progress.stability = Math.Clamp(progress.stability, 0, 100);
            progress.templeProgress = Math.Clamp(progress.templeProgress, 0,
                TempleCompletionProgress);
            UrukRegionalSystem.Validate(definition, progress);
        }

        static bool Percent(int value) => value >= 0 && value <= 100;

        public static bool TryApplyAction(HistoricalCampaignSession session, string actionId,
            out string resultJa)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            var state = session.State;
            var progress = session.Progress;
            if (state.IsGameOver)
            {
                resultJa = "共同体の年代記はすでに閉じられている。";
                return false;
            }

            int turn = state.TurnNumber;
            switch (actionId)
            {
                case MaintainCanalAction:
                    if (progress.lastCanalActionTurn == turn)
                    {
                        resultJa = "この期間の運河維持はすでに割り当て済み。";
                        return false;
                    }
                    if (!TryConsumeGood(progress, "reeds", 1))
                    {
                        resultJa = "運河補修に必要な葦が不足している。";
                        return false;
                    }
                    progress.lastCanalActionTurn = turn;
                    SetCanalCondition(progress, CanalCondition(progress) + 40);
                    resultJa = "労働力と葦を運河へ配分した。水路の堆積を除き、農地へ水を導く。";
                    break;

                case PrioritizeFoodAction:
                    if (progress.lastFoodPriorityTurn == turn)
                    {
                        resultJa = "この期間はすでに食料優先の人口配置になっている。";
                        return false;
                    }
                    progress.lastFoodPriorityTurn = turn;
                    progress.populationAutomation = true;
                    progress.laborAutomationPolicy = "food_security";
                    UrukSubsistenceSystem.ApplyFoodSecurityAutomation(progress);
                    resultJa = "顧問が公共労働を食料安全優先へ再配分した。強制労働や権利変更は行わない。";
                    break;

                case PlanTempleAction:
                    if (progress.templePlanned)
                    {
                        resultJa = "神殿区画はすでに建設中。";
                        return false;
                    }
                    if (!HasGood(progress, "alluvial_clay", 1) ||
                        !HasGood(progress, "reeds", 1))
                    {
                        resultJa = "初期神殿区画の敷地準備には沖積粘土1・葦1が必要。";
                        return false;
                    }
                    TryConsumeGood(progress, "alluvial_clay", 1);
                    TryConsumeGood(progress, "reeds", 1);
                    progress.templePlanned = true;
                    progress.templeProgress = 5;
                    progress.templeStage = 1;
                    progress.lastConstructionReportJa =
                        "初期神殿区画（復元推定）: 敷地選定と準備を開始。";
                    resultJa = "敷地を定め、初期神殿区画の5段階建設を始めた（ジッグラトではない）。";
                    break;

                case AdoptAdministrationAction:
                    if (progress.administrationAdopted)
                    {
                        resultJa = "配給・記録制度はすでに採用済み。";
                        return false;
                    }
                    if (progress.templeProgress < AdministrationUnlockProgress ||
                        progress.templeStage < 5)
                    {
                        resultJa = "神殿の祭祀・貯蔵・配給段階が完成すると数量記録行政を採用できる。";
                        return false;
                    }
                    if (progress.recordKnowledgeElements < 3)
                    {
                        resultJa = "トークン・封泥・数量知識が不足している。";
                        return false;
                    }
                    if (!TryConsumeGood(progress, "alluvial_clay", 2))
                    {
                        resultJa = "封泥・記録媒体に使う沖積粘土が不足している。";
                        return false;
                    }
                    progress.administrationAdopted = true;
                    resultJa = "数量記録行政を採用した。これは文字の完成ではなく、在庫推定と配給予測を改善する。";
                    break;

                default:
                    if (actionId != null &&
                        actionId.StartsWith("labor_", StringComparison.Ordinal))
                    {
                        if (!TryApplyLaborAction(progress, actionId, out resultJa))
                            return false;
                    }
                    else if (!UrukRegionalSystem.TryApplyAction(session, actionId,
                        out resultJa))
                    {
                        return false;
                    }
                    break;
            }

            UrukRegionalSystem.OnLegacyAction(session, actionId);
            progress.introTutorialCompleted = progress.lastCanalActionTurn > 0 &&
                progress.lastFoodPriorityTurn > 0 && progress.templePlanned;
            state.EmitLog(resultJa);
            state.Bump();
            return true;
        }

        /// <summary>
        /// 通常4Xの1ターンが進んだ直後に、同じ20年間の人口・物資・洪水を集計する。
        /// 乱数ストリームを汚さず、campaign seedと完了ターンだけで決定する。
        /// </summary>
        public static void AdvanceAfterTurn(HistoricalCampaignSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            var state = session.State;
            var progress = session.Progress;
            int completedTurn = Math.Clamp(state.TurnNumber - 1, 1,
                session.Definition.maxTurns);
            var flood = FloodForPeriod(session.Definition.seed, completedTurn);
            progress.currentFloodTrend = (int)flood;

            // 生産・消費・人口・社会・建設を同じ期間条件で一括解決する。
            // 運河の劣化は当期の収穫後に反映し、次期間の予測へ影響させる。
            UrukRegionalSystem.Advance(session, completedTurn, flood);
            UrukSubsistenceSystem.Advance(session, completedTurn, flood);
            int canalCondition = CanalCondition(progress);
            bool maintained = progress.lastCanalActionTurn == completedTurn;
            canalCondition -= maintained ? 5 : 12;
            if (flood == UrukFloodTrend.Severe) canalCondition -= 18;
            SetCanalCondition(progress, canalCondition);

            TryFoundCityState(session);
            int totalLoss = progress.lastDistributionLoss + progress.lastFoodSpoiled;
            progress.lastReportJa =
                $"{UrukFloodTrendNameJa(flood)}。食料 産出{progress.lastFoodProduced}／" +
                $"消費{progress.lastFoodConsumed}／損失{totalLoss}、" +
                $"備蓄{UrukSubsistenceSystem.FoodReserveMonthsJa(progress)}、" +
                $"人口{Signed(progress.lastPopulationChange)}人、" +
                $"運河状態{CanalCondition(progress)}%。";
            state.EmitLog("ウルク年代記: " + progress.lastReportJa);
            CheckVictoryAndDefeat(session);
            ValidateProgress(session.Definition, progress);
            state.Bump();
        }

        public static int FarmCount(UrukCampaignProgress progress)
        {
            int count = 0;
            foreach (var improvement in progress.improvements)
                if (improvement.kind == "farm") count++;
            return count;
        }

        public static int IrrigatedFarmCount(UrukCampaignProgress progress)
        {
            int farms = FarmCount(progress);
            int condition = CanalCondition(progress);
            if (condition >= 60) return farms;
            if (condition >= 30) return Math.Min(1, farms);
            return 0;
        }

        public static int CanalCondition(UrukCampaignProgress progress)
        {
            foreach (var improvement in progress.improvements)
                if (improvement.kind == "canal") return improvement.condition;
            return 0;
        }

        public static int TotalFood(UrukCampaignProgress progress)
        {
            return GoodAmount(progress, "barley") +
                GoodAmount(progress, "emmer_wheat") + GoodAmount(progress, "fish");
        }

        public static int GoodAmount(UrukCampaignProgress progress, string id)
        {
            if (progress?.stockpiles == null) return 0;
            foreach (var stock in progress.stockpiles)
                if (stock != null && stock.id == id) return Math.Max(0, stock.amount);
            return 0;
        }

        public static string TutorialTitleJa(HistoricalCampaignSession session)
        {
            int turn = session.State.TurnNumber;
            var progress = session.Progress;
            if (turn <= 1 && progress.lastCanalActionTurn <= 0) return "1. 運河を整備する";
            if (turn <= 2 && progress.lastFoodPriorityTurn <= 0) return "2. 食料を安定させる";
            if (turn <= 3 && !progress.templePlanned) return "3. 初期神殿区画を計画する";
            if (!progress.templePlanned) return "初期神殿区画を着工する";
            if (progress.templeStage < 5) return "神殿・倉庫を段階建設する";
            if (!progress.administrationAdopted) return "数量記録行政を整える";
            if (!progress.isCityState) return "人口と備蓄を育てる";
            return "都市国家ウルクの時代";
        }

        public static string TutorialBodyJa(HistoricalCampaignSession session)
        {
            int turn = session.State.TurnNumber;
            var progress = session.Progress;
            if (turn <= 1 && progress.lastCanalActionTurn <= 0)
                return "最大リスクは灌漑不足です。①運河を整備 ②食料労働を維持 ③ターン終了。詳細で予測を確認できます。";
            if (turn <= 2 && progress.lastFoodPriorityTurn <= 0)
                return "最大リスクは備蓄不足です。「食料を優先」で公共労働を再配分します。強制労働は自動化しません。";
            if (turn <= 3 && !progress.templePlanned)
                return "粘土1・葦1で初期神殿区画（復元推定）を着工します。全5段階で、途中停止と資源不足が起こります。";
            if (progress.templeStage < 5)
                return $"神殿段階 {progress.templeStage}/5「" +
                    $"{UrukSubsistenceSystem.TempleStageNameJa(progress.templeStage)}」。" +
                    "建設配分・粘土・葦を確保してください。";
            if (!progress.administrationAdopted)
                return "神殿倉庫完成後、粘土2で「数量記録行政」を採用できます。文字完成とは区別して表示します。";
            if (!progress.isCityState)
                return "人口2,500人、完全灌漑農地2区画、2期間連続の食料安全、神殿完成、数量記録行政をそろえてください。";
            return "軍事・科学・文化・経済の即時勝利、または最終ターンの存続勝利を目指します。";
        }

        public static string UrukFloodTrendNameJa(UrukFloodTrend trend)
        {
            return trend switch
            {
                UrukFloodTrend.Drought => "渇水傾向",
                UrukFloodTrend.Beneficial => "恵まれた氾濫",
                UrukFloodTrend.Severe => "洪水多発",
                _ => "安定した水位",
            };
        }

        static bool TryApplyLaborAction(UrukCampaignProgress progress,
            string actionId, out string resultJa)
        {
            if (string.IsNullOrWhiteSpace(actionId) ||
                !actionId.StartsWith("labor_", StringComparison.Ordinal))
            {
                resultJa = "不明なキャンペーン行動。";
                return false;
            }
            string[] parts = actionId.Split('_');
            if (parts.Length != 3)
            {
                resultJa = "労働配分の指定が不正。";
                return false;
            }
            int delta = parts[2] == "up" ? UrukSubsistenceSystem.LaborStep :
                parts[2] == "down" ? -UrukSubsistenceSystem.LaborStep : 0;
            if (delta == 0)
            {
                resultJa = "労働配分の増減指定が不正。";
                return false;
            }
            return UrukSubsistenceSystem.TryAdjustLabor(progress, parts[1],
                delta, out resultJa);
        }

        static UrukFloodTrend FloodForPeriod(int seed, int completedTurn)
        {
            if (completedTurn == 1) return UrukFloodTrend.Stable;
            if (completedTurn == 2) return UrukFloodTrend.Beneficial;
            if (completedTurn == 3) return UrukFloodTrend.Stable;
            int roll = Math.Abs((seed % 100) + completedTurn * 37 +
                completedTurn * completedTurn * 11) % 100;
            if (roll < 15) return UrukFloodTrend.Drought;
            if (roll < 50) return UrukFloodTrend.Stable;
            if (roll < 85) return UrukFloodTrend.Beneficial;
            return UrukFloodTrend.Severe;
        }

        static void TryFoundCityState(HistoricalCampaignSession session)
        {
            var progress = session.Progress;
            if (progress.isCityState) return;
            if (progress.actualPopulation < CityStatePopulation ||
                IrrigatedFarmCount(progress) < 2 ||
                progress.consecutiveFoodSecurePeriods <
                    UrukSubsistenceSystem.SecurePeriodsForCityState ||
                progress.templeProgress < CityStateTempleProgress ||
                progress.templeStage < 5 ||
                !progress.administrationAdopted)
                return;
            progress.isCityState = true;
            progress.cityStateFoundedTurn = Math.Max(1, session.State.TurnNumber - 1);
            var human = session.State.HumanPlayer;
            if (human != null) human.NameJa = "ウルク都市国家";
            session.State.EmitLog(
                $"ウルクが都市国家として成立した（ターン{progress.cityStateFoundedTurn}）");
        }

        static void CheckVictoryAndDefeat(HistoricalCampaignSession session)
        {
            var state = session.State;
            var progress = session.Progress;
            var human = state.HumanPlayer;
            if (human == null || human.IsEliminated || human.Cities.Count == 0)
            {
                EndGame(state, null, "ウルク中心集落が占領・併合され、共同体が消滅した。");
                return;
            }
            if (progress.actualPopulation < MinimumCommunityPopulation)
            {
                EndGame(state, null, "人口が500人未満となり、ウルク共同体を維持できなくなった。");
                return;
            }
            if (progress.consecutiveCriticalUnrest >= CriticalUnrestPeriodsForDefeat)
            {
                EndGame(state, null, "社会不安を再統合できず、ウルク共同体が分裂した。");
                return;
            }
            if (progress.tributaryCount >= 4)
            {
                EndGame(state, human, "ウルクが南メソポタミアの軍事的覇権を確立した。");
                return;
            }
            if (progress.knowledgeMilestones >= 4 &&
                HistoricalCampaignCalendar.YearAtTurnStart(
                    session.Definition, state.TurnNumber) >= -3400)
            {
                EndGame(state, human, "ウルクが原文字体系を完成し、科学勝利を収めた。");
                return;
            }
            if (progress.templeProgress >= TempleCompletionProgress &&
                progress.culturalReach >= 5)
            {
                EndGame(state, human, "ウルクの祭祀・建築様式が広がり、文化勝利を収めた。");
                return;
            }
            if (progress.permanentTradeRoutes >= 4 && progress.importedGoodKinds >= 4 &&
                TotalFood(progress) >= 12)
            {
                EndGame(state, human, "ウルクが広域交換網の中心となり、経済勝利を収めた。");
                return;
            }
            if (state.TurnNumber > session.Definition.maxTurns)
            {
                EndGame(state, human,
                    "紀元前3000年まで独立政体として存続し、存続勝利を収めた。");
            }
        }

        static void EndGame(GameState state, Player winner, string message)
        {
            if (state.IsGameOver) return;
            state.IsGameOver = true;
            state.Winner = winner;
            state.GameOverMessageJa = message;
            state.EmitLog(message);
            state.RaiseGameEnded(winner, message);
        }

        static int ConsumeFood(UrukCampaignProgress progress, int amount)
        {
            int remaining = amount;
            remaining -= ConsumeUpTo(progress, "barley", remaining);
            remaining -= ConsumeUpTo(progress, "emmer_wheat", remaining);
            remaining -= ConsumeUpTo(progress, "fish", remaining);
            return Math.Max(0, remaining);
        }

        static int ConsumeUpTo(UrukCampaignProgress progress, string id, int requested)
        {
            if (requested <= 0) return 0;
            foreach (var stock in progress.stockpiles)
            {
                if (stock.id != id) continue;
                int used = Math.Min(Math.Max(0, stock.amount), requested);
                stock.amount -= used;
                return used;
            }
            return 0;
        }

        static void ApplyPopulationChange(UrukCampaignProgress progress, int change)
        {
            int target = Math.Max(0, progress.actualPopulation + change);
            int actual = target - progress.actualPopulation;
            progress.actualPopulation = target;
            if (actual >= 0)
            {
                progress.roles.farmers += actual;
                progress.statuses.free += actual;
                return;
            }

            int remove = -actual;
            remove = RemoveUpTo(ref progress.roles.laborers, remove);
            remove = RemoveUpTo(ref progress.roles.farmers, remove);
            remove = RemoveUpTo(ref progress.roles.pastoralists, remove);
            remove = RemoveUpTo(ref progress.roles.fishers, remove);
            remove = RemoveUpTo(ref progress.roles.artisans, remove);
            remove = RemoveUpTo(ref progress.roles.warriors, remove);
            remove = RemoveUpTo(ref progress.roles.priests, remove);
            int statusRemove = -actual;
            statusRemove = RemoveUpTo(ref progress.statuses.free, statusRemove);
            statusRemove = RemoveUpTo(ref progress.statuses.dependent, statusRemove);
            statusRemove = RemoveUpTo(ref progress.statuses.debtBound, statusRemove);
            statusRemove = RemoveUpTo(ref progress.statuses.captive, statusRemove);
            RemoveUpTo(ref progress.statuses.enslaved, statusRemove);
        }

        static int RemoveUpTo(ref int value, int requested)
        {
            int removed = Math.Min(Math.Max(0, value), Math.Max(0, requested));
            value -= removed;
            return requested - removed;
        }

        static void SetCanalCondition(UrukCampaignProgress progress, int condition)
        {
            foreach (var improvement in progress.improvements)
                if (improvement.kind == "canal")
                    improvement.condition = Math.Clamp(condition, 0, 100);
        }

        static bool HasGood(UrukCampaignProgress progress, string id, int amount)
        {
            int reserved = id == "alluvial_clay" ? progress.reservedClay :
                id == "reeds" ? progress.reservedReeds : 0;
            return GoodAmount(progress, id) - reserved >= amount;
        }

        static bool TryConsumeGood(UrukCampaignProgress progress, string id, int amount)
        {
            if (!HasGood(progress, id, amount)) return false;
            foreach (var stock in progress.stockpiles)
            {
                if (stock.id != id) continue;
                stock.amount -= amount;
                return true;
            }
            return false;
        }

        static void AddGood(UrukCampaignProgress progress, string id, int amount)
        {
            if (amount <= 0) return;
            foreach (var stock in progress.stockpiles)
            {
                if (stock.id != id) continue;
                stock.amount += amount;
                return;
            }
        }

        static HistoricalGoodDefinition FindGood(HistoricalCampaignDefinition definition, string id)
        {
            foreach (var good in definition.goods)
                if (good != null && good.id == id) return good;
            return null;
        }

        static HistoricalPopulationRoles CopyRoles(HistoricalPopulationRoles source)
        {
            return new HistoricalPopulationRoles
            {
                farmers = source.farmers,
                pastoralists = source.pastoralists,
                fishers = source.fishers,
                artisans = source.artisans,
                priests = source.priests,
                warriors = source.warriors,
                laborers = source.laborers,
            };
        }

        static HistoricalPopulationStatuses CopyStatuses(HistoricalPopulationStatuses source)
        {
            return new HistoricalPopulationStatuses
            {
                free = source.free,
                dependent = source.dependent,
                debtBound = source.debtBound,
                captive = source.captive,
                enslaved = source.enslaved,
            };
        }

        static HistoricalGoodAmount[] CopyStockpiles(HistoricalGoodAmount[] source)
        {
            var copy = new HistoricalGoodAmount[source.Length];
            for (int i = 0; i < source.Length; i++)
                copy[i] = new HistoricalGoodAmount
                {
                    id = source[i].id,
                    amount = source[i].amount,
                };
            return copy;
        }

        static HistoricalImprovementState[] CopyImprovements(
            HistoricalImprovementDefinition[] source)
        {
            var copy = new HistoricalImprovementState[source.Length];
            for (int i = 0; i < source.Length; i++)
                copy[i] = new HistoricalImprovementState
                {
                    id = source[i].id,
                    kind = source[i].kind,
                    col = source[i].col,
                    row = source[i].row,
                    condition = source[i].condition,
                };
            return copy;
        }

        static string Signed(int value)
        {
            return value >= 0 ? "+" + value : value.ToString();
        }
    }
}
