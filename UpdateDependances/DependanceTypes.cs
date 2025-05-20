using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpdateDependances
{
    public static class DependanceTypes
    {
        public const int FicheMereOuFille = 1;
        public const int DocumentationMethodeXls = 2;
        public const int FicheModificationNumerisee = 3;
        public const int Nomenclature = 4;
        public const int PlansEtDocumentsNumerises = 5;
        public const int AmeliorationsPermanentesFaites = 6;
        public const int AmeliorationsPermanentesNonFaites = 7;
        public const int FichiersMica = 8;
        public const int FichiersTolerie = 9;
        public const int PlansAcim = 10;
        public const int PlanNonNumerisesDeclare = 11;
        public const int ModificationsFicheMereFaites = 12;
        public const int ModificationsFicheMereEnCours = 13;
        public const int DocumentationMethodesNumerisees = 14;
        public const int Marquages = 15;
        public const int Photos = 16;
        public const int RetoursNC = 17;

        public static string GetCodCtr(int dependanceType)
        {
            switch (dependanceType)
            {
                case FicheMereOuFille: return "010";
                case DocumentationMethodeXls: return "020";
                case FicheModificationNumerisee: return "030";
                case Nomenclature: return "040";
                case PlansEtDocumentsNumerises: return "050";
                case AmeliorationsPermanentesFaites: return "060";
                case AmeliorationsPermanentesNonFaites: return "070";
                case FichiersMica: return "080";
                case FichiersTolerie: return "090";
                case PlansAcim: return "100";
                case PlanNonNumerisesDeclare: return "110";
                case ModificationsFicheMereFaites: return "120";
                case ModificationsFicheMereEnCours: return "130";
                case DocumentationMethodesNumerisees: return "140";
                case Marquages: return "150";
                case Photos: return "160";
                case RetoursNC: return "170";
                default: return "000";
            }
        }

        public static string GetTempTableName(int dependanceType)
        {
            switch (dependanceType)
            {
                case FicheMereOuFille: return "#Dep01FichesMères";
                case DocumentationMethodeXls: return "#Dep02DocumentationsMéthode";
                case FicheModificationNumerisee: return "#Dep03FichesModificationsNumérisées";
                case Nomenclature: return "#Dep04Nomenclatures";
                case PlansEtDocumentsNumerises: return "#Dep05PlansEtDocuments";
                case AmeliorationsPermanentesFaites: return "#Dep06AméliorationsPermanenteDemandées";
                case AmeliorationsPermanentesNonFaites: return "#Dep07AméliorationsPermanenetesNonFaites";
                case FichiersMica: return "#Dep08FichiersMica";
                case FichiersTolerie: return "#Dep09FichiersTôle";
                case PlansAcim: return "#Dep10PlansAcim";
                case PlanNonNumerisesDeclare: return "#Dep11PlansNonNumérisés";
                case ModificationsFicheMereFaites: return "#Dep12ModificationFM";
                case ModificationsFicheMereEnCours: return "#Dep13ModificationFMEnCours";
                case DocumentationMethodesNumerisees: return "#Dep14DocumentationsMéthodesNumérisées";
                case Marquages: return "#Dep15Marquage";
                case Photos: return "#Dep16Photos";
                case RetoursNC: return "#Dep17RetoursNC";
                default: return string.Empty;
            }
        }
        public static string GetDepDescription(int dependanceType)
        {
            switch (dependanceType)
            {
                case FicheMereOuFille: return "Fiche mère ou fille";
                case DocumentationMethodeXls: return "Documentation méthodes xls";
                case FicheModificationNumerisee: return "Fiche de modification numérisée";
                case Nomenclature: return "Nomenclature";
                case PlansEtDocumentsNumerises: return "Plans et documents numérisés";
                case AmeliorationsPermanentesFaites: return "Améliorations permanentes faites";
                case AmeliorationsPermanentesNonFaites: return "Améliorations permanentes non faites";
                case FichiersMica: return "Fichier(s) mica";
                case FichiersTolerie: return "Fichier(s) tôlerie";
                case PlansAcim: return "Plans Acim";
                case PlanNonNumerisesDeclare: return "Plan non numérisé déclaré";
                case ModificationsFicheMereFaites: return "Modifications de fiche mère faites";
                case ModificationsFicheMereEnCours: return "Modifications de fiche mère en cours";
                case DocumentationMethodesNumerisees: return "Documentation méthodes numérisées";
                case Marquages: return "Marquages";
                case Photos: return "Photos";
                case RetoursNC: return "Retours NC";
                default: return "Inconnu";
            }
        }

        public static string GetDepName(int dependanceType)
        {
            switch (dependanceType)
            {
                case FicheMereOuFille: return "Dep01";
                case DocumentationMethodeXls: return "Dep02";
                case FicheModificationNumerisee: return "Dep03";
                case Nomenclature: return "Dep04";
                case PlansEtDocumentsNumerises: return "Dep05";
                case AmeliorationsPermanentesFaites: return "Dep06";
                case AmeliorationsPermanentesNonFaites: return "Dep07";
                case FichiersMica: return "Dep08";
                case FichiersTolerie: return "Dep09";
                case PlansAcim: return "Dep10";
                case PlanNonNumerisesDeclare: return "Dep11";
                case ModificationsFicheMereFaites: return "Dep12";
                case ModificationsFicheMereEnCours: return "Dep13";
                case DocumentationMethodesNumerisees: return "Dep14";
                case Marquages: return "Dep15";
                case Photos: return "Dep16";
                case RetoursNC: return "Dep17";
                default: return string.Empty;
            }
        }

        public static string GetExtension(int dependanceType)
        {
            switch (dependanceType)
            {
                case FicheMereOuFille: return ".tif";
                case DocumentationMethodeXls: return ".xls";
                case FicheModificationNumerisee: return ".tif";
                case Nomenclature: return ".xls";
                case PlansEtDocumentsNumerises: return ".tif";
                case FichiersMica: return ".dxf";
                case FichiersTolerie: return ".jgf";
                case DocumentationMethodesNumerisees: return ".tif";
                case Photos: return ".jpg";
                default: return "*";
            }
        }
    }
}