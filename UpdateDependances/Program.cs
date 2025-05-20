using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace UpdateDependances
{
    internal static class Program
    {
        /// <summary>
        /// Point d'entrée principal de l'application.
        /// </summary>
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                DependancesProcessor processor = new DependancesProcessor();

                if (args[0].ToUpper() == "ALL")
                {
                    processor.ExecuteTraitement("ALL", null);
                }
                else if (args[0].ToUpper().StartsWith("ONE") && args.Length > 1)
                {
                    processor.ExecuteTraitement("ONE", args[1]);
                }
                else if (args[0].ToUpper() == "PLA")
                {
                    processor.ExecuteTraitement("PLA", null);
                }
                else
                {
                    Console.WriteLine("Mode de lancement invalide");
                    Console.WriteLine("Utilisation: DependancesService.exe [ALL|ONE code_be|PLA]");
                }
            }
            // Sinon démarrer en tant que service Windows
            else
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                    new DependancesService()
                };
                ServiceBase.Run(ServicesToRun);
            }
        }
    }
}
