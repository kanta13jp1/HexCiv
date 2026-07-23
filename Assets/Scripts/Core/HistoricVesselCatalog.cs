using System;
using System.Collections.Generic;

namespace HexCiv.Core
{
    /// <summary>世界各地の船舶・造船・航海史を段階的に収録する台帳レコード。</summary>
    public sealed class HistoricVesselDef
    {
        public readonly string Id;
        public readonly string NameJa;
        public readonly string RegionJa;
        public readonly string EraJa;
        public readonly string TraditionJa;
        public readonly string RoleJa;
        public readonly string SummaryJa;

        public HistoricVesselDef(string id, string nameJa, string regionJa, string eraJa,
            string traditionJa, string roleJa, string summaryJa)
        {
            Id = id;
            NameJa = nameJa;
            RegionJa = regionJa;
            EraJa = eraJa;
            TraditionJa = traditionJa;
            RoleJa = roleJa;
            SummaryJa = summaryJa;
        }
    }

    /// <summary>歴史船舶台帳・第1弾。6地域×6件を一意IDで収録する純データ。</summary>
    public static class HistoricVesselCatalog
    {
        static readonly List<HistoricVesselDef> Definitions = new List<HistoricVesselDef>
        {
            V("egypt_papyrus_boat", "古代エジプトのパピルス舟", "アフリカ", "古代", "ナイル川舟運", "河川輸送", "束ねた葦を船体に用いた初期の河川舟"),
            V("khufu_ship", "クフ王の船", "アフリカ", "古代エジプト古王国", "木造船", "儀礼・航行", "大型木造船の接合技術を伝える王墓付属船"),
            V("felucca", "フェルーカ", "アフリカ", "古代〜現代", "ナイル・紅海帆走", "交易・旅客", "三角帆で風を捉える浅喫水の帆船"),
            V("mtepe", "ムテペ", "アフリカ", "中世〜近世", "スワヒリ海岸造船", "インド洋交易", "縫合船体と帆を備えた東アフリカの交易船"),
            V("jahazi", "ジャハジ", "アフリカ", "中世〜現代", "インド洋ダウ船", "交易・移住", "季節風航海で港湾社会を結んだ木造帆船"),
            V("madagascar_outrigger", "マダガスカルのアウトリガー舟", "アフリカ", "古代〜現代", "インド洋島嶼航海", "漁撈・沿岸移動", "外付け浮材で横安定を高めた沿岸舟"),

            V("guffa", "グッファ", "西・南アジア", "古代〜現代", "メソポタミア円形舟", "河川輸送", "葦や木枠を瀝青で防水した円形のコラクル"),
            V("tarada", "タラダ", "西・南アジア", "古代〜現代", "イラク湿地舟", "移動・漁撈", "湿地と河川を細長い船体で進む木造カヌー"),
            V("dhow", "ダウ船", "西・南アジア", "古代〜現代", "アラビア海帆走", "長距離交易", "三角帆と季節風航海でインド洋交易圏を結んだ船"),
            V("lenj", "レンジ船", "西・南アジア", "中世〜現代", "ペルシア湾造船", "交易・漁撈", "手工業的な造船と航海知識を継承する木造船"),
            V("pattamar", "パッタマール", "西・南アジア", "近世〜近代", "インド西岸帆走", "沿岸交易", "複数の帆でモンスーン海域を往来した貨物帆船"),
            V("baghlah", "バグラ", "西・南アジア", "近世", "湾岸大型ダウ", "外洋交易", "大型船体でペルシア湾とインド洋を結んだ交易船"),

            V("chinese_junk", "中国ジャンク", "東・東南アジア", "古代〜近代", "中国帆船造船", "交易・航海", "帆と水密隔壁を発達させた東アジアの代表的帆船"),
            V("treasure_ship", "宝船", "東・東南アジア", "明代", "鄭和船団", "遠洋航海・外交", "大規模船団で東南アジアからインド洋へ航海した大型船"),
            V("panokseon", "板屋船", "東・東南アジア", "朝鮮時代", "朝鮮水軍", "海戦", "高い上甲板と櫂走を備えた朝鮮半島の軍船"),
            V("turtle_ship", "亀甲船", "東・東南アジア", "朝鮮時代", "朝鮮水軍", "海戦", "覆いを持つ突撃用軍船として知られる艦種"),
            V("atakebune", "安宅船", "東・東南アジア", "戦国〜江戸初期", "日本水軍", "海戦・指揮", "大型上構を持ち水軍の中核となった和船"),
            V("pinisi", "ピニシ", "東・東南アジア", "近世〜現代", "南スラウェシ造船", "交易・航海", "オーストロネシア造船伝統を継ぐ帆装と帆船"),

            V("minoan_galley", "ミノアのガレー船", "ヨーロッパ・地中海", "青銅器時代", "エーゲ海航海", "交易・海上移動", "櫂走と帆走を組み合わせ島々を結んだ細長い船"),
            V("phoenician_bireme", "フェニキアの二段櫂船", "ヨーロッパ・地中海", "古代", "フェニキア航海", "交易・海戦", "地中海航路の拡大を支えた櫂走船"),
            V("greek_trireme", "ギリシアの三段櫂船", "ヨーロッパ・地中海", "古典古代", "ポリス海軍", "海戦", "多数の漕手と衝角を用いた高速の軍船"),
            V("roman_quinquereme", "ローマの五段櫂船", "ヨーロッパ・地中海", "共和政ローマ", "ローマ海軍", "海戦・輸送", "大型櫂走軍船として地中海戦争に投入された艦種"),
            V("viking_longship", "ヴァイキングのロングシップ", "ヨーロッパ・地中海", "中世前期", "北欧クリンカー造船", "航海・戦闘", "浅喫水と帆・櫂で外洋と河川を行動した長船"),
            V("caravel", "キャラベル船", "ヨーロッパ・地中海", "15〜16世紀", "イベリア外洋航海", "探検・交易", "操帆性を生かして大西洋航海に用いられた帆船"),

            V("tlingit_canoe", "トリンギットの外洋カヌー", "アメリカ大陸", "古代〜現代", "北西海岸木彫舟", "交易・資源採集", "杉の丸木を彫り島と沿岸を結んだ大型カヌー"),
            V("birchbark_canoe", "樺皮カヌー", "アメリカ大陸", "古代〜現代", "北米森林圏舟", "河川・湖沼移動", "軽量な樺皮船体で水系間を運搬できる舟"),
            V("tomol", "トモル", "アメリカ大陸", "先コロンブス期〜現代", "チュマシュ縫合板舟", "沿岸航海・交易", "板材を植物繊維で縫い海峡を渡った舟"),
            V("caballito_totora", "トトラ葦舟", "アメリカ大陸", "先コロンブス期〜現代", "アンデス葦舟", "漁撈・湖上移動", "トトラを束ね海岸と高地湖で用いた舟"),
            V("dalca", "ダルカ", "アメリカ大陸", "先コロンブス期〜近代", "南部チリ縫合板舟", "島嶼航海", "植物繊維で板を縫い合わせたパタゴニアの舟"),
            V("martinique_yole", "マルティニークのヨール", "アメリカ大陸", "近世〜現代", "カリブ海木造舟", "漁撈・競漕", "浅喫水の軽量船体と帆を乗員の体重移動で操る舟"),

            V("austronesian_outrigger", "オーストロネシアのアウトリガー舟", "オセアニア", "古代〜現代", "島嶼造船", "移住・漁撈", "外付け浮材により外洋での安定性を得た舟"),
            V("double_hulled_vaka", "双胴ヴァカ", "オセアニア", "古代〜現代", "ポリネシア航海", "長距離移住・交易", "二つの船体と帆で人員・物資を外洋輸送した航海カヌー"),
            V("lakatoi", "ラカトイ", "オセアニア", "近世〜現代", "パプア湾航海", "交易", "複数船体と帆で季節交易航海を行った大型舟"),
            V("chamorro_proa", "チャモロのプロア", "オセアニア", "古代〜近世", "ミクロネシア航海", "高速航海", "非対称船体とアウトリガーを用いた高速帆走舟"),
            V("te_puke", "テ・プケ", "オセアニア", "古代〜現代", "タウマコ航海", "外洋航海", "クラブクロウ帆と浮材を備える伝統的航海舟"),
            V("hokulea", "ホクレア", "オセアニア", "現代", "伝統航海復興", "教育・航海実証", "星・波・生物を読む非計器航法を継承する復元航海カヌー"),
        };

        static readonly IReadOnlyList<HistoricVesselDef> ReadOnlyDefinitions =
            Definitions.AsReadOnly();
        static readonly Dictionary<string, HistoricVesselDef> ById = BuildIndex();

        public static IReadOnlyList<HistoricVesselDef> All => ReadOnlyDefinitions;

        public static HistoricVesselDef Find(string id)
        {
            HistoricVesselDef result;
            return !string.IsNullOrEmpty(id) && ById.TryGetValue(id, out result) ? result : null;
        }

        public static List<HistoricVesselDef> ForRegion(string regionJa)
        {
            var result = new List<HistoricVesselDef>();
            bool all = string.IsNullOrEmpty(regionJa) || regionJa == GlobalHistoryIndex.AllRegions;
            for (int i = 0; i < Definitions.Count; i++)
                if (all || string.Equals(Definitions[i].RegionJa, regionJa,
                    StringComparison.Ordinal)) result.Add(Definitions[i]);
            return result;
        }

        static HistoricVesselDef V(string id, string name, string region, string era,
            string tradition, string role, string summary)
        {
            return new HistoricVesselDef(id, name, region, era, tradition, role, summary);
        }

        static Dictionary<string, HistoricVesselDef> BuildIndex()
        {
            var result = new Dictionary<string, HistoricVesselDef>(
                StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < Definitions.Count; i++)
                result.Add(Definitions[i].Id, Definitions[i]);
            return result;
        }
    }
}
