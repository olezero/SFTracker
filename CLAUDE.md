# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A single-project ASP.NET Core / Blazor Server app (`SFTracker.App`, .NET 9) for planning **Satisfactory** factories: you compose factories out of game recipes with production multipliers, mark manual inputs/outputs, and see a running resource balance (surplus/deficit per part) for one factory or across all of them.

It is a purely local personal hobby tool (never deployed) — keep changes proportionate and don't over-engineer.

Note: the project folder is `SFTracker.App` but the assembly and root namespace are `SFTracker` (namespaces are `SFTracker.Models`, `SFTracker.Services`, etc. — never `SFTracker.App.*`).

## Commands

```bash
dotnet run --project SFTracker.App        # runs on http://localhost:5062 (Development)
dotnet build                              # build the solution
dotnet watch --project SFTracker.App      # hot reload during dev
```

There is no test project. Verify changes with `dotnet build`; the user does the in-app testing.

## Architecture

**Startup (`Program.cs`)** runs a fixed initialization sequence on boot:
1. `GameInfoService.InitializeAsync()` — loads `game_data.json` into memory.
2. `DatabaseInitializer.InitializeAsync(app.Services)` (own scope), which does: `context.Database.EnsureCreatedAsync()` — a fresh DB is created from the current EF model (**migrations are not applied at runtime**); `DataImportService.ImportParts/RecipesAsync()` — idempotent seed (skips if tables non-empty); `GameService.EnsureDefaultGameAsync()` — creates "Game 1" if the `Games` table is empty. The same method is re-run when the user changes the database path in the settings modal.

**User settings (`UserSettingsService`, singleton):** per-machine overrides live in `SFTracker.App/usersettings.json` (gitignored, next to `appsettings.json`). Currently only `DatabasePath` (absolute, `~`-prefixed, or relative to the project folder); empty/missing = the `DefaultConnection` path from `appsettings.json`. The DbContext's connection string is read from this service per scope, so a change applies to new circuits; `SettingsModal` (gear button in the `GameSelector` bar) saves the file, runs `DatabaseInitializer` on the new file, and force-reloads the page. The point is to let the DB live outside the repo (e.g. in its own git repo).

**Schema changes and the live DB (important):** because startup uses `EnsureCreated`, it never alters an existing database. The `Migrations/` folder is stale (it stops at `AddIsSloopedToFactoryRecipe`; `FactoryRecipe.Sorting`, the `Games` table (including `Games.FluidDivisor`) and `GameId` on `Factories`/`Networks` were all applied to the live DB by hand with `ALTER TABLE`, so those `GameId` columns have no real FK constraint — `GameService.DeleteGameAsync` deletes factories explicitly for that reason). When you add a column/table, either apply it manually with `sqlite3 Database/sftracker.db "ALTER TABLE ..."` (preserves the user's factories) or tell the user to delete `Database/sftracker.db*` so it is recreated. Don't rely on `dotnet ef database update`.

**Two-layer data model — keep these distinct:**
- `Services/Data/*` (`GameData`, `Recipe`, `Part`, `Machine`, …) — plain DTOs deserialized from `game_data.json`. Read-only game reference data held in the singleton `GameInfoService`.
- `Models/Db*` and the factory entities — EF Core entities persisted to SQLite. `DataImportService` copies the JSON DTOs into `DbPart`/`DbRecipe`/`DbRecipeInput`/`DbRecipeOutput` on first run.

**Persistence:** SQLite at `Database/sftracker.db` by default (connection string `../Database/sftracker.db` in `appsettings.json`, resolved against the project folder, so it lands in the repo-root `Database/` folder), unless `usersettings.json` overrides it (see above). The `Database/*.db*` files are gitignored.

**Recipe/factory schema (the core relationships):**
- `Game` is the top-level container (one per Satisfactory save). Every `Factory` and `Network` has a required `GameId`; factory names are unique per game, not globally. `Game.LastOpenedAt` is bumped on selection and decides which game opens on startup.
- `Factory` has many `FactoryInput` / `FactoryOutput` (manual, user-entered amounts) and many `FactoryRecipe`. `Factory.Sorting` (double) is the manual list order.
- `FactoryRecipe` is the pivot between a `Factory` and a `DbRecipe`, carrying `Multiplier` (number of machine-equivalents, e.g. 10 machines at 250% = 25), `IsSlooped` (doubles output), `IsActive`, and `Sorting` (manual order within the factory; ties fall back to `Id`).
- `FactoryRecipeInputOverride` / `FactoryRecipeOutputOverride` implement per-recipe toggling of whether a specific input/output counts toward the balance. **Absence of an override = enabled** (see `IsRecipeInputEnabled`/`IsRecipeOutputEnabled` defaulting to `true`). Toggling creates a `IsEnabled=false` override, then flips it thereafter.
- `Network` groups factories within a game (`Factory.NetworkId` nullable FK, at most one network per factory). `NetworkService` is the CRUD seam; a factory's network is set in the `FactoryList` edit modal (which can also create a new network inline) via `FactoryService.SetFactoryNetworkAsync`, and `ResourceBalance` offers each network as a scope (`network:<id>` selector value) that balances its member factories as one unit. Networks are shown as a small label under the factory name in `FactoryList`. There is no rename/delete UI for networks yet (the service supports both).

**Rate calculation** lives on the entities, not a service: `DbRecipeInput/Output.GetQuantityPerMinute(...)` = `Quantity * Multiplier / Recipe.TimeSeconds * 60`, with output ×2 when slooped. Any per-minute number in the UI comes from these methods.

**Balance calculation** lives in the `ResourceBalance` component (`LoadPartBalances`), not in `FactoryService`: it sums manual inputs/outputs plus enabled recipe inputs/outputs for either the selected factory or all factories. `ResourceBalance` takes a `GameId` parameter and refreshes itself in `OnParametersSetAsync` whenever it changes.

**`GameTransferService`** (scoped) exports a game to JSON (`format: "sftracker-game"`, `version: 1`; parts/recipes referenced by **name**, overrides stored as `disabledInputs`/`disabledOutputs` part names, only `IsActive` recipes) and imports such JSON as a **new** game named `<name> (imported)` (counter suffix if taken) — it never modifies existing games. Import resolves every name up front and throws `GameImportException` listing unknowns before writing anything. UI: Export/Import buttons in the `GameSelector` bar open `GameTransferModals` (textarea + copy-to-clipboard via `sftracker.copyText`); a successful import fires `OnGameOperation` with `"imported"`.

**`GameService`** (scoped) is the CRUD seam for games. **`FactoryService`** is the scoped data-access seam — `GetAllFactoriesAsync(gameId)` and `CreateFactoryAsync(name, gameId)` are always scoped to a game; — all reads use eager `.Include(...).AsSplitQuery()` chains and filter recipes to `IsActive`. When adding a query that touches recipes/overrides, mirror the existing Include chain in `GetAllFactoriesAsync`/`GetFactoryByIdAsync` or navigation properties will be null in the Razor components. `AddFactoryRecipeAsync` appends at the end of the manual order; `ReorderFactoryRecipesAsync` rewrites `Sorting` from a list of ids.

**Component tree (Blazor Server, interactive):** `Home` → `FactoryManager` (state owner: holds `games`, `selectedGame`, `factories`, `allParts`, `allRecipes`, `selectedFactory`). A full-width `GameSelector` bar sits on top (game dropdown + new/rename/delete; switching games clears the selected factory and reloads everything). Below it the layout is a **narrow left column** (`FactoryList` on top, `ResourceBalance` below) and a **wide right column** (`FactoryDetail` → `RecipeSection`, `CustomInputOutput`, `AddForms`).
- `RecipeSection` — the per-factory recipe list: clicking the multiplier badge or any input/output rate opens `RecipeEditorModal` with that field highlighted and focused (`HighlightMultiplier` / `HighlightInputId` / `HighlightOutputId`), eye buttons toggle overrides, two-click delete with a 3s confirmation timer, and a pencil button opens the same modal without a highlight.
- `CustomInputOutput` — inline add/edit/remove of the factory's manual inputs/outputs.
- `AddForms` — quick-add forms for recipe/input/output plus the `RecipeEditorModal` in add mode.
- `RecipeEditorModal` — shared add/edit modal; editing any input/output rate back-calculates the multiplier (and vice versa), with a sloop checkbox.
- Child components signal changes up via `EventCallback` (e.g. `OnRecipeChanged`); the handler is `FactoryManager.LoadDataAndRefreshBalance`, which re-fetches everything and refreshes the balance.
- `FactoryManager` holds an `@ref` to its child `ResourceBalance` (field `resourceBalance`) and calls `RefreshBalanceDebounced()` on it via `TriggerBalanceRefresh` (the "debounce" is currently immediate). `ResourceBalance` otherwise loads its own data (including its per-factory / "All Factories" selector).
- **Drag-and-drop ordering** uses Blazor.Bootstrap's `SortableList` (sortablejs is loaded from CDN in `App.razor`). `FactoryList` reorders factories and `FactoryManager.OnFactoryListChanged` persists `Sorting`; `RecipeSection` reorders recipes, updates `Sorting` in memory first (optimistic — otherwise the row snaps back) and then calls `ReorderFactoryRecipesAsync`.
- Third-party UI: Blazor.Bootstrap 3.5 (`AddBlazorBootstrap`) with the Bootswatch **Darkly** theme and Bootstrap Icons from CDN (`App.razor`). Searchable dropdowns use Blazor.Bootstrap's native `AutoComplete` via two reusable wrappers, `PartSelector` and `RecipeSelector` (each keeps a `selectedText` string for `@bind-Value`, exposes a `@bind-Selected*` API, and filters the in-memory list in its `DataProvider`; `RecipeSelector` also matches on output part name). `AutoComplete` is a typeahead — the menu only opens once you type a character (this is by design and was chosen over a custom combobox to keep arrow-key/Enter navigation). Prefer these wrappers over raw `<select>` for part/recipe pickers.
- `Constants/DisplayConstants.cs` holds the balance display format (`F2`) and the ±0.01 "balanced" threshold used by `ResourceBalance`; recipe rows use a local `FormatNumber` (4 decimals, trailing zeros trimmed) instead.

## Conventions

- Indentation is **tabs** (`.editorconfig`), K&R braces (`{` on the same line — `csharp_new_line_before_open_brace = none`), including in Razor `@code` blocks.
- Private instance fields in services use the `m_` prefix (`m_context`, `m_gameData`); component-local fields in `.razor` use plain camelCase.
- FICSMAS (holiday event) parts/recipes are filtered out of queries by name — preserve the `!Name.Contains("FICSMAS")` filters when adding recipe/part queries.
- `DataImportService` skips recipes whose `BatchTime` contains `/` (fractional times) — a known gap noted in code.
- Commits are mostly `wip`; don't commit unless asked.

## Reference files

`TODO` (repo root) is a freeform wishlist — most items in it are already implemented (input/output toggles, recipe modal, balance factory selector); remaining ideas are a per-recipe machine-tier dropdown and an "auto calculate multiplier" button.
