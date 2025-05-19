


namespace Sample
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using log4net;
    using log4net.Config;
    using Microsoft.Win32.TaskScheduler;
    using TestAnalisisSoftwareDeveloper;
    using TestAnalisisSoftwareDeveloper.Business;
    

    public static class MonitorUpdaterManagerSample
    {
        #region Constantes


        private const string MonitorServiceNameKey = "monitorsk";
        public const string UpdaterMonitorInstallationFolder = "monSelfUpdater";
        const string MonitorUpdatesPath="/tmp";
        public const string UpdaterMonitorFolder = "actualizaciones";
        public static string InstalledRollbackFilesPath = "/tmp";
        public static string taskName = "ActualizacionMonitor_Emergencia";
        public static string processName = "psample";


        private static readonly ILog Log = LogManager.GetLogger(typeof(Program));

        #endregion



        #region Metodos

        public static void UpdateMonitor(string monitorFilesLocation, string installationFolder, string version)
        {
            XmlConfigurator.Configure(LogManager.GetRepository(Assembly.GetEntryAssembly()));
            monitorFilesLocation = Path.Combine(monitorFilesLocation, UpdaterMonitorFolder);
            installationFolder = Path.Combine(installationFolder, UpdaterMonitorInstallationFolder);
            try
            {

                var winServiceManager = new WindowsServiceManager();

                Log.Info($"Iniciando las actualizaciones de la version {version.ToString()} al monitor...");
                try
                {
                    CreateUpdateMonitorTask(taskName);
                    StopMonitorProcesses(processName);
                    winServiceManager.StopService(MonitorServiceNameKey);
                    
                }
                catch (Exception ex)
                {
                    Log.Error("Ocurrió un error al intentar terminar el proceso del monitor de actualizaciones.", ex);
                }

           
                var backupPath = System.IO.Path.Combine(MonitorUpdatesPath, "Backup", Guid.NewGuid().ToString().Replace("-", "").Substring(0, 6));
                if (!System.IO.Directory.Exists(backupPath))
                {
                    try
                    {
                        System.IO.Directory.CreateDirectory(backupPath);
                    }
                    catch
                    {
                        backupPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), UpdaterMonitorFolder, "Backup", Guid.NewGuid().ToString().Replace("-", "").Substring(0, 6));
                        if (!System.IO.Directory.Exists(backupPath))
                        {
                            System.IO.Directory.CreateDirectory(backupPath);
                        }
                    }
                }

                var fileManager = new FileManagerSample();
                
                var result = fileManager.UpdateFiles(monitorFilesLocation.Trim(new char[] { '"' }), installationFolder.Trim(new char[] { '"' }), backupPath);
                bool updateError = false;

                if (!string.IsNullOrEmpty(result))
                {
                    updateError = true;
                    Log.Error(result);

                    result = null;
                }

          
                if (updateError)
                {
                    Log.Info("Realizando rollback de las actualizaciones al monitor...");
                    
                    result = fileManager.UpdateFiles(backupPath, installationFolder);
                    fileManager.RemoveDirectoryContents(backupPath);

                    if (!string.IsNullOrEmpty(result))
                    {
                        Exception exception = new Exception(result);
                        Log.Info( "MonitorUpdater", exception);
                    }
                    else
                    {
                        Log.Info("Terminado rollback de las actualizaciones al monitor...");
                    }

              

                    return;
                }

                fileManager.RemoveDirectoryContents(backupPath);
                fileManager.RemoveDirectoryContents(monitorFilesLocation.Trim(new char[] { '"' }));
                System.IO.Directory.Delete(backupPath, true);
                System.IO.Directory.Delete(monitorFilesLocation.Trim(new char[] { '"' }), true);
                
                ReleaseUpdateMonitorTask(taskName);
                StartMonitorProcesses(processName);
                winServiceManager.StartService(MonitorServiceNameKey);


                Log.Info("Actualizaciones al monitor terminadas...");
            }
            catch (Exception ex)
            {
                Log.Error("Ocurrió un error durante el proceso de actualización del Monitor.", ex);
            }
        }

        private static void CreateUpdateMonitorTask(string taskName)
        {
            string updaterPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Updater.exe");

            try
            {
                using (var ts = new TaskService())
                {
                    var task = ts.NewTask();
                    task.RegistrationInfo.Description = "Tarea de recuperación para completar la actualización del monitor";

                    task.Triggers.Add(new BootTrigger { Delay = TimeSpan.FromMinutes(1) });

                    task.Actions.Add(new ExecAction(updaterPath, "--resume-update", null));

                    task.Settings.AllowDemandStart = true;
                    task.Settings.Enabled = true;
                    task.Settings.StartWhenAvailable = true;
                    task.Settings.DisallowStartIfOnBatteries = false;

                    ts.RootFolder.RegisterTaskDefinition(taskName, task);
                    Log.Info($"Tarea programada '{taskName}' creada como respaldo.");
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"No se pudo crear la tarea programada: {ex.Message}");
            }
        }

        private static void ReleaseUpdateMonitorTask(string UpdateTaskName)
        {
            // elimina la tarea

            try
            {
                using (var ts = new Microsoft.Win32.TaskScheduler.TaskService())
                {
                    ts.RootFolder.DeleteTask(UpdateTaskName, false);
                    Log.Info($"Tarea programada '{UpdateTaskName}' eliminada exitosamente.");
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"No se pudo eliminar la tarea '{UpdateTaskName}'. Puede que no exista.", ex);
            }

        }

        private static void StopMonitorProcesses(string processName)
        {
            try
            {
                Process[] processes = Process.GetProcessesByName(processName);

                if (processes.Any())
                {
                    Log.Info("Cerrando el monitor de actualizaciones...");
                    foreach (var proc in processes)
                    {
                        proc.Kill();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error al intentar detener los procesos del monitor.", ex);
                throw;
            }
        }

        private static bool StartMonitorProcesses(string processName)
        {
            const string executablePath = @"C:\Users\enriq\OneDrive\Escritorio\Pruebas\psample.exe"; 
            const int startupTimeoutSeconds = 15;

            try
            {
                // Verificar si ya está en ejecución
                var existingProcesses = Process.GetProcessesByName(processName);
                if (existingProcesses.Any())
                {
                    Log.Warn($"El proceso '{processName}' ya está en ejecución. IDs: {string.Join(", ", existingProcesses.Select(p => p.Id))}");
                    return true; 
                }

                Log.Info($"Iniciando proceso '{processName}' desde: {executablePath}");

                var startInfo = new ProcessStartInfo
                {
                    FileName = executablePath,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Normal,
                    WorkingDirectory = Path.GetDirectoryName(executablePath)
                };

                var process = Process.Start(startInfo);

                if (process == null)
                {
                    Log.Error("No se pudo crear el proceso (Process.Start devolvió null)");
                    return false;
                }

                // Esperar inicialización (sin bloquear si falla)
                if (!process.WaitForInputIdle(startupTimeoutSeconds * 1000))
                {
                    Log.Warn($"El proceso inició pero no completó inicialización en {startupTimeoutSeconds}s. Continuando...");
                }

                Log.Info($"Proceso '{processName}' iniciado. ID: {process.Id}");
                return true;
            }
            catch (FileNotFoundException ex)
            {
                Log.Error($"No se encontró el ejecutable en {executablePath}. Verifica la ruta de instalación.", ex);
            }
            catch (Win32Exception ex)
            {
                Log.Error($"Error de permisos al iniciar el proceso. ¿Ejecutando como administrador?", ex);
            }
            catch (Exception ex)
            {
                Log.Error($"Error inesperado al iniciar '{processName}'. Detalles: {ex.Message}", ex);
            }

            return false; 
        }

        #endregion
    }
}
