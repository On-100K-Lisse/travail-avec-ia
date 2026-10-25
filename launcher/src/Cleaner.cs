// Licence MIT (voir LICENSE-MIT du dépôt travail-avec-ia). Code écrit par Claude (Anthropic) pour On-100-K-Lisse.
// DLSS5 Launcher — réinitialisation universelle : défait aussi ce que d'autres launchers ont mis
// (1-Click DLSS5 via son manifeste, setup_windows.bat d'OptiScaler, installations à la main…).
// Rien n'est supprimé : tout part en quarantaine dans le dossier du launcher, avec un bouton « Annuler ».
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace Dlss5Launcher
{
    public class CleanItem
    {
        public string Path, Reason;
        public bool IsDir;
        public bool MayBeGameFile; // le jeu avait peut-être un original de ce nom -> vérification Steam conseillée
    }

    public class CleanResult
    {
        public List<string> Errors = new List<string>();
        public int Moved, Restored;
        public bool NeedValidate;
        public string QuarantineDir;
    }

    public static class Cleaner
    {
        public static string QuarantineRoot { get { return Path.Combine(AppPaths.Root, "quarantaine"); } }

        // Fichiers qu'aucun jeu ne livre : ce sont toujours des restes de mods DLSS / ReShade / OptiScaler.
        static readonly string[] ModFiles = {
            "OptiScaler.ini", "OptiScaler.log", "OptiScaler.asi", "OptiScaler.dll", "nvngx.dll_dlssnr.dll",
            "ReShade.ini", "ReShadePreset.ini", "ReShade.log", "ReShade.log.old",
            "dlss5-feed.addon64", "dlss5-feed.addon32", "dlss5-feed.cfg", "dlss5-feed.ini", "dlss5-feed.log", "dlss5-feed-host.log",
            "Remove_OptiScaler.bat", "Remove OptiScaler.bat", "dgVoodoo.conf", "dgVoodooCpl.exe",
            "standalone-dlssnr.addon64", "standalone-dlssnr.addon32", "dlss5-aio-x86.cfg", "AIO DLSS5 32-bit Wrapper.exe",
            "nr_port.ini", "nr_port.log", "nr_tile_cache.txt", "OptiScaler_crash.dmp",
            "!! EXTRACT ALL FILES TO GAME FOLDER !!", "!! README_EXTRACT ALL FILES TO GAME FOLDER !!.txt" };
        // Fichiers du package OptiScaler, retirés seulement s'il y a aussi des traces d'OptiScaler dans le même dossier.
        static readonly string[] OptiPackageFiles = { "setup_windows.bat", "setup_linux.sh", "get_streamline.ps1", "INSTALL-DLSSNR.md", "TROUBLESHOOTING.md", "SHA256SUMS.txt" };
        // Noms sous lesquels un mod se fait charger par le jeu.
        static readonly string[] ProxyNames = { "dxgi.dll", "winmm.dll", "version.dll", "dbghelp.dll", "d3d12.dll", "wininet.dll", "winhttp.dll",
            "d3d11.dll", "d3d9.dll", "d3d8.dll", "ddraw.dll", "opengl32.dll", "dinput8.dll", "nvapi64.dll", "nvngx.dll" };

        // Hash connus de fichiers de mods (tes payloads, le 1-Click, tes DLL NR testées).
        static readonly HashSet<string> KnownModHashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CDFA6C1EC784867182164C7836F324F087B5C9F374D0C9FE46F54999FEABF3ED", // OpenNR 310.9.16 compat-fp16
            "2BC19BC09A7B89DD44F73E2AC0108FCC586E2154BDD0830F242F4F040E28789F", // 310.9.16 NVIDIA d'origine
            "CCF6C7B53E95F68E9B03FB91A9A32BEBA2476980BDECD0475C0F2FFDC7A04AF9", // 310.9.16 patchée
            "4B8D19BC3EFF58A084F5ECA7489C921501C203450169FB82FF4F649A4482BA05", // NR du payload 1-Click
            "8270B350CD82DE5CE89806872CDD6B6A9249B80836B91BBEB3573470744CC206", // 310.8 du pack streamline
            "6EB209E764F39872625DEBD6ABAF45E2BB6322F6F270F781F70C059AE30B3927", // 310.8.SF (Dead as Disco)
            "E16BCF15E16E13F527491CDF7845B2FE6521A738D8F7C9C721866A8496E1FC8E", // 310.8 RTX 50 (guide F5)
            "E67DEE209320CDAFE0E93E45675D7AA34323A53ACC57A72B2E40A181581C989A", // 310.8 ShortFuse (guide F5)
            "C85F971CE023C9F3492FC7455F0B01A24BA18EA39636407A846902C4360B0B7E", // nvngx_dlss.dll du 1-Click / streamline
            "9F6672E5E0170DC118A3188D21BDA187E1FC1AA3502895B21AB846D23165C11D", // sl.dlss_nr.dll du 1-Click
            "0CEE63F9C9F13F3AC909C5B4903F4DBB4B719A7AB3B4F13B0DEAF83C814B94F7", // ReShade dxgi.dll
            "DA430E0A9C6EECEFA0D1B27D05E16C426FB5D04E808B194D914EAAC4B31BC0F8", // ReShade dxgi32.dll
            "14017C79B30814E7EA3297291331A3A3AE5B2E6C4B9F4DD5AA7C4599BF061C2D", // OptiScaler F5 0.1.32
            "FBFB6676B829DAD7E020FB830586A16AA0EC6ADD78016DB48EF12E2AE1803231", // OptiScaler 0.9.4 (1-Click)
            "6198502F1B71D861964F41FC7FA8DE0268FBF994D439C9C6DC9005514B605A7F", // forwarder nvngx.dll_dlssnr.dll
            "0BEE429A8336B2FB9EEA7973AEB1FF73FAF07F77E734D5F574E2EE75F97C8A0D", // nvngx.dll pont de DLSS5-ReShade-AIO 2.2.2
            "30AEFC7AA19399C2905BFA64F69CCA7EB1574135425D905C0E2F052FC523099C", // forwarder vendor-neutral (AMD) 0.1.32
        };
        static readonly HashSet<long> KnownSizes = new HashSet<long> { 276162560, 266424944, 165840496, 165830144, 58956400, 401024, 5592064, 4398080, 28518400, 25379632, 152064, 10240 };

        static bool KnownHash(string path, bool always)
        {
            try
            {
                long len = new FileInfo(path).Length;
                if (!always && !KnownSizes.Contains(len)) return false;
                return KnownModHashes.Contains(Hash.Sha(path));
            }
            catch { return false; }
        }

        static string VersionText(string path)
        {
            try
            {
                var v = FileVersionInfo.GetVersionInfo(path);
                return (v.ProductName + "|" + v.OriginalFilename + "|" + v.FileDescription + "|" + v.InternalName + "|" + v.CompanyName).ToLowerInvariant();
            }
            catch { return ""; }
        }
        static bool IsModBinary(string path)
        {
            string t = VersionText(path);
            if (t.Contains("optiscaler") || t.Contains("reshade") || t.Contains("dgvoodoo") || t.Contains("fakenvapi")) return true;
            return KnownHash(path, false);
        }

        // ------------------------------------------------------------- dossiers 1-Click
        public static List<string> FindOneClickDirs(Game g)
        {
            var res = new List<string>();
            var stack = new Stack<KeyValuePair<string, int>>();
            stack.Push(new KeyValuePair<string, int>(g.Root, 0));
            while (stack.Count > 0)
            {
                var cur = stack.Pop();
                string[] dirs;
                try { dirs = Directory.GetDirectories(cur.Key); } catch { continue; }
                foreach (var d in dirs)
                {
                    string n = Path.GetFileName(d);
                    if (n.Equals("_DLSS5_Backup", StringComparison.OrdinalIgnoreCase) || n.Equals("_1Click_DLSS5_Backup", StringComparison.OrdinalIgnoreCase)) { res.Add(d); continue; }
                    if (cur.Value < 6 && !n.Equals(AppPaths.StateDirName, StringComparison.OrdinalIgnoreCase)) stack.Push(new KeyValuePair<string, int>(d, cur.Value + 1));
                }
            }
            return res;
        }

        // ------------------------------------------------------------- plan (lecture seule)
        public static List<CleanItem> Plan(Game g)
        {
            var items = new List<CleanItem>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Action<string, bool, string, bool> add = (p, dir, why, game) =>
            {
                if (seen.Add(p)) items.Add(new CleanItem { Path = p, IsDir = dir, Reason = why, MayBeGameFile = game });
            };

            // dossiers à balayer : celui de l'exe, la racine du jeu, et tout dossier où traîne un fichier de mod
            var sweep = new List<string>();
            Action<string> addDir = d => { if (d != null && Directory.Exists(d) && !sweep.Contains(d, StringComparer.OrdinalIgnoreCase)) sweep.Add(d); };
            addDir(g.ExeDir); addDir(g.Root);
            var markers = new HashSet<string>(ModFiles.Where(f => f.EndsWith(".ini") || f.EndsWith(".dll") || f.Contains("addon") || f.EndsWith(".asi")), StringComparer.OrdinalIgnoreCase);
            foreach (var f in Scanner.Walk(g.Root, 8, 80000))
            {
                string n = Path.GetFileName(f);
                if (markers.Contains(n) || n.StartsWith("standalone-dlssnr", StringComparison.OrdinalIgnoreCase) || (n.StartsWith("renodx-", StringComparison.OrdinalIgnoreCase) && n.IndexOf(".addon", StringComparison.OrdinalIgnoreCase) > 0))
                    addDir(Path.GetDirectoryName(f));
            }
            foreach (var bd in FindOneClickDirs(g)) { addDir(Path.GetDirectoryName(bd)); add(bd, true, "dossier de sauvegarde d'un 1-Click DLSS5", false); }

            foreach (var d in sweep)
            {
                string[] files; string[] dirs;
                try { files = Directory.GetFiles(d); dirs = Directory.GetDirectories(d); } catch { continue; }
                var names = new HashSet<string>(files.Select(Path.GetFileName), StringComparer.OrdinalIgnoreCase);
                var dnames = new HashSet<string>(dirs.Select(Path.GetFileName), StringComparer.OrdinalIgnoreCase);

                bool hasOpti = names.Contains("OptiScaler.ini") || names.Contains("OptiScaler.dll") || names.Contains("nvngx.dll_dlssnr.dll") || dnames.Contains("OptiScaler");
                var proxyHits = new List<string>();
                foreach (var p in ProxyNames)
                    if (names.Contains(p) && IsModBinary(Path.Combine(d, p))) { proxyHits.Add(p); if (VersionText(Path.Combine(d, p)).Contains("optiscaler")) hasOpti = true; }
                bool hasMod = hasOpti || proxyHits.Count > 0 || names.Contains("ReShade.ini") || names.Any(n => n.StartsWith("renodx-", StringComparison.OrdinalIgnoreCase) || n.StartsWith("dlss5-feed", StringComparison.OrdinalIgnoreCase) || n.StartsWith("standalone-dlssnr", StringComparison.OrdinalIgnoreCase));
                // DLSS5-ReShade-AIO (kibblerz) : son README et ses licences
                if (names.Contains("README_FIRST.txt"))
                    try { if (File.ReadAllText(Path.Combine(d, "README_FIRST.txt")).IndexOf("DLSS5 ReShade AIO", StringComparison.OrdinalIgnoreCase) >= 0) add(Path.Combine(d, "README_FIRST.txt"), false, "README de DLSS5-ReShade-AIO", false); } catch { }
                foreach (var lic in dirs.Where(x => Path.GetFileName(x).Equals("licenses", StringComparison.OrdinalIgnoreCase)))
                    if (File.Exists(Path.Combine(lic, "DLSS5-ReShade-AIO-LICENSE.txt")) || File.Exists(Path.Combine(lic, "DLSS5-Feeder-LICENSE.txt")))
                        add(lic, true, "licences de DLSS5-ReShade-AIO", false);
                if (dnames.Contains("host64") && File.Exists(Path.Combine(d, "host64", "AIO DLSS5 32-bit Wrapper.exe"))) add(Path.Combine(d, "host64"), true, "hôte 32-bit de DLSS5-ReShade-AIO", false);

                foreach (var f in ModFiles) if (names.Contains(f)) add(Path.Combine(d, f), false, "fichier de mod", false);
                foreach (var n in names.Where(n => n.StartsWith("renodx-", StringComparison.OrdinalIgnoreCase) && n.IndexOf(".addon", StringComparison.OrdinalIgnoreCase) > 0))
                    add(Path.Combine(d, n), false, "addon RenoDX", false);
                foreach (var p in proxyHits) add(Path.Combine(d, p), false, "DLL proxy d'un mod (" + Describe(Path.Combine(d, p)) + ")", false);
                if (hasOpti)
                {
                    foreach (var f in OptiPackageFiles) if (names.Contains(f)) add(Path.Combine(d, f), false, "fichier du package OptiScaler", false);
                    if (dnames.Contains("Licenses") && (File.Exists(Path.Combine(d, "Licenses", "XeSS_LICENSE.txt")) || File.Exists(Path.Combine(d, "Licenses", "DLSS5Feeder_LICENSE.txt"))))
                        add(Path.Combine(d, "Licenses"), true, "licences du package OptiScaler", false);
                    if (dnames.Contains("docs") && File.Exists(Path.Combine(d, "docs", "DLSS-FRAME-GENERATION.md")))
                        add(Path.Combine(d, "docs"), true, "docs du package OptiScaler", false);
                    if (dnames.Contains("redist") && File.Exists(Path.Combine(d, "redist", "streamline", "manifest.json")) && names.Contains("get_streamline.ps1"))
                        add(Path.Combine(d, "redist"), true, "redist du package OptiScaler", false);
                }
                if (dnames.Contains("OptiScaler")) add(Path.Combine(d, "OptiScaler"), true, "dossier OptiScaler", false);
                if (dnames.Contains("reshade-shaders")) add(Path.Combine(d, "reshade-shaders"), true, "shaders ReShade", false);
                if (dnames.Contains("dlssnr-capture")) add(Path.Combine(d, "dlssnr-capture"), true, "captures NR", false);
                if (dnames.Contains("host64") && File.Exists(Path.Combine(d, "host64", "dlss5-feed-host64.exe"))) add(Path.Combine(d, "host64"), true, "hôte 64-bit du feeder", false);

                if (names.Contains("nvngx_dlssnr.dll"))
                {
                    string p = Path.Combine(d, "nvngx_dlssnr.dll");
                    if (hasMod || KnownHash(p, true)) add(p, false, "DLL DLSS 5 (NR) ajoutée par un mod", true);
                }
                foreach (var n in new[] { "nvngx_dlss.dll", "sl.dlss_nr.dll" })
                    if (names.Contains(n) && KnownHash(Path.Combine(d, n), false))
                        add(Path.Combine(d, n), false, "DLL déposée par le 1-Click / pack streamline", true);
            }
            return items;
        }

        static string Describe(string path)
        {
            string t = VersionText(path);
            if (t.Contains("optiscaler")) return "OptiScaler";
            if (t.Contains("reshade")) return "ReShade";
            if (t.Contains("dgvoodoo")) return "dgVoodoo";
            if (t.Contains("fakenvapi")) return "fakenvapi";
            return "hash connu";
        }

        // ------------------------------------------------------------- exécution
        public static CleanResult Clean(Game g)
        {
            var r = new CleanResult();
            if (Installer.IsGameRunning(g)) throw new UserFacingException("Le jeu roule encore. Ferme-le complètement, puis réessaie.");
            var q = new Quarantine(g);
            r.QuarantineDir = q.Dir;
            Log.W("Nettoyage universel de " + g.Name + " → quarantaine : " + q.Dir);

            // 1. installations 1-Click : on remet ses originaux d'après son manifeste
            foreach (var bd in FindOneClickDirs(g))
            {
                string gameFolder = Path.GetDirectoryName(bd);
                string mf = Path.Combine(bd, "manifest.json");
                if (!File.Exists(mf)) continue;
                try
                {
                    var js = new JavaScriptSerializer();
                    var root = js.DeserializeObject(File.ReadAllText(mf, Encoding.UTF8)) as Dictionary<string, object>;
                    if (root == null) throw new Exception("manifeste illisible");
                    string prefix = (Str(root, "backupPrefix") ?? "").Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
                    string orig = Path.Combine(bd, prefix);
                    foreach (var row in List(root, "replaced"))
                    {
                        var rd = row as Dictionary<string, object>;
                        string rel = rd != null ? Str(rd, "rel") : row as string;
                        if (string.IsNullOrEmpty(rel)) continue;
                        rel = Norm(rel);
                        string src = Path.Combine(orig, rel), dst = Path.Combine(gameFolder, rel);
                        if (!File.Exists(src)) { r.NeedValidate = true; r.Errors.Add("1-Click : original absent pour " + rel + " (vérification Steam conseillée)"); continue; }
                        if (File.Exists(dst)) q.Put(dst, false);
                        Directory.CreateDirectory(Path.GetDirectoryName(dst));
                        File.Copy(src, dst, true);
                        q.NoteRestored(dst);
                        r.Restored++;
                        Log.W("  1-Click : original remis : " + rel);
                    }
                    foreach (var a in List(root, "added"))
                    {
                        string rel = a as string; if (string.IsNullOrEmpty(rel)) continue;
                        string p = Path.Combine(gameFolder, Norm(rel));
                        if (File.Exists(p)) { q.Put(p, false); r.Moved++; }
                    }
                    var rs = root.ContainsKey("reshade") ? root["reshade"] as Dictionary<string, object> : null;
                    if (rs != null && rs.ContainsKey("installedByUs") && rs["installedByUs"] is bool && (bool)rs["installedByUs"])
                    {
                        string file = Str(rs, "file");
                        var gm = root.ContainsKey("game") ? root["game"] as Dictionary<string, object> : null;
                        string exeRel = gm != null ? Str(gm, "exe") : null;
                        string hookDir = exeRel != null ? Path.GetDirectoryName(Path.Combine(gameFolder, Norm(exeRel))) : gameFolder;
                        if (file != null && File.Exists(Path.Combine(hookDir, file))) { q.Put(Path.Combine(hookDir, file), false); r.Moved++; }
                    }
                    var dirs = List(root, "addedDirs").OfType<string>().Select(x => Path.Combine(gameFolder, Norm(x))).OrderByDescending(x => x.Length);
                    foreach (var d in dirs)
                        try { if (Directory.Exists(d) && !Directory.EnumerateFileSystemEntries(d).Any()) Directory.Delete(d); } catch { }
                }
                catch (Exception ex) { r.Errors.Add("Manifeste 1-Click (" + bd + ") : " + ex.Message); r.NeedValidate = true; }
            }

            // 1b. notre propre installation (le 1-Click, s'il y en a un, est forcément venu après : il est défait avant)
            if (Manifest.Load(g.ExeDir) != null)
            {
                var errs = new Installer(g).Reset();
                if (errs.Count > 0)
                {
                    r.Errors.AddRange(errs.Select(e => "Launcher : " + e));
                    r.Errors.Add("Balayage annulé : réessaie « Réinitialiser » une fois le problème réglé.");
                    q.Close();
                    return r;
                }
            }

            // 2. balayage de tout ce qui reste
            foreach (var it in Plan(g))
            {
                try
                {
                    if (!(it.IsDir ? Directory.Exists(it.Path) : File.Exists(it.Path))) continue;
                    q.Put(it.Path, it.IsDir); r.Moved++;
                    if (it.MayBeGameFile) r.NeedValidate = true;
                    Log.W("  quarantaine : " + it.Path + "  (" + it.Reason + ")");
                }
                catch (Exception ex) { r.Errors.Add(it.Path + " : " + ex.Message); }
            }
            q.Close();
            Log.W("Nettoyage terminé : " + r.Moved + " élément(s) en quarantaine, " + r.Restored + " original(aux) remis" + (r.Errors.Count > 0 ? ", " + r.Errors.Count + " problème(s)" : "") + ".");
            return r;
        }

        static string Norm(string rel) { return rel.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar); }
        static string Str(Dictionary<string, object> d, string k) { object o; return d.TryGetValue(k, out o) && o != null ? o.ToString() : null; }
        static IEnumerable<object> List(Dictionary<string, object> d, string k)
        {
            object o; if (!d.TryGetValue(k, out o) || o == null) return new object[0];
            if (o is string || o is Dictionary<string, object>) return new[] { o }; // PowerShell écrit parfois un seul élément sans tableau
            var arr = o as System.Collections.IEnumerable;
            return arr == null ? new object[0] : arr.Cast<object>();
        }

        // ------------------------------------------------------------- annulation
        public static string LastQuarantine(Game g)
        {
            string d = Path.Combine(QuarantineRoot, Installer.Safe(g.Name));
            if (!Directory.Exists(d)) return null;
            return Directory.GetDirectories(d).Where(x => File.Exists(Path.Combine(x, "liste.tsv")) && !File.Exists(Path.Combine(x, "ANNULE.txt")))
                            .OrderByDescending(x => x).FirstOrDefault();
        }

        public static List<string> Undo(string qdir)
        {
            var errs = new List<string>();
            var lines = File.ReadAllLines(Path.Combine(qdir, "liste.tsv"), Encoding.UTF8).Where(l => !l.StartsWith("#")).ToList();
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                var p = lines[i].Split('\t');
                try
                {
                    if (p[0] == "R")
                    {   // original remis par nous : on l'enlève seulement s'il n'a pas changé depuis
                        if (File.Exists(p[1]) && Hash.Sha(p[1]) == p[2]) File.Delete(p[1]);
                    }
                    else if (p[0] == "F" || p[0] == "D")
                    {
                        string orig = p[1], held = Path.Combine(qdir, p[2]);
                        bool dir = p[0] == "D";
                        if (dir ? Directory.Exists(orig) : File.Exists(orig)) { errs.Add(orig + " : existe déjà, laissé tel quel"); continue; }
                        Directory.CreateDirectory(Path.GetDirectoryName(orig));
                        Quarantine.MoveAny(held, orig, dir);
                    }
                }
                catch (Exception ex) { errs.Add(p.Length > 1 ? p[1] + " : " + ex.Message : ex.Message); }
            }
            File.WriteAllText(Path.Combine(qdir, "ANNULE.txt"), "Nettoyage annulé le " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + (errs.Count > 0 ? "\r\n" + string.Join("\r\n", errs) : ""));
            Log.W("Nettoyage annulé (" + qdir + ")" + (errs.Count > 0 ? " — " + errs.Count + " problème(s)" : ""));
            return errs;
        }
    }

    // Quarantaine : déplace (ou copie+vérifie+efface entre deux disques) en gardant la liste pour l'annulation.
    public class Quarantine
    {
        public readonly string Dir;
        readonly Game g;
        readonly StringBuilder list = new StringBuilder("#DLSS5-Launcher quarantaine v1 — F/D = déplacé, R = original remis\r\n");
        int n;

        public Quarantine(Game game)
        {
            g = game;
            Dir = Path.Combine(Cleaner.QuarantineRoot, Installer.Safe(g.Name), DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
            Directory.CreateDirectory(Dir);
            Flush();
        }

        public void Put(string path, bool isDir)
        {
            string rel = RelName(path);
            string dest = Path.Combine(Dir, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(dest));
            list.Append((isDir ? "D" : "F") + "\t" + path + "\t" + rel + "\r\n");
            Flush(); // noté avant le déplacement
            MoveAny(path, dest, isDir);
        }

        public void NoteRestored(string path) { list.Append("R\t" + path + "\t" + Hash.Sha(path) + "\r\n"); Flush(); }
        public void Close() { Flush(); }
        void Flush() { File.WriteAllText(Path.Combine(Dir, "liste.tsv"), list.ToString(), new UTF8Encoding(false)); }

        string RelName(string path)
        {
            string root = g.Root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string rel = path.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? path.Substring(root.Length) : (n++ + "_" + Path.GetFileName(path));
            string cand = Path.Combine("fichiers", rel); int k = 1;
            while (File.Exists(Path.Combine(Dir, cand)) || Directory.Exists(Path.Combine(Dir, cand))) cand = Path.Combine("fichiers", rel + "." + (k++));
            return cand;
        }

        public static void MoveAny(string src, string dst, bool isDir)
        {
            bool sameVol = string.Equals(Path.GetPathRoot(Path.GetFullPath(src)), Path.GetPathRoot(Path.GetFullPath(dst)), StringComparison.OrdinalIgnoreCase);
            if (isDir)
            {
                if (sameVol) { Directory.Move(src, dst); return; }
                CopyDir(src, dst);
                Directory.Delete(src, true);
            }
            else
            {
                File.SetAttributes(src, FileAttributes.Normal);
                if (sameVol) { File.Move(src, dst); return; }
                string h = Hash.Sha(src);
                File.Copy(src, dst, true);
                if (Hash.Sha(dst) != h) throw new IOException("copie non conforme vers la quarantaine");
                File.Delete(src);
            }
        }

        static void CopyDir(string src, string dst)
        {
            Directory.CreateDirectory(dst);
            foreach (var f in Directory.GetFiles(src)) { string t = Path.Combine(dst, Path.GetFileName(f)); File.Copy(f, t, true); if (new FileInfo(t).Length != new FileInfo(f).Length) throw new IOException("copie incomplète : " + f); }
            foreach (var d in Directory.GetDirectories(src)) CopyDir(d, Path.Combine(dst, Path.GetFileName(d)));
            foreach (var f in Directory.GetFiles(src, "*", SearchOption.AllDirectories)) File.SetAttributes(f, FileAttributes.Normal);
        }
    }
}
