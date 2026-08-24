# HexCiv 販売計測開始 — 2026-08-25

## 判定

- P1: 完了。本番行を集計確認し、検証用1行を対象限定で削除した。
- P2: 完了。2026-08-22に実ゲーム画像3枚を本番公開した。
- H1: 計測中。2026-08-25を集計確認済みの開始日とする。
- H2: 計測中。画像露出は2026-08-22開始、集計確認は2026-08-25。
- H4/H7: 開始ゲート通過済み。Xアカウントが一時的read-only modeのため初回投稿不能。
- H5: itch.ioはログイン済み・Draftフォーム利用可能。Publisher規約と支払設定の完了待ち。

## P1の本番集計

| 時点 | source | stage | 到達人数 |
|---|---|---|---:|
| 検証用行削除前 | direct | product_view | 3 |
| 検証用行削除前 | probe_claude | product_view | 1 |
| 検証用行削除後 | direct | product_view | 3 |

削除対象は `product_id=hexciv-win64`、`source=probe_claude` の検証用1行だけ。
statement timeout 5秒の短いtransactionを使い、削除件数1を確認した。個人識別子は取得・保存していない。

## P2とH2の初期値

商品ページPR: <https://github.com/kanta13jp1/my_web_app/pull/4702>

本番deploy: <https://github.com/kanta13jp1/my_web_app/actions/runs/32579800151>

| 期間 | product_view | purchase_click | 押下率 |
|---|---:|---:|---:|
| 画像公開前 | 2 | 0 | 0% |
| 画像公開後 | 1 | 0 | 0% |
| 合計 | 3 | 0 | 0% |

0押下はP1のテーブル確認後に得た実測値。ただし分母3人では結論を出さず、各案100ユニーク訪問者
または14日の事前登録条件まで保留する。欠測値は0へ置換しない。

## 次の外部操作

1. itch.io非公開Draftの対象、価格、通貨、公開範囲、2ZIP、画像を送信直前に確認する。
2. H4/H7の1本目について本文、画像、UTM URL、投稿先を送信直前に確認する。
3. 実行後に公開URL・時刻・初期カウンタをObsidian正本へ記録する。

## 外部チャネルの確認結果

### itch.io

- Account: `kanta13jp1`
- Draftフォーム: 利用可能
- 予定slug: `hexciv`（2026-08-25時点の公開URLはHTTP 404）
- 警告: 支払設定がなく、最低価格を0より大きくしてもダウンロード不能
- 未完了: Publisher規約、決済接続、Draft保存、ファイル送信、公開
- カバー: `MarketingAssets/itchio/hexciv_itchio_cover_630x500_v1.png`
- Cover SHA256: `6577f134dad257cce3a9544498949e10e91032761a441d6f035bd6387ec0f32f`
- Artifact: `hexciv-itchio-cover-v1-20260825` / lifecycle `release_candidate`

### X

- Account: `@kanta13jp1`
- Login: 済み
- 状態: 一時的read-only mode。投稿・Repost・Like不可
- H4/H7: 初回投稿未送信、計測開始日は未設定
