# Claude Code instructions for HexCiv

This directory is the single canonical Unity project shared with Codex.

Before editing:

1. Read `ARCHITECTURE.md` for module contracts.
2. Read `COLLABORATION.md` for the latest implementation status and handoff notes.
3. Inspect the current contents of files before modifying them; preserve Codex and user changes.

Do not add new implementation to `C:\Users\kanta\CivilizationLike`; it is a legacy backup.
Use Unity 6.3 LTS (`6000.3.20f1`). After meaningful changes, run `SmokeTest.Run`, and run
`BuildScript.PerformBuild` for player-facing changes. Record completed work and remaining issues in
`COLLABORATION.md`.
