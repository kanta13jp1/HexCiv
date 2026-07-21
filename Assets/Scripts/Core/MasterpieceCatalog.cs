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
