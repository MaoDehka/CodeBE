using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Threading.Tasks;
using UpdateDependances;
using System.Linq;

namespace UpdateDependances
{
    public class SqlHelper
    {
        private readonly string _connectionString;
        private readonly Logger _logger;
        private readonly AppSettings _settings;
        private readonly int _retryAttempts;
        private readonly int _retryDelay;

        public SqlHelper(string connectionString)
        {
            _connectionString = connectionString;
            _logger = new Logger("SqlHelper");
            _settings = new AppSettings();
            _retryAttempts = _settings.GetSettingInt("RetryAttempts", 3);
            _retryDelay = _settings.GetSettingInt("RetryDelaySeconds", 60);
        }

        public int ExecuteNonQuery(string commandText, CommandType commandType = CommandType.Text, params SqlParameter[] parameters)
        {
            int attempt = 0;
            while (true)
            {
                try
                {
                    attempt++;

                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    using (SqlCommand command = new SqlCommand(commandText, connection))
                    {
                        command.CommandType = commandType;
                        command.CommandTimeout = 300; // 5 minutes

                        if (parameters != null)
                        {
                            command.Parameters.AddRange(parameters);
                        }

                        connection.Open();
                        return command.ExecuteNonQuery();
                    }
                }
                catch (SqlException ex)
                {
                    if (IsTransientError(ex) && attempt <= _retryAttempts)
                    {
                        _logger.LogWarning($"Erreur SQL transitoire (attempt {attempt}/{_retryAttempts}): {ex.Message}. Nouvelle tentative dans {_retryDelay} secondes.");
                        Task.Delay(_retryDelay * 1000).Wait();
                    }
                    else
                    {
                        _logger.LogError($"Erreur SQL: {ex.Message}");
                        _logger.LogError($"Command text: {commandText}");
                        _logger.LogError($"Parameters: {FormatParameters(parameters)}");
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Erreur non-SQL: {ex.Message}");
                    _logger.LogError($"Command text: {commandText}");
                    _logger.LogError($"Parameters: {FormatParameters(parameters)}");
                    throw;
                }
            }
        }

        public object ExecuteScalar(string commandText, CommandType commandType = CommandType.Text, params SqlParameter[] parameters)
        {
            int attempt = 0;
            while (true)
            {
                try
                {
                    attempt++;

                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    using (SqlCommand command = new SqlCommand(commandText, connection))
                    {
                        command.CommandType = commandType;
                        command.CommandTimeout = 300; // 5 minutes

                        if (parameters != null)
                        {
                            command.Parameters.AddRange(parameters);
                        }

                        connection.Open();
                        return command.ExecuteScalar();
                    }
                }
                catch (SqlException ex)
                {
                    if (IsTransientError(ex) && attempt <= _retryAttempts)
                    {
                        _logger.LogWarning($"Erreur SQL transitoire (attempt {attempt}/{_retryAttempts}): {ex.Message}. Nouvelle tentative dans {_retryDelay} secondes.");
                        Task.Delay(_retryDelay * 1000).Wait();
                    }
                    else
                    {
                        _logger.LogError($"Erreur SQL: {ex.Message}");
                        _logger.LogError($"Command text: {commandText}");
                        _logger.LogError($"Parameters: {FormatParameters(parameters)}");
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Erreur non-SQL: {ex.Message}");
                    _logger.LogError($"Command text: {commandText}");
                    _logger.LogError($"Parameters: {FormatParameters(parameters)}");
                    throw;
                }
            }
        }

        public DataTable ExecuteDataTable(string commandText, CommandType commandType = CommandType.Text, params SqlParameter[] parameters)
        {
            int attempt = 0;
            while (true)
            {
                try
                {
                    attempt++;

                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    using (SqlCommand command = new SqlCommand(commandText, connection))
                    {
                        command.CommandType = commandType;
                        command.CommandTimeout = 300; // 5 minutes

                        if (parameters != null)
                        {
                            command.Parameters.AddRange(parameters);
                        }

                        connection.Open();

                        DataTable dt = new DataTable();
                        using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                        {
                            adapter.Fill(dt);
                        }

                        return dt;
                    }
                }
                catch (SqlException ex)
                {
                    if (IsTransientError(ex) && attempt <= _retryAttempts)
                    {
                        _logger.LogWarning($"Erreur SQL transitoire (attempt {attempt}/{_retryAttempts}): {ex.Message}. Nouvelle tentative dans {_retryDelay} secondes.");
                        Task.Delay(_retryDelay * 1000).Wait();
                    }
                    else
                    {
                        _logger.LogError($"Erreur SQL: {ex.Message}");
                        _logger.LogError($"Command text: {commandText}");
                        _logger.LogError($"Parameters: {FormatParameters(parameters)}");
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Erreur non-SQL: {ex.Message}");
                    _logger.LogError($"Command text: {commandText}");
                    _logger.LogError($"Parameters: {FormatParameters(parameters)}");
                    throw;
                }
            }
        }

        public void ExecuteStoredProcedure(string procedureName, params SqlParameter[] parameters)
        {
            ExecuteNonQuery(procedureName, CommandType.StoredProcedure, parameters);
        }

        public void ExecuteWithTransaction(Action<SqlConnection, SqlTransaction> action)
        {
            int attempt = 0;
            while (true)
            {
                try
                {
                    attempt++;

                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();

                        using (SqlTransaction transaction = connection.BeginTransaction())
                        {
                            try
                            {
                                action(connection, transaction);
                                transaction.Commit();
                                return;
                            }
                            catch
                            {
                                transaction.Rollback();
                                throw;
                            }
                        }
                    }
                }
                catch (SqlException ex)
                {
                    if (IsTransientError(ex) && attempt <= _retryAttempts)
                    {
                        _logger.LogWarning($"Erreur SQL transitoire dans la transaction (attempt {attempt}/{_retryAttempts}): {ex.Message}. Nouvelle tentative dans {_retryDelay} secondes.");
                        Task.Delay(_retryDelay * 1000).Wait();
                    }
                    else
                    {
                        _logger.LogError($"Erreur SQL dans la transaction: {ex.Message}");
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Erreur non-SQL dans la transaction: {ex.Message}");
                    throw;
                }
            }
        }

        public void UploadToTempTable(string tempTableName, List<string> codeBEList, SqlConnection connection = null, SqlTransaction transaction = null)
        {
            if (codeBEList == null || codeBEList.Count == 0)
            {
                return;
            }

            bool externalConnection = connection != null;

            try
            {
                if (!externalConnection)
                {
                    connection = new SqlConnection(_connectionString);
                    connection.Open();
                }

                // Créer la table temporaire si elle n'existe pas
                string createTableCommand = $@"
                IF OBJECT_ID('tempdb..{tempTableName}') IS NULL
                BEGIN
                    CREATE TABLE {tempTableName} (
                        CodeBe VARCHAR(50)
                    );
                END
                ELSE
                BEGIN
                    TRUNCATE TABLE {{tempTableName}};
                END";
                
                using (SqlCommand cmd = new SqlCommand(createTableCommand, connection))
                {
                    if (transaction != null)
                        cmd.Transaction = transaction;
                    
                    cmd.ExecuteNonQuery();
                }
                
                // Utiliser un SqlBulkCopy pour l'insertion massive
                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction))
                {
                    bulkCopy.DestinationTableName = tempTableName;
                    bulkCopy.BulkCopyTimeout = 600;

                    DataTable dt = new DataTable();
                    dt.Columns.Add("CodeBe", typeof(string));
                    
                    foreach (string codeBE in codeBEList)
                    {
                        if (!string.IsNullOrEmpty(codeBE) && !dt.AsEnumerable().Any(row => row.Field<string>("CodeBe") == codeBE))
                        {
                            dt.Rows.Add(codeBE);
                        }
                    }

                    if (dt.Rows.Count > 0)
                    {
                        bulkCopy.WriteToServer(dt);
                        _logger.LogInfo($"Importé {dt.Rows.Count} codes BE dans la table temporaire {tempTableName}");
                    }
                    else
                    {
                        _logger.LogWarning($"Aucun code BE à importer dans la table temporaire {tempTableName}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors de l'importation des codes BE dans la table temporaire {tempTableName}: {ex.Message}");
                throw;
            }
            finally
            {
                // Fermer la connexion uniquement si nous l'avons créée
                if (!externalConnection && connection != null)
                {
                    connection.Close();
                    connection.Dispose();
                }
            }
        }

        public void ExecuteDirectSqlWithRetry(string sql)
        {
            int attempt = 0;
            while (true)
            {
                try
                {
                    attempt++;
                    
                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();
                        using (SqlCommand command = new SqlCommand(sql, connection))
                        {
                            command.CommandTimeout = 300; // 5 minutes
                            command.ExecuteNonQuery();
                        }
                        return;
                    }
                }
                catch (SqlException ex)
                {
                    if (IsTransientError(ex) && attempt <= _retryAttempts)
                    {
                        _logger.LogWarning($"Erreur SQL transitoire (attempt {{attempt}}/{{_retryAttempts}}): {{ex.Message}}. Nouvelle tentative dans {{_retryDelay}} secondes.");
                        Task.Delay(_retryDelay * 1000).Wait();
                    }
                    else
                    {
                        _logger.LogError($"Erreur SQL: {{ex.Message}}");
                        _logger.LogError($"SQL: {{sql}}");
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Erreur non-SQL: {ex.Message}");
                    _logger.LogError($"SQL: {sql}");
                    throw;
                }
            }
        }
        
        private bool IsTransientError(SqlException ex)
        {
            // Liste des erreurs SQL Server considérées comme transitoires
            int[] transientErrorNumbers = { -2, 4060, 40197, 40501, 40613, 49918, 49919, 49920 };
            
            foreach (SqlError error in ex.Errors)
            {
                if (Array.IndexOf(transientErrorNumbers, error.Number) >= 0)
                {
                    return true;
                }
            }
            
            return false;
        }
        
        private string FormatParameters(SqlParameter[] parameters)
        {
            if (parameters == null || parameters.Length == 0)
            {
                return "None";
            }
            
            List<string> paramInfo = new List<string>();
            foreach (SqlParameter param in parameters)
            {
                string value = param.Value?.ToString() ?? "NULL";
                paramInfo.Add($"{{param.ParameterName}} = {value}");
            }
            
            return string.Join(", ", paramInfo);
        }
    }
}