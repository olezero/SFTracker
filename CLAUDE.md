# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A single-project ASP.NET Core / Blazor Server app (`SFTracker.App`, .NET 9) for planning **Satisfactory** factories: you compose factories out of game recipes with production multipliers, mark manual inputs/outputs, and see a running global resource balance (surplus/deficit per part) across all factories.

Note: the project folder is `SFTracker.App` but the assembly and root namespace are `SFTracker` (namespaces are `SFTracker.Models`, `SFTracker.Services`, etc. — never `SFTracker.App.*`).

## Commands

```bash
dotnet run --project SFTracker.App        # runs on http://localhost:5062 (Development)
dotnet build                              # build the solution
dotnet watch --project SFTracker.App      # hot reload during dev
```

EF Core migrations (run from `SFTracker.App/`):

```bash
dotnet ef migrations add <Name>
dotnet ef database update
```

There is no test project.

## Architecture

**Startup (`Program.cs`)** runs a fixed initialization sequence on boot inside a service scope:
1. `GameInfoService.InitializeAsync()` — loads `game_data.json` into memory.
2. `context.Database.EnsureCreatedAsync()` — note this uses EnsureCreated, **not** `Database.Migrate()`, even though Migrations exist. A fresh DB is created from the current model; migrations are not auto-applied at runtime.
3. `DataImportService.ImportParts/RecipesAsync()` — idempotent seed (skips if tables non-empty).

**Two-layer data model — keep these distinct:**
- `Services/Data/*` (`GameData`, `Recipe`, `Part`, `Machine`, …) — plain DTOs deserialized from `game_data.json`. Read-only game reference data held in the singleton `GameInfoService`.
- `Models/Db*` and the factory entities — EF Core entities persisted to SQLite. `DataImportService` copies the JSON DTOs into `DbPart`/`DbRecipe`/`DbRecipeInput`/`DbRecipeOutput` on first run.

**Persistence:** SQLite at `Database/sftracker.db` (connection string in `appsettings.json`, path is relative to the working dir so it resolves to the repo-root `Database/` folder). The `Database/*.db*` files are gitignored.

**Recipe/factory schema (the core relationships):**
- `Factory` has many `FactoryInput` / `FactoryOutput` (manual, user-entered amounts) and many `FactoryRecipe`.
- `FactoryRecipe` is the pivot between a `Factory` and a `DbRecipe`, carrying `Multiplier` (number of machine-equivalents, e.g. 10 machines at 250% = 25), `IsSlooped` (doubles output), and `IsActive`.
- `FactoryRecipeInputOverride` / `FactoryRecipeOutputOverride` implement per-recipe toggling of whether a specific input/output counts toward the balance. **Absence of an override = enabled** (see `IsRecipeInputEnabled`/`IsRecipeOutputEnabled` defaulting to `true`). Toggling creates a `IsEnabled=false` override, then flips it thereafter.

**Rate calculation** lives on the entities, not a service: `DbRecipeInput/Output.GetQuantityPerMinute(...)` = `Quantity * Multiplier / Recipe.TimeSeconds * 60`, with output ×2 when slooped. Any per-minute number in the UI comes from these methods.

**`FactoryService`** is the scoped data-access seam — all reads use eager `.Include(...).AsSplitQuery()` chains and filter recipes to `IsActive`. When adding a query that touches recipes/overrides, mirror the existing Include chain in `GetAllFactoriesAsync`/`GetFactoryByIdAsync` or navigation properties will be null in the Razor components.

**Component tree (Blazor Server, interactive):** `Home` → `FactoryManager` (state owner: holds `factories`, `allParts`, `allRecipes`, `selectedFactory`) + `ResourceBalance` (sidebar). `FactoryManager` → `FactoryList` + `FactoryDetail` → `RecipeSection`, `AddForms`, `RecipeEditorModal`, etc.
- Child components signal changes up via `EventCallback` (e.g. `OnRecipeChanged`); the handler is `FactoryManager.LoadDataAndRefreshBalance`, which re-fetches everything and debounce-refreshes the balance.
- `Home.razor` wires `FactoryManager` to `ResourceBalance` imperatively after first render (`SetResourceBalanceComponent`) with a 100ms delay — this cross-component reference is intentional; don't "clean it up" without understanding the balance-refresh path.
- Third-party UI: Blazor.Bootstrap (`AddBlazorBootstrap`). Searchable dropdowns use Blazor.Bootstrap's native `AutoComplete` via two reusable wrappers, `PartSelector` and `RecipeSelector` (each keeps a `selectedText` string for `@bind-Value`, exposes a `@bind-Selected*` API, and filters the in-memory list in its `DataProvider`; `RecipeSelector` also matches on output part name). `AutoComplete` is a typeahead — the menu only opens once you type a character (this is by design and was chosen over a custom combobox to keep arrow-key/Enter navigation). Prefer these wrappers over raw `<select>` for part/recipe pickers.

## Conventions

- Indentation is **tabs** (`.editorconfig`), K&R braces (`{` on the same line — `csharp_new_line_before_open_brace = none`), including in Razor `@code` blocks.
- Private instance fields in services use the `m_` prefix (`m_context`, `m_gameData`); component-local fields in `.razor` use plain camelCase.
- FICSMAS (holiday event) parts/recipes are filtered out of queries by name — preserve the `!Name.Contains("FICSMAS")` filters when adding recipe/part queries.
- `DataImportService` skips recipes whose `BatchTime` contains `/` (fractional times) — a known gap noted in code.

## Reference files

`Prompt`, `Prompt2`, and `TODO` (repo root) are freeform design/spec notes for planned features — useful for intent, not authoritative about current behavior.
