using Microsoft.EntityFrameworkCore;
using SFTracker.Data;

namespace SFTracker.Services;

/// <summary>
/// Brings an existing database up to the current EF model. Startup creates fresh databases with
/// EnsureCreated (full current schema) but never alters existing ones, so every schema change is
/// listed here as an idempotent step that is applied only when the live schema lacks it.
/// See README.md ("Schema changes") for the procedure.
/// </summary>
public static class SchemaUpgrader {
	private abstract record Step;

	/// <summary>Adds a column when the table does not have it yet. The definition is the SQLite column DDL.</summary>
	private sealed record AddColumn(string Table, string Column, string Definition) : Step;

	/// <summary>
	/// Creates a table (and its indexes) when it does not exist yet. The statements must match what
	/// EnsureCreated generates for a fresh file; SchemaUpgraderTests compares the two.
	/// </summary>
	private sealed record CreateTable(string Table, string[] Statements) : Step;

	// Append new steps at the end; never edit or remove an existing one.
	private static readonly Step[] s_steps = [
		new AddColumn("Recipes", "Machine", "TEXT NOT NULL DEFAULT ''"),
		new CreateTable("FactoryWorldParts", [
			"CREATE TABLE \"FactoryWorldParts\" ("
				+ "\"Id\" INTEGER NOT NULL CONSTRAINT \"PK_FactoryWorldParts\" PRIMARY KEY AUTOINCREMENT, "
				+ "\"FactoryId\" INTEGER NOT NULL, "
				+ "\"PartId\" INTEGER NOT NULL, "
				+ "CONSTRAINT \"FK_FactoryWorldParts_Factories_FactoryId\" FOREIGN KEY (\"FactoryId\") REFERENCES \"Factories\" (\"Id\") ON DELETE CASCADE, "
				+ "CONSTRAINT \"FK_FactoryWorldParts_Parts_PartId\" FOREIGN KEY (\"PartId\") REFERENCES \"Parts\" (\"Id\") ON DELETE CASCADE)",
			"CREATE UNIQUE INDEX \"IX_FactoryWorldParts_FactoryId_PartId\" ON \"FactoryWorldParts\" (\"FactoryId\", \"PartId\")",
			"CREATE INDEX \"IX_FactoryWorldParts_PartId\" ON \"FactoryWorldParts\" (\"PartId\")",
		]),
		// Empty rather than '{}': ExecuteSqlRaw treats braces as format placeholders. GameSettings.Parse
		// reads an empty string as defaults, so existing rows behave exactly like fresh ones.
		new AddColumn("Games", "Settings", "TEXT NOT NULL DEFAULT ''"),
	];

	/// <returns>The number of steps that were applied.</returns>
	public static async Task<int> UpgradeAsync(SFTrackerDbContext context, ILogger? logger = null) {
		var applied = 0;
		foreach (var step in s_steps) {
			// Plain strings on purpose: DDL cannot take parameters, and `dotnet format` rewrites an
			// interpolated ExecuteSqlRaw call into the parameterised ExecuteSql (EF1002), which breaks it.
			switch (step) {
				case AddColumn column:
					if (await ColumnExistsAsync(context, column.Table, column.Column)) {
						continue;
					}
					var sql = "ALTER TABLE \"" + column.Table + "\" ADD COLUMN \"" + column.Column + "\" " + column.Definition;
					await context.Database.ExecuteSqlRawAsync(sql);
					logger?.LogInformation("Schema upgrade: added column {Table}.{Column}.", column.Table, column.Column);
					break;
				case CreateTable table:
					if (await TableExistsAsync(context, table.Table)) {
						continue;
					}
					foreach (var statement in table.Statements) {
						await context.Database.ExecuteSqlRawAsync(statement);
					}
					logger?.LogInformation("Schema upgrade: created table {Table}.", table.Table);
					break;
			}
			applied++;
		}
		return applied;
	}

	private static async Task<bool> TableExistsAsync(SFTrackerDbContext context, string table) {
		var count = await context.Database
			.SqlQuery<int>($"SELECT COUNT(*) AS \"Value\" FROM sqlite_master WHERE type = 'table' AND name = {table}")
			.SingleAsync();
		return count > 0;
	}

	private static async Task<bool> ColumnExistsAsync(SFTrackerDbContext context, string table, string column) {
		var count = await context.Database
			.SqlQuery<int>($"SELECT COUNT(*) AS \"Value\" FROM pragma_table_info({table}) WHERE name = {column}")
			.SingleAsync();
		return count > 0;
	}
}
