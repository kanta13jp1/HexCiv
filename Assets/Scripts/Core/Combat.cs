using System;

namespace HexCiv.Core
{
    /// <summary>
    /// 戦闘処理。和平中の文明への攻撃は自動的に宣戦布告になる。
    /// 近接:双方がダメージ。遠隔:攻撃側は無傷。都市HPは遠隔では1未満にならず、
    /// 近接でHP1以下になると占領される。民間人(戦闘力0)は防御時に強さ1として扱う。
    /// </summary>
    public static class Combat
    {
        /// <summary>攻撃可能か。対象タイルに敵ユニットまたは敵都市があり、射程と移動力の条件を満たす。</summary>
        public static bool CanAttack(GameState s, Unit attacker, Tile target)
        {
            if (s == null || attacker == null || target == null) return false;
            if (attacker.IsDead || attacker.MovesLeft <= 0) return false;
            var def = attacker.Def;
            if (def.Strength <= 0 && def.RangedStrength <= 0) return false; // 民間人は攻撃不可

            bool enemyCity = target.City != null && target.City.PlayerId != attacker.PlayerId;
            bool enemyUnit = target.Unit != null && target.Unit.PlayerId != attacker.PlayerId;
            if (!enemyCity && !enemyUnit) return false;
            // 最初の海戦モデルは同一領域だけで解決する。艦船は陸上都市を占領せず、
            // 陸軍も海上艦を攻撃しない（将来の沿岸砲撃は遠隔艦として別契約で追加する）。
            if (def.IsNaval && enemyCity) return false;
            if (enemyUnit && target.Unit.Def.IsNaval != def.IsNaval) return false;

            int dist = attacker.Coord.DistanceTo(target.Coord);
            if (def.IsRanged) return dist >= 1 && dist <= def.Range;
            return dist == 1;
        }

        /// <summary>タイルの防御側の実効防御力(UI/AIの見積もり用)。空タイルは0。</summary>
        public static float EffectiveDefense(GameState s, Tile tile)
        {
            if (tile == null) return 0f;
            if (tile.City != null)
            {
                var city = tile.City;
                return GameRules.HealthScaledStrength(city.DefenseStrength(s), city.Hp, city.MaxHp);
            }
            if (tile.Unit != null)
            {
                var u = tile.Unit;
                int baseStr = Math.Max(1, u.Def.Strength); // 民間人は強さ1で防御
                float eff = GameRules.HealthScaledStrength(baseStr, u.Hp, GameRules.UnitMaxHp);
                eff = LogisticsSystem.ScaleCombat(u, eff);
                float bonus = 1f + GameRules.DefenseBonusAt(tile) + (u.Fortified ? GameRules.FortifyDefenseBonus : 0f);
                return eff * bonus;
            }
            return 0f;
        }

        /// <summary>攻撃を実行する。和平中なら自動宣戦。攻撃後は移動力0・行動済みになる。</summary>
        public static void PerformAttack(GameState s, Unit attacker, Tile target)
        {
            if (!CanAttack(s, attacker, target)) return;

            var atkOwner = s.GetPlayer(attacker.PlayerId);
            bool targetIsCity = target.City != null && target.City.PlayerId != attacker.PlayerId;
            int defPlayerId = targetIsCity ? target.City.PlayerId : target.Unit.PlayerId;
            var defOwner = s.GetPlayer(defPlayerId);

            // 和平中への攻撃 → 自動宣戦布告
            if (atkOwner != null && !atkOwner.IsAtWarWith(defPlayerId))
                atkOwner.DeclareWarOn(s, defOwner);

            bool ranged = attacker.Def.IsRanged;
            int atkBase = ranged ? attacker.Def.RangedStrength : attacker.Def.Strength;
            float atkEff = GameRules.HealthScaledStrength(atkBase, attacker.Hp, GameRules.UnitMaxHp);
            atkEff = LogisticsSystem.ScaleCombat(attacker, atkEff);
            atkEff *= NaturalGeographySystem.RiverCrossingAttackMultiplier(s, attacker, target.Coord);
            // AI攻撃側は難易度に応じて実効戦闘力を補正(普通=±0で無変換。2026-07-20 追加)
            atkEff = DifficultyRules.ScaleCombatForAI(s, attacker.PlayerId, atkEff);

            // 攻撃前の座標を控える(勝利進駐で attacker.Coord が変わるため。表示イベント用。2026-07-21 追加)
            HexCoord attackerCoordBefore = attacker.Coord;
            HexCoord targetCoord = target.Coord;
            int dmgToDefender, dmgToAttacker;

            if (targetIsCity)
                AttackCity(s, attacker, atkOwner, target.City, atkEff, ranged, out dmgToDefender, out dmgToAttacker);
            else
                AttackUnit(s, attacker, target, atkEff, ranged, out dmgToDefender, out dmgToAttacker);

            if (!attacker.IsDead)
            {
                attacker.MovesLeft = 0;
                attacker.ActedThisTurn = true;
                attacker.Fortified = false;
                attacker.GotoPath = null;
            }

            if (defOwner != null) s.EliminateIfDefeated(defOwner);
            s.CheckDominationVictory();
            s.Bump();

            // 表示層向けの戦闘解決イベント(購読者がいなければ no-op。ヘッドレス実行でも安全。2026-07-21 追加)
            s.RaiseCombatResolved(attackerCoordBefore, targetCoord, dmgToDefender, dmgToAttacker);
        }

        static void AttackUnit(GameState s, Unit attacker, Tile target, float atkEff, bool ranged,
            out int dmgToDefender, out int dmgToAttacker)
        {
            dmgToDefender = 0;
            dmgToAttacker = 0;
            var defender = target.Unit;
            bool defenderCivilian = defender.Def.Strength <= 0;

            // 近接で民間人を攻撃 → 捕獲(民間人は破壊され、攻撃側が進駐)
            if (!ranged && defenderCivilian)
            {
                s.KillUnit(defender);
                s.EmitLog($"{attacker.Def.NameJa}が敵の{defender.Def.NameJa}を倒した");
                attacker.Relocate(s, target.Coord);
                return;
            }

            float defEff = EffectiveDefense(s, target);
            // AI防御側は難易度に応じて実効戦闘力を補正(普通=±0で無変換。2026-07-20 追加)
            defEff = DifficultyRules.ScaleCombatForAI(s, defender.PlayerId, defEff);
            int dmg = GameRules.CombatDamage(atkEff, defEff, s.Rng);
            defender.Hp -= dmg;
            dmgToDefender = dmg;

            if (!ranged)
            {
                int back = GameRules.CombatDamage(defEff, atkEff, s.Rng);
                attacker.Hp -= back;
                dmgToAttacker = back;
            }

            bool defDead = defender.IsDead;
            bool atkDead = attacker.IsDead;

            if (defDead)
            {
                s.KillUnit(defender);
                s.EmitLog($"{attacker.Def.NameJa}が敵の{defender.Def.NameJa}を倒した");
            }
            if (atkDead)
            {
                s.KillUnit(attacker);
                s.EmitLog($"{defender.Def.NameJa}が敵の{attacker.Def.NameJa}を倒した");
            }

            // 近接で防御側が倒れ、攻撃側が生存 → 進駐
            if (!ranged && defDead && !atkDead)
                attacker.Relocate(s, target.Coord);
        }

        static void AttackCity(GameState s, Unit attacker, Player atkOwner, City city, float atkEff, bool ranged,
            out int dmgToDefender, out int dmgToAttacker)
        {
            dmgToDefender = 0;
            dmgToAttacker = 0;
            float defEff = GameRules.HealthScaledStrength(city.DefenseStrength(s), city.Hp, city.MaxHp);
            // AI都市の防御は難易度に応じて実効戦闘力を補正(普通=±0で無変換。2026-07-20 追加)
            defEff = DifficultyRules.ScaleCombatForAI(s, city.PlayerId, defEff);
            int dmg = GameRules.CombatDamage(atkEff, defEff, s.Rng);
            dmgToDefender = dmg;

            if (ranged)
            {
                // 遠隔攻撃では都市HPは1未満にならない(占領は近接のみ)
                city.Hp = Math.Max(1, city.Hp - dmg);
                return;
            }

            int back = GameRules.CombatDamage(defEff, atkEff, s.Rng);
            int newHp = city.Hp - dmg;
            attacker.Hp -= back;
            dmgToAttacker = back;

            if (attacker.IsDead)
            {
                city.Hp = Math.Max(1, newHp);
                s.KillUnit(attacker);
                s.EmitLog($"都市「{city.NameJa}」の防衛が敵の{attacker.Def.NameJa}を倒した");
                return;
            }

            if (newHp <= 1)
            {
                city.Hp = Math.Max(1, newHp);
                s.CaptureCity(city, atkOwner);
                attacker.Relocate(s, city.Coord);
            }
            else
            {
                city.Hp = newHp;
            }
        }
    }
}
