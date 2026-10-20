# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A single-project ASP.NET Core / Blazor Server app (`SFTracker.App`, .NET 10) for planning **Satisfactory** factories: you compose factories out of game recipes with production multipliers, mark manual inputs/outputs, and see a running resource balance (surplus/deficit per part) for one factory or across all of them.

It is a purely local personal hobby tool (never deployed) — keep changes proportionate and don't over-engineer.

Note: the project folder is `SFTracker.App` but the assembly and root namespace are `SFTracker` (namespaces are `SFTracker.Models`, `SFTracker.Services`, etc. — never `SFTracker.App.*`).

## Commands

```bash
dotnet run --project SFTracker.App        # runs on http://localhost:5062 (Development)
dotnet build                              # build the solution
dotnet watch --project SFTracker.App      # hot reload during dev
dotnet test                               # NUnit tests in SFTracker.Tests
```

`SFTracker.Tests` (NUnit) covers the non-UI logic: rate maths, override semantics, `BalanceCalculator`, the game-data import, `FactoryService`, `GameService` and the export/import round trip. Database tests run against in-memory SQLite (`TestDatabase`), seeded from the real `game_data.json` (linked into the test project). Run `dotnet test` after changes to services/models; the user does the in-app testing of the Blazor UI.

## Architecture

**Startup (`Program.cs`)** runs a fixed initialization sequence on boot:
1. `GameInfoService.InitializeAsync(path)` — loads `game_data.json` (from the content root) into memory; a missing/broken file fails startup.
2. `DatabaseInitializer.InitializeAsync(app.Services)` (own scope), which does: `context.Database.EnsureCreatedAsync()` — a fresh DB is created from the current EF model (**migrations are not applied at runtime**); `DataImportService.ImportParts/RecipesAsync()` — idempotent seed that adds any part/recipe (by name) not already in the DB, so additions to `game_data.json` land on the next start; `GameService.EnsureDefaultGameAsync()` — creates "Game 1" if the `Games` table is empty. The same method is re-run when the user changes the database path in the settings modal.

**User settings (`UserSettingsService`, singleton):** per-machine overrides live in `SFTracker.App/usersettings.json` (gitignored, next to `appsettings.json`): `DatabasePath` (absolute, `~`-prefixed, or relative to the project folder; empty/missing = the `DefaultConnection` path from `appsettings.json`) and `Layout` (dashboard card positions, see below). The DbContext's connection string is read from this service per scope, so a change applies to new circuits; `SettingsModal` (gear button in the `GameSelector` bar) saves the file, runs `DatabaseInitializer` on the new file, and force-reloads the page. The point is to let the DB live outside the repo (e.g. in its own git repo). `SaveLayoutAsync` writes only the layout (no DB re-init); `SettingsModal` must carry `Layout` over when it saves a new database path.

**Dashboard layout (Gridstack.js):** the three cards (`FactoryList`, `ResourceBalance`, `FactoryDetail`) are items in a `DashboardGrid` (`Components/DashboardGrid.razor`, JS glue in `App.razor` under `sftracker.dashboard`), draggable by their `.card-header` and resizable from the bottom-right corner. `Constants/DashboardLayout.cs` holds the card ids, the 12-column/40px grid constants and the defaults (sized for 1080p; smaller screens are not a target). `FactoryManager` renders the `.grid-stack-item` wrappers **unconditionally** with `gs-x/y/w/h` from a snapshot of the saved layout taken at init; Gridstack owns the wrappers from then on and every `change` event is saved to `usersettings.json` via `OnLayoutChanged`. Blazor owns everything inside `.grid-stack-item-content`. CSS in `app.css` makes each card fill its cell and scroll its own `card-body`, so a card's header/footer must be direct children of the `.card` (the balance footer sits outside the body for that reason). The `.card-header` is the drag handle and Gridstack binds to the element it finds at init, so each card's header element must be rendered unconditionally and never re-created (`FactoryDetail` renders one card and varies only its contents for this reason). "Reset layout" in `SettingsModal` clears the saved layout and reloads.

**Schema changes and the live DB (important):** because startup uses `EnsureCreated`, it never alters an existing database. EF migrations are not used (a `Migrations/` folder, if still present, is stale and unused). `FactoryRecipe.Sorting`, the `Games` table (including `Games.FluidDivisor`) and `GameId` on `Factories`/`Networks` were applied to the live DB by hand with `ALTER TABLE`, so those `GameId` columns have no real FK constraint — `GameService.DeleteGameAsync` deletes factories explicitly for that reason. When you add a column/table, either apply it manually with `sqlite3 Database/sftracker.db "ALTER TABLE ..."` (preserves the user's factories) or tell the user to delete `Database/sftracker.db*` so it is recreated. Don't rely on `dotnet ef database update`.

**Two-layer data model — keep these distinct:**
- `Services/Data/*` (`GameData`, `Recipe`, `Part`, `Machine`, …) — plain DTOs deserialized from `game_data.json`. Read-only game reference data held in the singleton `GameInfoService`. Only parts/recipes are used so far; the machine DTOs are kept for a future machine-tier feature.
- `Models/Db*` and the factory entities — EF Core entities persisted to SQLite. `DataImportService` copies the JSON DTOs into `DbPart`/`DbRecipe`/`DbRecipeInput`/`DbRecipeOutput`. Amounts and batch times in the JSON can be fractions ("12/5"); `DataImportService.ParseNumber` handles both.

**Persistence:** SQLite at `Database/sftracker.db` by default (connection string `../Database/sftracker.db` in `appsettings.json`, resolved against the project folder, so it lands in the repo-root `Database/` folder), unless `usersettings.json` overrides it (see above). The `Database/*.db*` files are gitignored.

**Recipe/factory schema (the core relationships):**
- `Game` is the top-level container (one per Satisfactory save). Every `Factory` and `Network` has a required `GameId`; factory names are unique per game, not globally. `Game.LastOpenedAt` is bumped on selection and decides which game opens on startup.
- `Factory` has many `FactoryInput` / `FactoryOutput` (manual, user-entered amounts) and many `FactoryRecipe`. `Factory.Sorting` (double) is the manual list order.
- `FactoryRecipe` is the pivot between a `Factory` and a `DbRecipe`, carrying `Multiplier` (number of machine-equivalents, e.g. 10 machines at 250% = 25), `IsSlooped` (doubles output), `IsActive`, and `Sorting` (manual order within the factory; ties fall back to `Id`).
- `FactoryRecipeInputOverride` / `FactoryRecipeOutputOverride` implement per-recipe toggling of whether a specific input/output counts toward the balance. **Absence of an override = enabled** (see `FactoryRecipe.IsInputEnabled`/`IsOutputEnabled` defaulting to `true`). Toggling (`FactoryService.ToggleRecipeInput/OutputAsync`) creates a `IsEnabled=false` override, then flips it thereafter. Switching a row to another recipe drops its overrides.
- `Network` groups factories within a game (`Factory.NetworkId` nullable FK, at most one network per factory). `NetworkService` is the CRUD seam; a factory's network is set in the `FactoryList` edit modal (which can also create a new network inline) via `FactoryService.UpdateFactoryAsync`, and `ResourceBalance` offers each network as a scope (`network:<id>` selector value) that balances its member factories as one unit. Networks are shown as a small label under the factory name in `FactoryList`. There is no rename/delete UI for networks yet (the service supports both).

**Rate calculation** lives on the entities, not a service: `DbRecipeInput/Output.GetQuantityPerMinute(...)` = `Quantity * Multiplier / Recipe.TimeSeconds * 60`, with output ×2 when slooped. Any per-minute number in the UI comes from these methods.

**Balance calculation** is the pure, static `BalanceCalculator.Calculate(factories, fluidDivisor)` (`Services/BalanceCalculator.cs`, returns `PartBalance` per part): manual inputs and enabled recipe inputs count as consumption, manual outputs and enabled recipe outputs as production. `ResourceBalance` loads the game's factories once per refresh and filters them in memory to the selected scope (all / one network / one factory) before calling it. `ResourceBalance` takes `GameId` and `FluidDivisor` parameters and refreshes/recalculates itself in `OnParametersSetAsync` whenever they change.

**`GameTransferService`** (scoped) exports a game to JSON (`format: "sftracker-game"`, `version: 1`; parts/recipes referenced by **name**, overrides stored as `disabledInputs`/`disabledOutputs` part names, only `IsActive` recipes) and imports such JSON as a **new** game named `<name> (imported)` (counter suffix if taken) — it never modifies existing games. Import resolves every name up front and throws `GameImportException` listing unknowns before writing anything. UI: Export/Import buttons in the `GameSelector` bar open `GameTransferModals` (textarea + copy-to-clipboard via `sftracker.copyText`); a successful import fires `OnGameOperation` with `"imported"`.

**`GameService`** (scoped) is the CRUD seam for games. **`FactoryService`** is the scoped data-access seam — `GetAllFactoriesAsync(gameId)` and `CreateFactoryAsync(name, gameId)` are always scoped to a game. Both factory reads go through the private `FactoriesWithDetails()` query (eager `.Include(...).AsSplitQuery()` chain, recipes filtered to `IsActive`); extend that one query when a new navigation is needed, or it will be null in the Razor components. `CreateFactoryAsync`/`AddFactoryRecipeAsync` append at the end of the manual order; `ReorderFactoriesAsync`/`ReorderFactoryRecipesAsync` rewrite `Sorting` from a list of ids. `UpdateFactoryAsync(id, name, networkId)` is the single edit entry point used by the `FactoryList` modal.

**Component tree (Blazor Server, interactive):** `Home` → `FactoryManager` (state owner: holds `games`, `selectedGame`, `factories`, `networks`, `selectedFactory`, plus `allParts`/`allRecipes`, which are game reference data loaded once per circuit and never re-fetched). A full-width `GameSelector` bar sits on top (game dropdown + new/rename/delete; switching games clears the selected factory and reloads everything). Below it the layout is a **narrow left column** (`FactoryList` on top, `ResourceBalance` below) and a **wide right column** (`FactoryDetail` → `RecipeSection`, `CustomInputOutput`, `AddForms`).
- `RecipeSection` — the per-factory recipe list: clicking the multiplier badge or any input/output rate opens `RecipeEditorModal` with that field highlighted and focused, eye buttons toggle overrides, two-click delete with a 3s confirmation timer, a bullseye "match demand" button, per-ingredient "+" dropdowns that add a producing/consuming recipe sized to the current gap, and a pencil button opens the same modal without a highlight.
- `CustomInputOutput` — inline add/edit/remove of the factory's manual inputs/outputs.
- `AddForms` — quick-add forms for recipe (always 1x; "Advance" opens the modal) / input / output.
- `RecipeEditorModal` — shared add/edit modal. It is opened by method call, not parameters: `ShowAddAsync(preselectedRecipe?)` or `ShowEditAsync(factoryRecipe, highlightInputId?, highlightOutputId?, highlightMultiplier)`, and reports back through `OnSave` with `(recipeId, multiplier, isSlooped, factoryRecipeId?)`. Editing any input/output rate back-calculates the multiplier (and vice versa; the sloop state is part of that calculation), with a sloop checkbox.
- Child components signal changes up via `EventCallback` (`RecipeSection.OnRecipeChanged`, `CustomInputOutput.OnChanged`, `AddForms.OnAdded`, collapsed into `FactoryDetail.OnChanged`); the handler is `FactoryManager.LoadDataAndRefreshBalance`, which re-fetches the game's factories/networks and refreshes the balance.
- `FactoryManager` holds an `@ref` to its child `ResourceBalance` (field `resourceBalance`) and calls `RefreshBalance()` on it via `TriggerBalanceRefresh`. `ResourceBalance` otherwise loads its own data (including its all / network / factory scope selector).
- **Drag-and-drop ordering** uses Blazor.Bootstrap's `SortableList` (sortablejs is loaded from CDN in `App.razor`). `FactoryList` reorders factories and `FactoryManager.OnFactoryListChanged` persists `Sorting`; `RecipeSection` reorders recipes, updates `Sorting` in memory first (optimistic — otherwise the row snaps back) and then calls `ReorderFactoryRecipesAsync`.
- Third-party UI: Blazor.Bootstrap 4.0 (`AddBlazorBootstrap`; the chart components live in a separate package since 4.0 and are not used) with the Bootswatch **Darkly** theme, Bootstrap Icons, sortablejs and Gridstack 10 from CDN (`App.razor`). Searchable dropdowns use Blazor.Bootstrap's native `AutoComplete` via two reusable wrappers, `PartSelector` and `RecipeSelector` (each keeps a `selectedText` string for `@bind-Value`, exposes a `@bind-Selected*` API, and filters the in-memory list in its `DataProvider`; `RecipeSelector` also matches on output part name). `AutoComplete` is a typeahead — the menu only opens once you type a character (this is by design and was chosen over a custom combobox to keep arrow-key/Enter navigation). Prefer these wrappers over raw `<select>` for part/recipe pickers.
- `Constants/DisplayConstants.cs` holds the balance display format (`F2`), the ±0.01 "balanced" threshold used by `ResourceBalance`, and `FormatNumber` (4 decimals, trailing zeros trimmed, invariant culture) used by recipe rows, the hover card and the editor modal via `@using static SFTracker.Constants.DisplayConstants`.

## Conventions

- Indentation is **tabs** (`.editorconfig`), K&R braces (`{` on the same line — `csharp_new_line_before_open_brace = none`), including in Razor `@code` blocks.
- Private instance fields in services use the `m_` prefix (`m_context`, `m_gameData`); component-local fields in `.razor` use plain camelCase. **Local functions are camelCase** (`balanceFor`, `tryParse`), not PascalCase: the `.editorconfig` naming rules treat them as "everything else". `dotnet format -v d` reports violations as IDE1006 warnings; run it after adding code.
- FICSMAS (holiday event) parts/recipes are filtered out of queries by name — preserve the `!Name.Contains("FICSMAS")` filters when adding recipe/part queries.
- `DataImportService` skips recipes whose `BatchTime` contains `/` (fractional times) — a known gap noted in code.
- Commits are mostly `wip`; don't commit unless asked.

## Ideas not yet implemented

A per-recipe machine-tier dropdown (the machine data is already in `game_data.json`/`GameInfoService`) and a rename/delete UI for networks (`NetworkService` already supports both).
