using System;
using System.Collections.Generic;

namespace HexCiv.Core
{
    /// <summary>文化政策が文明へ与える恒久効果。</summary>
    public enum CulturePolicyEffect
    {
        Culture,
        Science,
        Production,
        Influence,
    }

    /// <summary>文化史台帳の1件を、採用可能な政策としてゲーム化した定義。</summary>
    public sealed class CulturePolicyDef
    {
        public readonly string Id;
        public readonly string NameJa;
        public readonly int Cost;
        public readonly string[] Prereqs;
        public readonly CulturalTraditionDef Tradition;
        public readonly CulturePolicyEffect Effect;
        public readonly int EffectValue;
        public readonly string EffectTextJa;

        public CulturePolicyDef(string id, string nameJa, int cost, string[] prereqs,
            CulturalTraditionDef tradition, CulturePolicyEffect effect, int effectValue,
            string effectTextJa)
        {
            Id = id;
            NameJa = nameJa;
            Cost = cost;
            Prereqs = prereqs ?? new string[0];
            Tradition = tradition;
            Effect = effect;
            EffectValue = effectValue;
            EffectTextJa = effectTextJa;
        }
    }

    /// <summary>
    /// 文化史120件を6地域×20段階の政策ツリーとして提供する。
    /// 地域内の収録順を前提関係に使い、文化の優劣を示す順位付けには用いない。
    /// </summary>
    public static class CulturePolicyCatalog
    {
        public const string PolicyPrefix = "policy_";
        public const int BaseCost = 30;
        public const int TierCost = 20;

        static readonly List<CulturePolicyDef> Definitions = BuildDefinitions();
        static readonly IReadOnlyList<CulturePolicyDef> ReadOnlyDefinitions = Definitions.AsReadOnly();
        static readonly Dictionary<string, CulturePolicyDef> ById = BuildIndex();

        public static IReadOnlyList<CulturePolicyDef> All => ReadOnlyDefinitions;

        public static string PolicyIdForTradition(string traditionId)
        {
            return string.IsNullOrEmpty(traditionId) ? null : PolicyPrefix + traditionId;
        }

        public static CulturalTraditionDef TraditionForPolicy(string policyId)
        {
            if (string.IsNullOrEmpty(policyId) ||
                !policyId.StartsWith(PolicyPrefix, StringComparison.Ordinal)) return null;
            return CulturalTraditionCatalog.Find(policyId.Substring(PolicyPrefix.Length));
        }

        public static CulturePolicyDef Get(string id)
        {
            CulturePolicyDef result;
            if (string.IsNullOrEmpty(id) || !ById.TryGetValue(id, out result))
                throw new KeyNotFoundException("Unknown culture policy id: " + id);
            return result;
        }

        public static bool TryGet(string id, out CulturePolicyDef policy)
        {
            if (string.IsNullOrEmpty(id))
            {
                policy = null;
                return false;
            }
            return ById.TryGetValue(id, out policy);
        }

        public static List<CulturePolicyDef> ForRegion(string regionJa)
        {
            var result = new List<CulturePolicyDef>();
            for (int i = 0; i < Definitions.Count; i++)
                if (string.IsNullOrEmpty(regionJa) ||
                    Definitions[i].Tradition.RegionJa == regionJa)
                    result.Add(Definitions[i]);
            return result;
        }

        public static int EffectTotal(Player player, CulturePolicyEffect effect)
        {
            if (player == null || player.KnownCulturePolicies == null) return 0;
            int total = 0;
            foreach (string id in player.KnownCulturePolicies)
            {
                CulturePolicyDef policy;
                if (TryGet(id, out policy) && policy.Effect == effect)
                    total += policy.EffectValue;
            }
            return total;
        }

        static List<CulturePolicyDef> BuildDefinitions()
        {
            var result = new List<CulturePolicyDef>(CulturalTraditionCatalog.All.Count);
            var previousByRegion = new Dictionary<string, string>(StringComparer.Ordinal);
            var tierByRegion = new Dictionary<string, int>(StringComparer.Ordinal);

            for (int i = 0; i < CulturalTraditionCatalog.All.Count; i++)
            {
                var tradition = CulturalTraditionCatalog.All[i];
                int tier;
                if (!tierByRegion.TryGetValue(tradition.RegionJa, out tier)) tier = 0;

                string previous;
                string[] prereqs = previousByRegion.TryGetValue(tradition.RegionJa, out previous)
                    ? new[] { previous }
                    : new string[0];

                var effect = (CulturePolicyEffect)(tier % 4);
                int value;
                string effectText;
                switch (effect)
                {
                    case CulturePolicyEffect.Science:
                        value = 1;
                        effectText = "科学力+1%";
                        break;
                    case CulturePolicyEffect.Production:
                        value = 1;
                        effectText = "都市の生産力+1%";
                        break;
                    case CulturePolicyEffect.Influence:
                        value = 1;
                        effectText = "文化的影響力+1/文明・ターン";
                        break;
                    default:
                        value = 1;
                        effectText = "文化力+1/ターン";
                        break;
                }

                string id = PolicyIdForTradition(tradition.Id);
                result.Add(new CulturePolicyDef(
                    id,
                    tradition.NameJa,
                    BaseCost + tier * TierCost,
                    prereqs,
                    tradition,
                    effect,
                    value,
                    effectText));

                previousByRegion[tradition.RegionJa] = id;
                tierByRegion[tradition.RegionJa] = tier + 1;
            }

            return result;
        }

        static Dictionary<string, CulturePolicyDef> BuildIndex()
        {
            var result = new Dictionary<string, CulturePolicyDef>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < Definitions.Count; i++)
                result.Add(Definitions[i].Id, Definitions[i]);
            return result;
        }
    }
}
