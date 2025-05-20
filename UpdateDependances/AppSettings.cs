using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace UpdateDependances
{
    public class AppSettings
    {
        private readonly Logger _logger;

        public AppSettings()
        {
            _logger = new Logger("AppSettings");
        }

        public string GetConnectionString()
        {
            try
            {
                return ConfigurationManager.ConnectionStrings["SQLServer"].ConnectionString;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors de la récupération de la chaîne de connexion: {ex.Message}");
                throw new ConfigurationErrorsException("Chaîne de connexion SQL Server non trouvée", ex);
            }
        }

        public string GetSetting(string key, string defaultValue)
        {
            try
            {
                string value = ConfigurationManager.AppSettings[key];
                return string.IsNullOrEmpty(value) ? defaultValue : value;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Erreur lors de la récupération du paramètre {key}: {ex.Message}. Utilisation de la valeur par défaut: {defaultValue}");
                return defaultValue;
            }
        }

        public int GetSettingInt(string key, int defaultValue)
        {
            try
            {
                string value = ConfigurationManager.AppSettings[key];
                if (string.IsNullOrEmpty(value) || !int.TryParse(value, out int result))
                {
                    return defaultValue;
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Erreur lors de la récupération du paramètre {key}: {ex.Message}. Utilisation de la valeur par défaut: {defaultValue}");
                return defaultValue;
            }
        }

        public bool GetSettingBool(string key, bool defaultValue)
        {
            try
            {
                string value = ConfigurationManager.AppSettings[key];
                if (string.IsNullOrEmpty(value) || !bool.TryParse(value, out bool result))
                {
                    return defaultValue;
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Erreur lors de la récupération du paramètre {key}: {ex.Message}. Utilisation de la valeur par défaut: {defaultValue}");
                return defaultValue;
            }
        }

        // Méthodes spécifiques pour les chemins
        public string GetChe00() => GetSetting("Che00", @"\\Zeta_ged\E\Prog ACIM JOUANIN\Codes BE\Données");
        public string GetChe01() => GetSetting("Che01", @"\\Zeta_ged\E\FICHES MERES");
        public string GetChe02() => GetSetting("Che02", @"\\Zeta_ged\E\FICHES MERES\`DOCS METHODES");
        public string GetChe03() => GetSetting("Che03", @"\\Zeta_ged\E\FICHES MERES\`FICHES MODIFS");
        public string GetChe04() => GetSetting("Che04", @"\\Zeta_ged\E\FICHES MERES\`NOMENCLATURES");
        public string GetChe05() => GetSetting("Che05", @"\\Zeta_ged\E\FICHES MERES\`PLANS ET DOCS");
        public string GetChe08() => GetSetting("Che08", @"\\Ing_be\c\GRAFIT\DXFS");
        public string GetChe09() => GetSetting("Che09", @"\\Jetcam\C\Newjc\geo2");
        public string GetChe14() => GetSetting("Che14", @"\\zeta_ged\e\FICHES MERES\`DOCS METHODES");
        public string GetChe15() => GetSetting("Che15", @"\\Txlinux\samba\marque");
        public string GetChe16() => GetSetting("Che16", @"\\Zeta_ged\E\FICHES MERES\`PHOTOS");
    }
}