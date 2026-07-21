using System.Collections.Generic;
using UnityEngine;
using HexCiv.Core;
using HexCiv.Render;
using HexCiv.UI;
using HexCiv.Audio;

namespace HexCiv.Control
{
    /// <summary>
    /// マウス/キーボード入力の処理。MonoBehaviour ではなく、GameBootstrap が毎フレーム
    /// Update() を呼ぶ。左クリックで自軍ユニット/都市の選択、右クリックで移動・攻撃命令、
    /// Enter でターン終了、Esc でパネルを閉じる/選択解除(どちらの仕事も無ければ
    /// フルスクリーン解除。2026-07-21 追加)。ホバーでタイルツールチップ。
    /// 左/右ボタンはマップ上で押下後、カーソルが閾値(6px)を超えて動くとカメラの
    /// ドラッグパンに切り替わる(中ボタンと同じ地面つかみ方式。パン後の離しでは
    /// 選択・命令を行わない。閾値未満の離しは従来どおりクリック扱い。2026-07-21 追加)。
    /// ゲーム終了後はカメラ(ドラッグパン含む)・再開ボタン・Esc(パネル閉/フルスクリーン解除)
    /// 以外の入力を無視する。
    /// </summary>
    public class InputController
    {
        const float ClickDragThresholdPx = 6f;

        readonly GameState state;
        readonly MapRenderer map;
        readonly EntityRenderer ents;
        readonly UIManager ui;
        readonly GameActions actions;
        readonly CameraController camCtl;

        Unit selectedUnit;

        bool lmbTracking;
        bool lmbDownOverUI;
        Vector2 lmbDownPos;
        /// <summary>左ボタンが閾値超えでドラッグパン中か(2026-07-21 Claude Code 追加)。</summary>
        bool lmbPanning;

        bool rmbTracking;
        bool rmbDownOverUI;
        Vector2 rmbDownPos;
        /// <summary>右ボタンが閾値超えでドラッグパン中か(2026-07-21 Claude Code 追加)。</summary>
        bool rmbPanning;

        int lastSeenVersion = -1;

        /// <summary>ロード実行時に立てる。成功すると GameBootstrap が世界と入力を再構築するため、
        /// この(旧)インスタンスは同フレームの残り処理を行わない。</summary>
        bool abortFrame;

        /// <summary>前フレームに独立Canvas UI(世界史図鑑・文化政策)が開いていたか(2026-07-21 追加)。
        /// これらは同じ Esc 押下を自前の Update で消費して閉じるが、Update の実行順によっては
        /// 本クラスより先に閉じ終わるため、直前フレームの開閉状態も「Esc に仕事があった」判定に含める。</summary>
        bool externalPanelOpenPrevFrame;
        /// <summary>今フレームまたは前フレームに独立Canvas UIが開いていたか(毎フレーム更新)。</summary>
        bool externalPanelOpenRecently;

        public InputController(GameState s, MapRenderer map, EntityRenderer ents, UIManager ui,
            GameActions actions, CameraController camCtl)
        {
            state = s;
            this.map = map;
            this.ents = ents;
            this.ui = ui;
            this.actions = actions;
            this.camCtl = camCtl;
        }

        /// <summary>現在選択中の自軍ユニット(null 可)。</summary>
        public Unit SelectedUnit => selectedUnit;

        /// <summary>毎フレーム呼ばれる入力処理。</summary>
        public void Update()
        {
            if (state == null) return;

            // 独立Canvas UI(世界史図鑑・文化政策)の開閉状態を毎フレーム記録する
            // (Esc のフルスクリーン解除判定用。2026-07-21 Claude Code 追加)
            bool externalOpenNow = ui != null && ui.IsExternalPanelOpen;
            externalPanelOpenRecently = externalOpenNow || externalPanelOpenPrevFrame;
            externalPanelOpenPrevFrame = externalOpenNow;

            // F11:フルスクリーン切替(2026-07-21 Claude Code 追加)。
            // 通常プレイ・観戦モード・ゲーム終了後のいずれでも有効にするため、
            // ゲームオーバーの早期 return より前で処理する。テキスト入力欄(シード値)への
            // 入力中は、HandleKeys 冒頭の他ホットキーと同じ規約で無効化する。
            if (ui != null && !ui.IsTextInputFocused && Input.GetKeyDown(KeyCode.F11))
                ui.RequestFullscreenToggle();

            // F12:スクリーンショット保存(2026-07-21 Claude Code 追加)。
            // F11 と同様、通常プレイ・観戦モード・ゲーム終了後のいずれでも有効にするため、
            // ゲームオーバーの早期 return より前で処理する(テキスト入力とも衝突しない)。
            if (Input.GetKeyDown(KeyCode.F12))
                CaptureScreenshot();

            if (state.IsGameOver)
            {
                // ゲーム終了後はカメラと「もう一度プレイ」ボタン(uGUI側)のみ有効
                if (selectedUnit != null) ClearSelection();
                if (ui != null) ui.HideTileTooltip();

                // 左/右ドラッグのカメラパンはゲーム終了後も有効(2026-07-21 Claude Code 追加)。
                // 戻り値(クリック候補)は捨てるため、選択・命令は一切発生しない。
                // 全画面の暗幕オーバーレイでポインタ判定が常に「UI上」になるため、
                // ここでは UI 上の押下でもパン開始を許可する(「もう一度プレイ」等の
                // ボタンクリックは従来どおり uGUI 側がそのまま処理する)。
                TrackDragPan(0, ref lmbTracking, ref lmbDownOverUI, ref lmbDownPos,
                    ref lmbPanning, rmbPanning, true);
                TrackDragPan(1, ref rmbTracking, ref rmbDownOverUI, ref rmbDownPos,
                    ref rmbPanning, lmbPanning, true);

                // Esc(2026-07-21 Claude Code 追加):ゲーム終了画面でも通常時と同じ優先順位
                // (開いているパネル(最終戦況グラフ等)を閉じる → 何も無ければフルスクリーン解除)
                // で処理する。「もう一度プレイ」ボタンは従来どおり有効。
                if (ui != null && !ui.IsTextInputFocused && Input.GetKeyDown(KeyCode.Escape))
                    HandleEscape();
                return;
            }

            ValidateSelection();
            HandleKeys();
            if (abortFrame)
            {
                abortFrame = false;
                return;
            }
            HandleHover();
            HandleLeftClick();
            HandleRightClick();

            // 状態が変わった(ターン進行・生産完了など)ら選択中のハイライトを更新する
            if (state.Version != lastSeenVersion)
            {
                lastSeenVersion = state.Version;
                ValidateSelection();
                if (selectedUnit != null) PushHighlights();
            }
        }

        /// <summary>選択を解除し、ユニットパネルとハイライトを消す。</summary>
        public void ClearSelection()
        {
            selectedUnit = null;
            if (ui != null) ui.SetSelectedUnit(null);
            if (map != null) map.ClearHighlights();
        }

        /// <summary>
        /// 人間プレイヤーの未行動ユニット(移動力あり・防御態勢でない・移動命令なし)を
        /// Id 昇順で巡回選択する(現在の選択ユニットの次の Id から探し、末尾なら先頭へ戻る)。
        /// 見つかれば通常の左クリック選択と同じ経路(パネル・ハイライト・選択SE)で選択し、
        /// カメラをそのユニットへフォーカスして true を返す。対象がなければ何もせず false。
        /// (2026-07-20 Claude Code 追加)
        /// </summary>
        public bool SelectNextIdleUnit()
        {
            if (state == null || state.IsGameOver) return false;
            var human = state.HumanPlayer;
            if (human == null) return false;

            // Id 昇順の安定した未行動ユニット一覧を作る
            var idle = new List<Unit>();
            for (int i = 0; i < human.Units.Count; i++)
            {
                var u = human.Units[i];
                if (u == null || u.IsDead) continue;
                if (u.MovesLeft <= 0 || u.Fortified) continue;
                if (u.GotoPath != null && u.GotoPath.Count > 0) continue;
                idle.Add(u);
            }
            if (idle.Count == 0) return false;
            idle.Sort((a, b) => a.Id.CompareTo(b.Id));

            // 現在の選択の「次」から巡回(選択なし・選択中が末尾以降なら先頭)
            var next = idle[0];
            if (selectedUnit != null)
            {
                for (int i = 0; i < idle.Count; i++)
                {
                    if (idle[i].Id > selectedUnit.Id) { next = idle[i]; break; }
                }
            }

            SelectUnitAndFocus(next);
            return true;
        }

        // ---- 内部処理 ----

        /// <summary>選択中ユニットが死亡・喪失していたら選択を解除する。</summary>
        void ValidateSelection()
        {
            if (selectedUnit == null) return;
            var human = state.HumanPlayer;
            if (selectedUnit.IsDead || human == null || selectedUnit.PlayerId != human.Id)
                ClearSelection();
        }

        void HandleKeys()
        {
            // テキスト入力欄(ゲーム設定のシード値)へ入力中はホットキーを処理しない
            // (Enter/Space/Tab/F5/F9 が入力操作と衝突するのを防ぐ。Esc は InputField 自身が処理する)
            // (2026-07-20 Claude Code 追加)
            if (ui != null && ui.IsTextInputFocused) return;

            // 観戦モード(人間プレイヤー不在)ではターン終了・ユニット巡回・セーブ/ロードの
            // ホットキーを無効化する。Esc(パネルを閉じる)のみ有効(2026-07-20 Claude Code 追加)
            bool spectator = state.HumanPlayer == null;

            if (!spectator &&
                (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
            {
                if (actions != null && actions.OnEndTurn != null) actions.OnEndTurn();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                HandleEscape();
                return;
            }

            if (spectator) return;   // 以降(Space/Tab/F5/F9)は人間プレイヤーがいる時のみ

            // Space / Tab:次の未行動ユニットを巡回選択(2026-07-20 Claude Code 追加)。
            // ゲーム終了後は Update 冒頭で弾かれる。テキスト入力中は本メソッド冒頭のガードで弾かれる。
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Tab))
            {
                SelectNextIdleUnit();
                return;
            }

            // F5=クイックセーブ / F9=クイックロード(いずれもスロット1。
            // ゲーム終了後は Update 冒頭で弾かれるため、プレイ中のみ有効)
            if (Input.GetKeyDown(KeyCode.F5))
            {
                if (actions != null)
                {
                    if (actions.OnSaveGameSlot != null) actions.OnSaveGameSlot(1);
                    else if (actions.OnSaveGame != null) actions.OnSaveGame();
                }
                return;
            }

            if (Input.GetKeyDown(KeyCode.F9))
            {
                // ロード成功時は GameBootstrap が入力/描画/UIを再構築するため、
                // この(旧)インスタンスは同フレームの残り処理を中断する
                abortFrame = true;
                if (actions != null)
                {
                    if (actions.OnLoadGameSlot != null) actions.OnLoadGameSlot(1);
                    else if (actions.OnLoadGame != null) actions.OnLoadGame();
                }
                return;
            }
        }

        /// <summary>
        /// Esc の優先処理(2026-07-21 Claude Code 変更。従来の「パネルを閉じる/選択解除」に
        /// フルスクリーン解除を追加):
        /// ① まず従来どおりの仕事を行う — 開いているパネル(都市/技術/設定/文明/指導者/スロット/
        /// 戦況グラフ/はじめてガイド)を閉じ、選択中ユニットを解除する。世界史図鑑・文化政策の
        /// 独立UIは同じ Esc 押下を自前の Update で消費して閉じる(本クラスからは閉じない)。
        /// ② ①の仕事が一切無かった(パネルなし・選択なし・独立UIも直前まで開いていない)場合のみ、
        /// フルスクリーン中ならウィンドウ表示へ戻す。切替は F11 と同一経路
        /// (UIManager.RequestFullscreenToggle → GameBootstrap.ToggleFullscreen)のため、
        /// ゲーム設定のラベルと PlayerPrefs "HexCiv.Fullscreen" も常に同期する。
        /// 通常プレイ・観戦モード・ゲーム終了画面のすべてで同じ優先順位で動く。
        /// </summary>
        void HandleEscape()
        {
            bool hadPanel = ui != null && ui.AnyPanelOpen;
            bool hadSelection = selectedUnit != null;

            if (ui != null)
            {
                ui.CloseAllPanels();
                ui.HideTutorial();   // はじめてガイドも Esc で閉じる(非表示なら no-op)
            }
            if (hadSelection) ClearSelection();

            // 独立UIが今フレームまたは直前フレームに開いていた場合、この Esc は
            // 「パネルを閉じる」操作として消費された(またはこれから消費される)とみなす
            if (hadPanel || hadSelection || externalPanelOpenRecently) return;

            if (ui != null && ui.IsFullscreenActive) ui.RequestFullscreenToggle();
        }

        /// <summary>
        /// スクリーンショットを保存する(2026-07-21 Claude Code 追加。F12)。
        /// 保存先: Application.persistentDataPath/screenshots/hexciv_yyyyMMdd_HHmmss.png
        /// (フォルダは無ければ作成)。ScreenCapture は非同期にフレーム末尾で書き出すため、
        /// ログ表示はここで即時に出す。失敗してもゲーム進行には影響させない(警告ログのみ)。
        /// </summary>
        void CaptureScreenshot()
        {
            try
            {
                string dir = System.IO.Path.Combine(Application.persistentDataPath, "screenshots");
                System.IO.Directory.CreateDirectory(dir);
                string file = System.IO.Path.Combine(dir,
                    "hexciv_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");
                ScreenCapture.CaptureScreenshot(file);
                if (ui != null) ui.AddLog("スクリーンショットを保存しました");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("スクリーンショットの保存に失敗しました: " + e.Message);
            }
        }

        void HandleHover()
        {
            if (ui == null) return;

            if (ui.IsPointerOverUI())
            {
                ui.HideTileTooltip();
                return;
            }

            if (!TryGetTileUnderMouse(out var coord))
            {
                ui.HideTileTooltip();
                return;
            }

            var tile = state.Map != null ? state.Map.Get(coord) : null;
            if (tile == null)
            {
                ui.HideTileTooltip();
                return;
            }

            // 未踏のタイルは表示しない(人間プレイヤーがいない場合は全表示)
            var human = state.HumanPlayer;
            if (human != null && !human.Explored.Contains(coord))
            {
                ui.HideTileTooltip();
                return;
            }

            ui.ShowTileTooltip(tile);
        }

        void HandleLeftClick()
        {
            // 押下追跡+閾値超えのドラッグパン。true=パンせずに離した(クリック候補)のみ続行
            // (2026-07-21 Claude Code 変更。閾値未満の離しの選択動作は従来と同一)
            if (!TrackDragPan(0, ref lmbTracking, ref lmbDownOverUI, ref lmbDownPos,
                    ref lmbPanning, rmbPanning))
                return;

            if (lmbDownOverUI) return;
            if (ui != null && ui.IsPointerOverUI()) return;
            if (((Vector2)Input.mousePosition - lmbDownPos).magnitude >= ClickDragThresholdPx) return;
            if (!TryGetTileUnderMouse(out var coord)) return;

            DoSelect(coord);
        }

        void HandleRightClick()
        {
            // 押下追跡+閾値超えのドラッグパン。true=パンせずに離した(クリック候補)のみ続行
            // (2026-07-21 Claude Code 変更。閾値未満の離しの移動・攻撃命令は従来と同一。
            // 従来「何もしなかった」閾値超えの右ドラッグがカメラパンになる)
            if (!TrackDragPan(1, ref rmbTracking, ref rmbDownOverUI, ref rmbDownPos,
                    ref rmbPanning, lmbPanning))
                return;

            if (rmbDownOverUI) return;
            if (ui != null && ui.IsPointerOverUI()) return;
            if (((Vector2)Input.mousePosition - rmbDownPos).magnitude >= ClickDragThresholdPx) return;
            if (selectedUnit == null || selectedUnit.IsDead) return;
            if (!TryGetTileUnderMouse(out var coord)) return;

            DoOrder(coord);
        }

        /// <summary>
        /// 指定マウスボタンの押下追跡とドラッグカメラパン(2026-07-21 Claude Code 追加)。
        /// 押下時に位置とUI上か(downOverUI)を記録し、UI外の押下でカーソルが閾値
        /// (ClickDragThresholdPx)を超えて動いたら CameraController.TryBeginDragPan で
        /// 「地面つかみ」パンを開始する(掴んだ地点=押下位置の真下の地点がカーソル下に
        /// 留まる。中ボタンドラッグと同じ実装を共有し、数学の重複はない)。以後ボタンを
        /// 離すまで毎フレーム UpdateDragPan で追従し、離したら EndDragPan で終了する。
        /// 戻り値: このフレームでボタンが「パンせずに」離された(=呼び出し元は従来どおり
        /// クリック処理を続行してよい)。パンした場合の離しでは false を返すため、
        /// 選択・移動/攻撃命令・チュートリアル通知は一切発火しない。
        /// otherPanning: 他ボタンがパン中は新たなパンを開始しない(カメラの掴み点は1つ。
        /// その場合でも既存の閾値チェックにより、閾値超えの離しがクリック扱いになることはない)。
        /// allowStartOverUI: ゲーム終了画面(全面オーバーレイでポインタが常にUI上)用に、
        /// UI上の押下でもパン開始を許可する。通常プレイでは false(UI上の押下はパンしない)。
        /// </summary>
        bool TrackDragPan(int button, ref bool tracking, ref bool downOverUI, ref Vector2 downPos,
            ref bool panning, bool otherPanning, bool allowStartOverUI = false)
        {
            if (Input.GetMouseButtonDown(button))
            {
                tracking = true;
                downOverUI = ui != null && ui.IsPointerOverUI();
                downPos = (Vector2)Input.mousePosition;
                panning = false;
            }

            if (tracking && (!downOverUI || allowStartOverUI) && Input.GetMouseButton(button))
            {
                if (!panning && !otherPanning && camCtl != null &&
                    ((Vector2)Input.mousePosition - downPos).magnitude >= ClickDragThresholdPx)
                    panning = camCtl.TryBeginDragPan(downPos);
                if (panning && camCtl != null) camCtl.UpdateDragPan(Input.mousePosition);
            }

            if (!Input.GetMouseButtonUp(button)) return false;
            if (!tracking) return false;
            tracking = false;
            bool panned = panning;
            panning = false;
            if (panned && camCtl != null) camCtl.EndDragPan();
            return !panned;
        }

        /// <summary>左クリック:自軍ユニット選択 → 自軍都市パネル → 選択解除。</summary>
        void DoSelect(HexCoord coord)
        {
            var human = state.HumanPlayer;
            if (human == null) return;
            var tile = state.Map != null ? state.Map.Get(coord) : null;
            if (tile == null)
            {
                ClearSelection();
                return;
            }

            // 敵ユニットは選択できない
            if (tile.Unit != null && tile.Unit.PlayerId == human.Id && !tile.Unit.IsDead)
            {
                selectedUnit = tile.Unit;
                if (ui != null)
                {
                    ui.CloseAllPanels();
                    ui.SetSelectedUnit(selectedUnit);
                }
                PushHighlights();
                GameAudio.Instance?.PlaySelect();
                if (ui != null) ui.NotifyTutorialEvent("unit_selected");   // チュートリアル連動
                return;
            }

            if (tile.City != null && tile.City.PlayerId == human.Id)
            {
                ClearSelection();
                if (ui != null) ui.ShowCityPanel(tile.City);
                GameAudio.Instance?.PlaySelect();
                return;
            }

            ClearSelection();
            if (ui != null) ui.CloseAllPanels();
        }

        /// <summary>右クリック:攻撃可能なら攻撃、そうでなければ経路探索して移動命令。</summary>
        void DoOrder(HexCoord coord)
        {
            var tile = state.Map != null ? state.Map.Get(coord) : null;
            if (tile == null) return;
            if (coord == selectedUnit.Coord) return;

            if (Combat.CanAttack(state, selectedUnit, tile))
            {
                GameAudio.Instance?.PlayAttack();
                Combat.PerformAttack(state, selectedUnit, tile);
                lastSeenVersion = state.Version;
                if (selectedUnit == null || selectedUnit.IsDead) ClearSelection();
                else PushHighlights();
                return;
            }

            // 目的地に敵がいる場合は攻撃移動として経路を許可する(手前で隣接停止する)
            bool enemyGoal =
                (tile.Unit != null && tile.Unit.PlayerId != selectedUnit.PlayerId) ||
                (tile.City != null && tile.City.PlayerId != selectedUnit.PlayerId);

            var path = Pathfinder.FindPath(state, selectedUnit, coord, enemyGoal);
            if (path == null) return;

            GameAudio.Instance?.PlayMove();
            selectedUnit.OrderMove(state, path);
            lastSeenVersion = state.Version;
            if (ui != null) ui.NotifyTutorialEvent("unit_moved");   // チュートリアル連動
            if (selectedUnit.IsDead) ClearSelection();
            else PushHighlights();
        }

        /// <summary>
        /// ユニットを選択状態にし(DoSelect のユニット選択と同じ経路)、カメラをフォーカスする。
        /// チュートリアルの unit_selected イベントは左クリック選択の学習用のため、ここでは発火しない。
        /// (2026-07-20 Claude Code 追加)
        /// </summary>
        void SelectUnitAndFocus(Unit u)
        {
            selectedUnit = u;
            if (ui != null)
            {
                ui.CloseAllPanels();
                ui.SetSelectedUnit(selectedUnit);
            }
            PushHighlights();
            GameAudio.Instance?.PlaySelect();
            if (camCtl != null) camCtl.FocusOn(u.Coord.ToWorld());
        }

        /// <summary>選択中ユニットの移動可能範囲・攻撃対象・移動経路をハイライト表示する。</summary>
        void PushHighlights()
        {
            if (map == null || selectedUnit == null) return;

            var reachable = new HashSet<HexCoord>(Pathfinder.ReachableThisTurn(state, selectedUnit).Keys);
            var attackable = new HashSet<HexCoord>(Pathfinder.AttackableTiles(state, selectedUnit));
            List<HexCoord> path = selectedUnit.GotoPath != null
                ? new List<HexCoord>(selectedUnit.GotoPath)
                : null;

            map.SetHighlights(reachable, attackable, path, selectedUnit.Coord);
        }

        Camera ActiveCamera()
        {
            if (camCtl != null && camCtl.Cam != null) return camCtl.Cam;
            return Camera.main;
        }

        bool TryGetTileUnderMouse(out HexCoord coord)
        {
            coord = default;
            var cam = ActiveCamera();
            if (cam == null || map == null) return false;
            return map.TryGetTileUnderMouse(cam, out coord);
        }
    }
}
