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
        public int version = 1;
        public int actualPopulation;
        public HistoricalPopulationRoles roles;
        public HistoricalPopulationStatuses statuses;
        public HistoricalGoodAmount[] stockpiles;
        public HistoricalImprovementState[] improvements;
        public int stability;
        public int consecutiveCriticalUnrest;
        public int currentFloodTrend = (int)UrukFloodTrend.Stable;
        public int lastFoodProduced;
        public int lastFoodConsumed;
        public int lastPopulationChange;
        public int lastCanalActionTurn;
        public int lastFoodPriorityTurn;
        public bool populationAutomation;
        public bool productionAutomation;
        public bool tradeAutomation;
        public bool templePlanned;
        public int templeProgress;
        public bool administrationAdopted;
        public bool isCityState;
        public int cityStateFoundedTurn;
        public int tributaryCount;
        public int permanentTradeRoutes;
        public int importedGoodKinds;
        public int culturalReach;
        public int knowledgeMilestones;
        public bool introTutorialCompleted;
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
        public const int AdministrationUnlockProgress = 60;
        public const int MinimumCommunityPopulation = 500;
        public const int CriticalStability = 15;
        public const int CriticalUnrestPeriodsForDefeat = 3;

        public static UrukCampaignProgress CreateInitialProgress(
            HistoricalCampaignDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            HistoricalCampaignValidator.ThrowIfInvalid(definition);
            var starting = definition.startingScenario;
            var progress = new UrukCampaignProgress
            {
                actualPopulation = starting.actualPopulation,
                roles = CopyRoles(starting.roles),
                statuses = CopyStatuses(starting.statuses),
                stockpiles = CopyStockpiles(starting.stockpiles),
                improvements = CopyImprovements(starting.improvements),
                stability = starting.stability,
                populationAutomation = starting.populationAutomation,
                productionAutomation = starting.productionAutomation,
                tradeAutomation = starting.tradeAutomation,
                currentFloodTrend = (int)UrukFloodTrend.Stable,
                lastReportJa = "小集落ウルク。食料備蓄はわずかで、運河は堆積により十分に機能していない。",
            };
            ValidateProgress(definition, progress);
            return progress;
        }

        public static void ValidateProgress(HistoricalCampaignDefinition definition,
            UrukCampaignProgress progress)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            if (progress.version != 1)
                throw new InvalidOperationException("ウルク進捗versionが不正");
            if (progress.actualPopulation < 0)
                throw new InvalidOperationException("実人口が負数");
            if (progress.roles == null || progress.roles.Total != progress.actualPopulation)
                throw new InvalidOperationException("役割別人口の合計が実人口と一致しない");
            if (progress.statuses == null || progress.statuses.Total != progress.actualPopulation)
                throw new InvalidOperationException("地位別人口の合計が実人口と一致しない");
            if (progress.statuses.enslaved < 0)
                throw new InvalidOperationException("奴隷人口が負数");
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
            progress.stability = Math.Clamp(progress.stability, 0, 100);
            progress.templeProgress = Math.Clamp(progress.templeProgress, 0,
                TempleCompletionProgress);
        }

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
                    SetCanalCondition(progress, CanalCondition(progress) + 35);
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
                    resultJa = "顧問が人口を農耕・漁撈へ優先配置した。次の収支で食料+1。";
                    break;

                case PlanTempleAction:
                    if (progress.templePlanned)
                    {
                        resultJa = "神殿区画はすでに建設中。";
                        return false;
                    }
                    if (!HasGood(progress, "alluvial_clay", 4) ||
                        !HasGood(progress, "reeds", 2))
                    {
                        resultJa = "神殿区画の着工には沖積粘土4・葦2が必要。";
                        return false;
                    }
                    TryConsumeGood(progress, "alluvial_clay", 4);
                    TryConsumeGood(progress, "reeds", 2);
                    progress.templePlanned = true;
                    progress.templeProgress = 5;
                    resultJa = "日干し煉瓦と葦を確保し、神殿区画の建設を始めた。";
                    break;

                case AdoptAdministrationAction:
                    if (progress.administrationAdopted)
                    {
                        resultJa = "配給・記録制度はすでに採用済み。";
                        return false;
                    }
                    if (progress.templeProgress < AdministrationUnlockProgress)
                    {
                        resultJa = $"神殿区画の進捗{AdministrationUnlockProgress}%で制度を採用できる。";
                        return false;
                    }
                    if (!TryConsumeGood(progress, "alluvial_clay", 2))
                    {
                        resultJa = "封泥・記録媒体に使う沖積粘土が不足している。";
                        return false;
                    }
                    progress.administrationAdopted = true;
                    resultJa = "配給と物資記録を担う行政制度を採用した。";
                    break;

                default:
                    resultJa = "不明なキャンペーン行動。";
                    return false;
            }

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

            int canalCondition = CanalCondition(progress);
            bool maintained = progress.lastCanalActionTurn == completedTurn;
            canalCondition -= maintained ? 5 : 12;
            if (flood == UrukFloodTrend.Severe) canalCondition -= 18;
            SetCanalCondition(progress, canalCondition);

            int irrigated = IrrigatedFarmCount(progress);
            int produced = FarmCount(progress) * 2 + irrigated * 2 +
                (progress.roles.fishers > 0 ? 1 : 0);
            if (progress.lastFoodPriorityTurn == completedTurn) produced++;
            switch (flood)
            {
                case UrukFloodTrend.Drought: produced -= 2; break;
                case UrukFloodTrend.Beneficial: produced += 2; break;
                case UrukFloodTrend.Severe: produced -= 3; break;
            }
            produced = Math.Max(0, produced);
            int consumed = Math.Max(1, (progress.actualPopulation + 449) / 450);
            AddGood(progress, "barley", produced);
            int shortage = ConsumeFood(progress, consumed);
            int remainingFood = TotalFood(progress);

            int populationChange;
            if (shortage > 0)
            {
                populationChange = -Math.Min(180, 60 + shortage * 30);
                progress.stability -= 8 + shortage * 2;
            }
            else if (remainingFood >= consumed * 2 && progress.stability >= 45)
            {
                populationChange = 110;
                progress.stability += 1;
            }
            else
            {
                populationChange = 40;
            }
            if (flood == UrukFloodTrend.Severe)
            {
                populationChange -= 30;
                progress.stability -= 4;
            }
            else if (flood == UrukFloodTrend.Beneficial)
            {
                progress.stability += 2;
            }

            ApplyPopulationChange(progress, populationChange);
            progress.lastFoodProduced = produced;
            progress.lastFoodConsumed = consumed;
            progress.lastPopulationChange = populationChange;
            progress.stability = Math.Clamp(progress.stability, 0, 100);

            if (progress.templePlanned && progress.templeProgress < TempleCompletionProgress)
            {
                int build = 14;
                if (progress.roles.artisans >= 80) build += 2;
                if (flood == UrukFloodTrend.Severe) build -= 4;
                progress.templeProgress = Math.Min(TempleCompletionProgress,
                    progress.templeProgress + Math.Max(1, build));
            }

            if (progress.stability <= CriticalStability)
                progress.consecutiveCriticalUnrest++;
            else
                progress.consecutiveCriticalUnrest = 0;

            TryFoundCityState(session);
            progress.lastReportJa =
                $"{UrukFloodTrendNameJa(flood)}。食料 産出{produced}／消費{consumed}、" +
                $"人口{Signed(populationChange)}人、運河状態{CanalCondition(progress)}%。";
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
            if (condition >= 50) return farms;
            if (condition >= 25) return Math.Min(1, farms);
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
            if (turn <= 3 && !progress.templePlanned) return "3. 神殿区画を計画する";
            if (!progress.administrationAdopted) return "都市国家成立への準備";
            if (!progress.isCityState) return "人口と備蓄を育てる";
            return "都市国家ウルクの時代";
        }

        public static string TutorialBodyJa(HistoricalCampaignSession session)
        {
            int turn = session.State.TurnNumber;
            var progress = session.Progress;
            if (turn <= 1 && progress.lastCanalActionTurn <= 0)
                return "未整備の運河へ労働力と葦を割り当ててください。整備後にターンを終了します。";
            if (turn <= 2 && progress.lastFoodPriorityTurn <= 0)
                return "人口配置は自動管理中です。「食料を優先」で農耕・漁撈へ重点を置きます。";
            if (turn <= 3 && !progress.templePlanned)
                return "粘土と葦を使って神殿区画を着工します。完成まで複数期間かかります。";
            if (!progress.administrationAdopted)
                return $"神殿進捗{progress.templeProgress}%／{AdministrationUnlockProgress}%で" +
                    "配給・記録制度を採用できます。";
            if (!progress.isCityState)
                return "人口2,500人、灌漑農地2区画、食料余裕、神殿完成をそろえてください。";
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
            int consumption = Math.Max(1, (progress.actualPopulation + 449) / 450);
            if (progress.actualPopulation < CityStatePopulation ||
                IrrigatedFarmCount(progress) < 2 ||
                TotalFood(progress) < consumption * 2 ||
                progress.templeProgress < CityStateTempleProgress ||
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
            return GoodAmount(progress, id) >= amount;
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
