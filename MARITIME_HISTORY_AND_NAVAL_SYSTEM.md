# 歴史船舶台帳と海軍システム

最終更新: 2026-07-23 / 第1弾

## 方針

歴史上の船を単一の進歩順や文明の固定所有物として扱わず、河川・湖沼・沿岸・外洋という
環境への応答、造船共同体、交易、移住、漁撈、軍事、儀礼を分けて記録します。失われた船、
地方名、同型異名、継続中の伝統があるため「全件収録済み」とはせず、安定IDで後方追加します。

## 第1弾36件

| 地域 | 収録船舶（6件） |
|---|---|
| アフリカ | 古代エジプトのパピルス舟、クフ王の船、フェルーカ、ムテペ、ジャハジ、マダガスカルのアウトリガー舟 |
| 西・南アジア | グッファ、タラダ、ダウ船、レンジ船、パッタマール、バグラ |
| 東・東南アジア | 中国ジャンク、宝船、板屋船、亀甲船、安宅船、ピニシ |
| ヨーロッパ・地中海 | ミノアのガレー船、フェニキアの二段櫂船、ギリシアの三段櫂船、ローマの五段櫂船、ヴァイキングのロングシップ、キャラベル船 |
| アメリカ大陸 | トリンギットの外洋カヌー、樺皮カヌー、トモル、トトラ葦舟、ダルカ、マルティニークのヨール |
| オセアニア | オーストロネシアのアウトリガー舟、双胴ヴァカ、ラカトイ、チャモロのプロア、テ・プケ、ホクレア |

正本は `Assets/Scripts/Core/HistoricVesselCatalog.cs` です。各レコードは `Id / NameJa /
RegionJa / EraJa / TraditionJa / RoleJa / SummaryJa` を持ちます。個別船の設計図や文化財画像を
複製せず、ゲーム内の艦船は歴史的機能を抽象化したオリジナル表現です。

## ゲームへの接続

- 基礎技術に「帆走」と「航海術」を追加。全研究対象は基礎14+研究史132=146件。
- 「帆走」は港とガレー船、「航海術」は三段櫂船と護送船団庁を解禁。
- 艦船は水域だけ、既存ユニットは陸地だけを移動する。経路探索と実移動は同じ領域判定を使う。
- 艦船は港を持つ水域隣接都市だけで生産され、空いている隣接水域へ進水する。
- 現段階の海戦は艦船対艦船。艦船は陸上都市を直接占領せず、陸軍も海上艦を攻撃しない。
- 敵艦船は沿岸封鎖圧力2、敵沿岸部隊・敵港は各1。近くの味方艦船は護衛力1として圧力を相殺。
- 護送船団庁は封鎖成立に必要な実効圧力を1から2へ上げる。敵が実際に占有する海域は通過不可。
- AIは港数に応じて艦船を建造し、戦時は敵艦迎撃または敵港沖の封鎖、平時は自国港沖の警備を行う。
- セーブ形式はユニット定義IDを保存する既存version 16のまま。旧セーブは新艦船を持たないだけで読める。

## 表示

`EntityRenderer` が外部画像なしで五角形船体、文明色の甲板、白い帆、淡い二条の航跡を
共有メッシュとして一度だけ生成します。既存の移動トゥイーン、被弾フラッシュ、HP、
補給マーカー、視界規則をそのまま再利用します。

## 次の増補

1. 船舶台帳を時代・用途別に増補し、河川舟、商船、軍船、漁船、救難船の偏りを調整する。
2. 遠隔艦・沿岸砲撃・港湾防御。ただし都市占領は陸軍に限定する。
3. 風向・海流・外洋航行技術、航路と船団を明示する海上交易。
4. 艦船経験、修理、港ごとの建造能力、輸送船と上陸作戦。
5. 外部録音を使わない帆・櫂・波・号鐘の手続きSE。

## 参照した公的・一次資料への入口

- Royal Museums Greenwich, “Shipbuilding: The earliest vessels”
  https://www.rmg.co.uk/stories/maritime-history/shipbuilding-earliest-vessels
- UNESCO, “Watertight-bulkhead technology of Chinese junks”
  https://ich.unesco.org/en/USL/watertight-bulkhead-technology-of-chinese-junks-00321
- UNESCO, “Pinisi, art of boatbuilding in South Sulawesi”
  https://ich.unesco.org/en/RL/pinisi-art-of-boatbuilding-in-south-sulawesi-01197
- UNESCO, “The Canoe is the People”
  https://www.unesco.org/en/links/canoe
- UNESCO, “The Martinique yole”
  https://ich.unesco.org/en/BSP/the-martinique-yole-from-construction-to-sailing-practices-a-model-for-heritage-safeguarding-01582
- UNESCO, “Floating architectures and traditional boats on the Tigris and Euphrates Rivers”
  https://www.unesco.org/en/virtual-science-museum/voices-water/iraq
- Smithsonian Ocean, “Raven Spirit: A Native American Canoe's Journey”
  https://ocean.si.edu/human-connections/history-cultures/raven-spirit-native-american-canoes-journey
- U.S. Naval Institute, Hellenic Navyの復元三段櫂船Olympias紹介
  https://www.usni.org/magazines/proceedings/2018/january/photo-week-19-january
- Unity 6 Manual, “スクリプトによるメッシュの作成とアクセス”
  https://docs.unity3d.com/ja/current/Manual/creating-meshes.html

これらは収録候補と抽象化の調査入口です。個々の地域・船名は次回増補時にも複数資料で再確認します。
