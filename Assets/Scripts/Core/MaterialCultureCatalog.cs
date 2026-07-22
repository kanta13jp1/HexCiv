using System;
using System.Collections.Generic;

namespace HexCiv.Core
{
    /// <summary>生活文化・交通・技術史台帳の分類。値は後方互換のため末尾へ追加する。</summary>
    public enum MaterialCultureKind
    {
        SpecialtyProduct = 0,
        RegionalProduct = 1,
        LocalIcon = 2,
        Cuisine = 3,
        Ship = 4,
        Vehicle = 5,
        Aircraft = 6,
        Rocket = 7,
        Weapon = 8,
        Dance = 9,
        Song = 10,
        MartialArt = 11,
    }

    /// <summary>
    /// 生産物・料理・乗り物・武器・身体文化を横断する史実項目。
    /// 「名産品」「特産品」「名物」は学術上の普遍分類ではなく図鑑上の検索タグとして用いる。
    /// </summary>
    public sealed class MaterialCultureDef
    {
        public readonly string Id;
        public readonly string NameJa;
        public readonly MaterialCultureKind Kind;
        public readonly string RegionJa;
        public readonly string PeriodJa;
        public readonly string PlaceJa;
        public readonly string SummaryJa;

        public MaterialCultureDef(string id, string nameJa, MaterialCultureKind kind,
            string regionJa, string periodJa, string placeJa, string summaryJa)
        {
            Id = id;
            NameJa = nameJa;
            Kind = kind;
            RegionJa = regionJa;
            PeriodJa = periodJa;
            PlaceJa = placeJa;
            SummaryJa = summaryJa;
        }

        public string KindNameJa => MaterialCultureCatalog.KindNameJa(Kind);
    }

    /// <summary>
    /// 生活・技術史台帳第1弾。現存する伝統は過去の遺物ではなく、担い手が更新する実践として記述する。
    /// 歌詞・楽譜・意匠を複製せず、名称と短い歴史的説明だけを収録する。
    /// </summary>
    public static class MaterialCultureCatalog
    {
        static readonly MaterialCultureDef[] Definitions =
        {
            // ---- アフリカ ----
            Item("ethiopian_coffee", "エチオピアのコーヒー", MaterialCultureKind.SpecialtyProduct, "アフリカ", "長期栽培・近世以降広域流通", "エチオピア高地", "在来品種、栽培、焙煎、歓待の実践が地域社会と世界交易を結ぶ。"),
            Item("kente_textile", "ケンテ織", MaterialCultureKind.RegionalProduct, "アフリカ", "近世以降", "ガーナ周辺", "細い帯を織り合わせ、色と文様に社会的意味を込める織物文化。"),
            Item("argan_oil", "アルガン油", MaterialCultureKind.LocalIcon, "アフリカ", "長期継承", "モロッコ南西部", "乾燥地のアルガン林と結び付いた採取・加工・食文化の産物。"),
            Item("couscous", "クスクス", MaterialCultureKind.Cuisine, "アフリカ", "長期継承", "北アフリカ", "穀粒の加工、蒸し調理、共食を通して家族と地域を結ぶ多様な料理文化。"),
            Item("nile_felucca", "ナイルのフェルーカ", MaterialCultureKind.Ship, "アフリカ", "古代的系譜・現代まで", "ナイル川流域", "帆と河川風を用い、人と物を運んできた小型帆船の系譜。"),
            Item("cape_ox_wagon", "ケープの牛車", MaterialCultureKind.Vehicle, "アフリカ", "近世～近代", "南部アフリカ", "長距離移動と植民地拡大に用いられ、先住社会への影響も伴った車両。"),
            Item("rooivalk", "ローイファルク攻撃ヘリコプター", MaterialCultureKind.Aircraft, "アフリカ", "20世紀末以降", "南アフリカ", "南アフリカで開発された双発の軍用回転翼機。"),
            Item("al_kahir_rocket", "アル・カーヒル・ロケット", MaterialCultureKind.Rocket, "アフリカ", "20世紀", "エジプト", "エジプトが進めた初期の国産弾道ロケット計画を示す技術史資料。"),
            Item("iklwa_spear", "イクルワ", MaterialCultureKind.Weapon, "アフリカ", "19世紀を中心に", "南部アフリカ", "近接戦闘に用いられた短い柄の槍。軍制変化とともに語られる。"),
            Item("adumu_dance", "アドゥム", MaterialCultureKind.Dance, "アフリカ", "継承中", "ケニア・タンザニアのマーサイ共同体", "歌と跳躍を伴う年齢集団の儀礼的表現。観光表象だけに還元しない。"),
            Item("mbube_song", "『ムブーベ』", MaterialCultureKind.Song, "アフリカ", "1939年録音", "南アフリカ", "ソロモン・リンダと歌手たちの録音から世界へ広がった歌。権利史も重要である。"),
            Item("dambe", "ダンベ", MaterialCultureKind.MartialArt, "アフリカ", "継承中", "西アフリカ・ハウサ社会", "打撃を中心とする格闘文化で、祭りや職能集団の歴史と結び付く。"),

            // ---- 西・南アジア ----
            Item("persian_carpet", "ペルシア絨毯", MaterialCultureKind.SpecialtyProduct, "西・南アジア", "長期継承", "イラン各地", "地域・工房ごとの結び、素材、文様が交易と生活空間を形づくった。"),
            Item("kashmir_pashmina", "カシミールのパシュミナ", MaterialCultureKind.RegionalProduct, "西・南アジア", "中世以降", "カシミール", "細い獣毛の採取、紡績、織り、刺繍をつなぐ広域的な手仕事。"),
            Item("turkish_coffee", "トルコ・コーヒー", MaterialCultureKind.LocalIcon, "西・南アジア", "近世以降", "アナトリアと周辺", "細挽きの豆を煮出す調理法と、歓待・会話・儀礼の文化。"),
            Item("biryani", "ビリヤニ", MaterialCultureKind.Cuisine, "西・南アジア", "中世末～近世以降", "南アジア", "米、香辛料、肉や野菜を重ねる料理群で、各都市と共同体に多くの型がある。"),
            Item("dhow", "ダウ船", MaterialCultureKind.Ship, "西・南アジア", "古代的系譜・現代まで", "インド洋西部", "季節風航海と港市交易を支えた多様な木造帆船の総称。"),
            Item("hindustan_ambassador", "ヒンドゥスタン・アンバサダー", MaterialCultureKind.Vehicle, "西・南アジア", "20～21世紀", "インド", "長く生産され、行政・タクシー・日常交通の象徴となった乗用車。"),
            Item("hal_tejas", "HALテジャス", MaterialCultureKind.Aircraft, "西・南アジア", "21世紀", "インド", "国内の研究・生産基盤を結集して開発された軽戦闘機。"),
            Item("pslv", "PSLV", MaterialCultureKind.Rocket, "西・南アジア", "20世紀末以降", "インド", "極軌道衛星打上げを主用途に発展し、多数の宇宙ミッションを担ったロケット。"),
            Item("shamshir", "シャムシール", MaterialCultureKind.Weapon, "西・南アジア", "中世末以降", "イランと周辺", "強く湾曲した片刃を特徴とする刀剣の一系統。"),
            Item("kathak", "カタック", MaterialCultureKind.Dance, "西・南アジア", "中世的系譜・現代まで", "北インド", "語り、身振り、旋回、リズムを結ぶ舞踊で、宮廷と舞台で変化してきた。"),
            Item("qawwali_singing", "カッワーリー歌唱", MaterialCultureKind.Song, "西・南アジア", "中世以降", "南アジア", "スーフィー詩を反復的な歌唱と合奏で表す実践。特定の一作品に限定されない。"),
            Item("kalaripayattu", "カラリパヤット", MaterialCultureKind.MartialArt, "西・南アジア", "長期形成・現代まで", "南インド・ケーララ", "武器、徒手、身体鍛錬を含む複数流派の武術伝統。"),

            // ---- 東・東南アジア ----
            Item("jingdezhen_porcelain", "景徳鎮の磁器", MaterialCultureKind.SpecialtyProduct, "東・東南アジア", "中世以降", "中国・江西", "分業化した窯業都市から国内外へ広く流通した磁器。"),
            Item("indonesian_batik_product", "インドネシアのバティック", MaterialCultureKind.RegionalProduct, "東・東南アジア", "長期継承", "インドネシア各地", "蝋防染と重ね染めにより、地域と場面に応じた文様を表す布。"),
            Item("japanese_tea", "日本茶", MaterialCultureKind.LocalIcon, "東・東南アジア", "中世以降", "日本各地", "栽培、製茶、喫茶、贈答が地域産業と季節の文化を形づくる。"),
            Item("sushi", "すし", MaterialCultureKind.Cuisine, "東・東南アジア", "古い保存食の系譜・近世以降発展", "日本", "発酵保存から酢飯と具材の多様な型へ変化した料理群。"),
            Item("chinese_junk", "ジャンク船", MaterialCultureKind.Ship, "東・東南アジア", "古代末期以降", "中国沿海と東南アジア", "隔壁構造や帆装を発展させ、沿海・外洋交易に用いられた船の系譜。"),
            Item("jinrikisha", "人力車", MaterialCultureKind.Vehicle, "東・東南アジア", "19世紀以降", "日本発祥・アジア各地", "人が引く二輪車として都市交通に普及し、労働史とも結び付く。"),
            Item("mitsubishi_zero", "零式艦上戦闘機", MaterialCultureKind.Aircraft, "東・東南アジア", "20世紀", "日本", "第二次世界大戦期の艦上戦闘機。戦争被害と技術史を併記して扱う。"),
            Item("h2a", "H-IIAロケット", MaterialCultureKind.Rocket, "東・東南アジア", "21世紀", "日本", "人工衛星や月・惑星探査機の打上げを担った液体燃料ロケット。"),
            Item("katana", "日本刀", MaterialCultureKind.Weapon, "東・東南アジア", "中世以降", "日本", "刀匠の分業と鍛造技術、武家文化、美術評価が重なる刀剣。"),
            Item("bon_odori", "盆踊り", MaterialCultureKind.Dance, "東・東南アジア", "中世的系譜・現代まで", "日本各地", "祖先供養や地域の夏行事と結び付き、土地ごとの歌と振りで踊られる。"),
            Item("arirang", "アリラン", MaterialCultureKind.Song, "東・東南アジア", "長期継承", "朝鮮半島とディアスポラ", "多数の詞章と旋律変種を持ち、離別・希望・共同体の記憶を歌う。"),
            Item("judo", "柔道", MaterialCultureKind.MartialArt, "東・東南アジア", "19世紀末以降", "日本発祥・世界各地", "柔術諸流を再編した教育・競技・武道で、世界的に実践される。"),

            // ---- ヨーロッパ・地中海 ----
            Item("murano_glass", "ムラーノ・ガラス", MaterialCultureKind.SpecialtyProduct, "ヨーロッパ・地中海", "中世末以降", "イタリア・ヴェネツィア潟", "島の工房で技法と意匠を発展させたガラス工芸。"),
            Item("champagne", "シャンパーニュの発泡性ワイン", MaterialCultureKind.RegionalProduct, "ヨーロッパ・地中海", "近世以降", "フランス・シャンパーニュ", "産地、醸造法、流通、祝祭イメージが結び付いた地域産品。"),
            Item("swiss_watch", "スイス時計", MaterialCultureKind.LocalIcon, "ヨーロッパ・地中海", "近世以降", "スイス各地", "精密加工、分業、意匠、国際市場によって形成された時計産業。"),
            Item("neapolitan_pizza", "ナポリのピッツァ", MaterialCultureKind.Cuisine, "ヨーロッパ・地中海", "近代以降", "イタリア・ナポリ", "都市の職人技と日常食から広がった円形の焼成料理。"),
            Item("trireme", "三段櫂船", MaterialCultureKind.Ship, "ヨーロッパ・地中海", "古代", "東地中海", "多数の漕ぎ手を三段に配置し、海戦と都市国家の動員を支えた軍船。"),
            Item("benz_patent_motorwagen", "ベンツ・パテント・モトールヴァーゲン", MaterialCultureKind.Vehicle, "ヨーロッパ・地中海", "1886年", "ドイツ", "内燃機関を前提に設計された初期の実用的自動車として知られる。"),
            Item("supermarine_spitfire", "スーパーマリン・スピットファイア", MaterialCultureKind.Aircraft, "ヨーロッパ・地中海", "20世紀", "イギリス", "第二次世界大戦期を代表する戦闘機の一つ。記憶文化と工業史にも関わる。"),
            Item("ariane_5", "アリアン5", MaterialCultureKind.Rocket, "ヨーロッパ・地中海", "20世紀末～21世紀", "ヨーロッパ", "欧州の協力体制で開発され、商業衛星や科学探査機を打ち上げた。"),
            Item("english_longbow", "イングランド長弓", MaterialCultureKind.Weapon, "ヨーロッパ・地中海", "中世", "ブリテン島", "長い弓身と訓練された射手集団が戦場と社会制度に影響した。"),
            Item("flamenco_dance", "フラメンコ舞踊", MaterialCultureKind.Dance, "ヨーロッパ・地中海", "18～19世紀以降", "スペイン・アンダルシア", "歌、踊り、ギターが多様な共同体の交流から形成された舞台芸能。"),
            Item("ode_to_joy", "『歓喜の歌』", MaterialCultureKind.Song, "ヨーロッパ・地中海", "1824年初演", "ウィーン", "ベートーヴェンの交響曲第9番終楽章で歌われ、後世に多様な意味を付与された。"),
            Item("savate", "サバット", MaterialCultureKind.MartialArt, "ヨーロッパ・地中海", "19世紀以降", "フランス", "蹴りと拳を体系化した近代フランスの格闘・競技文化。"),

            // ---- アメリカ大陸 ----
            Item("mesoamerican_cacao", "メソアメリカのカカオ", MaterialCultureKind.SpecialtyProduct, "アメリカ大陸", "先コロンブス期以降", "メソアメリカ", "飲料、贈答、儀礼、交換に用いられ、後に世界商品となった作物。"),
            Item("maple_syrup", "メープルシロップ", MaterialCultureKind.RegionalProduct, "アメリカ大陸", "先住民の知識・植民地期以降発展", "北米北東部", "樹液採取と煮詰めの季節労働が地域の食文化と産業を形づくる。"),
            Item("andean_chullo", "アンデスのチュリョ", MaterialCultureKind.LocalIcon, "アメリカ大陸", "長期継承", "アンデス高地", "耳当てを備えた編み帽子で、地域ごとの素材、色、文様を持つ。"),
            Item("ceviche", "セビチェ", MaterialCultureKind.Cuisine, "アメリカ大陸", "長期形成・現代まで", "太平洋岸ラテンアメリカ", "魚介を酸味、香味野菜などで調える料理群で、各地域に異なる型がある。"),
            Item("birchbark_canoe", "樺皮カヌー", MaterialCultureKind.Ship, "アメリカ大陸", "長期継承", "北米北部", "軽量な樺皮船体が河川・湖沼の移動、交易、知識伝承を支えた。"),
            Item("ford_model_t", "フォード・モデルT", MaterialCultureKind.Vehicle, "アメリカ大陸", "1908～1927年", "アメリカ合衆国", "大量生産方式と自動車の大衆化を象徴し、労働と都市景観を変えた。"),
            Item("wright_flyer", "ライト・フライヤー", MaterialCultureKind.Aircraft, "アメリカ大陸", "1903年", "アメリカ合衆国", "動力・操縦・揚力を統合した飛行実験で航空史の転機となった。"),
            Item("saturn_v", "サターンV", MaterialCultureKind.Rocket, "アメリカ大陸", "1960～70年代", "アメリカ合衆国", "アポロ計画で人を月へ送るために用いられた大型打上げロケット。"),
            Item("macuahuitl", "マクアウィトル", MaterialCultureKind.Weapon, "アメリカ大陸", "先コロンブス期", "メソアメリカ", "木製の本体に黒曜石の刃を装着した武器。"),
            Item("tango_dance", "タンゴ", MaterialCultureKind.Dance, "アメリカ大陸", "19世紀末以降", "リオ・デ・ラ・プラタ地域", "都市の移民社会で音楽・舞踊・詩が交差し、世界へ広がった。"),
            Item("el_condor_pasa", "『コンドルは飛んでいく』", MaterialCultureKind.Song, "アメリカ大陸", "1913年初演", "ペルー", "ダニエル・アロミア・ロブレスのサルスエラから広がった旋律。"),
            Item("capoeira", "カポエイラ", MaterialCultureKind.MartialArt, "アメリカ大陸", "植民地期以降", "ブラジル", "闘い、舞踊、競技、音楽、共同体の記憶を同時に担うアフロ・ブラジル文化。"),

            // ---- オセアニア ----
            Item("tahitian_black_pearl", "タヒチの黒蝶真珠", MaterialCultureKind.SpecialtyProduct, "オセアニア", "20世紀以降養殖発展", "フランス領ポリネシア", "黒蝶貝の養殖技術と島嶼の海洋環境を結ぶ産品。"),
            Item("tapa_barkcloth", "タパ樹皮布", MaterialCultureKind.RegionalProduct, "オセアニア", "長期継承", "太平洋諸島", "樹皮を打ち延ばし、地域ごとの文様を施す布状素材と制作文化。"),
            Item("boomerang", "ブーメラン", MaterialCultureKind.LocalIcon, "オセアニア", "長期継承", "オーストラリア先住民社会", "狩猟・儀礼・音響など用途の異なる投擲具群で、すべてが戻る型ではない。"),
            Item("hangi", "ハーンギ", MaterialCultureKind.Cuisine, "オセアニア", "長期継承", "アオテアロア／ニュージーランド", "地中の加熱石で食材を蒸し焼きにし、集まりと歓待を支える調理法。"),
            Item("polynesian_vaka", "ポリネシアのヴァカ", MaterialCultureKind.Ship, "オセアニア", "長期継承・復興中", "ポリネシア", "船体、星、波、風、生物の知識を組み合わせた航海カヌーの系譜。"),
            Item("holden_ute", "ホールデン・ユート", MaterialCultureKind.Vehicle, "オセアニア", "20～21世紀", "オーストラリア", "乗用車と荷台を結ぶ車型が農村・労働・大衆文化の象徴となった。"),
            Item("cac_boomerang", "CACブーメラン戦闘機", MaterialCultureKind.Aircraft, "オセアニア", "20世紀", "オーストラリア", "戦時下に短期間で国内開発・生産された戦闘機。"),
            Item("electron_rocket", "エレクトロン・ロケット", MaterialCultureKind.Rocket, "オセアニア", "21世紀", "ニュージーランド／アメリカ合衆国", "小型衛星打上げ市場向けに開発され、ニュージーランドからも発射される。"),
            Item("taiaha", "タイアハ", MaterialCultureKind.Weapon, "オセアニア", "長期継承", "マオリ文化", "木製の長柄武器で、戦闘技術、演説、系譜、儀礼と結び付く。"),
            Item("haka", "ハカ", MaterialCultureKind.Dance, "オセアニア", "長期継承・現代まで", "マオリ共同体", "歌詞、姿勢、動作で歓迎・追悼・抗議・結束など多様な目的を表す。"),
            Item("pokarekare_ana", "『ポーカレカレ・アナ』", MaterialCultureKind.Song, "オセアニア", "20世紀初頭以降", "アオテアロア／ニュージーランド", "マオリ語で歌い継がれる愛の歌。成立と伝承には複数の記録がある。"),
            Item("mau_rakau", "マウ・ラーカウ", MaterialCultureKind.MartialArt, "オセアニア", "長期継承・復興中", "マオリ文化", "タイアハなどの武器技法、身体訓練、口承知識を結ぶ武術伝統。"),
        };

        public static IReadOnlyList<MaterialCultureDef> All => Definitions;

        public static MaterialCultureDef Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < Definitions.Length; i++)
                if (string.Equals(Definitions[i].Id, id, StringComparison.OrdinalIgnoreCase))
                    return Definitions[i];
            return null;
        }

        public static List<MaterialCultureDef> ForRegion(string regionJa)
        {
            var result = new List<MaterialCultureDef>();
            for (int i = 0; i < Definitions.Length; i++)
                if (string.IsNullOrEmpty(regionJa) ||
                    string.Equals(Definitions[i].RegionJa, regionJa, StringComparison.Ordinal))
                    result.Add(Definitions[i]);
            return result;
        }

        public static int CountKind(IEnumerable<MaterialCultureDef> items, MaterialCultureKind kind)
        {
            int count = 0;
            if (items == null) return count;
            foreach (MaterialCultureDef item in items) if (item != null && item.Kind == kind) count++;
            return count;
        }

        public static string KindNameJa(MaterialCultureKind kind)
        {
            return kind switch
            {
                MaterialCultureKind.SpecialtyProduct => "名産品",
                MaterialCultureKind.RegionalProduct => "特産品",
                MaterialCultureKind.LocalIcon => "名物",
                MaterialCultureKind.Cuisine => "料理",
                MaterialCultureKind.Ship => "船",
                MaterialCultureKind.Vehicle => "車",
                MaterialCultureKind.Aircraft => "飛行機",
                MaterialCultureKind.Rocket => "ロケット",
                MaterialCultureKind.Weapon => "武器",
                MaterialCultureKind.Dance => "踊り",
                MaterialCultureKind.Song => "歌",
                MaterialCultureKind.MartialArt => "武道・武術",
                _ => "生活・技術",
            };
        }

        static MaterialCultureDef Item(string id, string nameJa, MaterialCultureKind kind,
            string regionJa, string periodJa, string placeJa, string summaryJa)
        {
            return new MaterialCultureDef(id, nameJa, kind, regionJa, periodJa, placeJa, summaryJa);
        }
    }
}
