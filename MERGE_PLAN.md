# 統合計画: HexCiv × CivilizationLike (Hex Empires)

> このファイルは両プロジェクトのルートに同内容で置かれています。
> 対象読者: このPC上で作業する全AIアシスタント(Claude Code / Unity 6.3側アシスタント)と、ユーザー本人。
> 更新したら両方のコピーを同期してください。

## 現状比較(2026-07-20 16時時点の事実)

| 項目 | HexCiv (`C:\Users\kanta\GitHub\HexCiv`) | CivilizationLike (`C:\Users\kanta\CivilizationLike`) |
|---|---|---|
| Unityバージョン | 2022.3.29f1 | 6.3 LTS (6000.3.20f1) |
| コード量 | C# 31ファイル / 約228KB | C# 7ファイル / 約50KB(うち1ファイル38KB) |
| マップ | ランダム大陸生成(気候帯/丘陵/森林/山岳/資源5種/戦場の霧) | 小型固定規模のヘックスマップ |
| 文明 | 4文明(プレイヤー+AI3、宣戦・攻城AI) | 2文明(自動研究・自動生産) |
| システム | 技術12種・ユニット7種・建物5種・都市成長/生産キュー・1UPT戦闘(遠隔/地形ボーナス/都市HP)・複数ターン移動・スコア/制覇勝利 | 戦士/開拓者・首都占領勝利・**セーブ/ロード(JSON)**・**日本語チュートリアル(実装中)** |
| UI | 全日本語(都市/技術/ユニットパネル、ログ、ツールチップ) | 英語ベース+日本語化作業中 |
| 検証 | バッチコンパイル0エラー / 150ターンAI対戦ヘッドレステスト合格 / EXEビルド済み / 起動30秒例外なし | Unity 6コンパイル・実画面確認済み(アシスタント報告) |
| 設計文書 | ARCHITECTURE.md(モジュール間API契約)、README.md | README.md |

## 統合方針(推奨案)

**HexCivを統合先(ベース)とする。** 理由: 機能量・検証状態で大差があり、「内容が充実している方を統合先にする」という方針に合致する。

CivilizationLikeから移植する価値があるもの:
1. **セーブ/ロード** — `CivilizationGame.cs` の SaveGame/LoadGame (JsonUtility + persistentDataPath) の方式を参考に、HexCivでは `Core/SaveLoad.cs`(純C#、GameStateシリアライズ)+ UIボタンとして新規実装するのが適切(HexCivのCore層はMonoBehaviour禁止のため移植ではなく再実装)
2. **ゲーム内日本語チュートリアル** — HexCivでは `UI/TutorialPanel.cs` を新設し `UIManager.Init` から表示(初回のみ+「遊び方」ボタンで再表示)
3. 移動/攻撃可能マスのハイライトはHexCivに実装済み(MapRenderer.SetHighlights)のため移植不要

## ✅ ユーザー決定(2026-07-20 16時台・確定)

1. **統合先 = HexCiv、Unity 6.3 (6000.3.20f1) へ移行する**
2. **統合作業はUnity 6.3側アシスタントが主導。Claude Codeは待機し、レビューと検証のみ担当**

### Unity 6.3移行の技術メモ(移行担当者向け)

- 6.3エディタ: `C:\Program Files\Unity\Hub\Editor\6000.3.20f1\Editor\Unity.exe`
- プロジェクトはBuilt-in Render Pipelineのまま(URP変換不要)。使用シェーダーは `Sprites/Default` とフォントマテリアルのみ
- 想定される警告(動作には支障なし): Input Manager非推奨警告 / `FindObjectOfType` 系の deprecation(`FindFirstObjectByType` へ置換推奨)
- uGUI(legacy Text)・TextMesh・`Font.CreateDynamicFontFromOSFont` は6.3でも動作する
- バッチ検証コマンド(移行後は6.3のUnity.exeで実行。**エディタを閉じてから**):
  - コンパイル: `-batchmode -nographics -quit -projectPath C:\Users\kanta\GitHub\HexCiv -logFile Logs\compile.log`
  - 150ターンAI対戦テスト: `-batchmode -nographics -projectPath . -executeMethod SmokeTest.Run`
  - ビルド: `-executeMethod SceneSetup.EnsureScene` → `-executeMethod BuildScript.PerformBuild`
  - ※ Unity.exeはGUIサブシステムのため、PowerShellでは `Start-Process -Wait` で終了を待つこと

### Claude Codeが統合完了後に実施する検証(引き渡し時に声をかけてください)

1. 6.3バッチコンパイル(エラー0確認) → 2. SmokeTest.Run(150ターン完走) → 3. EXEビルド → 4. プレイヤー起動テスト → 5. セーブ/ロードとチュートリアルの動作レビュー

## 共同作業のルール(重要)

1. **HexCivの `Assets/Scripts/Core/{HexCoord,Enums,Yields,Defs,GameRules,Tile,HexMap,GameConfig,GameState,Contracts}.cs` は凍結ファイル**。変更する場合は ARCHITECTURE.md の契約も更新すること
2. 新機能はARCHITECTURE.mdの名前空間/モジュール構成に従う(Core=純C#でMonoBehaviour禁止、UI=HexCiv.UI、など)
3. **Unityバッチ処理(コンパイル/テスト/ビルド)を同時に走らせない** — `Temp/UnityLockfile` が存在する間は他方が実行中。検証コマンドはREADME.md記載(SmokeTest.Run / SceneSetup.EnsureScene / BuildScript.PerformBuild)
4. 大きな変更の前に、このファイルの「作業ログ」に1行追記して意図を宣言する
5. CivilizationLikeは統合完了後もしばらく残す(削除はユーザー判断)

## 作業ログ

- 2026-07-20 16:1x [Claude Code] 本計画書を両プロジェクトに配置。HexCivは未着手のまま(統合方針・分担のユーザー回答待ち)
- 2026-07-20 16:2x [Claude Code] ユーザー決定を記録: HexCivベース+Unity 6.3移行、Unity 6.3側アシスタント主導。HexCivのmanifest.jsonが6.3形式に更新済みであることを確認(移行開始とみなす)。Claude Codeは待機に入る — 以後、統合完了の引き渡しがあるまでHexCivのコード・Unityバッチ処理には触れない
- 2026-07-20 16:4x [Claude Code] **統合完了・検証承認済み。本ファイルの役目は終了** — 以後の状況共有と作業ルールは `C:\Users\kanta\GitHub\HexCiv\COLLABORATION.md` に一本化(Codex作: CLAUDE.md/AGENTS.mdも参照)。検証結果はCOLLABORATION.md「2026-07-20 Claude Code独立検証」を参照
