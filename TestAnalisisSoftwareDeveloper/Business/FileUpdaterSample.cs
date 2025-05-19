
using log4net;

namespace Sample
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using Microsoft.Web.XmlTransform;


    public class FileUpdater
    {
        private static readonly ILog Log;
        #region Constants


        private const string DeleteCommandExtension = ".del";


        private const string AddCommandExtension = ".add";

      
        private const string UpdateCommandExtension = ".upd";

        private const string XdtMergeCommandExtension = ".xmrg";


        private const string ExecuteCommandExtension = ".exc";

        private const string ExecuteCommandExtensionInitial = ".eini";

        private const string ExecuteCommandExtensionEnd = ".eend";

        private const string ExecuteCommandParamsExtension = ".params";

        private const string CannotTransformXdtMessage = "No se puede realizar la transformación del archivo .";

        #endregion

    
        #region Methods

       
        private static void BackupFile(string backupDir, string targetFolder, string originalFileName, string command)
        {
            var backupFileName = originalFileName;
            var targetRelativeDirectory = string.Empty;

           
            if (File.Exists(originalFileName))
            {
                if (command == DeleteCommandExtension)
                {
                    command = UpdateCommandExtension;
                    targetRelativeDirectory = Path.GetDirectoryName(GetRelativePath(originalFileName, targetFolder));
                }
                
                if (command == ExecuteCommandExtensionInitial || command == ExecuteCommandExtension || command == ExecuteCommandExtensionEnd || command == ExecuteCommandParamsExtension)
                {
                    backupFileName = Path.Combine(Path.GetDirectoryName(originalFileName), Path.GetFileNameWithoutExtension(originalFileName));
                    targetRelativeDirectory = targetFolder;
                }
            }
            else if (command == DeleteCommandExtension)
            {
                if (!Directory.Exists(Path.GetDirectoryName(originalFileName)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(originalFileName));
                }

               
                File.WriteAllText(originalFileName, string.Empty);

                targetRelativeDirectory = Path.GetDirectoryName(GetRelativePath(originalFileName, targetFolder));
            }
            else if (command == UpdateCommandExtension)
            {

                command = DeleteCommandExtension;
                if (!Directory.Exists(Path.GetDirectoryName(originalFileName)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(originalFileName));
                }

           
                File.WriteAllText(originalFileName, string.Empty);

                targetRelativeDirectory = Path.GetDirectoryName(GetRelativePath(originalFileName, targetFolder));
            }
            else
            {
                return;
            }

        
            var folderToBackupFile = Path.Combine(backupDir, targetRelativeDirectory);

            if (!Directory.Exists(folderToBackupFile))
            {
                Directory.CreateDirectory(folderToBackupFile);
            }
            
            File.Copy(originalFileName, Path.Combine(folderToBackupFile, Path.GetFileName(backupFileName)) + command, true);
        }


        private static string GetRelativePath(string filespec, string folder)
        {
            Uri pathUri = new Uri(filespec);

         
            if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                folder += Path.DirectorySeparatorChar;
            }

            Uri folderUri = new Uri(folder);

            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }

    
        public string UpdateFiles(string sourceFolder, string targetFolder, string backupDir = null)
        {
            var createBackup = !string.IsNullOrEmpty(backupDir);

            if (createBackup)
            {
                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }
            }

            try
            {
                Directory.EnumerateFiles(sourceFolder, "*" + ExecuteCommandExtensionInitial, SearchOption.AllDirectories).ToList().ForEach(file =>
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var targetRelativeDirectory = Path.GetDirectoryName(GetRelativePath(file, sourceFolder));
                    this.ExecuteBats(file, fileName, targetRelativeDirectory, createBackup, Path.Combine(backupDir, targetRelativeDirectory), ExecuteCommandExtensionInitial);
                });
                
                foreach (var file in Directory.EnumerateFiles(sourceFolder, "*.*", SearchOption.AllDirectories).Where(s => Path.GetExtension(s) != ExecuteCommandExtensionInitial && Path.GetExtension(s) != ExecuteCommandExtensionEnd).ToList())
                {
                    var fileExtension = Path.GetExtension(file);
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var targetRelativeDirectory = Path.GetDirectoryName(GetRelativePath(file, sourceFolder));
                    var targetFileName = Path.Combine(targetFolder, targetRelativeDirectory ?? string.Empty, fileName ?? string.Empty);

                    switch (fileExtension.ToLower())
                    {
                        case AddCommandExtension:
                            if (createBackup)
                            {
                                BackupFile(Path.Combine(backupDir, targetRelativeDirectory ?? string.Empty), targetFolder, targetFileName, DeleteCommandExtension);
                            }

                            this.CopyFile(file, targetFileName);
                            break;
                        case XdtMergeCommandExtension:
                            if (createBackup && File.Exists(targetFileName))
                            {
                                BackupFile(Path.Combine(backupDir, targetRelativeDirectory ?? string.Empty), targetFolder, targetFileName, UpdateCommandExtension);
                            }

                            this.MergeXDT(file, targetFileName);
                            break;
                        case UpdateCommandExtension:
                            if (createBackup)
                            {
                                BackupFile(Path.Combine(backupDir, targetRelativeDirectory ?? string.Empty), targetFolder, targetFileName, UpdateCommandExtension);
                            }

                            this.CopyFile(file, targetFileName);
                            break;
                        case DeleteCommandExtension:
                            if (createBackup)
                            {
                                BackupFile(Path.Combine(backupDir, targetRelativeDirectory ?? string.Empty), targetFolder, targetFileName, AddCommandExtension);
                            }

                            this.RemoveFile(targetFileName);
                            break;
                        case ExecuteCommandExtension:
                            this.ExecuteBats(file, fileName, targetRelativeDirectory, createBackup, Path.Combine(backupDir ?? string.Empty, targetRelativeDirectory ?? string.Empty), ExecuteCommandExtension);
                            break;
                    }
                }

                Directory.EnumerateFiles(sourceFolder, "*" + ExecuteCommandExtensionEnd, SearchOption.AllDirectories).ToList().ForEach(file =>
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var targetRelativeDirectory = Path.GetDirectoryName(GetRelativePath(file, sourceFolder));
                    this.ExecuteBats(file, fileName, targetRelativeDirectory, createBackup, Path.Combine(backupDir ?? string.Empty, targetRelativeDirectory ?? string.Empty), ExecuteCommandExtensionEnd);
                });
            }
            catch (Exception ex)
            {
                if (createBackup)
                {
                    var errorMessage = ex.ToString();

                  
                    var rollbackError = this.UpdateFiles(backupDir, targetFolder);
                    
                    return errorMessage + (string.IsNullOrEmpty(rollbackError) ? string.Empty : Environment.NewLine + "Rollback Error =>" + Environment.NewLine + rollbackError);
                }

                return ex.ToString();
            }

            return string.Empty;
        }
        
        private void ExecuteBats(string file, string fileName, string targetRelativeDirectory, bool createBackup, string backupDir, string extension)
        {
            string rutaArchivoBat = Path.Combine(file, fileName + extension);

            try
            {
                if (!File.Exists(rutaArchivoBat))
                {
                    Console.WriteLine($"El archivo .bat no se encontró en la ruta: {rutaArchivoBat}");
                    return;
                }

                Console.WriteLine($"Ejecutando archivo .bat: {rutaArchivoBat}");

                Process proceso = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = rutaArchivoBat;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;
                startInfo.CreateNoWindow = true;

                proceso.StartInfo = startInfo;
                proceso.Start();

                // Leer la salida (opcional)
                string salida = proceso.StandardOutput.ReadToEnd();
                if (!string.IsNullOrEmpty(salida))
                {
                    Console.WriteLine("Salida del archivo .bat:");
                    Console.WriteLine(salida);
                }

                // Leer los errores (opcional)
                string errores = proceso.StandardError.ReadToEnd();
                if (!string.IsNullOrEmpty(errores))
                {
                    Console.WriteLine("Errores del archivo .bat:");
                    Console.WriteLine(errores);
                }

                // Esperar a que el proceso termine
                proceso.WaitForExit();

                Console.WriteLine($"El archivo .bat '{fileName + extension}' ha terminado con código de salida: {proceso.ExitCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ocurrió un error al ejecutar el archivo .bat '{fileName + extension}': {ex.Message}");
            }
        }


        private void CopyFile(string sourceFile, string targetFile)
        {
           File.Copy(sourceFile, targetFile, true);
        }

        private void RemoveFile(string targetFile)
        {
            File.Delete(targetFile);
        }


        private void MergeXDT(string sourceFile, string targetFile)
        {
            if (File.Exists(targetFile))
            {
                using (var target = new XmlTransformableDocument())
                {
                    target.PreserveWhitespace = true;
                    target.Load(targetFile);

                    using (var xdt = new XmlTransformation(sourceFile))
                    {
                        if (xdt.Apply(target))
                        {
                            target.Save(targetFile);
                        }
                        else
                        {
                            throw new XmlTransformationException(string.Format(CannotTransformXdtMessage, sourceFile));
                        }
                    }
                }
            }
        }

        #endregion
    }
}