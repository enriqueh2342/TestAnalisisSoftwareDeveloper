using log4net;

namespace Sample
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    

    public class FileManagerSample
    {
        private static readonly ILog Log;
        #region Constants
        private const string UpdatePackageFileNameFormat = "{0}.zip";

        #endregion


        #region Methods




        

        
        public string UpdateFiles(string sourceFolder, string targetFolder, string backupDir = null)
        {
            var fileUpdater = new FileUpdater();
            
            return fileUpdater.UpdateFiles(sourceFolder, targetFolder, backupDir);
        }

      
        public string RemoveDirectoryContents(string directory, string directoryToFilter = "", bool remove = false)
        {
            var directoryInfo = new DirectoryInfo(directory);

            if (!directoryInfo.Exists)
            {
                return string.Empty;
            }

            foreach (var file in directoryInfo.GetFiles("*.*", SearchOption.AllDirectories))
            {
                try
                {
                    if (string.IsNullOrEmpty(directoryToFilter) || !file.FullName.Contains(@"\" + directoryToFilter + @"\"))
                    {
                        file.Delete();
                    }
                }
                catch
                {
                    Log.Info("Algunos archivos no pudieron ser limpiados");
                }
            }

            try
            {
                foreach (var dir in directoryInfo.GetDirectories())
                {
                    if (string.IsNullOrEmpty(directoryToFilter) || (!dir.FullName.Contains(@"\" + directoryToFilter + @"\") && !dir.FullName.EndsWith(@"\" + directoryToFilter)))
                    {
                        dir.Delete(true);
                    }
                }
            }
            catch
            {
                Log.Info("Algunos directorios no pudieron ser limpiados");
            }

            try
            {
                if (remove)
                {
                    directoryInfo.Refresh();
                    if (directoryInfo.GetDirectories().Length == 0)
                    {
                        directoryInfo.Delete(true);
                    }
                }
            }
            catch
            {
                Log.Error("Algunos directorios no pudieron ser limpiados");
            }

            return string.Empty;
        }

        #endregion Methods
    }
}