# Codex instructions for HexCiv

- This is the single canonical project. Do not implement new work in `C:\Users\kanta\CivilizationLike`.
- Read `ARCHITECTURE.md` and `COLLABORATION.md` before changing code.
- Preserve changes made by Claude Code or the user; inspect current files before editing.
- Keep `Assets/Scripts/Core/` free of MonoBehaviour and presentation dependencies.
- Use the shared Japanese font helpers for all Japanese UI and TextMesh content.
- Validate material changes with `SmokeTest.Run`; build with `BuildScript.PerformBuild` when player-facing code changes.
- Update the latest-status section in `COLLABORATION.md` when handing work off.
