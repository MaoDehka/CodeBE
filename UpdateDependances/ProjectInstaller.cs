using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace UpdateDependances
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        private ServiceProcessInstaller processInstaller;
        private ServiceInstaller serviceInstaller;

        public ProjectInstaller()
        {
            InitializeComponent();

            // Installer le processus du service
            processInstaller = new ServiceProcessInstaller();
            processInstaller.Account = ServiceAccount.LocalSystem;

            // Installer le service
            serviceInstaller = new ServiceInstaller();
            serviceInstaller.ServiceName = "DependancesService";
            serviceInstaller.DisplayName = "Service de traitement des dépendances BE";
            serviceInstaller.Description = "Service pour le traitement automatique des dépendances des codes BE";
            serviceInstaller.StartType = ServiceStartMode.Automatic;
            serviceInstaller.DelayedAutoStart = true;

            // Ajouter les installateurs
            Installers.Add(processInstaller);
            Installers.Add(serviceInstaller);
        }
    }
}