# 自然地理台帳 — 山・川・海・湖・森・砂漠 第2弾

最終更新: 2026-07-23 / 72件

## 方針

自然地理は現代国家の「所有物」ではなく、流域・海域・生態系・先住諸民族の関係を含む景観として記録します。実在名は図鑑だけに置き、手続き生成マップへ「ナイル川」などの名前を無作為に割り当てません。境界や表記に複数の立場がある場所は、国名だけで固定せず地形的な位置と関係性を併記します。

第1弾は6地域それぞれに山2・川2・海2・湖2・森林2・砂漠／乾燥地2を配置し、合計72件です。「歴史上・地球上のすべて」を完了と断言する台帳ではなく、既存IDを変えずに増補します。

## 収録済み72件

| 地域 | 山・山脈 | 川・水系 | 海・海域 | 湖・内水域 | 森・森林景観 | 砂漠・乾燥地 |
|---|---|---|---|---|---|---|
| アフリカ | キリマンジャロ、アトラス山脈 | ナイル川、コンゴ川 | ギニア湾、モザンビーク海峡 | ヴィクトリア湖、タンガニーカ湖 | コンゴ盆地熱帯林、ミオンボ林地 | サハラ砂漠、ナミブ砂漠 |
| 西・南アジア | ヒマラヤ山脈、ザグロス山脈 | インダス川、ガンガー（ガンジス）川 | アラビア海、ペルシア湾 | カスピ海、死海 | スンダルバンス、ヒルカニア森林 | アラビア砂漠、タール砂漠 |
| 東・東南アジア | 富士山、キナバル山 | 長江、メコン川 | 南シナ海、日本海 | バイカル湖、トンレサップ湖 | ボルネオ熱帯林、屋久島の森林 | ゴビ砂漠、タクラマカン砂漠 |
| ヨーロッパ・地中海 | アルプス山脈、エトナ山 | ドナウ川、ライン川 | 地中海、バルト海 | ラドガ湖、レマン湖（ジュネーヴ湖） | ビャウォヴィェジャ森林、シュヴァルツヴァルト | タベルナス砂漠、バルデナス・レアレス |
| アメリカ大陸 | アンデス山脈、ロッキー山脈 | アマゾン川、ミシシッピ＝ミズーリ川水系 | カリブ海、メキシコ湾 | 北米五大湖、チチカカ湖 | アマゾン熱帯林、トンガス森林 | アタカマ砂漠、グレートベースン砂漠 |
| オセアニア | グレートディヴァイディング山脈、アオラキ／マウント・クック | マレー＝ダーリング川水系、セピック川 | 珊瑚海、タスマン海 | カティ・サンダ／レイク・エア、タウポ湖 | デインツリー熱帯林、テ・ウレウェラ | グレートビクトリア砂漠、シンプソン砂漠 |

各項目は `NaturalFeatureCatalog` に安定ID、名称、分類、地域、位置、地形形式、要約を持ちます。ゲーム内では「世界史図鑑」→「自然地理」から全件または6地域別に閲覧できます。

## 手続き生成世界への実装

- 標高の局所低地から1〜3個の内陸湖を決定的に生成する。
- 山麓（山がない特殊地形では丘陵）から最寄り水域へ1〜4本の河川を生成する。
- 同じ設定・seedなら地形、湖、河川、開始位置が一致し、追加の乱数を消費しない。
- 河川タイルは食料+1。河川・海・湖に接する都市は市場アクセスを得る。
- 各河川タイルは下流方向を保持し、河口まで循環せず到達する。地図上では下流へだけ流路を結び、流向矢印を表示する。
- 平地・砂漠の河川には氾濫原を形成し、恒久的に食料+1。12ターン周期で増水2ターン、肥沃期3ターン、平常7ターンを繰り返し、増水中は食料-1・移動+1、肥沃期は食料+1。
- 河道に沿わない渡河は移動コスト+1・近接攻撃力80%。建築学を得た河川圏都市で「橋梁網」を完成させると、その都市圏の渡河ペナルティを無効化する。
- 水域隣接都市は「港」を建設できる。港は市場アクセスと都市産出を増やし、海上補給を水域経由で自領沿岸へ運ぶ。交戦中の敵沿岸戦力は水域を封鎖する。
- 港完成後に「護送船団庁」を建設すると単独の封鎖を突破できるが、二つ以上の封鎖戦力には海上補給を遮断される。
- 文明圏が接する山・川・海・湖・森・砂漠の多様性により、科学は最大+2、文化は最大+2。
- 河川・氾濫原・季節水面・流向矢印・橋桁・港の桟橋は外部画像を使わず実行時生成する。増水面はゆっくり脈動し、図鑑の自然地理アイコンも山・樹木・水流から生成する。
- SaveLoad version 16で河川・流向・氾濫原を保存する。version 15は河川配列から流向と氾濫原を再構築し、version 14以前は保存済み地形から河川も決定論的に補完する。

## 調査とデータ導入の入口

第1弾は名称と概要を公的・研究機関資料で相互確認しました。将来、実世界地図モードを追加する場合も、出典・ライセンス・縮尺を記録して別データ層として導入します。

- [Natural Earth 50m Physical Vectors](https://www.naturalearthdata.com/downloads/50m-physical-vectors/) — 河川、湖、海、地形ラベルの小縮尺ベクタ。
- [Natural Earth Features](https://www.naturalearthdata.com/features/) — データの分類と制作方針。
- [HydroRIVERS technical documentation](https://data.hydrosheds.org/file/technical-documentation/HydroRIVERS_TechDoc_v10.pdf) — 全球河川ネットワークの技術資料。
- [FEMA Floodplain Management Requirements Study Guide](https://www.fema.gov/pdf/floodplain/nfip_sg_unit_1.pdf) — 氾濫原の貯留・通水・土地利用を扱う基礎資料。
- [U.S. Army Tactical River Crossings](https://www.armyupress.army.mil/Journals/Military-Review/English-Edition-Archives/March-April-2026/Tactical-River-Crossings/) — 渡河作戦の複雑性と架橋・工兵資産を扱う資料。
- [UNCTAD Port Interface](https://resilientmaritimelogistics.unctad.org/guidebook/31-port-interface) — 港と後背地・海運ネットワークの接続を扱う資料。
- [UNCTAD Resilient Ports](https://resilientmaritimelogistics.unctad.org/guidebook/2-resilient-ports-key-resilient-maritime-supply-chain) — 港湾途絶と海上・後背地ネットワークの強靱性を扱う資料。
- [U.S. Naval History and Heritage Command Order for Ships in Convoy](https://www.history.navy.mil/research/publications/documentary-histories/wwi/october-1917/rear-admiral-albert.html) — 商船船団の編成と護衛運用を示す一次史料。
- [The National Archives Royal Navy operations](https://www.nationalarchives.gov.uk/help-with-your-research/research-guides/royal-navy-operations-second-world-war) — 商船船団と護衛部隊の作戦記録への公的案内。
- [FAO Global Forest Resources Assessment 2025](https://www.fao.org/forest-resources-assessment/past-assessments/fra-2025/en) — 森林資源評価の現在版。
- [JRC World Atlas of Desertification](https://wad.jrc.ec.europa.eu/) — 乾燥地と土地劣化を単純な砂地だけに還元しない資料。
- [GEBCO gridded bathymetry data](https://www.gebco.net/data-products-gridded-bathymetry-data) — 海底地形と陸上標高を統合した全球グリッド。

## 次の増補

1. 各地域へ高原、湿地、氷河、島嶼、洞窟、珊瑚礁、草原を追加する。
2. 河川の分流・流量、干ばつ、橋の個別破壊・修復、明示的な艦隊・航路を導入する。
3. 気候と植生の長期変化、災害、環境利用と保全を事件として実装する。
4. 実世界地図モードを追加する場合は、ゲーム用の単純化と史実名称の層を明確に分離する。
