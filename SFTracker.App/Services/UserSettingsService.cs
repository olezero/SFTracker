using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;

namespace SFTracker.Services;

/// <summary>Per-machine settings stored in usersettings.json (gitignored) next to appsettings.json.</summary>
public class UserSettings {
	/// <summary>
	/// Custom SQLite database file. Absolute, or relative to the project folder (same base as the
	/// appsettings.json connection string). Null/empty = use the appsettings.json default.
	/// </summary>
	public string? DatabasePath { get; set; }
}

/// <summary>
/// Loads/saves <see cref="UserSettings"/> and resolves the effective database path: the user override
/// when set, otherwise the "DefaultConnection" connection string from appsettings.json.
/// </summary>
public class UserSettingsService {
	public const string FileName = "usersettings.json";

	private static readonly JsonSerializerOptions s_jsonOptions = new() {
		WriteIndented = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		PropertyNameCaseInsensitive = true
	};

	private readonly string m_contentRoot;
	private readonly string m_defaultDatabasePath;

	public UserSettingsService(IConfiguration configuration, IHostEnvironment environment) {
		m_contentRoot = environment.ContentRootPath;

		var connectionString = configuration.GetConnectionString("DefaultConnection")
			?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is missing from appsettings.json");
		m_defaultDatabasePath = new SqliteConnectionStringBuilder(connectionString).DataSource;

		Settings = Load();
	}

	public UserSettings Settings { get; private set; }

	public string FilePath => Path.Combine(m_contentRoot, FileName);

	/// <summary>The appsettings.json database path, as written there (may be relative).</summary>
	public string DefaultDatabasePath => m_defaultDatabasePath;

	public bool HasCustomDatabasePath => !string.IsNullOrWhiteSpace(Settings.DatabasePath);

	/// <summary>Absolute path of the database file currently in effect.</summary>
	public string EffectiveDatabasePath => ResolvePath(HasCustomDatabasePath ? Settings.DatabasePath! : m_defaultDatabasePath);

	public string ConnectionString => new SqliteConnectionStringBuilder { DataSource = EffectiveDatabasePath }.ToString();

	/// <summary>Expands "~" and makes relative paths relative to the project folder.</summary>
	public string ResolvePath(string path) {
		path = path.Trim();
		if (path == "~" || path.StartsWith("~/") || path.StartsWith("~\\"))
			path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), path.Length > 1 ? path[2..] : "");
		return Path.GetFullPath(path, m_contentRoot);
	}

	public async Task SaveAsync(UserSettings settings) {
		if (string.IsNullOrWhiteSpace(settings.DatabasePath))
			settings.DatabasePath = null;

		await File.WriteAllTextAsync(FilePath, JsonSerializer.Serialize(settings, s_jsonOptions));
		Settings = settings;
	}

	private UserSettings Load() {
		if (!File.Exists(FilePath))
			return new UserSettings();
		try {
			return JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(FilePath), s_jsonOptions) ?? new UserSettings();
		} catch (JsonException) {
			// A broken file shouldn't stop the app from starting; fall back to the defaults.
			return new UserSettings();
		}
	}
}
