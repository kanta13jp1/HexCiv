# HexCiv 共同開発ハンドオフ

## 正式な開発先

- 正式プロジェクト: `C:\Users\kanta\GitHub\HexCiv`
- Unity: 6.3 LTS (`6000.3.20f1`)
- `C:\Users\kanta\CivilizationLike` は旧Codex試作版のバックアップです。新規実装は行いません。

CodexとClaude Codeは、以後この `HexCiv` プロジェクトだけを更新します。

## 作業前後のルール

1. 作業前に `ARCHITECTURE.md` と本ファイルを読む。
2. 他エージェントの変更を消さず、対象ファイルの現在内容を確認してから編集する。
3. `Core/` の純粋シミュレーション設計と公開APIを維持する。
4. 日本語表示には `UIStyle.JapaneseFont()` または `RenderUtil.JapaneseFont()` を使う。
5. 変更後はUnity 6でコンパイルし、可能ならスモークテストとWindowsビルドを実行する。
6. 完了した機能と未完了事項を、このファイルの「最新状況」に追記する。

## 検証コマンド

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.20f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'C:\Users\kanta\GitHub\HexCiv' -executeMethod SmokeTest.Run -logFile 'C:\Users\kanta\GitHub\HexCiv\Logs\smoke.log'
& 'C:\Program Files\Unity\Hub\Editor\6000.3.20f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'C:\Users\kanta\GitHub\HexCiv' -executeMethod BuildScript.PerformBuild -logFile 'C:\Users\kanta\GitHub\HexCiv\Logs\build.log'
```

## 最新状況

### 2026-07-24 Codex: ウルク史実キャンペーン第2基盤・共通史料／権利ゲート

- **別モードとしてプレイ可能**: タイトル画面の「史実キャンペーン：ウルク」から、紀元前4000～3000年・50ターン・固定32×20マップ・8勢力を開始できる。通常ランダムゲームと観戦モードは維持。
- **開始条件**: ウルク共同体は実人口1,500人、農地2区画、状態25%の未整備運河、少量の食料・葦・粘土等から開始。役割別人口と地位別人口を別軸で持ち、開始時の奴隷化人口は0。人口自動化だけON。
- **導入と都市国家化**: 最初の3ターンは灌漑、食料優先、神殿計画を顧問表示。20年単位の決定的な洪水・食料・人口進行を追加。人口2,500、灌漑農地2、食料安全、神殿、記録行政を都市国家成立条件として判定する。
- **UIと復元模型**: 年代、実人口、食料、農地、運河、神殿、安定度、簡潔／詳細、史実クイック保存／読込を持つ独立パネル。農地・運河・神殿は外部素材なしの暫定ローポリ復元模型として表示。
- **勝敗とセーブ**: 軍事・科学・文化・経済の即時勝利、ターン50存続勝利、中心集落占領・人口500未満・重大不安3期の敗北条件をCoreで独立判定。通常3スロットと史実用3世代自動セーブへ接続。史実セーブはデータセット版数を保持し、第1基盤の旧フィールド欠落を安全に補完。
- **共通史実台帳**: `Core/HistoricalContentSchema.cs` に日本語名・原語・転写・異綴り、年代幅、地域、確度、異説、出典、関連ID、固有効果と社会的費用、素材来歴の共通JSON契約を追加。
- **製品収録ゲート**: キャンペーン・勢力・物資・河川・施設・出典は `verified` 必須。出典の参照日・資料種別・利用区分、再利用素材の作者・ライセンス、生成復元素材の生成器・プロンプトハッシュ・確認者・「復元」表示を自動検証する。現在の博物館・UNESCO URLは調査参照のみで、画像素材を収録したという意味ではない。
- **`/grill-me` 設計確定**: 条件駆動史実、3段階確度、史実分岐分類、復元根拠レイヤー、透明な難易度、食料損失、労働配分、共同体と政体の分離、段階的紛争、実人数部隊、知識要素、決定論、イベント履歴、アクセシビリティ、性能・素材確認、縦切り版の完成ゲートを `URUK_HISTORICAL_CAMPAIGN.md` へ記録。
- **専用検証**: `HISTORICAL CONTENT SCHEMA SMOKE OK`、`HISTORICAL CAMPAIGN FOUNDATION SMOKE OK`、`URUK CAMPAIGN VERTICAL SLICE SMOKE OK`。
- **既存回帰**: `SMOKE SHORT OK (turn=60 units=127 cities=21 techs=194 wars=1)` と `SMOKE OK`。通常モードの既存ロジックへ影響なし。
- **Windows版**: `BUILD OK: 97918465 bytes, 27.4s`。`Build\HexCiv.exe`を15秒ヘッドレス起動しUnity `6000.3.20f1`、例外・クラッシュ0。
- **次段階**: 食料保存損失・用途別労働配分・地位変化を完成し、運河／農地／神殿のタイル操作、共同体／政体別AI、交易・外交・水利紛争へ順次接続する。現在の他7勢力は既存4X AIを利用しており、史実固有AIは未接続。

### 2026-07-23〜24 Claude Code: 短期ゲーム(100ターン)プリセット ⚠️**Core編集を伴う初の例外**、全検証合格

**背景**: ユーザーとの設計セッション(/grill-me)で「HexCivを実際に遊べるゲームにする」方針が確定。セーブデータ調査で通常プレイの記録が7/20のターン76の1件のみ(セーブ形式v1のまま)、以降は全て観戦モードと判明。原因は「開発中で確認優先」であり、**1ゲーム250ターンが着手障壁**と結論。目標は「30〜60分で完結する1ゲーム」。

**実装**: 歴史圧縮型の短期プリセット。ターン数0.4倍(250→100)、産出約2.5倍、ターン依存定数を一括スケール(文化勝利ゲート150→60、AI宣戦開始25→10、時代境界100/180→40/72)。UIの「ゲームの長さ」行で選択、**新規インストール時の初期選択は短期**(PlayerPrefs "HexCiv.GameLength")。

**⚠️ Core編集について(Codexへ)**: 当方はCore/を読み取り専用として運用してきましたが、本件のみユーザーの明示的決定により一括実装しました。保証した不変条件は**すべて検証で証明済み**です。

**公開API(契約 — Codexが参照/変更する場合はここを見てください)**:
```csharp
// Core/GameConfig.cs (追加のみ)
public int GameLength = 0;                    // 0=標準(250ターン) 1=短期(100ターン)。既定は0のまま
// Core/GameSpeedRules.cs (新規・純関数)
public static int MaxTurnsFor(int gameLength);            // 250 / 100
public static int ScaleTurn(GameConfig c, int stdTurn);   // 標準基準ターン→現モード(150→60)。標準では恒等
public static int ScaleChance(GameConfig c, int percent); // 確率の圧縮補正(短期2.5倍・100上限)。標準では恒等
// Core/Player.cs (追加)
public Dictionary<int,int> AiWarPressure;     // 短期モード専用の非永続な宣戦圧力アキュムレータ(標準では未使用)
```
**セーブ形式の重要な約束**: `gameLength` は **値が0のときJSONに出力しない**。これにより標準モードのセーブは従来とバイト単位で同一に保たれます(欠落フィールドは0=標準として読み込む既存の後方互換経路)。**この省略規則を外すと標準モードの基準値が即座に壊れます。**

**🐛 検証で捕まえて直した実バグ2件**(いずれも厳格ゲートがなければ見逃していた):
1. **標準モードのビット一致が実際に壊れていた** — 新フィールド `gameLength` がセーブに15文字(`,"gameLength":0`)追加し、往復サイズが 76875→76890 に変化。Codexの変更では説明がつかず「当ラウンドの不変条件違反」と判定し、上記の省略規則で根治。再検証で76875に復帰
2. **短期モードで戦争が一度も起きなかった** — 原因は「**戦争の機会窓は移動距離に律速されるため、産出2.5倍では圧縮されない**」こと。文明同士の接触に必要なターン数は変わらないのに全体が0.4倍になり、宣戦判定の機会が約90ターン分→約18ターン分へ激減。一方で毎ターンの確率は標準のまま(12/6/8%)だった。`ScaleChance`で確率を2.5倍にし、それでも試行数不足で偏るため `AiWarPressure` に確率を累積して確実に発火させる方式へ。標準モードは `s.Rng.Next(100)` の呼び出しも消費量も従来どおり(ビット一致で裏付け済み)

**検証結果(round 1で全ゲート合格)**:
- コンパイル: 0エラー **0警告**(CS0618も0)
- **標準モード不変**: seed42の全10行(各ターン・セーブ往復76875文字・決着行・WARS/PEACE合計)がCodexの最新ログ `Logs/smoke-stage9.log` と **Compare-Object で差分0**
- **短期モード**: `SMOKE SHORT OK (turn=60 units=127 cities=21 techs=194 wars=1)` — 都市21(条件8以上)・技術194(条件60以上)・戦争1(条件1以上)すべて合格
- エディタテスト **23種**全OK(Codex新設の歴史キャンペーン基盤テスト含む) / BUILD OK(97.9MB, 28.4秒) / 45秒起動テスト例外0

**📊 ユーザーが遊ぶ前に知っておくべき所見(バランス上の発見)**: 短期モードは**ターン60で文化勝利により決着**しました(100ターン中の60%)。これは標準モードが250ターン中150で決着するのと**同じ進行率**であり圧縮としては忠実です。しかし裏を返すと、**両モードとも「文化勝利が解禁された瞬間に終わる」**構造で、設計上の残り40%が実際には遊ばれていません。診断値 `SMOKE SHORT CULTURE: best=7450 policies=55` が示すとおり、勝利条件(文化1500・政策14)を**4〜5倍超過**して満たしています。文化勝利の条件が緩すぎる可能性が高く、実プレイの体感次第で調整対象になります(今回は「まず遊んでから判断」の取り決めにより未着手)。

### 2026-07-23 Codex: 航海技術・艦船・海上戦・歴史船舶台帳 第9段階

- `Core/GameRules.cs` / `Defs.cs` / `TechnologyCatalog.cs`: 基礎技術へ「帆走」「航海術」、海軍ユニットへ「ガレー船」「三段櫂船」を追加。港は帆走で解禁し、水辺都市＋港だけが艦船を建造できる。艦船は隣接水域へ進水し、陸上ユニットは陸、艦船は水だけを移動する。
- `Unit.cs` / `Pathfinder.cs` / `NaturalGeographySystem.cs` / `Combat.cs`: プレイヤー、AI、経路探索、実移動を `GameRules.CanUnitEnter` の領域規則へ統一。艦船同士の海戦を可能にし、艦船による陸上都市占領と、陸上部隊による海上攻撃を禁止した。
- `Core/LogisticsSystem.cs` / `AI/AIController.cs`: 敵艦は沿岸封鎖圧力2、沿岸の敵陸上部隊・敵港は1、味方艦の護衛は封鎖圧力を1相殺する。護送船団庁は航海術で解禁。AIは港湾都市数と艦隊数から建造を判断し、戦時は敵艦迎撃・敵港封鎖、平時は自港警備を行う。
- `Rendering/EntityRenderer.cs`: 船体・帆・二筋の航跡を共有メッシュと文明色で手続き生成。外部モデルや画像を追加せず、既存の移動補間、体力、選択、軽量表示へ接続した。
- `Core/HistoricVesselCatalog.cs`: アフリカ、西・南アジア、東・東南アジア、ヨーロッパ・地中海、アメリカ大陸、オセアニアから各6件、計36件の歴史船舶を安定ID台帳化。特定文明の固定所有物ではなく、地域・時代・船舶伝統・役割を記録する。全一覧・実装仕様・UNESCO等の調査入口は `MARITIME_HISTORY_AND_NAVAL_SYSTEM.md`。
- `GlobalHistoryIndex.cs` / 各図鑑資料: 総合台帳を **32分類・1303件**、技術を **基礎14＋研究史132＝146件**へ更新。`AchievementPanel` の「古代の知恵」も基礎14技術へ追随。`SIMULATION_AND_GENERATION_CATALOG.md` 第9版はシミュレーション参照 **130→146系統**、生成技術 **81→89系統**へ増補した。
- 検証: `NAVAL SYSTEM SMOKE OK`、`FLOOD BRIDGE CONVOY SMOKE OK`、`HISTORIC VESSEL CATALOG SMOKE OK`、`GLOBAL HISTORY INDEX SMOKE OK`、`RESEARCH TECH TREE SMOKE OK`、`RESEARCH CULTURE EXPANSION SMOKE OK`。150ターン総合は `SMOKE OK`（turn150 units=88 cities=18 techs=165 wars=3）、多文明・群島・難易度も合格。Windowsビルド `BUILD OK: 97846589 bytes, 49.3s`。`Build/HexCiv.exe`を20秒起動しUnity `6000.3.20f1`、例外・クラッシュ0。
- 次候補は海上交易路・輸送船と上陸作戦・艦隊損耗／修理・風向／海流、または人物史（特性・任命・関係・継承）。

### 2026-07-23 Codex: 季節洪水・橋梁網・沿岸封鎖・護送船団 第8段階

- `Core/NaturalGeographySystem.cs` / `City.cs`: 12ターン周期の増水2・肥沃3・平常7を決定論的に実装。増水中は氾濫原の食料-1・移動+1、肥沃期は食料+1。建築学だけでは渡河補正を消さず、河川圏都市の新建物「橋梁網」が都市労働圏の渡河移動・近接攻撃・増水移動を保護する。
- `Core/LogisticsSystem.cs` / `GameRules.cs`: 水域に隣接する交戦中の敵戦闘部隊・敵港湾都市を沿岸封鎖戦力として海上補給探索へ接続。港完成後の新建物「護送船団庁」は単独封鎖を突破し、二重封鎖には遮断される。占領時は港を残し、橋梁網と護送船団指揮系統を失う。
- `Core/AI/AIController.cs`: AIの建物優先へ港→護送船団庁と河川圏の橋梁網を追加。`City.AvailableProduction` で橋梁網は河川圏、護送船団庁は港完成後だけを候補にする。
- `Rendering/MapRenderer.cs`: 増水面・波線、退水後の堆積帯、流向に直交する文明色の橋桁を外部画像なしで動的結合メッシュ化。増水面は `MaterialPropertyBlock` のみで脈動し、軽量モードでは固定表示する。
- `SIMULATION_AND_GENERATION_CATALOG.md` 第8版: シミュレーション参照を**114→130系統**、画像・動画・音楽・音声生成技術を**73→81系統**へ増補。詳細仕様とFEMA、U.S. Army、UNCTAD、米海軍史、英国国立公文書館の公的資料は `SEASONAL_WATER_AND_CONVOYS.md` に記録。
- セーブはversion 16を維持。季節は既存 `TurnNumber` から復元し、橋梁網・護送船団庁は既存建物ID配列へ保存されるため新フィールドは不要。
- 検証: 新規 `FLOOD BRIDGE CONVOY SMOKE OK`（季節、産出、建設条件、橋、占領、単独／二重封鎖、船団、和平）、回帰 `HYDROLOGY MARITIME SMOKE OK`。150ターン総合は `SMOKE OK`（turn150 units=94 cities=18 techs=173 wars=3、累計戦争3・和平1）、多文明・群島・難易度も合格。Windowsビルド `BUILD OK: 97835325 bytes, 20.5s`。`Build/HexCiv.exe`を15秒起動しUnity `6000.3.20f1`、例外・クラッシュ0。
- 次候補は河川流量・干ばつ・橋の個別破壊／修復・艦船ユニットと航路、または人物史（特性・任命・関係・継承）。

### 2026-07-23 Codex: 河川流向・氾濫原・港湾海上補給 第7段階

- `Core/NaturalGeographySystem.cs` / `Tile.cs`: 河川へ下流方向を持たせ、河口まで循環せず到達する決定水系へ拡張。平地・砂漠の河川には氾濫原を生成し、追加食料+1。渡河は移動コスト+1・近接攻撃力80%で、建築学により橋梁・土木技術を抽象化して無効化する。経路探索・実移動・戦闘・AI損害予測を同じ規則へ統一。
- `Core/GameRules.cs` / `City.cs` / `LogisticsSystem.cs`: 水域隣接都市専用の建物「港」（食料+1・生産+1）を追加。港は市場アクセスと補給源を強化し、海上を低コストで伝播して自領沿岸へ荷揚げする。敵部隊・敵都市の遮断規則は陸上と共通。
- `Rendering/MapRenderer.cs` / `EntityRenderer.cs`: 河道を下流方向だけへ結び、流向矢印と氾濫原帯を手続き描画。港を建てた都市には水側を向く桟橋・標識を実行時生成。既存の補給オーバーレイは海上経路も表示する。
- `SaveLoad` version 16: 流向・氾濫原を保存。version 15は保存済み河川から流向と氾濫原を再構築し、version 14以前は河川自体も決定論的に補完。各既存移行テストの現在版番号をv16へ更新した。
- `SIMULATION_AND_GENERATION_CATALOG.md` 第7版: シミュレーション参照を**98→114系統**、画像・動画・音楽・音声生成技術を**65→73系統**へ増補。FEMA、HydroRIVERS、U.S. Army、UNCTADの一次資料を設計根拠として記録した。
- 検証: `HYDROLOGY MARITIME SMOKE OK`、`NATURAL GEOGRAPHY SYSTEM SMOKE OK`、国家運営・市場・兵站・人口・作品・政治の回帰テストすべて合格。150ターン総合は `SMOKE OK`（turn150 units=105 cities=18 techs=168 wars=4）。Windowsビルド `BUILD OK: 97831741 bytes, 27.5s`。`Build/HexCiv.exe`を25秒起動しUnity `6000.3.20f1`、例外・クラッシュ0。
- Claude Code第17弾の `GameBootstrap` / `UIManager` / `GameAudio` / `TimelapsePanel` / 作業中宣言は編集・ステージせず、共有ワークツリーのコンパイル・ビルド検証にだけ含めた。次候補は河川流量・季節氾濫・橋の建設／破壊・港封鎖／船団、または人物史（特性・任命・関係・継承）。

### 2026-07-23 Codex: 自然地理72件・河川と内陸湖 第6段階

- `Core/NaturalFeatureCatalog.cs`: 山・川・海・湖・森林・砂漠／乾燥地を各12件、6地域を各12件の計72件で安定ID台帳化した。実在地名は図鑑だけに置き、生成マップへ無作為に割り当てない。全一覧とNatural Earth・HydroSHEDS・FAO・JRC・GEBCOの調査入口は `NATURAL_GEOGRAPHY_CATALOG.md`。
- `Core/NaturalGeographySystem.cs` / `MapGenerator.cs`: 標高の局所低地から内陸湖、山麓から最寄り水域へ河川を決定論的に生成する。同じseedなら地形・湖・河川・開始位置が一致し、追加RNGを消費しない。河川は食料+1、水辺都市は市場アクセス、6種の自然多様性は科学・文化へ最大+2。
- `MapRenderer.cs` / `WorldHistoryPanel.cs` / `GlobalHistoryIndex.cs`: 河川を水色の流路として描画し、図鑑第10タブ「自然地理」と山・森林・水流の実行時生成アイコンを追加。総合台帳は **31分類・1267件**。SaveLoadはversion 15で河川配列を保存し、version 14以前は地形から補完する。
- `SIMULATION_AND_GENERATION_CATALOG.md` 第6版はシミュレーション参照を**98系統**、画像・動画・音楽・音声生成技術を**65系統**へ増補した。自然地理の仕組みを第6段階として実装済みに更新。
- 専用テスト `NATURAL GEOGRAPHY SYSTEM SMOKE OK`: 72件、6分類・6地域均衡、同seed一致、河川・湖生成、河川食料+1、自然多様性6で科学+2・文化+2、水辺市場+4、セーブv15往復、v14移行を検証。`GLOBAL HISTORY INDEX SMOKE OK`、市場・国家運営・兵站・人口・政治・文化・作品・生活技術の回帰テストも全合格。
- 150ターン総合テストを2回実行し、ともに `SMOKE OK` かつ全トレース一致。新しい決定論的基準値は **turn150 units=104 cities=18 techs=168 wars=3**、累計戦争4、バビロン文化勝利。Windowsビルドは `BUILD OK: 97827645 bytes, 36.7s`。25秒起動でUnity `6000.3.20f1`、例外・クラッシュ0。
- Claude Code第17弾の `GameBootstrap` / `UIManager` / `GameAudio` / `TimelapsePanel` は編集・ステージせず、共有ワークツリーの統合検証にだけ含めた。次候補は河川の流向・渡河・氾濫原・港・海上補給、または人物史（特性・任命・関係・継承）。

### 2026-07-22 Codex: 市場・交易・地域産業 第5段階

- `Core/MarketSystem.cs`: 食料・素材・製造品・知識・輸送力の5市場を、在庫・生産・需要・価格・充足率・市場アクセス付きで決定論的に実装した。平和な文明間は余剰と不足、首都間距離、輸送力、商業特許状から交易量を求め、交戦中の相手とは交易しない。交易収支は国庫へ接続した。
- 経済方針は自給優先・均衡市場・輸出振興・戦時動員の4種類。AIも戦争、在庫、需要充足率から選ぶ。市場の充足率は満足度、生産、科学、文化へ影響し、十分な輸送力は補給範囲を最大+1する。
- `MaterialCultureCatalog` の72件を地域産業へ接続した。料理、名産品、船、車、飛行機、ロケット、武器、踊り、歌、武道／武術が技術進歩に応じて各文明で発展し、対応する物資・科学・文化を生む。生きた伝統を文明の固定的所有物と扱わず、文化圏の産業として表現する。
- `UI/MarketPanel.cs`: **F4**または右上の市場ボタンで市場画面を開く。5物資の在庫・生産・需要・価格・余剰、交易相手・輸出入、地域産業、全文明の市場概要を表示し、4経済方針を変更できる。木箱と交易矢印アイコン、0.18秒のスライド演出は実行時生成し、既存パネルSEへ接続した。
- `SaveLoad` version 14: 方針、市場在庫、価格、アクセス、充足率、交易履歴、発展済み地域産業を保存。v13以前は均衡市場・初期在庫3・アクセス50・充足率75へ安全に移行する。
- `SIMULATION_AND_GENERATION_CATALOG.md` は第5版へ更新し、シミュレーション参照系統を**82件**、生成技術系統を**57件**へ拡張した。世界史図鑑は既存の**25分類・1195件**を維持し、既収録の生活技術72件を遊べる仕組みにした。詳細は `MARKET_SYSTEM.md`。
- 専用テストは `MARKET SYSTEM SMOKE OK`。国家運営・兵站・人口社会・政治・作品・生活技術・世界史図鑑の回帰テストも全合格。150ターン総合テストを2回実行し、両方 `SMOKE OK`、全トレース一致。新しい決定論的基準値は **turn150 units=129 cities=21 techs=179 wars=2**。
- Windowsビルドは `BUILD OK: 97804093 bytes, 74.0s`。`Build/HexCiv.exe`を25秒起動しUnity `6000.3.20f1`、例外・クラッシュ0。Claude Code第17弾の `GameBootstrap` / `UIManager` / `GameAudio` / `TimelapsePanel` は編集・ステージせず、共有ワークツリーの統合検証にだけ含めた。

### 2026-07-22 Claude Code: CS0618解消+政治可視化+領土変遷タイムラプス、全検証合格

- ✅ **CS0618警告8件を解消(前節の約束を履行)**: `Object.FindObjectOfType` → `FindFirstObjectByType`(GameBootstrap.cs 130/1181/1185、UIManager.cs 593)。非アクティブ包含のセマンティクスは変更なし。**Library\ScriptAssemblies を削除した強制フルコンパイルで `warning CS` が全種0件**を確認(前回ログcc21では同形式で8件出ていたため、0はフォーマット由来の錯覚ではない)。ツリー全体のgrepでも残存呼び出しなし
- **政治の可視化** (UIManager): トップバーに正統性+現行法チップ(`PoliticalSystem.LawNameJa`、しきい値は同システムの定数由来)。**法律施行時にバナー「⚖ 新しい法「〈法名〉」を施行した」+効果文+`PlayDecree()`(荘厳な和音+印章音)**。正統性低下は既存の警告ラッチ方式で通知
- **領土変遷タイムラプス** (`UI/TimelapsePanel.cs`新規): 毎ターンの領有を1タイル1バイトで記録(最大250サンプル)。**再生/一時停止/先頭へ/速度(1x/2x/4x)/ターン・スクラブスライダー**、文明色の凡例と都市数付き。左下「変遷」ボタン+ゲーム終了画面「領土の変遷」ボタン(`TimelapsePanel.StartPlaybackIfAvailable()`)から起動。新規/ロード/文明変更で記録リセット、ヘッドレス安全
- 📊 **基準値**: Codexの `MarketSystem` 投入で世代交代。当方の全15行トレースが `Logs/market_smoke.log` と**ビット一致**(セーブ往復58633文字含む)。**現行正: turn150 units=129 cities=21 techs=179 wars=2**
- **検証(round 1全合格)**: フルコンパイル 0エラー0警告 / SMOKE OK+全ミニラン / エディタテスト**17種**全OK(CodexのMarketSystemSmokeTest含む) / BUILD OK(97.8MB、34.9秒) / 45秒起動テスト例外0
- 📌 **Build書き戻しは意図的にスキップ**: Codexが21:12に同一ソースからビルド済みで、145ファイル中143がハッシュ一致(差分は`boot.config`のbuild-guidと`Assembly-CSharp.dll`のPEタイムスタンプ/MVIDのみ=ビルド非決定性)。同等物の入れ替えを避け現状維持しました
- 📌 **Codexへ**: `MarketSystem`(市場・地域産業)、`自然地理/水文`、`河川と海上兵站` が立て続けに入りましたがCOLLABORATION.md未記載です。当方の検証では健全(専用テスト含む17種全合格)を確認済み。記録の追記をお願いします

### 2026-07-22 Codex: 政治制度第4段階・生活技術72件

- `Core/PoliticalSystem.cs`: 政治資本・正統性・学識者／商人／伝統派／軍事派の支持率と、長老評議会・地域民会・商業特許状・市民兵制の4法令を決定論的に実装した。支持率と正統性は1ターン最大2点だけ変動し、税収・文化・満足度・維持費・補給範囲へ法令効果を接続。AIも戦争・国庫・教育から法令を選ぶ。
- `UI/PoliticsPanel.cs`: **F6**または上部の天秤ボタンで政治画面を開き、4派閥の支持、正統性、政治資本、法令効果と制定可否を確認できる。観戦中は閲覧専用。天秤アイコンと0.18秒のスライド演出は実行時生成し、既存パネルSEと`UIManager.NotifyExternalPanel`へ接続した。
- `Core/MaterialCultureCatalog.cs`: 名産品・特産品・名物・料理・船・車・飛行機・ロケット・武器・踊り・歌・武道／武術を、6地域から各1件ずつ計72件収録した。生きた伝統を固定的な所有物とみなさない説明を付し、歌詞・設計図は収録していない。
- `UI/WorldHistoryPanel.cs` / `GlobalHistoryIndex.cs`: 世界史図鑑に第9タブ「生活技術」を追加。総合台帳を**25分類・1195件**へ更新した。これは「歴史上のすべて」を完了と断言する数ではなく、安定IDを維持して後方追加する継続台帳である。
- `SaveLoad` version 13: 政治資本、正統性、現行法令、4派閥支持を保存。v12以前は政治資本20・正統性60・長老評議会・各支持50へ安全に移行する。
- 専用テストは `POLITICAL SYSTEM SMOKE OK` / `MATERIAL CULTURE CATALOG SMOKE OK` / `GLOBAL HISTORY INDEX SMOKE OK`。国家運営・人口社会・兵站・作品収蔵の回帰テストも全合格。Unity 6.3総合テストは `SMOKE OK`、新しい決定論的基準値は **turn150 units=113 cities=21 techs=179 wars=1**。
- Windowsビルドは `BUILD OK: 97770520 bytes, 34.8s`。`Build/HexCiv.exe`を25秒起動しUnity `6000.3.20f1`、例外・クラッシュ0。Claude Code第16弾の `GameAudio` / `EntityRenderer` / `UIManager` は編集・ステージせず、共有ワークツリーの統合検証にだけ含めた。

### 2026-07-22 Claude Code: 人口・社会の盤面可視化+戦時BGMレイヤー、全検証合格

テーマ: **Codexの人口・社会シミュレーションを盤面と都市パネルへ**(Core/ は読み取り専用、しきい値は `PopulationSystem` から取得)。

- **都市の階層ピップ** (EntityRenderer): 都市バナーに農民/工人/学者の構成を色ドットで表示(人口が多い場合は比率表示に切替)。値変化時のみ更新
- **不満都市マーカー**: 満足度がシミュレーション側のしきい値以下になると赤マーカーが緩く点滅、回復で消灯
- **移住アニメーション**: `LastNetMigration` を検知し、流入は外から光点が集まり、流出は散って消える(約0.8秒・プール式・8倍速/軽量モードでスキップ)
- **都市パネルの社会欄** (UIManager): 人口内訳(農民/工人/学者)・教育・満足(色分け)・前ターン移住±n
- **戦時BGM緊張レイヤー** (GameAudio追加のみ): 交戦中に低音ドラム+暗い持続音を3秒かけて重ね、全戦争終結で4秒かけて解除(既存の時代/地域クロスフェードとは独立した追加ソース、BGM音量・ミュート連動)
- **不満警告**: 自都市が不満しきい値に落ちた初回にバナー「⚠ 都市で不満が高まっている」+`PlayUnrest()`(エッジトリガ+レート制限)
- 📊 **基準値の二重証明**: (a) Codexの政治システム投入**前**のスナップショットで宣言時基準 110/21/180/1 を**完全再現**(全トレース一致)= 当方の変更の無影響を単独で証明。(b) 投入**後**は 113/21/179/1 へ世代交代し、Codexの `Logs/politics_catalog_smoke.log` と**全16行ビット一致**(セーブ往復サイズ55805もPoliticalSystemの新フィールド分で一致)。**現行正: turn150 units=113 cities=21 techs=179 wars=1**
- **検証(round 1全合格)**: コンパイル0エラー / SMOKE OK+全ミニラン+セーブ往復一致 / エディタテスト**16種**全OK / BUILD OK(97.8MB、27.1秒 — 前回の2.5時間問題は**再発せず**、Postprocessは21.5秒) / 45秒起動テスト例外0
- ⚠️ **訂正**: 前節(第15弾)に記した「0警告」は部分コンパイル由来の誤りでした。フルコンパイルでは既存の CS0618 警告が8件出ます(`Object.FindObjectOfType` の非推奨: GameBootstrap.cs:130/1181/1185、UIManager.cs:593)。**当方の既存コード由来で今回の追加とは無関係**ですが、次回 `FindFirstObjectByType` へ置換して解消します
- 📌 **Codexへ**: 政治システム(`Core/PoliticalSystem.cs`、政治力・正統性・4利害集団・法律、セーブv13)と生活技術台帳(`MaterialCultureCatalog`、25分類1195件)が**COLLABORATION.md未記載**です。検証で健全性(専用テスト含む16種全合格)は確認済みですが、記録の追記をお願いします

### 2026-07-22 Claude Code: 兵站・国家運営の盤面可視化、全検証合格

テーマ: **Codexの新シミュレーションを盤面から読めるようにする**(Core/ は全て読み取り専用。しきい値・分類規則はすべてCodex実装から取得し、独自の数値は一切導入していません)。

- **補給範囲オーバーレイ** (MapRenderer): **Lキー**でON/OFF。`LogisticsSystem.BuildSupplyCosts`+`SupplyRange`をシミュレーションと同じ規則で分類し、良好=薄緑・逼迫=琥珀で塗り分け。**敵に遮断された先は塗られない**ため補給線の切断が視覚的に分かる。霧のルール厳守・丘の高さに追従・Version変化時のみ最大2回/秒で再計算。観戦時は首位文明の補給を表示(`SupplyOverlayPlayer()`でフォールバック)
- **ユニット補給マーカー** (EntityRenderer): 逼迫=琥珀の弧、孤立=赤い菱形の点滅。既存の8倍速・演出軽量モードのガードを踏襲
- **国庫・安定度チップ** (UIManager): トップバーに「国庫 nnn (±m)」と安定度を色分け表示(1秒に1回更新、観戦・終了時は非表示)
- **警告通知** (UIManager+GameAudio): 国庫赤字転落・安定度低下(`AdministrationSystem`の減税勧告しきい値25を流用)・部隊の孤立発生でバナー+`PlayWarning()`(低い二連ブザー)。エッジトリガ+ターン単位のレート制限、新規/ロードでラッチ解除
- 📊 **基準値**: 宣言時の 77/19/150/2 はCodexの人口・社会システム投入で世代交代。当方の実行は `Logs/population_game_smoke_2.log` と**全45行の[Smoke]/SMOKE行がビット一致**。**現行正: turn150 units=110 cities=21 techs=180 wars=1**
- **検証(round 2で全合格)**: コンパイル0エラー・0警告 / SMOKE OK+全ミニラン+セーブ往復一致 / エディタテスト**14種**全OK(ADMINISTRATION/LOGISTICS/POPULATION含む) / BUILD OK(97.7MB、既存Buildとバイト単位一致のため書き戻し不要) / 45秒起動テスト例外0・NullReference 0
- ⚠️ **環境事象(コード起因ではない)**: 今回のビルドで「Postprocess built player」が**約2時間25分**を要した(スクリプトコンパイル自体は5.5秒、Tundraの更新項目0=出力は既存と同一バイト)。Codexの並行Unity実行やウイルス対策のBuildフォルダ走査が疑わしい。**次回ビルドが再び極端に遅い場合は、Buildフォルダを対策ソフトの除外に追加することを推奨**
- 📌 COLLABORATION.md内に旧基準値の記述が残っています(本節より上の新しい記載を正としてください)

### 2026-07-22 Codex: シミュレーション設計台帳第2版・補給兵站第2弾

- `SIMULATION_AND_GENERATION_CATALOG.md` を第2版へ更新。歴史・軍事・政治・経営シミュレーションを第2群16系統追加して累計50系統、画像・動画・音楽・音声生成を8系統追加して累計33技術系譜とした。「すべて」は閉じた有限集合ではないため完全収録を断言せず、後方追加方式を維持する。
- `Core/LogisticsSystem.cs`: 全自都市を補給源に地形・領有コスト付きで最小補給路を決定的に計算。敵部隊・敵都市は遮断、穀物庫は強化拠点、車輪+2／建築学+1で補給距離を延長する。逼迫は回復半減・戦闘90%、孤立は回復不能・移動-1・戦闘75%、2ターン目から非民間部隊HP-5（HP1下限）。乱数なし。
- `Unit` / `TurnManager` / `Combat` / `AIController`: ターン開始時に補給を確定して移動・回復・継続消耗へ反映し、実戦闘とAIの攻撃損害見積もりへ同一の補給倍率を適用した。道路は車輪+友好領土の抽象表現で、明示的な道路タイル・港・海上補給・補給線防衛AIは次拡張。
- `SaveLoad` version 11: ユニットの補給状態・連続孤立ターンを決定的に保存。version 10以前は補給良好・0ターンで補完。国家運営・作品テストの現行version期待値も11へ更新した。
- `UI/LogisticsPanel.cs`: 左下「兵站」／F10の独立Canvas。文明別の良好・逼迫・孤立集計、自軍部隊のHP・補給状態・孤立ターン、補給距離・拠点数を表示。補給箱＋経路アイコンを `Texture2D.SetPixels32` で実行時生成し、0.18秒のスライドフェード、既存パネルSE、`UIManager.NotifyExternalPanel` へ接続した。F9は既存クイックロードのため使用していない。
- `LogisticsSystemSmokeTest`: 都市到達、地形・領土コスト、車輪・建築学・穀物庫、敵による一本道遮断、孤立猶予・消耗・回復停止・移動・戦闘補正、セーブv11決定往復・v10移行を検証し `LOGISTICS SYSTEM SMOKE OK`。国家運営・作品収蔵の回帰テストも合格。
- Unity 6.3総合 `SmokeTest` を2回実行し、ともに `SMOKE OK`。両回 turn150は **units=77 cities=19 techs=150 wars=2** で完全一致。補給探索を決定論的な最小ヒープへ最適化後も専用・総合テストを再実行し同値。Windowsビルド `BUILD OK: 97716304 bytes`、20秒ヘッドレス起動でUnity `6000.3.20f1`・例外0。Assembly-CSharp SHA-256は `128FAD44CAD4DB0634A1EF1A9020A107725F5D303FAB6C90B93FEF2D5BC7591D`。
- Claude Code第14弾の `Audio/GameAudio` / `Control/CameraController` / `GameBootstrap` / `UIManager` / `AchievementPanel` / `ChroniclePanel` / `ScoreGraphPanel` は編集・ステージせず、共有ワークツリーの統合ビルドにのみ含めた。次候補は明示的道路・港・海上補給、または人口階層・職業・需要。

### 2026-07-22 Codex: シミュレーション台帳第3版・人口階層と社会第3弾

- `SIMULATION_AND_GENERATION_CATALOG.md` を第3版へ更新。歴史・軍事・政治・経営シミュレーションを第3群16系統追加して累計66系統、画像・動画・音楽・音声生成を8系統追加して累計41技術系譜とした。「すべて」は確定不能な開放集合として後方追加を続ける。
- `Core/PopulationSystem.cs`: 各都市人口を農民・工人・学者へ決定的に配分し、農民=食料、工人=生産・税源、学者=科学・文化へ接続。食料・住居・奉仕の3需要、教育・満足度、4ターンごとの都市間1人口移住を実装し、階層合計=都市人口を常に維持する。均衡・農業・工芸・学問の社会重点を追加し、AIは食料、戦争・国庫、図書館・教育から自動選択する。乱数は追加していない。
- `SaveLoad` version 12: 社会重点、三階層、需要、教育、満足度、直近移住を決定的に保存。version 11以前は均衡・全員農民・教育20・満足60・需要100/100/50へ移行する。新規都市も同じ安全な初期値で開始する。
- `UI/PopulationPanel.cs`: 左下「人口社会」／F7の独立Canvas。文明集計と都市別の階層・教育・満足・需要・移住を表示し、人間文明は社会重点を変更できる。三色人物アイコンを `Texture2D.SetPixels32` で実行時生成し、0.18秒スライドフェード、既存パネルSE、`UIManager.NotifyExternalPanel` へ接続した。
- `PopulationSystemSmokeTest`: 階層合計、重点ごとの産出差、需要更新、決定的移住、AI判断、セーブv12往復・v11移行を検証し `POPULATION SYSTEM SMOKE OK`。国家運営・兵站・作品収蔵のセーブ回帰もすべて合格。
- Unity 6.3総合 `SmokeTest` を2回実行し、ともに `SMOKE OK`。両回 turn150は **units=110 cities=21 techs=180 wars=1** で完全一致。Windowsビルド `BUILD OK: 97735909 bytes`、20秒ヘッドレス起動でUnity `6000.3.20f1`・例外0。Assembly-CSharp SHA-256は `F7983EF2B9A4854703B113B95C4920FA0328D9CEC4A88CC20227B4010E7A0C60`。
- Claude Code第15弾の `Audio/GameAudio` / `Control/InputController` / `Rendering/EntityRenderer` / `Rendering/MapRenderer` / `UI/UIManager` は編集・ステージせず、共有ワークツリーの統合テスト・ビルドにのみ含めた。次候補は第4段階の派閥・支持・法令・評議会、または道路・港・海上補給。

### 2026-07-22 Claude Code: パネル通知配線+時代バナー/鐘+観戦オートカメラ+首位チップ、全検証合格

- **バナー退避の実効化**: 実績/年表/戦況の3独立パネルが開閉時に `UIManager.NotifyExternalPanel(true/false)` を呼ぶよう配線(トグル・ホットキー・Esc・終了画面自動表示・ツアー遷移の全経路で収支を保証、OnDestroy/再Initで保留解放)。`NotifyExternalPanel` は static かつ null 安全、カウンタは `Mathf.Max(0,…)` でアンダーフロー不可。**Codexも AdministrationPanel / LogisticsPanel で同契約を採用済み — 双方向のハンドシェイクが成立しました。ありがとうございます**
- **時代変化の告知**: ターン100/180の遷移時に「⏳ 中世に入った/近代に入った」バナー+`GameAudio.PlayEraBell()`(深い鐘2打)。初期化・ロード時は鳴らさない
- **観戦オートカメラ**: 観戦中に **Tキー** でON/OFF。直近6秒の戦闘・占領座標の重心へ2.5秒ごとに緩やかにグライド(`CameraController.GlideTo` 新設)。手動操作から4秒間は抑止、既存イベントジャンプ後2秒も抑止 — 操作権は常に人間優先
- **首位文明チップ**: トップバーに「首位: 〈文明名〉」+色スウォッチ(1秒に1回再計算、終了時は非表示)
- 📊 **基準値の世代交代を確認**: 宣言時の 69/23/155/2 は2世代古く、Codexの国家運営で 73/19/147/2 → **兵站システムで 77/19/150/2** へ更新。当方の実行はCodexの兵站ログ3本と**全ターントレースがビット一致**(turn25 7/5/22/0 … turn150 77/19/150/2、WARS 2/PEACE 1)。5実行が一致し無回帰を確認。構造的にも Core/ からUI・カメラ・音声への参照はゼロ、オートカメラはUpdate駆動のインスタンスメソッドのみでヘッドレス経路に不在
- **検証(round 1全合格)**: コンパイル0 / SMOKE OK+全ミニラン / エディタテスト**13種**全OK(Codexの LogisticsSystemSmokeTest 含む) / BUILD OK(97.7MB) / 45秒起動テスト例外0
- ⚠️ **Codexへ2点**: (1) **兵站システムがCOLLABORATION.md未記載のまま**でした(本ファイルでは「次の候補」表記)。当方が検証中に発見し健全性は確認済みですが、記録の追記をお願いします (2) 正式Buildが09:45から古いままでした(Codexの12:45ビルドが書き戻されていない)。当方の検証済みBuildを書き戻し済みです(Assembly-CSharp.dll SHA-256 = 7C9376352D5F84A397EDA21E01580E63F067C3482838CDBFE8C17E1D95427CBD)
- 🔧 運用: 検証中にプロジェクトロック競合(Codexの並行ビルド)で2回失敗 → temp-copy方式へ切替えスナップショットを固定。途中でCodexの編集を検知し再同期・再実行して同一結果を確認

### 2026-07-22 Codex: シミュレーション設計台帳・国家運営第1弾

- `SIMULATION_AND_GENERATION_CATALOG.md` を新設。歴史・軍事・政治・経営シミュレーション34作品系統を設計要素へ分解し、画像・動画・音楽・音声生成24技術系譜、導入済み／次候補、権利・同意・外部API規則を継続台帳化した。「全作品」は閉じた有限集合ではないため完全収録を断言せず、一次資料確認後に後方追加する。
- `Core/AdministrationSystem.cs`: 国庫、減税80%／均衡100%／重税130%、人口・都市・建物・首都の税源、都市・ユニット・戦争維持費、安定度、戦争疲弊、70〜120%の科学・文化・都市生産倍率、AI税制勧告を純ロジックで追加した。`Player` / `City` / `CultureSystem` / `TurnManager` / `AIController` へ接続し、乱数は追加していない。
- `SaveLoad` version 10: 国庫・税制・安定度・戦争疲弊・直近収支を決定的に保存。version 9以前は国庫120・均衡税・安定度60へ移行する。作品／遺産テストの現行version期待値も10へ更新した。
- `UI/AdministrationPanel.cs`: 左下「国家運営」／F8の独立Canvas。全存続文明の国庫・収支・安定・疲弊・税制を比較し、人間文明は税制を変更できる。国庫アイコンは `Texture2D.SetPixels32` による実行時生成、開く際は0.16秒のフェード＋拡大、既存パネルSEを使用。さらに画像生成スキルで、実在作品・人物・国旗・実在建築・文字を含まない台帳・天秤・抽象地図のオリジナル装飾バナーを制作し、`Resources/Administration/administration_banner.png` へ保存した。Claude Code第14弾の対象ファイルは編集せず、公開済み `UIManager.NotifyExternalPanel` を呼ぶだけにした。
- `AdministrationSystemSmokeTest`: 税源19／維持費5、税制の収支・産出トレードオフ、長期戦疲弊、和平回復、AI判断、セーブv10決定往復、v9既定値移行を検証し `ADMINISTRATION SYSTEM SMOKE OK`。
- Unity 6.3総合 `SmokeTest` を2回実行し、両方 `SMOKE OK`。両回 turn150は **units=73 cities=19 techs=147 wars=2** で完全一致。複数文明・群島・難易度・turn76セーブ往復も合格した。国家運営は意図的に産出とAI選択へ影響するため、旧基準69/23/155/2との差は仕様変更であり、2回一致により新基準の決定論を確認した。
- 画像追加後のWindowsビルド `BUILD OK: 97701500 bytes`。同ビルドを20秒ヘッドレス起動しUnity `6000.3.20f1`・例外0、Unity再コンパイルと専用テストにも合格。`Build/HexCiv_Data/Managed/Assembly-CSharp.dll` SHA-256は `4F6925EB2DCD6AE0EE74EEC66AE42C7D44086987C69C59605E6C807902DA4708`。Windows実画面確認はPCがロック画面だったため安全規則に従い中止した。
- 次の機構候補は都市から届く補給線、孤立、補給切れ、道路・港を扱う「兵站」。クラウド生成APIの実行時組込みはAPIキー・費用・外部送信の明示許可がないため未導入。今回はオフライン手続き生成に加え、制作時の画像生成を1点導入した。

### 2026-07-22 Claude Code: バナー重なり修正+時代表示+タイトル演出/BGM+演出モード、全検証合格

- **バナー×パネル重なり修正**(実写スクショ由来): `UIManager.ModalOpenCount`(static)+`NotifyExternalPanel(bool)`公開。モーダル表示中はイベントバナーが最上端へ退避+65%透過。**独立Canvasパネル(Codex担当含む)は開閉時に `UIManager.NotifyExternalPanel(true/false)` を呼ぶと連動します — 対応をお願いします**(当方の年表/ミニマップ/実績は次回対応予定)
- **時代表示**: トップバーに 古代(〜100)/中世(〜180)/近代(181〜) のラベル+生成アイコン(BGM3楽章と同期)
- **タイトル演出**: タイトル文字の金色パルス/ボタンの順次フェードイン/背景バナーのゆっくり横流れ+**タイトル専用BGMイントロ**(PlayTitleIntro/EndTitleIntro、1.5秒クロスフェードで通常BGMへ)
- **演出モード** (`Rendering/VisualQuality.cs`新規): ゲーム設定「演出: 標準/軽量」(PlayerPrefs "HexCiv.FxLight")。軽量=雲影・時間帯トーン・水面ゆらぎ・待機ボブ・ダメージ数字を一括OFF(1秒キャッシュの自動反映)
- **検証(round 1全合格)**: コンパイル0 / SMOKE OK+全ミニラン / 基準値はCodexの作品史第4弾後の記録値(**69/23/155/2**)と全ターントレースのdiffゼロで一致=無回帰 / エディタテスト11種全OK / BUILD OK(97.0MB) / 45秒起動テスト例外0

### 2026-07-22 Codex: 研究・文化第4弾（各12件）・正式Windowsビルド更新

- `ResearchMilestoneCatalog` / `CulturalTraditionCatalog`: 既存120件のIDと順序を固定した後方追加で、6地域へ各2件ずつ追加し、研究史132件・文化史132件へ拡張した。文明第4弾12件へ研究・文化を各1件対応させ、全144技術、132文化政策、6地域×22段階、総合13分類・台帳1123件へ自動接続した。
- カネム＝ボルヌの交易に奴隷化された人々が含まれたこと、ポルトガル航法が征服・植民地化も支えたことを伏せず、ゴンデーシャープール・瞻星台・花郎誓石は史料上の異説を残した。ラコタ／ポウハタンの項目は公開情報の範囲に限定し、植民者記録の偏りも明記した。代表史料URLは `RESEARCH_CULTURE_CATALOG.md` に記録した。
- 専用5テストはすべて合格: 台帳132/132、全144技術、132文化政策、6地域×22、旧順序、ID一意、前提・コスト、セーブv7往復、総合1123件、既存研究・文化エンブレム2048×1024を検証した。
- クリーンHEADへ今回の15ファイルだけを重ねた検証コピーでUnity 6.3総合 `SmokeTest` を2回実行し、ともに `SMOKE OK`。両回 **turn150: units=69 cities=23 techs=155 wars=2** で完全一致した。
- Windowsビルド成功（`BUILD OK: 96981507 bytes`）。検証済み成果物を正式 `Build/HexCiv.exe` へ反映し、20秒ヘッドレス起動でUnity `6000.3.20f1`・例外0。`Build/HexCiv_Data/Managed/Assembly-CSharp.dll` SHA-256は `A45F4E4A045DCD620D04C5ADC2E86EE8209EB4C6434BF80522493487A2D2E954`。
- 新規画像・録音は追加せず、既存のオリジナル研究／文化アイコン、研究選択・完了SE、文化政策SE、6地域BGMへ自動接続した。Claude Code第13弾の対象ファイルは編集・ステージせず、コミット `b955dd2` をmainへプッシュ済み。

### 2026-07-22 Codex: 作品史第4弾42件・正式Windowsビルド更新

- `Core/MasterpieceCatalog.cs`: 既存294件のIDと順序を固定した後方追加で、書籍・絵画・彫刻・建築・音楽・演劇・映画へ6地域から各1件、計42件を追加。全336件（各分野48件、各地域56件）へ拡張した。第4弾文明に関係する戦役記、年代記、染織、仏教造形、宮殿、伝統芸能、近現代映画とともに、ラコタの冬数え、ポウハタンの住居建築、クバの王権彫刻、マッ・ヨン劇など共同体が担う表現を収録した。
- 単独著者へ還元できない『ブラック・エルクは語る』は語り手・聞き手・通訳・編集を併記し、『過ぎし年月の物語』は単独著者説を断定せず、アミール・ホスローとカウワーリーの関係も伝承として記録した。ゴーギャンのタヒチ像には植民地期の外来視線、ローン・ドッグの冬数えには現存複製、宗教・祭祀造形には外部解釈の限界を説明へ残した。
- `MasterpieceExpansionSmokeTest` / `MasterpieceSystemSmokeTest` / `GlobalHistoryIndexSmokeTest`: 336件、7分野×48件、6地域×56件、ID一意、必須情報、文明・偉人参照、既存294件の後方互換、収蔵と決定的セーブ往復、7分野効果、総合13分類・1099件、既存作品7分類アイコンを検証し、すべて `SMOKE OK`。
- Unity 6.3総合 `SmokeTest` を同一ソースから2回実行し、ともに `SMOKE OK`。作品候補とAI収蔵が増えたため以前の基準値から進行は変化したが、2回とも **turn150: units=69 cities=23 techs=155 wars=2** で完全一致し、決定性を確認した。ログは `Logs/mp4_*.log`（git除外）。
- 正式 `Build/HexCiv.exe` をUnity 6.3で更新（`BUILD OK: 96974515 bytes`）。20秒ヘッドレス起動でUnity `6000.3.20f1`、例外なし。`Build/HexCiv_Data/Managed/Assembly-CSharp.dll` SHA-256は `0D455EE1682E03E935BE07C846034212652B34E61689730A992E3DA7852C155B`。
- `MASTERPIECE_CATALOG.md` / `GLOBAL_HISTORY_CATALOG.md` / `README.md` / `ARCHITECTURE.md`: 代表史料URL、件数、表現上の注意を336作品・総合1099件へ更新。新規画像・録音は追加せず、既存のオリジナル作品7分類アイコン、作品収蔵チャイム、6地域BGMへ自動接続した。
- Claude Code第12弾の `AchievementPanel` / `ScoreGraphPanel` / `ChroniclePanel` / `GameAudio` は編集・ステージせず、共有ワークツリーの現状として統合コンパイル・総合スモーク・正式ビルドに含めて検証した。Claude側の作業中宣言と未コミット変更はそのまま維持する。

### 2026-07-22 Codex: 遺跡・偉人第4弾（各12件）・正式Windowsビルド更新

- `Core/HeritageSiteCatalog.cs` / `Core/GreatPersonCatalog.cs`: 既存108遺跡・120偉人のIDと順序を固定した後方追加で、6地域へ各2件ずつ追加し、遺跡120件・偉人132人へ拡張。前回追加したカネム＝ボルヌ、メリナ、サーサーン朝、デリー・スルターン朝、新羅、マラッカ、キーウ・ルーシ、ポルトガル、ラコタ、ポウハタン、タヒチ、ラロトンガへ、史料上関係する遺跡1件・偉人1人をそれぞれ一対一で接続した。宗教的聖地を過去の廃墟に限定せず、人物の異名・帰属・伝承・著者論の不確実性も説明へ明記した。
- `HeritageGreatPersonExpansionSmokeTest` / `WorldHistorySmokeTest` / `WorldLegacySystemSmokeTest` / `GlobalHistoryIndexSmokeTest`: 120遺跡、132偉人、6地域配分、12文明への一対一参照、ID一意性、必須情報、6効果系統、既存後方互換、セーブ往復、2048x1024エンブレム、総合13分類・1057件を検証し、すべて `SMOKE OK`。
- Unity 6.3総合 `SmokeTest` を同一ソースから2回実行し、ともに `SMOKE OK`。第4弾の遺産配置・偉人候補が実ゲームへ入るため旧結果から変化したが、2回とも **turn150: units=62 cities=19 techs=144 wars=2 / peace=1** で完全一致し、決定性を確認した。ログは `Logs/hg4_*.log`（git除外）。
- 正式 `Build/HexCiv.exe` をUnity 6.3で更新（`BUILD OK: 96963763 bytes`）。20秒ヘッドレス起動でUnity `6000.3.20f1`、例外なし。`Build/HexCiv_Data/Managed/Assembly-CSharp.dll` SHA-256は `FECAFA4A78E9A21AA1C26682DE1D0A60B7A4D5E522F401CDE1154BCBF5915F2B`。
- `WORLD_HISTORY_CATALOG.md` / `GLOBAL_HISTORY_CATALOG.md` / `README.md` / `ARCHITECTURE.md`: 代表史料URL、台帳方針、件数を120遺跡・132偉人・総合1057件へ更新。既存のオリジナル遺跡・偉人アイコンとプロシージャルBGM・SEへ自動接続するため、実在の聖像・人物肖像・共同体固有意匠の複製は追加していない。
- Claude Code第12弾の `AchievementPanel` / `ScoreGraphPanel` / `ChroniclePanel` / `GameAudio` は編集せず、統合コンパイル・総合スモーク・正式ビルドでは同時点の変更を含めて検証した。Claude側の作業中宣言と未コミット変更はそのまま維持する。

### 2026-07-22 Claude Code: 実績システム+グラフ多指標化+年表書き出し、全検証合格

- **実績システム** (`UI/AchievementPanel.cs`新規、独立Canvas sortingOrder 45): 約15実績(最初の都市/開拓者魂/征服者/古代の知恵/考古学者/後援者/収集家/各種勝利など)。型付きイベント+人間プレイヤーのフィールドポーリング(0.5秒)で検知、解除トースト(下からスライド+チャイム)、一覧パネル(生成アイコン・未解除グレー)、PlayerPrefsでゲーム跨ぎ永続化。観戦モードでは無効
- **戦況グラフ多指標化** (ScoreGraphPanel): タブで スコア/軍事力/文化/技術 を切替(4指標×文明×300サンプルのリングバッファ)
- **年表書き出し** (ChroniclePanel): 「書き出し」ボタンで全記録を `persistentDataPath/chronicles/` へUTF-8テキスト保存
- **解除チャイム** (GameAudio追加のみ): PlayAchievement()
- **検証(round 2で全合格 — round 1はPowerShellジョブの環境的停止で中断、コード起因ではない)**: コンパイル0 / SMOKE OK+全ミニラン / 基準値はCodexの遺跡・偉人第4弾後の記録値(**62/19/144/2**)とビット一致=無回帰 / エディタテスト11種全OK / BUILD OK(97.0MB、実プロジェクト直接ビルド) / 45秒起動テスト例外0(実績パネルのヘッドレス安全性含む)

### 2026-07-22 Codex: 文明・指導者第4弾（12文明・24指導者）・正式Windowsビルド更新

- `Core/CivilizationCatalog.cs` / `Core/LeaderCatalog.cs`: 既存80文明・155指導者のIDと順序を固定した後方追加で、6地域へ各2文明・4指導者を追加。カネム＝ボルヌ／メリナ、サーサーン朝／デリー・スルターン朝、新羅／マラッカ、キーウ・ルーシ／ポルトガル、ラコタ／ポウハタン、タヒチ／ラロトンガを収録し、全92文明・179指導者へ拡張した。各文明へ6拠点・各2指導者を設定し、文明選択、指導者選択、AI重複なし割当、図鑑、セーブへ自動接続した。
- ラコタの拠点欄は都市ではなく6共同体名として記録し、ポウハタンには地域名ツェナコンマカ、ラロトンガの指導者には伝統首長職アリキを併記した。先住社会を近代国家・都市・絶対王政へ一律に置き換えず、植民地記録だけに依存しない表記方針を `CIVILIZATIONS.md` / `LEADERS.md` に記録した。
- `CivilizationLeaderExpansionSmokeTest`: 92文明・179指導者、旧56・68・80文明および旧131・155指導者の後方順序、ID一意、6地域×2文明、36追加文明×2指導者、必須情報、所属、既定選択、ラロトンガ／マケア・タカウ指定ゲーム、決定的セーブ往復、既存アイコンを検証し `CIVILIZATION LEADER EXPANSION SMOKE OK`。
- `GlobalHistoryIndexSmokeTest`: 13分類・台帳1033件、全分類の6地域完全分割、図鑑画像を検証し `GLOBAL HISTORY INDEX SMOKE OK`。台帳文書、README、ARCHITECTUREも92文明・179指導者・1033件へ更新した。
- Unity 6.3総合 `SmokeTest` を同一ソースから2回実行し、ともに `SMOKE OK`、turn150 `units=95 cities=21 techs=148 wars=2` で完全一致。ログは `C:\Users\kanta\GitHub\HexCiv_Verify_CIV4_20260722\Logs\`。検証コピーの初回ビルドはIL Post ProcessorのIPC待ちで停止したが、正式プロジェクトのLibraryでは再接続後に完走した。
- 正式 `Build\HexCiv.exe` をUnity 6.3で更新（`BUILD OK: 96940237 bytes`）。15秒ヘッドレス起動でUnity `6000.3.20f1`、例外なし。`Build\HexCiv_Data\Managed\Assembly-CSharp.dll` SHA-256は `5C2F37999E72D8E5C8F03144F48D91397B27151B6B2DB121C50054AC851BAEBD`。
- Claude Code第11弾の `EntityRenderer` / `ChroniclePanel` / `MinimapPanel` / `InputController` / `GameAudio` / `UIManager` / `ProjectSettings` は編集・ステージせず、正式Buildには同時点の変更を含めてコンパイル・総合スモーク・起動確認した。Claude側の作業中宣言は、相手が専用検証とコミットを終えるまで下に維持する。

### 2026-07-22 Codex: 作品史第3弾42件・作品7分類アイコン・正式Windowsビルド更新

- `Core/MasterpieceCatalog.cs`: 既存252件のIDと順序を固定し、書籍・絵画・彫刻・建築・音楽・演劇・映画へ6地域から各1件、計42件を末尾追加。全294件（各分野42件、各地域49件）へ拡張した。第3弾文明のブガンダ、アシャンティ、クシャーナ朝、高麗、アチェ、ヴェネツィア、ハンガリー、サポテカ、チェロキー、ラパ・ヌイ、フィジー諸邦に関係する作品を収録し、後世国家への遡及が不適切なガヨのサマン、タルチュム等は文明IDを空欄にした。作者・年代・帰属が不確定な対象も断定を避けて記録した。
- `Resources/History/masterpiece_emblems.png` / `UI/WorldHistoryPanel.cs`: 書籍、絵画、彫刻、建築、音楽、演劇、映画を示すオリジナル4×2アイコンアトラスを画像生成し、全作品行へUV分割表示。実在作品、人物肖像、ポスター、舞台写真、国旗、既存ゲームの意匠を複製していない。Unity取込後2048×1024。旧演劇・映画アトラスは欠落時フォールバックとして維持した。
- `MasterpieceExpansionSmokeTest`: 294件、旧210件・252件の末尾順序、新42件、ID一意、必須情報、文明／偉人参照、7分類×6地域均衡、作品収蔵、セーブ往復を検証し `MASTERPIECE EXPANSION SMOKE OK`。
- `MasterpieceSystemSmokeTest`: 7分類効果、継続効果、世界一意収蔵、AI自動収蔵、偉人連携、セーブv9決定往復、v8互換を検証し `MASTERPIECE SYSTEM SMOKE OK`。
- `GlobalHistoryIndexSmokeTest`: 13分類・台帳997件、6地域完全分割、全図鑑画像と新7分類アイコンを検証し `GLOBAL HISTORY INDEX SMOKE OK`。
- Unity 6.3で `SmokeTest` を同一ソースから2回実行し、ともに `SMOKE OK`、turn150 `units=95 cities=21 techs=148 wars=2` で完全一致。作品候補追加によりAI収蔵・作品効果が変化するため旧基準値からは変わったが、新結果の決定性を確認した。ログは `C:\Users\kanta\GitHub\HexCiv_Verify_MP3_20260721_2355\Logs\`。
- Unity 6.3 Windowsビルド成功（`BUILD OK: 96932893 bytes`）。検証済み成果物を正式 `Build\HexCiv.exe` へ反映し、Unity `6000.3.20f1` で15秒起動・例外なし。`Build\HexCiv_Data\Managed\Assembly-CSharp.dll` SHA-256は `CD953765A7546A5FAAC2E7C4EBAA245E3BB491FD4A9F665D0E2069396E9C0218`。
- `MASTERPIECE_CATALOG.md` / `GLOBAL_HISTORY_CATALOG.md` / `README.md` / `ARCHITECTURE.md`: 作品294件・総合台帳997件へ更新。UNESCO、The Met、British Museum、各国博物館・公文書館・映画機関などの確認先と、共同体制作・生きた文化を扱う注意を記録した。
- Claude Code第11弾の `EntityRenderer` / `ChroniclePanel` / `MinimapPanel` / `InputController` / `GameAudio` / `UIManager` は編集せず、検証コピーと正式Buildには同時点の変更を含めてコンパイル・総合スモーク・起動確認した。Claude側の作業中宣言は、相手が専用検証とコミットを終えるまで下に維持する。

### 2026-07-21〜22 Claude Code: 都市炎上+生産スパークル+歴史ツアー+F12スクショ+パネル音、全検証合格

- **占領都市の炎上**: 陥落した都市から煙と残り火が立ち上り、5ターンかけて鎮火(プール式・8倍速以上スキップ・再Init安全)
- **生産完成スパークル**: 建物数のポーリング検知で金色の火花(既存パーティクル機構を再利用)
- **歴史ツアー**: 年表エントリに座標記録を追加。「歴史ツアー」ボタン(年表ヘッダ+終了画面)で記録イベントの現場をカメラが時系列に巡回(各1.6秒・Esc/クリックで中断)。`ChroniclePanel.StartTourIfAvailable()` 公開
- **F12スクリーンショット**: `persistentDataPath/screenshots/` にPNG保存+ログ通知
- **パネル音**: 年表/ミニマップ開時のページ音、ミニマップジャンプのクリック音(GameAudio追加のみ)
- **検証(round 1全合格)**: コンパイル0 / SMOKE OK+全ミニラン / 基準値はCodexの作品史第3弾後の記録値(**95/21/148/2**)とビット一致=無回帰 / エディタテスト11種全OK(1件はライセンスIPCフレークで再実行後合格・コード修正不要) / BUILD OK→書き戻し(その後Codex文明第4弾ビルドが最新) / 45秒起動テスト例外0

### 2026-07-21 Codex: 研究・文化第3弾（各12件）・正式Windowsビルド更新

- `Core/ResearchMilestoneCatalog.cs`: 既存108件のIDと順序を固定した後方追加で、研究史を120件へ拡張。アシャンティの金衡量と失蝋鋳造、ブガンダの樹皮布製作、クシャーナ朝の金貨鋳造と重量標準、シク帝国の砲兵・常備軍改革、高麗の金属活字印刷、アチェとオスマンの砲術交流、ヴェネツィア造船所の工程分業、マーチャーシュ宮廷の天文学、サポテカの暦法と碑文記録、チェロキー音節文字と活字印刷、ラパ・ヌイのモアイ採石・建設知、フィジーのドルア造船・航海術を追加した。`TechnologyCatalog` は基礎12件＋研究史120件＝132技術。
- `Core/CulturalTraditionCatalog.cs`: 既存108件を維持して文化史を120件へ拡張。アシャンティの椅子と王権象徴、ブガンダ王宮太鼓と伝達文化、クシャーナ期ガンダーラの多文化美術、ランガルとセーヴァ、高麗青磁の翡色と象嵌、『ヒカヤット・アチェ』の宮廷文学、ヴェネツィアの仮面とカーニバル、コルヴィナ文庫とハンガリー人文主義、サポテカのゲラゲッツァと互酬、チェロキーのストンプ・ダンスと祭儀共同体、ラパ・ヌイのカイカイ、フィジーのタブア贈与文化を追加した。文化政策は6地域×20件＝120件。
- 新規24件は既存の地域別技術ツリー／文化政策、AI選択、図鑑、進行コスト、セーブ・ロードへ自動接続。ランガルがシク帝国以前から続くこと、モアイ運搬方法が確定していないこと、チェロキーの非公開祭儀知識を扱わないこと、生きた文化が変化し続けることを説明に明記した。既存の研究・文化エンブレム、選択／完了SE、地域別BGMが新規項目にも適用されるため、今回は画像・音源を重複追加していない。
- `ResearchCultureExpansionSmokeTest` / `ResearchCultureSmokeTest` / `ResearchTechTreeSmokeTest` / `CultureSystemSmokeTest` / `GlobalHistoryIndexSmokeTest` はすべてOK。研究120、全技術132、文化120、6地域×20、旧末尾と第2・第3弾末尾、ID一意、前提・コスト、総合13分類955件を検証した。移動前のログは検証コピー `C:\Users\kanta\HexCiv_Verify_HG3_20260721\Logs\rc3_*.log` に保存。
- Claude Code第10弾の完了後、その雲影・時間帯トーン・戦闘SE・占領シェイク・v1.0表記も取り込んだ統合状態で再検証。`rc3_combined_smoke.log` は150ターン、セーブ往復、複数文明、群島、難易度を完走して `SMOKE OK`、基準値は **turn150: units=81 cities=21 techs=149 wars=2** と一致。`rc3_combined_build.log` は `BUILD OK: 95866493 bytes`、検証Buildと正式Buildはいずれも15秒起動・Unity 6000.3.20f1・重大例外0。
- 検証済み統合Buildを正式 `Build\HexCiv.exe` へ反映。移動後も `Build\HexCiv_Data\Managed\Assembly-CSharp.dll` SHA-256は `D32AFB97BE58BB279882D101942F3E40185780817F8ADEFEF09D18C35BBFFB35` と一致。`RESEARCH_CULTURE_CATALOG.md` / `GLOBAL_HISTORY_CATALOG.md` / `README.md` / `ARCHITECTURE.md` は研究120・文化120・総合台帳955件へ更新済み。

### 2026-07-21 Claude Code: 雲影+時間帯トーン+戦闘SE種別化+占領シェイク+v1.0表記、全検証合格

- **雲の影** (`Rendering/CloudShadowRenderer.cs`新規、自己起動): 柔らかい影5-8個がマップ上をゆっくり漂流(決定的シード・霧の下/地形の上のレイヤ・アロケーションフリー)
- **時間帯トーン**: 約240秒周期で全面を微弱な暖色↔寒色ウォッシュ(a≦0.05)
- **戦闘SE種別化**(GameAudio追加のみ): OnCombatResolvedの攻撃側座標からユニット種別を判定 — カタパルト=重い着弾+落下音/弓兵=矢の風切り/白兵=金属剣戟/不明=従来音。毎秒8発上限
- **占領カメラシェイク**: CameraControllerに公開Shake。観戦=全占領、通常プレイ=人間関係の占領のみ
- **v1.0表記**: タイトル右下に「HexCiv v1.0 — 2026 / Claude Code × Codex 共同開発」
- **検証(round 1全合格)**: コンパイル0 / SMOKE OK+全ミニラン+基準値81/21/149/2ビット一致 / エディタテスト11種全OK / BUILD OK(95.9MB)→書き戻し済み / 45秒起動テスト例外0

### 📦 2026-07-21 Claude Code: プロジェクト移動+GitHub公開(重要・Codexへ)

- **プロジェクトは `C:\Users\kanta\GitHub\HexCiv` へ移動しました**(ユーザー指示)。旧パス `C:\Users\kanta\HexCiv` は存在しません。**Codexは以後、新パスで作業してください**
- GitHubリポジトリ公開: **https://github.com/kanta13jp1/HexCiv** (public、main、gitルート=プロジェクトルート)。以後の完了作業は`git add -A && git commit && git push`推奨(コミットメッセージに担当AI名を)
- ⚠️ 移動直後、PowerShellのエンコーディング事故でREADME/COLLABORATION/MERGE_PLANが一時文字化け→バックアップから完全復元済み(教訓: BOMなしUTF-8のmdをPowerShellで触るときは`[IO.File]::ReadAllText($f,[Text.Encoding]::UTF8)`必須。`Get-Content -Raw`は不可)

### 2026-07-21 Codex: 遺跡・偉人第3弾（各12件）・正式Windowsビルド更新

- `Core/HeritageSiteCatalog.cs` / `Core/GreatPersonCatalog.cs`: 既存96遺跡・108偉人のIDと順序を固定した後方追加で、共通6地域へ各2件ずつ追加し、108遺跡・120偉人へ拡張。アシャンティの伝統的建造物群／オコンフォ・アノキェ、カスビ王墓／アポロ・カグワ、スルフ・コタル／馬鳴、ラム・バーグ宮殿／ファキール・アジズッディーン、開城高麗遺跡群／義天、グノンガン／ハムザ・ファンスリー、ヴェネツィアと潟／エレナ・コルナロ、ヴィシェグラード王宮／ヤーノシュ・ヴィテーズ、ミトラ／アンドレス・エネストロサ、ニュー・エコタ／メアリー・ゴルダ・ロス、オロンゴ／フアン・テパノ、レブカ／ラトゥ・スクナを追加し、文明第3弾の12文明へ遺跡1件・偉人1人を一対一接続した。馬鳴とカニシカの関係は後代伝承であることを説明内に明記した。
- 新規項目は既存の `WorldLegacySystem` により、seed由来の6地域遺産配置、発見報酬、文明／地域親和性、偉人世界一意登用、6系統効果、AI、セーブversion 9へ自動接続。専用回帰では新規 `new_echota` と `mary_golda_ross` を使って発見、工学効果、二重登用防止、決定的セーブ往復、旧版移行を実証した。
- `Assets/Editor/HeritageGreatPersonExpansionSmokeTest.cs` / `WorldHistorySmokeTest.cs` / `WorldLegacySystemSmokeTest.cs` / `GlobalHistoryIndexSmokeTest.cs`: 108/120件、旧末尾と新末尾、ID一意、必須情報、6地域配分、12文明参照、全6効果、画像2048×1024、13分類931件を検証。`Logs/hg3_catalog.log`、`hg3_world_history.log`、`hg3_world_legacy.log`、`hg3_global_history.log` はすべてOK。
- `WORLD_HISTORY_CATALOG.md` / `GLOBAL_HISTORY_CATALOG.md` / `README.md` / `ARCHITECTURE.md`: 遺跡108件・偉人120人・合計228件、総合台帳931件へ更新。UNESCO、ASI、インドネシア文化当局、INAH、NPS、The Met、SOAS、韓国民族文化大百科事典、パドヴァ大学、ハンガリー国立図書館、メキシコ文化省、スミソニアン、EISP、南太平洋大学等の史料入口を記録した。
- Unityエディタ稼働中のため、非競合コピー `C:\Users\kanta\GitHub\HexCiv_Verify_HG3_20260721` でUnity 6000.3.20f1検証。`Logs/hg3_game_smoke.log` は150ターン、75ターン時点セーブ往復、複数文明、群島、難易度を完走して `SMOKE OK`。新しい遺産候補と偉人効果がAI進行へ実際に作用するため、**新基準は turn150: units=81 cities=21 techs=149 wars=2**（旧71/21/147/2からの意図した内容変化）。同seed決定性は維持。Claude Code第9弾は以後この新基準で回帰確認してください。
- `Logs/hg3_build.log` は `BUILD OK: 95853536 bytes`。15秒の検証Build起動と、正式 `Build\HexCiv.exe` 反映後の `Logs/hg3_official_player.log` 12秒起動はいずれもUnity 6000.3.20f1・重大例外0。正式 `Build\HexCiv_Data\Managed\Assembly-CSharp.dll` SHA-256は `1B44E39F47C0AD126083E6816B95B3691799C88F890D075269DC9F4729E9C785`。
- Claude Code作業中宣言の `Rendering/MapRenderer` / `Rendering/EntityRenderer` / `Rendering/RenderUtil` / `Audio/GameAudio` は編集していない。新規画像・音源は今回追加せず、既存の遺跡／偉人エンブレム、発見／登用SE、BGMが24項目にも自動適用される。次の推奨は今回の12文明と直接関係する作品・研究・文化の増補、または遺産種別ごとの選択式イベント。

### 2026-07-21 Claude Code: 丘陵立体化+都市成長+資源アイコン+領土塗り+地域別BGM、全検証合格

- **丘陵の立体化**(視覚のみ): 丘タイルを高さ0.14で押し出し+スカート壁。霧/ハイライト/国境/資源/装飾の全レイヤーとユニット/都市/ダメージ数字/各種FXが高さに追従。ピッキングはXZ平面のまま不変。**`RenderUtil.TileVisualHeight(Tile)` を新設 — HeritageRenderer(Codex担当)への適用をお願いします**(遺産マーカーが丘上で沈んで見える場合あり)
- **領土の面塗り**: 領土タイルに約7%の文明色ウォッシュ(国境と同じ可視性ルール)
- **資源の形状アイコン**: 小麦=金の穂3本/牛・鹿=角ペア/鉄=十字/馬=蹄鉄アーチ
- **都市ビジュアル成長**: 人口段階で建物1→4棟+中央タワー(都市Id決定的レイアウト、段階変化時のみ再構築)。**城壁建設で石壁リング出現**
- **地域別BGMフレーバー**: 人間文明の地域で旋律テーブルを切替(東・東南アジア=ペンタトニック等6種、観戦=既定)。時代3変奏システムはそのまま
- **検証(round 1全合格)**: コンパイル0 / SMOKE OK+全ミニラン / 基準値はCodexのHG3バッチ後の記録値(**81/21/149/2**)とビット一致=無回帰 / エディタテスト11種全OK / BUILD OK(95.9MB)→書き戻し済み / 45秒起動テスト例外0

### 2026-07-21 Codex: 文明・指導者第3弾（12文明・24指導者）・正式Windowsビルド更新

- `Core/CivilizationCatalog.cs` / `Core/LeaderCatalog.cs`: 既存68文明・131指導者のIDと順序を固定した後方追加で、共通6地域へ各2文明・各4指導者を追加し、80文明・155指導者へ拡張。アシャンティ／ブガンダ、クシャーナ朝／シク帝国、高麗／アチェ、ヴェネツィア／ハンガリー、サポテカ／チェロキー、ラパ・ヌイ／フィジー諸邦を追加した。各文明に6拠点、各2指導者を設定し、都市共和国、先住民社会、口承王統、複数首長国を中央集権帝国と同一視しない名称・説明を採用した。
- オセイ・トゥトゥ1世、ヤァ・アサンテワァ、ムテサ1世、ムワンガ2世、クジュラ・カドフィセス、カニシカ1世、ランジート・シング、ジンド・カウル、王建、光宗、イスカンダル・ムダ、タジュル・アラム、エンリコ・ダンドロ、フランチェスコ・フォスカリ、イシュトヴァーン1世、マーチャーシュ1世、コシホエサ、コシホピ、ナンイェヒ、ジョン・ロス、ホトゥ・マトゥア、ンガアラ、タノア・ヴィサワンガ、セル・エペニサ・ザコンバウを収録。口承人物は伝承と明記し、史料にない逸話や架空名を補っていない。
- `Core/GlobalHistoryIndex.cs`: ポリネシアに加えてメラネシア、ミクロネシア、オセアニア表記を共通地域「オセアニア」へ写像。動的集計は13分類・台帳907件へ更新した。
- `Assets/Editor/CivilizationLeaderExpansionSmokeTest.cs`: 80文明・155指導者、旧56件・第2弾末尾・第3弾後方順序、ID一意、6地域×2文明、24新旧文明×2指導者、必須情報、所属、既定選択、フィジー／ザコンバウ指定ゲーム、決定的セーブ往復、既存アイコンを検証。`Logs/civ3_catalog_smoke_v2.log` で `CIVILIZATION LEADER EXPANSION SMOKE OK`。
- `Logs/civ3_global_history_smoke.log`: 13分類907件と6地域完全分割を確認し `GLOBAL HISTORY INDEX SMOKE OK`。`Logs/civ3_game_smoke.log`: 75ターン時点のセーブ／ロード決定往復を含む150ターン、複数文明、群島、難易度を完走して `SMOKE OK`（turn150: units=71 cities=21 techs=147 wars=2）。直前の検証済み基準と一致した。
- `CIVILIZATIONS.md` / `LEADERS.md` / `GLOBAL_HISTORY_CATALOG.md` / `README.md` / `ARCHITECTURE.md`: 80文明・155指導者・総合907件へ更新。The Met、大英博物館、ブガンダ王国、韓国国立中央博物館、UNESCO、INAH、米国国立公園局、ラパ・ヌイ国立公園、ケンブリッジ大学博物館、フィジー政府等の史料入口を記録し、追加済み文明を候補一覧から除外した。
- Unityエディタが正式プロジェクトを開いていたため、非競合コピー `C:\Users\kanta\GitHub\HexCiv_Verify_Civ3_20260721_210751` でUnity 6000.3.20f1検証。`Logs/civ3_build.log` は `BUILD OK: 95840736 bytes`、`Logs/civ3_player.log` は15秒起動・重大例外0。HexCiv.exeが未起動であることを確認後、検証済みBuildを正式 `Build\HexCiv.exe` へ反映し、EXEのSHA-256一致（`85513D355030E7828D46A1F3BB818F0E61E580F47C0134535FFD977FF1A1B49E`）を確認。正式Buildも `Logs/civ3_official_player.log` で12秒起動・重大例外0。
- 今回は文明・指導者台帳を優先し、新規画像・音声は追加していない。既存の文明／指導者図鑑アイコン、選択SE、BGMは新規36レコードにも自動適用される。次の推奨は遺跡・偉人第3弾（各地域2件ずつ）で、今回の12文明へ史料上直接関係する項目を接続すること。

### 2026-07-21 Claude Code: タイトル画面+開幕音+セーブサムネイル+待機アニメ、全検証合格

- **タイトル画面** (`UI/TitleScreen.cs`新規、独立Canvas sortingOrder 200): 起動ごとに1回表示。起動済みマップを生きた背景に60%暗幕+世界史バナーを薄敷き。「つづける(スロット1がある時のみ)/新しいゲーム・設定/シミュレーション観戦/そのまま遊ぶ/終了(エディタでは非表示)」。選択で0.4秒フェードアウト、Esc=そのまま遊ぶ。GameBootstrapに最小公開フック追加
- **開幕ホルン**: 新規ゲームの「文明の夜明け」で柔らかい2音ホルン(GameAudio追加のみ)
- **セーブスロットサムネイル**: セーブ/ロード画面の各スロットに地形+都市のミニマップ(約96x56、ファイル更新時刻でキャッシュ、破損セーブは無表示)
- **ユニット待機アニメ**: 上下ボブ(±0.02、Id位相ずらし)/防御態勢はリング明滅。8倍速以上と移動トゥイーン中はスキップ
- **検証(round 1全合格)**: コンパイル0 / SMOKE OK+全ミニラン / 基準値はCodexの作品第2弾・文明第3弾後の記録値(**71/21/147/2**)とビット一致=無回帰 / エディタテスト**11種**全OK / BUILD OK(95.8MB)→書き戻し済み / 45秒起動テスト例外0(TitleScreenのヘッドレス安全性含む)

### 2026-07-21 Codex: 作品史追加42件（7分野×6地域）・正式Windowsビルド更新

- `Core/MasterpieceCatalog.cs`: 既存210件のIDと順序を固定したまま、書籍・絵画・彫刻・建築・音楽・演劇・映画へ共通6地域から各1件、計42件を末尾追加。作品史を252件、各分野36件、各地域42件へ拡張した。ケブラ・ナガスト、ジュニャーネーシュワリー、おもろさうし、グアマン・ポマの年代記、琉球八景、イドゥア王母像、ファシル・ゲビ、ラカラカ、組踊『二童敵討』、『母たちの村』、『別離』、『パラサイト』、『クジラの島の少女』等を含む。作者不詳・共同制作・口承／継承伝統を架空の個人名へ置換せず記録した。
- `Core/GlobalHistoryIndex.cs` / `UI/WorldHistoryPanel.cs` / `UI/LegacyPanel.cs`: 既存の動的集計・動的総数表示が追加42件を自動反映することを確認し、公開APIや表示実装を変更せず13分類・台帳871件へ更新。新規作品も世界一意の収蔵、AI収蔵、分野効果、関連文明／偉人の費用軽減、偉人連携へ接続される。
- `Assets/Editor/MasterpieceExpansionSmokeTest.cs`: 追加42ID、既存210件末尾、ID一意、必須情報、文明／偉人参照、7分野×6地域各1件、各分野36・各地域42、新規作品収蔵、セーブv9復元を検証。`Logs/masterpiece2_masterpiece_expansion_smoke_v2.log` で `MASTERPIECE EXPANSION SMOKE OK`。
- 既存回帰 `Logs/masterpiece2_masterpiece_system_smoke_v2.log` は7分野効果・継続効果・世界一意・AI収蔵・偉人連携・セーブv9決定往復・v8互換を含め `MASTERPIECE SYSTEM SMOKE OK`。`Logs/masterpiece2_global_history_smoke_v2.log` は13分類871件と6地域完全分割を確認し `GLOBAL HISTORY INDEX SMOKE OK`。
- 全体回帰 `Logs/masterpiece2_game_smoke_v2.log`: 150ターンと複数文明・群島・難易度を含め `SMOKE OK`（turn150: units=71 cities=21 techs=147 wars=2）。作品台帳追加後もゲーム進行・終了まで例外なし。
- `MASTERPIECE_CATALOG.md` / `GLOBAL_HISTORY_CATALOG.md` / `README.md` / `ARCHITECTURE.md`: 作品252件・総合871件へ更新。UNESCO、The Met、National Gallery、Library of Congress、National Gallery of Australia、Festival de Cannes、New Zealand Film Commission、Danish Film Institute等の個別史料入口を追記した。
- Unityエディタが正式プロジェクトを開いていたため、非競合コピー `C:\Users\kanta\GitHub\HexCiv_Verify_MP2_20260721_2015` でUnity 6000.3.20f1検証。`Logs/masterpiece2_build_v2.log` は `BUILD OK: 95827119 bytes`、`Logs/masterpiece2_player_v2.log` は20秒起動・重大例外0。HexCiv.exeが未起動であることを確認後、検証済みBuildを正式 `Build\HexCiv.exe` へ書き戻し、SHA-256一致を確認した。正式Buildも `Logs/masterpiece2_official_player_v2.log` で12秒起動し重大例外0。
- 今回は史料台帳とゲーム接続を優先し、新規画像・音声は追加していない。既存の分類アイコン、収蔵SE、BGMは252件にそのまま適用される。次の推奨は、作品史の次の均衡バッチ42件、または未表示の書籍・絵画・彫刻・建築・音楽用オリジナル分類アイコン追加。

### 2026-07-21 Codex: 研究史・文化史第2弾（各12件）・図鑑アイコン

- `Core/ResearchMilestoneCatalog.cs` / `Core/CulturalTraditionCatalog.cs`: 共通6地域へ各2件、計12件ずつを後方追加し、研究史96→108件・文化史96→108件へ拡張。ベニンの都市土木、アクスム貨幣、ビーマーリスターン医学、マラーター要塞網、琉球林政、ビルマ暦、鉱山学、月面図、ムイスカ鋳造、タイノ農法、トンガ造船、サモア農林知と、それらの地域に関係する生活文化・舞台・儀礼・社会空間を追加した。既存96件のIDと地域内順序を固定し、新規2件を17・18段階へ接続した。
- `Core/TechnologyCatalog.cs` / `Core/CulturePolicyCatalog.cs` / `Core/GlobalHistoryIndex.cs` / `UI/CulturePanel.cs`: 拡張技術を基礎12+研究史108=120件、文化政策を108件、総合索引を13分類・829件へ動的更新。コストは研究第18段階705、文化政策第18段階370。表示上の総数も台帳から動的取得する。
- `UI/WorldHistoryPanel.cs` / `Assets/Resources/History/research_culture_emblems.png`: 研究／文化の総合分類と各行へ48px専用アイコンを表示。imagegen built-in tool modeで、観測器・書物・植物／編組・共同体・巻物・演奏を抽象化したオリジナル2分割エンブレムを制作。文字・肖像・国旗・実在図版・宗教的聖像・共同体固有意匠は複製しない。原本1774×887、Unity取込2048×1024、原本・最終プロンプト・SHA-256は `ART_ASSET_PROVENANCE.md` に記録した。
- `RESEARCH_CULTURE_CATALOG.md` / `GLOBAL_HISTORY_CATALOG.md` / `README.md` / `ARCHITECTURE.md`: 研究・文化各108件、合計216件、総合829件へ更新。UNESCO、The Met、NCBI、琉球大学、Florida Museum、USDA Forest Service等の史料入口を追加した。
- 専用検証 `Logs/research_culture_expansion_smoke_v1.log`: 108/108件、6地域×追加2件、後方追加順、拡張技術・文化政策の前提とコスト、画像比率を確認し `RESEARCH CULTURE EXPANSION SMOKE OK`。既存 `ResearchCultureSmokeTest` / `ResearchTechTreeSmokeTest` / `CultureSystemSmokeTest` / `GlobalHistoryIndexSmokeTest` も全件合格。
- 全体回帰 `Logs/research_culture_game_smoke_v2.log`: 150ターン、複数文明、群島、難易度を含め `SMOKE OK`。基準値 `units=69 cities=20 techs=150 wars=3` と完全一致し、追加が既存ゲームの決定的進行を変えていない。
- Windows検証 `Logs/research_culture_build_v1.log`: 正式プロジェクトをUnityエディタが使用中のため非競合の一時コピーで `BUILD OK: 95818415 bytes`。`Logs/research_culture_player_v1.log` はUnity 6000.3.20f1で20秒起動し重大例外0。ユーザーが正式 `Build/HexCiv.exe` をプレイ中かつClaude Code第7弾が作業中のため、正式Buildへの上書きは保留した。
- Claude Code第7弾の宣言対象 `UI/ChroniclePanel.cs` / `UIManager` / `Audio/GameAudio.cs` は編集せず、同時作業との境界を維持した。次の推奨バッチは作品史7分類へ各6件（6地域×各1件）を追加する第2弾、または文明別固有効果との接続。

### 2026-07-21深夜 Claude Code: 戦史年表+勝利画面強化、全検証合格

- **戦史年表** (`UI/ChroniclePanel.cs`新規、独立Canvas): 型付きイベント(宣戦/和平/陥落/滅亡/終戦+開幕)をターン付き・色分けアクセントで自動記録(最大200件)。**Cキー**または左下「年表」ボタンで閲覧、終了画面の上からも開ける(sortingOrder 40)。新規ゲーム/ロードで再購読・クリア
- **勝利画面強化**: 最終スコア一覧(全文明のスコア/都市/技術/文化を色付き表示、滅亡文明は☠グレー)+勝利時の紙吹雪約40枚(敗北時なし)+勝利ファンファーレ(約2.5秒アルペジオ、BGM自動ダック)
- **検証(round 1全合格)**: コンパイル0 / SMOKE OK+全ミニラン+基準値69/20/150/3ビット一致 / エディタテスト9種全OK / BUILD OK(94.4MB)
- ⚠️ **ビルド書き戻しは保留中**(ユーザーが約30分以上プレイ継続中のためウォッチャー3サイクルで打ち切り): 検証済みビルドは`%TEMP%\HexCiv-CC2-r1\Build`に保管。**次回の作業開始時にゲーム終了を確認してから反映すること**(Codexが次に全体ビルド→書き戻しをする場合はソースに年表・勝利画面強化が含まれるためそれでも解消する)。現行`Build\HexCiv.exe`には年表・勝利画面強化は未反映

### 2026-07-21 Codex: 遺跡・偉人第2弾（各12件）・図鑑アイコン

- `Core/HeritageSiteCatalog.cs`: 共通6地域へ各2件、計12件を追加して遺跡84→96件。ベニン・イヤ、ファシル・ゲビ、サーマッラー、ラーイガド、琉球グスク、カンバウザタディ、シュパイアー、ヴァヴェル、エル・インフィエルニト、カグアナ、ハアモンガ、プレメレイを、直前に追加した12文明へ史料上の直接関係で各1件ずつ接続した。
- `Core/GreatPersonCatalog.cs`: 共通6地域へ各2人、計12人を追加して偉人96→108人。ジェイコブ・エガレヴバ、ギヨルギス・セグラ、フナイン・イブン・イスハーク、ジュニャーネーシュヴァル、蔡温、ナワデ1世、ヒルデガルト・フォン・ビンゲン、ヤン・コハノフスキ、グアマン・ポマ、エドモニア・ルイス、エペリ・ハウオファ、アルバート・ウェントを追加。後世国家への遡及所属は避け、ジュニャーネーシュヴァル等は地域人物として扱う。
- `Core/GlobalHistoryIndex.cs` / `UI/WorldHistoryPanel.cs` / `UI/LegacyPanel.cs`: 総合索引を13分類・805件へ動的更新。遺跡／偉人の総合分類・各行へ48px専用アイコンを表示し、Legacyの偉人数表示を台帳件数から動的取得する。独立Canvasは WorldHistory=130 / Culture=135 / Legacy=140 かつ表示時 `SetAsLastSibling` で、Claude Code依頼のsortingOrder 20以上を既に満たすことを監査済み。
- `Assets/Resources/History/heritage_great_people_emblems.png`: imagegen built-in tool modeで、汎用の発掘区画・石造門／空のメダリオン・書物・方位磁針・歯車等を描くオリジナル2分割エンブレムを制作。肖像・国旗・実在遺構・出土品・文字は複製しない。原本1774×887、Unity取込2048×1024、原本・最終プロンプト・SHA-256は `ART_ASSET_PROVENANCE.md` に記録した。
- `WORLD_HISTORY_CATALOG.md` / `GLOBAL_HISTORY_CATALOG.md` / `README.md` / `ARCHITECTURE.md`: 遺跡96件・偉人108人・台帳805件へ更新し、公的機関・博物館・大学資料の照合入口を追記した。
- 専用検証 `Logs/heritage_great_person_expansion_smoke_v1.log`: 96/108件、6地域×各2件の追加、関連文明、偉人効果、画像比率を確認し `HERITAGE GREAT PERSON EXPANSION SMOKE OK`。既存 `WorldHistorySmokeTest` / `WorldLegacySystemSmokeTest` / `GlobalHistoryIndexSmokeTest` も全件合格。
- 全体回帰 `Logs/heritage_great_person_game_smoke_v1.log`: 150ターン、複数文明、群島、難易度を含め `SMOKE OK`。遺跡候補増加でseed固定の標準12件抽選と発見報酬が変わるため、基準値は意図的に `units=69 cities=20 techs=150 wars=3`（旧59/19/141/3）へ更新。
- Windows検証 `Logs/heritage_great_person_build_v1.log`: 正式プロジェクトをUnityエディタが使用中のため非競合の一時コピーで `BUILD OK: 94403355 bytes`。`Logs/heritage_great_person_player_v1.log` はUnity 6000.3.20f1で20秒起動し重大例外0。Claude Code第6弾が作業中のため正式 `Build` への上書きは保留し、正式プロジェクトのソースとResourcesは更新済み。
- Claude Code第6弾の宣言対象 `GameBootstrap` / `UIManager` / `UI/MinimapPanel` / `Audio/GameAudio` は編集せず、17:41時点の同変更を含む共有ソースをまとめて検証した。次の推奨バッチは研究史・文化史を6地域へ各2件ずつ追加する第2弾。

### 2026-07-21 Claude Code: 256倍速+Z順修正+トップバーアイコン+ミニマップ視界枠+環境音、全検証合格

- **256倍速**: 観戦速度9段階(等速〜256倍速)。フレーム時間予算方式で1フレーム最大6ターン処理
- **UIのZ順修正**(実写スクショで発見): 左下パネルボタン群を専用サブキャンバス(sortingOrder -5)へ退避し、モーダルパネル(ゲーム設定等)の上に浮く問題を解消。UIManager自身のモーダルは表示時にSetAsLastSiblingで最前面化
- **トップバーアイコン**: ゲーム設定=歯車/文明変更=地球/指導者変更=王冠/研究=フラスコ/戦況=グラフ
- **ミニマップ視界枠**: 現在のカメラ視界を白枠でミニマップに描画、カメラ移動へ即追従(約10回/秒・アロケーションフリー)
- **環境音レイヤー**: BGM下に静かな風のうねり(約-24dB相当、BGM音量/ミュートに連動、専用UI無し)
- **検証(round 1全合格)**: コンパイル0 / SMOKE OK+全ミニラン / 基準値はCodexの遺跡・偉人拡張後の記録値(69/20/150/3)とビット一致=無回帰 / エディタテスト9種全OK / BUILD OK(94.4MB)→書き戻し / 45秒起動テスト例外0
- 🤝 検証時に外部起動中の`HexCiv.exe`(ユーザープレイ中の可能性)を検知したが、**今回は停止せず**別ログファイルで回避(前回の反省を反映)
- 📌 Codexへの依頼(再掲): 独立Canvasパネルの sortingOrder を20以上へ — 当方の-5退避と合わせて恒久解決になります

### 2026-07-21 Codex: 文明・指導者第2弾（12文明・24指導者）・図鑑アイコン

- `Core/CivilizationCatalog.cs` / `Core/LeaderCatalog.cs`: 既存56文明と107指導者のID・順序を維持した後方追加で、ベニン王国、エチオピア帝国、アッバース朝、マラーター、琉球王国、タウングー朝、神聖ローマ帝国、ポーランド＝リトアニア連合、ムイスカ、タイノ諸社会、トンガ、サモア諸社会を追加。共通6地域へ各2文明・4指導者を均等追加し、68文明・131指導者へ拡張した。各新文明に6拠点名以上、各2名の選択可能指導者を設定し、諸社会を単一国家と断定しない表示を採用した。
- `Core/GlobalHistoryIndex.cs` / `UI/WorldHistoryPanel.cs`: カリブ海を共通地域「アメリカ大陸」へ写像。世界史総合索引を13分類・781件へ更新し、文明／指導者の総合分類と各行へ48px専用アイコンを表示する。
- `Assets/Resources/History/civilization_leader_emblems.png`: imagegen built-in tool modeで、城壁都市・市民会議／空席の議席・印章・憲章を描いたオリジナル2分割アイコンを制作。肖像・国旗・特定王朝紋章・実在建築・文字を使わない。原本1774×887、Unity取込2048×1024、原本保存先・最終プロンプト・SHA-256は `ART_ASSET_PROVENANCE.md` に記録した。
- `CIVILIZATIONS.md` / `LEADERS.md` / `GLOBAL_HISTORY_CATALOG.md` / `README.md` / `ARCHITECTURE.md`: 68文明・131指導者・台帳781件へ更新。公的機関・博物館資料の照合先を記録し、追加済み文明を候補一覧から除外した。
- 専用検証 `Logs/civilization_leader_expansion_smoke_v1.log`: 68文明・131指導者、旧56文明の互換順序、6地域×2文明、12文明×2指導者、必須情報・参照・既定指導者、アイコン読込を含め `CIVILIZATION LEADER EXPANSION SMOKE OK`。
- 総合索引検証 `Logs/civilization_leader_global_history_smoke_v1.log`: 13分類・台帳781件、6地域完全分割、全3図鑑画像を確認し `GLOBAL HISTORY INDEX SMOKE OK`。既存6テスト（世界史、研究文化、技術ツリー、文化、世界遺産・偉人、作品）も全件合格。
- 全体回帰 `Logs/civilization_leader_game_smoke_v1.log`: 150ターン基準値 `units=59 cities=19 techs=141 wars=3` と完全一致し `SMOKE OK`。既定AIは先頭文明を使うため、後方追加がシミュレーション結果を変更しないことを確認した。
- Windowsビルド `Logs/civilization_leader_build_v1.log`: `BUILD OK: 92993211 bytes, 70.8s`、145ファイル。検証コピーと正式プロジェクトのAssets/Packagesは差分0、正式 `Build` へ同期後に主要実行・Managed・ResourcesのSHA-256一致とファイル構成差分0を確認。`Logs/civilization_leader_official_player_v1.log` はUnity 6000.3.20f1で20秒起動し重大例外0。
- Claude Code第5弾の宣言対象 `GameBootstrap` / `UIManager` / `Rendering/EntityRenderer` / `Audio/GameAudio` は編集せず、17:05時点の同変更を含む共有ソースをまとめて検証・ビルドした。

### 2026-07-21 Claude Code: 128倍速+ログ幅修正+生産アイコン+誕生演出+和平/終盤音、全検証合格

- **128倍速**: 観戦速度8段階(等速〜128倍速)。ターン処理ループを「1フレーム1ターン」から**フレーム時間予算方式**へ拡張 — 1フレームに最大 ceil(速度/60)+1 ターン、Stopwatch実測で約10ms超過時は打ち切り、イベント演出発生時は即中断。64倍速以下は実質従来挙動。描画リフレッシュはバッチ後に1回
- **ログ幅修正**: ログパネル幅を画面約38%に制限し、中央バナー領域への食い込みを解消
- **生産リストアイコン**: 都市パネルの生産ボタンにユニット字章チップ/建物チップを付加
- **都市誕生バースト**: 新都市のバナーポップ+金色パーティクル6個(プール式・8倍速以上スキップ)
- **音**: 和平=柔らかい解決二音(宣戦スティングと対) / BGM第3バリエーション(ターン180超で荘厳な変奏へ。A≦100→B≦180→C、新規ゲームでA復帰)
- **検証(round 1全合格)**: コンパイル0 / SMOKE OK+全ミニラン+**基準値59/19/141/3ビット一致** / エディタテスト8種全OK(Codexの文明拡張テスト含む) / BUILD OK(93.0MB)→書き戻し / 45秒起動テスト例外0

### 2026-07-21 Claude Code: 64倍速+ミニマップ+アイコン+出現アニメ+スティング音、検証合格

- **64倍速**: 観戦速度を等速〜64倍速の7段階に拡張
- **ミニマップ** (`UI/MinimapPanel.cs`新規、Codexパネルと同じ独立Canvas方式): 右下に全体マップをテクスチャ描画(地形・領土・都市・ユニット点、通常プレイは霧反映/観戦は全表示)。**クリックでカメラジャンプ**、Mキーで表示切替。Version変化時のみ最大5回/秒更新・アロケーションフリー
- **アプリアイコン** (`Editor/AppIconGenerator.cs`新規): 紺地+金ヘクスのロゴを256pxで手続き生成し `Assets/Icon/app_icon.png` に保存、PlayerSettingsへ設定。BuildScript.PerformBuild冒頭で毎回保証(冪等)
- **ボタンアイコン**: 左下3パネル(図鑑=本/文化=旗/遺産=柱)とセーブ/ロードボタンに手続き生成の小アイコンを追加(UIStyleに再利用ヘルパー)
- **ユニット出現アニメ**: 新規生産ユニットがスケール0→1.15→1でポップイン(0.25s、8倍速以上はスキップ)
- **スティング音**: 宣戦布告=重い二音ヒット/都市陥落=ドラム+下降音(GameAudio追加のみ・既存アラート挙動保持)
- **検証**: コンパイル0 / SMOKE OK+全ミニラン / エディタテスト7種全OK / アイコン生成確認(実プロジェクトにSHA256一致で存在) / BUILD OK(91.9MB)→書き戻し(Codex統合ビルドとハッシュ一致) / 45秒起動テスト例外0
- 📊 **基準値について**: 検証中の照合対象(97/19/147/3)は本作業と並行したCodexの演劇・映画統合(意図的シミュレーション変更)により陳腐化。検証エージェントはCodex記録の新基準値と自実行結果の**ビット一致**を確認済み。**現行正: units=59 cities=19 techs=141 wars=3**(Codex第2弾エントリ記載と同値)

### 2026-07-21 Codex: 演劇・映画作品史 第2弾60件・図鑑アイコン

- `Core/MasterpieceCatalog.cs`: 演劇30件・映画30件を追加。各分野を6地域×5件とし、作品史を7分野×30＝210件、各地域35件へ拡張した。戯曲だけでなく仮面劇・人形劇・影絵・舞踊劇・即興演劇、無声・劇・実験・記録・共同製作映画を含め、作者不詳・共同体制作も創作名で補わない。
- `Core/MasterpieceSystem.cs`: 演劇は文化+55・全都市生産+10・各文明への影響力+10、映画は文化+50・科学+45・影響力+10と科学+1/ターンを付与。既存の作品ポイント、世界一意収蔵、AI収蔵、親和性、偉人連携、セーブv9のID保存をそのまま利用するため旧セーブ互換を維持した。
- `Core/GlobalHistoryIndex.cs` / `UI/WorldHistoryPanel.cs` / `UI/LegacyPanel.cs`: 総合索引へ演劇・映画を加えて13分類・台帳745件へ更新。作品210件を地域別に閲覧・収蔵でき、総数表示は台帳件数から動的に取得する。
- `Assets/Resources/History/theater_film_emblems.png`: imagegen built-in tool modeで実在の舞台写真・映画ポスター・俳優・キャラクターを複製しない演劇／映画2分割アイコンを制作。図鑑の総合分類と演劇・映画作品行に48px表示する。元PNGは1774×887、Unity取込時2048×1024。最終プロンプトと権利方針は `ART_ASSET_PROVENANCE.md` に記録した。
- `MASTERPIECE_CATALOG.md` / `GLOBAL_HISTORY_CATALOG.md` / `README.md` / `ARCHITECTURE.md`: 210作品・13分類745件へ更新。UNESCO無形文化遺産、Library of Congress National Film Registry、BFI、NFSAを候補発見と相互確認の入口へ追加した。
- 専用検証 `Logs/theater_film_masterpiece_smoke_v3.log`: 210件、7分野×30、6地域×35、ID一意、文明／偉人参照、7効果、継続効果、世界一意、AI収蔵、偉人連携、セーブv9決定往復・v8互換を含め `MASTERPIECE SYSTEM SMOKE OK`。
- 総合索引検証 `Logs/theater_film_global_history_smoke_v2.log`: 13分類・745件、6地域完全分割、文明・指導者写像、図鑑バナーと演劇／映画アイコン読込を含め `GLOBAL HISTORY INDEX SMOKE OK`。
- 第4弾ソースとの統合回帰 `Logs/theater_film_game_smoke_merged_v1.log`: 150ターン、複数文明、難易度、戦争3件を含め `SMOKE OK`。演劇・映画を収蔵候補と効果へ実接続したため、新基準候補は `units=59 cities=19 techs=141 wars=3`（従来97/19/147/3からの意図的変化）。
- 統合Windowsビルド `Logs/theater_film_build_merged_v1.log`: `BUILD OK: 91932763 bytes, 30.1s`、145ファイル。後発の正式 `Build/HexCiv.exe` は実行ファイル・主要Resourcesが統合ビルドとハッシュ一致し、`Logs/theater_film_official_player_v1.log` でUnity 6000.3.20f1・20秒起動・重大例外0を確認した。
- Claude Code第4弾の宣言対象ファイルは編集せず、その最新ソースとまとめてコンパイル・専用検証・全体回帰・ビルド・起動検証した。

### 2026-07-21 Claude Code: 観戦UIバグ修正+視認性/演出+BGM/SE追補+AI軍構成、全検証合格

- **観戦UIバグ修正**(実プレイのスクリーンショットで発見): 同時表示バナーの重なり→縦積みスロット化(8pxギャップ・フェードで詰め上がる)/観戦バー表示中はログパネルを下へ退避
- **視認性・演出**: 都市/ユニット名TextMeshを高解像度化(fontSize↑+characterSize↓で見かけサイズ不変)+ダーク影コピーで明地形でも判読可能に/選択リングのパルス点滅(非スケール1.2s)/人口増加時の都市バナー拡大パルス(Core変更なし・ポーリング検知)
- **GameAudio追補**(Codex実装を完全保持した追加のみ): 遺産発見・偉人登用・作品収蔵の専用チャイム3種(実際のEmitLog文言をgrepして照合)/ターン100超でBGMが第2バリエーションへ約2秒クロスフェード(新規ゲームでAに復帰)
- **AI軍構成バランス**(意図的なシミュレーション変更): 遠隔ユニット数≧近接ユニット数のとき近接を優先生産(スクリーンショットのカタパルト15体偏重を解消)。効果は基準値に現れた: units 62→97(前線ユニットが生き残るように)
- **検証(round 1全合格)**: コンパイル0 / SMOKE OK+全ミニラン / エディタテスト6種全OK / BUILD OK(91.2MB — Codexの図鑑バナー画像分増)→書き戻し / 起動テスト例外0
- 📊 **seed42基準値を更新(AI軍構成変更による意図的変動)**: **units=97 cities=19 techs=147 wars=3(累計 wars 3 / peace 1)**。次回検証からこの値を正とする

### 2026-07-21 Codex: 世界史総合索引・文明／指導者図鑑・オリジナル図鑑ビジュアル

- `Core/GlobalHistoryIndex.cs`: 既存の検証済み台帳を唯一の情報源として、文明、王・君主・指導者、遺跡、偉人、書籍、絵画、彫刻、建築、音楽、学問・科学技術、文化の11分類を横断集計。全地域685件を、アフリカ、南北アメリカ、オセアニア、東・東南アジア、西・南アジア、ヨーロッパ・地中海の共通6地域へ決定的に分割する。
- `UI/WorldHistoryPanel.cs`: 既存の「世界史図鑑」を総合／文明／指導者／遺跡／偉人／研究／文化／作品の8画面へ拡張。文明56件と指導者107件を地域別に全件閲覧でき、現ゲームの自文明・現在の指導者・登場状況も表示する。総合画面では11分類の現在件数と実装段階を一覧表示する。
- `Assets/Resources/History/world_history_banner.png`: 画像生成ツールのbuilt-inモードで、特定の実在作品や人物を複製しないオリジナルの世界史図鑑パノラマ（2048×1024）を制作・実装。最終プロンプト、権利上の制約、保存先は `ART_ASSET_PROVENANCE.md` に記録した。
- `GLOBAL_HISTORY_CATALOG.md`: 「歴史上のすべて」は失われた記録、無文字・口承文化、作者未詳、分類境界により有限の確定表にできないことを明記。捏造せず、安定ID・史料確認・地域偏りの監査を行いながら順次追加する11分類共通ロードマップと、UNESCO／博物館／図書館等の確認入口を整備した。
- 専用検証 `Logs/global_history_index_smoke_v3.log`: 11分類・全地域685件、6地域の合計完全一致、文明の地域写像欠落0、指導者107件の所属対応、バナー2048×1024のResources読込を確認し `GLOBAL HISTORY INDEX SMOKE OK`。
- 統合回帰 `Logs/global_history_game_smoke_v1.log`: 戦争3件、複数文明、セーブ・研究・文化・遺産・偉人・作品を含む全スモークが `SMOKE OK`。Windowsビルド `Logs/global_history_build_v1.log` は `BUILD OK: 90860633 bytes, 28.0s`。正式 `Build/HexCiv.exe` へ145ファイルを同期し主要リソースのハッシュ一致を確認、`Logs/global_history_official_player_v1.log` でUnity 6000.3.20f1の起動と重大例外0を確認した。
- Claude Codeの第3弾宣言対象5ファイルは編集していない。12:44時点の共有ソースを隔離コピーへ同期し、その状態をまとめてコンパイル・回帰・ビルド・起動検証した。

### 2026-07-21 Claude Code: 32倍速+ドラッグカメラ+アニメーション、全検証合格

- **32倍速**: 観戦速度を等速〜32倍速の6段階に拡張(1フレーム1ターン上限維持)
- **ドラッグカメラ**: 左/右ドラッグでマップをつかんでパン(地面グラブ方式、MMBと同実装を共用)。しきい値6px未満のリリースは従来どおり選択/移動命令 — 既存操作の互換性維持。ドラッグからはチュートリアルイベント発火なし
- **アニメーション**(全てコード生成): ユニット移動トゥイーン(1タイル約0.15-0.2s、3タイル超や8倍速以上はスナップ)/戦闘演出(攻撃突進+被弾フラッシュ+ダメージ数字ポップ「-28」最大24個プール+白兵反撃分も表示)/撃破フェードアウト/都市占領フラッシュ/水面の明滅ゆらぎ(コードハッシュ位相、毎秒10回更新上限・アロケーションフリー)
- **型付きイベント追加**: `GameState.OnCombatResolved(attackerCoord, targetCoord, dmgToDef, dmgToAtk)`(Core/GameEvents.cs追加+Combat.cs末尾Raise。**アブレーション検証済み**: Raise呼び出しを外して再実行しても結果が完全一致=シミュレーション無影響の実証)
- **検証(round 1全合格)**: コンパイル0 / SMOKE OK+全ミニラン+WARS TOTAL: 3 / エディタテスト6種全OK(作品システム含む) / BUILD OK(89.8MB)→書き戻し / **60秒**起動テスト(アニメーション動作込み)例外0・NaN 0
- 📊 **seed42基準値を更新**: units=62 cities=20 techs=151 wars=2(累計 wars 3 / peace 3)。前基準(82/20/132/1)からの変動はCodexのMasterpieceSystem(TurnManagerフック、セーブv9)由来 — 上記アブレーションで当方コードの無関係を実証済み。**次回検証からこの値を正とする**

### 2026-07-21 Codex: 作品史150件・世界一意の収蔵システム

- 「歴史上の全作品」は失われた作品・無名共同制作・口承異本・現在も変化する実践を含み有限確定できないため、`MASTERPIECE_CATALOG.md` に非断定の収録基準と継続追加手順を固定。第1弾は書籍・絵画・彫刻・建築・音楽各30件、6地域各25件の計150件。
- `Core/MasterpieceCatalog.cs`: 安定ID、日本語名、地域、時期、作者／担い手、要約、直接関係が確認できる文明・偉人IDを収録。テオティワカンをアステカへ遡及させない等、便宜的な誤関連は付けない。
- `Core/MasterpieceSystem.cs`: 都市・政策・登用偉人から作品ポイントを産出。作品は世界で一度だけ収蔵でき、AIも自動収蔵。関連文明20%・同地域10%・関連偉人15%（合計上限30%）の費用軽減、5分野別の即時／毎ターン効果、偉人登用から直接関連作品1件の無償収蔵を実装。
- `Player` / `CultureSystem` / `TurnManager` / `WorldLegacySystem`: 作品ポイント・収蔵ID、文化／科学継続効果、ターン進行、偉人連携、スコアを接続。`SaveLoad` version 9でポイント・累計・収蔵IDを決定的に保存し、version 8以前も受理。
- `UI/LegacyPanel.cs`: 左下ボタンを「遺産・偉人・作品」へ拡張し、「作品収蔵」タブから効果・親和性・費用を確認して収蔵可能。`UI/WorldHistoryPanel.cs`: 「作品史」タブで全150件を地域別閲覧し、自文明／他文明の収蔵状態を表示。
- 専用検証 `Logs/masterpiece_system_smoke_v4.log`: 150件、5分野×30、6地域×25、ID／参照整合、5効果、継続効果、世界一意、AI自動収蔵、北斎→「神奈川沖浪裏」、セーブv9完全往復・v8互換を含め `MASTERPIECE SYSTEM SMOKE OK`。
- 統合検証 `Logs/masterpiece_game_smoke_v1.log`: ターン76セーブ往復一致後も150まで進行。複数文明・群島・難易度・戦争3・和平3を含め `SMOKE OK`、重大例外0。
- Windowsビルド `Logs/masterpiece_build_v1.log`: `BUILD OK: 89797149 bytes, 9.7s`。正式 `Build/HexCiv.exe` へ143ファイル欠落／サイズ不一致0で同期。`Logs/masterpiece_player_v1.log` はUnity 6000.3.20f1で30秒ヘッドレス起動し重大例外0。
- Claude Codeの作業中宣言対象ファイルは変更せず、検証コピーとの同期差分が同宣言に由来する3つの孤立meta以外0であることを確認。次回は同じ基準で第2弾を追加可能。

### 2026-07-21 Claude Code: 16倍速+Escフルスクリーン解除+終了画面戦況グラフ、全検証合格

- **16倍速**: 観戦速度を等速/2倍速/4倍速/8倍速/**16倍速**の5段階に拡張(1フレーム1ターン上限維持)
- **Escでフルスクリーン解除**: Escは従来どおり「パネルを閉じる/選択解除」を優先し、**何も閉じるものがない時のみ**ウィンドウモードへ復帰(F11と同じ経路でトグル・設定表示・PlayerPrefs同期。通常/観戦/ゲーム終了画面すべてで有効)
- **終了画面の最終戦況**: ゲーム終了オーバーレイに「最終戦況」ボタンを追加し、既存ScoreGraphPanelをオーバーレイの上に表示
- 変更ファイルは宣言どおり GameBootstrap / UIManager / InputController の3つのみ
- **検証(round 1全合格)**: コンパイル0 / SMOKE OK+全ミニラン+WARS TOTAL: 3 / エディタテスト5種全OK(世界史・研究文化・技術ツリー・文化システム・**世界遺産システム**) / BUILD OK(89.76MB)→書き戻し / 30秒起動テスト例外なし
- 📊 **seed42基準値を更新**: units=82 cities=20 techs=132 wars=1(累計 wars 3 / peace 2)。前基準(83/20/116/2)からの変動はCodexのWorldLegacySystem(TurnManagerフック+遺産科学効果、セーブv8)由来であることをログ比較で特定済み — 当方3ファイルはヘッドレス経路に含まれず無関係。**次回検証からこの値を正とする**

### 2026-07-21 Codex: 世界遺産・偉人を実ゲームへ接続

- `Core/WorldLegacySystem.cs`: 世界史台帳84遺産から、6地域×2件＝12件をseed由来で決定的に配置。開始文明から到達可能な陸地を優先し、ゲーム本体のRngは消費しない。ユニット到達時に文化・科学・偉人ポイントを世界で一度だけ付与する。
- 文明・指導者の地域親和性を実装。遺産の関連文明は報酬+50%、同地域文明は+25%。偉人登用費はそれぞれ20%／10%軽減。指導者は所属文明の地域親和性を受け継ぐため、登録済み全107指導者へ適用される。
- 偉人96人を世界共通プールへ接続。都市数・文化政策から毎ターン偉人ポイントを獲得し、AIは自動登用、人間は独立 `UI/LegacyPanel.cs` の「遺産・偉人」画面から登用する。活動分野を学術・文化・技術・社会・探検・軍事の6系統へ分類し、科学／文化／都市生産／影響力／地図探索／全軍回復の即時効果を発動。
- `Rendering/HeritageRenderer.cs`: 探索済み遺産タイルに金色の「史跡」マーカーを表示し、自文明の発見後は青緑色と実名へ変更。`WorldHistoryPanel` にも「この世界に存在／発見済／他文明発見」「登用済／他文明登用」と偉人効果を表示。
- `SaveLoad` version 8: 遺産Id・座標・発見者・発見ターン、偉人ポイント累計、発見済み遺産、登用済み偉人を決定的に保存。version 7以前は `TurnManager` 構築時にseedから遺産を補完する。ターン上限スコアにも遺産・偉人を加点。
- 専用検証 `Logs/world_legacy_system_smoke.log`: 84遺産・96偉人・6効果系統、6地域12件の同seed完全一致、重複なし／通行可能地、一度限りの発見、関連文明+50%、偉人世界一意、効果、セーブv8完全往復、旧版移行を含め `WORLD LEGACY SYSTEM SMOKE OK`。
- 統合検証 `Logs/world_legacy_game_smoke.log`: ターン76のセーブ完全往復後も150まで進行。都市20・ユニット82・技術132・戦争3・和平2・ターン150文化勝利、マルチ文明・群島・難易度を含め `SMOKE OK`。
- Windowsビルド `Logs/world_legacy_build.log`: `BUILD OK: 89755165 bytes, 48.0s`。正式 `Build/HexCiv.exe` へ同期済み。`Logs/world_legacy_player.log` はUnity 6000.3.20f1で30秒ヘッドレス起動し例外0。
- Claude Codeの作業中宣言対象 `GameBootstrap` / `UIManager` / `InputController` は変更せず、検証・ビルド時点の最新版を保持して統合した。
- 次段階: 遺産種別ごとの選択イベント／個別報酬、偉人の年代記・複数回使用能力・都市への作品／学術成果の収蔵、地域単位の台帳増補。

### 2026-07-21 Codex: 文化進行・政策・交流・文化勝利

- `Core/CulturePolicyCatalog.cs`: 文化史96件を `policy_` 安定IDの6地域×16段階政策ツリーへ接続。コスト30～330、文化+1/ターン・科学+1%・都市生産+1%・文化的影響力+1/文明ターンの4効果を循環して付与。
- `Core/CultureSystem.cs`: 都市・人口・首都・記念碑・政策から文化ポイントを産出。AI政策自動選択、政策採用、非戦争文明間の文化交流、戦争中の交流停止、文化勝利判定を純Coreとして実装。
- 文化勝利はゲーム後半の150ターン以降、政策14件、累計文化1500、全文明より高い文化、文明別の必要影響力を条件とする。初期案の75/120ターンでは固定AI戦が解禁直後に終わったため、実シミュレーション結果を基に150へ調整。
- `UI/CulturePanel.cs`: 左下「文化・政策」から開く独立Canvas。文化産出と進行中政策、採用可能／採用済み政策、文明別影響力、戦争による交流停止、文化勝利進捗を表示。人間はここから政策を選択できる。Claude Code対象の `GameBootstrap` / `UIManager` / `InputController` は変更せず統合。
- `SaveLoad` version 7: 採用政策、選択中政策、文化貯蔵、累計文化、相手Id別影響力を決定的に保存。version 6以前は文化0・政策未採用で後方互換。科学・都市生産・ターン上限スコアにも政策効果を接続。
- 専用検証 `Logs/culture_system_smoke_final.log`: 文化史96件、6地域×16、前提・コスト、文化産出、政策効果、平和交流、戦時停止、文化勝利、セーブv7復元を含め `CULTURE SYSTEM SMOKE OK`。最終ゲート定数変更後もUnity正式RoslynでPlayer/Editor全体エラー0。
- 統合検証 `Logs/culture_system_final_game_smoke.log`: 75ターンのセーブ往復完全一致後も150ターンまで進行し、都市21・ユニット69・技術124・戦争1・和平1を経てターン150で文化勝利。マルチ文明・群島・難易度を含め `SMOKE OK`。
- Windowsビルド `Logs/culture_system_build.log`: `BUILD OK: 89734354 bytes, 40.5s`。検証コピーと正式プロジェクトの全ソース・設定はSHA-256差分0、正式Build一式も差分0で同期済み。`Logs/culture_system_player.log` はUnity 6000.3.20f1、30秒ヘッドレス起動、例外0。
- 次段階: 文明・指導者ごとの固有文化効果、マップ上の遺跡発見と報酬、偉人ポイント・登用・固有能力を文化／研究システムへ接続する。

### 2026-07-20深夜 Claude Code: 8倍速+フルスクリーン+通常プレイ通知、全検証合格

- **8倍速**: 観戦の速度切替を等速/2倍速/4倍速/**8倍速**の4段階に拡張(ターン間隔=1/速度の非スケール時間、Time.timeScale同期。1フレーム1ターン上限のためスパイラルなし)
- **フルスクリーン**: **F11**キー+ゲーム設定パネルの「フルスクリーン: ON/OFF」トグル。ON=ネイティブ解像度のFullScreenWindow、OFF=1280x720ウィンドウ。PlayerPrefs「HexCiv.Fullscreen」で永続化し起動時に適用(エディタでは安全に無効)
- **通常プレイの控えめイベント通知**: 型付きイベント(宣戦/和平/陥落/滅亡/終戦)を通常プレイでも小型バナー(上部中央・約2秒)で表示。カメラジャンプ・自動ターン小休止は観戦モード限定のまま
- 変更ファイルは宣言どおり GameBootstrap / UIManager / InputController の3つのみ
- **検証(round 1全合格)**: コンパイル0 / SMOKE OK+全ミニラン+WARS TOTAL: 2 / **seed42基準値(units=83 cities=20 techs=116 wars=2)ビット一致=回帰なし** / Codexの3カタログテスト(世界史・研究文化・技術ツリー)全OK / BUILD OK(89.72MB)→書き戻し / 30秒起動テスト例外なし

### 2026-07-20 Codex: 研究史96件を拡張技術ツリーへ接続

- `Core/TechnologyCatalog.cs` を新設し、既存12技術をID・順序・内容とも変更せず、研究史96件を加えた全108技術の統合カタログを実装。歴史技術IDは `history_` 接頭辞で旧セーブと衝突しない。
- 研究史は6地域×16段階の枝として接続。各枝の第1段階は `writing` / `archery` / `iron_working` / `mathematics` / `construction` を前提とし、以後は地域内で順次解禁。コストは110から635まで段階上昇する。
- `Player.AvailableTechs()`、`TurnManager` の研究完了判定、`UIManager` の研究表示を統合カタログへ接続。世界史図鑑の研究史行にも研究コストを表示。Claude Codeが同時期に追加した観戦演出・難易度・戦況グラフの最終ソースを保持した状態で統合済み。
- セーブ形式は変更せず、既存の `KnownTechs` / `CurrentResearch` の文字列ID保存をそのまま利用。旧12技術のセーブ互換性を維持し、新96技術も同じ経路で保存・復元できる。
- 専用検証 `Assets/Editor/ResearchTechTreeSmokeTest.cs`: 全108件、旧12件の参照・順序互換、ID一意性、6地域×16段階、前提関係、コスト、Playerの研究可能判定を確認。`Logs/research_tech_tree_merged_smoke.log` は `RESEARCH TECH TREE SMOKE OK`。
- 統合回帰検証 `Logs/research_tech_tree_merged_game_smoke.log`: 150ターンで4文明合計116技術まで研究が進行し、戦争2・和平1・マルチ文明・群島・難易度を含め `SMOKE OK`。研究史技術をAIも実際に研究することを確認。
- Windowsビルド `Logs/research_tech_tree_final_build.log`: `BUILD OK: 89717824 bytes, 71.7s`。一時検証コピーと正式プロジェクトの全ソース・設定はSHA-256差分0件（Unity生成metaを除く）、正式 `Build` 一式は差分0件で同期済み。`Logs/research_tech_tree_player.log` はUnity 6000.3.20f1、30秒ヘッドレス起動、例外0件。
- 次段階は文化史96件を文化ポイント・政策・文化交流・文化勝利へ接続し、その後に文明・指導者・遺跡・偉人との固有相互作用を追加する。

### 2026-07-20 Claude Code: 観戦演出+難易度設定、全検証合格

- **型付きゲームイベント** (`Core/GameEvents.cs`新規、partial GameState): OnWarDeclared / OnPeaceMade / OnCityCaptured / OnPlayerEliminated / OnGameEnded。既存のEmitLog直後にRaiseを追加(ログ文字列・既存ロジック不変)。プレゼン層がログ文字列パースに頼らず事件を検知できる基盤
- **観戦演出**(シミュレーションモード時のみ): 宣戦・和平・都市陥落・文明滅亡・勝敗決定でカメラが現場へジャンプ+中央バナー表示(「⚔ 宣戦布告!」「🏰 陥落」等、2.5秒フェード・最大3件キュー)。自動ターンは約1.2秒だけ小休止。手動カメラ操作が常に優先
- **戦況グラフ** (`UI/ScoreGraphPanel.cs`新規): 全文明のスコア推移をTexture2D折れ線(文明色・現在値ラベル付き)でリアルタイム描画。観戦バーと通常プレイ双方の「戦況」ボタンで開閉
- **難易度設定** (`Core/DifficultyRules.cs`新規 + GameConfig.Difficulty): やさしい(AI生産/科学85%・戦闘-10%)/普通(無補正)/むずかしい(120%・+10%)。AIのみに適用、ゲーム設定パネルで選択、PlayerPrefs保存、セーブ形式に永続化。SmokeTestに「SMOKE DIFFICULTY OK」ミニランを追加
- **検証(round 1全合格)**: コンパイル0 / SMOKE OK+MULTI-CIV+ARCHIPELAGO+DIFFICULTY+WARS TOTAL: 2 / WORLD HISTORY SMOKE OK / BUILD OK→書き戻し / 30秒起動テスト例外なし
- 📊 **ベースライン変動の調査記録**: seed42の150ターン結果が(units=103 cities=21 techs=48)→(units=83 cities=20 techs=116)に変化していたが、Codexの技術ツリー108技術統合(research_tech_tree_game_smoke.log 22:27、DifficultyRules.cs作成22:38より前)由来であることをログのビット一致比較で証明。**普通難易度・既定マップ種別は数値的に完全な無影響**を確認済み。以後のseed42正解値は units=83 cities=20 techs=116 wars=2
- 🤝 今回もCodexが並行統合(技術ツリー)したが、双方の最終ソースを保持した状態で両者それぞれが検証し、**両者のビルドがSHA-256完全一致**(決定的ビルド)で収束。宣言プロトコルは「同一ファイルの同時編集回避」としては不完全だが、Codexの統合品質+両側検証で実害ゼロが続いている
- ⚠️ Codex/エディタへ: 新規3ファイル(Core/DifficultyRules.cs, Core/GameEvents.cs, UI/ScoreGraphPanel.cs)の.metaはエディタ生成待ち。生成後はGUID安定のためそのまま保持を

### 2026-07-20 Codex: 世界史図鑑・研究史96件＋文化史96件

- 「すべての研究・文化」は失われた記録、共同研究、複数起源、継続する現代研究、共同体による文化の再創造により有限の確定表にできないため、収録基準・全192件・段階計画・調査基盤を `RESEARCH_CULTURE_CATALOG.md` に新設。
- `Core/ResearchMilestoneCatalog.cs`: 観測・記録・理論・実験・発明・研究制度・伝統的知識を、6地域×16件＝96件の純データ台帳として実装。単独発明者への過度な還元を避け、時期・分野・説明を保持。
- `Core/CulturalTraditionCatalog.cs`: 口承・文学・音楽・舞踊・工芸・思想・儀礼・生活文化を、6地域×16件＝96件の純データ台帳として実装。文化の優劣や固定的民族像は設定しない。
- `UI/WorldHistoryPanel.cs`: 既存の独立Canvasを4タブ（遺跡・史跡／偉人／研究史／文化史）へ拡張。全件＋6地域、6件/ページ、時期・分野・説明を表示。Claude Codeの作業対象だった `UIManager` / `GameBootstrap` / シミュレーション系ファイルは変更せず統合。
- `Assets/Editor/ResearchCultureSmokeTest.cs`: 件数、必須項目、ID一意性、ID検索、全地域16件配分を検証。`Logs/research_culture_unity_smoke_final.log` は研究史96件・文化史96件 `RESEARCH CULTURE SMOKE OK`。
- 回帰検証: `Logs/research_culture_game_smoke.log` は戦争3・和平2・マルチ文明・群島を含め `SMOKE OK`。Unity正規Roslyn設定による現行Player/Editor全体の直接コンパイルもエラー0。純データ単体.NETテストも警告0・エラー0。
- Windowsビルド: `Logs/research_culture_build.log` は `BUILD OK: 89703520 bytes`。一時検証コピーと正式プロジェクトの `Assets` / `Packages` / `ProjectSettings` はSHA-256差分0件。正式 `Build/HexCiv.exe` へ同期済み。`Logs/research_culture_player.log` はUnity 6000.3.20f1、30秒ヘッドレス起動、例外0件。
- 現段階は図鑑収録。次段階は①96件の時代・前提関係を既存技術ツリーへ接続 ②文化ポイント・政策・交流 ③文化勝利 ④文明・指導者・遺跡・偉人との固有相互作用とセーブ対応。

### 2026-07-20 Codex: 世界史図鑑・遺跡84件＋偉人96人

- 「すべての遺跡・偉人」は未発見遺跡、文化遺産の境界、人物評価の主観性により有限の確定表にできないため、収録基準・全180件・順次追加計画を `WORLD_HISTORY_CATALOG.md` に新設。
- `Core/HeritageSiteCatalog.cs`: 6地域の考古遺跡・歴史的建造物・文化的景観84件を純データ台帳として実装。安定ID、日本語名、地域、現在地、時期、種別、説明、関連文明IDを保持。
- `Core/GreatPersonCatalog.cs`: 故人を対象に、学術・芸術・技術・文学・社会運動等の偉人96人（6地域×16人）を純データ台帳として実装。価値順位は付けない。
- `UI/WorldHistoryPanel.cs`: 左下「世界史図鑑」から開く独立Canvas UI。遺跡／偉人タブ、全件＋6地域フィルター、6件/ページ、時期・種別/分野・説明を表示。Esc/×で閉じる。
- Claude Codeの作業中宣言を尊重し、競合対象の `UIManager` / `GameBootstrap` / `SaveLoad` / `SmokeTest` 等は一切変更せず、RuntimeInitializeの独立コンポーネントとして統合。
- `Assets/Editor/WorldHistorySmokeTest.cs`: 件数、ID一意性、必須情報、関連文明参照、地域フィルターを専用検証。
- 検証: `Logs/world_history_catalog_smoke.log` は遺跡84件・偉人96人 `WORLD HISTORY SMOKE OK`。`Logs/world_history_game_smoke.log` は既存の文明56・指導者107・セーブ往復・150ターンAI対戦 `SMOKE OK`。コンパイルエラー0。
- Windowsビルド: `Logs/world_history_build.log` は `BUILD OK: 89658464 bytes`。一時コピーと正式プロジェクトの全スクリプト/シーンをSHA-256比較して差分0件。起動中EXEを強制終了せず、終了後に正式 `Build/HexCiv.exe` へ同期済み。`Logs/world_history_player.log` はUnity 6000.3.20f1、75秒ヘッドレス起動、例外0件。
- 現段階は図鑑収録。次段階は①マップ上の遺跡発見と報酬 ②偉人ポイント ③登用・固有能力・セーブ対応。

### 2026-07-20 Claude Code: シミュレーションモード+自動和平+前線AI+マップ種別、全検証合格

- **シミュレーションモード**(ユーザー指定機能): ゲーム設定パネルから「シミュレーション観戦で開始」。`HumanPlayerIndex=-1`で全文明AI化し、`RunHeadlessTurn()`を非スケール時間で1ターン/(1/速度)秒ずつ自動実行。**既定2倍速**、観戦バーで一時停止/再開・速度切替(等速/2倍速/4倍速、`Time.timeScale`連動)・観戦終了。simulationModeは`ApplyState`で`HumanPlayer==null`から導出(リスタート/ロード経路でも一貫)。観戦中はユニット操作・ターン終了・セーブ/ロード無効、霧なし全体観戦。GameAudioのnullガードは既存のまま安全を確認
- **自動和平**: `Player.WarStartTurns`(開戦ターン記録)+`MakePeaceWith`。膠着(25ターン以上・戦力比0.7〜1.4)で15%/T、劣勢(15ターン以上・戦力0.4倍未満)で25%/T和平(対人間にも適用、ログ通知)。セーブversion 4(敵IDソート済み並列配列、v3以前は開戦=ロード時ターンで補完)
- **戦争AI前線強化**: 遠隔ユニットは非隣接の射撃位置を維持(白兵の後ろ)、白兵は都市隣接タイルを確保、城壁都市への単独特攻をダメージ見積りで抑止
- **マップ種別**: 大陸/パンゲア/群島。**大陸(既定)は従来と生成結果がビット一致**(既存シードのマップ不変=回帰なし)。パンゲア=中央寄せ超大陸(陸44-50%)、群島=高周波ノイズ+高海面(陸30-35%)。設定パネルで選択・PlayerPrefs保存・セーブ互換(v4)
- **検証**: コンパイル0エラー / SMOKE OK + SMOKE MULTI-CIV OK + **SMOKE ARCHIPELAGO OK(群島で7都市)** + **SMOKE WARS TOTAL: 3 / SMOKE PEACE TOTAL: 2**(戦争も和平も実際に発生) / **WORLD HISTORY SMOKE OK**(Codexの図鑑テストもマージ後ツリーで合格) / BUILD OK(89.67MB)→`Build\HexCiv.exe`書き戻し済み / 30秒起動テスト例外なし(6000.3.20f1)
- 運用メモ: 検証エージェントがビルド直前に結果未報告で終了する不具合があったが、コンパイル/スモーク/図鑑テストのログは全合格を確認済みで、残工程(ビルド〜起動テスト)はClaude Code本体が直接実行して完了。Codexが今回は作業中宣言を尊重して図鑑を独立コンポーネント化してくれた — この協調プロトコルは機能している

### 2026-07-20 Codex: 世界史指導者台帳・第1弾107件

- 「歴史上のすべての指導者」は失われた記録、無文字社会、共同統治、地方首長、公職の範囲により有限の確定表にできないため、収録基準・名未詳の扱い・順次追加計画を `LEADERS.md` に新設。
- `Core/LeaderCatalog.cs`: 56文明すべてをカバーする107件を純データ台帳として実装。安定ID、所属文明、日本語名、役職、時期、短い説明、氏名確認状態を保持。インダス・オルメカ・カホキア等は人物を捏造せず「名は未詳」と明記。
- `Player` / `GameBootstrap`: 文明と指導者を組で構築。既存 `BuildNewGame` APIを維持し、文明ID+指導者IDリストのオーバーロードを追加。不正な組合せは所属文明の既定指導者へフォールバック。人間の選択はPlayerPrefsへ保存。
- `SaveLoad`: version 3として指導者IDを決定的に保存・復元。version 1/2の旧セーブは文明の既定指導者を自動補完する後方互換。
- `UIManager`: 上部バーに「指導者変更」、役職・時期・説明を表示する5件/ページ式選択画面、文明名と指導者名の常時表示を追加。現行の3スロットUI等の変更を保持したまま統合。
- `SmokeTest`: 指導者ID一意性、必須情報、所属文明参照、56文明全カバー、名未詳件数、マオリ+テ・ラウパラハ指定構築、指導者セーブ往復を追加。
- Unity 6.3 temp-copy検証（本体エディタ起動中のため）: `Logs/leaders_smoke.log` は指導者107件・全56文明・名未詳7件OK、セーブ往復完全一致、150ターン `SMOKE OK`。コンパイルエラー0。
- Windowsビルド: `Logs/leaders_build.log` は `BUILD OK: 89617833 bytes`。検証済み成果物を `Build/HexCiv.exe` へ反映。`Logs/leaders_player.log` でUnity 6000.3.20f1、55秒ヘッドレス起動、GameBootstrap開始、例外0件を確認。
- 調査基盤: Wikidata Data Access/SPARQL、Met Heilbrunn Timeline、UNESCO資料を `LEADERS.md` に記録。大量追加は国・官職・時代単位で抽出し、典拠照合後に取り込む。

### 2026-07-20 Claude Code: 次候補4件を実装、マージ後の全体検証も合格

- **チュートリアル初回のみ表示**: PlayerPrefs「HexCiv.TutorialSeen」。初回起動のみ自動表示、ロード/リスタート/文明変更では再表示しない。「？ 遊び方」は常時有効
- **AI宣戦条件強化** (`Core/AI/AIController.cs`): ターン25以降、近接(8タイル)+戦力比1.15倍で12%/T、首都近傍の国境摩擦で+6%/T、最弱文明への日和見8%/T。多重戦争は禁止。戦時は軍事生産を優先。SmokeTestに「SMOKE WARS TOTAL」行を追加し、固定シードで戦争発生(TOTAL: 1)を確認
- **セーブスロット3枠化**: `hexciv_save_slot1..3.json`(旧ファイルはスロット1へ自動移行)。SaveDataにメタ情報(ターン/文明/保存日時 — 日時は`GameState.LastSavedAtIso`経由で往復一致を維持)。スロット選択UI(メタ表示付き)。F5/F9=スロット1クイック。`GameActions`に`OnSaveGameSlot`/`OnLoadGameSlot`追加(既存APIは温存)
- **ゲーム設定画面**: マップサイズ3種/文明数2〜8/シード指定。Codexの`BuildNewGame`文明IDオーバーロードを利用し、文明変更・指導者変更の新規ゲームにも設定を適用。SmokeTestに6文明・52x30・40ターンの「SMOKE MULTI-CIV OK」検証を追加
- **⚠️ 並行作業の衝突と収束**: 本作業中(作業中宣言掲示中)にCodexが指導者台帳(19:05〜19:23)を並行実装し、SaveLoad/UIManager/GameBootstrap/SmokeTestが両者から編集された。Codex側が当方のスロットUI等を保持して統合してくれたため破壊なし。**マージ後の最終ツリーを改めて一括検証**: コンパイル0エラー / SMOKE OK(指導者107件+セーブ往復+マルチ文明+戦争発生をすべて含む) / 現行`Build\HexCiv.exe`(19:30)はこの検証済みソースから生成されたものであることをdiffゼロで確認済み
- 📌 **教訓**: 作業中宣言だけでは並行編集を防げなかった。今後、大きめの作業前は相手の直近アクティビティ(ファイル更新時刻)を確認し、作業中はこのファイルの宣言節を互いに尊重すること

### 次の候補(2026-07-20 20時台更新。前リストの前線強化・和平・マップ種別は完了)

- 指導者に固有ボーナスを付与(CIVILIZATIONS.md/LEADERS.md第2〜5段階 — Codexの段階計画と連動)
- 遺跡のマップ配置・発見報酬・偉人ポイント(世界史図鑑の次段階 — Codex計画)
- シミュレーションモードの観戦演出(戦争勃発・都市陥落時のカメラジャンプ、戦況グラフ)
- 難易度設定(AIボーナス/ハンデ)

### 2026-07-20 Codex: 世界文明台帳・第1弾56文明

- 「文明」には有限の標準全件表がないため、都市国家・王国・帝国・遊牧国家・先住民連合を含む収録基準と段階的実装表を `CIVILIZATIONS.md` に新設。
- `Core/CivilizationCatalog.cs`: 全地域を横断する56文明をプレイアブル実装。各文明に安定ID、日本語名、地域、時代、固有色、6件以上の都市名（従来4文明は旧データ互換）を設定。
- `Player`: `CivilizationId` / 地域 / 時代を保持し、都市建設時は文明固有都市名を使用。旧セーブは文明名と旧GameConfigへフォールバック。
- `SaveLoad`: 文明IDを決定的に保存・復元。既存version 1セーブの文明ID欠落にも後方互換。
- `GameBootstrap`: 既存 `BuildNewGame(GameConfig)` を維持し、文明IDリストを受け取るオーバーロードを追加。人間が選んだ文明をPlayerPrefsへ保存し、AIには重複しない文明を割当。
- `UIManager`: 上部バーに「文明変更」、12件×ページ式の56文明選択画面を追加。選択すると新規ゲームとして再構築。チュートリアルの「青系」固定説明も任意色へ修正。
- `SmokeTest`: 文明ID一意性・必須データ・マオリ指定開始（都市名ワイタンギ）を検証に追加。
- 検証: C#コンパイル0エラー。`Logs/civilizations_smoke.log` は文明台帳56件OK、セーブ往復完全一致、150ターン `SMOKE OK`（都市21/ユニット103/技術48）。
- Windowsビルド: `Logs/civilizations_build.log` は `BUILD OK`。`Build/HexCiv.exe` を更新済み。
- UIキャプチャ用Windows操作ヘルパーはテストEXE起動を2回タイムアウトしたため目視操作のみ未実施。ビルド・ヘッドレス検証は合格。

### 2026-07-20 Claude Code: 開発候補3件+D1修正を実装、全検証合格

- **D1修正** (`Audio/GameAudio.cs`): 都市陥落警告SEを「人間プレイヤーの都市名スナップショット照合」方式で修正(CaptureCityがEmitLogより先に所有権を移すため、Version変更時にUpdate()で更新するスナップショットと照合。宣戦布告の警告は従来どおり)
- **チュートリアル実操作連動** (`UIManager.cs`/`InputController.cs`/`GameBootstrap.cs`): ページ→イベント対応表(p2=ユニット選択, p3=移動, p4=都市建設, p5=研究or生産選択, p6=ターン終了)。該当操作を検知すると「✓ できました!」を表示して1秒後に自動ページ送り。手動ナビは従来どおり有効。Space/Tabの自動選択からは連動発火しない(左クリック学習ページの誤進行防止)
- **未行動ユニット順次フォーカス** (`CameraController.cs`/`InputController.cs`/`GameBootstrap.cs`/`UIManager.cs`): `SelectNextIdleUnit()`(Id順サイクル・GotoPath保有/防御態勢は除外)+ `CameraController.FocusOn`。Space/Tabキー+「次のユニット」ボタン。ターン開始時に自動選択
- **検証(temp-copy方式、round 1で全合格)**: コンパイル0エラー / SmokeTest「SMOKE OK」(75ターン→セーブ往復完全一致→復元状態で75ターン継続の新構成) / BUILD OK(89.6MB, 75.9s)→ `Build\HexCiv.exe` へ書き戻し済み / EXE 30秒起動テスト例外なし(Unity 6000.3.20f1)
- ⚠️ 検証時、17:05起動の旧 `HexCiv.exe` プロセスがEXE書き戻しをロックしていたため停止した(手動プレイ中だった場合はご容赦を — 最新版を再起動してください)
- 既知の軽微な挙動: ロード/リスタート時にも「はじめてガイド」がページ1から再表示される(統合時からの既存挙動)

### 2026-07-20 Claude Code: セーブ/ロード機能追加(検証は後続フェーズ)

- `Core/SaveLoad.cs` を新規追加(namespace `HexCiv.Core`、Core純度維持: MonoBehaviour/GameObject不使用、UnityEngineはJsonUtilityとColorのみ)。JsonUtility互換DTO(SaveData/PlayerDto/CityDto/UnitDto)で全シミュレーション状態を直列化。HashSet由来(KnownTechs/AtWarWith/Explored)は決定的ソートで保存し、`Serialize(Deserialize(json)) == json` が成立する。Rng内部状態は保存せず Seed+TurnNumber から再シード(セーブ跨ぎの乱数決定論は非保証と明記)。private Idカウンタは同ファイル内の `partial GameState` で退避・復元
- `Core/Contracts.cs`: `GameActions` に `OnSaveGame` / `OnLoadGame` を追加(追加のみ、既存API不変)
- `GameBootstrap.cs`: `StartNewGame` から `ApplyState(GameState)` を抽出し、新規開始・リスタート・ロードで世界再構築を共通化。保存先は `persistentDataPath/hexciv_save.json`。セーブは人間の手番中かつゲーム終了前のみ。ロードは復元成功時のみ世界を差し替え、失敗時は現行ゲーム続行+「ロードに失敗しました」。ロード後は人間の首都(無ければユニット)へカメラフォーカス
- `UI/UIManager.cs`: トップバーの研究ボタンと文明名の間(x604/x690)に「セーブ」「ロード」ボタンを追加(BGM/SEコントロール・都市パネルと非重複)。`Control/InputController.cs`: F5=セーブ、F9=ロード(ゲーム終了後は無効)。ロード直後は旧InputControllerの同フレーム残処理を中断
- `Assets/Editor/SmokeTest.cs`: 75ターン → セーブ往復完全一致検証(不一致なら "SMOKE FAIL: save roundtrip mismatch")→ 復元状態で75ターン継続、の構成に拡張。"SMOKE OK"/"SMOKE FAIL" マーカーは従来どおり
- ARCHITECTURE.md: §2 SimulationモジュールにSaveLoad.cs追記、GameActions拡張の注記追加
- **未検証**: Unityコンパイル・SmokeTest・ビルドは後続の検証フェーズで実施のこと

### 2026-07-20 Claude Code検証: BGM・SE追加分

- 検証方法: エディタ起動中(UnityLockfile存在)のためバッチ再実行は行わず、①Codexの検証ログ実物確認 ②EXE起動テスト ③全変更ファイルのコードレビュー(レビューエージェント使用)で実施
- `Logs/audio_smoke.log`: SMOKE OK・150ターン結果が従来と完全一致(都市21/ユニット103/技術48) → 音声追加はシミュレーションに無影響
- `Logs/audio_build.log`: BUILD OK (86.5s)。`Build\HexCiv.exe` をバッチモード30秒起動 → Unity 6000.3.20f1・例外なし
- レビュー: 契約違反なし(Core非汚染・MUST-MATCH API維持・AudioListener重複なし・ヘッドレス安全・リスタート安全・PlayerPrefs適切・決定論維持)。**総合PASS**
- 🐛 **要修正 D1** (`Audio/GameAudio.cs:227-231`): 都市陥落時の警告SEが鳴らない。条件が「メッセージに人間文明名を含む」だが、実際のログ(`GameStateOps.cs:146`)は `都市「{都市名}」が陥落した!` で文明名を含まないため、条件が恒偽。修正時の注意: CaptureCityはEmitLogより先に都市の所有を移す可能性があるため、陥落メッセージの都市名照合は「捕獲前スナップショット」か「戦争中の陥落は常に警告」方式が安全
- 軽微(修正不要の観察): BGMのコード進行境界で微小なプチ音(位相不連続)/24秒ループ点で約0.5秒のフェード無音(意図的)/ターン終了ボタンはUIクリック音+ターン終了音の2重再生(仕様の範囲)
- ARCHITECTURE.md §2 のモジュール表に Audio モジュールを追記済み

### 2026-07-20 Codex BGM・SE追加

- `Audio/GameAudio.cs` を追加。外部素材に依存しない実行時生成の戦略BGMと効果音を一元管理。
- SE対象: UIボタン、ユニット/都市選択、移動、戦闘、都市建設、ターン終了、研究完了、生産完了、警告、勝利、敗北。
- 画面右上に `BGM` / `SE` 音量変更と全体ON/OFFを追加。設定は `PlayerPrefs` に保存。
- `Core/` と `GameActions` / `InputController` の公開APIは変更なし。音素材への将来の差し替えは `GameAudio` 内で完結可能。
- UnityエディタのPlay Modeでゲーム起動、サウンド設定UI、`GameAudio` 生成を確認。コンパイルエラー0。
- Unity 6.3 SmokeTest: `SMOKE OK`（150ターン、都市21/ユニット103/技術48。`Logs/audio_smoke.log`）。
- Unity 6.3 Windowsビルド: `BUILD OK`（`Logs/audio_build.log`）。正式版 `Build/HexCiv.exe` を更新済み。

### 2026-07-20 Codex統合

- Claude Codeの高機能版 `HexCiv` を正式プロジェクトに採用。
- Unity 6.3 LTSへ更新。
- 6ページの日本語「はじめてガイド」と再表示ボタンを追加。
- 画面下部に常設の操作説明を追加。
- 移動可能マスを黄色、攻撃可能マスを赤、選択中を白枠に統一。
- すべてのユニットに日本語名ラベルを追加。
- Unity 6で150ターンのヘッドレステスト成功。
- Unity 6 Windows版 `Build\HexCiv.exe` のビルド成功。

### 2026-07-20 Claude Code独立検証(統合の受け入れ確認)

- Unity 6.3バッチコンパイル: エラー0(`Logs/verify_u6_compile.log`)
- SmokeTest.Run (Unity 6.3): SMOKE OK。seed 42・150ターンで都市21/ユニット103/技術48 — 2022.3時代と完全一致し、移行によるシミュレーション挙動の変化なしを確認(`Logs/verify_u6_smoke.log`)
- `Build\HexCiv.exe`: Player.logで Unity 6000.3.20f1 ビルドであることを確認。バッチモード30秒起動テストで例外なし
- コードレビュー: はじめてガイド(UIManager)・ユニット名ラベル(EntityRenderer)・ハイライト配色(MapRenderer)はいずれもARCHITECTURE.mdの契約に適合、MUST-MATCH APIの変更なし
- 結論: **統合を承認**。以後この節の上に新しい状況を追記していく

### 次の候補

(2026-07-20 旧候補3件はすべて完了 → 上記「開発候補3件+D1修正」参照)

- ロード/リスタート時に「はじめてガイド」を再表示しない(初回起動のみ表示にする)
- AIの宣戦条件を強化する(現状スモークテストでは150ターンでAI同士の戦争が発生しない: wars=0)
- セーブスロットの複数化(現在1スロット)
- ゲーム設定画面(マップサイズ・文明数・シード指定)
