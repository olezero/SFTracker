# SFTracker

A local Blazor Server app for planning Satisfactory factories: compose factories from game recipes
with production multipliers, mark manual inputs/outputs, and see the resource balance per factory,
per network or across a whole game.

```bash
dotnet run --project SFTracker.App     # http://localhost:5062
dotnet test                            # NUnit tests
```

Data lives in a SQLite file, `Database/sftracker.db` by default. The gear button in the top bar lets
you point the app at a different file (for example one kept in its own git repo). The database files
are gitignored, so each person running the app has their own.

## Schema changes

The app does **not** use EF Core migrations. On startup `DatabaseInitializer` does:

1. `EnsureCreated` — a **new** database file gets the full current schema from the EF model.
   An **existing** file is left exactly as it is.
2. `SchemaUpgrader.UpgradeAsync` — an ordered list of idempotent steps, each applied only when the
   live schema is missing it. This is what brings an existing file up to date after a `git pull`.
3. Seeding of parts/recipes from `game_data.json` (adds what is missing, by name) and a default game.

Because of step 1, **every change to the EF model that touches the database must get a matching step
in `SFTracker.App/Services/SchemaUpgrader.cs`**, or anyone with an existing database will crash on
their next start. Nobody should have to run `sqlite3` or `dotnet ef` by hand.

When you add a column:

1. Add the property to the entity in `SFTracker.App/Models`.
2. Append an `AddColumn` step to `s_steps` with the table, column and SQLite column DDL. Give it a
   `DEFAULT` so existing rows get a value, e.g.
   `new("Recipes", "Machine", "TEXT NOT NULL DEFAULT ''")`.
3. If existing rows need real data rather than the default, backfill them in code on startup
   (see `DataImportService.BackfillMachinesAsync` for the pattern).
4. Run `dotnet test`; `SchemaUpgraderTests` checks the steps are idempotent.

Rules:

- Append steps at the end. Never edit or remove a step once it has been pushed: someone's database
  may already have been upgraded by it.
- Match the DDL to what `EnsureCreated` would generate for a fresh file, so old and new files end up
  with the same schema.
- A new table is a `CreateTable` step: the `CREATE TABLE` and `CREATE INDEX` statements EF would
  generate for it (see the `FactoryWorldParts` step for the shape). `SchemaUpgraderTests` drops the
  table from a fresh database, runs the upgrader and compares the resulting schema with the original,
  so a mismatch fails the tests.
- Anything SQLite's `ALTER TABLE` cannot do (changing or dropping a column) needs a new step kind in
  `SchemaUpgrader`. Keep it idempotent in the same way: check the live schema with
  `pragma_table_info` / `sqlite_master`, then apply.

History: columns added before the upgrader existed (`FactoryRecipes.Sorting`, the `Games` table,
`GameId` on `Factories`/`Networks`, `Games.FluidDivisor`) were applied by hand and are not covered by
steps; a database created before them needs to be deleted and recreated. Everything from
`Recipes.Machine` onward is handled by the upgrader.
