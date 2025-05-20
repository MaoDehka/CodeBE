using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UpdateDependances;

namespace UpdateDependances
{
    public class FileSystemHelper
    {
        private readonly Logger _logger;

        public FileSystemHelper()
        {
            _logger = new Logger("FileSystemHelper");
        }

        public List<string> GetDossiers(string chemin, string pattern)
        {
            List<string> dossiers = new List<string>();

            try
            {
                if (!Directory.Exists(chemin))
                {
                    _logger.LogWarning($"Le répertoire {chemin} n'existe pas");
                    return dossiers;
                }

                string[] dirs = Directory.GetDirectories(chemin);
                Regex regex = new Regex(pattern);

                foreach (string dir in dirs)
                {
                    string nomDossier = Path.GetFileName(dir);
                    if (regex.IsMatch(nomDossier))
                    {
                        dossiers.Add(nomDossier);
                    }
                }
                _logger.LogInfo($"Trouvé {dossiers.Count} dossiers correspondants au pattern '{pattern}' dans {chemin}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors de la récupération des dossiers de {chemin}: {ex.Message}");
            }

            return dossiers;
        }

        public List<string> GetFichiers(string chemin, string pattern, bool recursive = false)
        {
            List<string> fichiers = new List<string>();

            try
            {
                if (!Directory.Exists(chemin))
                {
                    _logger.LogWarning($"Le répertoire {chemin} n'existe pas");
                    return fichiers;
                }

                SearchOption option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

                foreach (string fichier in Directory.GetFiles(chemin, pattern, option))
                {
                    fichiers.Add(fichier);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors de la récupération des fichiers de {chemin} avec pattern {pattern}: {ex.Message}");
            }

            return fichiers;
        }

        public DateTime GetLastWriteTime(string cheminFichier)
        {
            try
            {
                if (File.Exists(cheminFichier))
                {
                    DateTime lastWriteTime = File.GetLastWriteTime(cheminFichier);
                    return File.GetLastWriteTime(cheminFichier);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors de la récupération de la date de dernière modification de {cheminFichier}: {ex.Message}");
            }

            return DateTime.MinValue;
        }

        public List<string> GetCodeBEFromFiles(string dossier, string extension, string prefixe, int longueurCode)
        {
            List<string> codesBE = new List<string>();

            try
            {
                if (!Directory.Exists(dossier))
                {
                    _logger.LogWarning($"Le répertoire {dossier} n'existe pas");
                    return codesBE;
                }

                foreach (string fichier in Directory.GetFiles(dossier, extension))
                {
                    string nomFichier = Path.GetFileNameWithoutExtension(fichier);

                    if (!string.IsNullOrEmpty(prefixe))
                    {
                        if (nomFichier.StartsWith(prefixe))
                        {
                            nomFichier = nomFichier.Substring(prefixe.Length);
                        }
                        else
                        {
                            continue; // Ignorer les fichiers qui ne commencent pas par le préfixe
                        }
                    }

                    if (longueurCode > 0 && nomFichier.Length >= longueurCode)
                    {
                        string codeBE = nomFichier.Substring(0, longueurCode);
                        codesBE.Add(codeBE);
                    }
                    else
                    {
                        codesBE.Add(nomFichier);
                    }
                }
                _logger.LogInfo($"Extrait {codesBE.Count} codes BE depuis les fichiers de {dossier}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors de la récupération des codes BE à partir des fichiers de {dossier}: {ex.Message}");
            }

            return codesBE;
        }

        public bool VerifierAccesRepertoire(string chemin)
        {
            try
            {
                if (Directory.Exists(chemin))
                {
                    // Tester l'accès en essayant de lister les fichiers
                    Directory.GetFiles(chemin, "*", SearchOption.TopDirectoryOnly);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Accès impossible au répertoire {chemin}: {ex.Message}");
                return false;
            }
        }

        public List<string> VerifierAccesRepertoires(List<string> chemins)
        {
            List<string> cheminsInaccessibles = new List<string>();

            foreach (string chemin in chemins)
            {
                if (!VerifierAccesRepertoire(chemin))
                {
                    cheminsInaccessibles.Add(chemin);
                }
            }

            if (cheminsInaccessibles.Count > 0)
            {
                _logger.LogWarning($"Répertoires inaccessibles: {string.Join(", ", cheminsInaccessibles)}");
            }
            else
            {
                _logger.LogInfo("Tous les répertoires sont accessibles");
            }

            return cheminsInaccessibles;
        }

        public string GetDatFMPath(string che01, string codeBE)
        {
            string dossier = codeBE.Length >= 4 ? codeBE.Substring(0, 4) : codeBE;
            return Path.Combine(che01, dossier, codeBE + ".TIF");
        }

        public bool ExisteFichier(string chemin)
        {
            try
            {
                bool exists = File.Exists(chemin);
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors de la vérification de l'existence du fichier {chemin}: {ex.Message}");
                return false;
            }
        }

        public void CopierFichier(string source, string destination)
        {
            try
            {
                // Créer le répertoire de destination s'il n'existe pas
                string destDir = Path.GetDirectoryName(destination);
                if (!Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }

                File.Copy(source, destination, true);
                _logger.LogInfo($"Fichier copié de {source} vers {destination}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors de la copie du fichier de {source} vers {destination}: {ex.Message}");
                throw;
            }
        }

        public void DeplacerFichier(string source, string destination)
        {
            try
            {
                // Créer le répertoire de destination s'il n'existe pas
                string destDir = Path.GetDirectoryName(destination);
                if (!Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }

                if (File.Exists(destination))
                {
                    File.Delete(destination);
                }

                File.Move(source, destination);
                _logger.LogInfo($"Fichier déplacé de {source} vers {destination}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors du déplacement du fichier de {source} vers {destination}: {ex.Message}");
                throw;
            }
        }

        public void SupprimerFichier(string chemin)
        {
            try
            {
                if (File.Exists(chemin))
                {
                    File.Delete(chemin);
                    _logger.LogInfo($"Fichier supprimé: {chemin}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors de la suppression du fichier {chemin}: {ex.Message}");
                throw;
            }
        }

        public long GetTailleFichier(string chemin)
        {
            try
            {
                if (File.Exists(chemin))
                {
                    FileInfo fileInfo = new FileInfo(chemin);
                    return fileInfo.Length;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors de la récupération de la taille du fichier {chemin}: {ex.Message}");
            }

            return 0;
        }
    }
}