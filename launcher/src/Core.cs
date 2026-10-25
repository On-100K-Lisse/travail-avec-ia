// Licence MIT (voir LICENSE-MIT du dépôt travail-avec-ia). Code écrit par Claude (Anthropic) pour On-100-K-Lisse.
// DLSS5 Launcher — coeur : chemins, journal, hash, INI, PE, scan, installation, réinitialisation.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace Dlss5Launcher
{
    // ------------------------------------------------------------------ chemins
    public static class AppPaths
    {
        public static string Root { get { return AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/'); } }
        public static string Payload { get { return Path.Combine(Root, "payload"); } }
        public static string OptiDir { get { return Path.Combine(Payload, "optiscaler-f5"); } }
        public static string RenoDir { get { return Path.Combine(Payload, "renodx"); } }
        public static string FeederDir { get { return Path.Combine(Payload, "feeder-reshade"); } }
        public static string AmdDir { get { return Path.Combine(Payload, "amd"); } }
        public static string NrOriginal { get { return Path.Combine(Payload, "nr-origine", "nvngx_dlssnr.dll"); } } // 310.9.16 NVIDIA signée
        public static string NrDll { get { return Path.Combine(Payload, "nr", "nvngx_dlssnr.dll"); } }
        public static string Config { get { return Path.Combine(Root, "launcher-config.ini"); } }
        public static string Journaux { get { return Path.Combine(Root, "journaux"); } }
        public static string Fiches { get { return Path.Combine(Root, "fiches-de-test"); } }
        public static string LogFile { get { return Path.Combine(Root, "launcher.log"); } }
        public const string StateDirName = "_DLSS5_Launcher";
    }

    // ------------------------------------------------------------------ journal
    public static class Log
    {
        public static event Action<string> Line;
        static readonly object Lk = new object();
        public static void W(string msg)
        {
            string l = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + msg;
            lock (Lk) { try { File.AppendAllText(AppPaths.LogFile, l + Environment.NewLine, Encoding.UTF8); } catch { } }
            var h = Line; if (h != null) h(l);
        }
    }

    // ------------------------------------------------------------------ hash
    public static class Hash
    {
        static readonly Dictionary<string, string> Cache = new Dictionary<string, string>();
        public static string Sha(string path)
        {
            using (var s = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1 << 20))
            using (var h = SHA256.Create())
                return BitConverter.ToString(h.ComputeHash(s)).Replace("-", "");
        }
        // Les fichiers du payload ne changent pas pendant une session : on garde leur hash en cache.
        public static void ClearCache() { lock (Cache) Cache.Clear(); }
        public static string ShaCached(string path)
        {
            var fi = new FileInfo(path);
            string k = fi.FullName + "|" + fi.Length + "|" + fi.LastWriteTimeUtc.Ticks;
            lock (Cache)
            {
                string v; if (Cache.TryGetValue(k, out v)) return v;
            }
            string r = Sha(path);
            lock (Cache) Cache[k] = r;
            return r;
        }
        public static string ShaText(string text)
        {
            using (var h = SHA256.Create())
                return BitConverter.ToString(h.ComputeHash(new UTF8Encoding(false).GetBytes(text))).Replace("-", "");
        }
    }

    // ------------------------------------------------------------------ INI (respecte les sections)
    public static class Ini
    {
        public static string Set(string text, string section, string key, string value)
        {
            string nl = text.Contains("\r\n") ? "\r\n" : "\n";
            var lines = text.Replace("\r\n", "\n").Split('\n').ToList();
            int start = -1, end = lines.Count;
            for (int i = 0; i < lines.Count; i++)
            {
                string t = lines[i].Trim();
                if (t.StartsWith("[") && t.EndsWith("]"))
                {
                    if (start >= 0) { end = i; break; }
                    if (string.Equals(t, "[" + section + "]", StringComparison.OrdinalIgnoreCase)) start = i;
                }
            }
            if (start < 0)
            {
                if (lines.Count > 0 && lines[lines.Count - 1].Trim() != "") lines.Add("");
                lines.Add("[" + section + "]");
                lines.Add(key + "=" + value);
            }
            else
            {
                bool done = false;
                for (int i = start + 1; i < end; i++)
                {
                    string t = lines[i].TrimStart();
                    if (t.StartsWith(";") || t.StartsWith("#")) continue;
                    int eq = t.IndexOf('=');
                    if (eq > 0 && string.Equals(t.Substring(0, eq).Trim(), key, StringComparison.OrdinalIgnoreCase))
                    { lines[i] = key + "=" + value; done = true; break; }
                }
                if (!done)
                {
                    int ins = end;
                    while (ins - 1 > start && lines[ins - 1].Trim() == "") ins--;
                    lines.Insert(ins, key + "=" + value);
                }
            }
            return string.Join(nl, lines);
        }

        public static string Get(string text, string section, string key)
        {
            bool inSec = false;
            foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
            {
                string t = raw.Trim();
                if (t.StartsWith("[") && t.EndsWith("]")) { inSec = string.Equals(t, "[" + section + "]", StringComparison.OrdinalIgnoreCase); continue; }
                if (!inSec || t.StartsWith(";") || t.StartsWith("#")) continue;
                int eq = t.IndexOf('=');
                if (eq > 0 && string.Equals(t.Substring(0, eq).Trim(), key, StringComparison.OrdinalIgnoreCase)) return t.Substring(eq + 1).Trim();
            }
            return null;
        }

        public static string ReadFile(string path) { return File.ReadAllText(path, Encoding.UTF8); }
        public static void WriteFile(string path, string text) { File.WriteAllText(path, text, new UTF8Encoding(false)); }
    }

    // ------------------------------------------------------------------ lecture PE (architecture + DLL importées)
    public class PeInfo
    {
        public bool Ok;
        public bool Is64;
        public HashSet<string> Imports = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    public static class Pe
    {
        public static PeInfo Read(string path)
        {
            var r = new PeInfo();
            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var br = new BinaryReader(fs))
                {
                    if (fs.Length < 0x200 || br.ReadUInt16() != 0x5A4D) return r;
                    fs.Position = 0x3C; int peOff = br.ReadInt32();
                    fs.Position = peOff; if (br.ReadUInt32() != 0x00004550) return r;
                    ushort machine = br.ReadUInt16(); ushort nSec = br.ReadUInt16();
                    fs.Position += 12; ushort optSize = br.ReadUInt16(); fs.Position += 2;
                    long optStart = fs.Position;
                    ushort magic = br.ReadUInt16();
                    r.Is64 = magic == 0x20B || machine == 0x8664;
                    long ddStart = optStart + (magic == 0x20B ? 112 : 96);
                    fs.Position = ddStart + 8 * 1; uint impRva = br.ReadUInt32();
                    fs.Position = ddStart + 8 * 13; uint dlyRva = br.ReadUInt32();
                    long secStart = optStart + optSize;
                    var secs = new List<uint[]>();
                    for (int i = 0; i < nSec; i++)
                    {
                        fs.Position = secStart + i * 40 + 8;
                        uint vsz = br.ReadUInt32(), va = br.ReadUInt32(), rsz = br.ReadUInt32(), raw = br.ReadUInt32();
                        secs.Add(new[] { va, Math.Max(vsz, rsz), raw });
                    }
                    Func<uint, long> off = rva =>
                    {
                        foreach (var s in secs) if (rva >= s[0] && rva < s[0] + s[1]) return (long)(rva - s[0] + s[2]);
                        return -1;
                    };
                    Func<long, string> cstr = o =>
                    {
                        if (o < 0 || o >= fs.Length) return null;
                        fs.Position = o; var sb = new StringBuilder();
                        for (int k = 0; k < 260; k++) { int c = fs.ReadByte(); if (c <= 0) break; sb.Append((char)c); }
                        return sb.ToString();
                    };
                    if (impRva != 0)
                    {
                        long o = off(impRva);
                        for (int i = 0; o >= 0 && i < 512; i++)
                        {
                            fs.Position = o + i * 20 + 12; uint nameRva = br.ReadUInt32();
                            if (nameRva == 0) break;
                            string n = cstr(off(nameRva)); if (!string.IsNullOrEmpty(n)) r.Imports.Add(n);
                        }
                    }
                    if (dlyRva != 0)
                    {
                        long o = off(dlyRva);
                        for (int i = 0; o >= 0 && i < 512; i++)
                        {
                            fs.Position = o + i * 32 + 4; uint nameRva = br.ReadUInt32();
                            if (nameRva == 0) break;
                            string n = cstr(off(nameRva)); if (!string.IsNullOrEmpty(n)) r.Imports.Add(n);
                        }
                    }
                    r.Ok = true;
                }
            }
            catch { }
            return r;
        }
    }

    // ------------------------------------------------------------------ modèle de jeu
    public enum GameStatus { NonAnalyse, Compatible, ATester, FeederPossible, PeuProbable, Bloque, NonCompatible, Installe, AutreMod, Incomplet, ExeIntrouvable }

    public class Game
    {
        public string Name, Store, AppId, Root, ExePath;
        public string ExeDir { get { return ExePath == null ? null : Path.GetDirectoryName(ExePath); } }
        public string ExeName { get { return ExePath == null ? "" : Path.GetFileName(ExePath); } }
        public bool Is64;
        public List<string> Apis = new List<string>();
        public List<string> Upscalers = new List<string>();
        public List<string> AntiCheat = new List<string>();
        public List<string> ForeignMods = new List<string>();     // fichiers de mod qu'on mettra de côté
        public List<string> TakenProxyNames = new List<string>(); // DLL proxy inconnues (ex. CET version.dll) à ne pas écraser
        public bool OneClickInstalled;
        public List<CleanItem> CleanPlan = new List<CleanItem>(); // traces d'autres mods/launchers (lecture seule)
        public string Engine;            // Unity / Unreal / null
        public string ApiSource;         // "log du jeu" quand l'API vient du log Unity/Unreal, sinon deviné
        public bool Dxvk;                // DX11 traduit en Vulkan par dxvk (dxgi.dll/d3d11.dll de dxvk)
        public Manifest Installed;
        public GameStatus Status;
        public string Note, RecommendedProxy;

        public string ApiText { get { return Apis.Count == 0 ? "?" : string.Join("/", Apis); } }
        public string UpscalerText { get { return Upscalers.Count == 0 ? "aucun" : string.Join(", ", Upscalers); } }
        public string StatusText
        {
            get
            {
                switch (Status)
                {
                    case GameStatus.Compatible: return "Compatible";
                    case GameStatus.ATester: return "À tester";
                    case GameStatus.FeederPossible: return "Feeder possible";
                    case GameStatus.PeuProbable: return "Peu probable";
                    case GameStatus.Bloque: return "Bloqué (anti-cheat)";
                    case GameStatus.NonCompatible: return "Non compatible";
                    case GameStatus.Installe: return "Installé (" + (Installed != null ? Installed.ModeLabel : "?") + ")";
                    case GameStatus.AutreMod: return "Autre mod présent";
                    case GameStatus.Incomplet: return "Install incomplète";
                    case GameStatus.NonAnalyse: return "Analyse échouée";
                    default: return "Exe introuvable";
                }
            }
        }
    }

    // ------------------------------------------------------------------ connaissances locales (tes tests)
    public static class KnownGames
    {
        public class Info { public string Note, Proxy; }
        static readonly Dictionary<string, Info> Map = new Dictionary<string, Info>(StringComparer.OrdinalIgnoreCase)
        {
            { "PagodaSteam-Win64-Shipping.exe", new Info { Note = "Dead as Disco : validé sur ta 4070 — OptiScaler F5 (test 03, preset B, ~3,5 ms NR) et RenoDX (test 04)." } },
            { "Cyberpunk2077.exe", new Info { Note = "Cyberpunk 2077 : 4 crashs GPU (19 et 24 oct., aussi en vanilla), toujours juste après une recréation du modèle NR avec la Ray Reconstruction active (Preset A, pleine résolution → DEVICE_HUNG). Coupe la Ray Reconstruction dans le jeu, garde le « profil sûr RR » et évite de changer les réglages NR en pleine partie. Proxy dbghelp.dll pour cohabiter avec CET/RED4ext.", Proxy = "dbghelp.dll" } },
            { "SpeciesUnknown-Win64-Shipping.exe", new Info { Note = "Species: Unknown : Feeder F5Low stable 2 h 36 (NR ≈ 6 ms en Preset A), puis crash GPU signalé par Unreal (0x8000) pendant des changements de scène, le 24 oct. La profondeur du jeu n'est jamais trouvée : décoche « Utiliser la profondeur du jeu » pour ce jeu (même qualité, moins de surcharge)." } },
            { "CrimsonDesert.exe", new Info { Note = "Crimson Desert : crash le 24 oct. avec la Ray Reconstruction active, Preset A, la génération d'images du jeu et 8 recréations du modèle en 1 minute (réglages changés en jeu). Coupe la RR, garde le « profil sûr RR » et règle l'effet dans le launcher plutôt qu'en jeu." } },
        };
        public static Info Get(string exeName)
        {
            Info i; return exeName != null && Map.TryGetValue(exeName, out i) ? i : null;
        }
    }

    // ------------------------------------------------------------------ scan des bibliothèques
    public static class Scanner
    {
        static readonly string[] SkipDirs = { "_commonredist", "redist", "redistributables", "directx", "support", "installer", "installers", "__installer",
            "easyanticheat", "easyanticheat_eos", "battleye", "crashreportclient", "prereq", "prerequisites", "vcredist", "dotnet",
            AppPaths.StateDirName.ToLowerInvariant(), "_dlss5_backup", "optiscaler", "reshade-shaders", "__overlay", "dlssnr-capture" };
        static readonly string[] BadExeWords = { "unins", "setup", "install", "crash", "report", "redist", "vcredist", "dxsetup", "prereq", "helper",
            "service", "updater", "update", "cefprocess", "protected_game", "easyanticheat", "beservice", "eaanticheat", "unitycrash", "dowser",
            "quicksfv", "touchup", "cleanup", "launcher", "bootstrap", "benchmark", "config", "settings", "modmanager", "server", "editor", "7z", "cmd" };
        static readonly string[] ProxyNames = { "dxgi.dll", "winmm.dll", "version.dll", "dbghelp.dll", "d3d12.dll", "wininet.dll", "winhttp.dll", "d3d11.dll", "d3d9.dll", "opengl32.dll" };
        static readonly string[] ForeignFiles = { "OptiScaler.ini", "ReShade.ini", "ReShadePreset.ini", "nvngx.dll_dlssnr.dll", "nvngx_dlssnr.dll",
            "renodx-dlss5.addon64", "dlss5-feed.addon64", "dlss5-feed.addon32", "dlss5-feed.cfg", "Remove_OptiScaler.bat", "Remove OptiScaler.bat", "OptiScaler.asi", "nvngx.dll" };
        static readonly string[] ForeignDirs = { "OptiScaler", "reshade-shaders" };

        // ---- Steam
        public static List<Game> Steam()
        {
            var list = new List<Game>();
            var libs = new List<string>();
            string sp = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) as string;
            if (string.IsNullOrEmpty(sp)) sp = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath", null) as string;
            if (string.IsNullOrEmpty(sp)) { Log.W("Steam : introuvable dans le registre."); return list; }
            sp = sp.Replace('/', '\\');
            libs.Add(sp);
            string vdf = Path.Combine(sp, "steamapps", "libraryfolders.vdf");
            if (File.Exists(vdf))
                foreach (Match m in Regex.Matches(File.ReadAllText(vdf), "\"path\"\\s+\"(.+?)\""))
                    libs.Add(m.Groups[1].Value.Replace("\\\\", "\\"));
            foreach (var lib in libs.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                string apps = Path.Combine(lib, "steamapps");
                if (!Directory.Exists(apps)) continue;
                foreach (var acf in Directory.GetFiles(apps, "appmanifest_*.acf"))
                {
                    try
                    {
                        string t = File.ReadAllText(acf);
                        string id = Rx(t, "appid"), name = Rx(t, "name"), dir = Rx(t, "installdir");
                        if (name == null || dir == null) continue;
                        if (name.StartsWith("Steamworks") || name.StartsWith("Proton") || name.StartsWith("Steam Linux Runtime") || name == "SteamVR") continue;
                        string root = Path.Combine(apps, "common", dir);
                        if (!Directory.Exists(root)) continue;
                        list.Add(new Game { Name = name, Store = "Steam", AppId = id, Root = root });
                    }
                    catch { }
                }
            }
            Log.W("Steam : " + list.Count + " jeux dans " + libs.Distinct(StringComparer.OrdinalIgnoreCase).Count() + " bibliothèque(s).");
            return list;
        }
        static string Rx(string t, string key)
        {
            var m = Regex.Match(t, "\"" + key + "\"\\s+\"(.*?)\"", RegexOptions.IgnoreCase);
            return m.Success ? m.Groups[1].Value : null;
        }

        // ---- Epic (bonus)
        public static List<Game> Epic()
        {
            var list = new List<Game>();
            string d = @"C:\ProgramData\Epic\EpicGamesLauncher\Data\Manifests";
            if (!Directory.Exists(d)) return list;
            foreach (var f in Directory.GetFiles(d, "*.item"))
            {
                try
                {
                    string t = File.ReadAllText(f);
                    var n = Regex.Match(t, "\"DisplayName\"\\s*:\\s*\"(.*?)\"");
                    var p = Regex.Match(t, "\"InstallLocation\"\\s*:\\s*\"(.*?)\"");
                    if (!n.Success || !p.Success) continue;
                    string root = p.Groups[1].Value.Replace("\\\\", "\\").Replace('/', '\\');
                    if (Directory.Exists(root)) list.Add(new Game { Name = Regex.Unescape(n.Groups[1].Value), Store = "Epic", Root = root });
                }
                catch { }
            }
            if (list.Count > 0) Log.W("Epic : " + list.Count + " jeux.");
            return list;
        }

        // ---- GOG (bonus)
        public static List<Game> Gog()
        {
            var list = new List<Game>();
            try
            {
                using (var k = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\GOG.com\Games"))
                {
                    if (k == null) return list;
                    foreach (var sub in k.GetSubKeyNames())
                        using (var g = k.OpenSubKey(sub))
                        {
                            string n = g.GetValue("gameName") as string, p = g.GetValue("path") as string;
                            if (n != null && p != null && Directory.Exists(p)) list.Add(new Game { Name = n, Store = "GOG", Root = p });
                        }
                }
            }
            catch { }
            if (list.Count > 0) Log.W("GOG : " + list.Count + " jeux.");
            return list;
        }

        public static Game Manual(string folder)
        {
            return new Game { Name = Path.GetFileName(folder.TrimEnd('\\')), Store = "Manuel", Root = folder };
        }

        // ---- trouver l'exe principal
        public static string FindMainExe(string root)
        {
            string best = null; double bestScore = double.MinValue;
            foreach (var f in Walk(root, 6, 30000, true))
            {
                if (!f.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) continue;
                string n = Path.GetFileNameWithoutExtension(f).ToLowerInvariant();
                long len; try { len = new FileInfo(f).Length; } catch { continue; }
                double s = len / 1048576.0;
                if (n.EndsWith("-win64-shipping") || n.EndsWith("-wingdk-shipping")) s += 800;
                else if (n.Contains("shipping")) s += 400;
                string dl = f.ToLowerInvariant();
                if (dl.Contains(@"\binaries\win64\") || dl.Contains(@"\bin\x64\")) s += 150;
                if (dl.Contains(@"\engine\binaries\")) s -= 2000;
                foreach (var w in BadExeWords) if (n.Contains(w)) { s -= 1500; break; }
                if (len < 1048576) s -= 300;
                if (s > bestScore) { bestScore = s; best = f; }
            }
            return best;
        }

        public static IEnumerable<string> Walk(string root, int maxDepth, int maxFiles) { return Walk(root, maxDepth, maxFiles, false); }
        public static IEnumerable<string> Walk(string root, int maxDepth, int maxFiles, bool skipThirdParty)
        {
            var stack = new Stack<KeyValuePair<string, int>>();
            stack.Push(new KeyValuePair<string, int>(root, 0));
            int count = 0;
            while (stack.Count > 0)
            {
                var cur = stack.Pop();
                string[] files = null, dirs = null;
                try { files = Directory.GetFiles(cur.Key); dirs = Directory.GetDirectories(cur.Key); } catch { continue; }
                foreach (var f in files) { if (++count > maxFiles) yield break; yield return f; }
                if (cur.Value >= maxDepth) continue;
                foreach (var d in dirs)
                {
                    string dn = Path.GetFileName(d).ToLowerInvariant();
                    if (SkipDirs.Contains(dn)) continue;
                    if (skipThirdParty && (dn == "thirdparty" || dn == "plugins")) continue;
                    stack.Push(new KeyValuePair<string, int>(d, cur.Value + 1));
                }
            }
        }

        // ---- analyse complète d'un jeu
        public static void Analyze(Game g)
        {
            g.Apis.Clear(); g.Upscalers.Clear(); g.AntiCheat.Clear(); g.ForeignMods.Clear(); g.TakenProxyNames.Clear();
            g.Installed = null; g.OneClickInstalled = false; g.Engine = null; g.Dxvk = false; g.ApiSource = null; g.Note = null; g.RecommendedProxy = null; g.CleanPlan = new List<CleanItem>();
            if (g.ExePath == null || !File.Exists(g.ExePath)) g.ExePath = FindMainExe(g.Root);
            if (g.ExePath == null) { g.Status = GameStatus.ExeIntrouvable; return; }
            string exeDir = g.ExeDir;

            // API. Les moteurs modernes chargent DirectX dynamiquement : il n'apparaît PAS dans les imports.
            // (Unity importe opengl32.dll même quand il tourne en DX11 — c'était le bug du feeder.)
            var pe = Pe.Read(g.ExePath);
            g.Is64 = pe.Is64;
            var imports = new HashSet<string>(pe.Imports, StringComparer.OrdinalIgnoreCase);
            bool isUnity = File.Exists(Path.Combine(exeDir, "UnityPlayer.dll")) || Directory.GetDirectories(exeDir, "*_Data").Any(d => Directory.Exists(Path.Combine(d, "Managed")) || File.Exists(Path.Combine(d, "globalgamemanagers")));
            bool isUnreal = g.ExeName.IndexOf("-Win64-Shipping", StringComparison.OrdinalIgnoreCase) >= 0 || g.ExeName.IndexOf("-WinGDK-Shipping", StringComparison.OrdinalIgnoreCase) >= 0
                            || Directory.Exists(Path.Combine(g.Root, "Engine", "Binaries"));
            g.Engine = isUnity ? "Unity" : isUnreal ? "Unreal" : null;
            // imports des autres DLL du jeu (moteur de rendu séparé, UnityPlayer.dll…), sauf les DLL de mods
            try
            {
                int nDll = 0;
                foreach (var dll in Directory.GetFiles(exeDir, "*.dll"))
                {
                    string n = Path.GetFileName(dll);
                    if (ProxyNames.Contains(n, StringComparer.OrdinalIgnoreCase) || n.StartsWith("nvngx", StringComparison.OrdinalIgnoreCase) || n.StartsWith("sl.", StringComparison.OrdinalIgnoreCase)) continue;
                    if (++nDll > 60) break;
                    foreach (var i in Pe.Read(dll).Imports) imports.Add(i);
                }
            }
            catch { }
            bool d3d12 = imports.Contains("d3d12.dll") || Directory.Exists(Path.Combine(exeDir, "D3D12"));
            bool imD3d11 = imports.Contains("d3d11.dll");
            bool dxgi = imports.Contains("dxgi.dll") || imports.Contains("d3d10.dll") || imports.Contains("d3d10_1.dll");
            if (d3d12) g.Apis.Add("DX12");
            if (imD3d11 || (dxgi && !d3d12)) g.Apis.Add("DX11");
            if (g.Apis.Count == 0 && isUnity) g.Apis.Add("DX11");                         // défaut de Unity sur Windows
            if (g.Apis.Count == 0 && isUnreal) { g.Apis.Add("DX12"); g.Apis.Add("DX11"); } // UE5 = DX12, UE4 = DX11
            if (imports.Contains("vulkan-1.dll") && g.Apis.Count == 0) g.Apis.Add("Vulkan");
            if (g.Apis.Count == 0 && imports.Contains("d3d9.dll")) g.Apis.Add("DX9");
            // OpenGL seulement si rien d'autre : pas de DirectX, pas de moteur connu qui charge DirectX tout seul
            if (g.Apis.Count == 0 && imports.Contains("opengl32.dll")) g.Apis.Add("OpenGL");
            // Le mieux : ce que le jeu a vraiment utilisé à sa dernière partie (Player.log de Unity, log d'Unreal)
            try
            {
                string real = isUnity ? EngineLogs.UnityApi(g) : isUnreal ? EngineLogs.UnrealApi(g) : null;
                if (real != null) { g.Apis.Clear(); g.Apis.Add(real); g.ApiSource = "log du jeu"; }
            }
            catch { }

            // État d'installation (lu avant le scan pour ignorer nos propres fichiers)
            g.Installed = Manifest.Load(exeDir);
            var ours = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (g.Installed != null)
                foreach (var e in g.Installed.Entries)
                    if (e.Kind == "ADDED" || e.Kind == "REPLACED") ours.Add(Path.Combine(exeDir, e.Rel));

            // Upscalers et anti-cheat dans tout le dossier du jeu
            var ups = new HashSet<string>();
            foreach (var f in Walk(g.Root, 10, 60000))
            {
                if (ours.Contains(f)) continue;
                string n = Path.GetFileName(f).ToLowerInvariant();
                if (n == "nvngx_dlss.dll") ups.Add("DLSS");
                else if (n == "nvngx_dlssd.dll") ups.Add("DLSS-RR");
                else if (n == "sl.interposer.dll") ups.Add("Streamline");
                else if (n.StartsWith("libxess") && n.EndsWith(".dll")) ups.Add("XeSS");
                else if ((n.StartsWith("ffx_fsr") || n.StartsWith("amd_fidelityfx")) && n.EndsWith(".dll")) ups.Add("FSR");
                if (n.StartsWith("easyanticheat") || n == "start_protected_game.exe") Add(g.AntiCheat, "EasyAntiCheat");
                else if (n.StartsWith("beservice") || n.StartsWith("beclient") || n.StartsWith("battleye")) Add(g.AntiCheat, "BattlEye");
                else if (n.StartsWith("eaanticheat")) Add(g.AntiCheat, "EA AntiCheat");
                else if (n.StartsWith("pnkbstr")) Add(g.AntiCheat, "PunkBuster");
                else if (n.StartsWith("xigncode") || n.StartsWith("gameguard") || n.StartsWith("npgame") || n.StartsWith("mhyprot") || n.StartsWith("ace-") || n.Contains("anticheatexpert"))
                    Add(g.AntiCheat, "Anti-cheat noyau");
            }
            foreach (var d in new[] { "EasyAntiCheat", "EasyAntiCheat_EOS", "BattlEye" })
                if (Directory.Exists(Path.Combine(g.Root, d)) || Directory.Exists(Path.Combine(exeDir, d))) Add(g.AntiCheat, d.StartsWith("Easy") ? "EasyAntiCheat" : "BattlEye");
            foreach (var u in new[] { "DLSS", "DLSS-RR", "Streamline", "FSR", "XeSS" }) if (ups.Contains(u)) g.Upscalers.Add(u);

            try { g.OneClickInstalled = Cleaner.FindOneClickDirs(g).Any(d => File.Exists(Path.Combine(d, "manifest.json"))); } catch { }

            // Mods étrangers (seulement si ce n'est pas nous qui les avons mis)
            if (g.Installed == null)
            {
                foreach (var p in ProxyNames)
                {
                    string fp = Path.Combine(exeDir, p);
                    if (!File.Exists(fp)) continue;
                    if (IsOptiOrReShade(fp)) g.ForeignMods.Add(p); else g.TakenProxyNames.Add(p);
                }
                foreach (var f in ForeignFiles) if (File.Exists(Path.Combine(exeDir, f))) g.ForeignMods.Add(f);
                foreach (var f in Directory.GetFiles(exeDir, "renodx-*.addon*")) { string n = Path.GetFileName(f); if (!g.ForeignMods.Contains(n)) g.ForeignMods.Add(n); }
                foreach (var d in ForeignDirs) if (Directory.Exists(Path.Combine(exeDir, d))) g.ForeignMods.Add(d + "\\");
            }
            string d3d11 = Path.Combine(exeDir, "d3d11.dll");
            if (File.Exists(d3d11) && !ours.Contains(d3d11))
            {
                string desc = "";
                try { var v = FileVersionInfo.GetVersionInfo(d3d11); desc = (v.ProductName + "|" + v.FileDescription + "|" + v.OriginalFilename).ToLowerInvariant(); } catch { }
                g.Dxvk = desc.Contains("dxvk") || File.Exists(Path.Combine(exeDir, "dxgi.dll")) && !Scanner.IsOptiOrReShade(Path.Combine(exeDir, "dxgi.dll")) && !ours.Contains(Path.Combine(exeDir, "dxgi.dll"));
            }

            if (g.Installed == null)
                try { g.CleanPlan = Cleaner.Plan(g); } catch (Exception ex) { Log.W("Recherche de traces impossible pour " + g.Name + " : " + ex.Message); }

            var k = KnownGames.Get(g.ExeName);
            if (k != null) { g.Note = k.Note; g.RecommendedProxy = k.Proxy; }

            // Statut
            bool hasUpscaler = g.Upscalers.Any(u => u == "DLSS" || u == "FSR" || u == "XeSS" || u == "Streamline");
            if (g.Installed != null) g.Status = g.Installed.Complete ? GameStatus.Installe : GameStatus.Incomplet;
            else if (g.OneClickInstalled) g.Status = GameStatus.AutreMod;
            else if (!g.Is64) g.Status = GameStatus.NonCompatible;
            else if (g.AntiCheat.Count > 0) g.Status = GameStatus.Bloque;
            else if (g.ForeignMods.Count > 0 || g.CleanPlan.Count > 0) g.Status = GameStatus.AutreMod;
            else if (g.Upscalers.Contains("DLSS") && g.Apis.Contains("DX12")) g.Status = GameStatus.Compatible;
            else if (hasUpscaler) g.Status = GameStatus.ATester;
            else if (g.Apis.Count == 0 || g.Apis.Contains("DX12") || g.Apis.Contains("DX11") || g.Apis.Contains("Vulkan") || g.Apis.Contains("OpenGL"))
                g.Status = GameStatus.FeederPossible;
            else g.Status = GameStatus.PeuProbable; // DX9 seul : pas encore géré
        }

        static void Add(List<string> l, string s) { if (!l.Contains(s)) l.Add(s); }

        public static bool IsOptiOrReShade(string path)
        {
            try
            {
                var v = FileVersionInfo.GetVersionInfo(path);
                string all = (v.ProductName + "|" + v.OriginalFilename + "|" + v.FileDescription + "|" + v.InternalName).ToLowerInvariant();
                if (all.Contains("optiscaler") || all.Contains("reshade")) return true;
            }
            catch { }
            return false;
        }
    }

    // ------------------------------------------------------------------ API réelle d'après les logs des moteurs
    public static class EngineLogs
    {
        static string Read(string p)
        {
            try
            {
                using (var fs = new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var rd = new StreamReader(fs))
                {
                    var buf = new char[400000]; int n = rd.Read(buf, 0, buf.Length); return new string(buf, 0, n);
                }
            }
            catch { return null; }
        }

        // %USERPROFILE%\AppData\LocalLow\<société>\<produit>\Player.log  (société/produit dans <Data>\app.info)
        public static string UnityApi(Game g)
        {
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrEmpty(local)) return null;
            string low = Path.Combine(Path.GetDirectoryName(local.TrimEnd('\\')), "LocalLow");
            var data = Directory.GetDirectories(g.ExeDir, "*_Data").FirstOrDefault();
            if (data == null || !Directory.Exists(low)) return null;
            string info = Path.Combine(data, "app.info");
            var cands = new List<string>();
            if (File.Exists(info))
            {
                var l = File.ReadAllLines(info);
                if (l.Length >= 2)
                {
                    string comp = Path.Combine(low, Clean(l[0].Trim()));
                    cands.Add(Path.Combine(comp, Clean(l[1].Trim())));
                    if (Directory.Exists(comp)) cands.AddRange(Directory.GetDirectories(comp));
                }
            }
            string dataName = Path.GetFileName(data);
            foreach (var d in cands.Distinct())
                foreach (var f in new[] { "Player.log", "Player-prev.log" })
                {
                    string t = Read(Path.Combine(d, f));
                    if (t == null || t.IndexOf(dataName, StringComparison.OrdinalIgnoreCase) < 0 && cands.Count > 2) continue;
                    if (Regex.IsMatch(t, @"Version:\s+Direct3D 12")) return "DX12";
                    if (Regex.IsMatch(t, @"Version:\s+Direct3D 11")) return "DX11";
                    if (Regex.IsMatch(t, @"Vulkan API version|Version:\s+Vulkan")) return "Vulkan";
                    if (Regex.IsMatch(t, @"Version:\s+OpenGL")) return "OpenGL";
                }
            return null;
        }

        // %LOCALAPPDATA%\<Projet>\Saved\Logs\*.log  : "Using Default RHI: D3D12"
        public static string UnrealApi(Game g)
        {
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrEmpty(local)) return null;
            var names = new List<string>();
            var m = Regex.Match(g.ExeName, @"^(.+?)-Win(64|GDK)-Shipping", RegexOptions.IgnoreCase);
            if (m.Success) names.Add(m.Groups[1].Value);
            // dossier du projet : Root\<Projet>\Binaries\Win64
            var bin = Path.GetDirectoryName(Path.GetDirectoryName(g.ExeDir));
            if (bin != null) names.Add(Path.GetFileName(bin));
            foreach (var n in names.Distinct())
            {
                string logs = Path.Combine(local, n, "Saved", "Logs");
                if (!Directory.Exists(logs)) continue;
                foreach (var f in Directory.GetFiles(logs, "*.log").OrderByDescending(File.GetLastWriteTimeUtc).Take(3))
                {
                    string t = Read(f); if (t == null) continue;
                    var r = Regex.Match(t, @"Using (?:Default|Selected) RHI: (D3D12|D3D11|Vulkan|OpenGL)", RegexOptions.IgnoreCase);
                    if (r.Success) return Map(r.Groups[1].Value);
                    if (t.Contains("LogD3D12RHI")) return "DX12";
                    if (t.Contains("LogD3D11RHI")) return "DX11";
                    if (t.Contains("LogVulkanRHI")) return "Vulkan";
                }
            }
            return null;
        }
        // Logs du moteur lui-même (le détail des crashs GPU y est, pas dans OptiScaler.log)
        public static readonly List<string> LastSearched = new List<string>();
        public static List<string> EngineLogFiles(Game g)
        {
            var res = new List<string>();
            try
            {
                string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (string.IsNullOrEmpty(local) || g.ExePath == null) return res;
                LastSearched.Clear();
                if (g.Engine == "Unreal")
                {
                    var names = new List<string>();
                    var m = Regex.Match(g.ExeName, @"^(.+?)-Win(64|GDK)-Shipping", RegexOptions.IgnoreCase);
                    if (m.Success) names.Add(m.Groups[1].Value);
                    var bin = Path.GetDirectoryName(Path.GetDirectoryName(g.ExeDir));
                    if (bin != null) names.Add(Path.GetFileName(bin));
                    // Les jeux Unreal ne rangent pas tous leurs logs au même endroit : on essaie les emplacements courants.
                    string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    string user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    var bases = new List<string> { local };
                    if (!string.IsNullOrEmpty(docs)) { bases.Add(Path.Combine(docs, "My Games")); bases.Add(docs); }
                    if (!string.IsNullOrEmpty(user)) bases.Add(Path.Combine(user, "Saved Games"));
                    // nom du jeu « nettoyé » (Species: Unknown -> SpeciesUnknown) et dossiers de LOCALAPPDATA qui y ressemblent
                    string norm = Regex.Replace(g.Name ?? "", @"[^A-Za-z0-9]", "").ToLowerInvariant();
                    if (norm.Length >= 4) names.Add(Regex.Replace(g.Name, @"[^A-Za-z0-9]", ""));
                    try
                    {
                        foreach (var d in Directory.GetDirectories(local))
                        {
                            string dn = Regex.Replace(Path.GetFileName(d), @"[^A-Za-z0-9]", "").ToLowerInvariant();
                            if (dn.Length >= 4 && names.Any(n => Regex.Replace(n, @"[^A-Za-z0-9]", "").ToLowerInvariant() == dn || (norm.Length >= 4 && (dn == norm || dn.StartsWith(norm) || norm.StartsWith(dn)))))
                                names.Add(Path.GetFileName(d));
                        }
                    }
                    catch { }
                    foreach (var bs in bases)
                        foreach (var n in names.Distinct(StringComparer.OrdinalIgnoreCase))
                        {
                            string saved = Path.Combine(bs, n, "Saved");
                            LastSearched.Add(saved);
                            string logs = Path.Combine(saved, "Logs");
                            if (Directory.Exists(logs)) res.AddRange(Directory.GetFiles(logs, "*.log").OrderByDescending(File.GetLastWriteTimeUtc).Take(2));
                            string crashes = Path.Combine(saved, "Crashes");
                            if (Directory.Exists(crashes))
                            {   // dernier rapport du Crash Reporter d'Unreal : son log et son CrashContext (message d'erreur)
                                var last = Directory.GetDirectories(crashes).OrderByDescending(Directory.GetLastWriteTimeUtc).FirstOrDefault();
                                if (last != null)
                                {
                                    res.AddRange(Directory.GetFiles(last, "*.log"));
                                    res.AddRange(Directory.GetFiles(last, "CrashContext.runtime-xml"));
                                }
                            }
                        }
                    // logs dans le dossier d'installation (rare)
                    string inst = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(g.ExeDir)) ?? g.Root, "Saved", "Logs");
                    LastSearched.Add(inst);
                    if (Directory.Exists(inst)) res.AddRange(Directory.GetFiles(inst, "*.log").OrderByDescending(File.GetLastWriteTimeUtc).Take(2));
                    res = res.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                }
                else if (g.Engine == "Unity")
                {
                    string low = Path.Combine(Path.GetDirectoryName(local.TrimEnd('\\')), "LocalLow");
                    var data = Directory.GetDirectories(g.ExeDir, "*_Data").FirstOrDefault();
                    string info = data == null ? null : Path.Combine(data, "app.info");
                    if (info != null && File.Exists(info))
                    {
                        var l = File.ReadAllLines(info);
                        if (l.Length >= 2)
                        {
                            string dir = Path.Combine(low, Clean(l[0].Trim()), Clean(l[1].Trim()));
                            LastSearched.Add(dir);
                            foreach (var f in new[] { "Player.log", "Player-prev.log" }) if (File.Exists(Path.Combine(dir, f))) res.Add(Path.Combine(dir, f));
                        }
                    }
                }
            }
            catch { }
            return res;
        }

        public static List<string> EngineErrors(string path, int max)
        {
            var hits = new List<string>();
            string t = null;
            try
            {   // la fin du log (le crash est à la fin) : les 3 derniers Mo au plus
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    long start = Math.Max(0, fs.Length - 3 * 1024 * 1024); fs.Position = start;
                    using (var rd = new StreamReader(fs)) t = rd.ReadToEnd();
                }
            }
            catch { }
            if (t == null) return hits;
            var rx = new Regex(@"<ErrorMessage>|GPU ?crash|device (removed|hung|lost)|DXGI_ERROR|out of (video )?memory|Fatal error|Unhandled Exception|Assertion failed|LogD3D12RHI: Error|LogRHI: Error|D3D12 call|d3d1[12]: .*(removed|failed)|Crash in runnable|EXCEPTION_ACCESS_VIOLATION", RegexOptions.IgnoreCase);
            foreach (var line in t.Replace("\r\n", "\n").Split('\n'))
                if (rx.IsMatch(line)) hits.Add(line.Length > 240 ? line.Substring(0, 240) + "…" : line);
            return hits.Skip(Math.Max(0, hits.Count - max)).ToList();
        }

        static string Map(string rhi) { rhi = rhi.ToUpperInvariant(); return rhi == "D3D12" ? "DX12" : rhi == "D3D11" ? "DX11" : rhi == "VULKAN" ? "Vulkan" : "OpenGL"; }
        static string Clean(string s) { foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_'); return s; }
    }

    // ------------------------------------------------------------------ GPU
    public static class Gpu
    {
        public static string Vendor = "NVIDIA";   // NVIDIA, AMD, Intel ou "?"
        public static bool IsAmdMode { get { return Vendor == "AMD" || Vendor == "Intel"; } }
        public static int Series;                  // 20, 30, 40, 50… (RTX), 0 si inconnu
        public static string ForceNr;              // "openNR" ou "origine" (launcher-config.ini : dllNr=…)
        public static string Name;                 // nom de la carte (ex. « NVIDIA GeForce RTX 4070 »), pour l'historique
        // RTX 50 (Blackwell) et AMD/Intel : la 310.9.16 NVIDIA d'origine. RTX 20/30/40 : ta build OpenNR compat-fp16.
        public static bool UseOriginalNr
        {
            get
            {
                if (ForceNr == "origine") return true;
                if (ForceNr == "openNR") return IsAmdMode; // le port AMD exige la 310.9 d'origine
                return IsAmdMode || Series >= 50;
            }
        }
        public static string NrDllPath { get { return UseOriginalNr ? AppPaths.NrOriginal : AppPaths.NrDll; } }
        public static string NrLabel { get { return UseOriginalNr ? "310.9.16 NVIDIA d'origine" : "OpenNR 310.9.16 compat-fp16"; } }

        // Cherche la carte graphique : une NVIDIA d'abord, sinon une Radeon / Arc (on ignore les iGPU si une dGPU existe).
        public static string Describe(out bool driverTooOld)
        {
            driverTooOld = false;
            string amd = null, intel = null;
            try
            {
                using (var cls = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}"))
                {
                    if (cls == null) { Vendor = "?"; return "GPU inconnu"; }
                    foreach (var sub in cls.GetSubKeyNames())
                    {
                        if (!Regex.IsMatch(sub, @"^\d{4}$")) continue;
                        using (var k = cls.OpenSubKey(sub))
                        {
                            string desc = k == null ? null : k.GetValue("DriverDesc") as string;
                            if (desc == null) continue;
                            string ver = k.GetValue("DriverVersion") as string ?? "";
                            if (desc.IndexOf("NVIDIA", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                string nv = NvVersion(ver);
                                double d;
                                if (nv != null && double.TryParse(nv, NumberStyles.Float, CultureInfo.InvariantCulture, out d) && d < 616.56) driverTooOld = true;
                                Vendor = "NVIDIA";
                                var sm = Regex.Match(desc, @"RTX\s*(?:PRO\s*)?(\d)0\d0", RegexOptions.IgnoreCase);
                                Series = sm.Success ? int.Parse(sm.Groups[1].Value) * 10 : 0;
                                Name = desc;
                                return desc + (nv != null ? " — pilote " + nv : "");
                            }
                            if (amd == null && (desc.IndexOf("Radeon", StringComparison.OrdinalIgnoreCase) >= 0 || desc.IndexOf("AMD", StringComparison.OrdinalIgnoreCase) >= 0))
                            {
                                // une Radeon intégrée (« AMD Radeon(TM) Graphics ») passe après une vraie carte RX
                                if (amd == null || desc.IndexOf(" RX ", StringComparison.OrdinalIgnoreCase) >= 0) amd = desc;
                            }
                            if (desc.IndexOf("Arc", StringComparison.OrdinalIgnoreCase) >= 0 && desc.IndexOf("Intel", StringComparison.OrdinalIgnoreCase) >= 0) intel = desc;
                        }
                    }
                }
            }
            catch { }
            if (amd != null) { Vendor = "AMD"; Name = amd; return amd + " — mode AMD (port vendor-neutral)"; }
            if (intel != null) { Vendor = "Intel"; Name = intel; return intel + " — mode Intel (port vendor-neutral)"; }
            Vendor = "?";
            return "Aucun GPU NVIDIA / AMD détecté";
        }
        // 32.0.16.1714 -> 617.14
        static string NvVersion(string v)
        {
            string digits = v.Replace(".", "");
            if (digits.Length < 5) return null;
            string last = digits.Substring(digits.Length - 5);
            return last.Substring(0, 3) + "." + last.Substring(3);
        }
    }
}
