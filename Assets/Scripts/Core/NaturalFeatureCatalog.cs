using System;
using System.Collections.Generic;

namespace HexCiv.Core
{
    /// <summary>自然地理台帳の分類。値は図鑑IDの意味を固定するため順序を変更しない。</summary>
    public enum NaturalFeatureKind
    {
        Mountain,
        River,
        Sea,
        Lake,
        Forest,
        Desert,
    }

    /// <summary>実在する自然地理を記録する一項目。手続き生成世界の地名には流用しない。</summary>
    public sealed class NaturalFeatureDef
    {
        public readonly string Id;
        public readonly string NameJa;
        public readonly NaturalFeatureKind Kind;
        public readonly string RegionJa;
        public readonly string LocationJa;
        public readonly string FormJa;
        public readonly string SummaryJa;

        public NaturalFeatureDef(string id, string nameJa, NaturalFeatureKind kind,
            string regionJa, string locationJa, string formJa, string summaryJa)
        {
            Id = id;
            NameJa = nameJa;
            Kind = kind;
            RegionJa = regionJa;
            LocationJa = locationJa;
            FormJa = formJa;
            SummaryJa = summaryJa;
        }

        public string KindNameJa => NaturalFeatureCatalog.KindNameJa(Kind);
    }

    /// <summary>
    /// 山・川・海域・湖・森林・砂漠の継続増補台帳。
    /// 各分類12件・各広域12件の計72件を安定IDで保持する。
    /// 境界を単一国家の所有物とせず、複数の呼称や越境性を説明へ残す。
    /// </summary>
    public static class NaturalFeatureCatalog
    {
        static readonly NaturalFeatureDef[] Definitions =
        {
            // ---- アフリカ（各分類2件） ----
            F("kilimanjaro", "キリマンジャロ", NaturalFeatureKind.Mountain, "アフリカ", "タンザニア", "火山体・高山", "赤道近くに氷雪帯を持つ火山群。標高帯に応じて植生と土地利用が大きく変わる。"),
            F("atlas_mountains", "アトラス山脈", NaturalFeatureKind.Mountain, "アフリカ", "モロッコ・アルジェリア・チュニジア", "山脈", "地中海岸とサハラの間に連なり、水系、放牧、都市と隊商路の環境を形づくる。"),
            F("nile", "ナイル川", NaturalFeatureKind.River, "アフリカ", "東部・北東部アフリカ", "国際河川", "複数の源流域から北へ流れ、氾濫原、灌漑、航行と長い都市史を結んできた。"),
            F("congo_river", "コンゴ川", NaturalFeatureKind.River, "アフリカ", "中央アフリカ", "大河川・流域", "広大な熱帯流域を排水し、急流と航行可能区間が交通と生態系を分節する。"),
            F("gulf_of_guinea", "ギニア湾", NaturalFeatureKind.Sea, "アフリカ", "西アフリカ沿岸", "湾・海域", "大西洋東部の湾岸海域。河川流入、漁業、港湾、海上交易と深く関わる。"),
            F("mozambique_channel", "モザンビーク海峡", NaturalFeatureKind.Sea, "アフリカ", "モザンビークとマダガスカルの間", "海峡・海域", "インド洋西部の海峡で、季節風航海、生物移動、沿岸社会を結ぶ。"),
            F("lake_victoria", "ヴィクトリア湖", NaturalFeatureKind.Lake, "アフリカ", "ウガンダ・ケニア・タンザニア", "淡水湖", "多数の流域共同体を結ぶ大湖。漁業、交通、都市化と生態系変化を併せて考える必要がある。"),
            F("lake_tanganyika", "タンガニーカ湖", NaturalFeatureKind.Lake, "アフリカ", "東アフリカ大地溝帯", "地溝湖", "細長く深い地溝湖で、固有性の高い生態系と沿岸交通を支える。"),
            F("congo_basin_forest", "コンゴ盆地森林", NaturalFeatureKind.Forest, "アフリカ", "中央アフリカ", "熱帯林景観", "河川網と多様な森林型からなる広域景観。地域社会の暮らしと地球規模の炭素循環に関わる。"),
            F("miombo_woodlands", "ミオンボ林地", NaturalFeatureKind.Forest, "アフリカ", "南部・中南部アフリカ", "季節性疎開林", "乾季と雨季に適応した広域林地で、採集、農耕、火管理と野生生物の生息地を支える。"),
            F("sahara", "サハラ砂漠", NaturalFeatureKind.Desert, "アフリカ", "北アフリカ", "高温乾燥地域", "砂丘だけでなく礫原、岩石地、山地、オアシスを含む。隊商と牧畜の移動史も持つ。"),
            F("namib", "ナミブ砂漠", NaturalFeatureKind.Desert, "アフリカ", "ナミビアを中心とする大西洋岸", "沿岸砂漠", "寒流と霧の影響を受ける沿岸乾燥地で、砂丘と独特の生態系が連なる。"),

            // ---- 西・南アジア ----
            F("himalaya", "ヒマラヤ山脈", NaturalFeatureKind.Mountain, "西・南アジア", "南アジアとチベット高原の間", "造山帯・山脈", "多数の大河の源流域を抱え、高度差が気候、農牧、信仰、移動路を形づくる。"),
            F("zagros", "ザグロス山脈", NaturalFeatureKind.Mountain, "西・南アジア", "イラン西部・イラク北東部", "褶曲山脈", "高原とメソポタミア低地を分け、牧畜移動、水源、集落史に影響してきた。"),
            F("indus", "インダス川", NaturalFeatureKind.River, "西・南アジア", "チベット高原・南アジア", "国際河川", "高地から乾燥平野へ流れ、灌漑農業、都市文明、現在の水管理を支える。"),
            F("ganges_ganga", "ガンガー（ガンジス川）", NaturalFeatureKind.River, "西・南アジア", "インド北部・バングラデシュ", "大河川・デルタ水系", "農業と都市を支える一方、宗教的意味を持つ。呼称と流域の多様性を併記する。"),
            F("arabian_sea", "アラビア海", NaturalFeatureKind.Sea, "西・南アジア", "アラビア半島と南アジアの間", "海・季節風海域", "季節風と海流を利用する航海が、インド洋各地の港市と交易を結んだ。"),
            F("persian_gulf", "ペルシア湾", NaturalFeatureKind.Sea, "西・南アジア", "イラン高原とアラビア半島の間", "湾・浅海域", "河口、真珠採取、港市、石油産業が重なる。地域による呼称差にも留意する。"),
            F("caspian_sea", "カスピ海", NaturalFeatureKind.Lake, "西・南アジア", "西アジア・中央アジア境界", "閉鎖性塩湖", "世界最大級の閉鎖性内水域。河川流入、漁業、資源開発と水位変動が関わる。"),
            F("dead_sea", "死海", NaturalFeatureKind.Lake, "西・南アジア", "ヨルダン地溝帯", "高塩分閉鎖湖", "流出河川を持たない塩湖で、低い湖面標高と急速な水位低下が知られる。"),
            F("sundarbans", "シュンドルボン", NaturalFeatureKind.Forest, "西・南アジア", "ガンガー・ブラマプトラ・メグナ河口", "マングローブ林", "潮汐と淡水が交わる森林景観。高潮緩和、生業、生物多様性を支える越境地域である。"),
            F("hyrcanian_forests", "ヒルカニア森林", NaturalFeatureKind.Forest, "西・南アジア", "カスピ海南岸", "温帯広葉樹林", "山地と海の間に残る古い森林系統で、多様な標高帯と固有生物を含む。"),
            F("arabian_desert", "アラビア砂漠", NaturalFeatureKind.Desert, "西・南アジア", "アラビア半島", "亜熱帯乾燥地域", "砂海、礫地、台地を含み、オアシス、牧畜移動、巡礼路と都市をつなぐ。"),
            F("thar_desert", "タール砂漠", NaturalFeatureKind.Desert, "西・南アジア", "インド北西部・パキスタン東部", "季節風縁辺砂漠", "乾燥地でありながら人口と農牧活動が多く、季節風と灌漑の影響を強く受ける。"),

            // ---- 東・東南アジア ----
            F("mount_fuji", "富士山", NaturalFeatureKind.Mountain, "東・東南アジア", "日本・本州", "成層火山", "火山地形、水源、巡礼、芸術表現が重なり、周辺の土地利用と防災にも関わる。"),
            F("mount_kinabalu", "キナバル山", NaturalFeatureKind.Mountain, "東・東南アジア", "マレーシア・ボルネオ島", "花崗岩山塊", "急な標高差が多様な生態帯を生み、先住共同体の文化的景観とも結び付く。"),
            F("yangtze", "長江", NaturalFeatureKind.River, "東・東南アジア", "中国", "大河川", "高原から東シナ海へ流れ、農業、都市、航運、水利と大規模な環境改変を結ぶ。"),
            F("mekong", "メコン川", NaturalFeatureKind.River, "東・東南アジア", "中国南部・大陸部東南アジア", "国際河川", "季節的な増水が漁業と農業を支え、複数国家の水利用と生態系を結ぶ。"),
            F("south_china_sea", "南シナ海", NaturalFeatureKind.Sea, "東・東南アジア", "東南アジアと中国南部の間", "縁海", "島嶼、浅海、深海盆を含む交通の要衝。領有主張とは分けて自然・航海史を扱う。"),
            F("sea_of_japan", "日本海", NaturalFeatureKind.Sea, "東・東南アジア", "日本列島・朝鮮半島・ロシア沿岸の間", "縁海", "海流と季節風が漁業、降雪、港湾交流に影響する。呼称の国際的差異にも留意する。"),
            F("lake_baikal", "バイカル湖", NaturalFeatureKind.Lake, "東・東南アジア", "ロシア・シベリア南部", "地溝湖・淡水湖", "非常に深く古い湖で、固有生物と大量の淡水を持つ。周辺諸民族の生活文化とも結び付く。"),
            F("tonle_sap", "トンレサップ湖", NaturalFeatureKind.Lake, "東・東南アジア", "カンボジア", "季節変動湖・氾濫原", "メコン川の増水期に流向と面積が変化し、漁業、農業、湖上生活を支える。"),
            F("borneo_rainforest", "ボルネオ熱帯林", NaturalFeatureKind.Forest, "東・東南アジア", "ボルネオ島", "熱帯雨林景観", "低地から山地まで多様な森林が続き、先住社会、生物多様性、伐採・農園化の問題が重なる。"),
            F("yakushima_forest", "屋久島の森林", NaturalFeatureKind.Forest, "東・東南アジア", "日本・屋久島", "垂直分布森林", "海岸近くから高地まで植生帯が連なり、長寿のスギを含む湿潤な島嶼森林である。"),
            F("gobi", "ゴビ砂漠", NaturalFeatureKind.Desert, "東・東南アジア", "モンゴル・中国北部", "冷涼乾燥地域", "礫地と岩地が多く、寒暖差の大きい乾燥地。牧畜移動と古生物学資料でも知られる。"),
            F("taklamakan", "タクラマカン砂漠", NaturalFeatureKind.Desert, "東・東南アジア", "中国・タリム盆地", "内陸砂砂漠", "高山に囲まれた乾燥盆地で、周縁オアシスがシルクロードの交通を支えた。"),

            // ---- ヨーロッパ・地中海 ----
            F("alps", "アルプス山脈", NaturalFeatureKind.Mountain, "ヨーロッパ・地中海", "中南部ヨーロッパ", "山脈・氷河地形", "複数国家にまたがり、河川源流、峠道、牧畜、観光と氷河変動を結ぶ。"),
            F("mount_etna", "エトナ山", NaturalFeatureKind.Mountain, "ヨーロッパ・地中海", "イタリア・シチリア島", "活火山", "長期にわたる噴火活動が土壌、集落、農業、信仰と火山監視を形づくる。"),
            F("danube", "ドナウ川", NaturalFeatureKind.River, "ヨーロッパ・地中海", "中部・東南ヨーロッパ", "国際河川", "多数の国家と都市を通り、内陸航行、湿地、生態回廊と国際水管理を結ぶ。"),
            F("rhine", "ライン川", NaturalFeatureKind.River, "ヨーロッパ・地中海", "西ヨーロッパ", "国際河川", "アルプス圏から北海へ流れ、工業、港湾、交通、国境形成と河川再生の歴史を持つ。"),
            F("mediterranean_sea", "地中海", NaturalFeatureKind.Sea, "ヨーロッパ・地中海", "ヨーロッパ・アフリカ・西アジアの間", "大陸間海", "多数の海盆と島を含み、航海、移住、交易、生態交流を通じて三大陸を結ぶ。"),
            F("baltic_sea", "バルト海", NaturalFeatureKind.Sea, "ヨーロッパ・地中海", "北ヨーロッパ", "汽水性内海", "塩分の低い半閉鎖海で、河川流入、港湾、富栄養化と沿岸協力が関わる。"),
            F("lake_ladoga", "ラドガ湖", NaturalFeatureKind.Lake, "ヨーロッパ・地中海", "ロシア北西部", "淡水湖", "氷河地形に形成された大湖で、ネヴァ川水系、交通、戦争史と生態系に関わる。"),
            F("lake_geneva_leman", "レマン湖（ジュネーヴ湖）", NaturalFeatureKind.Lake, "ヨーロッパ・地中海", "スイス・フランス", "氷河湖", "ローヌ川が通過する越境湖。都市、ブドウ栽培、観光と水質管理を結ぶ。"),
            F("bialowieza_forest", "ビャウォヴィエジャ森林", NaturalFeatureKind.Forest, "ヨーロッパ・地中海", "ポーランド・ベラルーシ", "温帯原生林景観", "低地性の古い森林が越境して残り、大型動物と長期的な保護・利用の議論を抱える。"),
            F("black_forest", "シュヴァルツヴァルト（黒い森）", NaturalFeatureKind.Forest, "ヨーロッパ・地中海", "ドイツ南西部", "山地森林", "ライン上流域の山地林で、林業、集落、水源、工芸と観光の景観を形成する。"),
            F("tabernas_desert", "タベルナス砂漠", NaturalFeatureKind.Desert, "ヨーロッパ・地中海", "スペイン南東部", "半乾燥悪地", "少雨と侵食による裸地景観が発達し、地中海性乾燥地の生態と土地利用を示す。"),
            F("bardenas_reales", "バルデナス・レアレス", NaturalFeatureKind.Desert, "ヨーロッパ・地中海", "スペイン・ナバラ", "半乾燥悪地", "粘土・砂岩の侵食地形と草原が入り交じり、放牧と軍事利用を含む複合景観である。"),

            // ---- アメリカ大陸 ----
            F("andes", "アンデス山脈", NaturalFeatureKind.Mountain, "アメリカ大陸", "南アメリカ西縁", "造山帯・火山弧", "長大な高地帯で、氷河、乾燥高原、農牧、鉱業と多数の先住社会の歴史を含む。"),
            F("rocky_mountains", "ロッキー山脈", NaturalFeatureKind.Mountain, "アメリカ大陸", "北アメリカ西部", "山脈", "大陸分水界の一部をなし、水源、野生生物、先住領域、資源利用と保護地域を結ぶ。"),
            F("amazon_river", "アマゾン川", NaturalFeatureKind.River, "アメリカ大陸", "南アメリカ北部", "大河川・流域", "多数の支流と氾濫原を持ち、森林生態系、河川交通、都市と先住地域を結ぶ。"),
            F("mississippi_missouri", "ミシシッピ＝ミズーリ川水系", NaturalFeatureKind.River, "アメリカ大陸", "北アメリカ中央部", "大河川水系", "広大な農業・都市流域を排水し、航運、堤防、氾濫原改変と河口湿地に影響する。"),
            F("caribbean_sea", "カリブ海", NaturalFeatureKind.Sea, "アメリカ大陸", "中米・アンティル諸島・南米北岸の間", "海・島嶼海域", "島嶼と大陸沿岸を結び、海流、サンゴ礁、植民地交易、移住とハリケーン史を持つ。"),
            F("gulf_of_mexico", "メキシコ湾", NaturalFeatureKind.Sea, "アメリカ大陸", "北アメリカ南東部", "湾・海域", "河川流入と湿地、漁業、海流、港湾・エネルギー産業、暴風災害が重なる。"),
            F("great_lakes", "北米五大湖", NaturalFeatureKind.Lake, "アメリカ大陸", "カナダ・アメリカ合衆国", "連結淡水湖群", "氷河起源の五湖が水路で連なり、都市、工業、航運、越境水管理を支える。"),
            F("lake_titicaca", "チチカカ湖", NaturalFeatureKind.Lake, "アメリカ大陸", "ペルー・ボリビア高原", "高地淡水湖", "アンデス高原の生活、農牧、航行、信仰と考古学的景観を結ぶ越境湖である。"),
            F("amazon_rainforest", "アマゾン熱帯林", NaturalFeatureKind.Forest, "アメリカ大陸", "南アメリカ北部", "熱帯林景観", "多数の森林型、河川、先住地域からなり、生物多様性、炭素循環と急速な土地利用変化に関わる。"),
            F("tongass_forest", "トンガス森林", NaturalFeatureKind.Forest, "アメリカ大陸", "アラスカ南東部", "温帯雨林", "フィヨルドと島々に広がる湿潤な森林で、サケ水系、先住社会、林業と保全が関わる。"),
            F("atacama", "アタカマ砂漠", NaturalFeatureKind.Desert, "アメリカ大陸", "南米太平洋岸・高原西縁", "極端乾燥地域", "寒流と雨陰の影響を受け、塩湖、鉱業、天文観測、先住地域の水利用が重なる。"),
            F("great_basin_desert", "グレートベースン砂漠", NaturalFeatureKind.Desert, "アメリカ大陸", "アメリカ合衆国西部", "冷涼内陸砂漠", "外洋へ流出しない盆地群に広がり、塩湖、山地林、先住領域と乾燥地農牧を含む。"),

            // ---- オセアニア ----
            F("great_dividing_range", "グレートディヴァイディング山脈", NaturalFeatureKind.Mountain, "オセアニア", "オーストラリア東部", "山地帯・分水界", "東岸河川と内陸流域を分け、森林、農牧、都市水源と先住諸民族の国土観に関わる。"),
            F("aoraki_mount_cook", "アオラキ／マウント・クック", NaturalFeatureKind.Mountain, "オセアニア", "ニュージーランド南島", "氷河性高山", "南アルプスの高峰。マオリの地名と関係性、氷河景観、登山史を併記して扱う。"),
            F("murray_darling", "マレー＝ダーリング川水系", NaturalFeatureKind.River, "オセアニア", "オーストラリア南東部", "内陸河川水系", "農業と湿地を支える一方、流量配分、塩類化、先住諸民族の水文化をめぐる課題を持つ。"),
            F("sepik_river", "セピック川", NaturalFeatureKind.River, "オセアニア", "パプアニューギニア", "大河川・氾濫原", "蛇行する本流と湿地が集落、交通、食料、生物多様性と造形文化を支える。"),
            F("coral_sea", "珊瑚海", NaturalFeatureKind.Sea, "オセアニア", "オーストラリア北東・メラネシア南方", "海・サンゴ礁海域", "サンゴ礁、深海盆、島嶼を含み、海流、生物移動、航海と気候変動の影響が重なる。"),
            F("tasman_sea", "タスマン海", NaturalFeatureKind.Sea, "オセアニア", "オーストラリアとニュージーランドの間", "海域", "南西太平洋の海流と偏西風の影響を受け、航海、気象、生物分布を結ぶ。"),
            F("kati_thanda_lake_eyre", "カティ・サンダ／レイク・エア", NaturalFeatureKind.Lake, "オセアニア", "オーストラリア内陸部", "間欠性塩湖", "大雨の時に広い水面となる内陸湖。アラバナの文化的関係と流域生態を尊重して併記する。"),
            F("lake_taupo", "タウポ湖", NaturalFeatureKind.Lake, "オセアニア", "ニュージーランド北島", "火山カルデラ湖", "大規模噴火で形成された湖で、水系、地熱活動、ンガーティ・トゥファレトアとの関係を持つ。"),
            F("daintree_rainforest", "デインツリー熱帯林", NaturalFeatureKind.Forest, "オセアニア", "オーストラリア北東部", "湿潤熱帯林", "山地から海岸へ続く古い熱帯林で、サンゴ礁、生物多様性、東部クク・ヤランジの文化的景観と結ぶ。"),
            F("te_urewera", "テ・ウレウェラ", NaturalFeatureKind.Forest, "オセアニア", "ニュージーランド北島", "森林・湖沼景観", "トゥホエとの関係を基礎に、自然そのものの法的地位を認めた森林・湖沼景観である。"),
            F("great_victoria_desert", "グレートビクトリア砂漠", NaturalFeatureKind.Desert, "オセアニア", "オーストラリア南部内陸", "乾燥砂丘地域", "砂丘、礫地、塩湖、低木地が広がり、複数の先住諸民族のカントリーにまたがる。"),
            F("simpson_desert", "シンプソン砂漠", NaturalFeatureKind.Desert, "オセアニア", "オーストラリア中央東部", "縦列砂丘砂漠", "長い平行砂丘と間欠水系を持ち、乾燥地生態とワンカンガルや東部アランダなどのカントリーに関わる。"),
        };

        static readonly Dictionary<string, NaturalFeatureDef> ById = BuildIndex();
        public static IReadOnlyList<NaturalFeatureDef> All => Definitions;

        static NaturalFeatureDef F(string id, string name, NaturalFeatureKind kind,
            string region, string location, string form, string summary)
        {
            return new NaturalFeatureDef(id, name, kind, region, location, form, summary);
        }

        static Dictionary<string, NaturalFeatureDef> BuildIndex()
        {
            var result = new Dictionary<string, NaturalFeatureDef>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < Definitions.Length; i++) result.Add(Definitions[i].Id, Definitions[i]);
            return result;
        }

        public static NaturalFeatureDef Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            NaturalFeatureDef value;
            return ById.TryGetValue(id, out value) ? value : null;
        }

        public static List<NaturalFeatureDef> ForRegion(string regionJa)
        {
            var result = new List<NaturalFeatureDef>();
            bool all = string.IsNullOrEmpty(regionJa) || regionJa == GlobalHistoryIndex.AllRegions;
            for (int i = 0; i < Definitions.Length; i++)
                if (all || string.Equals(Definitions[i].RegionJa, regionJa, StringComparison.Ordinal))
                    result.Add(Definitions[i]);
            return result;
        }

        public static int CountKind(List<NaturalFeatureDef> items, NaturalFeatureKind kind)
        {
            int count = 0;
            if (items == null) return count;
            for (int i = 0; i < items.Count; i++) if (items[i].Kind == kind) count++;
            return count;
        }

        public static string KindNameJa(NaturalFeatureKind kind)
        {
            switch (kind)
            {
                case NaturalFeatureKind.Mountain: return "山・山脈";
                case NaturalFeatureKind.River: return "川・水系";
                case NaturalFeatureKind.Sea: return "海・海域";
                case NaturalFeatureKind.Lake: return "湖・内水域";
                case NaturalFeatureKind.Forest: return "森・森林景観";
                case NaturalFeatureKind.Desert: return "砂漠・乾燥地";
                default: return "自然地理";
            }
        }
    }
}
