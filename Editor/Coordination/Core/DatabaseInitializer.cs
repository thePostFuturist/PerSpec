using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using SQLite;

namespace PerSpec.Editor.Coordination
{
    /// <summary>
    /// Automatically initializes the SQLite database when PerSpec is enabled.
    /// Creates all required tables using CREATE TABLE IF NOT EXISTS for idempotence.
    /// </summary>
    [InitializeOnLoad]
    public static class DatabaseInitializer
    {
        private const string ENABLED_PREF_KEY = "PerSpec_Enabled";
        private static bool _hasInitializedThisSession;

        static DatabaseInitializer()
        {
            // Use delayCall to ensure proper initialization order
            EditorApplication.delayCall += InitializeIfNeeded;
        }

        private static void InitializeIfNeeded()
        {
            // Avoid multiple initializations in same session
            if (_hasInitializedThisSession) return;

            // Only run if PerSpec should be running (enabled AND initialized)
            // Inline the ShouldRun check to avoid circular assembly dependency
            string projectPath = Directory.GetParent(Application.dataPath).FullName;
            string perspecPath = Path.Combine(projectPath, "PerSpec");

            bool isEnabled = EditorPrefs.GetBool(ENABLED_PREF_KEY, true);
            bool isInitialized = Directory.Exists(perspecPath);

            if (!isEnabled || !isInitialized) return;

            // Check if database needs initialization
            string dbPath = Path.Combine(perspecPath, "test_coordination.db");

            if (!File.Exists(dbPath))
            {
                EnsureDatabaseExists();
            }

            _hasInitializedThisSession = true;
        }

        /// <summary>
        /// Ensures the database file and all tables exist.
        /// Safe to call multiple times - uses CREATE TABLE IF NOT EXISTS.
        /// </summary>
        /// <returns>True if database is ready, false on error</returns>
        public static bool EnsureDatabaseExists()
        {
            try
            {
                string projectPath = Directory.GetParent(Application.dataPath).FullName;
                string perspecPath = Path.Combine(projectPath, "PerSpec");
                string dbPath = Path.Combine(perspecPath, "test_coordination.db");

                // Check if PerSpec folder exists
                if (!Directory.Exists(perspecPath))
                {
                    Debug.LogWarning("[DatabaseInitializer] PerSpec folder does not exist. Cannot initialize database.");
                    return false;
                }

                // Create database and tables
                using (var connection = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex))
                {
                    connection.BusyTimeout = TimeSpan.FromSeconds(5);

                    // Enable WAL mode for better concurrency
                    connection.Execute("PRAGMA journal_mode=WAL");

                    // Create all tables
                    CreateTables(connection);

                    // Create all indexes
                    CreateIndexes(connection);

                    // Initialize system status if empty
                    InitializeSystemStatus(connection);
                }

                Debug.Log("[DatabaseInitializer] Database initialized successfully");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[DatabaseInitializer] Error initializing database: {e.Message}");
                return false;
            }
        }

        private static void CreateTables(SQLiteConnection connection)
        {
            // Table 1: test_requests
            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS test_requests (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    request_type TEXT NOT NULL,
                    test_filter TEXT,
                    test_platform TEXT NOT NULL,
                    status TEXT NOT NULL DEFAULT 'pending',
                    priority INTEGER DEFAULT 0,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    started_at TIMESTAMP,
                    completed_at TIMESTAMP,
                    result_summary TEXT,
                    error_message TEXT,
                    total_tests INTEGER DEFAULT 0,
                    passed_tests INTEGER DEFAULT 0,
                    failed_tests INTEGER DEFAULT 0,
                    skipped_tests INTEGER DEFAULT 0,
                    duration_seconds REAL DEFAULT 0.0
                )
            ");

            // Table 2: test_results
            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS test_results (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    request_id INTEGER NOT NULL,
                    test_name TEXT NOT NULL,
                    test_class TEXT,
                    test_method TEXT,
                    result TEXT NOT NULL,
                    duration_ms REAL DEFAULT 0.0,
                    error_message TEXT,
                    stack_trace TEXT,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (request_id) REFERENCES test_requests(id) ON DELETE CASCADE
                )
            ");

            // Table 3: system_status
            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS system_status (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    component TEXT NOT NULL,
                    status TEXT NOT NULL,
                    last_heartbeat TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    message TEXT,
                    metadata TEXT
                )
            ");

            // Table 4: execution_log
            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS execution_log (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    request_id INTEGER,
                    log_level TEXT NOT NULL,
                    message TEXT NOT NULL,
                    source TEXT,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (request_id) REFERENCES test_requests(id) ON DELETE CASCADE
                )
            ");

            // Table 5: asset_refresh_requests
            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS asset_refresh_requests (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    refresh_type TEXT NOT NULL DEFAULT 'full',
                    paths TEXT,
                    import_options TEXT DEFAULT 'default',
                    status TEXT NOT NULL DEFAULT 'pending',
                    priority INTEGER DEFAULT 0,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    started_at TIMESTAMP,
                    completed_at TIMESTAMP,
                    duration_seconds REAL DEFAULT 0.0,
                    result_message TEXT,
                    error_message TEXT
                )
            ");

            // Table 6: console_logs
            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS console_logs (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    session_id TEXT NOT NULL,
                    log_level TEXT NOT NULL,
                    message TEXT NOT NULL,
                    stack_trace TEXT,
                    truncated_stack TEXT,
                    source_file TEXT,
                    source_line INTEGER,
                    timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    frame_count INTEGER,
                    is_truncated BOOLEAN DEFAULT 0,
                    context TEXT,
                    request_id INTEGER,
                    FOREIGN KEY (request_id) REFERENCES test_requests(id)
                )
            ");

            // Table 7: menu_item_requests
            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS menu_item_requests (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    menu_path TEXT NOT NULL,
                    status TEXT NOT NULL DEFAULT 'pending',
                    priority INTEGER DEFAULT 0,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    started_at TIMESTAMP,
                    completed_at TIMESTAMP,
                    duration_seconds REAL DEFAULT 0.0,
                    result TEXT,
                    error_message TEXT
                )
            ");

            // Table 8: scene_hierarchy_requests
            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS scene_hierarchy_requests (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    request_type TEXT NOT NULL DEFAULT 'full',
                    target_path TEXT,
                    include_inactive INTEGER DEFAULT 1,
                    include_components INTEGER DEFAULT 1,
                    status TEXT NOT NULL DEFAULT 'pending',
                    priority INTEGER DEFAULT 0,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    started_at TIMESTAMP,
                    completed_at TIMESTAMP,
                    output_file TEXT,
                    error_message TEXT
                )
            ");
        }

        private static void CreateIndexes(SQLiteConnection connection)
        {
            // Test request indexes
            connection.Execute("CREATE INDEX IF NOT EXISTS idx_requests_status ON test_requests(status)");
            connection.Execute("CREATE INDEX IF NOT EXISTS idx_requests_created ON test_requests(created_at DESC)");

            // Test result indexes
            connection.Execute("CREATE INDEX IF NOT EXISTS idx_results_request ON test_results(request_id)");

            // Execution log indexes
            connection.Execute("CREATE INDEX IF NOT EXISTS idx_log_request ON execution_log(request_id)");

            // System status indexes
            connection.Execute("CREATE INDEX IF NOT EXISTS idx_status_component ON system_status(component)");

            // Asset refresh indexes
            connection.Execute("CREATE INDEX IF NOT EXISTS idx_refresh_status ON asset_refresh_requests(status)");
            connection.Execute("CREATE INDEX IF NOT EXISTS idx_refresh_created ON asset_refresh_requests(created_at DESC)");

            // Console log indexes
            connection.Execute("CREATE INDEX IF NOT EXISTS idx_console_logs_session ON console_logs(session_id, timestamp DESC)");
            connection.Execute("CREATE INDEX IF NOT EXISTS idx_console_logs_level ON console_logs(log_level, timestamp DESC)");
            connection.Execute("CREATE INDEX IF NOT EXISTS idx_console_logs_request ON console_logs(request_id, timestamp DESC)");

            // Menu item request indexes
            connection.Execute("CREATE INDEX IF NOT EXISTS idx_menu_status ON menu_item_requests(status)");
            connection.Execute("CREATE INDEX IF NOT EXISTS idx_menu_created ON menu_item_requests(created_at DESC)");

            // Scene hierarchy indexes
            connection.Execute("CREATE INDEX IF NOT EXISTS idx_hierarchy_status ON scene_hierarchy_requests(status)");
        }

        private static void InitializeSystemStatus(SQLiteConnection connection)
        {
            // Check if Database component already exists
            var count = connection.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM system_status WHERE component = 'Database'"
            );

            if (count == 0)
            {
                connection.Execute(@"
                    INSERT INTO system_status (component, status, message)
                    VALUES ('Database', 'online', 'Database initialized successfully')
                ");
            }
        }
    }
}
