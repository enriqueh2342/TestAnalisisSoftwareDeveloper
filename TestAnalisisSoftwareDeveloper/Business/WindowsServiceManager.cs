using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceProcess;
using Sample;


namespace TestAnalisisSoftwareDeveloper.Business
{
    public class WindowsServiceManager
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WindowsServiceManager));
        public void StopService(string serviceName)
        {
            try
            {
                using (ServiceController serviceController = new ServiceController(serviceName))
                {
                    if (serviceController.Status == ServiceControllerStatus.Running ||
                        serviceController.Status == ServiceControllerStatus.StartPending)
                    {
                        serviceController.Stop();
                        serviceController.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                        Log.Info($"Servicio '{serviceName}' detenido exitosamente.");
                    }
                    else if (serviceController.Status == ServiceControllerStatus.Stopped)
                    {
                        Log.Info($"El servicio '{serviceName}' ya está detenido.");
                    }
                    else
                    {
                        Log.Info($"No se puede detener el servicio '{serviceName}'. Estado actual: {serviceController.Status}");
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                Log.Error($"Error al detener el servicio '{serviceName}': {ex.Message}");
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                Log.Error($"Error al detener el servicio '{serviceName}'. Asegúrate de tener los permisos necesarios: {ex.Message}");
            }
        }

        public void StartService(string serviceName)
        {
            try
            {
                using (ServiceController serviceController = new ServiceController(serviceName))
                {
                    if (serviceController.Status == ServiceControllerStatus.Stopped ||
                        serviceController.Status == ServiceControllerStatus.StopPending)
                    {
                        serviceController.Start();
                        serviceController.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30)); 
                        Log.Info($"Servicio '{serviceName}' inició exitosamente.");
                    }
                    else if (serviceController.Status == ServiceControllerStatus.Running)
                    {
                        Log.Info($"Servicio '{serviceName}' ya está ejecutándose.");
                    }
                    else
                    {
                        Log.Info($"No se puede inciar el servicio '{serviceName}'. Estatus actual: {serviceController.Status}");
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                Log.Error($"Error al iniciar el servicio '{serviceName}': {ex.Message}");
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                Log.Error($"Error al iniciar el servicio '{serviceName}'. Asegúrate de tener los permisos necesarios");
            }

        }
    }
}
