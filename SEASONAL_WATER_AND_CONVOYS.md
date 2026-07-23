# 季節水系・橋梁・護送船団 — 実装仕様

最終更新: 2026-07-23

## 実装したゲーム規則

- 氾濫原は12ターン周期で、増水2ターン、退水後の肥沃期3ターン、平常7ターンを繰り返す。
- 恒久的な氾濫原ボーナスは食料+1。増水中は食料-1・進入コスト+1、肥沃期はさらに食料+1。
- 建築学は「橋梁網」を解禁する。河川圏都市で完成させると都市労働圏の渡河移動・近接攻撃・増水移動ペナルティを無効化する。
- 港は海上補給源となる。交戦中の敵戦闘部隊または敵港湾都市が水域に隣接すると、その水域は封鎖される。
- 港を持つ都市の「護送船団庁」は単独封鎖を突破する。二つ以上の封鎖戦力には遮断される。
- 都市占領時、恒久施設の港は残るが、守備と指揮に依存する橋梁網・護送船団庁は失われる。

洪水季節は `TurnNumber` から計算し、施設は既存の建物ID一覧に入るため、新しいセーブ項目は不要である。Coreは乱数を消費せず、同じ状態から同じ産出・経路・戦闘補正を返す。

## 表示

`MapRenderer` は外部画像を使わず、増水時の半透明水面と波線、肥沃期の堆積帯、河道の流向に直交する橋桁を結合メッシュで生成する。増水面は共有マテリアルを複製せず `MaterialPropertyBlock` のアルファだけを正弦波で変える。軽量演出モードでは固定表示になる。

## 史料から抽象化した点

- FEMAの氾濫原資料は、氾濫原が洪水を貯留・通水し、堆積や生産性にも関係することを説明する。HexCivでは連続流体計算を行わず、増水と退水後肥沃化の季節状態へ抽象化した。
- U.S. Armyの河川渡河資料は、渡河地点、橋頭堡、工兵・架橋資材、交通統制の重要性を扱う。HexCivでは個別工兵ユニットではなく都市圏建物「橋梁網」へ集約した。
- UNCTADは港を船舶ネットワークと後背地輸送の接点として扱う。HexCivでは港から海上へ出て自領沿岸へ戻る補給経路として表現した。
- 海軍史料の船団命令と船団記録は商船の編成・武装護衛を示す。HexCivでは個別船舶をまだ持たないため、護送船団庁による封鎖耐性として表現した。

## 参照資料

- [FEMA — Floodplain Management Requirements Study Guide](https://www.fema.gov/pdf/floodplain/is_9_complete.pdf)
- [U.S. Army — Tactical River Crossings](https://www.armyupress.army.mil/Journals/Military-Review/English-Edition-Archives/March-April-2026/Tactical-River-Crossings/)
- [UNCTAD — Resilient Ports as a Key Pillar of a Resilient Maritime Supply Chain](https://resilientmaritimelogistics.unctad.org/guidebook/2-resilient-ports-key-resilient-maritime-supply-chain)
- [U.S. Naval History and Heritage Command — Order for Ships in Convoy (1917)](https://www.history.navy.mil/research/publications/documentary-histories/wwi/october-1917/rear-admiral-albert.html)
- [The National Archives — Royal Navy operations and Merchant Navy convoy records](https://www.nationalarchives.gov.uk/help-with-your-research/research-guides/royal-navy-operations-second-world-war)

## 検証

`FloodBridgeConvoySmokeTest` は季節4地点、季節産出、増水移動、研究だけでは消えない渡河補正、橋梁網、建設条件、占領時喪失、単独封鎖、船団突破、二重封鎖、和平解除をUnityバッチモードで検証する。既存の `HydrologyMaritimeSmokeTest` も明示的な橋梁網を使う仕様へ更新した。
