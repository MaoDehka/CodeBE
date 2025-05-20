using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpdateDependances
{
    public class DependancesProcessor
    {
        private readonly string _connectionString;
        private readonly SqlHelper _sqlHelper;
        private readonly FileSystemHelper _fileHelper;
        private readonly Logger _logger;
        private readonly AppSettings _settings;

        // Chemins d'accès
        private readonly string Che00;
        private readonly string Che01;
        private readonly string Che02;
        private readonly string Che03;
        private readonly string Che04;
        private readonly string Che05;
        private readonly string Che08;
        private readonly string Che09;
        private readonly string Che14;
        private readonly string Che15;
        private readonly string Che16;

        public DependancesProcessor()
        {
            _settings = new AppSettings();
            _connectionString = _settings.GetConnectionString();
            _sqlHelper = new SqlHelper(_connectionString);
            _fileHelper = new FileSystemHelper();
            _logger = new Logger("DependancesProcessor");

            // Initialisation des chemins
            Che00 = _settings.GetChe00();
            Che01 = _settings.GetChe01();
            Che02 = _settings.GetChe02();
            Che03 = _settings.GetChe03();
            Che04 = _settings.GetChe04();
            Che05 = _settings.GetChe05();
            Che08 = _settings.GetChe08();
            Che09 = _settings.GetChe09();
            Che14 = _settings.GetChe14();
            Che15 = _settings.GetChe15();
            Che16 = _settings.GetChe16();
        }

        public void ExecuteTraitement(string mode, string codeBE)
        {
            _logger.LogInfo($"Exécution du traitement en mode {mode}" + (codeBE != null ? $" pour le code BE {codeBE}" : ""));

            try
            {
                // Vérifier les accès aux répertoires
                List<string> chemins = new List<string>
                {
                    Che00, Che01, Che02, Che03, Che04, Che05, Che08, Che09, Che14, Che15, Che16
                };

                List<string> cheminsInaccessibles = _fileHelper.VerifierAccesRepertoires(chemins);

                if (cheminsInaccessibles.Count > 0)
                {
                    string message = $"Accès impossible aux répertoires: {string.Join(", ", cheminsInaccessibles)}";
                    _logger.LogError(message);
                    throw new IOException(message);
                }

                if (mode == "ONE" && codeBE != null)
                {
                    ExecuteTraitementIndividuel(codeBE);
                }
                else // "ALL" ou "PLA"
                {
                    ExecuteTraitementCollectif();
                }

                _logger.LogInfo($"Traitement {mode} terminé avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors de l'exécution du traitement {mode}: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                }

                throw;
            }
        }

        private void ExecuteTraitementCollectif()
        {
            _logger.LogInfo("Exécution du traitement collectif");

            // Appel initial à la procédure qui va créer les tables temporaires et initialiser
            _sqlHelper.ExecuteStoredProcedure("sp_TraitementCollectif");

            // Traitement de chaque type de dépendance
            ProcessDepFichesMeres();
            ProcessDepDocumentationMethode();
            ProcessDepFicheModificationNumerisee();
            ProcessDepNomenclature();
            ProcessDepPlansEtDocumentsNumerises();
            ProcessDepFichiersMica();
            ProcessDepFichiersTolerie();
            ProcessDepDocumentationMethodesNumerisees();
            ProcessDepMarquage();
            ProcessDepPhotos();
            ProcessDepAmeliorationsPermanentesFaites();
            ProcessDepAmeliorationsPermanentesNonFaites();
            ProcessDepPlansAcim();
            ProcessDepComptagePlansNonNumerises();
            ProcessDepPlansNonNumerisesDeclares();
            ProcessDepModificationsFicheMereFaites();
            ProcessDepModificationsFicheMereEnCours();
            ProcessDepRetoursNC();

            // Finaliser les mises à jour des dépendances
            FinalizeDepUpdates();

            // Mise à jour des dates de scan FM
            UpdateDatesScans();

            _logger.LogInfo("Traitement collectif terminé");
        }

        private void ExecuteTraitementIndividuel(string codeBE)
        {
            _logger.LogInfo($"Exécution du traitement individuel pour le code BE {codeBE}");

            // Procédure d'initialisation
            _sqlHelper.ExecuteStoredProcedure("sp_TraitementInitial");

            // Réinitialiser toutes les dépendances pour ce code BE
            _sqlHelper.ExecuteNonQuery(
                "UPDATE Dependances SET Dep01 = 0, Dep02 = 0, Dep03 = 0, Dep04 = 0, Dep05 = 0, " +
                "Dep06 = 0, Dep07 = 0, Dep08 = 0, Dep09 = 0, Dep10 = 0, Dep11 = 0, Dep12 = 0, " +
                "Dep13 = 0, Dep14 = 0, Dep15 = 0, Dep16 = 0, Dep17 = 0 " +
                "WHERE CodeBE = @CodeBE",
                CommandType.Text,
                new SqlParameter("@CodeBE", codeBE));

            // Insérer le code BE s'il n'existe pas déjà
            _sqlHelper.ExecuteNonQuery(
                "IF NOT EXISTS (SELECT 1 FROM Dependances WHERE CodeBE = @CodeBE) " +
                "INSERT INTO Dependances (CodeBE) VALUES (@CodeBE)",
                CommandType.Text,
                new SqlParameter("@CodeBE", codeBE));

            // Tables temporaires pour traitement individuel
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Vérification 1: Fiche mère
                        bool ficheMereExiste = _fileHelper.ExisteFichier(
                            _fileHelper.GetDatFMPath(Che01, codeBE));

                        if (ficheMereExiste)
                        {
                            using (SqlCommand cmd = new SqlCommand(
                                "UPDATE Dependances SET Dep01 = 1 WHERE CodeBE = @CodeBE", connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@CodeBE", codeBE);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // Vérification 2: Documentation méthode
                        string docMethodePath = Path.Combine(Che02, codeBE.Substring(0, 4), codeBE + ".xls");
                        bool docMethodeExiste = _fileHelper.ExisteFichier(docMethodePath);

                        if (docMethodeExiste)
                        {
                            using (SqlCommand cmd = new SqlCommand(
                                "UPDATE Dependances SET Dep02 = 1 WHERE CodeBE = @CodeBE", connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@CodeBE", codeBE);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // Vérification 3: Fiche modification numérisée
                        string ficheModifPath = Path.Combine(Che03, codeBE.Substring(0, 4), codeBE + ".tif");
                        bool ficheModifExiste = _fileHelper.ExisteFichier(ficheModifPath);

                        if (ficheModifExiste)
                        {
                            using (SqlCommand cmd = new SqlCommand(
                                "UPDATE Dependances SET Dep03 = 1 WHERE CodeBE = @CodeBE", connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@CodeBE", codeBE);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // Vérification 4: Nomenclature
                        string nomenclaturePath = Path.Combine(Che04, codeBE.Substring(0, 4), codeBE + ".xls");
                        bool nomenclatureExiste = _fileHelper.ExisteFichier(nomenclaturePath);

                        if (nomenclatureExiste)
                        {
                            using (SqlCommand cmd = new SqlCommand(
                                "UPDATE Dependances SET Dep04 = 1 WHERE CodeBE = @CodeBE", connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@CodeBE", codeBE);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // Vérification 5: Plans et documents numérisés
                        string planDocPath = Path.Combine(Che05, codeBE.Substring(0, 4), codeBE + ".tif");
                        bool planDocExiste = _fileHelper.ExisteFichier(planDocPath);

                        if (planDocExiste)
                        {
                            using (SqlCommand cmd = new SqlCommand(
                                "UPDATE Dependances SET Dep05 = 1 WHERE CodeBE = @CodeBE", connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@CodeBE", codeBE);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // Vérification 6: Améliorations permanentes faites
                        using (SqlCommand cmd = new SqlCommand(
                            "SELECT COUNT(*) FROM BloDemande WHERE CodeBE = @CodeBE AND DateModification IS NOT NULL",
                            connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@CodeBE", codeBE);
                            int count = Convert.ToInt32(cmd.ExecuteScalar());

                            if (count > 0)
                            {
                                using (SqlCommand updateCmd = new SqlCommand(
                                    "UPDATE Dependances SET Dep06 = 1 WHERE CodeBE = @CodeBE", connection, transaction))
                                {
                                    updateCmd.Parameters.AddWithValue("@CodeBE", codeBE);
                                    updateCmd.ExecuteNonQuery();
                                }
                            }
                        }

                        // Vérification 7: Améliorations permanentes non faites
                        using (SqlCommand cmd = new SqlCommand(
                            "SELECT COUNT(*) FROM BloDemande WHERE CodeBE = @CodeBE AND DateModification IS NULL",
                            connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@CodeBE", codeBE);
                            int count = Convert.ToInt32(cmd.ExecuteScalar());

                            if (count > 0)
                            {
                                using (SqlCommand updateCmd = new SqlCommand(
                                    "UPDATE Dependances SET Dep07 = 1 WHERE CodeBE = @CodeBE", connection, transaction))
                                {
                                    updateCmd.Parameters.AddWithValue("@CodeBE", codeBE);
                                    updateCmd.ExecuteNonQuery();
                                }
                            }
                        }

                        // Vérification 8: Fichiers mica
                        // La recherche de fichiers .dxf est plus complexe car elle implique des sous-dossiers
                        bool fichierMicaTrouve = false;
                        string dossierPrincipal = Path.Combine(Che08, codeBE.Substring(0, 4));

                        if (Directory.Exists(dossierPrincipal))
                        {
                            string[] sousDossiers = Directory.GetDirectories(dossierPrincipal);
                            foreach (string sousDossier in sousDossiers)
                            {
                                string pattern = $"{codeBE.Substring(0, 8)}_{codeBE.Substring(9)}*.dxf";
                                string[] fichiersMica = Directory.GetFiles(sousDossier, pattern);
                                if (fichiersMica.Length > 0)
                                {
                                    fichierMicaTrouve = true;
                                    break;
                                }
                            }
                        }

                        if (fichierMicaTrouve)
                        {
                            using (SqlCommand cmd = new SqlCommand(
                                "UPDATE Dependances SET Dep08 = 1 WHERE CodeBE = @CodeBE", connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@CodeBE", codeBE);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // Vérification 9: Fichiers tôlerie
                        string cheminTolerie = Path.Combine(Che09, codeBE.Substring(0, 4), codeBE.Substring(4, 4));
                        bool fichierTolerieTrouve = false;

                        if (Directory.Exists(cheminTolerie))
                        {
                            string pattern = $"{codeBE.Substring(0, 8)}_{codeBE.Substring(9)}*.jgf";
                            string[] fichiersTolerie = Directory.GetFiles(cheminTolerie, pattern);
                            fichierTolerieTrouve = fichiersTolerie.Length > 0;
                        }

                        if (fichierTolerieTrouve)
                        {
                            using (SqlCommand cmd = new SqlCommand(
                                "UPDATE Dependances SET Dep09 = 1 WHERE CodeBE = @CodeBE", connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@CodeBE", codeBE);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // Vérification 10: Plans Acim
                        using (SqlCommand cmd = new SqlCommand(
                            "SELECT COUNT(*) FROM ListePlan WHERE CodeArticle = @CodeBE",
                            connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@CodeBE", codeBE);
                            int count = Convert.ToInt32(cmd.ExecuteScalar());

                            if (count > 0)
                            {
                                using (SqlCommand updateCmd = new SqlCommand(
                                    "UPDATE Dependances SET Dep10 = 1 WHERE CodeBE = @CodeBE", connection, transaction))
                                {
                                    updateCmd.Parameters.AddWithValue("@CodeBE", codeBE);
                                    updateCmd.ExecuteNonQuery();
                                }
                            }
                        }

                        // Vérification 11: Plan non numérisé déclaré
                        using (SqlCommand cmd = new SqlCommand(
                            "SELECT COUNT(*) FROM Plans WHERE CodeBE = @CodeBE",
                            connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@CodeBE", codeBE);
                            int count = Convert.ToInt32(cmd.ExecuteScalar());

                            if (count > 0)
                            {
                                using (SqlCommand updateCmd = new SqlCommand(
                                    "UPDATE Dependances SET Dep11 = 1 WHERE CodeBE = @CodeBE", connection, transaction))
                                {
                                    updateCmd.Parameters.AddWithValue("@CodeBE", codeBE);
                                    updateCmd.ExecuteNonQuery();
                                }
                            }
                        }

                        // Vérification 12: Modifications de fiche mère faites
                        using (SqlCommand cmd = new SqlCommand(
                            "SELECT COUNT(*) FROM BloModificationsFM WHERE CodeBE = @CodeBE " +
                            "AND DateRealisation IS NOT NULL " +
                            "AND (ModifTole = 0 OR (ModifTole = 1 AND DateModifTole IS NOT NULL)) " +
                            "AND (ModifCodeBE = 0 OR (ModifCodeBE = 1 AND DateModifCodeBE IS NOT NULL))",
                            connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@CodeBE", codeBE);
                            int count = Convert.ToInt32(cmd.ExecuteScalar());

                            if (count > 0)
                            {
                                using (SqlCommand updateCmd = new SqlCommand(
                                    "UPDATE Dependances SET Dep12 = 1 WHERE CodeBE = @CodeBE", connection, transaction))
                                {
                                    updateCmd.Parameters.AddWithValue("@CodeBE", codeBE);
                                    updateCmd.ExecuteNonQuery();
                                }
                            }
                        }

                        // Vérification 13: Modifications de fiche mère en cours
                        using (SqlCommand cmd = new SqlCommand(
                            "SELECT COUNT(*) FROM BloModificationsFM WHERE CodeBE = @CodeBE " +
                            "AND (DateRealisation IS NULL " +
                            "OR (ModifTole = 1 AND DateModifTole IS NULL) " +
                            "OR (ModifCodeBE = 1 AND DateModifCodeBE IS NULL))",
                            connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@CodeBE", codeBE);
                            int count = Convert.ToInt32(cmd.ExecuteScalar());

                            if (count > 0)
                            {
                                using (SqlCommand updateCmd = new SqlCommand(
                                    "UPDATE Dependances SET Dep13 = 1 WHERE CodeBE = @CodeBE", connection, transaction))
                                {
                                    updateCmd.Parameters.AddWithValue("@CodeBE", codeBE);
                                    updateCmd.ExecuteNonQuery();
                                }
                            }
                        }

                        // Vérification 14: Documentation méthodes numérisées
                        string docMethodeNumPath = Path.Combine(Che14, codeBE.Substring(0, 4), codeBE + ".tif");
                        bool docMethodeNumExiste = _fileHelper.ExisteFichier(docMethodeNumPath);

                        if (docMethodeNumExiste)
                        {
                            using (SqlCommand cmd = new SqlCommand(
                                "UPDATE Dependances SET Dep14 = 1 WHERE CodeBE = @CodeBE", connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@CodeBE", codeBE);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // Vérification 15: Marquage
                        string marquagePath = Path.Combine(Che15, codeBE);
                        bool marquageExiste = Directory.Exists(marquagePath);

                        if (marquageExiste)
                        {
                            using (SqlCommand cmd = new SqlCommand(
                                "UPDATE Dependances SET Dep15 = 1 WHERE CodeBE = @CodeBE", connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@CodeBE", codeBE);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // Vérification 16: Photos
                        string photosPattern = codeBE + "*.jpg";
                        string photosDir = Path.Combine(Che16, codeBE.Substring(0, 4));
                        bool photosTrouvees = false;

                        if (Directory.Exists(photosDir))
                        {
                            string[] photos = Directory.GetFiles(photosDir, photosPattern);
                            photosTrouvees = photos.Length > 0;
                        }

                        if (photosTrouvees)
                        {
                            using (SqlCommand cmd = new SqlCommand(
                                "UPDATE Dependances SET Dep16 = 1 WHERE CodeBE = @CodeBE", connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@CodeBE", codeBE);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // Vérification 17: Retours NC
                        using (SqlCommand cmd = new SqlCommand(
                            "SELECT COUNT(*) FROM AC_F_RETOUR_AJ WHERE REFERENCE = @CodeBE AND TYPE = 'NC' AND LHT <> ''",
                            connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@CodeBE", codeBE);
                            int count = Convert.ToInt32(cmd.ExecuteScalar());

                            if (count > 0)
                            {
                                using (SqlCommand updateCmd = new SqlCommand(
                                    "UPDATE Dependances SET Dep17 = 1 WHERE CodeBE = @CodeBE", connection, transaction))
                                {
                                    updateCmd.Parameters.AddWithValue("@CodeBE", codeBE);
                                    updateCmd.ExecuteNonQuery();
                                }
                            }
                        }

                        // Vérification supplémentaire: Fiche fille
                        using (SqlCommand cmd = new SqlCommand(
                            "SELECT COUNT(*) FROM Filles WHERE Fille = @CodeBE",
                            connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@CodeBE", codeBE);
                            int count = Convert.ToInt32(cmd.ExecuteScalar());

                            if (count > 0)
                            {
                                using (SqlCommand updateCmd = new SqlCommand(
                                    "UPDATE Dependances SET Dep01 = 1 WHERE CodeBE = @CodeBE", connection, transaction))
                                {
                                    updateCmd.Parameters.AddWithValue("@CodeBE", codeBE);
                                    updateCmd.ExecuteNonQuery();
                                }
                            }
                        }

                        // Mise à jour de la date de scan FM
                        if (ficheMereExiste)
                        {
                            DateTime scanDate = _fileHelper.GetLastWriteTime(
                                _fileHelper.GetDatFMPath(Che01, codeBE));

                            if (scanDate != DateTime.MinValue)
                            {
                                using (SqlCommand cmd = new SqlCommand(
                                    "UPDATE CodesBE SET scan_fm = @ScanDate WHERE code_ref = @CodeBE",
                                    connection, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@ScanDate", scanDate);
                                    cmd.Parameters.AddWithValue("@CodeBE", codeBE);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        _logger.LogError($"Erreur lors du traitement individuel pour {codeBE}: {ex.Message}");
                        throw;
                    }
                }
            }

            _logger.LogInfo($"Traitement individuel pour le code BE {codeBE} terminé");
        }

        private void ProcessDepFichesMeres()
        {
            _logger.LogInfo("Traitement des fiches mères");

            try
            {
                List<string> codesBE = new List<string>();

                // Récupérer les répertoires de niveau 1
                List<string> dossiers = _fileHelper.GetDossiers(Che01, "[A-Za-z]{4}");

                foreach (string dossier in dossiers)
                {
                    string cheminDossier = Path.Combine(Che01, dossier);
                    List<string> fichiers = _fileHelper.GetFichiers(cheminDossier, "*.tif");

                    foreach (string fichier in fichiers)
                    {
                        string codeBe = Path.GetFileNameWithoutExtension(fichier);
                        codesBE.Add(codeBe);
                    }
                }

                // Mettre à jour la table temporaire et le compteur
                _sqlHelper.ExecuteWithTransaction((connection, transaction) =>
                {
                    // Utiliser la table temporaire Dep01FichesMères
                    string tempTable = DependanceTypes.GetTempTableName(DependanceTypes.FicheMereOuFille);

                    // Créer et remplir la table temporaire
                    _sqlHelper.UploadToTempTable(tempTable, codesBE, connection, transaction);

                    // Mettre à jour le compteur
                    using (SqlCommand cmd = new SqlCommand(
                        $"UPDATE Compteurs SET ValCtr = {codesBE.Count} WHERE CodCtr = @CodCtr",
                        connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@CodCtr", DependanceTypes.GetCodCtr(DependanceTypes.FicheMereOuFille));
                        cmd.ExecuteNonQuery();
                    }
                });

                _logger.LogInfo($"Traitement des fiches mères terminé. {codesBE.Count} fiches trouvées.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors du traitement des fiches mères: {ex.Message}");
                throw;
            }
        }

        private void ProcessDepDocumentationMethode()
        {
            _logger.LogInfo("Traitement des documentations méthode");

            try
            {
                List<string> codesBE = new List<string>();

                // Récupérer les répertoires de niveau 1
                List<string> dossiers = _fileHelper.GetDossiers(Che02, "[A-Za-z]{4}");

                foreach (string dossier in dossiers)
                {
                    string cheminDossier = Path.Combine(Che02, dossier);
                    List<string> fichiers = _fileHelper.GetFichiers(cheminDossier, "*.xls");

                    foreach (string fichier in fichiers)
                    {
                        string codeBe = Path.GetFileNameWithoutExtension(fichier);
                        codesBE.Add(codeBe);
                    }
                }

                // Mettre à jour la table temporaire et le compteur
                _sqlHelper.ExecuteWithTransaction((connection, transaction) =>
                {
                    // Utiliser la table temporaire
                    string tempTable = DependanceTypes.GetTempTableName(DependanceTypes.DocumentationMethodeXls);

                    // Créer et remplir la table temporaire
                    _sqlHelper.UploadToTempTable(tempTable, codesBE, connection, transaction);

                    // Mettre à jour le compteur
                    using (SqlCommand cmd = new SqlCommand(
                        $"UPDATE Compteurs SET ValCtr = {codesBE.Count} WHERE CodCtr = @CodCtr",
                        connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@CodCtr", DependanceTypes.GetCodCtr(DependanceTypes.DocumentationMethodeXls));
                        cmd.ExecuteNonQuery();
                    }
                });

                _logger.LogInfo($"Traitement des documentations méthode terminé. {codesBE.Count} documentations trouvées.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors du traitement des documentations méthode: {ex.Message}");
                throw;
            }
        }

        private void ProcessDepFicheModificationNumerisee()
        {
            _logger.LogInfo("Traitement des fiches modifications numérisées");

            try
            {
                List<string> codesBE = new List<string>();

                // Récupérer les répertoires de niveau 1
                List<string> dossiers = _fileHelper.GetDossiers(Che03, "[A-Za-z]{4}");

                foreach (string dossier in dossiers)
                {
                    string cheminDossier = Path.Combine(Che03, dossier);
                    List<string> fichiers = _fileHelper.GetFichiers(cheminDossier, "*.tif");

                    foreach (string fichier in fichiers)
                    {
                        string codeBe = Path.GetFileNameWithoutExtension(fichier);
                        codesBE.Add(codeBe);
                    }
                }

                // Mettre à jour la table temporaire et le compteur
                _sqlHelper.ExecuteWithTransaction((connection, transaction) =>
                {
                    // Utiliser la table temporaire
                    string tempTable = DependanceTypes.GetTempTableName(DependanceTypes.FicheModificationNumerisee);

                    // Créer et remplir la table temporaire
                    _sqlHelper.UploadToTempTable(tempTable, codesBE, connection, transaction);

                    // Mettre à jour le compteur
                    using (SqlCommand cmd = new SqlCommand(
                        $"UPDATE Compteurs SET ValCtr = {codesBE.Count} WHERE CodCtr = @CodCtr",
                        connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@CodCtr", DependanceTypes.GetCodCtr(DependanceTypes.FicheModificationNumerisee));
                        cmd.ExecuteNonQuery();
                    }
                });

                _logger.LogInfo($"Traitement des fiches modifications numérisées terminé. {codesBE.Count} fiches trouvées.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors du traitement des fiches modifications numérisées: {ex.Message}");
                throw;
            }
        }

        private void ProcessDepNomenclature()
        {
            _logger.LogInfo("Traitement des nomenclatures");

            try
            {
                List<string> codesBE = new List<string>();

                // Récupérer les répertoires de niveau 1
                List<string> dossiers = _fileHelper.GetDossiers(Che04, "[A-Za-z]{4}");

                foreach (string dossier in dossiers)
                {
                    string cheminDossier = Path.Combine(Che04, dossier);
                    List<string> fichiers = _fileHelper.GetFichiers(cheminDossier, "*.xls");

                    foreach (string fichier in fichiers)
                    {
                        string codeBe = Path.GetFileNameWithoutExtension(fichier);
                        codesBE.Add(codeBe);
                    }
                }

                // Mettre à jour la table temporaire et le compteur
                _sqlHelper.ExecuteWithTransaction((connection, transaction) =>
                {
                    // Utiliser la table temporaire
                    string tempTable = DependanceTypes.GetTempTableName(DependanceTypes.Nomenclature);

                    // Créer et remplir la table temporaire
                    _sqlHelper.UploadToTempTable(tempTable, codesBE, connection, transaction);

                    // Mettre à jour le compteur
                    using (SqlCommand cmd = new SqlCommand(
                        $"UPDATE Compteurs SET ValCtr = {codesBE.Count} WHERE CodCtr = @CodCtr",
                        connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@CodCtr", DependanceTypes.GetCodCtr(DependanceTypes.Nomenclature));
                        cmd.ExecuteNonQuery();
                    }
                });

                _logger.LogInfo($"Traitement des nomenclatures terminé. {codesBE.Count} nomenclatures trouvées.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors du traitement des nomenclatures: {ex.Message}");
                throw;
            }
        }

        private void ProcessDepPlansEtDocumentsNumerises()
        {
            _logger.LogInfo("Traitement des plans et documents numérisés");

            try
            {
                List<string> codesBE = new List<string>();

                // Récupérer les répertoires de niveau 1
                List<string> dossiers = _fileHelper.GetDossiers(Che05, "[A-Za-z]{4}");

                foreach (string dossier in dossiers)
                {
                    string cheminDossier = Path.Combine(Che05, dossier);
                    List<string> fichiers = _fileHelper.GetFichiers(cheminDossier, "*.tif");

                    foreach (string fichier in fichiers)
                    {
                        string codeBe = Path.GetFileNameWithoutExtension(fichier);
                        codesBE.Add(codeBe);
                    }
                }

                // Mettre à jour la table temporaire et le compteur
                _sqlHelper.ExecuteWithTransaction((connection, transaction) =>
                {
                    // Utiliser la table temporaire
                    string tempTable = DependanceTypes.GetTempTableName(DependanceTypes.PlansEtDocumentsNumerises);

                    // Créer et remplir la table temporaire
                    _sqlHelper.UploadToTempTable(tempTable, codesBE, connection, transaction);

                    // Mettre à jour le compteur
                    using (SqlCommand cmd = new SqlCommand(
                        $"UPDATE Compteurs SET ValCtr = {codesBE.Count} WHERE CodCtr = @CodCtr",
                        connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@CodCtr", DependanceTypes.GetCodCtr(DependanceTypes.PlansEtDocumentsNumerises));
                        cmd.ExecuteNonQuery();
                    }
                });

                _logger.LogInfo($"Traitement des plans et documents numérisés terminé. {codesBE.Count} plans/documents trouvés.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors du traitement des plans et documents numérisés: {ex.Message}");
                throw;
            }
        }

        private void ProcessDepFichiersMica()
        {
            _logger.LogInfo("Traitement des fichiers mica");

            try
            {
                List<string> codesBE = new List<string>();

                // Récupérer les répertoires de niveau 1 puis les sous-répertoires
                List<string> dossiers = _fileHelper.GetDossiers(Che08, "[A-Za-z]{4}");
                List<string> sousDossiers = new List<string>();

                foreach (string dossier in dossiers)
                {
                    string cheminDossier = Path.Combine(Che08, dossier);
                    List<string> dirs = _fileHelper.GetDossiers(cheminDossier, ".*");

                    foreach (string dir in dirs)
                    {
                        sousDossiers.Add(Path.Combine(dossier, dir));
                    }
                }

                // Parcourir tous les sous-dossiers pour trouver les fichiers .dxf
                foreach (string sousDossier in sousDossiers)
                {
                    string cheminComplet = Path.Combine(Che08, sousDossier);
                    List<string> fichiers = _fileHelper.GetFichiers(cheminComplet, "*.dxf");

                    foreach (string fichier in fichiers)
                    {
                        string nomFichier = Path.GetFileNameWithoutExtension(fichier);

                        // Format du nom de fichier: XXXX_XXX_X.dxf pour XXXX.XXX
                        string[] parts = nomFichier.Split('_');
                        if (parts.Length >= 2)
                        {
                            string codeBe = parts[0] + "." + parts[1];
                            if (!codesBE.Contains(codeBe))
                            {
                                codesBE.Add(codeBe);
                            }
                        }
                    }
                }

                // Mettre à jour la table temporaire et le compteur
                _sqlHelper.ExecuteWithTransaction((connection, transaction) =>
                {
                    // Utiliser la table temporaire
                    string tempTable = DependanceTypes.GetTempTableName(DependanceTypes.FichiersMica);

                    // Créer et remplir la table temporaire
                    _sqlHelper.UploadToTempTable(tempTable, codesBE, connection, transaction);

                    // Mettre à jour le compteur
                    using (SqlCommand cmd = new SqlCommand(
                        $"UPDATE Compteurs SET ValCtr = {codesBE.Count} WHERE CodCtr = @CodCtr",
                        connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@CodCtr", DependanceTypes.GetCodCtr(DependanceTypes.FichiersMica));
                        cmd.ExecuteNonQuery();
                    }
                });

                _logger.LogInfo($"Traitement des fichiers mica terminé. {codesBE.Count} fichiers trouvés.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors du traitement des fichiers mica: {ex.Message}");
                throw;
            }
        }

        private void ProcessDepFichiersTolerie()
        {
            _logger.LogInfo("Traitement des fichiers tôlerie");

            try
            {
                List<string> codesBE = new List<string>();

                // Structure spécifique : Che09\XXXX\YYYY\*.jgf
                List<string> dossiers = _fileHelper.GetDossiers(Che09, "[A-Za-z0-9]{4}");

                foreach (string dossier in dossiers)
                {
                    string cheminDossier = Path.Combine(Che09, dossier);
                    List<string> sousDossiers = _fileHelper.GetDossiers(cheminDossier, "[A-Za-z0-9]{4}");

                    foreach (string sousDossier in sousDossiers)
                    {
                        string cheminSousDossier = Path.Combine(cheminDossier, sousDossier);
                        List<string> fichiers = _fileHelper.GetFichiers(cheminSousDossier, "*.jgf");

                        foreach (string fichier in fichiers)
                        {
                            string nomFichier = Path.GetFileNameWithoutExtension(fichier);

                            // Format du nom de fichier: XXXX_XXX_X.jgf pour XXXX.XXX
                            string[] parts = nomFichier.Split('_');
                            if (parts.Length >= 2)
                            {
                                string codeBe = parts[0] + "." + parts[1];
                                if (!codesBE.Contains(codeBe))
                                {
                                    codesBE.Add(codeBe);
                                }
                            }
                        }
                    }
                }

                // Mettre à jour la table temporaire et le compteur
                _sqlHelper.ExecuteWithTransaction((connection, transaction) =>
                {
                    // Utiliser la table temporaire
                    string tempTable = DependanceTypes.GetTempTableName(DependanceTypes.FichiersTolerie);

                    // Créer et remplir la table temporaire
                    _sqlHelper.UploadToTempTable(tempTable, codesBE, connection, transaction);

                    // Mettre à jour le compteur
                    using (SqlCommand cmd = new SqlCommand(
                        $"UPDATE Compteurs SET ValCtr = {codesBE.Count} WHERE CodCtr = @CodCtr",
                        connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@CodCtr", DependanceTypes.GetCodCtr(DependanceTypes.FichiersTolerie));
                        cmd.ExecuteNonQuery();
                    }
                });

                _logger.LogInfo($"Traitement des fichiers tôlerie terminé. {codesBE.Count} fichiers trouvés.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors du traitement des fichiers tôlerie: {ex.Message}");
                throw;
            }
        }

        private void ProcessDepDocumentationMethodesNumerisees()
        {
            _logger.LogInfo("Traitement des documentations méthodes numérisées");

            try
            {
                List<string> codesBE = new List<string>();

                // Récupérer les répertoires de niveau 1
                List<string> dossiers = _fileHelper.GetDossiers(Che14, "[A-Za-z]{4}");

                foreach (string dossier in dossiers)
                {
                    string cheminDossier = Path.Combine(Che14, dossier);
                    List<string> fichiers = _fileHelper.GetFichiers(cheminDossier, "*.tif");

                    foreach (string fichier in fichiers)
                    {
                        string codeBe = Path.GetFileNameWithoutExtension(fichier);
                        codesBE.Add(codeBe);
                    }
                }

                // Mettre à jour la table temporaire et le compteur
                _sqlHelper.ExecuteWithTransaction((connection, transaction) =>
                {
                    // Utiliser la table temporaire
                    string tempTable = DependanceTypes.GetTempTableName(DependanceTypes.DocumentationMethodesNumerisees);

                    // Créer et remplir la table temporaire
                    _sqlHelper.UploadToTempTable(tempTable, codesBE, connection, transaction);

                    // Mettre à jour le compteur
                    using (SqlCommand cmd = new SqlCommand(
                        $"UPDATE Compteurs SET ValCtr = {codesBE.Count} WHERE CodCtr = @CodCtr",
                        connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@CodCtr", DependanceTypes.GetCodCtr(DependanceTypes.DocumentationMethodesNumerisees));
                        cmd.ExecuteNonQuery();
                    }
                });

                _logger.LogInfo($"Traitement des documentations méthodes numérisées terminé. {codesBE.Count} documentations trouvées.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors du traitement des documentations méthodes numérisées: {ex.Message}");
                throw;
            }
        }

        private void ProcessDepMarquage()
        {
            _logger.LogInfo("Traitement des marquages");

            try
            {
                List<string> codesBE = new List<string>();

                // Pour les marquages, les codes BE sont directement présents comme dossiers
                if (Directory.Exists(Che15))
                {
                    string[] entries = Directory.GetDirectories(Che15);

                    foreach (string entry in entries)
                    {
                        string codeBe = Path.GetFileName(entry);
                        if (!string.IsNullOrEmpty(codeBe) && codeBe.Length >= 4)
                        {
                            codesBE.Add(codeBe);
                        }
                    }
                }

                // Mettre à jour la table temporaire et le compteur
                _sqlHelper.ExecuteWithTransaction((connection, transaction) =>
                {
                    // Utiliser la table temporaire
                    string tempTable = DependanceTypes.GetTempTableName(DependanceTypes.Marquages);

                    // Créer et remplir la table temporaire
                    _sqlHelper.UploadToTempTable(tempTable, codesBE, connection, transaction);

                    // Mettre à jour le compteur
                    using (SqlCommand cmd = new SqlCommand(
                        $"UPDATE Compteurs SET ValCtr = {codesBE.Count} WHERE CodCtr = @CodCtr",
                        connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@CodCtr", DependanceTypes.GetCodCtr(DependanceTypes.Marquages));
                        cmd.ExecuteNonQuery();
                    }
                });

                _logger.LogInfo($"Traitement des marquages terminé. {codesBE.Count} marquages trouvés.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors du traitement des marquages: {ex.Message}");
                throw;
            }
        }

        private void ProcessDepPhotos()
        {
            _logger.LogInfo("Traitement des photos");

            try
            {
                List<string> codesBE = new List<string>();

                // Récupérer les répertoires de niveau 1
                List<string> dossiers = _fileHelper.GetDossiers(Che16, "[A-Za-z]{4}");

                foreach (string dossier in dossiers)
                {
                    string cheminDossier = Path.Combine(Che16, dossier);
                    List<string> fichiers = _fileHelper.GetFichiers(cheminDossier, "*.jpg");

                    foreach (string fichier in fichiers)
                    {
                        string nomFichier = Path.GetFileNameWithoutExtension(fichier);

                        // Les photos peuvent avoir des formats variés, on prend juste les 8 premiers caractères
                        if (nomFichier.Length >= 8)
                        {
                            string codeBe = nomFichier.Substring(0, 8);
                            if (codeBe.Contains("."))
                            {
                                codesBE.Add(codeBe);
                            }
                            else if (codeBe.Length == 8 && !string.IsNullOrEmpty(codeBe))
                            {
                                // Format XXXXXXXX à convertir en XXXX.XXX
                                codeBe = codeBe.Substring(0, 4) + "." + codeBe.Substring(4, 3);
                                codesBE.Add(codeBe);
                            }
                        }
                    }
                }

                // Mettre à jour la table temporaire et le compteur
                _sqlHelper.ExecuteWithTransaction((connection, transaction) =>
                {
                    // Utiliser la table temporaire
                    string tempTable = DependanceTypes.GetTempTableName(DependanceTypes.Photos);

                    // Créer et remplir la table temporaire
                    _sqlHelper.UploadToTempTable(tempTable, codesBE, connection, transaction);

                    // Mettre à jour le compteur
                    using (SqlCommand cmd = new SqlCommand(
                        $"UPDATE Compteurs SET ValCtr = {codesBE.Count} WHERE CodCtr = @CodCtr",
                        connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@CodCtr", DependanceTypes.GetCodCtr(DependanceTypes.Photos));
                        cmd.ExecuteNonQuery();
                    }
                });

                _logger.LogInfo($"Traitement des photos terminé. {codesBE.Count} photos trouvées.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors du traitement des photos: {ex.Message}");
                throw;
            }
        }

        // Méthodes pour les dépendances qui ne nécessitent pas d'accès aux fichiers mais aux données SQL

        private void ProcessDepAmeliorationsPermanentesFaites()
        {
            _logger.LogInfo("Traitement des améliorations permanentes faites");

            try
            {
                // Cette dépendance est traitée directement via SQL
                _sqlHelper.ExecuteWithTransaction((connection, transaction) =>
                {
                    // Créer la table temporaire si nécessaire
                    string tempTable = DependanceTypes.GetTempTableName(DependanceTypes.AmeliorationsPermanentesFaites);
                    string createTableSql = $@"
                IF OBJECT_ID('tempdb..{tempTable}') IS NULL
                BEGIN
                    CREATE TABLE {tempTable} (CodeBe VARCHAR(50));
                END
                ELSE
                BEGIN
                    TRUNCATE TABLE {tempTable};
                END";

                    using (SqlCommand cmd = new SqlCommand(createTableSql, connection, transaction))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Insérer les données depuis BloDemande
                    string insertSql = $@"
                INSERT INTO {tempTable} (CodeBe)
                SELECT CodeBE FROM BloDemande 
                WHERE DateModification IS NOT NULL";

                    using (SqlCommand cmd = new SqlCommand(insertSql, connection, transaction))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Compter et mettre à jour le compteur
                    string countSql = $"SELECT COUNT(*) FROM {tempTable}";
                    int count = 0;

                    using (SqlCommand cmd = new SqlCommand(countSql, connection, transaction))
                    {
                        object result = cmd.ExecuteScalar();
                        if (result != DBNull.Value && result != null)
                        {
                            count = Convert.ToInt32(result);
                        }
                    }

                    // Mettre à jour le compteur
                    using (SqlCommand cmd = new SqlCommand(
                        "UPDATE Compteurs SET ValCtr = @Count WHERE CodCtr = @CodCtr",
                        connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@Count", count);
                        cmd.Parameters.AddWithValue("@CodCtr", DependanceTypes.GetCodCtr(DependanceTypes.AmeliorationsPermanentesFaites));
                        cmd.ExecuteNonQuery();
                    }
                });

                _logger.LogInfo("Traitement des améliorations permanentes faites terminé.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors du traitement des améliorations permanentes faites: {ex.Message}");
                throw;
            }
        }

        private void ProcessDepAmeliorationsPermanentesNonFaites()
        {
            _logger.LogInfo("Traitement des améliorations permanentes non faites");

            try
            {
                // Cette dépendance est traitée directement via SQL
                _sqlHelper.ExecuteWithTransaction((connection, transaction) =>
                {
                    // Créer la table temporaire si nécessaire
                    string tempTable = DependanceTypes.GetTempTableName(DependanceTypes.AmeliorationsPermanentesNonFaites);
                    string createTableSql = $@"
                IF OBJECT_ID('tempdb..{tempTable}') IS NULL
                BEGIN
                    CREATE TABLE {tempTable} (CodeBe VARCHAR(50));
                END
                ELSE
                BEGIN
                    TRUNCATE TABLE {tempTable};
                END";

                    using (SqlCommand cmd = new SqlCommand(createTableSql, connection, transaction))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Insérer les données depuis BloDemande
                    string insertSql = $@"
                INSERT INTO {tempTable} (CodeBe)
                SELECT CodeBE FROM BloDemande 
                WHERE DateModification IS NULL";

                    using (SqlCommand cmd = new SqlCommand(insertSql, connection, transaction))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Compter et mettre à jour le compteur
                    string countSql = $"SELECT COUNT(*) FROM {tempTable}";
                    int count = 0;

                    using (SqlCommand cmd = new SqlCommand(countSql, connection, transaction))
                    {
                        object result = cmd.ExecuteScalar();
                        if (result != DBNull.Value && result != null)
                        {
                            count = Convert.ToInt32(result);
                        }
                    }

                    // Mettre à jour le compteur
                    using (SqlCommand cmd = new SqlCommand(
                        "UPDATE Compteurs SET ValCtr = @Count WHERE CodCtr = @CodCtr",
                        connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@Count", count);
                        cmd.Parameters.AddWithValue("@CodCtr", DependanceTypes.GetCodCtr(DependanceTypes.AmeliorationsPermanentesNonFaites));
                        cmd.ExecuteNonQuery();
                    }
                });

                _logger.LogInfo("Traitement des améliorations permanentes non faites terminé.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors du traitement des améliorations permanentes non faites: {ex.Message}");
                throw;
            }
        }

        private void ProcessDepPlansAcim()
        {
            _logger.LogInfo("Traitement des plans Acim");

            try
            {
                // Cette dépendance est traitée directement via SQL
                _sqlHelper.ExecuteWithTransaction((connection, transaction) =>
                {
                    // Créer la table temporaire si nécessaire
                    string tempTable = DependanceTypes.GetTempTableName(DependanceTypes.PlansAcim);
                    string createTableSql = $@"
                IF OBJECT_ID('tempdb..{tempTable}') IS NULL
                BEGIN
                    CREATE TABLE {tempTable} (CodeBe VARCHAR(50));
                END
                ELSE
                BEGIN
                    TRUNCATE TABLE {tempTable};
                END";

                    using (SqlCommand cmd = new SqlCommand(createTableSql, connection, transaction))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Insérer les données depuis ListePlan
                    string insertSql = $@"
                INSERT INTO {tempTable} (CodeBe)
                SELECT CodeArticle FROM ListePlan";

                    using (SqlCommand cmd = new SqlCommand(insertSql, connection, transaction))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Compter et mettre à jour le compteur
                    string countSql = $"SELECT COUNT(*) FROM {tempTable}";
                    int count = 0;

                    using (SqlCommand cmd = new SqlCommand(countSql, connection, transaction))
                    {
                        object result = cmd.ExecuteScalar();
                        if (result != DBNull.Value && result != null)
                        {
                            count = Convert.ToInt32(result);
                        }
                    }

                    // Mettre à jour le compteur
                    using (SqlCommand cmd = new SqlCommand(
                        "UPDATE Compteurs SET ValCtr = @Count WHERE CodCtr = @CodCtr",
                        connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@Count", count);
                        cmd.Parameters.AddWithValue("@CodCtr", DependanceTypes.GetCodCtr(DependanceTypes.PlansAcim));
                        cmd.ExecuteNonQuery();
                    }
                });

                _logger.LogInfo("Traitement des plans Acim terminé.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors du traitement des plans Acim: {ex.Message}");
                throw;
            }
        }

        private void ProcessDepPlansNonNumerisesDeclares()
        {
            _logger.LogInfo("Traitement des plans non numérisés déclarés");

            try
            {
                // Cette dépendance est traitée directement via SQL
                _sqlHelper.ExecuteWithTransaction((connection, transaction) =>
                {
                    // Créer la table temporaire si nécessaire
                    string tempTable = DependanceTypes.GetTempTableName(DependanceTypes.PlanNonNumerisesDeclare);
                    string createTableSql = $@"
                IF OBJECT_ID('tempdb..{tempTable}') IS NULL
                BEGIN
                    CREATE TABLE {tempTable} (CodeBe VARCHAR(50));
                END
                ELSE
                BEGIN
                    TRUNCATE TABLE {tempTable};
                END";

                    using (SqlCommand cmd = new SqlCommand(createTableSql, connection, transaction))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Insérer les données depuis Plans
                    string insertSql = $@"
                INSERT INTO {tempTable} (CodeBe)
                SELECT CodeBE FROM Plans";

                    using (SqlCommand cmd = new SqlCommand(insertSql, connection, transaction))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Compter et mettre à jour le compteur
                    string countSql = $"SELECT COUNT(*) FROM {tempTable}";
                    int count = 0;

                    using (SqlCommand cmd = new SqlCommand(countSql, connection, transaction))
                    {
                        object result = cmd.ExecuteScalar();
                        if (result != DBNull.Value && result != null)
                        {
                            count = Convert.ToInt32(result);
                        }
                    }

                    // Mettre à jour le compteur
                    using (SqlCommand cmd = new SqlCommand(
                        "UPDATE Compteurs SET ValCtr = @Count WHERE CodCtr = @CodCtr",
                        connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@Count", count);
                        cmd.Parameters.AddWithValue("@CodCtr", DependanceTypes.GetCodCtr(DependanceTypes.PlanNonNumerisesDeclare));
                        cmd.ExecuteNonQuery();
                    }
                });

                _logger.LogInfo("Traitement des plans non numérisés déclarés terminé.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors du traitement des plans non numérisés déclarés: {ex.Message}");
                throw;
            }
        }

        private void ProcessDepModificationsFicheMereFaites()
        {
            _logger.LogInfo("Traitement des modifications de fiche mère faites");

            try
            {
                // Cette dépendance est traitée directement via SQL
                _sqlHelper.ExecuteWithTransaction((connection, transaction) =>
                {
                // Créer la table temporaire si nécessaire
                string tempTable = DependanceTypes.GetTempTableName(DependanceTypes.ModificationsFicheMereFaites);
                string createTableSql = $@"
                IF OBJECT_ID('tempdb..{tempTable}') IS NULL
                BEGIN
                    CREATE TABLE {tempTable} (CodeBe VARCHAR(50));
                END
                ELSE
                BEGIN
                    TRUNCATE TABLE {tempTable};
                END";

                using (SqlCommand cmd = new SqlCommand(createTableSql, connection, transaction))
                {
                    cmd.ExecuteNonQuery();
                }

                // Insérer les données depuis BloModificationsFM
                string insertSql = $@"
                INSERT INTO {tempTable} (CodeBe)
                SELECT CodeBE FROM BloModificationsFM
                WHERE DateRealisation IS NOT NULL
                AND (ModifTole = 0 OR (ModifTole = 1 AND DateModifTole IS NOT NULL))
                AND (ModifCodeBE = 0 OR (ModifCodeBE = 1 AND DateModifCodeBE IS NOT NULL))";

                using (SqlCommand cmd = new SqlCommand(insertSql, connection, transaction))
                {
                    cmd.ExecuteNonQuery();
                }

                // Compter et mettre à jour le compteur
                string countSql = $"SELECT COUNT(*) FROM {tempTable}";
                int count = 0;

                using (SqlCommand cmd = new SqlCommand(countSql, connection, transaction))
                {
                    object result = cmd.ExecuteScalar();
                        if (result != DBNull.Value && result != null)
                        {
                            count = Convert.ToInt32(result);
                        }
                    }

                    // Mettre à jour le compteur
                    using (SqlCommand cmd = new SqlCommand(
                        "UPDATE Compteurs SET ValCtr = @Count WHERE CodCtr = @CodCtr",
                        connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@Count", count);
                        cmd.Parameters.AddWithValue("@CodCtr", DependanceTypes.GetCodCtr(DependanceTypes.ModificationsFicheMereFaites));
                        cmd.ExecuteNonQuery();
                    }
                });

                _logger.LogInfo("Traitement des modifications de fiche mère faites terminé.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors du traitement des modifications de fiche mère faites: {ex.Message}");
                throw;
            }
        }

        private void ProcessDepModificationsFicheMereEnCours()
        {
            _logger.LogInfo("Traitement des modifications de fiche mère en cours");

            try
            {
                // Cette dépendance est traitée directement via SQL
                _sqlHelper.ExecuteWithTransaction((connection, transaction) =>
                {
                    // Créer la table temporaire si nécessaire
                    string tempTable = DependanceTypes.GetTempTableName(DependanceTypes.ModificationsFicheMereEnCours);
                    string createTableSql = $@"
               IF OBJECT_ID('tempdb..{tempTable}') IS NULL
               BEGIN
                   CREATE TABLE {tempTable} (CodeBe VARCHAR(50));
               END
               ELSE
               BEGIN
                   TRUNCATE TABLE {tempTable};
               END";

                    using (SqlCommand cmd = new SqlCommand(createTableSql, connection, transaction))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Insérer les données depuis BloModificationsFM
                    string insertSql = $@"
               INSERT INTO {tempTable} (CodeBe)
               SELECT CodeBE FROM BloModificationsFM
               WHERE DateRealisation IS NULL
               OR (ModifTole = 1 AND DateModifTole IS NULL)
               OR (ModifCodeBE = 1 AND DateModifCodeBE IS NULL)";

                    using (SqlCommand cmd = new SqlCommand(insertSql, connection, transaction))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Compter et mettre à jour le compteur
                    string countSql = $"SELECT COUNT(*) FROM {tempTable}";
                    int count = 0;

                    using (SqlCommand cmd = new SqlCommand(countSql, connection, transaction))
                    {
                        object result = cmd.ExecuteScalar();
                        if (result != DBNull.Value && result != null)
                        {
                            count = Convert.ToInt32(result);
                        }
                    }

                    // Mettre à jour le compteur
                    using (SqlCommand cmd = new SqlCommand(
                        "UPDATE Compteurs SET ValCtr = @Count WHERE CodCtr = @CodCtr",
                        connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@Count", count);
                        cmd.Parameters.AddWithValue("@CodCtr", DependanceTypes.GetCodCtr(DependanceTypes.ModificationsFicheMereEnCours));
                        cmd.ExecuteNonQuery();
                    }
                });

                _logger.LogInfo("Traitement des modifications de fiche mère en cours terminé.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors du traitement des modifications de fiche mère en cours: {ex.Message}");
                throw;
            }
        }

        private void ProcessDepRetoursNC()
        {
            _logger.LogInfo("Traitement des retours NC");

            try
            {
                // Cette dépendance est traitée directement via SQL
                _sqlHelper.ExecuteWithTransaction((connection, transaction) =>
                {
                    // Créer la table temporaire si nécessaire
                    string tempTable = DependanceTypes.GetTempTableName(DependanceTypes.RetoursNC);
                    string createTableSql = $@"
               IF OBJECT_ID('tempdb..{tempTable}') IS NULL
               BEGIN
                   CREATE TABLE {tempTable} (CodeBe VARCHAR(50));
               END
               ELSE
               BEGIN
                   TRUNCATE TABLE {tempTable};
               END";

                    using (SqlCommand cmd = new SqlCommand(createTableSql, connection, transaction))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Paramètres pour la requête
                    string wtype = "NC";
                    string wlht = "";

                    // Insérer les données depuis AC_F_RETOUR_AJ
                    string insertSql = $@"
               INSERT INTO {tempTable} (CodeBe)
               SELECT REFERENCE FROM AC_F_RETOUR_AJ
               WHERE TYPE = @Type AND LHT <> @Lht";

                    using (SqlCommand cmd = new SqlCommand(insertSql, connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@Type", wtype);
                        cmd.Parameters.AddWithValue("@Lht", wlht);
                        cmd.ExecuteNonQuery();
                    }

                    // Compter et mettre à jour le compteur
                    string countSql = $"SELECT COUNT(*) FROM {tempTable}";
                    int count = 0;

                    using (SqlCommand cmd = new SqlCommand(countSql, connection, transaction))
                    {
                        object result = cmd.ExecuteScalar();
                        if (result != DBNull.Value && result != null)
                        {
                            count = Convert.ToInt32(result);
                        }
                    }

                    // Mettre à jour le compteur
                    using (SqlCommand cmd = new SqlCommand(
                        "UPDATE Compteurs SET ValCtr = @Count WHERE CodCtr = @CodCtr",
                        connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@Count", count);
                        cmd.Parameters.AddWithValue("@CodCtr", DependanceTypes.GetCodCtr(DependanceTypes.RetoursNC));
                        cmd.ExecuteNonQuery();
                    }
                });

                _logger.LogInfo("Traitement des retours NC terminé.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors du traitement des retours NC: {ex.Message}");
                throw;
            }
        }

        // Méthode pour traiter les fiches filles
        private void ProcessDepFichesFilles()
        {
            _logger.LogInfo("Traitement des fiches filles");

            try
            {
                // Cette dépendance est traitée directement via SQL et est liée à la Dep01
                _sqlHelper.ExecuteWithTransaction((connection, transaction) =>
                {
                    // Compter les fiches filles
                    string countSql = "SELECT COUNT(*) FROM Filles";
                    int count = 0;

                    using (SqlCommand cmd = new SqlCommand(countSql, connection, transaction))
                    {
                        object result = cmd.ExecuteScalar();
                        if (result != DBNull.Value && result != null)
                        {
                            count = Convert.ToInt32(result);
                        }
                    }

                    // Mettre à jour le compteur des fiches filles (le code VBA utilise "011")
                    using (SqlCommand cmd = new SqlCommand(
                        "UPDATE Compteurs SET ValCtr = @Count WHERE CodCtr = '011'",
                        connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@Count", count);
                        cmd.ExecuteNonQuery();
                    }

                    // Mettre à jour directement la table Dependances pour les fiches filles
                    string updateSql = @"
               UPDATE d
               SET d.Dep01 = 1
               FROM Dependances d
               INNER JOIN Filles f ON d.CodeBE = f.Fille";

                    using (SqlCommand cmd = new SqlCommand(updateSql, connection, transaction))
                    {
                        cmd.ExecuteNonQuery();
                    }
                });

                _logger.LogInfo("Traitement des fiches filles terminé.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors du traitement des fiches filles: {ex.Message}");
                throw;
            }
        }

        // Cette méthode doit être appelée avant ProcessDepPlansNonNumerisesDeclares pour mettre à jour la table temporaire
        private void ProcessDepComptagePlansNonNumerises()
        {
            _logger.LogInfo("Comptage des plans non numérisés");

            try
            {
                // Cette dépendance est traitée directement via SQL
                _sqlHelper.ExecuteWithTransaction((connection, transaction) =>
                {
                    // Compter les plans non numérisés
                    string countSql = "SELECT COUNT(*) FROM Plans";
                    int count = 0;

                    using (SqlCommand cmd = new SqlCommand(countSql, connection, transaction))
                    {
                        object result = cmd.ExecuteScalar();
                        if (result != DBNull.Value && result != null)
                        {
                            count = Convert.ToInt32(result);
                        }
                    }

                    // Mettre à jour le compteur (le code VBA utilise "110")
                    using (SqlCommand cmd = new SqlCommand(
                        "UPDATE Compteurs SET ValCtr = @Count WHERE CodCtr = '110'",
                        connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@Count", count);
                        cmd.ExecuteNonQuery();
                    }
                });

                _logger.LogInfo("Comptage des plans non numérisés terminé.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors du comptage des plans non numérisés: {ex.Message}");
                throw;
            }
        }

        // Étape finale pour mettre à jour toutes les dépendances basées sur les tables temporaires
        private void FinalizeDepUpdates()
        {
            _logger.LogInfo("Finalisation des mises à jour des dépendances");

            try
            {
                // Exécuter les requêtes de mise à jour des dépendances
                string[] updateQueries = {
           // Dep01 (déjà fait lors du processus précédent)
           "UPDATE d SET d.Dep01 = 1 FROM Dependances d INNER JOIN #Dep01FichesMères fm ON d.CodeBE = fm.CodeBe",
           "UPDATE d SET d.Dep01 = 1 FROM Dependances d INNER JOIN Filles f ON d.CodeBE = f.Fille",
           
           // Dep02-Dep17
           "UPDATE d SET d.Dep02 = 1 FROM Dependances d INNER JOIN #Dep02DocumentationsMéthode dm ON d.CodeBE = dm.CodeBe",
           "UPDATE d SET d.Dep03 = 1 FROM Dependances d INNER JOIN #Dep03FichesModificationsNumérisées fm ON d.CodeBE = fm.CodeBe",
           "UPDATE d SET d.Dep04 = 1 FROM Dependances d INNER JOIN #Dep04Nomenclatures n ON d.CodeBE = n.CodeBe",
           "UPDATE d SET d.Dep05 = 1 FROM Dependances d INNER JOIN #Dep05PlansEtDocuments pd ON d.CodeBE = pd.CodeBe",
           "UPDATE d SET d.Dep06 = 1 FROM Dependances d INNER JOIN #Dep06AméliorationsPermanenteDemandées ap ON d.CodeBE = ap.CodeBe",
           "UPDATE d SET d.Dep07 = 1 FROM Dependances d INNER JOIN #Dep07AméliorationsPermanenetesNonFaites apnf ON d.CodeBE = apnf.CodeBe",
           "UPDATE d SET d.Dep08 = 1 FROM Dependances d INNER JOIN #Dep08FichiersMica fm ON d.CodeBE = fm.CodeBe",
           "UPDATE d SET d.Dep09 = 1 FROM Dependances d INNER JOIN #Dep09FichiersTôle ft ON d.CodeBE = ft.CodeBe",
           "UPDATE d SET d.Dep10 = 1 FROM Dependances d INNER JOIN #Dep10PlansAcim pa ON d.CodeBE = pa.CodeBe",
           "UPDATE d SET d.Dep11 = 1 FROM Dependances d INNER JOIN Plans p ON d.CodeBE = p.CodeBE",
           "UPDATE d SET d.Dep12 = 1 FROM Dependances d INNER JOIN #Dep12ModificationFM mfm ON d.CodeBE = mfm.CodeBe",
           "UPDATE d SET d.Dep13 = 1 FROM Dependances d INNER JOIN #Dep13ModificationFMEnCours mfmc ON d.CodeBE = mfmc.CodeBe",
           "UPDATE d SET d.Dep14 = 1 FROM Dependances d INNER JOIN #Dep14DocumentationsMéthodesNumérisées dmn ON d.CodeBE = dmn.CodeBe",
           "UPDATE d SET d.Dep15 = 1 FROM Dependances d INNER JOIN #Dep15Marquage m ON d.CodeBE = m.CodeBe",
           "UPDATE d SET d.Dep16 = 1 FROM Dependances d INNER JOIN #Dep16Photos p ON d.CodeBE = p.CodeBe",
           "UPDATE d SET d.Dep17 = 1 FROM Dependances d INNER JOIN #Dep17RetoursNC rnc ON d.CodeBE = rnc.CodeBe"
       };

                // Exécuter chaque requête de mise à jour
                foreach (string query in updateQueries)
                {
                    _sqlHelper.ExecuteDirectSqlWithRetry(query);
                }

                _logger.LogInfo("Finalisation des mises à jour des dépendances terminée.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors de la finalisation des mises à jour des dépendances: {ex.Message}");
                throw;
            }
        }

        private void UpdateDatesScans()
        {
            _logger.LogInfo("Mise à jour des dates de scan des fiches mères");

            try
            {
                // Récupérer tous les codes BE
                DataTable dtCodesBE = _sqlHelper.ExecuteDataTable("SELECT code_ref FROM CodesBE");

                foreach (DataRow row in dtCodesBE.Rows)
                {
                    string codeBE = row["code_ref"].ToString();
                    string scanPath = _fileHelper.GetDatFMPath(Che01, codeBE);

                    if (_fileHelper.ExisteFichier(scanPath))
                    {
                        DateTime scanDate = _fileHelper.GetLastWriteTime(scanPath);

                        if (scanDate != DateTime.MinValue)
                        {
                            _sqlHelper.ExecuteNonQuery(
                                "UPDATE CodesBE SET scan_fm = @ScanDate WHERE code_ref = @CodeBE",
                                CommandType.Text,
                                new SqlParameter("@ScanDate", scanDate),
                                new SqlParameter("@CodeBE", codeBE));
                        }
                    }
                }

                _logger.LogInfo("Mise à jour des dates de scan terminée");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors de la mise à jour des dates de scan: {ex.Message}");
                throw;
            }
        }
    }
}