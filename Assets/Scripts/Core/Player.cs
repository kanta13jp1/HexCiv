using System.Collections.Generic;
using UnityEngine;

namespace HexCiv.Core
{
    /// <summary>1文明(プレイヤー)。人間かAIかは IsHuman で区別する。</summary>
    public class Player
    {
        public int Id;
        public string CivilizationId;
        public string NameJa;
        public string RegionJa;
        public string EraJa;
        public string LeaderId;
        public string LeaderNameJa;
        public string LeaderTitleJa;
        public Color Color;
        public bool IsHuman;
        public bool IsEliminated;

        public List<Unit> Units = new List<Unit>();
        public List<City> Cities = new List<City>();

        /// <summary>習得済み技術。開始時は GameRules.StartingTech のみ。</summary>
        public HashSet<string> KnownTechs = new HashSet<string> { GameRules.StartingTech };
        /// <summary>研究中の技術Id。null = 未選択。</summary>
        public string CurrentResearchId;
        /// <summary>蓄積科学。研究未選択でも貯まり、技術完成時に消費される。</summary>
        public int ScienceStored;

        /// <summary>採用済み文化政策。文化史132件の安定IDを保持する。</summary>
        public HashSet<string> KnownCulturePolicies = new HashSet<string>();
        /// <summary>現在、文化ポイントを投入している政策Id。null = 未選択。</summary>
        public string CurrentCulturePolicyId;
        /// <summary>次の政策採用へ使える文化ポイント。</summary>
        public int CultureStored;
        /// <summary>文明がゲーム中に生み出した文化の累計。文化防御と勝利判定に使う。</summary>
        public int TotalCulture;
        /// <summary>相手プレイヤーIdごとの文化的影響力。</summary>
        public Dictionary<int, int> CulturalInfluence = new Dictionary<int, int>();

        /// <summary>偉人登用に使えるポイント。</summary>
        public int GreatPersonPoints;
        /// <summary>ゲーム中に得た偉人ポイントの累計。</summary>
        public int TotalGreatPersonPoints;
        /// <summary>この文明が発見したマップ上の遺産Id。</summary>
        public HashSet<string> DiscoveredHeritageSites = new HashSet<string>();
        /// <summary>この文明が登用した偉人Id。偉人は世界全体で一度だけ登用できる。</summary>
        public HashSet<string> RecruitedGreatPeople = new HashSet<string>();

        /// <summary>作品の収蔵に使うポイント。</summary>
        public int MasterpiecePoints;
        /// <summary>ゲーム中に得た作品ポイントの累計。</summary>
        public int TotalMasterpiecePoints;
        /// <summary>この文明が収蔵した作品Id。作品は世界全体で一度だけ収蔵できる。</summary>
        public HashSet<string> CollectedMasterpieces = new HashSet<string>();

        // ---- 国家運営（バージョン10からセーブ対象） ----
        public int Treasury = AdministrationSystem.StartingTreasury;
        public TaxPolicy Taxation = TaxPolicy.Balanced;
        public int Stability = AdministrationSystem.StartingStability;
        public int WarWeariness;
        public int LastRevenue;
        public int LastExpenses;

        // ---- 人口社会（バージョン12からセーブ対象） ----
        public SocialFocus SocialFocus = SocialFocus.Balanced;

        // ---- 政治・法律（バージョン13からセーブ対象） ----
        public int PoliticalCapital = PoliticalSystem.StartingPoliticalCapital;
        public int Legitimacy = PoliticalSystem.StartingLegitimacy;
        public CivicLaw ActiveLaw = CivicLaw.CouncilOfElders;
        public int ScholarSupport = PoliticalSystem.StartingSupport;
        public int MerchantSupport = PoliticalSystem.StartingSupport;
        public int TraditionalSupport = PoliticalSystem.StartingSupport;
        public int MilitarySupport = PoliticalSystem.StartingSupport;

        public HashSet<HexCoord> Explored = new HashSet<HexCoord>();
        public HashSet<HexCoord> Visible = new HashSet<HexCoord>();

        /// <summary>首都の都市Id(-1 = なし)。</summary>
        public int CapitalCityId = -1;

        public HashSet<int> AtWarWith = new HashSet<int>();

        /// <summary>
        /// 開戦ターン(相手プレイヤーId → 宣戦時の TurnNumber)。AIの和平判断で戦争の
        /// 経過ターンを求めるために使う(2026-07-20 追加)。AtWarWith と対で維持される。
        /// </summary>
        public Dictionary<int, int> WarStartTurns = new Dictionary<int, int>();

        public bool IsAtWarWith(int playerId) => AtWarWith.Contains(playerId);

        /// <summary>宣戦布告(相互)。開戦ターンを両者に記録する。</summary>
        public void DeclareWarOn(GameState s, Player other)
        {
            if (other == null || other == this || other.Id == Id) return;
            if (AtWarWith.Contains(other.Id)) return;
            AtWarWith.Add(other.Id);
            other.AtWarWith.Add(Id);
            int turn = s != null ? s.TurnNumber : 0;
            WarStartTurns[other.Id] = turn;
            other.WarStartTurns[Id] = turn;
            if (s != null) s.EmitLog($"「{NameJa}」が「{other.NameJa}」に宣戦布告した!");
            if (s != null) s.RaiseWarDeclared(this, other);   // 型付きイベント(2026-07-20 追加)
        }

        /// <summary>和平(相互)。交戦状態と開戦ターンを両者から取り除く(2026-07-20 追加)。</summary>
        public void MakePeaceWith(GameState s, Player other)
        {
            if (other == null || other == this || other.Id == Id) return;
            if (!AtWarWith.Contains(other.Id)) return;
            AtWarWith.Remove(other.Id);
            other.AtWarWith.Remove(Id);
            WarStartTurns.Remove(other.Id);
            other.WarStartTurns.Remove(Id);
            if (s != null)
            {
                s.EmitLog($"「{NameJa}」と「{other.NameJa}」が和平した");
                s.RaisePeaceMade(this, other);   // 型付きイベント(2026-07-20 追加)
                s.Bump();
            }
        }

        /// <summary>毎ターンの科学産出 = 首都ボーナス + Σ都市(人口 + 建物の科学)。</summary>
        public int SciencePerTurn(GameState s)
        {
            int total = 0;
            bool capitalAlive = false;
            for (int i = 0; i < Cities.Count; i++)
            {
                var c = Cities[i];
                if (c.Id == CapitalCityId) capitalAlive = true;
                int cityScience = c.Population;
                for (int j = 0; j < c.Buildings.Count; j++)
                    cityScience += GameRules.GetBuilding(c.Buildings[j]).Bonus.Science;
                cityScience += PopulationSystem.ScienceBonus(this, c);
                total += cityScience;
            }
            if (capitalAlive && CapitalCityId >= 0) total += GameRules.BaseSciencePerCiv;
            total += MasterpieceSystem.SciencePerTurnBonus(this);
            total = CultureSystem.ScaleScience(this, total);
            total = AdministrationSystem.ScaleOutput(this, total);
            // AI文明は難易度に応じて科学産出を補正(普通=100%で無変換。2026-07-20 追加)
            return DifficultyRules.ScaleForAI(s, this, total, DifficultyRules.AISciencePercent);
        }

        /// <summary>研究可能な技術(前提を満たし、未習得)。</summary>
        public List<TechDef> AvailableTechs()
        {
            var list = new List<TechDef>();
            foreach (var t in TechnologyCatalog.All)
            {
                if (KnownTechs.Contains(t.Id)) continue;
                bool ok = true;
                for (int i = 0; i < t.Prereqs.Length; i++)
                    if (!KnownTechs.Contains(t.Prereqs[i])) { ok = false; break; }
                if (ok) list.Add(t);
            }
            return list;
        }

        public void SetResearch(string techId)
        {
            CurrentResearchId = string.IsNullOrEmpty(techId) ? null : techId;
        }

        /// <summary>前提を満たす未採用の文化政策。</summary>
        public List<CulturePolicyDef> AvailableCulturePolicies()
        {
            return CultureSystem.AvailablePolicies(this);
        }

        public void SetCulturePolicy(string policyId)
        {
            CurrentCulturePolicyId = string.IsNullOrEmpty(policyId) ? null : policyId;
        }

        /// <summary>null/空 なら常に true(技術不要)。</summary>
        public bool HasTech(string techId)
        {
            return string.IsNullOrEmpty(techId) || KnownTechs.Contains(techId);
        }

        /// <summary>次の都市名。文明台帳から未使用のものを選ぶ。旧セーブはGameConfigへフォールバックする。</summary>
        public string NextCityName(GameState s)
        {
            var civilization = CivilizationCatalog.Find(CivilizationId) ?? CivilizationCatalog.FindByName(NameJa);
            string[] names = civilization != null ? civilization.CityNames : null;
            if (names == null || names.Length == 0)
            {
                int idx = ((Id % GameConfig.CityNames.Length) + GameConfig.CityNames.Length) % GameConfig.CityNames.Length;
                names = GameConfig.CityNames[idx];
            }
            var used = new HashSet<string>();
            if (s != null)
                foreach (var c in s.AllCities) used.Add(c.NameJa);

            for (int i = 0; i < names.Length; i++)
                if (!used.Contains(names[i])) return names[i];

            for (int n = 1; ; n++)
            {
                string candidate = $"新しい都市{n}";
                if (!used.Contains(candidate)) return candidate;
            }
        }

        /// <summary>軍事力 = Σ非民間ユニット (Strength + RangedStrength) × hp/100。</summary>
        public int MilitaryPower()
        {
            float sum = 0f;
            for (int i = 0; i < Units.Count; i++)
            {
                var u = Units[i];
                if (u.Def.IsCivilian) continue;
                sum += (u.Def.Strength + u.Def.RangedStrength) * (u.Hp / 100f);
            }
            return (int)sum;
        }
    }
}
