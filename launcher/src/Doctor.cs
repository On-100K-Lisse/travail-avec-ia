// Licence MIT (voir LICENSE-MIT du dépôt travail-avec-ia). Code écrit par Claude (Anthropic) pour On-100-K-Lisse.
// DLSS5 Launcher — « Docteur payload » : vérifie le dossier payload\ (présence, SHA-256, copie incomplète)
// pour qu'un payload abîmé ou mélangé ne soit jamais pris pour un bug du launcher.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Dlss5Launcher
{
    // Fichiers connus, identifiés par leur SHA-256 (ceux du payload testé, plus d'autres versions déjà rencontrées).
    public static class KnownFiles
    {
        static readonly Dictionary<string, string> Names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "CDFA6C1EC784867182164C7836F324F087B5C9F374D0C9FE46F54999FEABF3ED", "DLL NR OpenNR 310.9.16 compat-fp16 (RTX 20/30/40)" },
            { "2BC19BC09A7B89DD44F73E2AC0108FCC586E2154BDD0830F242F4F040E28789F", "DLL NR 310.9.16 NVIDIA d'origine (RTX 50, AMD/Intel)" },
            { "CCF6C7B53E95F68E9B03FB91A9A32BEBA2476980BDECD0475C0F2FFDC7A04AF9", "DLL NR 310.9.16 patchée (ancienne)" },
            { "4B8D19BC3EFF58A084F5ECA7489C921501C203450169FB82FF4F649A4482BA05", "DLL NR du payload 1-Click" },
            { "8270B350CD82DE5CE89806872CDD6B6A9249B80836B91BBEB3573470744CC206", "DLL NR 310.8 (pack streamline)" },
            { "6EB209E764F39872625DEBD6ABAF45E2BB6322F6F270F781F70C059AE30B3927", "DLL NR 310.8.SF" },
            { "E16BCF15E16E13F527491CDF7845B2FE6521A738D8F7C9C721866A8496E1FC8E", "DLL NR 310.8 RTX 50 (guide F5)" },
            { "E67DEE209320CDAFE0E93E45675D7AA34323A53ACC57A72B2E40A181581C989A", "DLL NR 310.8 ShortFuse" },
            { "14017C79B30814E7EA3297291331A3A3AE5B2E6C4B9F4DD5AA7C4599BF061C2D", "OptiScaler F5 0.1.32" },
            { "FD35BC693BA4133FA10E9746CE84486D617BB2AC5AE76E096FBAB77B28C8BAA2", "OptiScaler F5 0.1.34" },
            { "42E76FB30E652F098556BDEF5B0165CB7C06F779AC23177B05DED2011D7F6655", "forwarder NR d'OptiScaler F5 0.1.34 (NVIDIA)" },
            { "FBFB6676B829DAD7E020FB830586A16AA0EC6ADD78016DB48EF12E2AE1803231", "OptiScaler 0.9.4 (1-Click)" },
            { "6198502F1B71D861964F41FC7FA8DE0268FBF994D439C9C6DC9005514B605A7F", "forwarder NR d'OptiScaler F5 0.1.32 (NVIDIA)" },
            { "30AEFC7AA19399C2905BFA64F69CCA7EB1574135425D905C0E2F052FC523099C", "forwarder vendor-neutral 0.1.32 (AMD/Intel)" },
            { "251659DD84A3E84DE67C886A4186E01F3ECA49B00641906FE38BB6B807E5D5B7", "libxess.dll d'OptiScaler F5 0.1.32" },
            { "EC5E0C65E075570C6EDE72618BB666D0BE0C2E10B2EA9762C0FE8CB8E375AB27", "libxess_fg.dll d'OptiScaler F5 0.1.32" },
            { "D0DCCCC74A43C44BA435B7A369B456E0970D8A4464E4BD683119B374F2C9FB46", "FSR upscaler DX12 d'OptiScaler F5 0.1.32" },
            { "02297BEEDD285E822D3A64F314CF00FAF378DCEC0EDC47FF0C4DD71B3A8C2F18", "FSR frame generation DX12 d'OptiScaler F5 0.1.32" },
            { "A1624CC4238FEF046F30C4D80CE3F47BE63FC5F5373F49E3EE9EDB9960F54C78", "FSR Vulkan d'OptiScaler F5 0.1.32" },
            { "07D286C306F8117321422AFFD9E6388C12D0FB4BE1C7FC689D9E899324FEEB24", "D3D12Core.dll d'OptiScaler F5 0.1.32" },
            { "0CEE63F9C9F13F3AC909C5B4903F4DBB4B719A7AB3B4F13B0DEAF83C814B94F7", "ReShade dxgi.dll (payload RenoDX)" },
            { "DA430E0A9C6EECEFA0D1B27D05E16C426FB5D04E808B194D914EAAC4B31BC0F8", "ReShade dxgi32.dll" },
            { "D5ADF82EB44B065F4C590AC91FE824BAB07AFEA0EB9F994BDE936710C8593952", "add-on RenoDX DLSS 5 (payload)" },
            { "066EEC8C797DF2D656F2AB2324278921B1DD6E9116C9945294F4A00F7FEC608A", "add-on DLSS5-Feeder (1-Click)" },
            { "C85F971CE023C9F3492FC7455F0B01A24BA18EA39636407A846902C4360B0B7E", "nvngx_dlss.dll du 1-Click / streamline" },
            { "9F6672E5E0170DC118A3188D21BDA187E1FC1AA3502895B21AB846D23165C11D", "sl.dlss_nr.dll du 1-Click" },
            { "0BEE429A8336B2FB9EEA7973AEB1FF73FAF07F77E734D5F574E2EE75F97C8A0D", "nvngx.dll pont de DLSS5-ReShade-AIO 2.2.2" },
        };
        public static string Identify(string sha) { string n; return sha != null && Names.TryGetValue(sha, out n) ? n : null; }
        public static bool IsKnown(string sha) { return Identify(sha) != null; }
    }

    public enum PState { Ok, AutreVersion, Inconnu, Abime, Manquant }

    public class PItem
    {
        public string Group, Rel, Label, Expected, Found, Detail;
        public PState State;
        public bool Needed;     // utilisé sur cette carte (ou pour une copie à un ami : tout)
        public bool Extra;      // version d'OptiScaler ajoutée à côté de la 0.1.32 (payload\optiscaler-f5-<version>)
        public string Folder;   // dossier de payload de cette version
        public bool Blocking { get { return State == PState.Manquant || State == PState.Abime; } }
        public string StateText
        {
            get
            {
                switch (State)
                {
                    case PState.Ok: return "OK";
                    case PState.AutreVersion: return "AUTRE VERSION";
                    case PState.Inconnu: return "VERSION INCONNUE";
                    case PState.Abime: return "ABÎMÉ";
                    default: return "MANQUANT";
                }
            }
        }
    }

    public static class PayloadDoctor
    {
        public static Func<string, string> ShaOf = Hash.ShaCached;   // remplaçable par les tests
        class Spec
        {
            public string Group, Rel, Label, Sha; public int MinFiles; public bool IsDir; public string Use; // Use : NR_OPEN, NR_ORIG, AMD, OPTI, NVIDIA
        }

        const string GNrOpen = "DLL NR — RTX 20/30/40", GNrOrig = "DLL NR — RTX 50 et AMD/Intel", GOpti = "OptiScaler F5 0.1.32",
                     GAmd = "Port vendor-neutral 0.1.32 (AMD/Intel)", GReno = "RenoDX / ReShade", GFeed = "Feeder ReShade (1-Click)";

        static readonly Spec[] Specs =
        {
            new Spec { Group = GNrOpen, Rel = @"nr\nvngx_dlssnr.dll", Label = "OpenNR 310.9.16 compat-fp16", Sha = "CDFA6C1EC784867182164C7836F324F087B5C9F374D0C9FE46F54999FEABF3ED", Use = "NR_OPEN" },
            new Spec { Group = GNrOrig, Rel = @"nr-origine\nvngx_dlssnr.dll", Label = "310.9.16 NVIDIA d'origine", Sha = "2BC19BC09A7B89DD44F73E2AC0108FCC586E2154BDD0830F242F4F040E28789F", Use = "NR_ORIG" },
            new Spec { Group = GOpti, Rel = @"optiscaler-f5\OptiScaler.dll", Label = "OptiScaler F5 0.1.32", Sha = "14017C79B30814E7EA3297291331A3A3AE5B2E6C4B9F4DD5AA7C4599BF061C2D", Use = "OPTI" },
            new Spec { Group = GOpti, Rel = @"optiscaler-f5\nvngx.dll_dlssnr.dll", Label = "forwarder NR (NVIDIA)", Sha = "6198502F1B71D861964F41FC7FA8DE0268FBF994D439C9C6DC9005514B605A7F", Use = "OPTI" },
            new Spec { Group = GOpti, Rel = @"optiscaler-f5\OptiScaler.ini", Label = "réglages de base (modèle)", Use = "OPTI" },
            new Spec { Group = GOpti, Rel = @"optiscaler-f5\OptiScaler", Label = "dossier des composants", IsDir = true, MinFiles = 20, Use = "OPTI" },
            new Spec { Group = GOpti, Rel = @"optiscaler-f5\OptiScaler\libxess.dll", Label = "XeSS", Sha = "251659DD84A3E84DE67C886A4186E01F3ECA49B00641906FE38BB6B807E5D5B7", Use = "OPTI" },
            new Spec { Group = GOpti, Rel = @"optiscaler-f5\OptiScaler\libxess_fg.dll", Label = "XeSS génération d'images", Sha = "EC5E0C65E075570C6EDE72618BB666D0BE0C2E10B2EA9762C0FE8CB8E375AB27", Use = "OPTI" },
            new Spec { Group = GOpti, Rel = @"optiscaler-f5\OptiScaler\amd_fidelityfx_upscaler_dx12.dll", Label = "FSR upscaler DX12", Sha = "D0DCCCC74A43C44BA435B7A369B456E0970D8A4464E4BD683119B374F2C9FB46", Use = "OPTI" },
            new Spec { Group = GOpti, Rel = @"optiscaler-f5\OptiScaler\amd_fidelityfx_framegeneration_dx12.dll", Label = "FSR génération d'images DX12", Sha = "02297BEEDD285E822D3A64F314CF00FAF378DCEC0EDC47FF0C4DD71B3A8C2F18", Use = "OPTI" },
            new Spec { Group = GOpti, Rel = @"optiscaler-f5\OptiScaler\amd_fidelityfx_vk.dll", Label = "FSR Vulkan", Sha = "A1624CC4238FEF046F30C4D80CE3F47BE63FC5F5373F49E3EE9EDB9960F54C78", Use = "OPTI" },
            new Spec { Group = GOpti, Rel = @"optiscaler-f5\OptiScaler\D3D12_OptiScaler\D3D12Core.dll", Label = "D3D12 Agility", Sha = "07D286C306F8117321422AFFD9E6388C12D0FB4BE1C7FC689D9E899324FEEB24", Use = "OPTI" },
            new Spec { Group = GAmd, Rel = @"amd\nvngx.dll_dlssnr.dll", Label = "forwarder vendor-neutral", Sha = "30AEFC7AA19399C2905BFA64F69CCA7EB1574135425D905C0E2F052FC523099C", Use = "AMD" },
            new Spec { Group = GAmd, Rel = @"amd\nr_port.ini", Label = "réglages du port", Use = "AMD" },
            new Spec { Group = GReno, Rel = @"renodx\dxgi.dll", Label = "ReShade", Sha = "0CEE63F9C9F13F3AC909C5B4903F4DBB4B719A7AB3B4F13B0DEAF83C814B94F7", Use = "NVIDIA" },
            new Spec { Group = GReno, Rel = @"renodx\renodx-dlss5.addon64", Label = "add-on RenoDX DLSS 5", Sha = "D5ADF82EB44B065F4C590AC91FE824BAB07AFEA0EB9F994BDE936710C8593952", Use = "NVIDIA" },
            new Spec { Group = GReno, Rel = @"renodx\ReShade.ini", Label = "réglages ReShade", Use = "NVIDIA" },
            new Spec { Group = GFeed, Rel = @"feeder-reshade\dlss5-feed.addon64", Label = "add-on DLSS5-Feeder", Sha = "066EEC8C797DF2D656F2AB2324278921B1DD6E9116C9945294F4A00F7FEC608A", Use = "NVIDIA" },
            new Spec { Group = GFeed, Rel = @"feeder-reshade\dlss5-feed.cfg", Label = "réglages du feeder", Use = "NVIDIA" },
            new Spec { Group = GFeed, Rel = @"feeder-reshade\nvngx_dlss.dll", Label = "DLSS (upscaler NVIDIA)", Sha = "C85F971CE023C9F3492FC7455F0B01A24BA18EA39636407A846902C4360B0B7E", Use = "NVIDIA" },
            new Spec { Group = GFeed, Rel = @"feeder-reshade\shaders", Label = "shaders LumeniteFX", IsDir = true, MinFiles = 20, Use = "NVIDIA" },
            new Spec { Group = GFeed, Rel = @"feeder-reshade\textures", Label = "textures des shaders", IsDir = true, MinFiles = 3, Use = "NVIDIA" },
        };

        static bool UsedHere(string use)
        {
            switch (use)
            {
                case "NR_OPEN": return !Gpu.UseOriginalNr;
                case "NR_ORIG": return Gpu.UseOriginalNr;
                case "AMD": return Gpu.IsAmdMode;
                case "NVIDIA": return !Gpu.IsAmdMode;
                default: return true;
            }
        }

        // Les groupes dont une méthode d'installation a besoin sur cette carte.
        static bool UsedBy(string group, InstallMode mode)
        {
            if (group == GNrOpen) return !Gpu.UseOriginalNr;
            if (group == GNrOrig) return Gpu.UseOriginalNr;
            if (group == GAmd) return Gpu.IsAmdMode;
            if (group == GOpti) return mode == InstallMode.OptiScaler || mode == InstallMode.Feeder;
            if (group == GReno) return mode == InstallMode.RenoDX || mode == InstallMode.FeederReShade;
            if (group == GFeed) return mode == InstallMode.FeederReShade;
            return false;
        }

        // payloadRoot : le dossier payload à vérifier ; forFriend : tout est « utilisé » (l'ami peut avoir n'importe quelle carte).
        public static List<PItem> Check(string payloadRoot, bool forFriend)
        {
            var r = new List<PItem>();
            foreach (var s in Specs)
            {
                var it = new PItem { Group = s.Group, Rel = s.Rel, Label = s.Label, Expected = s.Sha, Needed = forFriend || UsedHere(s.Use) };
                string p = Path.Combine(payloadRoot, s.Rel.Replace('\\', Path.DirectorySeparatorChar));
                try
                {
                    if (s.IsDir)
                    {
                        if (!Directory.Exists(p)) it.State = PState.Manquant;
                        else
                        {
                            int n = Directory.GetFiles(p, "*", SearchOption.AllDirectories).Length;
                            it.Found = n + " fichiers";
                            if (n < s.MinFiles) { it.State = PState.Abime; it.Detail = "incomplet : " + n + " fichiers, au moins " + s.MinFiles + " attendus"; }
                        }
                    }
                    else if (!File.Exists(p)) it.State = PState.Manquant;
                    else if (new FileInfo(p).Length == 0) { it.State = PState.Abime; it.Detail = "fichier vide"; }
                    else if (s.Sha == null) it.Found = "présent";
                    else
                    {
                        string sha = ShaOf(p);
                        bool ok = sha.Equals(s.Sha, StringComparison.OrdinalIgnoreCase);
                        // un fichier abîmé n'est jamais passé à la lecture des infos de version (Mono y plante)
                        string trunc = !ok && IsBinary(p) ? PeProblem(p) : null;
                        string ver = trunc == null ? FileVersion(p) : null;
                        it.Found = Short(sha) + (ver != null ? "  (v " + ver + ")" : "");
                        if (ok) it.State = PState.Ok;
                        else
                        {
                            string known = KnownFiles.Identify(sha);
                            if (trunc != null) { it.State = PState.Abime; it.Detail = trunc; }
                            else if (known != null) { it.State = PState.AutreVersion; it.Detail = "c'est : " + known; }
                            else { it.State = PState.Inconnu; it.Detail = "hash jamais vu : peut-être une version plus récente, mais le launcher n'a été testé qu'avec " + s.Label; }
                        }
                    }
                }
                catch (Exception ex) { it.State = PState.Abime; it.Detail = "illisible : " + ex.Message; }
                r.Add(it);
            }
            // Versions d'OptiScaler ajoutées : présence, fichiers intacts, version reconnue (aucun hash « attendu »).
            foreach (var folder in OptiVersions.Folders(payloadRoot).Where(x => x != OptiVersions.Default))
            {
                string od = Path.Combine(payloadRoot, folder);
                foreach (var f in new[] { "OptiScaler.dll", "nvngx.dll_dlssnr.dll", "OptiScaler.ini", "OptiScaler" })
                {
                    var it = new PItem { Group = "OptiScaler — " + folder, Rel = folder + "\\" + f, Label = f, Extra = true, Folder = folder, Needed = false };
                    string p = Path.Combine(od, f);
                    try
                    {
                        if (f == "OptiScaler")
                        {
                            if (!Directory.Exists(p)) it.State = PState.Manquant;
                            else { int n = Directory.GetFiles(p, "*", SearchOption.AllDirectories).Length; it.Found = n + " fichiers"; if (n < 10) { it.State = PState.Abime; it.Detail = "incomplet : " + n + " fichiers"; } }
                        }
                        else if (!File.Exists(p)) it.State = PState.Manquant;
                        else if (new FileInfo(p).Length == 0) { it.State = PState.Abime; it.Detail = "fichier vide"; }
                        else if (f.EndsWith(".ini")) it.Found = "présent";
                        else
                        {
                            string pe = PeProblem(p);
                            if (pe != null) { it.State = PState.Abime; it.Detail = pe; }
                            else
                            {
                                string sha = ShaOf(p), known = KnownFiles.Identify(sha), ver = FileVersion(p);
                                it.Found = Short(sha) + (ver != null ? "  (v " + ver + ")" : "");
                                it.Detail = known != null ? "c'est : " + known + " — pas encore validée avec le launcher" : "version inconnue — pas encore validée avec le launcher";
                            }
                        }
                    }
                    catch (Exception ex) { it.State = PState.Abime; it.Detail = "illisible : " + ex.Message; }
                    r.Add(it);
                }
            }
            // Mélange de versions dans OptiScaler : le cœur est bon mais un composant ne l'est pas (ou l'inverse).
            var opti = r.Where(x => x.Group == GOpti && !x.Extra && x.Expected != null && x.State != PState.Manquant).ToList();
            if (opti.Any(x => x.State == PState.Ok) && opti.Any(x => x.State == PState.AutreVersion || x.State == PState.Inconnu))
                foreach (var x in opti.Where(x => x.State == PState.AutreVersion || x.State == PState.Inconnu))
                    x.Detail = (x.Detail ?? "") + " — MÉLANGE de versions : le reste d'OptiScaler est la 0.1.32";
            return r;
        }

        public static List<PItem> Check() { return Check(AppPaths.Payload, false); }

        // Problèmes qui touchent une installation précise sur cette carte.
        public static List<PItem> ProblemsFor(InstallMode mode) { return ProblemsFor(mode, OptiVersions.Default); }
        public static List<PItem> ProblemsFor(InstallMode mode, string optiFolder)
        {
            var all = Check();
            bool opti = mode == InstallMode.OptiScaler || mode == InstallMode.Feeder;
            var r = all.Where(x => !x.Extra && x.State != PState.Ok && UsedBy(x.Group, mode) && !(opti && x.Group == GOpti && optiFolder != OptiVersions.Default)).ToList();
            if (opti && optiFolder != OptiVersions.Default) r.AddRange(all.Where(x => x.Extra && x.Folder == optiFolder && x.State != PState.Ok));
            return r;
        }

        public static string Verdict(List<PItem> items)
        {
            var bad = items.Where(x => (x.Needed || x.Extra) && x.State != PState.Ok).ToList();
            int blocking = bad.Count(x => x.Blocking);
            int extra = items.Where(x => x.Extra).Select(x => x.Folder).Distinct().Count();
            if (bad.Count == 0) return "tout est bon (" + items.Count(x => x.Needed) + " éléments vérifiés" + (extra > 0 ? ", + " + extra + " autre" + (extra > 1 ? "s" : "") + " version" + (extra > 1 ? "s" : "") + " d'OptiScaler" : "") + ")";
            return bad.Count + " problème" + (bad.Count > 1 ? "s" : "") + (blocking > 0 ? " dont " + blocking + " bloquant" + (blocking > 1 ? "s" : "") : "");
        }

        public static string Report(List<PItem> items, bool forFriend)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Docteur payload — " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
            if (!forFriend) sb.AppendLine("Carte : " + Gpu.Vendor + (Gpu.Series > 0 ? " RTX " + Gpu.Series + "xx" : "") + "   →   DLL NR utilisée : " + Gpu.NrLabel + (Gpu.IsAmdMode ? " + port vendor-neutral" : ""));
            sb.AppendLine("Verdict : " + Verdict(items) + (forFriend ? " (copie pour un ami : tout est vérifié)" : " pour cette carte"));
            sb.AppendLine();
            foreach (var part in new[] { 0, 1, 2 })
            {
                var sel = items.Where(x => part == 2 ? x.Extra : !x.Extra && x.Needed == (part == 0)).ToList();
                if (sel.Count == 0) continue;
                sb.AppendLine(part == 2 ? "=== Autres versions d'OptiScaler (choisies jeu par jeu, à côté de la 0.1.32 testée)" : part == 0 ? (forFriend ? "=== Payload complet" : "=== Utilisé sur ta carte") : "=== Pas utilisé sur ta carte (sert à une copie pour un ami)");
                foreach (var grp in sel.GroupBy(x => x.Group))
                {
                    sb.AppendLine(grp.Key);
                    foreach (var x in grp)
                    {
                        sb.AppendLine("  " + x.StateText.PadRight(17) + x.Rel.PadRight(58) + (x.Found ?? ""));
                        if (x.Detail != null) sb.AppendLine("  " + "".PadRight(17) + "→ " + x.Detail);
                    }
                }
                sb.AppendLine();
            }
            sb.AppendLine("OK : identique au payload testé.  AUTRE VERSION : un fichier connu, mais pas celui attendu ici.");
            sb.AppendLine("VERSION INCONNUE : jamais vu (souvent une mise à jour) : ça peut marcher, mais ce n'est pas la version testée.");
            sb.AppendLine("ABÎMÉ : copie incomplète ou fichier vide : à recopier.  MANQUANT : à remettre dans payload\\.");
            return sb.ToString();
        }

        // Résumé court pour la fiche de test.
        public static string FicheSection(List<PItem> items)
        {
            var sb = new StringBuilder();
            sb.AppendLine("- Verdict : " + Verdict(items));
            foreach (var x in items.Where(x => (x.Needed || x.Extra) && x.State != PState.Ok))
                sb.AppendLine("- " + x.StateText + " : `" + x.Rel + "`" + (x.Found != null ? " — " + x.Found : "") + (x.Detail != null ? " — " + x.Detail : ""));
            return sb.ToString();
        }

        static string Short(string sha) { return sha.Length < 16 ? sha : sha.Substring(0, 8) + "…" + sha.Substring(sha.Length - 6); }

        static bool IsBinary(string p)
        {
            string e = Path.GetExtension(p).ToLowerInvariant();
            return e == ".dll" || e == ".addon64" || e == ".addon32" || e == ".exe";
        }

        static string FileVersion(string p)
        {
            if (!IsBinary(p)) return null;
            try
            {
                var v = FileVersionInfo.GetVersionInfo(p);
                string s = !string.IsNullOrEmpty(v.ProductVersion) ? v.ProductVersion : v.FileVersion;
                return string.IsNullOrEmpty(s) ? null : s.Trim();
            }
            catch { return null; }
        }

        // Copie incomplète : un fichier PE dont les sections (ou la signature) dépassent la fin du fichier.
        public static string PeProblem(string p)
        {
            try
            {
                using (var f = new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var br = new BinaryReader(f))
                {
                    long len = f.Length;
                    if (len < 0x40 || br.ReadUInt16() != 0x5A4D) return "pas un fichier Windows valide (copie abîmée ?)";
                    f.Position = 0x3C; int pe = br.ReadInt32();
                    if (pe <= 0 || pe + 24 > len) return "en-tête coupé (copie incomplète)";
                    f.Position = pe; if (br.ReadUInt32() != 0x00004550) return "en-tête invalide (copie abîmée ?)";
                    f.Position = pe + 6; int nsec = br.ReadUInt16();
                    f.Position = pe + 20; int optSize = br.ReadUInt16();
                    long opt = pe + 24;
                    f.Position = opt; ushort magic = br.ReadUInt16();
                    long dd = opt + (magic == 0x20B ? 112 : 96);          // répertoires de données
                    long secTable = opt + optSize;
                    if (secTable + nsec * 40L > len) return "table des sections coupée (copie incomplète)";
                    long end = 0;
                    for (int i = 0; i < nsec; i++)
                    {
                        f.Position = secTable + i * 40L + 16;
                        uint rawSize = br.ReadUInt32(), rawPtr = br.ReadUInt32();
                        if (rawSize > 0) end = Math.Max(end, (long)rawPtr + rawSize);
                    }
                    if (dd + 5 * 8 <= secTable)
                    {   // répertoire 4 = signature Authenticode (offset dans le fichier)
                        f.Position = dd + 4 * 8; uint certPtr = br.ReadUInt32(), certSize = br.ReadUInt32();
                        if (certPtr > 0 && certSize > 0) end = Math.Max(end, (long)certPtr + certSize);
                    }
                    if (end > len) return "fichier coupé : " + len.ToString("N0") + " octets sur " + end.ToString("N0") + " attendus (copie incomplète)";
                }
            }
            catch (Exception ex) { return "illisible : " + ex.Message; }
            return null;
        }
    }

    // Plusieurs versions d'OptiScaler côte à côte : payload\optiscaler-f5 (0.1.32, testée) et payload\optiscaler-f5-<version>.
    // Le choix se fait jeu par jeu (optiscaler-par-jeu.tsv) ; il compte à la prochaine installation.
    public static class OptiVersions
    {
        public const string Default = "optiscaler-f5";
        static readonly object Lk = new object();
        public static string FilePath { get { return Path.Combine(AppPaths.Root, "optiscaler-par-jeu.tsv"); } }

        public static List<string> Folders() { return Folders(AppPaths.Payload); }
        public static List<string> Folders(string payloadRoot)
        {
            var r = new List<string>();
            try
            {
                foreach (var d in Directory.GetDirectories(payloadRoot))
                {
                    string n = Path.GetFileName(d);
                    if ((n.Equals(Default, StringComparison.OrdinalIgnoreCase) || n.StartsWith(Default + "-", StringComparison.OrdinalIgnoreCase)) && File.Exists(Path.Combine(d, "OptiScaler.dll"))) r.Add(n);
                }
            }
            catch { }
            return r.OrderBy(x => x.Equals(Default, StringComparison.OrdinalIgnoreCase) ? "" : x, StringComparer.OrdinalIgnoreCase).ToList();
        }
        public static string Dir(string folder) { return Path.Combine(AppPaths.Payload, folder); }

        // « 0.1.32 », d'après le hash, sinon le nom du dossier, sinon les infos de version du fichier
        public static string Version(string folder)
        {
            try
            {
                string n = KnownFiles.Identify(Hash.ShaCached(Path.Combine(Dir(folder), "OptiScaler.dll")));
                if (n != null && n.StartsWith("OptiScaler F5 ")) return n.Substring(14);
            }
            catch { }
            if (folder.StartsWith(Default + "-", StringComparison.OrdinalIgnoreCase)) return folder.Substring(Default.Length + 1);
            return folder == Default ? "0.1.32 ?" : folder;
        }
        public static string Label(string folder)
        {
            return "OptiScaler F5 " + Version(folder) + (folder == Default ? "  (testée)" : "  (ajoutée, à tester)");
        }

        public static string For(string gameName)
        {
            string k = Installer.Safe(gameName), v = null;
            lock (Lk)
                try { if (File.Exists(FilePath)) foreach (var l in File.ReadAllLines(FilePath, Encoding.UTF8)) { var p = l.Split('\t'); if (p.Length >= 2 && p[0] == k) v = p[1]; } } catch { }
            return v != null && Folders().Contains(v) ? v : Default;
        }
        public static void Set(string gameName, string folder)
        {
            string k = Installer.Safe(gameName);
            lock (Lk)
            {
                var lines = new List<string>();
                try { if (File.Exists(FilePath)) lines = File.ReadAllLines(FilePath, Encoding.UTF8).Where(l => !l.StartsWith(k + "\t")).ToList(); } catch { }
                if (folder != Default) lines.Add(k + "\t" + folder);
                File.WriteAllLines(FilePath, lines, new UTF8Encoding(false));
            }
            Log.W("Version d'OptiScaler pour " + gameName + " : " + Label(folder));
        }
    }
}
