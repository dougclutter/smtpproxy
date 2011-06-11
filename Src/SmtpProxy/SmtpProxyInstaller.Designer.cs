namespace SmtpProxy
{
    partial class SmtpProxyInstaller
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                    components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.smtpProxyServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.smtpProxyServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // smtpProxyServiceProcessInstaller
            // 
            this.smtpProxyServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.NetworkService;
            this.smtpProxyServiceProcessInstaller.Password = null;
            this.smtpProxyServiceProcessInstaller.Username = null;
            // 
            // smtpProxyServiceInstaller
            // 
            this.smtpProxyServiceInstaller.Description = "Provides an SMTP Proxy that forwards SMTP traffic to an external SMTP server";
            this.smtpProxyServiceInstaller.DisplayName = "SMTP Proxy";
            this.smtpProxyServiceInstaller.ServiceName = "SmtpProxy";
            this.smtpProxyServiceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.smtpProxyServiceProcessInstaller,
            this.smtpProxyServiceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller smtpProxyServiceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller smtpProxyServiceInstaller;
    }
}