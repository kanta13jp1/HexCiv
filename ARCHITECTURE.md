# HexCiv — Architecture Contract

Unity 2022.3.29f1 / C# / no external assets. A Civilization-like 4X strategy prototype:
procedural hex map, turn-based, cities, tech tree, combat, fog of war, AI civs. UI text is **Japanese**.

This document is the **binding contract** between modules. Foundation files (already written, listed in §2)
must NOT be modified by module agents. Pinned public APIs (marked `MUST-MATCH`) must be implemented
with exactly these signatures, because other modules are written against them in parallel.

## 1. Golden rules

1. **Namespaces**: simulation `HexCiv.Core` (+ `HexCiv.Core.AI`), rendering `HexCiv.Render`, UI `HexCiv.UI`, input/camera `HexCiv.Control`. Bootstrap: `HexCiv` root namespace.
2. **Simulation purity**: nothing under `Assets/Scripts/Core/` may derive from MonoBehaviour, create GameObjects, or call UnityEngine APIs other than pure math/structs (`Mathf`, `Color`, `Vector3`). It must run headless in batch mode (the smoke test drives 150 turns without any scene).
3. **Determinism**: simulation randomness uses `state.Rng` (System.Random) only. `UnityEngine.Random` is allowed only in presentation code.
4. **1 unit per tile (1UPT)**. A unit may never end its move on an occupied tile. Moving onto an enemy-occupied tile/city = melee attack.
5. **Fonts**: built-in `LegacyRuntime.ttf` has NO Japanese glyphs. All text (uGUI `Text` and 3D `TextMesh`) must use the OS font helper (§7, `UIStyle.JapaneseFont()`), which tries `"Yu Gothic UI", "Meiryo UI", "Meiryo", "MS Gothic"` via `Font.CreateDynamicFontFromOSFont`. For `TextMesh`, also assign `font.material` to the MeshRenderer.
6. **Shaders/materials**: only shaders guaranteed in builds: `Sprites/Default` (unlit, vertex color, transparency) for all colored meshes/quads, and font materials from the Font object. Do not use Standard/URP materials or custom shaders.
7. **All source files UTF-8.** Japanese string literals go directly in code.
8. **File ownership** is strict (§2). Never write a file owned by another module; integration fixes cross-module drift.
9. Write **complete, compiling C#** — no TODO stubs on the pinned APIs.

## 2. Modules, owners, files

| Module | Files (under Assets/Scripts unless noted) |
|---|---|
| Foundation (DONE, frozen) | `Core/HexCoord.cs, Core/Enums.cs, Core/Yields.cs, Core/Defs.cs, Core/GameRules.cs, Core/Tile.cs, Core/HexMap.cs, Core/GameConfig.cs, Core/GameState.cs, Core/Contracts.cs` |
| Simulation | `Core/GameStateOps.cs, Core/Player.cs, Core/Unit.cs, Core/City.cs, Core/Combat.cs, Core/Pathfinder.cs, Core/Visibility.cs, Core/TurnManager.cs, Core/SaveLoad.cs, Core/CivilizationCatalog.cs, Core/LeaderCatalog.cs, Core/HeritageSiteCatalog.cs, Core/GreatPersonCatalog.cs, Core/ResearchMilestoneCatalog.cs, Core/CulturalTraditionCatalog.cs, Core/MasterpieceCatalog.cs, Core/GlobalHistoryIndex.cs, Core/TechnologyCatalog.cs, Core/CulturePolicyCatalog.cs, Core/CultureSystem.cs, Core/WorldLegacySystem.cs, Core/MasterpieceSystem.cs, Core/AdministrationSystem.cs` (SaveLoad: 決定的なセーブ/ロード直列化。CivilizationCatalog: 92文明。LeaderCatalog: 指導者179件。HeritageSiteCatalog: 遺跡・史跡120件。GreatPersonCatalog: 偉人132人。ResearchMilestoneCatalog: 研究史132件。CulturalTraditionCatalog: 文化史132件。MasterpieceCatalog: 書籍・絵画・彫刻・建築・音楽・演劇・映画336件。GlobalHistoryIndex: 13分類・台帳1123件の横断集計と文明詳細地域の6地域写像。TechnologyCatalog: 既存12技術+研究史132件。CulturePolicyCatalog/CultureSystem: 文化史132件の政策化、文化ポイント、交流、文化勝利。WorldLegacySystem: 遺産12件の決定的配置・発見報酬、偉人ポイント・世界一意の登用・分野別効果・文明地域親和性。MasterpieceSystem: 作品ポイント、世界一意の収蔵、7分野効果、偉人連携。AdministrationSystem: 国庫・税制・維持費・安定度・戦争疲弊・総合産出倍率・AI税制勧告。各Catalog/SystemはMonoBehaviour非依存の純データ/ロジック) |
| Map generation | `Core/MapGenerator.cs` |
| AI | `Core/AI/AIController.cs` (helpers allowed in `Core/AI/`) |
| Rendering | `Rendering/MapRenderer.cs, Rendering/EntityRenderer.cs, Rendering/HeritageRenderer.cs` (HeritageRenderer: 探索済み遺産タイルの独立マーカー。helpers allowed in `Rendering/`) |
| UI | `UI/UIManager.cs, UI/UIStyle.cs, UI/WorldHistoryPanel.cs, UI/CulturePanel.cs, UI/LegacyPanel.cs, UI/AdministrationPanel.cs` (WorldHistoryPanel: 総合・文明・指導者を含む世界史台帳8画面とゲーム内状態を閲覧。CulturePanel: 文化政策・影響力。LegacyPanel: 遺産探索・偉人登用・作品収蔵。AdministrationPanel: 国庫・収支・税制・安定度・戦争疲弊を比較・操作し、手続き生成アイコン、開閉アニメーション、`Resources/Administration/administration_banner` のオリジナル装飾画像を持つ。いずれもsortingOrder 130以上の独立Canvas。helpers allowed in `UI/`) |
| Input/Camera | `Control/CameraController.cs, Control/InputController.cs` |
| Audio (added 2026-07-20, Codex) | `Audio/GameAudio.cs` — namespace `HexCiv.Audio`; procedural BGM/SFX, presentation-only (reads Core state, never mutates; no state.Rng usage) |
| Integration | `GameBootstrap.cs`, `Assets/Editor/SmokeTest.cs`, `Assets/Editor/SceneSetup.cs`, `Assets/Editor/BuildScript.cs` |

Foundation summary: `HexCoord` (axial, pointy-top, `ToWorld/FromWorld/Range/Ring/DistanceTo`, odd-r offset conversion),
`Tile` (terrain/hill/forest/resource/owner/Unit/City + `GetYields()`), `HexMap` (`Get/InBounds/AllTiles/NeighborsOf/TilesInRange`),
`GameRules` (all data tables + formulas: `MoveCostInto`, `DefenseBonusAt`, `GrowthFoodNeeded`, `CombatDamage`, `HealthScaledStrength`, constants),
`GameState` (partial: Map/Players/TurnNumber/Version/Bump()/OnLog/EmitLog/TakeNextUnitId/TakeNextCityId/HumanPlayer/GetPlayer),
`GameConfig` (map 44x26, 4 civs, names/colors/city name lists), `IAIController`, `GameActions`, `ProductionItem`.
Note: `GameActions` に `OnSaveGame` / `OnLoadGame` を追加(2026-07-20、セーブ/ロード。実装は `Core/SaveLoad.cs`、配線は GameBootstrap §9。UI: セーブ/ロードボタン+F5/F9)。
Note: `GameConfig` に `MapType` を追加(2026-07-20、0=大陸(既定)/1=パンゲア/2=群島。`MapGenerator` が生成パラメータを分岐、SaveLoad version 5 で永続化(旧セーブは0)、UI: ゲーム設定「マップ種別」行。PlayerPrefs "HexCiv.MapType")。
Note: SaveLoad version 10 で国庫・税制・安定度・戦争疲弊・直近収支を永続化。version 9以前は国庫120・均衡税・安定度60で補完する。version 9 で作品ポイント・収蔵を追加し、version 7以前は `TurnManager` 構築時にseed由来で遺産を決定的に補完する。

## 3. Simulation module (MUST-MATCH APIs)

### GameStateOps.cs — `public partial class GameState`
```csharp
public IEnumerable<Unit> AllUnits { get; }        // live units of all players
public IEnumerable<City> AllCities { get; }
public Unit CreateUnit(Player owner, string unitDefId, HexCoord coord);   // places on tile, updates tile.Unit
public void KillUnit(Unit u);                     // removes from tile+player
public City FoundCity(Player owner, HexCoord coord);  // creates city, claims border tiles (radius 1), names it via owner.NextCityName(this)
public bool CanFoundCityAt(Player owner, HexCoord coord); // passable land, no city within DistanceTo < GameRules.MinCityDistance, tile not owned by another civ
public void CaptureCity(City city, Player newOwner);  // transfer city+tiles, hp to 50%, capital handling, elimination check
```

### Player.cs
```csharp
public class Player {
    public int Id; public string NameJa; public Color Color; public bool IsHuman; public bool IsEliminated;
    public List<Unit> Units; public List<City> Cities;
    public HashSet<string> KnownTechs;           // starts with GameRules.StartingTech
    public string CurrentResearchId;             // null = none
    public int ScienceStored;                    // accumulates even with no research selected; spent when a tech completes
    public HashSet<string> KnownCulturePolicies; // adopted culture-policy ids
    public string CurrentCulturePolicyId;        // null = none
    public int CultureStored; public int TotalCulture;
    public Dictionary<int,int> CulturalInfluence; // target player id -> influence
    public int GreatPersonPoints; public int TotalGreatPersonPoints;
    public HashSet<string> DiscoveredHeritageSites; public HashSet<string> RecruitedGreatPeople;
    public int MasterpiecePoints; public int TotalMasterpiecePoints;
    public HashSet<string> CollectedMasterpieces; // world-unique work ids owned by this civilization
    public int Treasury; public TaxPolicy Taxation; public int Stability; public int WarWeariness;
    public int LastRevenue; public int LastExpenses; // AdministrationSystemが更新する直近収支
    public HashSet<HexCoord> Explored; public HashSet<HexCoord> Visible;
    public int CapitalCityId;                    // -1 if none
    public HashSet<int> AtWarWith;
    public bool IsAtWarWith(int playerId);
    public void DeclareWarOn(GameState s, Player other);   // mutual, EmitLog "「A」が「B」に宣戦布告した!"
    public int SciencePerTurn(GameState s);      // GameRules.BaseSciencePerCiv (if capital alive) + Σ city (pop + building science)
    public List<TechDef> AvailableTechs();       // prereqs met, not known
    public void SetResearch(string techId);
    public List<CulturePolicyDef> AvailableCulturePolicies();
    public void SetCulturePolicy(string policyId);
    public bool HasTech(string techId);          // null/empty => true
    public string NextCityName(GameState s);     // from GameConfig.CityNames[civ index], fallback "新しい都市N"
    public int MilitaryPower();                  // Σ non-civilian units (Strength + RangedStrength) * hp/100
}
```

### Unit.cs
```csharp
public class Unit {
    public int Id; public int PlayerId; public string DefId; public HexCoord Coord;
    public UnitDef Def { get; }                  // GameRules.GetUnit(DefId)
    public int Hp;                               // max GameRules.UnitMaxHp
    public int MovesLeft;                        // in movement points
    public bool Fortified;
    public bool ActedThisTurn;                   // moved or attacked (blocks healing next turn-start)
    public List<HexCoord> GotoPath;              // pending multi-turn path, null if none
    public bool CanAct => MovesLeft > 0 && !IsDead;
    public bool IsDead { get; }
    public void ResetForNewTurn(GameState s);    // moves reset, healing (GameRules.Heal* by territory, +HealFortifyBonus if fortified), continue GotoPath
    public bool TryStepTo(GameState s, HexCoord next);  // adjacent step, pays cost, updates tiles+visibility, returns false if blocked
    public void OrderMove(GameState s, List<HexCoord> path);  // executes steps while MovesLeft>0, stores remainder in GotoPath
}
```
Movement cost: a step always costs `GameRules.MoveCostInto(tile)`; a unit may enter a tile if `MovesLeft > 0` even when cost > MovesLeft (Civ-style), MovesLeft floors at 0.

### City.cs
```csharp
public class City {
    public int Id; public int PlayerId; public string NameJa; public HexCoord Coord;
    public int Population;                       // starts 1
    public int FoodStored; public int ProductionStored;
    public int Hp; public int MaxHp;             // GameRules.CityMaxHp
    public List<string> Buildings;               // building ids
    public ProductionItem CurrentProduction;     // null = none selected
    public int DefenseStrength(GameState s);     // CityBaseStrength + pop*CityStrengthPerPop + walls CityDefense (+ garrison Def.Strength/4 if unit on tile)
    public Yields ComputeYields(GameState s);    // city center tile + best `Population` owned unworked-by-others tiles within CityWorkRadius; +building bonuses; science += pop
    public List<ProductionItem> AvailableProduction(GameState s);  // units (tech ok) + buildings (tech ok, not already built)
    public void SetProduction(ProductionItem item);
    public int TurnsToComplete(GameState s); public int TurnsToGrow(GameState s);   // 99 if never
    public void ProcessTurnStart(GameState s);   // yields -> food growth/starvation, production progress -> spawn/complete, hp heal +CityHealPerTurn
    public void ExpandBordersIfNeeded(GameState s); // radius 2 at pop >= CityBorderGrowPop (claim unowned tiles only)
}
```
Production spawn: unit appears on city tile if free, else first free passable neighbor; if none, stays queued (retry next turn). On complete: `EmitLog($"「{NameJa}」で{item.NameJa}が完成した")`.

### Combat.cs
```csharp
public static class Combat {
    public static bool CanAttack(GameState s, Unit attacker, Tile target);  // enemy unit or enemy city on target; melee: adjacent & MovesLeft>0; ranged: DistanceTo <= Range & MovesLeft>0
    public static void PerformAttack(GameState s, Unit attacker, Tile target);
    public static float EffectiveDefense(GameState s, Tile tile);           // helper for UI/AI estimates
}
```
Rules: attacking a player you are at peace with auto-declares war first. Melee vs unit: both take `GameRules.CombatDamage` (defender's effective strength × (1 + tile DefenseBonus + fortify bonus)); if defender dies, attacker moves in (captures civilian: civilian is destroyed). Ranged vs unit: attacker takes no damage. Vs city: city hp floors at 1 for ranged; melee at city hp<=1 captures via `s.CaptureCity`. Attacker's `MovesLeft = 0` and `ActedThisTurn = true` after any attack. Civilians (Strength 0) defend at strength 1.

### Pathfinder.cs
```csharp
public static class Pathfinder {
    // A*; path excludes start, includes goal; null if unreachable. Occupied tiles are blocked
    // except the goal when it holds an enemy (for attack moves) — controlled by allowEnemyGoal.
    public static List<HexCoord> FindPath(GameState s, Unit u, HexCoord goal, bool allowEnemyGoal = false);
    public static Dictionary<HexCoord, int> ReachableThisTurn(GameState s, Unit u);   // coord -> cost, within MovesLeft (entering with MovesLeft>0 allowed)
    public static List<HexCoord> AttackableTiles(GameState s, Unit u);  // tiles with enemy unit/city this unit could attack right now
}
```
Friendly-occupied tiles: never a valid destination. Tiles of civs we are NOT at war with holding units: treated as blocked.

### Visibility.cs
```csharp
public static class Visibility {
    public static void Recompute(GameState s, Player p);  // Visible = union of unit sight ranges (Def.Sight, +1 if on hill) and city ranges (GameRules.CitySight); Explored |= Visible
    public static void RecomputeAll(GameState s);
}
```

### TurnManager.cs
```csharp
public class TurnManager {
    public TurnManager(GameState state, IAIController ai);
    public void BeginGame();          // visibility init, EmitLog opening line
    public void EndTurn();            // human ended: AI players PlayTurn in order -> AdvanceTurn()
    public void RunHeadlessTurn();    // ALL players (incl. index HumanPlayerIndex if any) play via AI -> AdvanceTurn(); for smoke test / spectator
}
```
`AdvanceTurn()` (private): TurnNumber++; for every player: units `ResetForNewTurn` (healing, goto continuation), cities `ProcessTurnStart` + `ExpandBordersIfNeeded`, research progress, `CultureSystem.AdvancePlayer`, `WorldLegacySystem.AdvancePlayer`（遺産検査・偉人ポイント・AI登用）, `MasterpieceSystem.AdvancePlayer`（作品ポイント・AI収蔵）, `AdministrationSystem.AdvancePlayer`（国庫収支・安定度・戦争疲弊）, visibility recompute, then elimination/制覇判定、`CultureSystem.AdvanceExchange` + `CheckCulturalVictory`、ターン上限スコア判定、`Bump()`。
Eliminations: a player with 0 cities and 0 settler units is eliminated (units removed, log). Victory: last non-eliminated player => domination win; TurnNumber > MaxTurns => score win (`score = Σpop*3 + cities*8 + techs*5`). Set `IsGameOver/GameOverMessageJa/Winner` (message states winner + reason, Japanese).

## 4. MapGenerator (MUST-MATCH)

```csharp
public static class MapGenerator {
    public static HexMap Generate(GameConfig config, System.Random rng, out List<HexCoord> startPositions);
}
```
- Continents/pangaea-style: layered value noise (implement your own — `Mathf.PerlinNoise` is fine seeded via random offsets) with edge falloff so map borders are ocean. Target ~40–50% land.
- Latitude bands: snow/tundra near top/bottom rows, desert band near equator with noise jitter, grassland/plains mix elsewhere. `Coast` = water tile adjacent to land. Mountains ~5% of land (clustered), hills ~15%, forest ~20% (not on desert/snow), resources on ~8% of suitable land tiles (Wheat: grass/plains; Cattle: grass; Deer: tundra/forest; Iron: hills/mountainside; Horses: plains/grass).
- `startPositions`: config.NumPlayers positions on passable land, pairwise `DistanceTo >= 10` (relax by 1 until satisfiable), each with >= 8 passable land tiles within radius 3, decent yields nearby. Guarantee land connectivity is NOT required, but prefer the largest landmass (flood-fill).

## 5. AI (`HexCiv.Core.AI`, MUST-MATCH)

```csharp
public class AIController : IAIController {
    public void PlayTurn(GameState state, Player player);
}
```
Per turn, in order: (1) pick research if none (prefer cheapest available, slight randomness via state.Rng). (2) each city with no production: choose via heuristic — monument if missing early, settler if Cities.Count < 4 && no settler alive && not at war, military if at war or few units, else best building/unit mix. (3) war decision: for each rival with shared explored border within ~6 tiles: if `MilitaryPower() > 1.5 * theirs` && TurnNumber > 40, small chance (5%) declare war. (4) units: settlers move to best city site (score = Σ yields radius 1, must satisfy CanFoundCityAt; prefer near capital, avoid enemy units), found city when on site; scouts explore nearest unexplored frontier; military: if at war — attack any `AttackableTiles` target first (prefer kills/cities), else march toward nearest enemy city; if at peace — escort settlers / fortify in own territory near borders. Keep it robust: every unit either acts or fortifies; avoid infinite loops (cap iterations per unit).
AI must play with full knowledge only of its Explored tiles (soft rule; don't over-engineer).

## 6. Rendering (`HexCiv.Render`, MUST-MATCH)

```csharp
public class MapRenderer : MonoBehaviour {
    public void Init(GameState state);       // build static terrain mesh(es) once
    public void RefreshDynamic();            // fog + territory borders, from state.HumanPlayer (if null: all visible)
    public void SetHighlights(HashSet<HexCoord> reachable, HashSet<HexCoord> attackable, List<HexCoord> path, HexCoord? selected);
    public void ClearHighlights();
    public bool TryGetTileUnderMouse(Camera cam, out HexCoord coord);  // math picking via XZ plane + HexCoord.FromWorld, InBounds check
}
public class EntityRenderer : MonoBehaviour {
    public void Init(GameState state);
    public void Refresh();                   // create/destroy/move unit & city views to match state; hide those not visible to HumanPlayer (explored cities stay ghost-visible)
}
```
- Hex size = 1.0, XZ plane, y≈0. Layer heights: terrain y=0, fog y=0.06, borders y=0.04, highlights y=0.05, units y=0.1, text above.
- Terrain: one combined mesh, 7 verts/hex (center+corners), vertex colors from `TerrainDef.Color` (slight per-tile brightness jitter). Hills: smaller inner hex overlay, darker. Forest: 2–3 small dark-green triangles. Mountain: gray raised cone/triangles. Resource: small colored diamond (`ResourceDef.Color`).
- Fog: per-hex overlay mesh, vertex colors updated in `RefreshDynamic`: unexplored `(0.02,0.03,0.06,1)`, explored-not-visible `(0,0,0,0.45)`, visible transparent.
- Borders: thin edge quads on owned-tile edges facing a different owner, colored `player.Color`.
- Units: disc (player color) + glyph TextMesh (white, `Def.Glyph`) + HP bar (only when damaged); fortified: darker ring. City: colored banner quad above tile + TextMesh `"名前 (pop)"` + HP bar when damaged; a small gray block on the tile. Material: `new Material(Shader.Find("Sprites/Default"))`.
- All view objects parented under this renderer's transform; `Refresh()` diffs by unit/city Id (no full rebuild every call — but correctness first).

## 7. UI (`HexCiv.UI`, uGUI built 100% in code, MUST-MATCH)

```csharp
public static class UIStyle {
    public static Font JapaneseFont();       // cached; CreateDynamicFontFromOSFont chain "Yu Gothic UI","Meiryo UI","Meiryo","MS Gothic"; last resort LegacyRuntime.ttf
    // helpers to create Panel/Button/Text are recommended
}
public class UIManager : MonoBehaviour {
    public void Init(GameState state, GameActions actions);   // builds Canvas(ScaleWithScreenSize 1280x720)+EventSystem, all panels
    public void RefreshAll();                // top bar, unit panel, city panel, log — called when state.Version changes
    public void SetSelectedUnit(Unit u);     // null hides unit panel
    public void ShowCityPanel(City city);    // production list uses actions.OnChooseProduction
    public void ShowTechPanel();             // available techs -> actions.OnChooseResearch
    public void CloseAllPanels();
    public bool IsPointerOverUI();
    public void ShowTileTooltip(Tile tile);  // small mouse-follow panel: terrain, yields, unit/city, owner
    public void HideTileTooltip();
    public void AddLog(string messageJa);    // subscribe to state.OnLog too; keep last ~6 lines top-left
    public void ShowGameOver(string messageJa);  // dark overlay, big text, "もう一度プレイ" -> actions.OnRestart
}
```
Layout: top bar (turn `ターン 12/250`, science `科学 +9`, clickable research status `研究中: 筆記 (あと3T)` / `研究を選択` → tech panel, civ name + color swatch on right). Bottom-right: `ターン終了` button (also Enter key handled by InputController). Bottom-left unit panel: name/glyph/HP/移動力/戦闘力 + buttons 都市建設 (settlers only, enabled per `CanFoundCityAt`), 防御態勢, 待機. Right side city panel: pop, growth turns, production progress, `AvailableProduction` buttons `名前 (3T)`, close ×. Tech panel center: buttons `名前 コスト (nT) — 解禁内容`. All Japanese.

## 8. Input & Camera (`HexCiv.Control`, MUST-MATCH)

```csharp
public class CameraController : MonoBehaviour {
    public void Init(Rect worldBoundsXZ, Vector3 startFocus);  // creates/uses Camera.main; angled top-down (pitch ~55°), WASD/arrows pan, wheel zoom (ortho or persp height 8..35), MMB-drag pan, clamped to bounds
}
public class InputController {                 // plain class; GameBootstrap calls Update() every frame
    public InputController(GameState s, MapRenderer map, EntityRenderer ents, UIManager ui, GameActions actions, CameraController camCtl);
    public Unit SelectedUnit { get; }
    public void Update();
    public void ClearSelection();
}
```
Bindings: LMB — select own unit on tile (else own city → ShowCityPanel, else deselect). RMB click (release with <6px drag, not over UI) with unit selected: if target attackable → `Combat.PerformAttack`; else `Pathfinder.FindPath` + `OrderMove` (multi-turn goto OK). Esc: close panels / deselect. Enter: `actions.OnEndTurn`. Hover (not over UI): tile tooltip for explored tiles. After selection/move: update `MapRenderer.SetHighlights(ReachableThisTurn, AttackableTiles, GotoPath, unit.Coord)`; clear on deselect. Selecting enemy units: not allowed. When game over: only camera + restart work.

## 9. GameBootstrap (root namespace `HexCiv`)

`[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]` spawns `GameObject("HexCivGame")` with `GameBootstrap` if not already present (works in ANY scene, including Untitled).
Awake/Start: build `GameConfig` (Seed = `Environment.TickCount & 0x7fffffff` if 0), `GameState` assembly: Rng, `MapGenerator.Generate`, create players (names/colors from GameConfig, `KnownTechs={GameRules.StartingTech}`, index 0 human), spawn per player: settler on start tile + warrior on a free neighbor, `Visibility.RecomputeAll`, `new TurnManager(state, new AIController())` + `BeginGame()`. Ensure camera (reuse `Camera.main` or create; background dark navy; also a directional light if none). Add MapRenderer/EntityRenderer/UIManager components (child GOs), `Init` each, wire `GameActions` handlers to sim calls (EndTurn → turnManager.EndTurn; FoundCity → checks + `s.FoundCity` + kill settler; Restart → destroy world root + rebuild fresh state in-place — scene reload will NOT re-run the bootstrap method). Update(): `inputController.Update()`; if `state.Version != lastSeen` → RefreshDynamic/Refresh/RefreshAll; on `IsGameOver` (once) → ShowGameOver. Camera starts focused on human settler.

## 10. Editor tooling (`Assets/Editor/`, no namespace, MUST-MATCH class+method names)

- `SmokeTest.Run` (static): headless sim — config `Seed=42, HumanPlayerIndex=-1, MapWidth=40, MapHeight=24, NumPlayers=4`; build state exactly like bootstrap but WITHOUT any GameObjects/renderers (duplicate the pure-sim assembly logic in a static helper is fine); run `RunHeadlessTurn()` × 150 inside try/catch; log stats each 25 turns (units, cities, techs, wars); on success `Debug.Log("SMOKE OK")` + `EditorApplication.Exit(0)`; on exception log `"SMOKE FAIL: " + ex` + `EditorApplication.Exit(1)`. Also fail if after 60 turns total cities == 0 (AI never founded a city — a real bug).
- `SceneSetup.EnsureScene` (static): programmatically create `Assets/Scenes/Main.unity` (new scene, DefaultGameObjects), save, add to `EditorBuildSettings.scenes`, `EditorApplication.Exit(0)`.
- `BuildScript.PerformBuild` (static): `BuildPipeline.BuildPlayer` scenes=`{"Assets/Scenes/Main.unity"}`, target `StandaloneWindows64`, output `Build/HexCiv.exe`; Exit(0/1) by `summary.result`.

## 11. Reference formulas (already in GameRules — use them, don't re-derive)

Damage `CombatDamage(atkEff, defEff, rng)`; effective strength `HealthScaledStrength`; growth `GrowthFoodNeeded(pop)`; food upkeep `Population * FoodPerPop`; starvation: if FoodStored<0 && surplus<0 → pop-1 (min 1), FoodStored=0. Science per city = pop + building Science bonuses; +BaseSciencePerCiv for the civ (capital). City defense strength & max hp per §3. Healing per §3.

## 12. Japanese string conventions

Log lines examples: `「ローマ」が都市「アンティウム」を建設した` / `「アテネ」が「バビロン」に宣戦布告した!` / `戦士が敵の弓兵を倒した` / `都市「ウル」が陥落した!` / `「エジプト」は滅亡した`. Keep them short, no honorifics, use 「」 for names.
