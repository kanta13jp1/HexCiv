using System;
using System.Collections.Generic;

namespace HexCiv.Core
{
    /// <summary>
    /// 基礎14技術（帆走・航海術を含む）と、世界史図鑑の研究史132件を
    /// 一つのプレイ可能な技術ツリーとして提供する。
    /// GameRulesの既存テーブルとIDを変更せず、旧セーブ・ユニット・建物の解禁条件を維持する。
    /// </summary>
    public static class TechnologyCatalog
    {
        public const string HistoricalPrefix = "history_";
        public const int HistoricalBaseCost = 110;
        public const int HistoricalTierCost = 35;

        static readonly string[] RootPrerequisites =
        {
            "writing", "archery", "iron_working", "mathematics", "construction"
        };

        static readonly List<TechDef> Definitions = BuildDefinitions();
        static readonly IReadOnlyList<TechDef> ReadOnlyDefinitions = Definitions.AsReadOnly();
        static readonly Dictionary<string, TechDef> ById = BuildIndex();

        /// <summary>基礎14技術に研究史132件を加えた全技術。</summary>
        public static IReadOnlyList<TechDef> All => ReadOnlyDefinitions;

        /// <summary>研究史の各地域分岐を開くために必要な、既存ツリーの終端技術。</summary>
        public static IReadOnlyList<string> HistoricalRootPrerequisites => RootPrerequisites;

        public static TechDef Get(string id)
        {
            TechDef result;
            if (string.IsNullOrEmpty(id) || !ById.TryGetValue(id, out result))
                throw new KeyNotFoundException("Unknown technology id: " + id);
            return result;
        }

        public static bool TryGet(string id, out TechDef technology)
        {
            if (string.IsNullOrEmpty(id))
            {
                technology = null;
                return false;
            }
            return ById.TryGetValue(id, out technology);
        }

        public static bool IsHistorical(string techId)
        {
            return !string.IsNullOrEmpty(techId) &&
                techId.StartsWith(HistoricalPrefix, StringComparison.Ordinal);
        }

        public static string TechIdForMilestone(string milestoneId)
        {
            return string.IsNullOrEmpty(milestoneId) ? null : HistoricalPrefix + milestoneId;
        }

        public static ResearchMilestoneDef MilestoneForTech(string techId)
        {
            if (!IsHistorical(techId)) return null;
            return ResearchMilestoneCatalog.Find(techId.Substring(HistoricalPrefix.Length));
        }

        public static List<TechDef> HistoricalForRegion(string regionJa)
        {
            var result = new List<TechDef>();
            var milestones = ResearchMilestoneCatalog.ForRegion(regionJa);
            for (int i = 0; i < milestones.Count; i++)
            {
                TechDef tech;
                if (TryGet(TechIdForMilestone(milestones[i].Id), out tech)) result.Add(tech);
            }
            return result;
        }

        static List<TechDef> BuildDefinitions()
        {
            var result = new List<TechDef>(GameRules.Techs.Count + ResearchMilestoneCatalog.All.Count);
            result.AddRange(GameRules.Techs);

            var previousByRegion = new Dictionary<string, string>(StringComparer.Ordinal);
            var tierByRegion = new Dictionary<string, int>(StringComparer.Ordinal);

            for (int i = 0; i < ResearchMilestoneCatalog.All.Count; i++)
            {
                var milestone = ResearchMilestoneCatalog.All[i];
                int tier;
                if (!tierByRegion.TryGetValue(milestone.RegionJa, out tier)) tier = 0;

                string[] prerequisites;
                string previous;
                if (previousByRegion.TryGetValue(milestone.RegionJa, out previous))
                    prerequisites = new[] { previous };
                else
                    prerequisites = (string[])RootPrerequisites.Clone();

                string id = TechIdForMilestone(milestone.Id);
                result.Add(new TechDef
                {
                    Id = id,
                    NameJa = milestone.NameJa,
                    Cost = HistoricalBaseCost + tier * HistoricalTierCost,
                    Prereqs = prerequisites,
                    DescJa = "世界史研究・" + milestone.RegionJa + "・" + milestone.DomainJa +
                        "：" + milestone.SummaryJa
                });

                previousByRegion[milestone.RegionJa] = id;
                tierByRegion[milestone.RegionJa] = tier + 1;
            }

            return result;
        }

        static Dictionary<string, TechDef> BuildIndex()
        {
            var result = new Dictionary<string, TechDef>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < Definitions.Count; i++) result.Add(Definitions[i].Id, Definitions[i]);
            return result;
        }
    }
}
