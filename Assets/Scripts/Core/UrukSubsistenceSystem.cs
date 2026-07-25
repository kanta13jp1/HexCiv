using System;

namespace HexCiv.Core
{
    [Serializable]
    public sealed class UrukLaborAllocation
    {
        public int food = 55;
        public int canal = 15;
        public int construction = 10;
        public int crafts = 10;
        public int trade = 5;
        public int militia = 5;

        public int Total => food + canal + construction + crafts + trade + militia;
    }

    [Serializable]
    public sealed class UrukSocialFactors
    {
        public int foodSecurity;
        public int laborBurden;
        public int distributionFairness;
        public int inequality;
        public int leadershipTrust;
        public int ritualSupport;
        public int externalSecurity;
        public int waterRelations;
        public int recentShock;

        public int Average =>
            (foodSecurity + laborBurden + distributionFairness + inequality +
             leadershipTrust + ritualSupport + externalSecurity + waterRelations +
             recentShock) / 9;
    }

    [Serializable]
    public sealed class UrukPeriodEvent
    {
        public int turn;
        public string kind;
        public int people;
        public string reasonJa;
        public string confidence;
    }

    /// <summary>
    /// 20年間を一収穫として扱わず、期間中の平均的な食料安全・労働・人口変化を
    /// 物流単位へ集約する。通常4XのCore状態やRNGには触れない。
    /// </summary>
    public static class UrukSubsistenceSystem
    {
        public const int LaborStep = 5;
        public const int FoodUnitPersonMonths = 300;
        public const int InitialStorageMonths = 3;
        public const int TempleStorageMonths = 8;
        public const int SecureReserveTenthsMonths = 30;
        public const int SecurePeriodsForCityState = 2;

        static readonly string[] LaborKinds =
        {
            "food", "canal", "construction", "crafts", "trade", "militia",
        };

        public static void EnsureDefaults(UrukCampaignProgress progress)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            progress.labor ??= new UrukLaborAllocation();
            if (progress.labor.Total == 0)
                progress.labor = new UrukLaborAllocation();
            progress.social ??= CreateSocial(progress.stability);
            progress.events ??= Array.Empty<UrukPeriodEvent>();
            if (progress.seedGrain < 0) progress.seedGrain = 0;
            if (progress.storageCapacityMonths <= 0)
                progress.storageCapacityMonths = InitialStorageMonths;
            if (string.IsNullOrWhiteSpace(progress.laborAutomationPolicy))
                progress.laborAutomationPolicy = "food_security";
            if (progress.recordKnowledgeElements <= 0)
                progress.recordKnowledgeElements = 3;
            progress.templeStage = Math.Clamp(progress.templeStage, 0, 5);
        }

        public static int MobilizablePopulation(UrukCampaignProgress progress)
        {
            if (progress == null) return 0;
            // 全年齢人口から、世帯維持・育児・健康上の制約を除いた公共動員可能数。
            int baseAvailable = progress.actualPopulation * 35 / 100;
            int crisisLoss = progress.lastCrisisDeaths / 2;
            return Math.Max(0, baseAvailable - crisisLoss);
        }

        public static int AssignedPeople(UrukCampaignProgress progress, string kind)
        {
            if (progress?.labor == null) return 0;
            return MobilizablePopulation(progress) * GetLabor(progress.labor, kind) / 100;
        }

        public static int FoodReserveTenthsMonths(UrukCampaignProgress progress)
        {
            if (progress == null || progress.actualPopulation <= 0) return 0;
            long personMonths = (long)UrukCampaignSystem.TotalFood(progress) *
                FoodUnitPersonMonths;
            return (int)Math.Min(999, personMonths * 10 / progress.actualPopulation);
        }

        public static string FoodReserveMonthsJa(UrukCampaignProgress progress)
        {
            int tenths = FoodReserveTenthsMonths(progress);
            return $"{tenths / 10}.{tenths % 10}か月分（推定）";
        }

        public static int ForecastFoodProduction(UrukCampaignProgress progress)
        {
            if (progress == null) return 0;
            EnsureDefaults(progress);
            var flood = Enum.IsDefined(typeof(UrukFloodTrend),
                progress.currentFloodTrend)
                ? (UrukFloodTrend)progress.currentFloodTrend
                : UrukFloodTrend.Stable;
            return ForecastFoodProduction(progress, flood);
        }

        public static bool TryAdjustLabor(UrukCampaignProgress progress, string kind,
            int delta, out string resultJa)
        {
            EnsureDefaults(progress);
            if (Array.IndexOf(LaborKinds, kind) < 0 ||
                (delta != LaborStep && delta != -LaborStep))
            {
                resultJa = "労働配分の指定が不正。";
                return false;
            }

            int current = GetLabor(progress.labor, kind);
            int minimum = kind == "food" ? 30 : 0;
            if (delta < 0 && current + delta < minimum)
            {
                resultJa = kind == "food"
                    ? "食料生産30%未満は通常配分では選べない。"
                    : "これ以上減らせない。";
                return false;
            }

            string counterpart;
            if (delta > 0)
            {
                counterpart = LargestDonor(progress.labor, kind);
                if (counterpart == null)
                {
                    resultJa = "移し替え可能な労働力がない。";
                    return false;
                }
                SetLabor(progress.labor, counterpart,
                    GetLabor(progress.labor, counterpart) - LaborStep);
                SetLabor(progress.labor, kind, current + LaborStep);
            }
            else
            {
                counterpart = kind == "food" ? "canal" : "food";
                SetLabor(progress.labor, kind, current - LaborStep);
                SetLabor(progress.labor, counterpart,
                    GetLabor(progress.labor, counterpart) + LaborStep);
            }

            progress.populationAutomation = false;
            resultJa = $"労働配分を変更: {LaborNameJa(kind)} " +
                $"{GetLabor(progress.labor, kind)}%。ターン終了まで再変更できる。";
            return true;
        }

        public static void ApplyFoodSecurityAutomation(UrukCampaignProgress progress)
        {
            EnsureDefaults(progress);
            if (!progress.populationAutomation) return;
            progress.labor.food = 55;
            progress.labor.canal = UrukCampaignSystem.CanalCondition(progress) < 60 ? 20 : 10;
            progress.labor.construction = progress.templePlanned &&
                progress.templeProgress < 100 ? 15 : 10;
            progress.labor.crafts = 10;
            progress.labor.trade = 5;
            progress.labor.militia = 5;
            int total = progress.labor.Total;
            progress.labor.food += 100 - total;
        }

        public static void Advance(HistoricalCampaignSession session, int completedTurn,
            UrukFloodTrend flood)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            var progress = session.Progress;
            EnsureDefaults(progress);
            ApplyFoodSecurityAutomation(progress);

            int populationBefore = progress.actualPopulation;
            ResolveFood(progress, flood);
            ResolvePopulation(progress, flood);
            ResolveStatuses(progress, completedTurn);
            AdvanceTemple(progress, completedTurn, flood);
            ResolveSocial(progress, flood, populationBefore);
        }

        public static string TempleStageNameJa(int stage)
        {
            return Math.Clamp(stage, 0, 5) switch
            {
                1 => "敷地整備",
                2 => "日干し煉瓦・葦材",
                3 => "基壇・壁体",
                4 => "祭祀空間・倉庫",
                5 => "配給・記録担当者",
                _ => "未着工",
            };
        }

        public static string LaborNameJa(string kind)
        {
            return kind switch
            {
                "food" => "食料生産",
                "canal" => "運河維持",
                "construction" => "建設",
                "crafts" => "工芸",
                "trade" => "交易",
                "militia" => "民兵",
                _ => kind,
            };
        }

        static void ResolveFood(UrukCampaignProgress progress, UrukFloodTrend flood)
        {
            int farms = UrukCampaignSystem.FarmCount(progress);
            int irrigated = UrukCampaignSystem.IrrigatedFarmCount(progress);
            int laborFactor = Math.Clamp(progress.labor.food * 100 / 55, 40, 160);
            int irrigationFactor = irrigated >= farms && farms > 0 ? 110 :
                irrigated > 0 ? 85 : 60;
            int soilFactor = AverageFarmCondition(progress);
            int seedFactor = progress.seedGrain >= 2 ? 100 :
                progress.seedGrain == 1 ? 75 : 55;
            int toolFactor = Math.Clamp(90 + progress.labor.crafts, 90, 115);
            int seedToolFactor = seedFactor * toolFactor / 100;
            int floodFactor = flood switch
            {
                UrukFloodTrend.Drought => 75,
                UrukFloodTrend.Beneficial => 120,
                UrukFloodTrend.Severe => 65,
                _ => 100,
            };

            // 基礎収量 × 労働 × 灌漑 × 土壌状態 × 種籾・道具 × 氾濫。
            int grainGross = farms * 10;
            grainGross = grainGross * laborFactor / 100;
            grainGross = grainGross * irrigationFactor / 100;
            grainGross = grainGross * soilFactor / 70;
            grainGross = grainGross * seedToolFactor / 100;
            grainGross = grainGross * floodFactor / 100;
            grainGross = Math.Max(0, grainGross);
            int fishGross = progress.roles.fishers > 0
                ? Math.Max(1, progress.labor.food / 25)
                : 0;
            if (flood == UrukFloodTrend.Severe) fishGross++;
            if (flood == UrukFloodTrend.Drought) fishGross = Math.Max(0, fishGross - 1);

            int seedUsed = Math.Min(2, progress.seedGrain);
            progress.seedGrain -= seedUsed;
            int seedRestored = Math.Min(2 - progress.seedGrain, grainGross);
            progress.seedGrain += seedRestored;
            grainGross -= seedRestored;

            int gross = grainGross + fishGross;
            int distributionRate = progress.administrationAdopted ? 4 : 10;
            int distributionLoss = gross <= 0 ? 0 :
                Math.Max(1, gross * distributionRate / 100);
            int grainLoss = Math.Min(grainGross, distributionLoss);
            grainGross -= grainLoss;
            int fishLoss = distributionLoss - grainLoss;
            fishGross = Math.Max(0, fishGross - fishLoss);

            AddGood(progress, "barley", grainGross);
            AddGood(progress, "fish", fishGross);
            int consumed = Math.Max(1, (progress.actualPopulation + 449) / 450);
            int shortage = ConsumeFoodFishFirst(progress, consumed);

            int fishSpoiled = UrukCampaignSystem.GoodAmount(progress, "fish");
            SetGood(progress, "fish", 0);
            int grainBeforeSpoilage = GrainAmount(progress);
            int spoilRate = progress.templeStage >= 4 ? 15 : 30;
            int grainSpoiled = grainBeforeSpoilage * spoilRate / 100;
            ConsumeGrain(progress, grainSpoiled);

            progress.storageCapacityMonths = progress.templeStage >= 4
                ? TempleStorageMonths
                : InitialStorageMonths;
            int capacityUnits = Math.Max(1,
                (progress.actualPopulation * progress.storageCapacityMonths +
                 FoodUnitPersonMonths - 1) / FoodUnitPersonMonths);
            int overflow = Math.Max(0, GrainAmount(progress) - capacityUnits);
            ConsumeGrain(progress, overflow);

            progress.lastFoodProduced = gross;
            progress.lastFoodConsumed = consumed - shortage;
            progress.lastFoodShortage = shortage;
            progress.lastDistributionLoss = distributionLoss;
            progress.lastFoodSpoiled = fishSpoiled + grainSpoiled + overflow;
            progress.lastFoodCarryover = UrukCampaignSystem.TotalFood(progress);
            progress.lastSeedUsed = seedUsed;
            progress.lastFoodLaborFactor = laborFactor;
            progress.lastIrrigationFactor = irrigationFactor;
            progress.lastFloodFactor = floodFactor;
            progress.lastSoilFactor = soilFactor;
            progress.lastSeedToolFactor = seedToolFactor;

            int reserve = FoodReserveTenthsMonths(progress);
            int forecast = ForecastFoodProduction(progress, flood);
            progress.lastForecastFood = forecast;
            bool secure = reserve >= SecureReserveTenthsMonths &&
                forecast >= consumed;
            progress.foodSecureThisPeriod = secure;
            progress.consecutiveFoodSecurePeriods = secure
                ? progress.consecutiveFoodSecurePeriods + 1
                : 0;
        }

        static int ForecastFoodProduction(UrukCampaignProgress progress,
            UrukFloodTrend currentFlood)
        {
            int farms = UrukCampaignSystem.FarmCount(progress);
            int irrigated = UrukCampaignSystem.IrrigatedFarmCount(progress);
            int irrigationFactor = irrigated >= farms && farms > 0 ? 110 :
                irrigated > 0 ? 85 : 60;
            int seedFactor = progress.seedGrain >= 2 ? 100 :
                progress.seedGrain == 1 ? 75 : 55;
            int floodFactor = currentFlood == UrukFloodTrend.Severe ? 80 :
                currentFlood == UrukFloodTrend.Drought ? 85 : 100;
            int estimate = farms * 10;
            estimate = estimate * progress.labor.food / 55;
            estimate = estimate * irrigationFactor / 100;
            estimate = estimate * AverageFarmCondition(progress) / 70;
            estimate = estimate * seedFactor / 100;
            estimate = estimate * Math.Clamp(90 + progress.labor.crafts, 90, 115) /
                100;
            estimate = estimate * floodFactor / 100;
            return Math.Max(0, estimate);
        }

        static int AverageFarmCondition(UrukCampaignProgress progress)
        {
            int total = 0;
            int count = 0;
            foreach (var improvement in progress.improvements)
            {
                if (improvement.kind != "farm") continue;
                total += improvement.condition;
                count++;
            }
            return count == 0 ? 70 : Math.Clamp(total / count, 30, 100);
        }

        static void ResolvePopulation(UrukCampaignProgress progress, UrukFloodTrend flood)
        {
            int population = progress.actualPopulation;
            int birthsRate = progress.lastFoodShortage > 0 ? 15 :
                progress.foodSecureThisPeriod ? 25 : 22;
            int deathRate = progress.lastFoodShortage > 0 ? 20 :
                progress.foodSecureThisPeriod ? 17 : 18;
            int births = population * birthsRate / 100;
            int normalDeaths = population * deathRate / 100;
            int crisisDeaths = progress.lastFoodShortage * 35;
            if (flood == UrukFloodTrend.Severe) crisisDeaths += population / 100;
            int migrationIn = progress.foodSecureThisPeriod &&
                progress.stability >= 45 ? Math.Max(10, population / 100) : 0;
            int migrationOut = progress.lastFoodShortage > 0
                ? Math.Max(10, population / 50)
                : progress.stability < 35 ? Math.Max(5, population / 100) : 0;

            int change = births - normalDeaths - crisisDeaths +
                migrationIn - migrationOut;
            ApplyPopulationChangeProportionally(progress, change);
            progress.lastBirths = births;
            progress.lastNormalDeaths = normalDeaths;
            progress.lastCrisisDeaths = crisisDeaths;
            progress.lastMigrationIn = migrationIn;
            progress.lastMigrationOut = migrationOut;
            progress.lastPopulationChange = change;
        }

        static void ResolveStatuses(UrukCampaignProgress progress, int turn)
        {
            progress.lastStatusEventJa = "";
            if (progress.lastFoodShortage > 0 && progress.statuses.free > 0)
            {
                int moved = Math.Min(progress.statuses.free,
                    Math.Max(15, progress.actualPopulation / 50));
                progress.statuses.free -= moved;
                progress.statuses.dependent += moved;
                progress.lastStatusEventJa =
                    $"{moved}人が食料援助への継続依存により依存的地位へ移行した。";
                AppendEvent(progress, turn, "status_dependency", moved,
                    progress.lastStatusEventJa, "inferred");
            }
            else if (progress.foodSecureThisPeriod && progress.stability >= 60 &&
                progress.statuses.dependent > 0)
            {
                int moved = Math.Min(progress.statuses.dependent,
                    Math.Max(5, progress.actualPopulation / 200));
                progress.statuses.dependent -= moved;
                progress.statuses.free += moved;
                progress.lastStatusEventJa =
                    $"{moved}人が食料安定と扶養関係の解消により自由な地位へ戻った。";
                AppendEvent(progress, turn, "status_release", moved,
                    progress.lastStatusEventJa, "inferred");
            }
        }

        static void AdvanceTemple(UrukCampaignProgress progress, int turn,
            UrukFloodTrend flood)
        {
            if (!progress.templePlanned || progress.templeStage <= 0 ||
                progress.templeStage >= 5)
                return;

            if (progress.labor.construction < 5)
            {
                int floor = StageFloor(progress.templeStage);
                progress.templeProgress = Math.Max(floor,
                    progress.templeProgress - 2);
                progress.lastConstructionReportJa =
                    $"初期神殿区画は労働不足で中断。未完成部分が劣化し、" +
                    $"進捗{progress.templeProgress}%。";
                return;
            }

            int gain = 8 + progress.labor.construction * 3 / 5;
            if (flood == UrukFloodTrend.Severe) gain = Math.Max(2, gain - 4);
            int target = Math.Min(100, progress.templeProgress + gain);
            int nextStage = progress.templeStage + 1;
            int threshold = StageThreshold(nextStage);
            if (target >= threshold)
            {
                int clay = nextStage == 5 ? 1 : 2;
                int reeds = nextStage == 5 ? 0 : 1;
                if (!HasGood(progress, "alluvial_clay", clay) ||
                    !HasGood(progress, "reeds", reeds))
                {
                    progress.templeProgress = Math.Max(progress.templeProgress,
                        threshold - 1);
                    progress.lastConstructionReportJa =
                        $"神殿段階「{TempleStageNameJa(nextStage)}」の資材が不足。";
                    return;
                }
                ConsumeGood(progress, "alluvial_clay", clay);
                ConsumeGood(progress, "reeds", reeds);
                progress.templeStage = nextStage;
                AppendEvent(progress, turn, "temple_stage", 0,
                    $"初期神殿区画の「{TempleStageNameJa(nextStage)}」段階が完了した。",
                    "inferred");
            }
            progress.templeProgress = target;
            progress.lastConstructionReportJa =
                $"初期神殿区画: {TempleStageNameJa(progress.templeStage)} " +
                $"{progress.templeProgress}%。";
        }

        static void ResolveSocial(UrukCampaignProgress progress, UrukFloodTrend flood,
            int populationBefore)
        {
            var social = progress.social ??= CreateSocial(progress.stability);
            int reserve = FoodReserveTenthsMonths(progress);
            social.foodSecurity = progress.lastFoodShortage > 0 ? 10 :
                reserve >= 30 ? 80 : reserve >= 10 ? 50 : 30;
            int publicMobilization = progress.labor.canal +
                progress.labor.construction + progress.labor.militia;
            social.laborBurden = Math.Clamp(100 - Math.Max(0,
                publicMobilization - 25) * 2, 0, 100);
            social.distributionFairness = progress.lastFoodShortage > 0 ? 20 :
                progress.administrationAdopted ? 75 : 55;
            social.inequality = Math.Clamp(100 -
                (progress.statuses.dependent + progress.statuses.debtBound +
                 progress.statuses.enslaved) * 100 /
                Math.Max(1, progress.actualPopulation), 0, 100);
            social.leadershipTrust = Math.Clamp(
                (progress.stability + social.foodSecurity) / 2, 0, 100);
            social.ritualSupport = Math.Clamp(45 + progress.templeStage * 9, 0, 100);
            social.externalSecurity = progress.lastMigrationOut > 0 ? 55 : 75;
            social.waterRelations = UrukCampaignSystem.CanalCondition(progress);
            social.recentShock = flood == UrukFloodTrend.Severe ? 25 :
                flood == UrukFloodTrend.Drought ? 50 :
                progress.lastFoodShortage > 0 ? 35 : 80;
            progress.stability = Math.Clamp(social.Average, 0, 100);

            UpdateCriticalUnrest(progress, populationBefore);
        }

        public static void UpdateCriticalUnrest(UrukCampaignProgress progress,
            int populationBefore)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            bool massFlight = progress.lastMigrationOut * 20 >=
                Math.Max(1, populationBefore);
            bool critical = progress.stability < 20 || massFlight;
            progress.lastCriticalUnrestReasonJa = critical
                ? massFlight ? "人口の5%以上が一期間に流出した。"
                    : "総合安定度が20未満となった。"
                : "";
            if (critical)
                progress.consecutiveCriticalUnrest++;
            else if (progress.stability >= 35 &&
                progress.lastFoodShortage == 0 && !massFlight)
                progress.consecutiveCriticalUnrest = 0;
        }

        static int StageThreshold(int stage)
        {
            return stage switch
            {
                2 => 20,
                3 => 45,
                4 => 75,
                5 => 100,
                _ => 0,
            };
        }

        static int StageFloor(int stage)
        {
            return stage switch
            {
                1 => 5,
                2 => 20,
                3 => 45,
                4 => 75,
                5 => 100,
                _ => 0,
            };
        }

        static UrukSocialFactors CreateSocial(int value)
        {
            int clamped = Math.Clamp(value, 0, 100);
            return new UrukSocialFactors
            {
                foodSecurity = clamped,
                laborBurden = clamped,
                distributionFairness = clamped,
                inequality = clamped,
                leadershipTrust = clamped,
                ritualSupport = clamped,
                externalSecurity = clamped,
                waterRelations = clamped,
                recentShock = clamped,
            };
        }

        static void AppendEvent(UrukCampaignProgress progress, int turn, string kind,
            int people, string reasonJa, string confidence)
        {
            var old = progress.events ?? Array.Empty<UrukPeriodEvent>();
            int keep = Math.Min(old.Length, 63);
            var next = new UrukPeriodEvent[keep + 1];
            int start = old.Length - keep;
            for (int i = 0; i < keep; i++) next[i] = old[start + i];
            next[keep] = new UrukPeriodEvent
            {
                turn = turn,
                kind = kind,
                people = people,
                reasonJa = reasonJa,
                confidence = confidence,
            };
            progress.events = next;
        }

        public static void ApplyExternalMigration(UrukCampaignProgress progress,
            int people, int turn, string reasonJa, string confidence = "inferred")
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            if (people == 0) return;
            ApplyPopulationChangeProportionally(progress, people);
            if (people > 0)
                progress.lastMigrationIn += people;
            else
                progress.lastMigrationOut += -people;
            AppendEvent(progress, turn, people > 0 ? "migration_in" : "migration_out",
                Math.Abs(people), reasonJa, confidence);
        }

        static void ApplyPopulationChangeProportionally(
            UrukCampaignProgress progress, int change)
        {
            int target = Math.Max(0, progress.actualPopulation + change);
            int actual = target - progress.actualPopulation;
            int before = Math.Max(1, progress.actualPopulation);
            progress.actualPopulation = target;
            if (actual >= 0)
            {
                int assigned = 0;
                assigned += AddShare(ref progress.roles.farmers,
                    actual, progress.roles.farmers, before);
                assigned += AddShare(ref progress.roles.pastoralists,
                    actual, progress.roles.pastoralists, before);
                assigned += AddShare(ref progress.roles.fishers,
                    actual, progress.roles.fishers, before);
                assigned += AddShare(ref progress.roles.artisans,
                    actual, progress.roles.artisans, before);
                assigned += AddShare(ref progress.roles.priests,
                    actual, progress.roles.priests, before);
                assigned += AddShare(ref progress.roles.warriors,
                    actual, progress.roles.warriors, before);
                progress.roles.laborers += actual - assigned;

                int dependentAdd = actual * progress.statuses.dependent / before;
                int debtAdd = actual * progress.statuses.debtBound / before;
                int captiveAdd = actual * progress.statuses.captive / before;
                int enslavedAdd = actual * progress.statuses.enslaved / before;
                int freeAdd = actual - dependentAdd - debtAdd - captiveAdd -
                    enslavedAdd;
                progress.statuses.free += freeAdd;
                progress.statuses.dependent += dependentAdd;
                progress.statuses.debtBound += debtAdd;
                progress.statuses.captive += captiveAdd;
                progress.statuses.enslaved += enslavedAdd;
                return;
            }

            ScaleRolesToPopulation(progress.roles, target, before);
            ScaleStatusesToPopulation(progress.statuses, target, before);
        }

        static int AddShare(ref int field, int change, int existing, int total)
        {
            int add = change * existing / Math.Max(1, total);
            field += add;
            return add;
        }

        static void ScaleRolesToPopulation(HistoricalPopulationRoles roles,
            int target, int before)
        {
            roles.farmers = roles.farmers * target / before;
            roles.pastoralists = roles.pastoralists * target / before;
            roles.fishers = roles.fishers * target / before;
            roles.artisans = roles.artisans * target / before;
            roles.priests = roles.priests * target / before;
            roles.warriors = roles.warriors * target / before;
            roles.laborers = target - roles.farmers - roles.pastoralists -
                roles.fishers - roles.artisans - roles.priests - roles.warriors;
        }

        static void ScaleStatusesToPopulation(HistoricalPopulationStatuses statuses,
            int target, int before)
        {
            statuses.dependent = statuses.dependent * target / before;
            statuses.debtBound = statuses.debtBound * target / before;
            statuses.captive = statuses.captive * target / before;
            statuses.enslaved = statuses.enslaved * target / before;
            statuses.free = target - statuses.dependent - statuses.debtBound -
                statuses.captive - statuses.enslaved;
        }

        static string LargestDonor(UrukLaborAllocation labor, string except)
        {
            string best = null;
            int bestAvailable = 0;
            foreach (string kind in LaborKinds)
            {
                if (kind == except) continue;
                int minimum = kind == "food" ? 30 : 0;
                int available = GetLabor(labor, kind) - minimum;
                if (available > bestAvailable)
                {
                    bestAvailable = available;
                    best = kind;
                }
            }
            return bestAvailable >= LaborStep ? best : null;
        }

        static int GetLabor(UrukLaborAllocation labor, string kind)
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

        static void SetLabor(UrukLaborAllocation labor, string kind, int value)
        {
            value = Math.Clamp(value, 0, 100);
            switch (kind)
            {
                case "food": labor.food = value; break;
                case "canal": labor.canal = value; break;
                case "construction": labor.construction = value; break;
                case "crafts": labor.crafts = value; break;
                case "trade": labor.trade = value; break;
                case "militia": labor.militia = value; break;
            }
        }

        static int GrainAmount(UrukCampaignProgress progress)
        {
            return UrukCampaignSystem.GoodAmount(progress, "barley") +
                UrukCampaignSystem.GoodAmount(progress, "emmer_wheat");
        }

        static int ConsumeFoodFishFirst(UrukCampaignProgress progress, int requested)
        {
            int remaining = requested;
            remaining -= ConsumeGoodUpTo(progress, "fish", remaining);
            remaining -= ConsumeGoodUpTo(progress, "barley", remaining);
            remaining -= ConsumeGoodUpTo(progress, "emmer_wheat", remaining);
            return Math.Max(0, remaining);
        }

        static int ConsumeGrain(UrukCampaignProgress progress, int requested)
        {
            int remaining = requested;
            remaining -= ConsumeGoodUpTo(progress, "barley", remaining);
            remaining -= ConsumeGoodUpTo(progress, "emmer_wheat", remaining);
            return requested - remaining;
        }

        static int ConsumeGoodUpTo(UrukCampaignProgress progress, string id, int requested)
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

        static bool HasGood(UrukCampaignProgress progress, string id, int amount)
        {
            return UrukCampaignSystem.GoodAmount(progress, id) >= amount;
        }

        static void ConsumeGood(UrukCampaignProgress progress, string id, int amount)
        {
            ConsumeGoodUpTo(progress, id, amount);
        }

        static void AddGood(UrukCampaignProgress progress, string id, int amount)
        {
            if (amount <= 0) return;
            foreach (var stock in progress.stockpiles)
                if (stock.id == id)
                {
                    stock.amount += amount;
                    return;
                }
        }

        static void SetGood(UrukCampaignProgress progress, string id, int amount)
        {
            foreach (var stock in progress.stockpiles)
                if (stock.id == id)
                {
                    stock.amount = Math.Max(0, amount);
                    return;
                }
        }
    }
}
