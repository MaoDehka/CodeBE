using System;
using System.ServiceProcess;
using System.Timers;
using System.Threading.Tasks;
using UpdateDependances;

namespace UpdateDependances
{
    public partial class DependancesService : ServiceBase
    {
        private Timer _dailyTimer;
        private DependancesProcessor _processor;
        private Logger _logger;
        private AppSettings _settings;
        private bool _processingInProgress = false;

        public DependancesService()
        {
            InitializeComponent();

            _settings = new AppSettings();
            _logger = new Logger("DependancesService");
            _processor = new DependancesProcessor();

            this.CanHandlePowerEvent = true;
            this.CanHandleSessionChangeEvent = true;
            this.CanPauseAndContinue = true;
            this.CanShutdown = true;
            this.AutoLog = true;
        }

        protected override void OnStart(string[] args)
        {
            _logger.LogInfo("Service démarré");

            // Configurer le timer pour exécution à l'heure programmée
            ConfigureTimer();

            // Exécution immédiate au démarrage si demandé
            if (args != null && args.Length > 0 && args[0] == "EXECUTE")
            {
                Task.Run(() => OnTimerElapsed(null, null));
            }
        }

        protected override void OnStop()
        {
            _logger.LogInfo("Service arrêté");
            _dailyTimer?.Stop();
            _dailyTimer?.Dispose();
        }

        protected override void OnPause()
        {
            _logger.LogInfo("Service mis en pause");
            _dailyTimer?.Stop();
            base.OnPause();
        }

        protected override void OnContinue()
        {
            _logger.LogInfo("Service relancé");
            _dailyTimer?.Start();
            base.OnContinue();
        }

        protected override void OnShutdown()
        {
            _logger.LogInfo("Arrêt du service (shutdown)");
            base.OnShutdown();
        }

        private void ConfigureTimer()
        {
            try
            {
                _dailyTimer = new Timer();

                // Configurer le timer pour exécuter à l'heure programmée
                string executionTimeStr = _settings.GetSetting("ExecutionTime", "02:00");
                if (TimeSpan.TryParse(executionTimeStr, out TimeSpan executionTime))
                {
                    DateTime now = DateTime.Now;
                    DateTime nextRun = new DateTime(now.Year, now.Month, now.Day,
                                                  executionTime.Hours, executionTime.Minutes, 0);

                    // Si l'heure est déjà passée aujourd'hui, programmer pour demain
                    if (now > nextRun)
                    {
                        nextRun = nextRun.AddDays(1);
                    }

                    double msUntilNextRun = (nextRun - now).TotalMilliseconds;
                    _dailyTimer.Interval = msUntilNextRun;
                    _dailyTimer.Elapsed += OnTimerElapsed;
                    _dailyTimer.Elapsed += ResetTimer; // Pour reconfigurer le timer après chaque exécution
                    _dailyTimer.Start();

                    _logger.LogInfo($"Timer configuré pour s'exécuter à {executionTimeStr}, prochain déclenchement dans {TimeSpan.FromMilliseconds(msUntilNextRun).TotalHours:F2} heures");
                }
                else
                {
                    _logger.LogError($"Format d'heure d'exécution invalide: {executionTimeStr}");

                    // Valeur par défaut: exécution toutes les 24 heures
                    _dailyTimer.Interval = 24 * 60 * 60 * 1000;
                    _dailyTimer.Elapsed += OnTimerElapsed;
                    _dailyTimer.Start();

                    _logger.LogInfo("Timer configuré par défaut (24 heures)");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors de la configuration du timer: {ex.Message}");
            }
        }

        private void ResetTimer(object sender, ElapsedEventArgs e)
        {
            _dailyTimer.Stop();
            ConfigureTimer();
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            // Éviter les exécutions simultanées
            if (_processingInProgress)
            {
                _logger.LogWarning("Un traitement est déjà en cours, ignorer cette exécution");
                return;
            }

            _processingInProgress = true;

            try
            {
                _logger.LogInfo("Démarrage du traitement planifié");
                _processor.ExecuteTraitement("PLA", null);
                _logger.LogInfo("Traitement planifié terminé avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors du traitement planifié: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                }
            }
            finally
            {
                _processingInProgress = false;
            }
        }
    }
}