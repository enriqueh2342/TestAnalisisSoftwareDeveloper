using Sample;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestAnalisisSoftwareDeveloper
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string baseRootFolder = @"C:\Users\enriq\OneDrive\Escritorio\Pruebas\";

        UpdatePackageSample updatePackageSample = new UpdatePackageSample();
            updatePackageSample.Version = "1.0.0";

            MonitorUpdaterManagerSample.UpdateMonitor(baseRootFolder, baseRootFolder, updatePackageSample.Version);

        }
    }
}
