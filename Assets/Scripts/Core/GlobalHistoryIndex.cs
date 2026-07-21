using System;
using System.Collections.Generic;

namespace HexCiv.Core
{
    /// <summary>世界史総合索引に表示する一分類。Countは地域フィルター適用後の件数。</summary>
    public sealed class GlobalHistoryIndexEntry
    {
        public readonly string Id;
        public readonly string NameJa;
        public readonly int Count;
        public readonly string DetailJa;

        public GlobalHistoryIndexEntry(string id, string nameJa, int count, string detailJa)
        {
            Id = id;
            NameJa = nameJa;
            Count = count;
            DetailJa = detailJa;
        }
    }

    /// <summary>
    /// 個別台帳を横断して件数と地域分類を返す純Core索引。
    /// 台帳そのものを複製せず、既存の安定IDと定義を唯一の情報源にする。
    /// </summary>
    public static class GlobalHistoryIndex
    {
        public const string AllRegions = "すべて";

        public static List<GlobalHistoryIndexEntry> Entries(string regionJa)
        {
            var civilizations = CivilizationsForRegion(regionJa);
            var leaders = LeadersForRegion(regionJa);
            var sites = HeritageSiteCatalog.ForRegion(Filter(regionJa));
            var people = GreatPersonCatalog.ForRegion(Filter(regionJa));
            var works = MasterpieceCatalog.ForRegion(Filter(regionJa));
            var research = ResearchMilestoneCatalog.ForRegion(Filter(regionJa));
            var culture = CulturalTraditionCatalog.ForRegion(Filter(regionJa));
            bool all = IsAll(regionJa);

            return new List<GlobalHistoryIndexEntry>
            {
                Entry("civilizations", "文明", civilizations.Count,
                    "国家・帝国・都市国家・先住民社会を含むプレイアブル台帳"),
                Entry("leaders", "王・君主・指導者", leaders.Count,
                    "実在の統治者・政治指導者。個人名未詳は創作せず明記"),
                Entry("heritage", "遺跡・史跡", sites.Count,
                    "都市・祭祀・墓葬・産業・文化的景観などの遺産台帳"),
                Entry("great_people", "偉人", people.Count,
                    "学術・文化・技術・社会・探検・軍事の人物台帳"),
                Entry("books", "書籍", CountKind(works, MasterpieceKind.Book),
                    "粘土板・写本・巻物・口承叙事詩・小説を含む"),
                Entry("paintings", "絵画", CountKind(works, MasterpieceKind.Painting),
                    "壁画・絵巻・屏風・版画・樹皮画を含む"),
                Entry("sculptures", "彫刻", CountKind(works, MasterpieceKind.Sculpture),
                    "丸彫・浮彫・記念碑・儀礼造形を含む"),
                Entry("architecture", "建築", CountKind(works, MasterpieceKind.Architecture),
                    "単体建築・都市複合体・景観と一体の建築群を含む"),
                Entry("music", "音楽", CountKind(works, MasterpieceKind.Music),
                    "記譜作品・歌唱・器楽・舞踊と結び付く継承伝統を含む"),
                Entry("theater", "演劇", CountKind(works, MasterpieceKind.Theater),
                    "戯曲・仮面劇・人形劇・影絵・舞踊劇・即興演劇を含む"),
                Entry("film", "映画", CountKind(works, MasterpieceKind.Film),
                    "無声・劇映画・実験映画・記録映画・共同製作を含む"),
                Entry("research", "学問・科学技術", research.Count,
                    all ? "史実マイルストーン120件を基礎12技術の先へ接続（研究対象計132）"
                        : "地域別の学術・技術・知識体系の史実マイルストーン"),
                Entry("culture", "文化", culture.Count,
                    "言語・信仰・芸能・工芸・生活・社会制度を含む文化史台帳"),
            };
        }

        public static List<CivilizationDef> CivilizationsForRegion(string regionJa)
        {
            var result = new List<CivilizationDef>();
            for (int i = 0; i < CivilizationCatalog.All.Count; i++)
            {
                var civilization = CivilizationCatalog.All[i];
                if (IsAll(regionJa) || string.Equals(BroadRegion(civilization), regionJa,
                    StringComparison.Ordinal)) result.Add(civilization);
            }
            return result;
        }

        public static List<LeaderDef> LeadersForRegion(string regionJa)
        {
            var result = new List<LeaderDef>();
            for (int i = 0; i < LeaderCatalog.All.Count; i++)
            {
                var leader = LeaderCatalog.All[i];
                var civilization = CivilizationCatalog.Find(leader.CivilizationId);
                if (civilization != null && (IsAll(regionJa) ||
                    string.Equals(BroadRegion(civilization), regionJa, StringComparison.Ordinal)))
                    result.Add(leader);
            }
            return result;
        }

        /// <summary>文明台帳の詳細地域を、他の世界史台帳と共通の6地域へ写像する。</summary>
        public static string BroadRegion(CivilizationDef civilization)
        {
            if (civilization == null) return "";
            string region = civilization.RegionJa ?? "";
            if (region.Contains("アフリカ")) return "アフリカ";
            if (region.Contains("アメリカ") || region == "アンデス" ||
                region == "メソアメリカ" || region == "カリブ海") return "アメリカ大陸";
            if (region == "ポリネシア" || region == "メラネシア" ||
                region == "ミクロネシア" || region == "オセアニア") return "オセアニア";
            if (region.Contains("東アジア") || region.Contains("東南アジア"))
                return "東・東南アジア";
            if (region.Contains("西アジア") || region.Contains("南アジア") ||
                region.Contains("中央アジア") || region == "アナトリア" ||
                region == "レヴァント") return "西・南アジア";
            if (region.Contains("ヨーロッパ") || region.Contains("地中海"))
                return "ヨーロッパ・地中海";
            return "";
        }

        static GlobalHistoryIndexEntry Entry(string id, string name, int count, string detail)
        {
            return new GlobalHistoryIndexEntry(id, name, count, detail);
        }

        static int CountKind(List<MasterpieceDef> works, MasterpieceKind kind)
        {
            int count = 0;
            for (int i = 0; i < works.Count; i++) if (works[i].Kind == kind) count++;
            return count;
        }

        static bool IsAll(string regionJa)
        {
            return string.IsNullOrEmpty(regionJa) || regionJa == AllRegions;
        }

        static string Filter(string regionJa)
        {
            return IsAll(regionJa) ? null : regionJa;
        }
    }
}
