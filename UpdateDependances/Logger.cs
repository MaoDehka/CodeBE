using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace UpdateDependances
{
    public class Logger
    {
        private string _logFolder;
        private string _logName;
        private readonly ConcurrentQueue<string> _logQueue;
        private readonly Task _logTask;
        private readonly CancellationTokenSource _cancelTokenSource;

        public enum LogLevel
        {
            DEBUG,
            INFO,
            WARNING,
            ERROR,
            FATAL
        }

        private LogLevel _currentLogLevel;

        public Logger(string logName)
        {
            _logName = logName;
            _logFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

            // Créer le dossier des logs s'il n'existe pas
            if (!Directory.Exists(_logFolder))
            {
                Directory.CreateDirectory(_logFolder);
            }

            // Définir le niveau de log
            string logLevelStr = System.Configuration.ConfigurationManager.AppSettings["LogLevel"] ?? "INFO";
            if (!Enum.TryParse(logLevelStr, true, out _currentLogLevel))
            {
                _currentLogLevel = LogLevel.INFO;
            }

            // Initialiser la file d'attente et le traitement asynchrone
            _logQueue = new ConcurrentQueue<string>();
            _cancelTokenSource = new CancellationTokenSource();
            _logTask = Task.Factory.StartNew(ProcessLogQueue, _cancelTokenSource.Token,
                                           TaskCreationOptions.LongRunning, TaskScheduler.Default);

            LogInfo($"Logger '{logName}' initialisé avec le niveau {_currentLogLevel}");
        }

        ~Logger()
        {
            if (!_cancelTokenSource.IsCancellationRequested)
            {
                _cancelTokenSource.Cancel();
                _logTask.Wait(1000); // Attendre max 1 seconde que la tâche se termine
            }
        }

        public void LogDebug(string message)
        {
            if (_currentLogLevel <= LogLevel.DEBUG)
                Log("DEBUG", message);
        }

        public void LogInfo(string message)
        {
            if (_currentLogLevel <= LogLevel.INFO)
                Log("INFO", message);
        }

        public void LogWarning(string message)
        {
            if (_currentLogLevel <= LogLevel.WARNING)
                Log("WARNING", message);
        }

        public void LogError(string message)
        {
            if (_currentLogLevel <= LogLevel.ERROR)
                Log("ERROR", message);
        }

        public void LogFatal(string message)
        {
            if (_currentLogLevel <= LogLevel.FATAL)
                Log("FATAL", message);
        }

        private void Log(string level, string message)
        {
            try
            {
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
                _logQueue.Enqueue(logEntry);
            }
            catch
            {
                // Rien à faire si l'enqueue échoue
            }
        }

        private void ProcessLogQueue()
        {
            while (!_cancelTokenSource.IsCancellationRequested)
            {
                try
                {
                    // Traiter tous les messages en attente
                    while (_logQueue.TryDequeue(out string logEntry))
                    {
                        string logFile = Path.Combine(_logFolder, $"{_logName}_{DateTime.Now:yyyy-MM-dd}.log");

                        try
                        {
                            using (StreamWriter writer = new StreamWriter(logFile, true, Encoding.UTF8))
                            {
                                writer.WriteLine(logEntry);
                            }
                        }
                        catch
                        {
                            // Ignorer les erreurs d'écriture
                        }
                    }

                    // Attendre un peu avant de vérifier à nouveau
                    Thread.Sleep(100);
                }
                catch
                {
                    // Ignorer les erreurs
                }
            }
        }
    }
}