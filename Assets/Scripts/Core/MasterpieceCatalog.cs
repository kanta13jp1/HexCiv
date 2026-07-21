using System;
using System.Collections.Generic;

namespace HexCiv.Core
{
    /// <summary>作品史の媒体分類。口承・共同制作も各媒体の歴史として扱う。</summary>
    public enum MasterpieceKind
    {
        Book,
        Painting,
        Sculpture,
        Architecture,
        Music,
        Theater,
        Film,
    }

    /// <summary>書籍・美術・建築・音楽を横断する作品史レコード。</summary>
    public sealed class MasterpieceDef
    {
        public readonly string Id;
        public readonly string NameJa;
        public readonly MasterpieceKind Kind;
        public readonly string RegionJa;
        public readonly string PeriodJa;
        public readonly string CreatorJa;
        public readonly string SummaryJa;
        public readonly string RelatedCivilizationId;
        public readonly string RelatedGreatPersonId;

        public MasterpieceDef(string id, string nameJa, MasterpieceKind kind, string regionJa,
            string periodJa, string creatorJa, string summaryJa, string civilizationId,
            string greatPersonId)
        {
            Id = id;
            NameJa = nameJa;
            Kind = kind;
            RegionJa = regionJa;
            PeriodJa = periodJa;
            CreatorJa = creatorJa;
            SummaryJa = summaryJa;
            RelatedCivilizationId = civilizationId ?? "";
            RelatedGreatPersonId = greatPersonId ?? "";
        }

        public string KindNameJa
        {
            get
            {
                switch (Kind)
                {
                    case MasterpieceKind.Book: return "書籍";
                    case MasterpieceKind.Painting: return "絵画";
                    case MasterpieceKind.Sculpture: return "彫刻";
                    case MasterpieceKind.Architecture: return "建築";
                    case MasterpieceKind.Music: return "音楽";
                    case MasterpieceKind.Theater: return "演劇";
                    case MasterpieceKind.Film: return "映画";
                    default: return "不明";
                }
            }
        }
    }

    /// <summary>
    /// 作品史台帳。価値の順位付けではなく、7媒体・6地域を同数で扱う調査基盤。
    /// 口承作品、共同制作、建築群、音楽伝統は単独作者を無理に設定しない。
    /// </summary>
    public static class MasterpieceCatalog
    {
        static readonly MasterpieceDef[] Definitions =
        {
            // ==================================================================
            // 書籍・文書・口承叙事詩（6地域×5件＝30件）
            // ==================================================================
            W("book_of_dead", "死者の書", MasterpieceKind.Book, "アフリカ", "紀元前16世紀以降", "古代エジプトの書記たち", "死後世界の旅を助ける呪文と図像を集めた葬祭文書群。", "egypt"),
            W("tale_of_sinuhe", "シヌヘの物語", MasterpieceKind.Book, "アフリカ", "紀元前20世紀ごろ", "作者不詳", "宮廷を離れた人物の逃亡と帰還を語る古代エジプト文学。", "egypt"),
            W("epic_of_sundiata", "スンジャタ叙事詩", MasterpieceKind.Book, "アフリカ", "13世紀以降の口承", "マンデの語り部たち", "マリ帝国の始祖スンジャタを語り継ぐ口承叙事詩。", "mali"),
            W("muqaddimah", "歴史序説", MasterpieceKind.Book, "アフリカ", "14世紀", "イブン・ハルドゥーン", "社会・国家・歴史変動を体系的に論じた歴史書の序論。", "", "ibn_khaldun"),
            W("things_fall_apart", "崩れゆく絆", MasterpieceKind.Book, "アフリカ", "20世紀", "チヌア・アチェベ", "植民地化に直面するイボ社会を内側から描いた小説。", ""),

            W("epic_of_gilgamesh", "ギルガメシュ叙事詩", MasterpieceKind.Book, "西・南アジア", "紀元前2千年紀", "メソポタミアの書記たち", "王ギルガメシュの友情と死への問いを刻んだ楔形文字文学。", "sumer"),
            W("avesta", "アヴェスター", MasterpieceKind.Book, "西・南アジア", "古代～中世に編纂", "ゾロアスター教共同体", "讃歌・祭儀・教説を伝えるゾロアスター教の聖典群。", "persia", "zoroaster"),
            W("mahabharata", "マハーバーラタ", MasterpieceKind.Book, "西・南アジア", "紀元前後に成立", "インドの詩人・伝承者たち", "王族間の戦争を軸に倫理・社会・宇宙観を語る大叙事詩。", "gupta"),
            W("ramayana", "ラーマーヤナ", MasterpieceKind.Book, "西・南アジア", "紀元前後に成立", "ヴァールミーキ伝承", "ラーマの追放、シーター救出、帰還を描く叙事詩。", "gupta"),
            W("shahnameh", "シャー・ナーメ", MasterpieceKind.Book, "西・南アジア", "11世紀", "フェルドウスィー", "イラン世界の神話時代から王朝史を詠う長大な叙事詩。", "persia"),

            W("analects", "論語", MasterpieceKind.Book, "東・東南アジア", "紀元前5～3世紀ごろ", "孔子の門人たち", "孔子と弟子の言行を編んだ思想書。", "han", "confucius"),
            W("records_grand_historian", "史記", MasterpieceKind.Book, "東・東南アジア", "紀元前1世紀", "司馬遷", "伝説時代から漢代までを紀伝体で記した歴史書。", "han", "sima_qian"),
            W("tale_of_genji", "源氏物語", MasterpieceKind.Book, "東・東南アジア", "11世紀初頭", "紫式部", "宮廷社会の人間関係と時間の移ろいを描く長編物語。", "japan", "murasaki_shikibu"),
            W("journey_to_west", "西遊記", MasterpieceKind.Book, "東・東南アジア", "16世紀", "呉承恩に帰される", "玄奘の旅をもとに孫悟空らの冒険を描く神魔小説。", "ming"),
            W("nagarakretagama", "ナーガラクルターガマ", MasterpieceKind.Book, "東・東南アジア", "14世紀", "プラパンチャ", "マジャパヒト宮廷・領域・祭儀を詠んだ古ジャワ語の頌詩。", "majapahit"),

            W("iliad", "イーリアス", MasterpieceKind.Book, "ヨーロッパ・地中海", "紀元前8世紀ごろ", "ホメロス伝承", "トロイア戦争末期のアキレウスの怒りを詠う叙事詩。", "athens"),
            W("republic_plato", "国家", MasterpieceKind.Book, "ヨーロッパ・地中海", "紀元前4世紀", "プラトン", "正義・教育・統治・知識を対話形式で論じる哲学書。", "athens", "plato"),
            W("divine_comedy", "神曲", MasterpieceKind.Book, "ヨーロッパ・地中海", "14世紀", "ダンテ・アリギエーリ", "地獄・煉獄・天国を巡る旅を描く長編叙事詩。", ""),
            W("don_quixote", "ドン・キホーテ", MasterpieceKind.Book, "ヨーロッパ・地中海", "17世紀", "ミゲル・デ・セルバンテス", "騎士道物語に魅せられた郷士の旅を描く小説。", "spain"),
            W("hamlet", "ハムレット", MasterpieceKind.Book, "ヨーロッパ・地中海", "17世紀初頭", "ウィリアム・シェイクスピア", "復讐、疑念、行為の葛藤を描く悲劇。", "england", "william_shakespeare"),

            W("popol_vuh", "ポポル・ヴフ", MasterpieceKind.Book, "アメリカ大陸", "植民地期に筆記", "キチェ・マヤの伝承者たち", "創世神話、英雄双生児、王統を伝えるキチェ語文書。", "maya"),
            W("florentine_codex", "フィレンツェ絵文書", MasterpieceKind.Book, "アメリカ大陸", "16世紀", "ナワの知識人とベルナルディーノ・デ・サアグン", "ナワ社会の言語・自然・儀礼・歴史を記録した百科全書的写本。", "aztec"),
            W("royal_commentaries_incas", "インカ皇統記", MasterpieceKind.Book, "アメリカ大陸", "17世紀", "インカ・ガルシラーソ・デ・ラ・ベガ", "アンデスとスペイン双方の視点からインカ史を叙述した。", "inca", "inca_garcilaso"),
            W("moby_dick", "白鯨", MasterpieceKind.Book, "アメリカ大陸", "19世紀", "ハーマン・メルヴィル", "捕鯨船の航海を通して執念・自然・知識を描く小説。", ""),
            W("hundred_years_solitude", "百年の孤独", MasterpieceKind.Book, "アメリカ大陸", "20世紀", "ガブリエル・ガルシア＝マルケス", "一族と町の盛衰を神話的な時間感覚で描く小説。", ""),

            W("kumulipo", "クムリポ", MasterpieceKind.Book, "オセアニア", "18世紀以前の口承", "ハワイの詠唱者たち", "宇宙創生から王統へ連なるハワイの長大な系譜詠唱。", "hawaii"),
            W("maori_legends_rangikaheke", "テ・ランギカーへケのマオリ伝承写本", MasterpieceKind.Book, "オセアニア", "19世紀", "ワイレム・マイヒ・テ・ランギカーへケ", "マオリの系譜・神話・慣習をマオリ語で記録した写本群。", "maori"),
            W("sentimental_bloke", "感傷的な男", MasterpieceKind.Book, "オセアニア", "20世紀初頭", "C・J・デニス", "オーストラリア都市口語を用いて恋と生活を描く物語詩。", ""),
            W("we_are_going", "私たちは行く", MasterpieceKind.Book, "オセアニア", "20世紀", "ウジェルー・ヌーナッカル", "先住民の経験・土地・権利を英語詩で表現した詩集。", "", "oodgeroo_noonuccal"),
            W("bone_people", "ボーン・ピープル", MasterpieceKind.Book, "オセアニア", "20世紀", "ケリ・ヒューム", "マオリとパーケハーの関係、家族、暴力と回復を描く小説。", "maori"),

            // ==================================================================
            // 絵画・壁画・絵巻・版画（30件）
            // ==================================================================
            W("nebamun_paintings", "ネバムン墓の壁画", MasterpieceKind.Painting, "アフリカ", "紀元前14世紀ごろ", "古代エジプトの画工たち", "狩猟・宴・庭園などを鮮やかに描いた墓室壁画。", "egypt"),
            W("san_rock_art", "サンの岩絵", MasterpieceKind.Painting, "アフリカ", "先史時代～近世", "サン諸共同体", "狩猟、動物、儀礼経験を岩面に描いた南部アフリカの絵画伝統。", ""),
            W("ethiopian_church_murals", "エチオピア正教会の聖堂壁画", MasterpieceKind.Painting, "アフリカ", "中世以降", "エチオピアの聖堂画家たち", "聖書物語と聖人を明快な色面で描く宗教絵画伝統。", "aksum"),
            W("song_of_pick", "つるはしの歌", MasterpieceKind.Painting, "アフリカ", "20世紀", "ジェラード・セコト", "南アフリカの労働と人間の尊厳を描いた作品。", "zulu"),
            W("arab_priest", "アラブの司祭", MasterpieceKind.Painting, "アフリカ", "20世紀", "イルマ・スターン", "ザンジバルで出会った人物を力強い色彩で表した肖像画。", ""),

            W("court_of_gayumars", "ガユーマルスの宮廷", MasterpieceKind.Painting, "西・南アジア", "16世紀", "スルタン・ムハンマド", "『シャー・ナーメ』写本のために描かれた精緻な宮廷細密画。", "persia"),
            W("seduction_of_yusuf", "ユースフの誘惑", MasterpieceKind.Painting, "西・南アジア", "15世紀", "カマールッディーン・ビフザード", "建築空間と物語の時間を一画面へ組み込んだペルシア細密画。", "persia"),
            W("hamzanama_folio", "ハムザ物語絵巻", MasterpieceKind.Painting, "西・南アジア", "16世紀", "ムガル宮廷工房", "英雄ハムザの冒険を大型画面に描いた物語画群。", "mughal"),
            W("bani_thani", "バニ・タニ", MasterpieceKind.Painting, "西・南アジア", "18世紀", "ニハール・チャンド", "キシャンガル派の理想化された横顔で知られる肖像表現。", ""),
            W("shah_jahan_globe", "地球儀に立つシャー・ジャハーン", MasterpieceKind.Painting, "西・南アジア", "17世紀", "ムガル宮廷画家", "皇帝の普遍的統治を寓意的に示す細密画。", "mughal"),

            W("admonitions_scroll", "女史箴図", MasterpieceKind.Painting, "東・東南アジア", "5～8世紀の伝世模本", "顧愷之に帰される原画", "宮廷女性への箴言を連続場面で表した絵巻。", ""),
            W("night_revels_han_xizai", "韓熙載夜宴図", MasterpieceKind.Painting, "東・東南アジア", "10世紀原画の伝世模本", "顧閎中に帰される", "宴の音楽・舞踊・人物心理を長巻に描く。", "tang"),
            W("qingming_scroll", "清明上河図", MasterpieceKind.Painting, "東・東南アジア", "12世紀", "張択端", "都市と郊外の人々・交通・商業を細密に描いた長巻。", ""),
            W("wind_thunder_gods", "風神雷神図屏風", MasterpieceKind.Painting, "東・東南アジア", "17世紀", "俵屋宗達", "金地に風神と雷神を大胆に配した屏風絵。", "japan"),
            W("great_wave", "神奈川沖浪裏", MasterpieceKind.Painting, "東・東南アジア", "19世紀", "葛飾北斎", "巨大な波と富士を対置した多色木版画。", "japan", "hokusai"),

            W("lascaux_paintings", "ラスコー洞窟壁画", MasterpieceKind.Painting, "ヨーロッパ・地中海", "後期旧石器時代", "先史時代の画工たち", "大型動物群を洞窟内に描いた先史絵画。", ""),
            W("mona_lisa", "モナ・リザ", MasterpieceKind.Painting, "ヨーロッパ・地中海", "16世紀初頭", "レオナルド・ダ・ヴィンチ", "繊細な陰影と遠景で人物の存在感を表す肖像画。", "", "leonardo_da_vinci"),
            W("sistine_ceiling", "システィーナ礼拝堂天井画", MasterpieceKind.Painting, "ヨーロッパ・地中海", "16世紀初頭", "ミケランジェロ", "創世記の物語と人物像を壮大な構成で描く天井画。", "rome"),
            W("las_meninas", "ラス・メニーナス", MasterpieceKind.Painting, "ヨーロッパ・地中海", "17世紀", "ディエゴ・ベラスケス", "宮廷肖像、視線、鏡、制作行為を一画面で交差させる。", "spain"),
            W("starry_night", "星月夜", MasterpieceKind.Painting, "ヨーロッパ・地中海", "19世紀", "フィンセント・ファン・ゴッホ", "渦巻く夜空と村を強い筆触で構成した風景画。", ""),

            W("bonampak_murals", "ボナンパク壁画", MasterpieceKind.Painting, "アメリカ大陸", "8世紀", "古典期マヤの画工たち", "宮廷儀礼・戦争・音楽を鮮色で描いた壁画群。", "maya"),
            W("codex_borgia_paintings", "ボルジア絵文書", MasterpieceKind.Painting, "アメリカ大陸", "15～16世紀ごろ", "中央メキシコの画家・書記たち", "暦・神々・祭儀を絵文字で記した折本。", "aztec"),
            W("virgin_guadalupe", "グアダルーペの聖母像", MasterpieceKind.Painting, "アメリカ大陸", "16世紀に由来", "作者不詳", "メキシコの信仰と文化的帰属の中心となった聖母像。", ""),
            W("two_fridas", "二人のフリーダ", MasterpieceKind.Painting, "アメリカ大陸", "20世紀", "フリーダ・カーロ", "二つの自己像を血管と手で結び、複層的な自己認識を描く。", "", "frida_kahlo"),
            W("abaporu", "アバポル", MasterpieceKind.Painting, "アメリカ大陸", "20世紀", "タルシラ・ド・アマラル", "誇張された身体と熱帯景観でブラジル近代美術を象徴する。", ""),

            W("kakadu_rock_art", "カカドゥの岩絵", MasterpieceKind.Painting, "オセアニア", "数千年以上の継続", "アボリジナル諸共同体", "動物、人間、精霊、生活史を重ねて描く岩絵群。", ""),
            W("yirrkala_church_panels", "イルカラ教会パネル", MasterpieceKind.Painting, "オセアニア", "20世紀", "ヨルングの画家たち", "氏族の祖先譚と土地の関係を樹皮画パネルに表した共同制作。", ""),
            W("central_australian_landscape", "中央オーストラリアの風景", MasterpieceKind.Painting, "オセアニア", "20世紀", "アルバート・ナマジラ", "中央オーストラリアの山地と光を水彩で表現した風景画群。", "", "albert_namatjira"),
            W("victory_over_death_2", "死に対する勝利2", MasterpieceKind.Painting, "オセアニア", "20世紀", "コリン・マッカホン", "大画面の文字と抽象的空間で信仰・死・存在を問う。", ""),
            W("black_phoenix", "ブラック・フェニックス", MasterpieceKind.Painting, "オセアニア", "20世紀", "ラルフ・ホテレ", "焼けた船材と文字を用い、移動・植民地史・再生を扱う複合素材作品。", "maori", "ralph_hotere"),

            // ==================================================================
            // 彫刻・浮彫・記念碑（30件）
            // ==================================================================
            W("great_sphinx", "ギザの大スフィンクス", MasterpieceKind.Sculpture, "アフリカ", "紀元前26世紀ごろ", "古代エジプトの石工たち", "獅子の身体と王の頭部を岩盤から彫り出した巨像。", "egypt"),
            W("bust_nefertiti", "ネフェルティティ胸像", MasterpieceKind.Sculpture, "アフリカ", "紀元前14世紀", "トトメスの工房", "王妃の顔貌と色彩を精緻に表した彩色胸像。", "egypt"),
            W("ife_bronze_head", "イフェの真鍮製頭像", MasterpieceKind.Sculpture, "アフリカ", "12～15世紀", "イフェの鋳造工たち", "自然主義的な顔貌と細い線刻を持つ王権彫刻。", ""),
            W("benin_bronze_plaque", "ベニン王宮の真鍮浮彫", MasterpieceKind.Sculpture, "アフリカ", "16～17世紀", "ベニン王国の鋳造工組合", "宮廷人物・儀礼・対外関係を浮彫で記録した王宮装飾。", ""),
            W("nok_terracotta", "ノク文化のテラコッタ像", MasterpieceKind.Sculpture, "アフリカ", "紀元前1千年紀～紀元後初期", "ノク文化の造形者たち", "幾何学化された顔と装身具を持つ焼成土像。", ""),

            W("lamassu", "人頭有翼牡牛像ラマッス", MasterpieceKind.Sculpture, "西・南アジア", "紀元前9～7世紀", "新アッシリアの彫刻家たち", "宮殿門を守る人頭・翼・牡牛の複合守護像。", "assyria"),
            W("ashoka_lion_capital", "アショーカ王柱の獅子柱頭", MasterpieceKind.Sculpture, "西・南アジア", "紀元前3世紀", "マウリヤ朝の石工たち", "四頭の獅子を背中合わせに配した磨研砂岩の柱頭。", "maurya"),
            W("gandhara_buddha", "ガンダーラの仏立像", MasterpieceKind.Sculpture, "西・南アジア", "2～3世紀", "ガンダーラの彫刻工房", "衣文と人体表現を組み合わせた初期仏像の代表形式。", ""),
            W("dancing_girl_mohenjo", "モヘンジョダロの踊る少女", MasterpieceKind.Sculpture, "西・南アジア", "紀元前3千年紀", "インダス文明の鋳造工", "片手を腰に置く若い人物を表した小型青銅像。", "indus"),
            W("persepolis_reliefs", "ペルセポリス朝貢使節浮彫", MasterpieceKind.Sculpture, "西・南アジア", "紀元前6～5世紀", "アケメネス朝の彫刻家たち", "帝国各地の使節を秩序ある行列として刻んだ浮彫。", "persia"),

            W("terracotta_army", "兵馬俑", MasterpieceKind.Sculpture, "東・東南アジア", "紀元前3世紀", "秦の造形工房", "等身大の兵士・馬・車を大量に制作した地下軍団。", "han"),
            W("longmen_vairocana", "龍門石窟の盧舎那仏", MasterpieceKind.Sculpture, "東・東南アジア", "7世紀", "唐代の石工たち", "奉先寺の中心に坐す巨大な石造仏。", "tang"),
            W("kamakura_buddha", "鎌倉大仏", MasterpieceKind.Sculpture, "東・東南アジア", "13世紀", "大仏鋳造工たち", "屋外に坐す大型の阿弥陀如来銅像。", "japan"),
            W("bayon_faces", "バイヨンの四面塔", MasterpieceKind.Sculpture, "東・東南アジア", "12～13世紀", "クメール王朝の石工たち", "塔の四方に穏やかな巨大顔を刻んだ寺院彫刻。", "khmer"),
            W("borobudur_reliefs", "ボロブドゥール浮彫群", MasterpieceKind.Sculpture, "東・東南アジア", "8～9世紀", "ジャワの石工たち", "仏伝・説話・修行世界を長大な回廊に刻んだ浮彫。", "srivijaya"),

            W("venus_willendorf", "ヴィレンドルフのヴィーナス", MasterpieceKind.Sculpture, "ヨーロッパ・地中海", "後期旧石器時代", "先史時代の彫刻者", "石灰岩に人体を強調して彫った小像。", ""),
            W("discobolus", "円盤投げ", MasterpieceKind.Sculpture, "ヨーロッパ・地中海", "紀元前5世紀原作", "ミュロン", "運動の一瞬を均衡ある構成で示した古代ギリシア彫刻。", "athens"),
            W("laocoon_group", "ラオコーン群像", MasterpieceKind.Sculpture, "ヨーロッパ・地中海", "紀元前後", "ロドスの彫刻家たち", "蛇に襲われる祭司と息子たちの苦闘を表す群像。", "rome"),
            W("michelangelo_david", "ダヴィデ", MasterpieceKind.Sculpture, "ヨーロッパ・地中海", "16世紀初頭", "ミケランジェロ", "戦いを前にした緊張を巨大な大理石裸体像に表す。", ""),
            W("the_thinker", "考える人", MasterpieceKind.Sculpture, "ヨーロッパ・地中海", "19～20世紀", "オーギュスト・ロダン", "思索する身体を強い量感で表したブロンズ像。", "france"),

            W("olmec_colossal_head", "オルメカ巨大石頭像", MasterpieceKind.Sculpture, "アメリカ大陸", "紀元前2～1千年紀", "オルメカの石工たち", "玄武岩を運び、統治者とみられる頭部を巨大に彫刻した。", "olmec"),
            W("coatlicue", "コアトリクエ像", MasterpieceKind.Sculpture, "アメリカ大陸", "15世紀ごろ", "メシカの彫刻家たち", "蛇・爪・頭蓋骨を組み合わせた大地神の巨像。", "aztec"),
            W("aztec_sun_stone", "アステカの太陽の石", MasterpieceKind.Sculpture, "アメリカ大陸", "15世紀末", "メシカの石工たち", "太陽神、暦、宇宙時代を同心円状に刻む石彫円盤。", "aztec"),
            W("copan_stela_a", "コパン石碑A", MasterpieceKind.Sculpture, "アメリカ大陸", "8世紀", "古典期マヤの彫刻家たち", "王の姿と暦・政治情報を高浮彫と文字で刻む石碑。", "maya"),
            W("pakal_sarcophagus", "パカル王石棺蓋", MasterpieceKind.Sculpture, "アメリカ大陸", "7世紀", "パレンケの彫刻家たち", "王の死と再生を宇宙樹・祖先の図像で表した石彫。", "maya"),

            W("moai", "ラパ・ヌイのモアイ", MasterpieceKind.Sculpture, "オセアニア", "13～17世紀ごろ", "ラパ・ヌイの石工たち", "祖先を表すと考えられる凝灰岩の巨大石像群。", ""),
            W("malagan_carving", "マラガン彫刻", MasterpieceKind.Sculpture, "オセアニア", "19世紀以前から継続", "ニューアイルランドの彫刻者たち", "葬送儀礼のために人物・鳥・魚を透彫した木彫。", ""),
            W("maori_tekoteko", "マオリのテコテコ像", MasterpieceKind.Sculpture, "オセアニア", "近世以降", "マオリの彫刻家たち", "集会所の切妻に置かれ、祖先を表す木彫像。", "maori"),
            W("pukumani_poles", "プクマニの墓標", MasterpieceKind.Sculpture, "オセアニア", "近代以前から継続", "ティウィの造形者たち", "葬送儀礼のために彩色・彫刻される柱状造形。", ""),
            W("asmat_bisj_poles", "アスマットのビス柱", MasterpieceKind.Sculpture, "オセアニア", "近代以前から継続", "アスマットの彫刻者たち", "祖先を記憶し共同体の関係を示す大型木彫柱。", ""),

            // ==================================================================
            // 建築・建築群（30件）
            // ==================================================================
            W("giza_pyramid_complex", "ギザのピラミッド複合体", MasterpieceKind.Architecture, "アフリカ", "紀元前26～25世紀", "古王国の建築家・労働者たち", "王墓、葬祭殿、参道、付属墓からなる巨大建築群。", "egypt"),
            W("karnak_complex", "カルナック神殿群", MasterpieceKind.Architecture, "アフリカ", "紀元前2千年紀以降", "歴代ファラオと建築家たち", "列柱室・塔門・聖域を長期間増築した宗教建築群。", "egypt"),
            W("great_mosque_djenne", "ジェンネの大モスク", MasterpieceKind.Architecture, "アフリカ", "現在の建物は20世紀初頭", "ジェンネの泥工共同体", "日干し煉瓦と塗土を共同補修し続けるスーダン・サヘル様式の大建築。", "mali"),
            W("great_zimbabwe_architecture", "グレート・ジンバブエ", MasterpieceKind.Architecture, "アフリカ", "11～15世紀", "ショナ系社会の石工たち", "モルタルを用いず石を積んだ囲壁・塔・丘上建築群。", "great_zimbabwe"),
            W("lalibela_churches", "ラリベラの岩窟教会群", MasterpieceKind.Architecture, "アフリカ", "12～13世紀", "ザグウェ朝期の石工たち", "岩盤を掘り下げ、建物全体を彫り残した教会群。", "aksum"),

            W("persepolis_palaces", "ペルセポリス宮殿群", MasterpieceKind.Architecture, "西・南アジア", "紀元前6～4世紀", "アケメネス朝の多地域工房", "大階段・列柱殿・王宮を組み合わせた帝国儀礼の中心。", "persia"),
            W("petra_treasury", "ペトラのエル・ハズネ", MasterpieceKind.Architecture, "西・南アジア", "1世紀ごろ", "ナバテアの石工たち", "砂岩の崖面に多層の柱廊正面を彫り出した墓廟建築。", ""),
            W("sanchi_great_stupa", "サーンチー第1ストゥーパ", MasterpieceKind.Architecture, "西・南アジア", "紀元前3世紀以降", "マウリヤ朝以降の工人たち", "半球形の覆鉢と物語浮彫の門を持つ仏教記念建築。", "maurya"),
            W("taj_mahal", "タージ・マハル", MasterpieceKind.Architecture, "西・南アジア", "17世紀", "ウスタード・アフマド・ラホーリーら", "白大理石の墓廟・庭園・水路を軸線上に構成した建築群。", "mughal"),
            W("suleymaniye_mosque", "スレイマニエ・モスク", MasterpieceKind.Architecture, "西・南アジア", "16世紀", "ミマール・スィナン", "大ドームを中心に礼拝・教育・福祉施設を統合した複合建築。", "ottoman", "mimar_sinan"),

            W("forbidden_city", "北京故宮", MasterpieceKind.Architecture, "東・東南アジア", "15世紀以降", "明・清の建築工房", "中軸線上に殿堂・門・庭を重ねた大規模宮殿都市。", "ming"),
            W("horyuji", "法隆寺西院伽藍", MasterpieceKind.Architecture, "東・東南アジア", "7～8世紀", "飛鳥・奈良期の工人たち", "金堂・五重塔・回廊からなる初期木造仏教建築群。", "japan"),
            W("angkor_wat", "アンコール・ワット", MasterpieceKind.Architecture, "東・東南アジア", "12世紀", "クメール王朝の建築家・石工たち", "環濠・回廊・中央祠堂を宇宙山の象徴として構成した寺院。", "khmer"),
            W("borobudur_temple", "ボロブドゥール", MasterpieceKind.Architecture, "東・東南アジア", "8～9世紀", "シャイレーンドラ朝期の工人たち", "方形壇から円壇へ上昇しながら仏教宇宙観を体験する石造寺院。", "srivijaya"),
            W("gyeongbokgung", "景福宮", MasterpieceKind.Architecture, "東・東南アジア", "14世紀創建", "朝鮮王朝の建築工房", "山並みと都城軸に合わせて殿閣・門・庭園を配置した王宮。", "joseon"),

            W("parthenon", "パルテノン神殿", MasterpieceKind.Architecture, "ヨーロッパ・地中海", "紀元前5世紀", "イクティノス、カリクラテスら", "柱列と比例調整でアテナを祀ったドーリス式神殿。", "athens"),
            W("pantheon_rome", "ローマのパンテオン", MasterpieceKind.Architecture, "ヨーロッパ・地中海", "2世紀", "ハドリアヌス帝期の建築家たち", "巨大な無筋コンクリートドームと天窓を持つ円堂。", "rome"),
            W("hagia_sophia", "アヤソフィア", MasterpieceKind.Architecture, "ヨーロッパ・地中海", "6世紀", "アンテミオスとイシドロス", "ペンデンティヴで大ドームを支える東ローマの大聖堂。", "byzantium"),
            W("notre_dame_paris", "ノートルダム大聖堂（パリ）", MasterpieceKind.Architecture, "ヨーロッパ・地中海", "12～14世紀", "中世パリの建築家・工人たち", "交差リブ、飛梁、ステンドグラスを備えるゴシック大聖堂。", "france"),
            W("sagrada_familia", "サグラダ・ファミリア", MasterpieceKind.Architecture, "ヨーロッパ・地中海", "19世紀末～建設継続", "アントニ・ガウディほか", "構造・幾何学・彫刻を統合し建設が継続する聖堂。", "spain"),

            W("pyramid_of_sun", "テオティワカンの太陽のピラミッド", MasterpieceKind.Architecture, "アメリカ大陸", "2～3世紀", "テオティワカンの建築共同体", "都市軸に沿って築かれた巨大な段状基壇建築。", ""),
            W("tikal_temple_one", "ティカル1号神殿", MasterpieceKind.Architecture, "アメリカ大陸", "8世紀", "古典期マヤの建築家たち", "急勾配の階段と高い屋根飾りを持つ王墓神殿。", "maya"),
            W("el_castillo_chichen", "チチェン・イッツァのエル・カスティージョ", MasterpieceKind.Architecture, "アメリカ大陸", "9～12世紀", "後古典期マヤの建築家たち", "暦的数理と方位を組み込んだ階段ピラミッド。", "maya"),
            W("machu_picchu_architecture", "マチュ・ピチュ", MasterpieceKind.Architecture, "アメリカ大陸", "15世紀", "インカの石工・建築家たち", "山稜に石造建築・段々畑・水路を統合した都市施設。", "inca"),
            W("monks_mound", "カホキアのモンクス・マウンド", MasterpieceKind.Architecture, "アメリカ大陸", "10～13世紀", "ミシシッピ文化の共同体", "大量の土を層状に積み上げた北米最大級の土製基壇。", "mississippian"),

            W("nan_madol_architecture", "ナン・マドール", MasterpieceKind.Architecture, "オセアニア", "13～17世紀", "サウデル朝期の建築共同体", "玄武岩柱を積み、礁湖上の人工島群に儀礼・居住施設を築いた。", ""),
            W("taputapuatea_marae", "タプタプアテアのマラエ", MasterpieceKind.Architecture, "オセアニア", "古代～近世", "東ポリネシアの共同体", "石敷きの祭祀空間と海上交流の記憶を持つ文化的中心。", ""),
            W("te_hau_ki_turanga", "テ・ハウ・キ・トゥランガ", MasterpieceKind.Architecture, "オセアニア", "19世紀", "ラホアル・ルクポら", "祖先像と文様彫刻を構造体へ統合したマオリ集会所。", "maori"),
            W("sydney_opera_house", "シドニー・オペラハウス", MasterpieceKind.Architecture, "オセアニア", "20世紀", "ヨーン・ウツソンほか", "港湾景観に連続する殻状屋根を持つ舞台芸術施設。", ""),
            W("tjibaou_cultural_centre", "チバウ文化センター", MasterpieceKind.Architecture, "オセアニア", "20世紀末", "レンゾ・ピアノとカナク共同体", "カナクの住居形態・風・現代技術を結ぶ文化施設。", ""),

            // ==================================================================
            // 音楽・詠唱・音楽伝統（30件）
            // ==================================================================
            W("ahellil_gourara", "グララ地方のアヘリル", MasterpieceKind.Music, "アフリカ", "長期継承", "グララのゼナタ共同体", "詩、合唱、踊りを組み合わせるアルジェリア南西部の儀礼音楽。", ""),
            W("mande_griot_music", "マンデのグリオ音楽", MasterpieceKind.Music, "アフリカ", "中世以降", "マンデのジェリ共同体", "コラなどを伴い、系譜・歴史・称賛を歌い継ぐ音楽伝統。", "mali"),
            W("mbira_music", "ムビラ／サンシの音楽", MasterpieceKind.Music, "アフリカ", "長期継承", "マラウイ・ジンバブエの共同体", "金属鍵盤楽器の循環音型、歌、儀礼を結ぶ演奏伝統。", "great_zimbabwe"),
            W("ethiopian_orthodox_chant", "エチオピア正教会聖歌", MasterpieceKind.Music, "アフリカ", "古代末期以降", "エチオピア正教会の聖歌共同体", "ゲエズ語典礼と旋律体系を伝える聖歌。", "aksum"),
            W("highlife_music", "ハイライフ", MasterpieceKind.Music, "アフリカ", "20世紀", "西アフリカの音楽家たち", "金管、ギター、地域のリズムを融合して発展した都市音楽。", ""),

            W("samaveda_chant", "サーマ・ヴェーダ詠唱", MasterpieceKind.Music, "西・南アジア", "古代から継承", "ヴェーダ詠唱者たち", "聖句を定型旋律で歌い、口承で精密に伝える儀礼音楽。", "gupta"),
            W("persian_radif", "イラン音楽のラディーフ", MasterpieceKind.Music, "西・南アジア", "19世紀に体系化", "イランの音楽家系", "旋法・旋律型を記憶し即興へ展開する古典音楽の体系。", "persia"),
            W("shashmaqom", "シャシュマカーム", MasterpieceKind.Music, "西・南アジア", "中世以降", "中央アジアの演奏家たち", "六つのマカームを器楽・歌・詩で展開する宮廷音楽伝統。", ""),
            W("azerbaijani_mugham", "アゼルバイジャンのムガーム", MasterpieceKind.Music, "西・南アジア", "長期継承", "アゼルバイジャンの演奏家たち", "旋法枠組みの中で歌と器楽を即興的に展開する。", ""),
            W("raga_darbari", "ラーガ・ダルバーリー・カーナダー", MasterpieceKind.Music, "西・南アジア", "近世以降", "北インド古典音楽の演奏家たち", "深い夜の情感をゆっくり展開するヒンドゥスターニー音楽のラーガ。", "mughal"),

            W("flowing_water_guqin", "流水（古琴曲）", MasterpieceKind.Music, "東・東南アジア", "古代伝承・近世譜", "古琴演奏家たち", "水の動きを多様な奏法で表す古琴の代表曲。", "han"),
            W("etenraku", "越天楽", MasterpieceKind.Music, "東・東南アジア", "古代から継承", "雅楽の楽人たち", "管絃や舞楽で演奏される雅楽の代表的な旋律。", "japan"),
            W("gamelan_music", "ガムラン音楽", MasterpieceKind.Music, "東・東南アジア", "長期継承", "インドネシア各地の共同体", "ゴング・鍵盤打楽器・太鼓が周期構造を作る合奏伝統。", "majapahit"),
            W("chunhyangga", "春香歌", MasterpieceKind.Music, "東・東南アジア", "18～19世紀に形成", "パンソリの唱者たち", "春香の物語を一人の歌い手と鼓手が長時間演じるパンソリ演目。", "joseon"),
            W("nha_nhac", "ニャーニャック（ベトナム宮廷音楽）", MasterpieceKind.Music, "東・東南アジア", "15～20世紀", "ベトナム宮廷の楽人たち", "王朝の大礼・祭祀・外交儀礼で演奏された宮廷音楽。", "dai_viet"),

            W("gregorian_chant", "グレゴリオ聖歌", MasterpieceKind.Music, "ヨーロッパ・地中海", "中世初期以降", "西方教会の聖歌共同体", "ラテン語典礼文を単旋律で歌う聖歌伝統。", "franks"),
            W("brandenburg_concertos", "ブランデンブルク協奏曲", MasterpieceKind.Music, "ヨーロッパ・地中海", "18世紀初頭", "ヨハン・ゼバスティアン・バッハ", "多様な独奏楽器と合奏の組み合わせを探究した六曲の協奏曲集。", ""),
            W("mozart_symphony_40", "交響曲第40番", MasterpieceKind.Music, "ヨーロッパ・地中海", "18世紀", "ヴォルフガング・アマデウス・モーツァルト", "緊張を帯びた主題と精緻な構成を持つト短調交響曲。", ""),
            W("beethoven_symphony_9", "交響曲第9番", MasterpieceKind.Music, "ヨーロッパ・地中海", "19世紀", "ルートヴィヒ・ヴァン・ベートーヴェン", "交響曲終楽章へ独唱と合唱を導入した大規模作品。", ""),
            W("rite_of_spring", "春の祭典", MasterpieceKind.Music, "ヨーロッパ・地中海", "20世紀初頭", "イーゴリ・ストラヴィンスキー", "不規則なリズムと強い管弦楽法で原始的祭儀を表現したバレエ音楽。", ""),

            W("pirekua", "プレペチャのピレクア", MasterpieceKind.Music, "アメリカ大陸", "近世以降", "プレペチャの歌い手たち", "地域の言語・歴史・感情を多様な編成で歌う伝統歌。", ""),
            W("samba_de_roda", "レコンカヴォのサンバ・ジ・ホーダ", MasterpieceKind.Music, "アメリカ大陸", "近世以降", "ブラジル・バイーアの共同体", "歌、手拍子、打楽器、輪舞を結ぶアフロ・ブラジル音楽。", ""),
            W("cuban_son", "キューバン・ソン", MasterpieceKind.Music, "アメリカ大陸", "19～20世紀", "キューバの音楽共同体", "弦楽器・打楽器・歌を融合し、多くのラテン音楽へ影響した。", ""),
            W("black_brown_beige", "ブラック、ブラウン・アンド・ベージュ", MasterpieceKind.Music, "アメリカ大陸", "20世紀", "デューク・エリントン", "アフリカ系アメリカ人の歴史を描く長大なジャズ組曲。", "", "duke_ellington"),
            W("rhapsody_in_blue", "ラプソディ・イン・ブルー", MasterpieceKind.Music, "アメリカ大陸", "20世紀", "ジョージ・ガーシュウィン", "ジャズ語法と協奏的形式を結んだピアノと楽団の作品。", ""),

            W("maori_waiata", "マオリのワイアタ", MasterpieceKind.Music, "オセアニア", "長期継承", "マオリの歌い手たち", "系譜・哀悼・恋・土地・政治を共同体で歌い継ぐ伝統。", "maori"),
            W("kumulipo_chant", "クムリポ詠唱", MasterpieceKind.Music, "オセアニア", "18世紀以前から継承", "ハワイの詠唱者たち", "長大な創世・系譜テキストを儀礼的な節回しで伝える。", "hawaii"),
            W("aboriginal_songlines", "アボリジナルのソングライン", MasterpieceKind.Music, "オセアニア", "長期継承", "オーストラリア先住民諸共同体", "歌・物語・土地の経路・法を結ぶ知識伝承。", ""),
            W("hawaiian_mele_hula", "ハワイのメレ・フラ", MasterpieceKind.Music, "オセアニア", "長期継承", "ハワイの詠唱者・踊り手たち", "詩的詠唱、歌、身振りで神々・首長・土地・出来事を記憶する。", "hawaii"),
            W("garamut_music", "ガラムット木鼓の音楽", MasterpieceKind.Music, "オセアニア", "長期継承", "パプアニューギニアの諸共同体", "大型割れ目太鼓の音で合図・儀礼・踊り・共同体の声を担う。", ""),

            // ==================================================================
            // 演劇・舞台芸術（6地域×5件＝30件）
            // ==================================================================
            W("al_aragoz", "アル・アラゴズ", MasterpieceKind.Theater, "アフリカ", "長期継承", "エジプトの人形遣いたち", "手袋人形、即興的な対話、音楽で社会や日常を風刺する伝統的な人形劇。", ""),
            W("alarinjo_theatre", "ヨルバのアラリンジョ劇", MasterpieceKind.Theater, "アフリカ", "近世以降", "ヨルバの旅芸人一座", "仮面、音楽、踊り、寸劇を携えて各地を巡るヨルバの移動演劇伝統。", ""),
            W("koteba_theatre", "コテバ劇", MasterpieceKind.Theater, "アフリカ", "長期継承", "マリのバマナ共同体", "収穫期などに踊りと風刺劇を演じ、共同体の問題を笑いと批評で映す。", ""),
            W("death_and_kings_horseman", "死と王の先導者", MasterpieceKind.Theater, "アフリカ", "20世紀", "ウォレ・ショインカ", "ヨルバ社会と植民地支配の衝突を、儀礼と責任をめぐる悲劇として描く戯曲。", ""),
            W("sizwe_banzi_is_dead", "シズウェ・バンジは死んだ", MasterpieceKind.Theater, "アフリカ", "20世紀", "アソル・フガード、ジョン・カニ、ウィンストン・ヌショナ", "身分証制度の下で生きる人物を通じ、アパルトヘイトの日常を描いた共同創作劇。", ""),

            W("kutiyattam", "クーティヤッタム", MasterpieceKind.Theater, "西・南アジア", "古代から継承", "ケーララの俳優・演奏家共同体", "寺院劇場でサンスクリット劇を精緻な身振り、表情、打楽器とともに演じる。", ""),
            W("abhijnanasakuntalam", "シャクンタラー", MasterpieceKind.Theater, "西・南アジア", "4～5世紀ごろ", "カーリダーサ", "記憶を失わせる呪いと再会を軸に、愛と王権を描くサンスクリット戯曲。", "gupta", "kalidasa"),
            W("tazieh", "タアズィーエ", MasterpieceKind.Theater, "西・南アジア", "近世以降", "イランの演者・共同体", "殉教の物語を詩、音楽、象徴的な衣装と円形の演技空間で表す宗教劇。", ""),
            W("karagoz_shadow_theatre", "カラギョズ影絵芝居", MasterpieceKind.Theater, "西・南アジア", "近世以降", "トルコの影絵人形遣いたち", "彩色した皮人形と即興的な会話で社会の多様な人物を風刺する影絵劇。", "ottoman"),
            W("naqqali", "ナッガーリー", MasterpieceKind.Theater, "西・南アジア", "長期継承", "イランの語り手たち", "叙事詩や歴史物語を声、身振り、ときに絵幕を使って演じる劇的語り。", ""),

            W("nogaku", "能楽", MasterpieceKind.Theater, "東・東南アジア", "中世以降", "能・狂言の演者と楽師たち", "仮面、謡、舞、器楽を統合する能と、対話喜劇の狂言からなる舞台芸術。", "japan"),
            W("kabuki", "歌舞伎", MasterpieceKind.Theater, "東・東南アジア", "17世紀以降", "歌舞伎の俳優・作者・演奏家たち", "様式化された演技、化粧、衣装、音楽、舞台機構を結ぶ日本の商業演劇。", "japan"),
            W("bunraku", "人形浄瑠璃文楽", MasterpieceKind.Theater, "東・東南アジア", "17世紀以降", "太夫・三味線奏者・人形遣いたち", "語り、三味線、複数人で操る人形が緊密に協働する劇場芸術。", "japan"),
            W("peking_opera", "京劇", MasterpieceKind.Theater, "東・東南アジア", "18～19世紀以降", "京劇の俳優・楽師たち", "唱、念、做、打と象徴的な衣装・化粧を組み合わせる中国の舞台芸術。", ""),
            W("wayang", "ワヤン人形劇", MasterpieceKind.Theater, "東・東南アジア", "長期継承", "インドネシアのダランと演奏家たち", "人形遣いが影絵・人形・語りをガムランと結び、叙事詩や地域物語を演じる。", "majapahit"),

            W("oedipus_rex", "オイディプス王", MasterpieceKind.Theater, "ヨーロッパ・地中海", "紀元前5世紀", "ソポクレス", "疫病の原因を追う王が自らの過去へ行き着く、古代アテネの悲劇。", "athens"),
            W("commedia_dell_arte", "コメディア・デッラルテ", MasterpieceKind.Theater, "ヨーロッパ・地中海", "16世紀以降", "イタリアの旅回り劇団", "定型人物、仮面、筋書きを基礎に俳優の即興で展開する喜劇伝統。", ""),
            W("hamlet_stage", "ハムレット", MasterpieceKind.Theater, "ヨーロッパ・地中海", "17世紀初頭", "ウィリアム・シェイクスピア", "父王の死をめぐる復讐と逡巡を通して権力、演技、死を問う悲劇。", "england", "william_shakespeare"),
            W("a_dolls_house", "人形の家", MasterpieceKind.Theater, "ヨーロッパ・地中海", "19世紀", "ヘンリック・イプセン", "家庭と社会の規範の中で主人公が自己決定へ向かう近代劇。", ""),
            W("waiting_for_godot", "ゴドーを待ちながら", MasterpieceKind.Theater, "ヨーロッパ・地中海", "20世紀", "サミュエル・ベケット", "二人の人物が来ない誰かを待ち続ける反復から、時間と存在を描く戯曲。", ""),

            W("rabinal_achi", "ラビナル・アチ", MasterpieceKind.Theater, "アメリカ大陸", "15世紀以前から継承", "マヤ・アチ共同体", "仮面、舞踊、音楽、台詞で対立と捕縛、儀礼を演じ継ぐマヤの舞踊劇。", "maya"),
            W("a_raisin_in_the_sun", "陽なたの干しぶどう", MasterpieceKind.Theater, "アメリカ大陸", "20世紀", "ロレイン・ハンズベリー", "住宅差別に直面する黒人家族の夢と世代間の選択を描いた戯曲。", ""),
            W("death_of_a_salesman", "セールスマンの死", MasterpieceKind.Theater, "アメリカ大陸", "20世紀", "アーサー・ミラー", "老セールスマンの記憶と家族関係を通じ、成功神話の重圧を描く。", ""),
            W("the_rez_sisters", "ザ・レズ・シスターズ", MasterpieceKind.Theater, "アメリカ大陸", "20世紀", "トムソン・ハイウェイ", "カナダ先住民居留地の女性たちの旅と共同体を、笑いと喪失を交えて描く。", ""),
            W("zoot_suit", "ズート・スーツ", MasterpieceKind.Theater, "アメリカ大陸", "20世紀", "ルイス・バルデス", "若者への冤罪事件とメディア表象をチカーノ演劇の音楽・語りで再構成する。", ""),

            W("pohutukawa_tree_play", "ポフツカワの樹", MasterpieceKind.Theater, "オセアニア", "20世紀", "ブルース・メイソン", "土地、信仰、世代の葛藤をマオリ女性と家族の視点から描いたニュージーランド戯曲。", "maori"),
            W("no_sugar_play", "ノー・シュガー", MasterpieceKind.Theater, "オセアニア", "20世紀", "ジャック・デイヴィス", "強制移住と管理政策に抵抗するヌンガーの家族を描くオーストラリア演劇。", ""),
            W("the_dreamers_play", "ザ・ドリーマーズ", MasterpieceKind.Theater, "オセアニア", "20世紀", "ジャック・デイヴィス", "都市生活の中で家族、記憶、植民地化の影響を描く先住民演劇。", ""),
            W("waiora_play", "ワイオラ", MasterpieceKind.Theater, "オセアニア", "20世紀末", "ホネ・コウカ", "都会へ移ったマオリ家族の記憶、喪失、帰属を世代横断で描く戯曲。", "maori"),
            W("woman_far_walking", "ウーマン・ファー・ウォーキング", MasterpieceKind.Theater, "オセアニア", "21世紀初頭", "ウィティ・イヒマエラ", "長い生涯を生きたマオリ女性の記憶から、土地と歴史の変化をたどる。", "maori"),

            // ==================================================================
            // 映画・映像作品（6地域×5件＝30件）
            // ==================================================================
            W("black_girl_1966", "ブラック・ガール", MasterpieceKind.Film, "アフリカ", "1966年", "ウスマン・センベーヌ", "フランスへ渡ったセネガル人女性の労働と孤立を、植民地主義の余波として描く。", ""),
            W("cairo_station", "カイロ駅", MasterpieceKind.Film, "アフリカ", "1958年", "ユーセフ・シャヒーン", "駅で働く人々と孤独な新聞売りを通じ、都市の欲望と格差を描く。", ""),
            W("touki_bouki", "トゥキ・ブゥキ／ハイエナの旅", MasterpieceKind.Film, "アフリカ", "1973年", "ジブリル・ジオップ・マンベティ", "ダカールを離れパリを夢見る若者たちを大胆な音と映像の連結で描く。", ""),
            W("yeelen", "イェーレン", MasterpieceKind.Film, "アフリカ", "1987年", "スレイマン・シセ", "バンバラの口承と宇宙観を背景に、父と子の力と知識をめぐる旅を描く。", ""),
            W("battle_of_algiers", "アルジェの戦い", MasterpieceKind.Film, "アフリカ", "1966年", "ジッロ・ポンテコルヴォとアルジェリアの製作陣", "アルジェリア独立戦争の都市闘争を、ニュース映像を思わせる演出で再構成する。", ""),

            W("raja_harishchandra", "ラージャ・ハリシュチャンドラ", MasterpieceKind.Film, "西・南アジア", "1913年", "ダーダーサーヘブ・パールケー", "神話上の王の試練を描いた、インド初期の長編無声映画。", ""),
            W("pather_panchali", "大地のうた", MasterpieceKind.Film, "西・南アジア", "1955年", "サタジット・レイ", "ベンガル農村の一家と子どもの成長を、環境と日常の細部から描く。", ""),
            W("house_is_black", "あの家は黒い", MasterpieceKind.Film, "西・南アジア", "1962年", "フォルーグ・ファッロフザード", "療養施設の日常を詩と記録映像で見つめ、尊厳と隔離を問う短編。", ""),
            W("the_cow_1969", "牛", MasterpieceKind.Film, "西・南アジア", "1969年", "ダリウシュ・メールジュイ", "唯一の牛を失った村人の変容を通じ、共同体、貧困、自己認識を描く。", ""),
            W("close_up_1990", "クローズ・アップ", MasterpieceKind.Film, "西・南アジア", "1990年", "アッバス・キアロスタミ", "映画監督になりすました人物の事件を本人たちと再演し、真実と演技の境界を問う。", ""),

            W("tokyo_story", "東京物語", MasterpieceKind.Film, "東・東南アジア", "1953年", "小津安二郎", "老夫婦の上京と家族の距離を、抑制された構図と日常の時間で描く。", "japan"),
            W("seven_samurai", "七人の侍", MasterpieceKind.Film, "東・東南アジア", "1954年", "黒澤明", "農村を守る侍と村人の協働を、群像劇と動的な戦闘演出で描く。", "japan"),
            W("spring_in_small_town", "小城之春", MasterpieceKind.Film, "東・東南アジア", "1948年", "費穆", "戦後の傷を抱える家庭へ旧友が訪れ、抑えた感情と選択が揺れる様子を描く。", ""),
            W("manila_claws_light", "マニラ・光る爪", MasterpieceKind.Film, "東・東南アジア", "1975年", "リノ・ブロッカ", "恋人を探して首都へ出た青年の目を通じ、労働搾取と都市の格差を描く。", ""),
            W("king_white_elephant", "白象王", MasterpieceKind.Film, "東・東南アジア", "1940年", "プリーディー・パノムヨンら", "戦争を避けようとする王を描き、制作時代の平和への主張を託したタイ映画。", "ayutthaya"),

            W("workers_leaving_factory", "工場の出口", MasterpieceKind.Film, "ヨーロッパ・地中海", "1895年", "リュミエール兄弟", "工場から出る人々を固定カメラで記録した、初期映画上映を代表する短編。", "france"),
            W("battleship_potemkin", "戦艦ポチョムキン", MasterpieceKind.Film, "ヨーロッパ・地中海", "1925年", "セルゲイ・エイゼンシュテイン", "反乱と弾圧を、衝突するショットの編集で構成した無声映画。", ""),
            W("metropolis_film", "メトロポリス", MasterpieceKind.Film, "ヨーロッパ・地中海", "1927年", "フリッツ・ラングと製作陣", "階層化された未来都市を大規模な美術、特撮、群衆演出で表現する。", ""),
            W("bicycle_thieves", "自転車泥棒", MasterpieceKind.Film, "ヨーロッパ・地中海", "1948年", "ヴィットリオ・デ・シーカ", "仕事に必要な自転車を盗まれた父子の探索から、戦後ローマの生活を描く。", ""),
            W("jeanne_dielman", "ジャンヌ・ディエルマン", MasterpieceKind.Film, "ヨーロッパ・地中海", "1975年", "シャンタル・アケルマン", "家事と仕事の反復を長い固定ショットで追い、日常時間の揺らぎを描く。", ""),

            W("citizen_kane", "市民ケーン", MasterpieceKind.Film, "アメリカ大陸", "1941年", "オーソン・ウェルズと製作陣", "新聞王の生涯を複数の証言、深い焦点、時間を横断する構成で探る。", ""),
            W("meshes_afternoon", "午後の網目", MasterpieceKind.Film, "アメリカ大陸", "1943年", "マヤ・デレン、アレクサンダー・ハミッド", "反復する物、人物、動作で夢と自己像を組み替える実験短編。", ""),
            W("memories_underdevelopment", "低開発の記憶", MasterpieceKind.Film, "アメリカ大陸", "1968年", "トマス・グティエレス・アレア", "革命後のハバナに残る知識人の疎外を、記録映像と主観的記憶で描く。", ""),
            W("hour_furnaces", "燃える時の記録", MasterpieceKind.Film, "アメリカ大陸", "1968年", "フェルナンド・ソラナス、オクタビオ・ヘティノ", "植民地主義と社会運動を論じ、上映と討議を行動へ結ぶ長編ドキュメンタリー。", ""),
            W("city_of_god_film", "シティ・オブ・ゴッド", MasterpieceKind.Film, "アメリカ大陸", "2002年", "フェルナンド・メイレレス、カチア・ルンド", "リオデジャネイロの地区で育つ若者たちを、写真家を目指す語り手から描く。", ""),

            W("story_kelly_gang", "ケリー・ギャング物語", MasterpieceKind.Film, "オセアニア", "1906年", "チャールズ・テイトと製作陣", "ネッド・ケリーの物語を長尺で描いたオーストラリア初期映画で、現存部分は断片。", ""),
            W("utu_film", "UTU／復讐", MasterpieceKind.Film, "オセアニア", "1983年", "ジェフ・マーフィー", "ニュージーランド戦争を背景に、マオリ兵士の離反と復讐を複数の視点で描く。", "maori"),
            W("ten_canoes", "十艘のカヌー", MasterpieceKind.Film, "オセアニア", "2006年", "ロルフ・デ・ヒーア、ピーター・ジギルと共同体", "アーネムランドの語りとヨルング諸語を用い、物語の中の物語として先祖の時代を描く。", ""),
            W("samson_delilah_2009", "サムソンとデリラ", MasterpieceKind.Film, "オセアニア", "2009年", "ワーウィック・ソーントン", "中央オーストラリアの若者二人の移動と関係を、少ない台詞と風景から描く。", ""),
            W("vai_film", "Vai～命の物語", MasterpieceKind.Film, "オセアニア", "2019年", "太平洋地域の女性監督9人", "一人の女性の人生段階を複数の太平洋諸島でつなぐ共同製作映画。", ""),

            // ==================================================================
            // 作品史追加バッチ（7分野×6地域＝42件）
            // 既存210件のID・順序を固定し、セーブ互換のため末尾へ追加する。
            // ==================================================================

            // 書籍（各地域1件）
            W("kebra_nagast", "ケブラ・ナガスト", MasterpieceKind.Book, "アフリカ", "14世紀に編纂", "エチオピアの書記・聖職者たち", "エチオピア王権と宗教的伝承を結ぶ物語を、複数の資料と翻訳伝統から編んだ文書。", "ethiopia"),
            W("dnyaneshwari", "ジュニャーネーシュワリー", MasterpieceKind.Book, "西・南アジア", "13世紀", "ジュニャーネーシュヴァル", "『バガヴァッド・ギーター』をマラーティー語の韻文で注釈し、哲学を広い聴衆へ伝えた。", "", "dnyaneshwar"),
            W("omoro_soshi", "おもろさうし", MasterpieceKind.Book, "東・東南アジア", "16～17世紀に編纂", "琉球王府の編纂者たち", "琉球各地の歌謡を22巻に集成し、祭祀、地域、英雄、航海の記憶を伝える。", "ryukyu"),
            W("odyssey", "オデュッセイア", MasterpieceKind.Book, "ヨーロッパ・地中海", "紀元前8世紀ごろ", "ホメロス伝承", "トロイア戦争後のオデュッセウスの帰郷と、待つ家族の選択を交互に語る叙事詩。", "athens"),
            W("nueva_coronica_buen_gobierno", "新しい年代記と良き統治", MasterpieceKind.Book, "アメリカ大陸", "17世紀初頭", "フェリペ・グアマン・ポマ・デ・アヤラ", "多数の挿絵と文章でアンデス史を記し、植民地統治を批判して改革を訴えた年代記。", "inca", "guaman_poma"),
            W("tales_of_tikongs", "ティコン島の物語", MasterpieceKind.Book, "オセアニア", "1983年", "エペリ・ハウオファ", "架空の太平洋島社会を舞台に、開発援助と官僚制を風刺した連作短編。", "tonga", "epeli_hauofa"),

            // 絵画（各地域1件）
            W("tutu_enwonwu", "トゥトゥ", MasterpieceKind.Painting, "アフリカ", "1973～1974年", "ベン・エンウォンウ", "アデトゥトゥ・アデミルイを描いた肖像画群で、戦後ナイジェリアの和解を象徴する作品として受容された。", ""),
            W("bharat_mata", "バーラト・マータ", MasterpieceKind.Painting, "西・南アジア", "1905年ごろ", "アバニンドラナート・タゴール", "土地と共同体を贈り物を携えた人物像として表した水彩画。", ""),
            W("eight_views_ryukyu", "琉球八景", MasterpieceKind.Painting, "東・東南アジア", "1832年", "葛飾北斎", "清の冊封使が伝えた琉球の景観資料をもとに、八つの風景を想像力豊かに再構成した錦絵。", "ryukyu", "hokusai"),
            W("arnolfini_portrait", "アルノルフィーニ夫妻像", MasterpieceKind.Painting, "ヨーロッパ・地中海", "1434年", "ヤン・ファン・エイク", "室内の二人、鏡、光、生活用品を精密に描き、多様な解釈を生んだ油彩画。", ""),
            W("migration_series", "マイグレーション・シリーズ", MasterpieceKind.Painting, "アメリカ大陸", "1940～1941年", "ジェイコブ・ローレンス", "アフリカ系住民の大移動を、短い文と60枚のテンペラ画で連続的に描いた。", ""),
            W("alhalker_suite", "アルハルケル連作", MasterpieceKind.Painting, "オセアニア", "1993年", "エミリー・カム・クングワレイ", "故郷アルハルケルの雨、植物、土地との関係を色と筆触で展開した22枚の連作。", ""),

            // 彫刻（各地域1件）
            W("idia_pendant_mask", "イドゥア王母のペンダント仮面", MasterpieceKind.Sculpture, "アフリカ", "16世紀", "ベニン王国の象牙彫刻工房", "王母イドゥアを記念するとされる象牙製の儀礼用肖像で、交易相手を示す小像も刻む。", "benin"),
            W("gommateshwara_bahubali", "ゴンマテーシュワラ（バーフバリ）像", MasterpieceKind.Sculpture, "西・南アジア", "981年", "ガンガ朝期の石工たち", "シュラヴァナベルゴラの岩山に立つ、ジャイナ教のバーフバリを表す巨大な一石彫像。", ""),
            W("ngoc_lu_bronze_drum", "ゴックルー銅鼓", MasterpieceKind.Sculpture, "東・東南アジア", "紀元前1千年紀後半", "ドンソン文化の鋳造工たち", "中央の星形文、舟、人、鳥、幾何文を精緻に鋳出した大型青銅鼓。", ""),
            W("marcus_aurelius_equestrian", "マルクス・アウレリウス騎馬像", MasterpieceKind.Sculpture, "ヨーロッパ・地中海", "2世紀後半", "ローマの青銅鋳造工たち", "皇帝が馬上から手を差し伸べる姿を等身大以上で表した、古代ローマの青銅騎馬像。", "rome"),
            W("death_of_cleopatra_lewis", "クレオパトラの死", MasterpieceKind.Sculpture, "アメリカ大陸", "1876年", "エドモニア・ルイス", "死の直後のクレオパトラを理想化だけに頼らず表した大理石彫刻。", "", "edmonia_lewis"),
            W("aboriginal_memorial", "アボリジナル・メモリアル", MasterpieceKind.Sculpture, "オセアニア", "1987～1988年", "ラミンギニングの芸術家たちとジョン・マンディン", "土地を守って命を失った先住民を悼む200本の中空棺を、河川と氏族の土地関係に沿って配置した。", ""),

            // 建築（各地域1件）
            W("fasil_ghebbi_architecture", "ファシル・ゲビ", MasterpieceKind.Architecture, "アフリカ", "17～18世紀", "ゴンダール宮廷の建築家・職人たち", "宮殿、広間、図書施設などを城壁内に重ね、エチオピア内外の建築要素を結んだ王宮複合体。", "ethiopia"),
            W("raigad_fort_architecture", "ラーイガド城塞", MasterpieceKind.Architecture, "西・南アジア", "17世紀", "マラーターの建築家・職人たち", "山上の地形を生かし、門、防御施設、貯水、宮殿、市場を組み合わせた王都城塞。", "maratha"),
            W("shuri_castle_architecture", "首里城", MasterpieceKind.Architecture, "東・東南アジア", "14世紀以降", "琉球王府の建築家・職人たち", "琉球王国の政治・外交・祭祀の中心として、中国、日本、島々との交流を独自の空間構成へ結んだ。", "ryukyu"),
            W("wawel_castle_architecture", "ヴァヴェル城・大聖堂群", MasterpieceKind.Architecture, "ヨーロッパ・地中海", "中世～近世", "ポーランドの王侯と各地の建築家・職人たち", "丘上に宮殿、大聖堂、中庭、防御施設を重ね、王権と都市の長い変化を伝える建築群。", "polish_lithuanian"),
            W("caguana_ceremonial_architecture", "カグアナ祭祀広場群", MasterpieceKind.Architecture, "アメリカ大陸", "13～15世紀ごろ", "タイノの共同体", "石で縁取った広場と球戯場、岩面彫刻を景観の中に組み合わせた祭祀空間。", "taino"),
            W("haamonga_trilithon_architecture", "ハアモンガ・ア・マウイ", MasterpieceKind.Architecture, "オセアニア", "13世紀ごろ", "トンガの石工たち", "三つの珊瑚石灰岩を組んだ巨石門で、トゥイ・トンガ王権期の記念的景観を伝える。", "tonga"),

            // 音楽（各地域1件）
            W("tizita_music", "ティジータ", MasterpieceKind.Music, "アフリカ", "長く継承される音楽伝統", "エチオピアの歌手・演奏家たち", "記憶、郷愁、別離を歌い、旋法と即興を通して世代ごとに表現を更新する音楽伝統。", "ethiopia"),
            W("marathi_abhang", "マラーティーのアバンガ", MasterpieceKind.Music, "西・南アジア", "中世以降", "ヴァールカリーの詩人・歌い手たち", "マラーティー語の短い信仰詩を旋律に乗せ、巡礼と共同歌唱で伝える。", ""),
            W("ryukyu_classical_music", "琉球古典音楽", MasterpieceKind.Music, "東・東南アジア", "17世紀以降", "琉球王府と地域の音楽家たち", "三線と歌を中心に、琉歌の言葉、節回し、宮廷芸能を継承する音楽。", "ryukyu"),
            W("heroic_polonaise", "ポロネーズ第6番『英雄』", MasterpieceKind.Music, "ヨーロッパ・地中海", "1842年", "フレデリック・ショパン", "ポロネーズの舞曲リズムを力強い主題と大規模なピアノ書法へ展開した作品。", ""),
            W("taino_areito_music", "タイノのアレイト", MasterpieceKind.Music, "アメリカ大陸", "植民地化以前に成立", "タイノの歌い手・踊り手たち", "歌、踊り、物語を結び、共同体の歴史や儀礼を共有した集団的表現として記録された。", "taino"),
            W("tongan_lakalaka_music", "トンガのラカラカ", MasterpieceKind.Music, "オセアニア", "19世紀以降", "トンガの詩人・振付家・共同体", "詩、歌、器楽的多声、整列した身振りを結び、歴史や価値を大人数で表現する。", "tonga"),

            // 演劇（各地域1件）
            W("lion_and_jewel", "ライオンと宝石", MasterpieceKind.Theater, "アフリカ", "1959年", "ウォレ・ショインカ", "ヨルバの村を舞台に、近代化、伝統、欲望、交渉を喜劇として描く。", ""),
            W("ghashiram_kotwal", "ガーシーラーム・コートワール", MasterpieceKind.Theater, "西・南アジア", "1972年", "ヴィジャイ・テンドゥルカル", "18世紀プネーを舞台に、音楽と群舞を用いて権力、腐敗、群衆心理を描く政治劇。", "maratha"),
            W("nido_tekiuchi", "二童敵討", MasterpieceKind.Theater, "東・東南アジア", "1719年", "玉城朝薫", "父を討たれた兄弟の仇討ちを、琉球の音楽、舞踊、古語で演じる組踊の古典。", "ryukyu"),
            W("dziady_play", "祖霊祭", MasterpieceKind.Theater, "ヨーロッパ・地中海", "1823～1832年", "アダム・ミツキェヴィチ", "祖先を迎える儀礼、個人の苦悩、歴史と亡命を断片的な詩劇に重ねる。", "polish_lithuanian"),
            W("dream_monkey_mountain", "猿山の夢", MasterpieceKind.Theater, "アメリカ大陸", "1967年", "デレック・ウォルコット", "カリブ海の仮面劇的な夢と現実を交差させ、植民地化と自己像を問う。", ""),
            W("songmakers_chair", "ソングメーカーズ・チェア", MasterpieceKind.Theater, "オセアニア", "2003年", "アルバート・ウェント", "サモア系家族の集まりを通して、移住、記憶、世代間の責任と家族の権威を描く。", "samoa", "albert_wendt"),

            // 映画（各地域1件）
            W("moolaade_film", "母たちの村", MasterpieceKind.Film, "アフリカ", "2004年", "ウスマン・センベーヌ", "少女たちへ保護を与えた女性を軸に、身体を傷つける慣行と庇護の原則が対立する村を描く。", ""),
            W("a_separation_film", "別離", MasterpieceKind.Film, "西・南アジア", "2011年", "アスガー・ファルハディ", "家族の別居と介護をめぐる出来事から、法、階層、信仰、責任の衝突を複数の視点で描く。", ""),
            W("parasite_film", "パラサイト 半地下の家族", MasterpieceKind.Film, "東・東南アジア", "2019年", "ポン・ジュノと製作陣", "二つの家族の接近を、住居の高低差とジャンルの転換を通して階級格差の物語にした。", ""),
            W("passion_joan_arc_film", "裁かるゝジャンヌ", MasterpieceKind.Film, "ヨーロッパ・地中海", "1928年", "カール・テオドア・ドライヤー", "ジャンヌ・ダルクの裁判と殉教を、顔の接写と簡素な空間で強く構成した無声映画。", "france"),
            W("roma_2018_film", "ROMA／ローマ", MasterpieceKind.Film, "アメリカ大陸", "2018年", "アルフォンソ・キュアロン", "1970年代メキシコ市の家族と家事労働者の日常を、社会の動乱と個人的記憶に重ねる。", ""),
            W("whale_rider_film", "クジラの島の少女", MasterpieceKind.Film, "オセアニア", "2002年", "ニキ・カーロと製作陣", "マオリの少女パイが伝統を学び、世代間の葛藤を越えて共同体を導く力を示す。", "maori"),

            // ==================================================================
            // 作品史追加バッチ第3弾（7分類×6地域＝42件）
            // 既存252件のID・順序を固定し、セーブ互換のため末尾へ追加する。
            // ==================================================================

            // 書籍・文書（各地域1件）
            W("basekabaka_be_buganda", "ブガンダ歴代王記", MasterpieceKind.Book, "アフリカ", "1901年初版・後に増補", "アポロ・カグワ", "ブガンダの王統、制度、口承をルガンダ語で記録した歴史書。版を重ねながら内容が増補された。", "buganda", "apollo_kaggwa"),
            W("buddhacarita", "ブッダチャリタ（仏所行讃）", MasterpieceKind.Book, "西・南アジア", "1～2世紀ごろ", "馬鳴（アシュヴァゴーシャ）", "釈迦の生涯をサンスクリット叙事詩として構成し、写本と諸言語への翻訳で伝わった。", "kushan", "ashvaghosha"),
            W("hikayat_aceh", "ヒカヤット・アチェ", MasterpieceKind.Book, "東・東南アジア", "17世紀", "アチェ宮廷の編者たち", "イスカンダル・ムダの生涯と王権をマレー語で語る宮廷年代記。複数の写本が伝える。", "aceh"),
            W("hypnerotomachia_poliphili", "ヒュプネロトマキア・ポリフィリ", MasterpieceKind.Book, "ヨーロッパ・地中海", "1499年", "作者未詳／アルドゥス・マヌティウス刊", "夢の旅を精緻な木版挿絵と実験的な言語で構成した、ヴェネツィア印刷文化の初期刊本。", "venice"),
            W("hombres_que_disperso_danza", "ロス・オンブレス・ケ・ディスペルソ・ラ・ダンサ", MasterpieceKind.Book, "アメリカ大陸", "1929年", "アンドレス・エネストロサ", "サポテカの口承、人物、土地の記憶をスペイン語文学へ再構成した物語集。", "zapotec", "andres_henestrosa"),
            W("kohau_rongorongo_tablet", "コハウ・ロンゴロンゴ木板", MasterpieceKind.Book, "オセアニア", "18～19世紀初頭", "ラパ・ヌイの記録者たち", "木板に絵文字状の記号を刻んだ文書。記号体系は現在も確定的には解読されていない。", "rapa_nui"),

            // 絵画（各地域1件）
            W("asante_adinkra_cloth_1817", "1817年収集のアシャンティ・アディンクラ布", MasterpieceKind.Painting, "アフリカ", "19世紀初頭", "アシャンティの染織職人たち", "濃色の布面へ反復文様を捺染した衣装用染織。1817年の戦いに関わる来歴とともに伝わる。", "asante"),
            W("kizil_cave_murals", "キジル石窟壁画", MasterpieceKind.Painting, "西・南アジア", "3～8世紀", "亀茲地域の画工たち", "石窟寺院の壁面に仏教説話、礼拝像、寄進者などを描き、シルクロードの交流を伝える。", ""),
            W("goryeo_water_moon_avalokiteshvara", "高麗の水月観音図", MasterpieceKind.Painting, "東・東南アジア", "14世紀", "高麗の仏画工房", "岩上の観音と善財童子を、精緻な線、金泥、透明感ある彩色で表した高麗仏画。", "goryeo"),
            W("procession_st_marks_square", "サン・マルコ広場の行列", MasterpieceKind.Painting, "ヨーロッパ・地中海", "1496年", "ジェンティーレ・ベッリーニ", "聖遺物を伴う大行列と広場の建築、人々の装いを大画面に組み上げたヴェネツィア絵画。", "venice"),
            W("chief_joseph_series_17", "チーフ・ジョセフ・シリーズ第17番", MasterpieceKind.Painting, "アメリカ大陸", "1976年", "ケイ・ウォーキングスティック", "蝋を用いた抽象的な色面と反復形で、歴史上の人物と記憶への応答を構成した絵画。", "cherokee"),
            W("ana_kai_tangata_rock_art", "アナ・カイ・タンガタの岩絵", MasterpieceKind.Painting, "オセアニア", "年代未確定", "ラパ・ヌイの画工たち", "海食洞の天井や壁に鳥や舟などの顔料画を残し、島の儀礼と海洋文化を伝える岩絵群。", "rapa_nui"),

            // 彫刻（各地域1件）
            W("sika_dwa_kofi", "黄金の椅子（シカ・ドゥワ・コフィ）", MasterpieceKind.Sculpture, "アフリカ", "17世紀末ごろの伝承", "アシャンティの祭司・工人たち", "アシャンティ共同体の魂と統合を象徴する聖なる椅子。伝承ではオコンフォ・アノキェと結び付けられる。", "asante", "okomfo_anokye"),
            W("gandhara_parinirvana_relief", "ガンダーラの仏涅槃浮彫", MasterpieceKind.Sculpture, "西・南アジア", "2～3世紀", "ガンダーラの石工たち", "横たわる釈迦と周囲の会衆を片岩へ彫り、涅槃の場面を凝縮した仏教浮彫。", "kushan"),
            W("goryeo_gilt_bronze_buddha", "高麗の金銅立像", MasterpieceKind.Sculpture, "東・東南アジア", "10～11世紀", "高麗の鋳造工たち", "均整の取れた立ち姿、衣文、手のしぐさを小型の金銅像へまとめた仏教彫刻。", "goryeo"),
            W("mars_neptune_doges_palace", "巨人の階段のマルスとネプトゥヌス", MasterpieceKind.Sculpture, "ヨーロッパ・地中海", "16世紀", "ヤコポ・サンソヴィーノ", "ヴェネツィアの陸海の支配を象徴する二神像を、ドゥカーレ宮殿の儀礼動線へ配置した。", "venice"),
            W("monte_alban_danzantes", "モンテ・アルバンの『踊る人々』石彫", MasterpieceKind.Sculpture, "アメリカ大陸", "紀元前500年以降", "サポテカの石工たち", "人体と初期文字の要素を刻んだ石板群。人物の解釈には複数の学説があり、断定を避けて研究される。", "zapotec"),
            W("hoa_hakananai_a", "ホア・ハカナナイア", MasterpieceKind.Sculpture, "オセアニア", "1000～1200年ごろ", "ラパ・ヌイの石工たち", "玄武岩製のモアイで、背面には後世の鳥人儀礼に関わる岩刻が加えられている。", "rapa_nui"),

            // 建築（各地域1件）
            W("muzibu_azaala_mpanga", "ムジブ・アザーラ・ムパンガ", MasterpieceKind.Architecture, "アフリカ", "1882年宮殿・1884年以降王墓", "ブガンダの建築家・職人たち", "巨大な茅葺き屋根と木、葦などを用い、カスビ王墓の中心建築として王統と儀礼を担う。", "buganda"),
            W("takht_i_bahi_monastery", "タフティ・バヒ仏教僧院", MasterpieceKind.Architecture, "西・南アジア", "1～7世紀", "ガンダーラの僧団と建築職人たち", "丘上に祠堂、ストゥーパ院、僧房を段階的に築いた僧院遺跡。クシャーナ期を含む複数時期を伝える。", "kushan"),
            W("baiturrahman_grand_mosque", "バイトゥラフマン大モスク", MasterpieceKind.Architecture, "東・東南アジア", "17世紀創建・19世紀以降再建", "アチェの宮廷と後世の建築家・職人たち", "アチェ王国期の創建伝承を持ち、1873年の焼失後に再建・拡張されたバンダ・アチェの礼拝建築。", "aceh"),
            W("doges_palace_venice", "ヴェネツィアのドゥカーレ宮殿", MasterpieceKind.Architecture, "ヨーロッパ・地中海", "14～16世紀を中心に形成", "ヴェネツィアの建築家・石工たち", "総督の居館、議会、裁判、行政機能を中庭と大広間にまとめ、都市国家の統治を可視化した。", "venice"),
            W("mitla_palace_group", "ミトラの宮殿群", MasterpieceKind.Architecture, "アメリカ大陸", "9～16世紀", "サポテカとミシュテカの建築職人たち", "中庭、列柱広間、石材モザイクの幾何学文様を組み合わせた複数の記念的建築群。", "zapotec"),
            W("ahu_tongariki", "アフ・トンガリキ", MasterpieceKind.Architecture, "オセアニア", "10～16世紀ごろ", "ラパ・ヌイの共同体", "15体のモアイを載せる島最大級の祭祀基壇で、海岸景観と祖先表象を結び付ける。", "rapa_nui"),

            // 音楽（各地域1件）
            W("buganda_royal_drum_repertoire", "ブガンダ王宮太鼓のレパートリー", MasterpieceKind.Music, "アフリカ", "長く継承される王宮音楽", "ブガンダの太鼓奏者・守護者たち", "王宮と王墓の儀礼で太鼓を用い、王統、共同体、場所の記憶を世代間で伝える演奏伝統。", "buganda"),
            W("gurbani_kirtan", "グルバニ・キールタン", MasterpieceKind.Music, "西・南アジア", "15世紀以降", "シク教の詩人・ラーギー・共同体", "聖典の賛歌をラーガに基づいて歌い、礼拝と共同体の学びの中で継承する音楽実践。", ""),
            W("saman_gayo", "ガヨのサマン", MasterpieceKind.Music, "東・東南アジア", "長く継承される生きた伝統", "ガヨの共同体", "横一列に座る演者が歌、手拍子、胸や腿を打つ動きを緊密に重ねる共同表現。", ""),
            W("vivaldi_four_seasons", "協奏曲集『四季』", MasterpieceKind.Music, "ヨーロッパ・地中海", "1725年刊", "アントニオ・ヴィヴァルディ", "四つのヴァイオリン協奏曲で季節の自然と人の営みを描写し、詩と器楽表現を結んだ。", "venice"),
            W("cherokee_syllabary_hymns", "チェロキー音節文字の聖歌と歌唱", MasterpieceKind.Music, "アメリカ大陸", "19世紀以降", "チェロキーの翻訳者・歌い手・共同体", "音節文字で記された聖歌を共同歌唱へ結び、言語、信仰、楽譜に依存しない旋律記憶を伝える。", "cherokee"),
            W("fijian_meke", "フィジーのメケ", MasterpieceKind.Music, "オセアニア", "長く継承される生きた伝統", "フィジーの作詞者・歌い手・踊り手・共同体", "詠唱、歌、打楽、身振りを一体化し、歴史、系譜、出来事を共同体ごとの形で表現する。", "fiji"),

            // 演劇（各地域1件）
            W("the_burdens_ruganda", "重荷", MasterpieceKind.Theater, "アフリカ", "1972年", "ジョン・ルガンダ", "失脚した政治家と家族の生活を通じ、独立後社会の権力、期待、崩壊を家庭劇にした。", ""),
            W("sariputraprakarana", "シャーリプトラ・プラカラナ", MasterpieceKind.Theater, "西・南アジア", "1～2世紀ごろ", "馬鳴（アシュヴァゴーシャ）に帰される", "断片写本で伝わる初期サンスクリット仏教劇。帰属と全体像には研究上の不確実性が残る。", "kushan", "ashvaghosha"),
            W("talchum_mask_dance_drama", "仮面舞踊劇タルチュム", MasterpieceKind.Theater, "東・東南アジア", "地域ごとに長く継承", "朝鮮半島各地の演者・共同体", "仮面、踊り、歌、台詞、風刺を組み合わせ、社会的緊張を観客との交流の中で演じる。", ""),
            W("servant_of_two_masters", "二人の主人を一度に持つと", MasterpieceKind.Theater, "ヨーロッパ・地中海", "1745年", "カルロ・ゴルドーニ", "一人の召使いが二人の主人に仕えることで生じる混乱を、仮面喜劇の型と人物描写で展開した。", "venice"),
            W("unto_these_hills", "アントゥ・ジーズ・ヒルズ", MasterpieceKind.Theater, "アメリカ大陸", "1950年初演・継続上演", "チェロキー歴史協会と歴代の創作者・出演者", "東部チェロキーの歴史を、欧州勢力との接触から強制移住と共同体の存続まで野外劇で語る。", "cherokee"),
            W("last_virgin_in_paradise", "楽園最後の処女", MasterpieceKind.Theater, "オセアニア", "1991年初演", "ヴィルソニ・ヘレニコ、テレシア・テアイワ", "架空の太平洋島を舞台に、外部が作る楽園像、ジェンダー、家族と主体性を風刺する喜劇。", "fiji"),

            // 映画（各地域1件）
            W("heritage_africa_film", "ヘリテージ・アフリカ", MasterpieceKind.Film, "アフリカ", "1989年", "クワウ・アンサーと製作陣", "植民地期の官吏を中心に、同化、名前、家族、文化的帰属をめぐる葛藤を描く。", ""),
            W("nanak_nam_jahaz_hai_film", "ナーナク・ナーム・ジャハーズ・ハイ", MasterpieceKind.Film, "西・南アジア", "1969年", "ラム・マヘシュワリと製作陣", "グル・ナーナクへの信仰と家族の再生を軸にしたパンジャーブ語映画。", ""),
            W("tjoet_nja_dhien_film", "チュッ・ニャ・ディン", MasterpieceKind.Film, "東・東南アジア", "1988年", "エロス・ジャロットと製作陣", "アチェ戦争の抵抗指導者チュッ・ニャ・ディンを、戦争、老い、信念の物語として描く。", "aceh"),
            W("mephisto_film", "メフィスト", MasterpieceKind.Film, "ヨーロッパ・地中海", "1981年", "サボー・イシュトヴァーンと製作陣", "俳優の成功と権力への迎合を、ナチ体制下の舞台とファウスト的な選択に重ねる。", "hungary"),
            W("cherokee_word_for_water_film", "チェロキー・ワード・フォー・ウォーター", MasterpieceKind.Film, "アメリカ大陸", "2013年", "チャーリー・ソープ、ティム・ケリーと製作陣", "ベル共同体の水道建設を通じ、協働の原理ガドゥギとウィルマ・マンキラーの歩みを描く。", "cherokee"),
            W("land_has_eyes_film", "大地には眼がある", MasterpieceKind.Film, "オセアニア", "2004年", "ヴィルソニ・ヘレニコと製作陣", "ロトゥマ島を舞台に、少女が家族の汚名と植民地的な権力関係に向き合う物語。", "fiji"),

            // ==================================================================
            // 第4弾（6地域×7分野＝42件）。既存294件の順序を保つ後方追加。
            // ==================================================================

            // 書籍（各地域1件）
            W("book_bornu_wars", "ボルヌ戦役記", MasterpieceKind.Book, "アフリカ", "1576年", "アフマド・イブン・フルトゥ", "イドリース・アローマ期の遠征を宮廷学者がアラビア語で記録した戦役史。", "kanem_bornu", "ahmad_ibn_furtu"),
            W("karnamag_ardashir", "アルダシール1世の事績の書", MasterpieceKind.Book, "西・南アジア", "サーサーン朝期に成立・中世写本で伝存", "中期ペルシア語の編者（名不詳）", "アルダシールの台頭を歴史、伝説、王権観を交えて語る中期ペルシア語散文。", "sasanian"),
            W("sejarah_melayu", "スジャラ・ムラユ（マレー年代記）", MasterpieceKind.Book, "東・東南アジア", "15～17世紀に編纂・伝写", "マレー宮廷の編者・写本伝承", "マラッカを中心に王統、外交、戦争、宮廷規範を伝える年代記。成立層と写本系統は一つではない。", "malacca"),
            W("tale_bygone_years", "過ぎし年月の物語", MasterpieceKind.Book, "ヨーロッパ・地中海", "12世紀初頭に編纂", "キーウの修道院編者たち（ネストル帰属の伝承）", "ルーシ諸国の起源、統治、改宗を複数の年代記資料と伝承から編んだ。単独著者説には議論がある。", "kyivan_rus", "nestor_chronicler"),
            W("black_elk_speaks", "ブラック・エルクは語る", MasterpieceKind.Book, "アメリカ大陸", "1932年", "ブラック・エルク、ジョン・G・ナイハート、通訳・編集協力者たち", "オグララ・ラコタのブラック・エルクの語りを聞き取り、通訳、編集を経て刊行した記録。単独の声そのものとは扱わない。", "lakota", "black_elk"),
            W("ancient_tahiti", "古代タヒチ", MasterpieceKind.Book, "オセアニア", "1928年刊", "テウイラ・ヘンリー（J・M・オーモンドの収集記録に基づく）", "タヒチの系譜、歌、物語、祭儀を、19世紀の聞き取り資料から整理した記録集。", "tahiti"),

            // 絵画・染織・記録図像（各地域1件）
            W("merina_lamba_akotifahana", "メリナのランバ・アコティファハナ", MasterpieceKind.Painting, "アフリカ", "19世紀中葉～後半", "メリナの女性織工たち", "絹の地へ補助緯糸で幾何学文様を織り出した肩掛け。宮廷社会の服飾、交易、女性の高度な染織技術を伝える。", "merina"),
            W("akbar_hawai_akbarnama", "アクバル、象ハワーイーを駆る", MasterpieceKind.Painting, "西・南アジア", "1586～1589年ごろ", "バサーワン（構図）、チェータルらムガル工房", "制御困難な象を駆る皇帝を、舟橋を横切る動勢と群衆の反応で描いた『アクバル・ナーマ』挿絵。", "mughal"),
            W("cheonmado_heavenly_horse", "天馬図（慶州・天馬塚）", MasterpieceKind.Painting, "東・東南アジア", "5～6世紀", "新羅の工芸職人たち", "白い馬状の霊獣を樺皮製の馬具へ描いた墓葬品。脆い有機素材に新羅の色彩と葬送表象を残す。", "silla"),
            W("saint_vincent_panels", "サン・ヴィセンテの祭壇画", MasterpieceKind.Painting, "ヨーロッパ・地中海", "1470年ごろ", "ヌーノ・ゴンサルヴェスに帰される", "六枚の板に多数の人物を配したポルトガル絵画。人物同定や本来の配置には複数の学説がある。", "portugal"),
            W("lone_dog_winter_count", "ローン・ドッグの冬数え", MasterpieceKind.Painting, "アメリカ大陸", "1800～1871年を記録", "ローン・ドッグとラコタの歴史保持者たち", "各冬を一つの記号で表し、共同体が年を記憶し語り直すために用いた図像記録。現存資料には後世の複製もある。", "lakota"),
            W("two_tahitian_women", "二人のタヒチ女性", MasterpieceKind.Painting, "オセアニア", "1899年", "ポール・ゴーギャン", "二人の女性を近接して配した油彩。植民地期の外来画家が構成した『タヒチ』像と視線も含めて読む必要がある。", ""),

            // 彫刻（各地域1件）
            W("kuba_ndop_1760", "クバ王を表すンドップ像", MasterpieceKind.Sculpture, "アフリカ", "1760～1780年ごろ", "クバ（ブショング）の彫刻家", "王権の装束と落ち着いた姿を木に表す宮廷肖像。複数の王への同定候補があり、個人名は確定していない。", ""),
            W("colossal_shapur_i", "シャープール1世巨像", MasterpieceKind.Sculpture, "西・南アジア", "3世紀", "サーサーン朝の石工たち", "洞窟内の石灰岩柱を彫り残して王の立像とした巨大彫刻。欠損を受けつつサーサーン朝王権の造形を伝える。", "sasanian"),
            W("seokguram_buddha", "石窟庵の本尊仏", MasterpieceKind.Sculpture, "東・東南アジア", "8世紀", "新羅の石工・建築職人たち", "人工石窟の円形主室に花崗岩の仏坐像と諸像を組み、彫刻、建築、礼拝空間を一体化した。", "silla"),
            W("tomb_ines_de_castro", "イネス・デ・カストロの墓廟彫刻", MasterpieceKind.Sculpture, "ヨーロッパ・地中海", "14世紀", "ポルトガルのゴシック彫刻工房", "横臥像、天蓋、宗教・王権図像を石棺に展開したアルコバサ修道院の墓廟。作者個人は確定していない。", "portugal"),
            W("raven_first_men", "ワタリガラスと最初の人々", MasterpieceKind.Sculpture, "アメリカ大陸", "1980年", "ビル・リードと制作協力者たち", "ハイダの物語をもとに、巨大なワタリガラスと貝から現れる人々を黄色杉へ彫った群像。", ""),
            W("rarotonga_staff_god", "ラロトンガの杖形神像", MasterpieceKind.Sculpture, "オセアニア", "18世紀末～19世紀初頭", "ラロトンガの彫刻家たち", "長い杖状の木彫へ人像要素と巻かれた樹皮布を組み合わせた祭祀具。固有の意味を外部解釈だけで断定しない。", "rarotonga"),

            // 建築（各地域1件）
            W("manjakamiadana_palace", "マンジャカミアダナ宮殿", MasterpieceKind.Architecture, "アフリカ", "1839～1841年創建・19世紀後半石造化", "メリナ王宮の職人、ジャン・ラボルド、ジェームズ・キャメロン", "アンタナナリボの王宮中心建築。木造宮殿として建てられ、後に石の外郭が加えられた複層的な建築史を持つ。", "merina"),
            W("taq_kasra", "ターク・カスラー（クテシフォンの大イーワーン）", MasterpieceKind.Architecture, "西・南アジア", "3～6世紀", "サーサーン朝の建築家・煉瓦職人たち", "巨大な煉瓦造ヴォールトと開放的な大広間を持つ王宮遺構。正確な建立王と年代には議論がある。", "sasanian"),
            W("bunhwangsa_pagoda", "芬皇寺模塼石塔", MasterpieceKind.Architecture, "東・東南アジア", "634年創建", "新羅の石工・仏教共同体", "煉瓦に似せて加工した安山岩を積む石塔。現存層は縮小・修復を経ており、創建時の全高は確定していない。", "silla"),
            W("belem_tower", "ベレンの塔", MasterpieceKind.Architecture, "ヨーロッパ・地中海", "1514～1520年ごろ", "フランシスコ・デ・アルーダと建築職人たち", "テージョ川河口の防衛施設を、稜堡、塔、航海時代の装飾でまとめたマヌエル様式建築。", "portugal"),
            W("powhatan_yehakin", "ポウハタンのイェハキン", MasterpieceKind.Architecture, "アメリカ大陸", "16～17世紀に記録・継承", "ポウハタン諸共同体の建築者たち", "曲げた若木の骨組みを樹皮や編みマットで覆う住居建築。規模と形は用途や季節に応じ、女性が建設と家の管理を担った。", "powhatan"),
            W("para_o_tane_palace", "パラ・オ・タネ宮殿（マケア宮殿）", MasterpieceKind.Architecture, "オセアニア", "19世紀（1830年代以降）", "マケア王統とラロトンガの建築職人たち", "アヴァルアの首長家宮殿。マケア・タカウの居所となり、島の統治と植民地期外交の舞台を伝える。", "rarotonga"),

            // 音楽（各地域1件）
            W("hiragasy_music", "ヒラガシ", MasterpieceKind.Music, "アフリカ", "18世紀以降に発展・現代も継承", "マダガスカル中央高地の一座と共同体", "歌、器楽、踊り、演説を一日の上演へ組み、社会的対話と祝祭を担う生きた芸能。", "merina"),
            W("qawwali_qaul", "カウワーリーのカウル", MasterpieceKind.Music, "西・南アジア", "13世紀以降の伝承", "南アジアのスーフィー音楽家たち（アミール・ホスロー伝承）", "詩句を独唱、合唱、手拍子、反復で展開するスーフィー音楽。創始をアミール・ホスローに結ぶ説明は伝承として扱う。", "delhi_sultanate", "amir_khusrau"),
            W("cheoyongga_silla", "郷歌『処容歌』", MasterpieceKind.Music, "東・東南アジア", "9世紀末の伝承", "新羅の歌い手たち（処容説話）", "疫病を退ける処容の物語と結び付く郷歌。歌詞は後世の史書に伝わるが、当時の旋律は残っていない。", "silla"),
            W("portuguese_fado", "ポルトガルのファド", MasterpieceKind.Music, "ヨーロッパ・地中海", "19世紀以降", "リスボンなどの歌手・奏者・共同体", "声とギターを中心に、運命、都市生活、喪失や郷愁を多様なレパートリーで歌い継ぐ都市音楽。", "portugal"),
            W("maple_leaf_rag", "メープル・リーフ・ラグ", MasterpieceKind.Music, "アメリカ大陸", "1899年刊", "スコット・ジョプリン", "シンコペーションを精密に組んだピアノ曲で、出版楽譜を通じてクラシック・ラグの代表作となった。", ""),
            W("te_atua_mou_e", "テ・アトゥア・モウ・エ", MasterpieceKind.Music, "オセアニア", "1980年代に国歌化", "パ・テパエル・テリト・アリキ（詞）、トム・デイヴィス（曲）", "クック諸島の現行国歌。現代の国家的作品であり、古代ラロトンガの歌と同一視しない。", "rarotonga"),

            // 演劇（各地域1件）
            W("marriage_anansewa", "アナンセワの結婚", MasterpieceKind.Theater, "アフリカ", "1975年", "エフア・サザーランド", "アナンセの語りを舞台形式アナンセゴロへ展開し、結婚、家族、交渉、近代社会を音楽と語りで描く。", ""),
            W("indar_sabha", "インドラの宮廷", MasterpieceKind.Theater, "西・南アジア", "1853年初演", "アーガー・ハサン・アマーナト", "妖精と王子の恋を歌、詩、舞踊で展開したウルドゥー語の音楽劇。ラクナウ宮廷文化の中で成立した。", ""),
            W("mak_yong_theatre", "マッ・ヨン劇", MasterpieceKind.Theater, "東・東南アジア", "長く継承される生きた伝統", "マレー半島の演者・音楽家・共同体", "演技、即興的な台詞、歌、器楽、身振り、衣装を統合する伝統劇。地域と上演状況ごとの差異を含む。", ""),
            W("auto_barca_inferno", "地獄の舟", MasterpieceKind.Theater, "ヨーロッパ・地中海", "1517年初演", "ジル・ヴィセンテ", "死者たちが天国と地獄の舟の前で裁かれる寓意劇。身分と職業を横断して社会と道徳を風刺する。", "portugal"),
            W("dry_lips_kapuskasing", "ドライ・リップスはカプスケーシングへ行くべきだ", MasterpieceKind.Theater, "アメリカ大陸", "1989年初演", "トムソン・ハイウェイ", "架空の先住民居留地を舞台に、男性たち、女子ホッケーチーム、暴力、信仰、共同体の再生を悲喜劇として描く。", ""),
            W("i_tai_henri_hiro", "イ・タイ", MasterpieceKind.Theater, "オセアニア", "1976年初演", "アンリ・ヒロと上演協力者たち", "レオ・タヒチで創作・上演され、植民地化後の社会と価値観を批判的に問い直した舞台作品。", "tahiti", "henri_hiro"),

            // 映画（各地域1件）
            W("tabataba_film", "タバタバ", MasterpieceKind.Film, "アフリカ", "1988年", "レイモン・ラジャオナリヴェロと製作陣", "1947年のマダガスカル蜂起を、村の人々の選択、植民地支配、世代間の緊張から描く。", ""),
            W("chess_players_film", "チェスをする人々", MasterpieceKind.Film, "西・南アジア", "1977年", "サタジット・レイと製作陣", "1856年のアワド併合を背景に、チェスへ没頭する二人の地主と植民地政治を並行して描く。", ""),
            W("hang_tuah_film", "ハン・トゥア", MasterpieceKind.Film, "東・東南アジア", "1956年", "ファニ・マジュムダール、P・ラムリーと製作陣", "マラッカの伝説的武人を、忠誠、友情、宮廷政治の葛藤として映画化したマレー語作品。", "malacca"),
            W("aniki_bobo_film", "アニキ・ボボ", MasterpieceKind.Film, "ヨーロッパ・地中海", "1942年", "マノエル・ド・オリヴェイラと製作陣", "ポルトの子どもたちの友情、恋、罪悪感を街路と河岸の風景の中で描いた長編劇映画。", "portugal"),
            W("daughter_dawn_film", "ドーター・オブ・ドーン", MasterpieceKind.Film, "アメリカ大陸", "1920年", "ノーバート・A・マイルズ、カイオワ・コマンチの出演者と製作陣", "カイオワとコマンチの出演者を中心に制作された無声劇映画。保存と復元の歴史も作品の受容を形作る。", ""),
            W("mauri_film", "マウリ", MasterpieceKind.Film, "オセアニア", "1988年", "メラタ・ミタと製作陣", "農村共同体の秘密、土地、家族、マウリを、英語とマオリ語を交えて描いた長編劇映画。", "maori"),
        };

        public static IReadOnlyList<MasterpieceDef> All => Definitions;

        public static MasterpieceDef Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < Definitions.Length; i++)
                if (string.Equals(Definitions[i].Id, id, StringComparison.OrdinalIgnoreCase))
                    return Definitions[i];
            return null;
        }

        public static List<MasterpieceDef> ForRegion(string regionJa)
        {
            var result = new List<MasterpieceDef>();
            for (int i = 0; i < Definitions.Length; i++)
                if (string.IsNullOrEmpty(regionJa) ||
                    string.Equals(Definitions[i].RegionJa, regionJa, StringComparison.Ordinal))
                    result.Add(Definitions[i]);
            return result;
        }

        public static List<MasterpieceDef> ForKind(MasterpieceKind kind)
        {
            var result = new List<MasterpieceDef>();
            for (int i = 0; i < Definitions.Length; i++)
                if (Definitions[i].Kind == kind) result.Add(Definitions[i]);
            return result;
        }

        static MasterpieceDef W(string id, string nameJa, MasterpieceKind kind, string regionJa,
            string periodJa, string creatorJa, string summaryJa, string civilizationId,
            string personId = "")
        {
            return new MasterpieceDef(id, nameJa, kind, regionJa, periodJa, creatorJa,
                summaryJa, civilizationId, personId);
        }
    }
}
