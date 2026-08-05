# HexCiv 販売ハンドオフ

更新日: 2026-08-05

この文書は、ゲーム本体を担当するCodexから、販売サイトと掲載作業を担当するClaude Codeへの
受け渡しです。仮説の判定記録は
`C:\Users\kanta\Documents\Obsidian Vault\HexCiv販売_仮説台帳.md` を正本とします。

## 現在販売できるもの

- 製品版: `dist/HexCiv-v1.0-win64.zip`
- 30ターン無料体験版: `dist/HexCiv-Demo-v1.0-win64.zip`
- Windows 10/11 64bit、日本語表示、買い切り
- 自サイト価格: ¥500
- 体験版セーブは製品版で続行可能

2026-08-05 Stage 4K検証済み配布物:

| 版 | サイズ | SHA256 |
|---|---:|---|
| 製品版 | 37,600,650 bytes | `a7493aee7bf8c477d3642f3e4393e5521305abac02800f92086d1a78dd9dc726` |
| 30ターンDemo | 37,603,352 bytes | `029c9c40bcbb8c48a0a0e0bfd0fd5e152c0cf383988881f33eda4d06a657fad4` |

両版ともUnity 6000.3.20f1で12秒起動し、重大例外0件。manifestのサイズ・SHA256と
実ZIPの一致も確認済み。販売サイトへ差し替える際は2ファイルを混同しない。

配布前にmanifestのSHA256とZIPを照合し、同じ版番号の古いZIPを誤配布しないでください。

## 実ゲーム画像

`GameplayScreenshot.Capture` が同一seedを30/80/150ターンまで進め、1920×1080で撮影します。
2026-08-05に3枚とも目視し、黒画面、欠け、例外がなく、領土・都市・部隊が増える差を確認済みです。

| 画像 | 用途 | 推奨キャプション |
|---|---|---|
| `Logs/gameplay_turn30.png` | 序盤 | 未開の大地に都市と国境が生まれる |
| `Logs/gameplay_turn80.png` | 中盤 | 交易・研究・外交と領土争いが広がる |
| `Logs/gameplay_turn150.png` | 終盤 | 文明が大陸を覆い、歴史の結果が地図に残る |
| `Logs/marketing/uruk_stage4f_water_arbitration.png` | ウルク編の新機能 | 3つの水利要求を比較し、推定上下流関係から第三者仲裁を選ぶ |
| `Logs/marketing/uruk_stage4g_land_rights.png` | ウルク編の土地外交 | 観測された収穫と双方の推定根拠から、共同耕作・補償・仲裁を選ぶ |
| `Logs/marketing/uruk_stage4h_water_agreement_selection.png` | ウルク編の合意管理 | 3つの履行中合意から対象を選び、表示中の相手だけを破約・再交渉する |
| `Logs/marketing/uruk_stage4i_kinship_diplomacy.png` | ウルク編の親族外交 | 氏名不詳・双方同意前提・推定と明示し、相手を選んで親族連携を提案する |
| `Logs/marketing/uruk_stage4j_information_transmission.png` | ウルク編の情報伝達 | 口頭・封泥・数量記録の年代・信頼度を選び、媒体史料と推定シナリオを分けて確認する |
| `Logs/marketing/uruk_stage4k_transport_forecast.png` | ウルク編の情報照合輸送 | 数量記録が後続輸送の数量条件と推定危険幅へ結び付くことを確認する |

Stage 4F画像は `UrukRegionalScreenshot.CaptureArbitrationCandidate`、Stage 4G画像は
`UrukRegionalScreenshot.CaptureLandRightsCandidate`、Stage 4H画像は
`UrukRegionalScreenshot.CaptureWaterAgreementCandidate`、Stage 4I画像は
`UrukRegionalScreenshot.CaptureKinshipCandidate`、Stage 4J画像は
`UrukRegionalScreenshot.CaptureInformationCandidate`、Stage 4K画像は
`UrukRegionalScreenshot.CaptureTransportForecastCandidate` で実キャンペーン状態から760px幅で
再生成できる。撮影は `-batchmode -force-d3d11` を使い、`-nographics` は使わない。
Null描画デバイスの場合は灰色画像を成功扱いせず失敗する。2026-08-05生成版は次のとおり。

| 画像 | サイズ | SHA256 |
|---|---:|---|
| Stage 4F | 77,743 bytes | `2930238ad5a878a3e5ab727a957bab5c04f248968a6d857784e7b5f5c146888c` |
| Stage 4G | 86,642 bytes | `2e4dc0cd5d0da878efc3d00967c055e4a9320f6ac0a416f3925ecb188038451a` |
| Stage 4H | 81,774 bytes | `1be02ed34766fee67f60be54e9593df7e93e39f4929aad17a0ad8f181aedeb59` |
| Stage 4I | 82,882 bytes | `4b231b5e8b170c8934960fc9964d14d873c38a10ba4bde8a406eb2bdc74266b5` |
| Stage 4J | 98,582 bytes | `50a46cfb605cd3c3286dacb4b5474ec6805a8db92aa2a7a4af5c548f24ab8e05` |
| Stage 4K | 110,049 bytes | `22ef2632e7c7e5c4fb7c2f0afcc7b2995570c08e01fd1ff8e488c31b5af97722` |

商品ページへ載せる際は圧縮前のPNGを使い、実ゲーム画面であること、上下流・季節利用・境界が
`inferred` な復元であることを明記する。

## itch.io非公開ページの原稿

### タイトル

HexCiv — 文明が育つ歴史4X

### 短い説明

ヘックス世界で都市を築き、研究・文化・外交・戦争を選ぶ、日本語のWindows向け4X戦略ゲーム。

### 冒頭

小さな開拓者から始まった文明が、都市を増やし、文化を生み、隣国と出会い、やがて世界史になる。
HexCivは「育っていく世界を見届ける」ことを中心にした、1人用ターン制4Xストラテジーです。

### 特徴

- 1ヘックス1部隊の地図上戦闘、都市建設、研究、文化、外交、経済
- 軍事・科学・文化・経済・存続による複数の勝利条件
- ランダム世界の通常ゲームと、紀元前4000年頃の南メソポタミアを扱うウルク編
- 貸付、労務、通行権、朝貢を、貨幣ではなく現物・期限・輸送として扱う地域契約
- 取水と収穫から発生する土地・耕作権問題を、共同耕作・補償・仲裁・拒否・再交渉で扱う地域外交
- 複数の水利要求・履行中合意・再交渉案件を切り替え、表示中の相手だけへ判断を適用する誤操作防止
- 氏名不詳・双方同意前提・推定を明示し、共同食と贈答から交易路の安全へつなげる親族外交
- 口頭伝言・封泥付き荷・数量記録板を、年代・行政・粘土・到着・誤解とともに扱い、媒体史料と推定シナリオを分ける情報伝達
- 到着済み情報を契約輸送へ照合し、連絡なしでは危険不明、記録ありでは媒体別の推定危険幅と数量条件を示す情報の霧
- 世界史図鑑、指導者、遺跡、偉人、作品、研究、文化をゲーム内で確認
- 観戦モードと自動管理。4X初心者向けの導入ガイド
- BGM・SE、セーブ/ロード、一般的な内蔵GPU向けの軽量表示

### 体験版

無料体験版は製品版と同じゲームを30ターンまで遊べます。セーブデータに体験版制限は
書き込まれないため、製品版を購入後に31ターン目から続行できます。

### 明示事項

- 対応OS: Windows 10/11 64bit
- 表示言語: 日本語のみ
- 形式: ZIPを展開し、`HexCiv.exe`を実行
- 1人用。オンライン対戦なし
- 開発中の更新版。購入者は同じ商品ページから将来版を再ダウンロード可能

### タグ候補（最大10）

`Strategy`, `Turn-based Strategy`, `4X`, `Historical`, `Simulation`, `Hex Based`,
`Singleplayer`, `Procedural Generation`, `Management`, `Windows`

## H5/H9計測用リンク

itch.io説明欄から自サイトへ戻すリンクは次のUTMを使います。

`https://my-web-app-b67f4.web.app/shop/hexciv?utm_source=itch_io&utm_campaign=h5_itch_channel`

体験版内または体験版説明から購入ページへ送る場合は次を使います。

`https://my-web-app-b67f4.web.app/shop/hexciv?utm_source=itch_io&utm_campaign=h9_demo`

## H4/H7投稿実験

X投稿12本の事前登録下書きと2×2実験設計は
`MARKETING_EXPERIMENT_H4_H7.md` に保存した。P1本番行確認とP2画像公開が終わるまでは
投稿せず、開始後は価格・商品ページ見出し・購入ボタンを同時変更しない。

## H10公開更新実験

公開更新3週と基準3週を比較する6週間の事前登録は
`MARKETING_EXPERIMENT_H10_UPDATES.md` に保存した。P1/P2、itch.io本人確認、H4/H7終了を
開始条件とし、それまでは開始日・売上・訪問を未計測として扱う。更新週はテスト済みビルド、
変更履歴、X告知を一組で公開し、基準週は開発を続けても公開更新とHexCiv投稿を行わない。
Stage 4F/4G/4H/4I/4J/4Kは開始前に完成した更新候補であり、まだH10第1週として公開・計測していない。

## Claude Codeの次アクション

1. 自サイトの商品ページへ実ゲーム画像3枚を追加するPR [#4408](https://github.com/kanta13jp1/my_web_app/pull/4408) は、12件の画面テストと主要CIを通過済み。必須 `release-notes-data` は最新main取り込み後の生成JSONが古いことだけで失敗しているため、承認後に `scripts/generate_release_notes.py` を実行し、生成差分だけを追加してマージする。
2. itch.ioで非公開ページを作成し、630×500カバー、確認済み画像3～5枚、製品ZIP、無料Demo ZIPを登録する。
3. 日本語のみであることを、購入ボタンより前に表示する。
4. P1の本番SQL確認とプローブ行削除を行う。
5. 公開日から30日間、`itch_io` と `direct` の4段ファネルを週次でObsidianへ記録する。
6. 外部公開・価格変更・有料広告は、対象と実験IDを記録してから実行する。

### H5 itch.ioアクセス状況（2026-08-05）

Codexが `https://itch.io/dashboard` へ到達したところ、Cloudflareの本人確認画面が表示された。
自動回避やアカウント作成は行わず、ブラウザをユーザー操作待ちで残している。本人確認とログインが
完了するまでは非公開商品ページは未作成であり、H5の開始日・訪問者数は未計測とする。

PR #4408が本番へ反映されるまではP2を完了扱いにせず、H2の計測開始日も記録しません。掲載後は画像以外の主要因を同時変更せず、`product_view` と `purchase_click` の分母・分子を日付付きで保存します。

## Codexの継続アクション

- ゲーム本体、製品/Demoビルド、セーブ互換、回帰試験を維持する。
- 大きな機能完成ごとに実キャンペーン状態の画像を再撮影し、同じ解像度・描画条件で比較可能にする。
- 販売文面に書く機能は、実ビルドで再現できるものだけにする。
