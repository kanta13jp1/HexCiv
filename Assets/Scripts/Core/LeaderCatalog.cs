using System;
using System.Collections.Generic;

namespace HexCiv.Core
{
    /// <summary>
    /// 文明に所属する実在の統治者・政治指導者。現段階では能力差を持たず、
    /// 表示、ゲーム開始時の選択、セーブ/ロードの安定した識別子として使う。
    /// </summary>
    public sealed class LeaderDef
    {
        public readonly string Id;
        public readonly string CivilizationId;
        public readonly string NameJa;
        public readonly string TitleJa;
        public readonly string PeriodJa;
        public readonly string SummaryJa;
        public readonly bool NameKnown;

        public LeaderDef(string id, string civilizationId, string nameJa, string titleJa,
            string periodJa, string summaryJa, bool nameKnown)
        {
            Id = id;
            CivilizationId = civilizationId;
            NameJa = nameJa;
            TitleJa = titleJa;
            PeriodJa = periodJa;
            SummaryJa = summaryJa;
            NameKnown = nameKnown;
        }
    }

    /// <summary>
    /// 世界史指導者台帳のプレイアブル第1・第2弾。
    /// 個人名を史料から確定できない文明には人物を創作せず、明示的な「名は未詳」を置く。
    /// </summary>
    public static class LeaderCatalog
    {
        static readonly LeaderDef[] Definitions =
        {
            // ---- 地中海・西アジア ----
            Leader("pericles", "athens", "ペリクレス", "将軍・政治指導者", "紀元前5世紀", "アテネ民主政の最盛期を導いた。"),
            Leader("themistocles", "athens", "テミストクレス", "将軍・政治指導者", "紀元前6～5世紀", "海軍を整備し、ペルシア戦争期のアテネを率いた。"),
            Leader("augustus", "rome", "アウグストゥス", "初代ローマ皇帝", "紀元前1～紀元1世紀", "内戦後の統治体制を整え、帝政の基礎を築いた。"),
            Leader("trajan", "rome", "トラヤヌス", "ローマ皇帝", "1～2世紀", "ローマ帝国最大版図期の皇帝。"),
            Leader("hatshepsut", "egypt", "ハトシェプスト", "ファラオ", "紀元前15世紀", "交易と大規模建築を進めた第18王朝の統治者。"),
            Leader("ramesses_ii", "egypt", "ラムセス2世", "ファラオ", "紀元前13世紀", "長期統治と建築事業で知られる第19王朝の王。"),
            Leader("hammurabi", "babylon", "ハンムラビ", "バビロン王", "紀元前18世紀", "メソポタミアを広く統合し、法典で知られる。"),
            Leader("nebuchadnezzar_ii", "babylon", "ネブカドネザル2世", "新バビロニア王", "紀元前7～6世紀", "新バビロニアの版図拡大と首都整備を進めた。"),
            Leader("ur_nammu", "sumer", "ウル・ナンム", "ウル王", "紀元前21世紀", "ウル第3王朝を創始し、法典を残した。"),
            Leader("gudea", "sumer", "グデア", "ラガシュの統治者", "紀元前22世紀", "神殿建設と多数の像・碑文で知られる。"),
            Leader("sargon_akkad", "akkad", "サルゴン", "アッカド王", "紀元前24～23世紀", "アッカド帝国を築いたと伝わる王。"),
            Leader("naram_sin", "akkad", "ナラム・シン", "アッカド王", "紀元前23世紀", "アッカドの勢力を拡大したサルゴンの孫。"),
            Leader("tiglat_pileser_iii", "assyria", "ティグラト・ピレセル3世", "アッシリア王", "紀元前8世紀", "軍制と地方統治を改革した新アッシリア王。"),
            Leader("ashurbanipal", "assyria", "アッシュルバニパル", "アッシリア王", "紀元前7世紀", "大図書館を形成した新アッシリア王。"),
            Leader("suppiluliuma_i", "hittite", "シュッピルリウマ1世", "ヒッタイト大王", "紀元前14世紀", "ヒッタイトの勢力を大きく拡張した。"),
            Leader("hattusili_iii", "hittite", "ハットゥシリ3世", "ヒッタイト大王", "紀元前13世紀", "エジプトとの条約で知られる。"),
            Leader("hiram_i", "phoenicia", "ヒラム1世", "ティルス王", "紀元前10世紀", "ティルスの建設・交易活動を推進した王。"),
            Leader("elissa_dido", "phoenicia", "エリッサ（ディードー）", "カルタゴ建設者（伝承）", "紀元前9世紀の伝承", "ティルス王家出身とされる伝承上の建設者。"),
            Leader("cyrus_ii", "persia", "キュロス2世", "アケメネス朝の王", "紀元前6世紀", "広大なアケメネス朝帝国を築いた。"),
            Leader("darius_i", "persia", "ダレイオス1世", "諸王の王", "紀元前6～5世紀", "行政区・道路・貨幣制度を整備した。"),

            // ---- アフリカ ----
            Leader("piye", "kush", "ピイ", "クシュ王・ファラオ", "紀元前8世紀", "エジプトを征服し第25王朝の基盤を築いた。"),
            Leader("amanirenas", "kush", "アマニレナス", "カンダケ", "紀元前1世紀", "ローマと戦ったメロエの女王。"),
            Leader("ezana", "aksum", "エザナ", "アクスム王", "4世紀", "領域を拡大し、キリスト教を受容した王。"),
            Leader("kaleb", "aksum", "カレブ", "アクスム王", "6世紀", "紅海を越えて南アラビアへ遠征した。"),
            Leader("hamilcar_barca", "carthage", "ハミルカル・バルカ", "将軍・政治指導者", "紀元前3世紀", "第一次ポエニ戦争後のカルタゴ再建を進めた。"),
            Leader("hannibal", "carthage", "ハンニバル", "将軍・政治指導者", "紀元前3～2世紀", "アルプスを越えてローマと戦った将軍。"),
            Leader("sundiata", "mali", "スンジャタ・ケイタ", "マンサ", "13世紀", "マリ帝国の創始者として伝承される。"),
            Leader("mansa_musa", "mali", "マンサ・ムーサ", "マンサ", "14世紀", "巡礼と富、学芸保護で知られるマリ皇帝。"),
            Leader("sunni_ali", "songhai", "スンニ・アリ", "ソンガイ王", "15世紀", "ソンガイを西アフリカの大国へ成長させた。"),
            Leader("askia_muhammad", "songhai", "アスキア・ムハンマド", "アスキア", "15～16世紀", "行政と交易を整えたソンガイの統治者。"),
            Unknown("great_zimbabwe_unknown", "great_zimbabwe", "グレート・ジンバブエの統治者", "11～15世紀", "現時点の史料では代表的統治者の個人名を確定できない。"),
            Leader("afonso_i_kongo", "kongo", "アフォンソ1世", "マニコンゴ", "15～16世紀", "キリスト教化と王国統治の再編を進めた。"),
            Leader("garcia_ii_kongo", "kongo", "ガルシア2世", "マニコンゴ", "17世紀", "対外関係と王権強化に取り組んだ。"),
            Leader("shaka", "zulu", "シャカ", "ズールー王", "19世紀", "軍制を再編しズールー王国を拡大した。"),
            Leader("cetshwayo", "zulu", "セテワヨ", "ズールー王", "19世紀", "ズールー戦争期の国王。"),

            // ---- 南アジア ----
            Unknown("indus_unknown", "indus", "インダス文明の統治者", "紀元前3～2千年紀", "文字が未解読のため、統治者の個人名を確定できない。"),
            Leader("chandragupta_maurya", "maurya", "チャンドラグプタ", "マウリヤ朝初代王", "紀元前4～3世紀", "マウリヤ朝を創始し北インドを統合した。"),
            Leader("ashoka", "maurya", "アショーカ", "マウリヤ朝の王", "紀元前3世紀", "石柱・磨崖詔勅と仏教保護で知られる。"),
            Leader("samudragupta", "gupta", "サムドラグプタ", "グプタ朝の王", "4世紀", "軍事遠征で王朝の勢力を広げた。"),
            Leader("chandragupta_ii", "gupta", "チャンドラグプタ2世", "グプタ朝の王", "4～5世紀", "交易と文化が栄えた時期を統治した。"),
            Leader("rajaraja_i", "chola", "ラージャラージャ1世", "チョーラ王", "10～11世紀", "南インドと海上へ勢力を拡大した。"),
            Leader("rajendra_i", "chola", "ラージェーンドラ1世", "チョーラ王", "11世紀", "海軍遠征と新都建設で知られる。"),
            Leader("deva_raya_ii", "vijayanagara", "デーヴァ・ラーヤ2世", "ヴィジャヤナガル王", "15世紀", "王国の軍事・文化を発展させた。"),
            Leader("krishnadevaraya", "vijayanagara", "クリシュナ・デーヴァ・ラーヤ", "ヴィジャヤナガル王", "16世紀", "領土拡大と文学保護で知られる。"),
            Leader("akbar", "mughal", "アクバル", "ムガル皇帝", "16世紀", "帝国を拡大し、多宗教を含む統治制度を整えた。"),
            Leader("shah_jahan", "mughal", "シャー・ジャハーン", "ムガル皇帝", "17世紀", "タージ・マハルなどの建築事業で知られる。"),

            // ---- 東・中央アジア ----
            Leader("wu_ding", "shang", "武丁", "殷王", "紀元前13～12世紀", "甲骨文が豊富に残る殷後期の王。"),
            Leader("fu_hao", "shang", "婦好", "王妃・軍事指導者", "紀元前13世紀", "武丁の王妃で、軍を率いたことが甲骨文に記される。"),
            Leader("gaozo_han", "han", "高祖（劉邦）", "前漢初代皇帝", "紀元前3世紀", "漢王朝を創始した。"),
            Leader("emperor_wu_han", "han", "武帝", "前漢皇帝", "紀元前2～1世紀", "帝国の領域と中央集権を拡大した。"),
            Leader("taizong_tang", "tang", "太宗（李世民）", "唐皇帝", "7世紀", "唐初の統治体制を安定させた。"),
            Leader("wu_zetian", "tang", "武則天", "皇帝", "7～8世紀", "中国史上唯一、皇帝を正式称号とした女性。"),
            Leader("hongwu", "ming", "洪武帝", "明初代皇帝", "14世紀", "明を建国し統治制度を整備した。"),
            Leader("yongle", "ming", "永楽帝", "明皇帝", "14～15世紀", "北京遷都と大規模な対外事業を進めた。"),
            Leader("tokugawa_ieyasu", "japan", "徳川家康", "征夷大将軍", "16～17世紀", "江戸幕府を開き、長期的な政治秩序の基礎を築いた。"),
            Leader("emperor_meiji", "japan", "明治天皇", "天皇", "19～20世紀", "近代国家形成の時代に在位した。"),
            Leader("gwanggaeto", "goguryeo", "広開土王", "高句麗王", "4～5世紀", "高句麗の領域を大きく拡大した。"),
            Leader("jangsu", "goguryeo", "長寿王", "高句麗王", "5世紀", "平壌へ遷都し南方へ勢力を広げた。"),
            Leader("taejo_joseon", "joseon", "太祖（李成桂）", "朝鮮王", "14世紀", "朝鮮王朝を創始した。"),
            Leader("sejong", "joseon", "世宗", "朝鮮王", "15世紀", "訓民正音の創製と学術振興で知られる。"),
            Leader("genghis_khan", "mongol", "チンギス・ハン", "大ハーン", "12～13世紀", "モンゴル諸部を統合し帝国を創始した。"),
            Leader("kublai_khan", "mongol", "クビライ", "大ハーン・元皇帝", "13世紀", "元を建て中国全土を統治した。"),

            // ---- 東南アジア ----
            Leader("suryavarman_ii", "khmer", "スーリヤヴァルマン2世", "クメール王", "12世紀", "アンコール・ワットを造営した王。"),
            Leader("jayavarman_vii", "khmer", "ジャヤヴァルマン7世", "クメール王", "12～13世紀", "アンコール・トムなど大規模建築を進めた。"),
            Leader("dapunta_hyang", "srivijaya", "ダプンタ・ヒャン・スリ・ジャヤナサ", "シュリーヴィジャヤの統治者", "7世紀", "碑文に遠征と建国活動が記される。"),
            Leader("balaputradewa", "srivijaya", "バラプトラデーワ", "シュリーヴィジャヤ王", "9世紀", "ナーランダーへの寄進で知られる王。"),
            Leader("tribhuwana", "majapahit", "トリブワナ・ウィジャヤトゥンガデウィ", "マジャパヒト女王", "14世紀", "王国拡大期を統治した。"),
            Leader("hayam_wuruk", "majapahit", "ハヤム・ウルク", "マジャパヒト王", "14世紀", "マジャパヒト最盛期の王。"),
            Leader("anawrahta", "pagan", "アノーヤター", "パガン王", "11世紀", "上座部仏教を保護し王国を統合した。"),
            Leader("kyansittha", "pagan", "チャンシッター", "パガン王", "11～12世紀", "王国の安定と寺院建設を進めた。"),
            Leader("trailokkanat", "ayutthaya", "ボーロマトライローカナート", "アユタヤ王", "15世紀", "行政と官位制度の整備で知られる。"),
            Leader("naresuan", "ayutthaya", "ナレースワン", "アユタヤ王", "16世紀", "独立回復と軍事活動で知られる。"),
            Leader("ly_thai_to", "dai_viet", "李太祖（李公蘊）", "大越皇帝", "11世紀", "李朝を開き昇龍へ遷都した。"),
            Leader("le_loi", "dai_viet", "黎利", "後黎朝初代皇帝", "15世紀", "明からの独立を回復し後黎朝を開いた。"),

            // ---- ヨーロッパ ----
            Leader("justinian_i", "byzantium", "ユスティニアヌス1世", "東ローマ皇帝", "6世紀", "法典編纂と領土回復、建築事業を進めた。"),
            Leader("basil_ii", "byzantium", "バシレイオス2世", "東ローマ皇帝", "10～11世紀", "長期統治で帝国の軍事力を強化した。"),
            Leader("clovis_i", "franks", "クローヴィス1世", "フランク王", "5～6世紀", "フランク諸勢力を統合しカトリックへ改宗した。"),
            Leader("charlemagne", "franks", "カール大帝", "フランク王・皇帝", "8～9世紀", "西・中央ヨーロッパに広い帝国を築いた。"),
            Leader("harald_bluetooth", "norse", "ハーラル1世（青歯王）", "デンマーク王", "10世紀", "デンマーク統合とキリスト教化で知られる。"),
            Leader("harald_hardrada", "norse", "ハーラル3世（苛烈王）", "ノルウェー王", "11世紀", "各地で戦ったノルウェー王。"),
            Leader("alfred_great", "england", "アルフレッド大王", "ウェセックス王", "9世紀", "ヴァイキングに抵抗し統治と学問を振興した。"),
            Leader("elizabeth_i", "england", "エリザベス1世", "イングランド女王", "16～17世紀", "宗教対立を調整し海洋進出期を統治した。"),
            Leader("louis_ix", "france", "ルイ9世", "フランス王", "13世紀", "司法改革と王権強化を進めた。"),
            Leader("louis_xiv", "france", "ルイ14世", "フランス王", "17～18世紀", "長期統治とヴェルサイユ宮廷で知られる。"),
            Leader("isabella_i", "spain", "イサベル1世", "カスティーリャ女王", "15～16世紀", "アラゴン王フェルナンド2世と共同統治した。"),
            Leader("charles_v", "spain", "カルロス1世（カール5世）", "スペイン王・神聖ローマ皇帝", "16世紀", "ヨーロッパとアメリカにまたがる領域を統治した。"),
            Leader("mehmed_ii", "ottoman", "メフメト2世", "オスマン皇帝", "15世紀", "コンスタンティノープルを征服した。"),
            Leader("suleiman_i", "ottoman", "スレイマン1世", "オスマン皇帝", "16世紀", "帝国拡大と法制度整備で知られる。"),

            // ---- アメリカ大陸 ----
            Leader("pakal", "maya", "キニチ・ハナーブ・パカル", "パレンケ王", "7世紀", "パレンケの建築と碑文で知られる王。"),
            Leader("lady_six_sky", "maya", "ワク・チャニル・アハウ（六の空の女王）", "ナランホの統治者", "7～8世紀", "ナランホの王統を再興した女性統治者。"),
            Leader("itzcoatl", "aztec", "イツコアトル", "トラトアニ", "15世紀", "三都市同盟形成期のテノチティトラン君主。"),
            Leader("moteuczoma_i", "aztec", "モテクソマ1世", "トラトアニ", "15世紀", "アステカの勢力と制度を拡大した。"),
            Leader("pachacuti", "inca", "パチャクティ", "サパ・インカ", "15世紀", "クスコ王国を大帝国へ発展させた。"),
            Leader("huayna_capac", "inca", "ワイナ・カパック", "サパ・インカ", "15～16世紀", "インカ帝国最大版図期を統治した。"),
            Unknown("olmec_unknown", "olmec", "オルメカの統治者", "紀元前2～1千年紀", "解読可能な王名史料がなく、代表的統治者の個人名を確定できない。"),
            Unknown("lord_of_sipan", "moche", "シパン王", "3世紀ごろ", "豪華な墓で知られるが、本人の個人名は確定していない。", "シパンの統治者"),
            Unknown("lady_of_cao", "moche", "カオの貴婦人", "4世紀ごろ", "墓葬から高位の女性統治者と考えられるが、個人名は不明。", "モチェの女性統治者"),
            Unknown("tiwanaku_unknown", "tiwanaku", "ティワナクの統治者", "5～10世紀", "現時点の史料では代表的統治者の個人名を確定できない。"),
            Unknown("cahokia_unknown", "mississippian", "カホキアの統治者", "11～14世紀", "文字史料がなく、代表的統治者の個人名を確定できない。"),
            Leader("hiawatha", "haudenosaunee", "ハイアワサ", "連邦形成の指導者（口承）", "成立年代には諸説", "平和の大法と連邦形成を伝える口承の中心人物。"),
            Leader("jigonsaseh", "haudenosaunee", "ジゴンサセ", "連邦形成の指導者（口承）", "成立年代には諸説", "諸国の和平と連邦形成に関わったと伝えられる。"),
            Leader("lautaro", "mapuche", "ラウタロ", "トキ（軍事指導者）", "16世紀", "スペイン勢力に抵抗したマプチェの軍事指導者。"),
            Leader("caupolican", "mapuche", "カウポリカン", "トキ（軍事指導者）", "16世紀", "アラウコ戦争で抵抗を率いた。"),

            // ---- オセアニア ----
            Leader("kamehameha_i", "hawaii", "カメハメハ1世", "ハワイ王", "18～19世紀", "ハワイ諸島を統一し王国を築いた。"),
            Leader("liliuokalani", "hawaii", "リリウオカラニ", "ハワイ女王", "19世紀", "ハワイ王国最後の君主。"),
            Leader("te_rauparaha", "maori", "テ・ラウパラハ", "ランガティラ・軍事指導者", "18～19世紀", "ンガーティ・トアを率いた指導者。"),
            Leader("hongi_hika", "maori", "ホンギ・ヒカ", "ランガティラ・軍事指導者", "18～19世紀", "ンガープヒの軍事・交易指導者。"),

            // ---- 第2弾: 新規12文明の代表指導者24件 ----
            // アフリカ
            Leader("ewuare_i", "benin", "エウアレ1世", "オバ（王）", "15世紀", "王権を強化し、ベニンの領域拡大と首都の城壁整備を進めた。"),
            Leader("idia", "benin", "イディア", "イヨバ（王母）・政治指導者", "15～16世紀", "エシギエ王を支え、王母の地位と政治的権限を確立した。"),
            Leader("zara_yaqob_emperor", "ethiopia", "ザラ・ヤコブ", "エチオピア皇帝", "15世紀", "宗教政策と外交、写本・聖堂への王室保護を通じて帝国統治を強化した。"),
            Leader("menelik_ii", "ethiopia", "メネリク2世", "エチオピア皇帝", "19～20世紀", "諸地域を統合し、アディスアベバを中心に近代国家建設を進めた。"),

            // 西・南アジア
            Leader("al_mansur", "abbasid", "アル＝マンスール", "アッバース朝カリフ", "8世紀", "新都バグダードを建設し、アッバース朝統治の基礎を固めた。"),
            Leader("harun_al_rashid", "abbasid", "ハールーン・アッ＝ラシード", "アッバース朝カリフ", "8～9世紀", "広域帝国を統治し、宮廷文化と学芸保護で知られる。"),
            Leader("shivaji", "maratha", "シヴァージー", "チャトラパティ", "17世紀", "デカンで領域と要塞網を築き、マラーター国家の基礎を形成した。"),
            Leader("tarabai", "maratha", "ターラーバーイー", "摂政・政治軍事指導者", "17～18世紀", "ムガルとの戦争期に政務と軍事抵抗を指導し、コールハープル政権の形成に関わった。"),

            // 東・東南アジア
            Leader("sho_hashi", "ryukyu", "尚巴志", "琉球国王", "14～15世紀", "沖縄本島の三山を統一し、琉球王国を成立させた。"),
            Leader("sho_shin", "ryukyu", "尚真", "琉球国王", "15～16世紀", "地方領主の首里集住などを通じて王国の中央集権化を進めた。"),
            Leader("tabinshwehti", "toungoo", "タビンシュウェティ", "タウングー王", "16世紀", "下ビルマへ勢力を広げ、分裂していた地域の再統合を進めた。"),
            Leader("bayinnaung", "toungoo", "バインナウン", "タウングー王", "16世紀", "広域遠征と統治によって東南アジア大陸部に大きな勢力圏を築いた。"),

            // ヨーロッパ・地中海
            Leader("otto_i_hre", "holy_roman", "オットー1世", "神聖ローマ皇帝", "10世紀", "王権を固め、962年の皇帝戴冠によって中欧の帝国秩序を形成した。"),
            Leader("frederick_ii_hre", "holy_roman", "フリードリヒ2世", "神聖ローマ皇帝", "12～13世紀", "シチリアとドイツを含む領域を統治し、地中海世界の外交と行政を展開した。"),
            Leader("jadwiga_poland", "polish_lithuanian", "ヤドヴィガ", "ポーランド国王", "14世紀", "ポーランド王として即位し、リトアニアとの連合形成を担った。"),
            Leader("wladyslaw_ii_jagiello", "polish_lithuanian", "ヴワディスワフ2世ヤギェウォ", "ポーランド国王・リトアニア大公", "14～15世紀", "改宗と婚姻を経てポーランド王となり、両国の連合を継続した。"),

            // アメリカ大陸
            Leader("nemequene", "muisca", "ネメケネ", "バカタのシパ", "15～16世紀", "バカタの勢力を広げ、統治規範を整えたと年代記に伝えられる。"),
            Leader("tisquesusa", "muisca", "ティスケスサ", "バカタのシパ", "16世紀", "ネメケネを継ぎ、ヨーロッパ勢力到来期のバカタを率いた。"),
            Leader("anacaona", "taino", "アナカオナ", "ハラグアのカシーカ", "15～16世紀", "ハラグアを統治し、カリブ海先住社会の政治的指導者として記録された。"),
            Leader("caonabo", "taino", "カオナボ", "マグアナのカシーケ", "15世紀", "イスパニョーラ島マグアナの首長として、初期植民地勢力への抵抗を率いた。"),

            // オセアニア
            Leader("george_tupou_i", "tonga", "ジョージ・トゥポウ1世", "トンガ国王", "19世紀", "諸島を統一し、1875年憲法の下で立憲王国の基礎を築いた。"),
            Leader("salote_tupou_iii", "tonga", "サローテ・トゥポウ3世", "トンガ女王", "20世紀", "長期在位の君主として統治し、トンガ語の詩歌と舞踊の継承にも貢献した。"),
            Leader("salamasina", "samoa", "サラマシナ", "タファイファ・最高位首長", "16世紀", "四つの最高位称号を兼ね、サモアの多くの首長系譜に結び付く指導者。"),
            Leader("malietoa_laupepa", "samoa", "マリエトア・ラウペパ", "マリエトア・王", "19世紀", "列強の介入と内戦が続く時代にサモアの王として承認され、統治を担った。"),

            // ---- 第3弾: 新規12文明の代表指導者24件 ----
            // アフリカ
            Leader("osei_tutu_i", "asante", "オセイ・トゥトゥ1世", "アシャンティヘネ", "17～18世紀", "クマシを中心とするアカン諸勢力の連合を築き、アシャンティ王国の基礎を固めた。"),
            Leader("yaa_asantewaa", "asante", "ヤァ・アサンテワァ", "エジスの王母・抵抗指導者", "19～20世紀", "1900年にイギリス植民地支配への抵抗を率いた政治・軍事指導者。"),
            Leader("mutesa_i_buganda", "buganda", "ムテサ1世", "カバカ", "19世紀", "交易と外交が変化する時代にブガンダを統治し、周辺諸国や海外勢力との関係を築いた。"),
            Leader("mwanga_ii_buganda", "buganda", "ムワンガ2世", "カバカ", "19～20世紀", "植民地勢力との関係が変動するなかで王権維持を試み、追放と復位を経験した。"),

            // 西・南アジア
            Leader("kujula_kadphises", "kushan", "クジュラ・カドフィセス", "クシャーナ王", "1世紀", "月氏系諸勢力をまとめ、クシャーナ朝の広域支配の基礎を築いた。"),
            Leader("kanishka_i", "kushan", "カニシカ1世", "クシャーナ王", "2世紀", "中央アジアと南アジアを結ぶ領域を統治し、宗教と交易、文化活動を保護した。"),
            Leader("ranjit_singh", "sikh_empire", "ランジート・シング", "マハーラージャ", "18～19世紀", "パンジャーブのシク諸勢力を統合し、ラホールを中心とする帝国を築いた。"),
            Leader("jind_kaur", "sikh_empire", "ジンド・カウル", "摂政・政治指導者", "19世紀", "幼いダリープ・シングの摂政として、シク帝国末期の主権維持に関わった。"),

            // 東・東南アジア
            Leader("taejo_wang_geon", "goryeo", "太祖（王建）", "高麗国王", "9～10世紀", "918年に高麗を建て、後三国を統一して新たな王朝秩序を築いた。"),
            Leader("gwangjong_goryeo", "goryeo", "光宗", "高麗国王", "10世紀", "王権と官僚制度を整え、仏教文化と対外交流を保護した。"),
            Leader("iskandar_muda", "aceh", "イスカンダル・ムダ", "アチェ・スルタン", "16～17世紀", "海上交易を背景にアチェの勢力を拡大し、行政と軍事を整備した。"),
            Leader("taj_ul_alam", "aceh", "タジュル・アラム・サフィアトゥッディン", "アチェ・スルタナ", "17世紀", "長期統治のスルタナとして、学芸、交易、外交を支えた。"),

            // ヨーロッパ・地中海
            Leader("enrico_dandolo", "venice", "エンリコ・ダンドロ", "ヴェネツィア元首", "12～13世紀", "第4回十字軍におけるヴェネツィアの行動を主導し、東地中海での影響を拡大した。"),
            Leader("francesco_foscari", "venice", "フランチェスコ・フォスカリ", "ヴェネツィア元首", "14～15世紀", "長期在任の元首として本土領拡大期を統治し、ドゥカーレ宮殿の改築を進めた。"),
            Leader("stephen_i_hungary", "hungary", "イシュトヴァーン1世", "ハンガリー国王", "10～11世紀", "1000年ごろに戴冠し、キリスト教王国の統治制度を整えた。"),
            Leader("matthias_corvinus", "hungary", "マーチャーシュ1世", "ハンガリー国王", "15世紀", "王権と行政を強化し、人文主義宮廷とコルヴィナ文庫を育てた。"),

            // アメリカ大陸
            Leader("cosijoeza", "zapotec", "コシホエサ", "サアチラの王", "15～16世紀", "サアチラを中心に統治し、メシカの圧力に対して軍事と婚姻同盟を用いたと記録される。"),
            Leader("cocijopii", "zapotec", "コシホピ", "テワンテペクの王", "16世紀", "スペイン勢力到来期のテワンテペクを統治し、植民地化初期の記録に現れる。"),
            Leader("nanyehi", "cherokee", "ナンイェヒ（ナンシー・ウォード）", "ギガウ・外交指導者", "18～19世紀", "チェロキーの評議会で高い地位を担い、戦争と和平の時代に外交へ尽力した。"),
            Leader("john_ross", "cherokee", "ジョン・ロス", "チェロキー・ネーション首席酋長", "18～19世紀", "立憲的なチェロキー政府を率い、強制移住に反対して共同体の権利を訴えた。"),

            // オセアニア
            Leader("hotu_matua", "rapa_nui", "ホトゥ・マトゥア", "祖先王（口承）", "成立年代には諸説", "ラパ・ヌイへの到来と社会の始まりを伝える口承で、祖先王として語られる。"),
            Leader("ngaara_rapa_nui", "rapa_nui", "ンガアラ", "アリキ・マウ", "19世紀", "19世紀の記録と口承に残る最高位首長で、島の祭祀とロンゴロンゴ伝承に結び付く。"),
            Leader("tanoa_visawaqa", "fiji", "タノア・ヴィサワンガ", "バウのヴニヴァル", "18～19世紀", "バウの高位首長として政争と復権を経験し、同地の影響力を強めた。"),
            Leader("seru_cakobau", "fiji", "セル・エペニサ・ザコンバウ", "バウのヴニヴァル・フィジー王", "19世紀", "バウを中心に影響力を広げ、1871年の王国成立と1874年のイギリスへの割譲に関わった。"),
        };

        public static IReadOnlyList<LeaderDef> All => Definitions;

        public static LeaderDef Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < Definitions.Length; i++)
                if (string.Equals(Definitions[i].Id, id, StringComparison.OrdinalIgnoreCase))
                    return Definitions[i];
            return null;
        }

        public static List<LeaderDef> ForCivilization(string civilizationId)
        {
            var result = new List<LeaderDef>();
            if (string.IsNullOrEmpty(civilizationId)) return result;
            for (int i = 0; i < Definitions.Length; i++)
                if (string.Equals(Definitions[i].CivilizationId, civilizationId,
                    StringComparison.OrdinalIgnoreCase))
                    result.Add(Definitions[i]);
            return result;
        }

        public static LeaderDef DefaultForCivilization(string civilizationId)
        {
            if (string.IsNullOrEmpty(civilizationId)) return null;
            for (int i = 0; i < Definitions.Length; i++)
                if (string.Equals(Definitions[i].CivilizationId, civilizationId,
                    StringComparison.OrdinalIgnoreCase))
                    return Definitions[i];
            return null;
        }

        public static bool BelongsTo(string leaderId, string civilizationId)
        {
            var leader = Find(leaderId);
            return leader != null && string.Equals(leader.CivilizationId, civilizationId,
                StringComparison.OrdinalIgnoreCase);
        }

        static LeaderDef Leader(string id, string civilizationId, string nameJa, string titleJa,
            string periodJa, string summaryJa)
        {
            return new LeaderDef(id, civilizationId, nameJa, titleJa, periodJa, summaryJa, true);
        }

        static LeaderDef Unknown(string id, string civilizationId, string titleJa,
            string periodJa, string summaryJa, string nameJa = "名は未詳")
        {
            return new LeaderDef(id, civilizationId, nameJa, titleJa, periodJa, summaryJa, false);
        }
    }
}
