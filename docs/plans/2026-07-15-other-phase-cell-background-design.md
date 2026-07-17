# Other Phase Cell Background

## Goal

Visually distinguish the classified `나머지` phase with an RGB `247 / 249 / 250` (`#F7F9FA`) cell background.

## Behavior

- Apply `#F7F9FA` only to the outer `Other` phase cell.
- Keep `Before`, `Processing`, and `After` cells white.
- Keep photo cards white so their boundaries remain clear.
- Keep all interaction, persistence, and export behavior unchanged.

## Validation

Implement the conditional background in `PhaseBox`, then run formatting checks and build the application. No automated test changes are needed for this visual-only adjustment.
