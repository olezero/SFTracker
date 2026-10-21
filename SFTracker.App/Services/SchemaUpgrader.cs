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
	/// <summary>Adds a column when the table does not have it yet. The definition is the SQLite column DDL.</summary>
	private sealed record AddColumn(string Table, string Column, string Definition);

	// Append new steps at the end; never edit or remove an existing one. Only "add column" has been
	// needed so far; a new table or a column rebuild would be a new step kind.
	private static readonly AddColumn[] s_steps = [
		new("Recipes", "Machine", "TEXT NOT NULL DEFAULT ''"),
	];

	/// <returns>The number of steps that were applied.</returns>
	public static async Task<int> UpgradeAsync(SFTrackerDbContext context, ILogger? logger = null) {
		var applied = 0;
		foreach (var step in s_steps) {
			if (await ColumnExistsAsync(context, step.Table, step.Column)) {
				continue;
			}
			await context.Database.ExecuteSqlAsync($"ALTER TABLE \"{step.Table}\" ADD COLUMN \"{step.Column}\" {step.Definition}");
			logger?.LogInformation("Schema upgrade: added column {Table}.{Column}.", step.Table, step.Column);
			applied++;
		}
		return applied;
	}

	private static async Task<bool> ColumnExistsAsync(SFTrackerDbContext context, string table, string column) {
		var count = await context.Database
			.SqlQuery<int>($"SELECT COUNT(*) AS \"Value\" FROM pragma_table_info({table}) WHERE name = {column}")
			.SingleAsync();
		return count > 0;
	}
}
