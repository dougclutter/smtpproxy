using System;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace SmtpProxy
{
    /// <summary>
    /// Provides services so Windows can install SmtpProxy as a Windows Service
    /// </summary>
    [RunInstaller(true)]
    public partial class SmtpProxyInstaller : Installer
    {
        readonly ServiceControllerStatus StatusOnStart = ServiceControllerStatus.Stopped;

        public SmtpProxyInstaller()
        {
            StatusOnStart = GetCurrentState();

            InitializeComponent();

            BeforeInstall += (s, e) => StopService();
            BeforeUninstall += (s, e) => StopService();
            AfterInstall += (s, e) => SetServiceStatus(ServiceControllerStatus.Running);
            AfterRollback += (s, e) => SetServiceStatus();
        }
        void StopService()
        {
            LogMessage("SmtpProxyInstaller.StopService Started");

            try
            {
                // Stop the service
                SetServiceStatus(ServiceControllerStatus.Stopped);
            }
            catch (Exception ex)
            {
                LogMessage("SmtpProxyInstaller.StopService {0} caught: {1}", ex.GetType().Name, ex.Message);
            }
        }
        ServiceControllerStatus GetCurrentState()
        {
            LogMessage("SmtpProxyInstaller.GetCurrentState Started");

            try
            {
                using (var service = new ServiceController(smtpProxyServiceInstaller.ServiceName))
                {
                    return service.Status;
                }
            }
            catch (Exception ex)
            {
                LogMessage("SmtpProxyInstaller.GetCurrentState {0} caught: {1}", ex.GetType().Name, ex.Message);
                return ServiceControllerStatus.Stopped;
            }
        }
        void SetServiceStatus()
        {
            SetServiceStatus(StatusOnStart);
        }
        void SetServiceStatus(ServiceControllerStatus statusDesired)
        {
            LogMessage("SmtpProxyInstaller.SetServiceStatus Started: {0}", statusDesired);

            try
            {
                using (var service = new ServiceController(smtpProxyServiceInstaller.ServiceName))
                {
                    if (service.Status == statusDesired)
                        return;
                    else if (statusDesired == ServiceControllerStatus.Running)
                        service.Start();
                    else if (statusDesired == ServiceControllerStatus.Stopped)
                        service.Stop();
                }
            }
            catch (Exception ex)
            {
                LogMessage("SmtpProxyInstaller.SetServiceStatus {0} caught: {1}", ex.GetType().Name, ex.Message);
            }
        }
        void LogMessage(string format, params object[] args)
        {
            string msg = string.Format(format, args);

            if (Context == null)
                Console.WriteLine(msg);
            else
                Context.LogMessage(msg);
        }
    }
}
