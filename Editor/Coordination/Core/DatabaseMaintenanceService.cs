using System;
using System.Threading;
using UnityEngine;
using UnityEditor;

namespace PerSpec.Editor.Coordination
{
    /// <summary>
    /// Background service for automated database maintenance to prevent bloat
    /// </summary>
    [InitializeOnLoad]
    public static class DatabaseMaintenanceService
    {
        private static Timer _maintenanceTimer;
        private static SQLiteManager _dbManager;
        private static readonly object _lockObject = new object();
        private static bool _isRunning = false;
        private static DateTime _lastMaintenance = DateTime.MinValue;
        
        // Configuration
        private static readonly int MaintenanceIntervalMinutes = 60; // Run every hour
        private static readonly int DataRetentionHours = 2; // Keep only 2 hours of data
        private static readonly long MaxDatabaseSizeBytes = 100 * 1024 * 1024; // 100MB threshold
        private static readonly long CriticalSizeBytes = 500 * 1024 * 1024; // 500MB critical threshold
        
        static DatabaseMaintenanceService()
        {
            // Check if PerSpec is initialized
            if (!SQLiteManager.IsPerSpecInitialized())
            {
                return;
            }
            
            try
            {
                _dbManager = new SQLiteManager();
                
                // Only proceed if database is ready
                if (!_dbManager.IsInitialized)
                {
                    return;
                }
                
                // Start the maintenance timer
                StartMaintenanceTimer();
                
                // Register for domain reload
                AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
                
                // Perform initial maintenance if database is large
                CheckAndPerformMaintenance();
                
                Debug.Log("[DatabaseMaintenanceService] Started with hourly maintenance schedule");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DatabaseMaintenanceService] Failed to initialize: {ex.Message}");
            }
        }
        
        private static void StartMaintenanceTimer()
        {
            lock (_lockObject)
            {
                StopMaintenanceTimer();
                
                // Create timer that runs maintenance periodically
                _maintenanceTimer = new Timer(
                    callback: _ => PerformScheduledMaintenance(),
                    state: null,
                    dueTime: TimeSpan.FromMinutes(MaintenanceIntervalMinutes),
                    period: TimeSpan.FromMinutes(MaintenanceIntervalMinutes)
                );
            }
        }
        
        private static void StopMaintenanceTimer()
        {
            lock (_lockObject)
            {
                _maintenanceTimer?.Dispose();
                _maintenanceTimer = null;
            }
        }
        
        private static void OnBeforeAssemblyReload()
        {
            StopMaintenanceTimer();
        }
        
        private static void PerformScheduledMaintenance()
        {
            lock (_lockObject)
            {
                if (_isRunning) return;
                _isRunning = true;
            }
            
            try
            {
                CheckAndPerformMaintenance();
            }
            finally
            {
                lock (_lockObject)
                {
                    _isRunning = false;
                }
            }
        }
        
        private static void CheckAndPerformMaintenance()
        {
            try
            {
                if (_dbManager == null || !_dbManager.IsInitialized)
                    return;
                
                long dbSize = _dbManager.GetDatabaseSize();
                
                // Critical size - aggressive cleanup
                if (dbSize > CriticalSizeBytes)
                {
                    Debug.LogWarning($"[DatabaseMaintenanceService] CRITICAL: Database size ({dbSize / (1024f * 1024f):F2} MB) exceeds critical threshold!");
                    
                    // Console logs now managed by file-based EditModeLogCapture (3 session limit)
                    _dbManager.DeleteOldTestResults(0); // Delete everything except current
                    _dbManager.DeleteOldExecutionLogs(0);
                    _dbManager.DeleteOldRefreshRequests(0);
                    
                    // Vacuum to reclaim space
                    _dbManager.VacuumDatabase();
                    
                    long newSize = _dbManager.GetDatabaseSize();
                    Debug.Log($"[DatabaseMaintenanceService] Critical cleanup complete. Size reduced from {dbSize / (1024f * 1024f):F2} MB to {newSize / (1024f * 1024f):F2} MB");
                }
                // Regular threshold - normal cleanup
                else if (dbSize > MaxDatabaseSizeBytes)
                {
                    Debug.Log($"[DatabaseMaintenanceService] Database size ({dbSize / (1024f * 1024f):F2} MB) exceeds threshold. Running maintenance...");
                    
                    _dbManager.PerformFullMaintenance(DataRetentionHours);
                    
                    long newSize = _dbManager.GetDatabaseSize();
                    Debug.Log($"[DatabaseMaintenanceService] Maintenance complete. Size: {newSize / (1024f * 1024f):F2} MB");
                }
                // Periodic cleanup even if under threshold
                else if (DateTime.Now - _lastMaintenance > TimeSpan.FromHours(2))
                {
                    // Console logs now managed by file-based EditModeLogCapture (3 session limit)
                    _dbManager.DeleteOldTestResults(DataRetentionHours);
                    _dbManager.DeleteOldExecutionLogs(DataRetentionHours);
                    _dbManager.DeleteOldRefreshRequests(DataRetentionHours);
                    
                    Debug.Log($"[DatabaseMaintenanceService] Periodic cleanup complete. Database size: {dbSize / (1024f * 1024f):F2} MB");
                }
                
                _lastMaintenance = DateTime.Now;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DatabaseMaintenanceService] Maintenance failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Force immediate maintenance
        /// </summary>
        public static void ForceMaintenance()
        {
            Debug.Log("[DatabaseMaintenanceService] Forcing immediate maintenance...");
            CheckAndPerformMaintenance();
        }
        
        /// <summary>
        /// Get current database statistics
        /// </summary>
        public static string GetMaintenanceStatus()
        {
            try
            {
                if (_dbManager == null || !_dbManager.IsInitialized)
                    return "Database not initialized";
                
                long dbSize = _dbManager.GetDatabaseSize();
                float sizeMB = dbSize / (1024f * 1024f);
                
                string status = $"Database Size: {sizeMB:F2} MB\n";
                status += $"Last Maintenance: {(_lastMaintenance == DateTime.MinValue ? "Never" : _lastMaintenance.ToString("yyyy-MM-dd HH:mm:ss"))}\n";
                status += $"Status: ";
                
                if (dbSize > CriticalSizeBytes)
                    status += "CRITICAL - Immediate cleanup needed";
                else if (dbSize > MaxDatabaseSizeBytes)
                    status += "Warning - Above threshold";
                else
                    status += "Normal";
                
                return status;
            }
            catch (Exception ex)
            {
                return $"Error getting status: {ex.Message}";
            }
        }
    }
}