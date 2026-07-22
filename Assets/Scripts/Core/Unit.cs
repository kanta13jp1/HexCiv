using System;
using System.Collections.Generic;

namespace HexCiv.Core
{
    /// <summary>1体のユニット。1タイル1体(1UPT)。</summary>
    public class Unit
    {
        public int Id;
        public int PlayerId;
        public string DefId;
        public HexCoord Coord;

        public UnitDef Def => GameRules.GetUnit(DefId);

        /// <summary>HP(最大 GameRules.UnitMaxHp)。</summary>
        public int Hp = GameRules.UnitMaxHp;
        /// <summary>残り移動力(移動ポイント)。</summary>
        public int MovesLeft;
        public bool Fortified;
        /// <summary>このターンに移動または攻撃した(翌ターン開始時の回復を妨げる)。</summary>
        public bool ActedThisTurn;
        /// <summary>ターン開始時に都市補給網から確定した補給状態。</summary>
        public SupplyLevel Supply = SupplyLevel.Supplied;
        /// <summary>連続して孤立判定を受けたターン数。補給回復時に0へ戻る。</summary>
        public int TurnsOutOfSupply;
        /// <summary>複数ターンにわたる移動経路。null なら無し。</summary>
        public List<HexCoord> GotoPath;

        public bool CanAct => MovesLeft > 0 && !IsDead;
        public bool IsDead => Hp <= 0;

        /// <summary>
        /// ターン開始処理:回復(前ターン未行動時のみ、領土種別+防御態勢ボーナス)、
        /// 移動力リセット、GotoPath の続行。
        /// </summary>
        public void ResetForNewTurn(GameState s)
        {
            if (IsDead) return;

            // 回復(前ターンに行動していない場合のみ)
            if (!ActedThisTurn && Hp < GameRules.UnitMaxHp)
            {
                var tile = s.Map.Get(Coord);
                var owner = s.GetPlayer(PlayerId);
                int heal;
                if (tile != null && tile.OwnerPlayerId == PlayerId)
                    heal = GameRules.HealOwnTerritory;
                else if (tile != null && tile.OwnerPlayerId >= 0 && owner != null && owner.IsAtWarWith(tile.OwnerPlayerId))
                    heal = GameRules.HealEnemyTerritory;
                else
                    heal = GameRules.HealNeutral;
                if (Fortified) heal += GameRules.HealFortifyBonus;
                heal = LogisticsSystem.ScaleHealing(this, heal);
                Hp = Math.Min(GameRules.UnitMaxHp, Hp + heal);
            }

            ActedThisTurn = false;
            MovesLeft = LogisticsSystem.MovementAllowance(this);

            // GotoPath の続行
            if (GotoPath != null && GotoPath.Count > 0)
            {
                while (MovesLeft > 0 && GotoPath != null && GotoPath.Count > 0)
                {
                    var next = GotoPath[0];
                    if (!TryStepTo(s, next))
                    {
                        // 経路が塞がれた → 中止
                        GotoPath = null;
                        break;
                    }
                    if (GotoPath != null) GotoPath.RemoveAt(0);
                }
                if (GotoPath != null && GotoPath.Count == 0) GotoPath = null;
            }
        }

        /// <summary>
        /// 隣接タイルへ1歩移動する。コストを支払い、タイル参照と視界を更新する。
        /// 塞がれている(占有・進入不可・敵都市)場合は false。
        /// </summary>
        public bool TryStepTo(GameState s, HexCoord next)
        {
            if (IsDead || MovesLeft <= 0) return false;
            if (Coord.DistanceTo(next) != 1) return false;
            var t = s.Map.Get(next);
            if (t == null || !t.IsPassable) return false;
            if (t.Unit != null) return false;
            if (t.City != null && t.City.PlayerId != PlayerId) return false;

            int cost = GameRules.MoveCostInto(t);
            var cur = s.Map.Get(Coord);
            if (cur != null && cur.Unit == this) cur.Unit = null;
            t.Unit = this;
            Coord = next;
            MovesLeft = Math.Max(0, MovesLeft - cost);
            Fortified = false;
            ActedThisTurn = true;

            var owner = s.GetPlayer(PlayerId);
            if (owner != null)
            {
                Visibility.Recompute(s, owner);
                WorldLegacySystem.CheckDiscoveryAt(s, owner, Coord);
            }
            return true;
        }

        /// <summary>
        /// 経路(開始タイルを含まない)に沿って移動力の続く限り進み、残りは GotoPath に保存する。
        /// </summary>
        public void OrderMove(GameState s, List<HexCoord> path)
        {
            GotoPath = null;
            if (path == null || path.Count == 0)
            {
                s.Bump();
                return;
            }
            Fortified = false;

            int i = 0;
            bool blocked = false;
            while (i < path.Count)
            {
                if (MovesLeft <= 0) break;
                if (!TryStepTo(s, path[i]))
                {
                    blocked = true;
                    break;
                }
                i++;
            }

            if (!blocked && i < path.Count)
                GotoPath = new List<HexCoord>(path.GetRange(i, path.Count - i));

            s.Bump();
        }

        /// <summary>戦闘勝利後の進駐など、コストを払わずタイルを移す内部処理。</summary>
        internal void Relocate(GameState s, HexCoord c)
        {
            var cur = s.Map.Get(Coord);
            if (cur != null && cur.Unit == this) cur.Unit = null;
            var t = s.Map.Get(c);
            if (t != null) t.Unit = this;
            Coord = c;
            var owner = s.GetPlayer(PlayerId);
            if (owner != null)
            {
                Visibility.Recompute(s, owner);
                WorldLegacySystem.CheckDiscoveryAt(s, owner, Coord);
            }
        }
    }
}
