# シミュレーションゲーム要素・生成技術台帳

最終更新: 2026-07-23 / 第8版

## この台帳の役割

「歴史上のすべてのゲーム・人物・文化・技術」は、同人作品、改版、DLC、MOD、地域限定作品、未記録の文化まで含めると閉じた有限集合として確定できません。HexCivでは、存在しない「完全列挙」を断言せず、次の方法で継続的に網羅性を高めます。

1. ゲーム名そのものではなく、再利用可能な**設計要素**へ分解する。
2. 一次資料または信頼できる史料を確認し、地域・時代・分野の偏りを減らす。
3. 台帳へ候補を追加し、Coreの決定論とセーブ互換を守って一群ずつ実装する。
4. 他作品の画像・音・文章・固有UI・数値表を複製せず、仕組みはHexCiv用に再設計する。

文明・指導者・遺跡・偉人・研究・文化・作品・生活技術・自然地理の収録状況は、既存の `GLOBAL_HISTORY_CATALOG.md`、`WORLD_HISTORY_CATALOG.md`、`RESEARCH_CULTURE_CATALOG.md`、`MASTERPIECE_CATALOG.md`、`MATERIAL_CULTURE_CATALOG.md`、`NATURAL_GEOGRAPHY_CATALOG.md` が正本です。この文書は、ゲーム機構とメディア生成技術の正本です。

## シミュレーションゲーム設計参照索引（第1群）

以下は「全作品を収録済み」という意味ではなく、ゲーム史の異なる系譜から抽出した最初の参照群です。各作品の固有表現を移植せず、右列の抽象化された設計要素だけを検討します。

| 系統 | 代表的な参照作品 | 抽出する設計要素 | HexCivでの状態 |
|---|---|---|---|
| 歴史 | Civilization | ターン制4X、都市、技術、文化、複数勝利 | 基盤実装済み |
| 歴史 | Europa Universalis | 国家外交、講和、交易、国家制度 | 宣戦・和平済み、外交拡張候補 |
| 歴史 | Crusader Kings | 人物特性、関係、家系、継承、評議会 | 指導者台帳済み、人物政治は候補 |
| 歴史 | Victoria | 人口集団、産業、市場、政治運動、国庫 | **国庫・人口社会・市場を第1・第3・第5実装** |
| 歴史 | Total War | 戦略地図と戦術戦闘の階層 | 戦略戦闘済み、戦術層は長期候補 |
| 歴史 | Old World | 命令資源、家族、事件、野心 | 事件・人物関係候補 |
| 歴史 | HUMANKIND | 時代遷移、文化継承、名声 | 時代表示済み、継承候補 |
| 歴史 | Romance of the Three Kingdoms | 武将、都市内政、任命、同盟 | 指導者・都市済み、任命候補 |
| 軍事 | Hearts of Iron | 補給、生産、戦線、師団編制、諜報 | 戦争・補給済み、戦線候補 |
| 軍事 | Command: Modern Operations | センサー、探知、射程、兵站、交戦規則 | 視界・射程済み、探知層候補 |
| 軍事 | Combat Mission | 士気、抑圧、地形、命令遅延 | 地形防御済み、士気候補 |
| 軍事 | Steel Panthers | ヘクス、部隊損耗、弾薬、諸兵科 | ヘクス・HP済み、弾薬候補 |
| 軍事 | Panzer General | シナリオ、補充、経験、作戦目標 | 経験・目標候補 |
| 軍事 | Unity of Command | 補給線、作戦機動、限定ターン目標 | **補給線を第2実装** |
| 軍事 | Gary Grigsby's War in the East | 兵站、指揮、疲労、戦域規模 | **戦争疲弊を第1実装** |
| 軍事 | Wargame / WARNO | 偵察、指揮域、増援、複合兵科 | 偵察済み、指揮域候補 |
| 政治 | Democracy | 有権者集団、政策連鎖、支持率、選挙 | **支持層・法律を第4実装**、選挙候補 |
| 政治 | Suzerain | 政策決定、予算、派閥、物語分岐 | **派閥・法律を第4実装**、事件候補 |
| 政治 | Tropico | 派閥、住民需要、選挙、経済 | **需要・満足・派閥を第3・第4実装**、選挙候補 |
| 政治 | Geo-Political Simulator | 省庁、指標、国際関係、危機 | 国家指標を第1実装 |
| 政治 | Hidden Agenda | 閣僚、予算、治安、政治的圧力 | 閣僚・治安候補 |
| 政治 | The Political Process | 選挙区、世論、運動、法案 | 選挙・法案候補 |
| 政治 | Urban Empire | 都市議会、法案、時代、市民需要 | 都市議会候補 |
| 政治 | Rogue State | 省庁予算、国際圧力、国内安定 | **安定度を第1実装** |
| 経営 | SimCity | 都市予算、税、需要、公共サービス | **税制・維持費を第1実装** |
| 経営 | Cities: Skylines | 税収、維持費、人口、産業・物流 | **収入・支出を第1実装** |
| 経営 | OpenTTD | 路線、輸送需要、利益、設備更新 | **抽象貨物・輸送容量を第5実装**、明示路線候補 |
| 経営 | Railroad Tycoon | 路線網、地域市場、株式、輸送価格 | **地域市場・輸送価格を第5実装**、金融候補 |
| 経営 | Capitalism | 生産連鎖、価格、在庫、競争 | **抽象5財・価格・在庫を第5実装**、企業競争候補 |
| 経営 | Anno | 人口階層、需要、生産網、海上交易 | **人口階層・需要・地域産業・交易を第3・第5実装** |
| 経営 | RollerCoaster Tycoon | 来訪者行動、価格、保守、満足度 | 観光・保守候補 |
| 経営 | Theme Hospital | 職員、設備、待ち行列、品質 | 専門家・公共施設候補 |
| 経営 | Football Manager | 人材、契約、育成、戦術、データ分析 | 人材登用・成長候補 |
| 経営 | RimWorld | 個人特性、仕事、需要、事件生成 | 人物・事件候補 |

## シミュレーションゲーム設計参照索引（第2群）

第2群では参照範囲を16系統増やし、累計50系統とした。作品名の収集自体を目的にせず、未収録の設計要素を後方追加する。

| 系統 | 代表的な参照作品 | 抽出する設計要素 | HexCivでの状態 |
|---|---|---|---|
| 歴史 | Imperator: Rome | 人口、文化統合、属州、交易品 | 人口社会・属州候補 |
| 歴史 | Nobunaga's Ambition | 武将配置、城域、内政、外交 | 指導者済み、任命候補 |
| 歴史 | Field of Glory: Empires | 国家衰退、地域、文化、戦闘連携 | 安定度済み、衰退候補 |
| 歴史 | Rise of Nations | 国境、時代、資源、消耗 | 国境・時代・**補給消耗を第2実装** |
| 軍事 | Decisive Campaigns | 補給、司令部、参謀、決断カード | **都市補給網を第2実装**、指揮候補 |
| 軍事 | Graviteam Tactics | 弾薬、士気、通信、命令遅延 | 補給基盤済み、弾薬・通信候補 |
| 軍事 | Flashpoint Campaigns | 指揮統制、非同期命令、偵察 | 視界済み、命令周期候補 |
| 軍事 | Radio Commander | 不完全報告、通信、指揮判断 | 戦場の霧済み、報告遅延候補 |
| 政治 | The Political Machine | 選挙運動、地域支持、資金、世論 | 国庫済み、選挙候補 |
| 政治 | Republic: The Revolution | 人脈、影響圏、工作、派閥 | 文化影響済み、派閥候補 |
| 政治 | Realpolitiks | 国家指標、外交、国際機構、危機 | 国家指標済み、機構候補 |
| 政治 | Power & Revolution | 法律、予算、省庁、支持率 | 国庫済み、法律・省庁候補 |
| 経営 | Dwarf Fortress | 個体、労働、物資、創発的事件 | 人物・在庫・事件候補 |
| 経営 | Factorio | 生産連鎖、自動化、物流容量 | **補給網を第2実装**、生産連鎖候補 |
| 経営 | Workers & Resources: Soviet Republic | 建設資材、物流、労働、国家経済 | 国庫・補給済み、資材候補 |
| 経営 | Transport Fever | 旅客・貨物需要、路線、輸送能力 | 補給基盤済み、交易路候補 |

## シミュレーションゲーム設計参照索引（第3群）

第3群ではさらに16系統を追加して累計66系統とした。今回は人口を単なる都市サイズではなく、職能・教育・需要・満足・移住を持つ社会として扱うための比較軸を優先した。

| 系統 | 代表的な参照作品 | 抽出する設計要素 | HexCivでの状態 |
|---|---|---|---|
| 歴史 | Millennia | 地域人口、労働者、需要、産出チェーン、時代分岐 | **職能産出を第3実装**、チェーン候補 |
| 歴史 | Making History | 国家経済、人口、工業、外交、戦争 | 人口・国庫済み、産業候補 |
| 歴史 | Age of History | 州、人口、税、外交、領土変化 | 人口・税制済み、州候補 |
| 歴史 | Terra Invicta | 国家支持、組織、人物、宇宙経済 | 支持・組織・人物は長期候補 |
| 軍事 | Shadow Empire | 補給、兵站拠点、指揮、政治、資源 | 補給・国庫済み、指揮候補 |
| 軍事 | The Operational Art of War | 作戦規模、補給、増援、戦闘準備 | 補給済み、戦闘準備候補 |
| 軍事 | Armored Brigade | 指揮遅延、士気、視界、諸兵科 | 視界済み、士気・指揮遅延候補 |
| 軍事 | Rule the Waves | 海軍設計、予算、造船、外交圧力 | 国庫済み、海軍・造船候補 |
| 政治 | Lawgivers | 議会、法案、政党、選挙、支持 | **法律・支持を第4実装**、議会・選挙候補 |
| 政治 | Political Animals | 有権者属性、地域運動、選挙資源 | 支持層・選挙候補 |
| 政治 | Crisis in the Kremlin | 省庁予算、派閥、改革、国家指標 | 国庫・安定度済み、派閥候補 |
| 政治 | NationStates | 政策事件、選択、指標の長期変化 | 選択式事件候補 |
| 経営 | Banished | 住民、職業、食料、住居、幸福、移住 | **階層・食料・住居・満足・移住を第3実装** |
| 経営 | Frostpunk | 需要、労働配置、法律、不満、危機 | 需要・満足済み、法律・危機候補 |
| 経営 | Oxygen Not Included | 個体需要、職能、物流、環境循環 | 職能・需要済み、環境循環候補 |
| 経営 | Railway Empire | 路線、貨物需要、都市成長、競争 | 人口成長済み、交易路候補 |

## シミュレーションゲーム設計参照索引（第4群）

第4群では市場・交易・産業・輸送を中心に16系統を追加し、累計82系統とした。作品固有の経済数値やUIを移植せず、需要、供給、在庫、価格、輸送制約、戦時遮断という比較軸だけを抽出する。

| 系統 | 代表的な参照作品 | 抽出する設計要素 | HexCivでの状態 |
|---|---|---|---|
| 歴史 | Sid Meier's Colonization | 特産物、加工、港湾、船舶、交易価格 | **地域産業・5財・価格を第5実装**、港湾候補 |
| 歴史 | Imperialism | 国家産業、原料、鉄道、世界市場、外交 | **素材・製品・市場アクセスを第5実装** |
| 歴史 | Imperialism II | 探索、植民、交易品、加工、海上輸送 | **地域産業・交易を第5実装**、植民表現は慎重に扱う |
| 歴史 | Grand Tactician: The Civil War | 国家経済、補給、鉄道、士官、戦域 | 国庫・補給・市場済み、士官候補 |
| 軍事 | War in the Pacific: Admiral's Edition | 海運、港湾、燃料、工業、長距離補給 | 抽象輸送・補給済み、海上船団候補 |
| 軍事 | Command Ops 2 | 司令部、補給、命令遅延、作戦テンポ | 補給済み、指揮遅延候補 |
| 軍事 | Strategic Command | 国家資源、研究、補給、外交、戦域 | 国庫・研究・補給済み、戦域候補 |
| 軍事 | The Operational Art of War IV | 補給、戦備、増援、作戦目標、時限 | 補給済み、戦備・目標候補 |
| 政治 | President Elect | 選挙区、世論、運動資源、候補者 | 支持層済み、選挙候補 |
| 政治 | Shadow President | 国家指標、外交危機、軍事・経済判断 | 国家指標済み、危機事件候補 |
| 政治 | SuperPower 2 | 予算、税、貿易、外交、軍事 | **国庫・税・交易を第1・第5実装** |
| 経営 | Patrician | 港市市場、需給、船団、倉庫、商人競争 | **在庫・価格・平時交易を第5実装**、港市候補 |
| 経営 | Port Royale | カリブ海交易、生産、船団、価格差、護衛 | **価格差交易・戦時遮断を第5実装**、護衛候補 |
| 経営 | Rise of Industry | 原料、工場、需要、輸送、生産連鎖 | **抽象5財と需要を第5実装**、多段加工候補 |
| 経営 | Industry Giant | 産業立地、加工、物流、小売、需要 | **地域産業・在庫を第5実装**、施設立地候補 |
| 経営 | Offworld Trading Company | 動的価格、資源競争、市場操作、企業戦略 | **不足・余剰価格を第5実装**、市場操作は候補 |

## シミュレーションゲーム設計参照索引（第5群）

第5群では地形、水系、生態、気候と人間活動の関係を中心に16系統を追加し、累計98系統とした。自然を単なる背景や無限資源にせず、移動・産出・市場・科学・文化へ小さく接続する比較軸を抽出する。

| 系統 | 代表的な参照作品 | 抽出する設計要素 | HexCivでの状態 |
|---|---|---|---|
| 歴史 | Civilization VI | 河川、沿岸、地形隣接、自然景観 | **河川食料・水辺市場・自然多様性を第6実装** |
| 歴史 | Endless Legend | 高低差、地域地形、季節、探索 | 山岳・地域地形済み、季節候補 |
| 歴史 | Ara: History Untold | 地域資源、生態帯、都市圏、物流 | 地域産業・自然多様性済み、生態帯候補 |
| 歴史 | At the Gates | 季節、河川、資源枯渇、移住 | 河川・移住済み、季節・枯渇候補 |
| 軍事 | Panzer Corps 2 | ヘクス地形、河川、天候、補給 | 補給・河川表示済み、渡河・天候候補 |
| 軍事 | Unity of Command II | 河川、道路、補給網、地形隘路 | 補給済み、渡河・橋梁候補 |
| 軍事 | Hex of Steel | 地形、天候、橋梁、海陸移動 | 地形済み、橋梁・天候候補 |
| 軍事 | Command: Modern Operations | 海域、地形、距離、探知環境 | 海域台帳済み、海上・探知環境候補 |
| 政治 | Fate of the World | 気候政策、地域影響、資源、長期指標 | 政策・地域集計済み、気候指標候補 |
| 政治 | Democracy 4 | 環境政策、世論、予算、政策連鎖 | 法律・支持・国庫済み、環境政策候補 |
| 政治 | Suzerain | 地域開発、インフラ、資源、政治選択 | 地域産業・法律済み、事件選択候補 |
| 政治 | Eco | 生態系、資源利用、法律、共同統治 | 法律済み、生態循環・共同統治候補 |
| 経営 | SimEarth | 気候、地形、生態系、人間活動 | 自然地理集計済み、長期環境循環候補 |
| 経営 | Timberborn | 河川流量、干ばつ、貯水、都市生産 | 湖・河川済み、流量・治水候補 |
| 経営 | Against the Storm | 生態帯、資源、住民需要、周期災害 | 需要・地域産業済み、周期災害候補 |
| 経営 | Terra Nil | 水系、植生回復、生態多様性、土地再生 | 自然多様性済み、回復・保全候補 |

## シミュレーションゲーム設計参照索引（第6群）

第6群では河川流域、渡河、港湾、海上輸送を中心に16系統を追加し、累計114系統とした。水辺を単なる産出ボーナスにせず、移動・戦闘・都市立地・補給を結ぶネットワークとして比較する。

| 系統 | 代表的な参照作品 | 抽出する設計要素 | HexCivでの状態 |
|---|---|---|---|
| 歴史 | Pharaoh: A New Era | 河川氾濫、肥沃化、農業周期、都市物流 | **氾濫原と12ターン季節周期を第7・第8実装** |
| 歴史 | Sumerians | 灌漑、水路、河川都市、農業余剰 | 河川都市・氾濫原済み、灌漑網候補 |
| 歴史 | Grand Ages: Rome | 道路、港、海上交易、属州都市 | **港と海上補給を第7実装**、道路網は候補 |
| 歴史 | Oriental Empires | 河川流域、交易、都市圏、軍の移動 | 流向・渡河・都市圏済み |
| 軍事 | Order of Battle: World War II | 河川、橋、補給、海上輸送 | **渡河・橋梁網・海上補給を第7・第8実装** |
| 軍事 | WarPlan | 港、船団、補給、戦域間輸送 | **港湾補給・護送船団・封鎖を第7・第8実装** |
| 軍事 | Strategic Mind: Blitzkrieg | 渡河、橋、地形目標、作戦テンポ | **渡河と都市圏橋梁網を第7・第8実装** |
| 軍事 | Campaign Series | ヘクス地形、河川、橋梁、補給 | ヘクス・河川・補給済み、橋破壊は候補 |
| 政治 | Balance of Power | 危機段階、影響圏、外交的抑制 | 宣戦・和平済み、段階的危機候補 |
| 政治 | Ostalgie | 政治派閥、経済指標、外交圏、改革 | 支持・市場・外交済み、改革事件候補 |
| 政治 | China: Mao's Legacy | 政策選択、派閥、国家指標、改革 | 法律・支持済み、事件連鎖候補 |
| 政治 | Precipice | 地域影響、危機管理、代理対立 | 文化影響済み、外交危機候補 |
| 経営 | Captain of Industry | バルク物流、港湾、輸送容量、生産連鎖 | 港・抽象5財済み、多段加工候補 |
| 経営 | Highrise City | 資源網、輸送、都市需要、地域物流 | 需要・輸送財済み、明示物流網候補 |
| 経営 | SeaOrama: World of Shipping | 船隊、港、航路、契約、市況 | 港・市場済み、船隊・契約候補 |
| 経営 | Port Royale 4 | 港市、船団、価格差、護衛、海戦 | **港・価格差交易・海上補給・護衛・封鎖を実装済み** |

## シミュレーションゲーム設計参照索引（第7群）

第7群では季節洪水、橋梁工兵、海上封鎖、護送船団、港湾経営を中心に16系統を追加し、累計130系統とした。艦船ユニットが未実装の現段階では、沿岸の交戦中戦力を艦隊・沿岸砲・私掠活動の集約表現として扱う。

| 系統 | 代表的な参照作品 | 抽出する設計要素 | HexCivでの状態 |
|---|---|---|---|
| 歴史 | Nebuchadnezzar | 河川都市、灌漑、農業、物流 | 河川都市・季節肥沃化済み、灌漑網は候補 |
| 歴史 | Builders of Egypt | ナイル周期、都市生産、交易、宗教 | **季節洪水・肥沃期を第8実装** |
| 歴史 | Egypt: Old Kingdom | 河川農業、季節、国家事業、危機 | 季節産出・国家運営済み、事業計画は候補 |
| 歴史 | Imperiums: Greek Wars | 都市国家、海域、補給、外交 | 補給・外交・海上網済み、海軍は候補 |
| 軍事 | Atlantic Fleet | 船団護衛、通商破壊、索敵、海戦 | **護送船団と沿岸封鎖を第8実装**、艦隊は候補 |
| 軍事 | UBOAT | 船団、哨戒、補給、潜水艦戦 | **船団耐性と封鎖圧力を第8実装** |
| 軍事 | Silent Hunter III | 航路、船団、護衛、探知 | 船団・視界済み、海上探知は候補 |
| 軍事 | War on the Sea | 港湾、船団、航空・海上阻止、補給 | 港・封鎖・補給済み、諸兵科海戦は候補 |
| 軍事 | Ultimate Admiral: Dreadnoughts | 艦隊設計、海上封鎖、港、国家経済 | **港・封鎖・国庫を実装済み**、造船は候補 |
| 軍事 | Carrier Battles 4 Guadalcanal | 海上補給、船団、制海、作戦目標 | **海上補給と二重封鎖を第8実装** |
| 政治 | Conflict: Middle East Political Simulator | 外交危機、軍事圧力、資源、内政 | 外交・戦争・国庫済み、危機事件は候補 |
| 政治 | Realpolitiks II | 国家指標、外交、戦争、経済圏 | 国家指標・市場・戦争済み、国際機構は候補 |
| 経営 | Ports of Call | 船隊、港、航路、運航リスク | 港・船団抽象済み、個別船隊は候補 |
| 経営 | TransOcean: The Shipping Company | 船団、契約、港湾、航路収益 | 港・市場・船団済み、契約は候補 |
| 経営 | Sailwind | 帆走、積荷、風、島嶼交易 | 海上物流済み、風と個別積荷は候補 |
| 経営 | Sweet Transit | 交通網、橋、都市需要、物流容量 | **橋梁網・需要・輸送財を実装済み**、明示路線は候補 |

### 機構実装ロードマップ

| 段階 | 機構 | 内容 | 状態 |
|---|---|---|---|
| 1 | 国家運営 | 国庫、税制、人口・都市収入、都市・軍事維持費、安定度、戦争疲弊、AI税制 | 実装済み |
| 2 | 兵站 | 都市からの補給到達、地形コスト、敵遮断、孤立、補給切れ、技術・穀物庫 | **陸上網・港湾拠点・海上補給・沿岸封鎖・護送船団を実装済み**（道路タイル・艦隊は次拡張） |
| 3 | 人口社会 | 人口階層、職業、需要、教育、満足度、移住、AI社会重点 | **実装済み** |
| 4 | 政治 | 派閥、支持、法令、評議会、事件選択 | **派閥・支持・法律・正統性を実装済み**（事件・選挙は次拡張） |
| 5 | 市場 | 資源在庫、交易路、価格、生産連鎖 | **5財・需要・在庫・価格・自動交易・地域産業・港湾市場を実装済み**（明示路線・多段加工は次拡張） |
| 6 | 自然地理 | 山・川・海・湖・森・砂漠、河川、自然多様性、立地効果 | **72件台帳・内陸湖・流向河川・季節氾濫・渡河・橋梁網を実装済み**（分流・気候・災害は次拡張） |
| 7 | 人物史 | 特性、任命、関係、継承、家系 | 設計候補 |

## 第1実装: 国家運営

- **税源**: 人口×2、都市×4、建物×1、存続する首都+6。
- **税制**: 減税80%、均衡100%、重税130%。
- **支出**: 都市維持、ユニット維持、交戦相手ごとの戦争行政費。
- **安定度**: 0〜100。税率、国庫赤字、首都喪失、戦争疲弊から目標値を求め、毎ターン漸進する。
- **総合産出**: 安定度・税制・赤字が科学、文化、都市生産へ70〜120%の範囲で影響する。
- **AI**: 崩壊寸前なら減税、赤字や戦費不足なら重税、余剰国庫があれば減税、それ以外は均衡を選ぶ。
- **UI**: 左下「国家運営」またはF8。全存続文明の国庫、収支、安定度、疲弊、税制を比較できる。
- **セーブ**: version 10。version 9以前は国庫120、均衡税、安定度60へ移行する。

## 第2実装: 補給・兵站

- **補給源**: 全自都市。穀物庫のある都市は強化拠点として補給コストを2軽減する。
- **到達**: 基本6。車輪+2、建築学+1。友好領土は通りやすく、森林・丘陵・中立地・敵領土はコストが増える。
- **遮断**: 交戦中の敵部隊・敵都市がいるヘクスを補給は通過できない。複数経路があれば最小コスト経路へ迂回する。
- **補給逼迫**: 到達距離を1〜4超過。回復半減、実効戦闘力90%。
- **孤立**: 回復不能、移動力-1、実効戦闘力75%。2ターン目以降、非民間部隊は毎ターンHP5を消耗するが、補給消耗だけでは消滅しない。
- **AI**: 攻撃候補の損害見積もりへ補給倍率を適用する。経路そのものを守る戦略AIは次拡張。
- **UI**: 左下「兵站」またはF10。文明別集計と自軍部隊の状態を表示し、生成補給箱アイコンとスライドフェードを使用する。
- **セーブ**: version 11。version 10以前は補給良好・連続孤立0ターンへ移行する。

## 第3実装: 人口・階層・社会

- **三つの職能階層**: 全都市人口を農民・工人・学者へ自動配分し、合計を常に都市人口と一致させる。歴史上の特定身分を普遍化せず、ゲーム上の職能集計として扱う。
- **産出**: 農民は食料、工人は生産と税源、学者は科学と文化へ寄与する。教育70以上の都市には追加科学が生じる。
- **社会重点**: 均衡・農業重視・工芸重視・学問重視。人間はF7画面で選択し、AIは食料不足、戦争・国庫、図書館・教育から毎ターン判断する。
- **需要**: 食料、住居、奉仕の充足度を0〜100で更新する。建物、首都、人口規模が住居と奉仕に影響する。
- **教育と満足度**: 0〜100。技術、図書館、学者、税制、安定度、戦争疲弊、各需要から目標値を求め、毎ターン最大2ずつ漸進する。
- **移住**: 4ターンごとに同一文明内で都市魅力を比較し、差が18以上なら低魅力都市から高魅力都市へ1人口だけ移す。乱数は使わない。
- **UI・演出**: 左下「人口社会」またはF7。三色の人物アイコンを実行時生成し、0.18秒のスライドフェードと既存パネルSEを使用する。
- **セーブ**: version 12。version 11以前は均衡、全員農民、教育20、満足60、需要100/100/50へ移行する。

## 第4実装: 政治・利害集団・法律

- **政治資源**: 政治力0〜999。都市数、正統性、現行法を支持する集団から毎ターン得る。法律変更は30を消費する。
- **正統性**: 0〜100。安定度、平均満足度、現行法への支持、戦争疲弊から目標値を求め、毎ターン最大2ずつ漸進する。
- **利害集団**: 学術層・商業層・伝統層・軍事層。実在社会の固定的身分ではなく、教育、職能、建物、国庫、戦争から計算する抽象的な政策支持である。
- **法律**: 長老評議会（正統性）、地域民会（文化・満足／税収減）、商業特許状（税収／満足低下）、市民兵制（補給距離／維持費増）の4種類。
- **AI**: 戦時は市民兵制、赤字危機は商業特許状、高教育社会は地域民会、それ以外は長老評議会を勧告する。乱数は使わない。
- **UI・演出**: 左下「政治」またはF6。4支持バー、法律カード、実行時生成の天秤アイコン、0.18秒のスライドフェード、既存パネルSE。
- **セーブ**: version 13。version 12以前は政治力20、正統性60、長老評議会、各支持50へ移行する。

## 第5実装: 市場・交易・地域産業

- **5つの抽象財**: 食料、素材、製品、知識、輸送。職能、都市、建物、技術、地域産業から生産し、人口・都市・軍事が消費する。
- **在庫と価格**: 国内需要を在庫から充足し、不足が大きい財ほど価格が上がり、余剰在庫が多い財ほど下がる。価格は1～20。
- **文明間交易**: 文明ID順・財種順に、輸出側の余剰と輸入側の不足を接続する。市場アクセス、輸送在庫、首都間距離、商業特許状、市場方針が容量を決める。
- **戦争と交易**: 交戦中の文明間は直接交易を禁止する。戦時動員は素材・輸送を増やす代わりに知識と市場アクセスを下げる。
- **市場方針**: 自給優先・均衡市場・輸出振興・戦時動員。AIは戦争、需要充足、国庫、直近交易収支から選ぶ。
- **地域産業**: 生活技術72件を各地域12件の発展候補へ接続する。歌・踊り・武術を交換可能な物品とみなさず、知識・文化への継承効果として扱う。
- **他システム**: 需要充足は満足、製品は生産、知識と交通技術は科学、料理・踊り・歌は文化、輸送在庫は補給、交易収支は国庫へ接続する。
- **UI・演出**: 右側「市場」またはF4。5財、需給、価格、交易、地域産業、4方針を表示し、実行時生成の木箱・双方向矢印アイコンと0.18秒スライドフェードを使う。
- **セーブ**: 市場値はversion 14から保存。現行version 16でも互換を維持し、version 13以前は均衡市場、各在庫3、市場アクセス50、需要充足75、地域産業なしへ移行する。

## 第6実装: 自然地理台帳・決定河川

- **地理台帳**: 山・川・海・湖・森・砂漠／乾燥地を6地域×12件、計72件収録する。実在地名は図鑑に置き、生成世界へ無作為に流用しない。
- **水系生成**: 地形生成後、局所低地へ湖を置き、山麓から最寄り水域へ決定的な河道を生成する。
- **接続効果**: 河川の食料、水辺都市の市場アクセス、自然環境の多様性による科学・文化を小さく接続する。

## 第7実装: 河川流向・氾濫原・港湾海上補給

- **流向**: 各河川タイルは0〜5の下流方向を保持し、河口まで循環せず到達する。分岐を描かず、合流時は既存下流を尊重する。
- **氾濫原**: 平地または砂漠の河川沿いに決定的に形成し、河川食料+1に加えて食料+1を得る。専用の明色帯を手続き描画する。
- **渡河**: 河道に沿わず河川タイルへ出入りする移動はコスト+1、近接攻撃力80%。建築学は橋梁網を解禁するが、研究だけではペナルティを無効化しない。
- **港**: 水域へ隣接する都市だけが建設できる。港は食料・生産・市場アクセスを増やし、都市模型には水側を向く桟橋と標識を実行時生成する。
- **海上補給**: 自文明の港から水域へ補給が入り、海上を低コストで進み、自領沿岸へ荷揚げされる。敵部隊・敵都市による遮断規則は陸上と共通。
- **表示**: 河道は下流方向だけを結び、河川上に小さな流向矢印を生成する。補給オーバーレイは海上経路も同じ色規則で表示する。
- **セーブ**: version 16で流向と氾濫原を保存。version 15は保存済み河川から流向・氾濫原を再構築し、version 14以前は河川自体を決定的に補完する。

## 第8実装: 季節洪水・橋梁網・沿岸封鎖・護送船団

- **季節洪水**: 12ターン周期を決定論的に計算し、2ターンの増水、3ターンの退水後肥沃期、7ターンの平常期を繰り返す。増水中は氾濫原の食料-1・移動コスト+1、肥沃期は食料+1。
- **橋梁網**: 建築学を得た河川圏都市だけが建設できる。都市労働圏の渡河移動・近接攻撃ペナルティと、架橋地点の増水移動ペナルティを無効化する。都市占領時は守備・管理網が失われ再建が必要。
- **沿岸封鎖**: 水域に隣接する交戦中の敵戦闘部隊または敵港湾都市を封鎖戦力として数え、海上補給の経路探索から当該水域を除外する。
- **護送船団庁**: 港を持つ都市だけが建設できる。文明の海上護衛網を有効にし、単独の封鎖戦力を突破する。二つ以上の封鎖戦力には遮断され、和平すると解除される。
- **AI・占領**: AIの建物優先へ港→護送船団庁と河川圏の橋梁網を加える。占領時は橋梁網と護送船団庁を失うが、恒久施設の港は残る。
- **表示・アニメーション**: 増水面、退水後の堆積帯、流向に直交する橋桁を外部画像なしで実行時生成する。増水面はMaterialPropertyBlockでゆっくり脈動し、軽量演出モードでは固定表示する。
- **セーブ**: 洪水状態はTurnNumberから再現し、橋梁網・護送船団庁は既存の建物ID一覧へ保存されるため、version 16のまま互換を維持する。

## 画像・動画・音楽・音声生成技術の分類台帳

「生成」は手続き生成、シミュレーション、制作支援、機械学習による合成を含みます。製品名の網羅ではなく、技術系譜を追跡します。第8版は8系統を増補し、累計81技術系譜です。

| 媒体 | 技術系譜 | 代表的な方式 | HexCiv方針・状態 |
|---|---|---|---|
| 画像 | 手続きラスタ | ピクセル規則、タイル、ノイズ、フラクタル | 地形・国庫アイコン等で実装済み |
| 画像 | 手続きベクタ | パス、幾何プリミティブ、図形文法 | UIアイコンで実装済み |
| 画像 | 生物・都市生成 | L-system、セル・オートマトン、形状文法 | 植生・都市景観へ導入候補 |
| 画像 | 制約充足 | Wave Function Collapse、グラフ文法 | 都市外観・遺跡配置候補 |
| 画像 | 物理レンダリング | ラスタライズ、レイトレーシング、パストレーシング | Unity表示は軽量ラスタを維持 |
| 画像 | 3D取得・再構成 | 写真測量、NeRF、Gaussian Splatting | 権利確認済み史跡資料のみ将来候補 |
| 画像 | 潜在生成 | VAE、GAN、正規化フロー | オフライン制作候補 |
| 画像 | 拡散・フロー | diffusion、latent diffusion、flow matching | 国家運営のオリジナル装飾画像で導入済み |
| 画像 | 自己回帰・マルチモーダル | token生成、画像言語モデル | 制作支援候補 |
| 画像 | 微分可能・逆レンダリング | differentiable rendering、inverse graphics | 史跡再構成の研究候補 |
| 画像 | 条件制御生成 | edge/depth/pose conditioning、adapter | オリジナルUI素材の構図制御候補 |
| 画像 | ニューラル素材生成 | texture synthesis、PBR material generation、seamless tiling | 地形用の権利確認済みオリジナル素材候補 |
| 画像 | 3D形状生成 | implicit field、point cloud、mesh diffusion | オリジナル遺産模型の制作支援候補 |
| 動画 | 伝統的アニメーション | セル、ストップモーション、キーフレーム | UIキーフレーム相当を実装済み |
| 動画 | 補間・トゥイーン | 線形/曲線補間、モーフィング、フレーム補間 | 国家運営画面のフェード・拡大で実装済み |
| 動画 | キャラクター動作 | スケルタル、IK、モーションキャプチャ | ユニット高度化候補 |
| 動画 | 手続き・物理 | パーティクル、群衆、流体、剛体 | 戦闘・環境演出に一部実装済み |
| 動画 | ニューラル合成 | GAN動画、動画拡散、自己回帰世界モデル | 実行時導入なし。事前生成のみ候補 |
| 動画 | モーショングラフ | 動作断片接続、状態機械、行動遷移 | ユニット動作拡張候補 |
| 動画 | 手続きカメラ | 注視点、衝突回避、イベント優先度 | 歴史ツアー・観戦カメラで一部実装済み |
| 動画 | 動作拡散 | motion diffusion、text-to-motion、trajectory conditioning | 架空ユニットの事前生成動作候補 |
| 動画 | ニューラル人物レンダリング | talking avatar、reenactment、neural character | 実在人物の無断再現は禁止、架空人物のみ候補 |
| 音楽 | 規則・確率生成 | 音楽文法、Markov連鎖、セル・オートマトン | 実行時作曲へ拡張候補 |
| 音楽 | 記号生成 | MIDI、RNN、Transformer | 制作支援候補 |
| 音楽 | 音響合成 | 加算・減算・FM・ウェーブテーブル・物理モデル | 手続きBGM/SEで実装済み |
| 音楽 | サンプル処理 | サンプリング、グラニュラー、タイムストレッチ | 外部素材なし方針では限定利用 |
| 音楽 | ニューラル音響 | neural codec、波形生成、音声拡散 | オリジナル事前生成のみ候補 |
| 音楽 | 適応型レイヤリング | stems、vertical remixing、horizontal resequencing | 時代・地域BGM切替で一部実装済み |
| 音楽 | 探索・進化生成 | genetic algorithm、constraint search | 制作支援候補 |
| 音楽 | 音源分離・再構成 | source separation、stem extraction、remix | 権利のある自作音源の適応化候補 |
| 音楽 | テキスト条件付き音楽 | audio token、latent audio、text-to-music | オリジナル事前生成のみ候補 |
| 音声 | 規則合成 | フォルマント、調音モデル | ナレーション候補 |
| 音声 | 連結合成 | diphone、unit selection | 権利確認済み録音が必要 |
| 音声 | 統計的音声 | HMM、パラメトリック音声、vocoder | 歴史的系譜として記録 |
| 音声 | 音声符号化 | LPC、CELP、neural audio codec | 軽量保存・伝送の将来候補 |
| 音声 | ニューラルTTS | seq2seq、Transformer、neural vocoder、diffusion TTS | 将来の読み上げ候補 |
| 音声 | 声質・歌唱変換 | voice conversion、singing synthesis | 権利・同意のある声のみ候補 |
| 音声 | 音声対音声生成 | speech-to-speech、prosody transfer | 同意済み架空音声のみ将来候補 |
| 音声 | 感情・韻律制御 | duration、pitch、energy、style token | ナレーション表現候補 |
| 音声 | 少量話者適応 | speaker embedding、few-shot adaptation、voice cloning | 本人同意を記録できる声のみ候補 |
| 音声 | 対話音声生成 | streaming TTS、turn-taking、contextual prosody | 架空ナレーター・明示設定時のみ候補 |
| 画像 | 距離場・陰関数描画 | signed distance field、implicit contour、analytic antialiasing | 軽量な紋章・地図記号の候補 |
| 画像 | 減色・ディザ生成 | palette quantization、ordered/error diffusion dithering | 低解像度ミニマップ・生成アイコンの候補 |
| 動画 | イベント駆動モーション | gameplay event、animation graph、procedural timing | 戦闘・移住・UI開閉のイベント演出で一部実装済み |
| 動画 | 時空間超解像 | frame synthesis、temporal upscaling、motion compensation | 実行時導入なし。低性能環境では軽量化を優先 |
| 音楽 | 生成的編曲・声部進行 | motif grammar、voice leading、constraint orchestration | 地域・時代BGMの自作旋律拡張候補 |
| 音楽 | パラメトリック音色変形 | spectral morphing、cross-synthesis、dynamic filtering | 戦時レイヤー・時代クロスフェードで一部実装済み |
| 音声 | 発音生成・多言語正規化 | grapheme-to-phoneme、phoneme lexicon、text normalization | 日本語・現地名読み上げの将来候補 |
| 音声 | 音声修復・強調 | denoise、dereverberation、bandwidth extension | 権利確認済み自作録音だけに適用する候補 |
| 画像 | グラフ駆動アイコン合成 | semantic graph、primitive composition、layout constraint | 市場の木箱・双方向矢印アイコンを規則合成して一部実装済み |
| 画像 | データ駆動配色生成 | categorical palette、contrast constraint、color-blind-safe mapping | 5財マーカーと国家色の可読性改善候補 |
| 動画 | スプライトシート生成 | pose sequence、atlas packing、frame timing | 軽量ユニット動作・交易荷役の候補 |
| 動画 | 群集エージェント動作 | steering、flow field、behavior tree、density animation | 都市人口・祭礼の抽象群集演出候補 |
| 音楽 | 状態変数連動スコア | tension curve、parameter mapping、rule-based orchestration | 戦争状態レイヤー済み、市場繁栄レイヤーは候補 |
| 音楽 | 手続き打楽器パターン | Euclidean rhythm、accent grammar、tempo subdivision | 戦時BGMレイヤーで一部実装済み |
| 音声 | 音素単位の固有名合成 | multilingual phoneme inventory、prosody rule、unit concatenation | 文明・人物・地名の権利安全な読み上げ候補 |
| 音声 | 空間音響シーン生成 | source placement、distance attenuation、procedural ambience | 都市・市場・戦場の抽象環境音候補 |
| 画像 | 水文地形合成 | flow accumulation、watershed、cost path、drainage graph | **局所低地の湖と山麓からの決定河川を第6実装** |
| 画像 | 侵食地形生成 | hydraulic erosion、thermal erosion、sediment transport | 河谷・三角州・海岸線の将来候補 |
| 画像 | 生態帯合成 | latitude、moisture、temperature、ecological constraint | 緯度・湿度・森林を一部実装、遷移帯は候補 |
| 画像 | 地理ベクタ・ラスタ融合 | DEM、hydrography vector、rasterization、resampling | Natural Earth・HydroSHEDS・GEBCOを実世界モードの調査入口に登録 |
| 画像 | 地図総描・ラベル配置 | simplification、conflict removal、scale-dependent labeling | 大縮尺名称と小縮尺図鑑の分離候補 |
| 動画 | 流線・ベクトル場アニメーション | spline flow、vector field、particle advection | 河川・風・海流の軽量演出候補 |
| 音楽 | 環境音景の規則合成 | layered ambience、stochastic event、biome parameter | 外部録音に依存しない森林・河川・沿岸音景の候補 |
| 画像 | 地理空間基盤モデル | multimodal embedding、segmentation、remote-sensing synthesis | 事前調査支援のみ。出典不明の地理画像をゲームへ自動投入しない |
| 画像 | 位相制約付き河道記号 | directed graph、topology validation、flow arrow synthesis | **下流方向だけを結ぶ河道と流向矢印を第7実装** |
| 画像 | 氾濫原マスク合成 | terrain predicate、river adjacency、strip tessellation | **平地・砂漠河川の氾濫原帯を第7実装** |
| 画像 | 港湾構造物の規則合成 | waterfront orientation、pier primitive、beacon marker | **水側を向く桟橋・標識を第7実装** |
| 画像 | ネットワーク状態可視化 | shortest-path field、cost band、categorical overlay | **陸海を連続表示する補給オーバーレイを第7実装** |
| 動画 | 流向パルス | path phase、directional pulse、speed mapping | 河川流速・季節変化の軽量演出候補 |
| 動画 | 港湾活動パーティクル | berth event、cargo particle、schedule-driven motion | 建設・交易・補給イベントの候補 |
| 音楽 | 地理状態ソニフィケーション | hydrology parameter、port activity、adaptive motif | 河川量・港湾繁栄を自作BGMへ反映する候補 |
| 音声 | 水辺環境音の手続き合成 | filtered noise、stochastic splash、harbor event grammar | 外部録音なしの河川・港環境音候補 |
| 画像 | 季節状態マスク合成 | turn phase、categorical mask、layer compositing | **増水・肥沃・平常の動的地表層を第8実装** |
| 画像 | 流向適応インフラ合成 | tangent/normal frame、oriented primitive、ownership color | **河道に直交する橋桁と文明色帯を第8実装** |
| 画像 | 半透明水位オーバーレイ | alpha mesh、depth layering、fog-compatible composition | **増水面・波線を第8実装** |
| 動画 | 状態駆動水面パルス | simulation phase、sine modulation、property block | **増水期のみの軽量アニメーションを第8実装** |
| 動画 | 季節タイムラプス可視化 | discrete state transition、temporal sampling、change emphasis | 領土タイムラプスと連携する季節履歴の候補 |
| 音楽 | 季節適応オーケストレーション | state parameter、layer mix、motif transition | 自作BGMの増水・収穫モチーフ切替候補 |
| 音声 | インフラ状態ソニフィケーション | event mapping、warning grammar、spatial cue | 架橋完成・封鎖・船団突破の自作SE候補 |
| 音声 | 水文粒状合成 | filtered noise grain、density envelope、flow parameter | 河川流量に追従する外部録音なし環境音候補 |

### 導入規則

- APIキー、従量課金、外部送信を伴うクラウド生成は、利用者の明示的な許可と設定画面なしに組み込まない。
- 実在人物の声を無断で模倣しない。音声・写真・映像は権利、同意、出典、生成履歴を記録する。
- ゲーム実行時は決定論、容量、速度、オフライン動作を優先する。第1群では `Texture2D.SetPixels32`、UIトゥイーン、既存の `AudioClip.Create` を利用する。
- AI生成物はオリジナルの雰囲気素材に限定し、実在作品・ポスター・人物肖像の複製を避ける。第1群では国家運営画面用の台帳・天秤・抽象地図バナーを制作し、プロジェクト内へ保存した。

## 参照した公式資料

- [Cities: Skylines II — Economy & Production](https://www.paradoxinteractive.com/games/cities-skylines-ii/features/economy-production)
- [Victoria 3 — Dev Diary #57: The Journey So Far](https://www.paradoxinteractive.com/games/victoria-3/news/dev-diary-57-the-journey-so-far)
- [Victoria 3 — About](https://www.paradoxinteractive.com/games/victoria-3/about)
- [Victoria 3 — Law Enactment and Revolution Clock](https://www.paradoxinteractive.com/games/victoria-3/news/dev-diary-80-law-enactment-and-revolution-clock-in-13)
- [Victoria 3 — Elections](https://www.paradoxinteractive.com/games/victoria-3/news/dev-diary-45-elections)
- [Victoria 3 — Political Parties](https://www.paradoxinteractive.com/games/victoria-3/news/dev-diary-46-political-parties)
- [Victoria 3 — Trade Revisions](https://www.paradoxinteractive.com/games/victoria-3/news/dev-diary-54-trade-revisions)
- [Anno Union — Residential Tiers](https://www.anno-union.com/devblog-residential-tiers/)
- [Anno Union — Fulfil Needs Your Way](https://www.anno-union.com/devblog-fulfil-needs-your-way/)
- [Anno Union — Your own trading empire](https://www.anno-union.com/devblog-your-own-trading-empire/)
- [OpenTTD Manual — Cargo](https://wiki.openttd.org/en/Manual/Cargo)
- [Transport Fever 2 Manual — Towns](https://wiki.transportfever2.com/doku.php?id=gamemanual:towns)
- [Millennia — Economy Part One](https://www.paradoxinteractive.com/games/millennia/news/economy-part-one)
- [Crusader Kings III — About](https://www.paradoxinteractive.com/games/crusader-kings-iii/about)
- [Unity of Command — Developer Diary 5: The Supply Network](https://unityofcommand.net/blog/2016/04/06/development-diary-5-the-supply-network/)
- [Unity of Command — The Power of Supply](https://unityofcommand.net/blog/2011/11/03/the-power-of-supply/)
- [Shadow Empire — Official Game Manual (Matrix Games)](https://www.matrixgames.com/amazon/PDF/SE/Shadow_Empire_manual_EBOOK.pdf)
- [Natural Earth — 50m Physical Vectors](https://www.naturalearthdata.com/downloads/50m-physical-vectors/)
- [HydroRIVERS — Technical Documentation](https://data.hydrosheds.org/file/technical-documentation/HydroRIVERS_TechDoc_v10.pdf)
- [FEMA — Floodplain Management Requirements Study Guide](https://www.fema.gov/pdf/floodplain/nfip_sg_unit_1.pdf)
- [U.S. Army — Tactical River Crossings](https://www.armyupress.army.mil/Journals/Military-Review/English-Edition-Archives/March-April-2026/Tactical-River-Crossings/)
- [UNCTAD — Port Interface](https://resilientmaritimelogistics.unctad.org/guidebook/31-port-interface)
- [UNCTAD — Resilient Ports as a Key Pillar of a Resilient Maritime Supply Chain](https://resilientmaritimelogistics.unctad.org/guidebook/2-resilient-ports-key-resilient-maritime-supply-chain)
- [U.S. Naval History and Heritage Command — Order for Ships in Convoy (1917)](https://www.history.navy.mil/research/publications/documentary-histories/wwi/october-1917/rear-admiral-albert.html)
- [U.S. Naval History and Heritage Command — Coastal Convoys and the Second Happy Time](https://www.history.navy.mil/about-us/leadership/director/directors-corner/h-grams/h-gram-008/h-008-5.html)
- [The National Archives — Royal Navy operations and Merchant Navy convoy records](https://www.nationalarchives.gov.uk/help-with-your-research/research-guides/royal-navy-operations-second-world-war)
- [FAO — Global Forest Resources Assessment 2025](https://www.fao.org/forest-resources-assessment/past-assessments/fra-2025/en)
- [JRC — World Atlas of Desertification](https://wad.jrc.ec.europa.eu/)
- [GEBCO — Gridded Bathymetry Data](https://www.gebco.net/data-products-gridded-bathymetry-data)
- [Unity 6 `Texture2D.SetPixels32`](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Texture2D.SetPixels32.html)
- [Unity 6 `AudioClip.Create`](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AudioClip.Create.html)
