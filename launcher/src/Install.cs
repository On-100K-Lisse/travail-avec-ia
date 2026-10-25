// Licence MIT (voir LICENSE-MIT du dépôt travail-avec-ia). Code écrit par Claude (Anthropic) pour On-100-K-Lisse.
// DLSS5 Launcher — réglages, manifeste, installation et réinitialisation.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace Dlss5Launcher
{
    public enum InstallMode { Auto, OptiScaler, RenoDX, Feeder, FeederReShade }

    // ------------------------------------------------------------------ réglages
    public class NrSettings
    {
        // OptiScaler F5 [DlssNr]
        public int Preset = 2;            // -1 auto, 0 la DLL choisit, 1 A (gros modèle), 2 B (petit modèle)
        public int Style = -1;            // -1 auto, 0 standard, 1 naturel, 2 cinématique
        public int Passes = 1;            // 1..3
        public decimal WorkingScale = 1.0m;
        public bool RunBeforeSR = true;
        public decimal Intensity = 1.0m, LocalStructure = 1.0m, LocalTone = 1.0m, SkinStructure = -1.0m;
        public bool AutoMask = true;
        public decimal TransferStrength = 1.0m, ColourStrength = 1.0m, MaxRatio = 2.0m;
        public int VitEvery = 1;          // 1 = chaque image, 2 = une image sur deux (plus rapide)
        public bool DetailReuse = false;
        public bool AutoCapture = true;
        public bool FrameGenOff = true;   // ancien réglage (remplacé par FrameGen), gardé pour lire les vieilles configs
        public bool LogToFile = true;

        // Stabilité
        public bool RrSafe = true;         // avec Ray Reconstruction : Preset B + modèle ≤ 0,6 (crashs Cyberpunk / Crimson Desert)
        public bool CrashDump = false;     // [Log] CrashDump : OptiScaler_crash.dmp pour les rapports

        // Fluidité / FPS
        public int FrameGen = 0;           // 0 désactivée, 1 XeFG, 2 FSR FG (OptiScaler, entrée « upscaler »)
        public decimal FpsLimit = 0;       // [Framerate] FramerateLimit, 0 = aucune
        public bool ReflexBoost = false;   // NativeLowLatencyBoost
        public bool SpatialCompression = false; // compresse les bords de l'image pour le modèle

        // Couleur & peau
        public string Lut = "";            // fichier .cube dans OptiScaler\LUTs ("" = aucun)
        public decimal LutStrength = 1.0m;
        public bool SkinProtection = false, SkinToneEnabled = true;
        public decimal SkinDetail = 1.0m, SkinColour = 1.0m, EnvDetail = 1.0m, EnvColour = 1.0m;

        // RenoDX [RenoDX.DLSS5]
        public int RPreset = 2;
        public int RStyle = 1;
        public bool RNeuralUplift = true, RAutoMask = true, RUICorrection = true;
        public decimal RGlobalTone = 0.9m, RLocalStructure = 0.44m, RLocalTone = 1.22m, RSkinStructure = 1.16m;
        public int RDiffuseWhiteNits = 203;

        // Feeder F5Low (Optical F5Low d'OptiScaler, jeux sans upscaler)
        public decimal FWorkingScale = 0.67m; // l'image finie est en pleine résolution : 0,67 ≈ même coût que tes tests
        public bool FUseDepth = true;          // NativeDepthFinder
        public int FPath = 0;                  // 0 auto, 1 NR seul, 2 via upscaler virtuel
        public bool FSmooth = false;           // NativeUpscalerPass (anti-scintillement, +1,5-3 ms)
        public int FMotionProfile = 0;         // default, anime, realistic, sportswide, sportspan
        public static readonly string[] MotionProfiles = { "default", "anime", "realistic", "sportswide", "sportspan" };

        public NrSettings Clone() { return (NrSettings)MemberwiseClone(); }

        static string D(decimal v) { return v.ToString("0.###", CultureInfo.InvariantCulture); }
        static string B(bool v) { return v ? "true" : "false"; }

        public static readonly string[] FrameGenNames = { "nofg", "xefg", "fsrfg" };

        public string BuildOptiIni(string template) { return BuildOptiIni(template, null); }
        public string BuildOptiIni(string template, string exeDir)
        {
            string t = template;
            if (Gpu.IsAmdMode)
            {   // AMD / Intel : pas de DLSS SR. FSR4/3.1 via FidelityFX ; en DX11/Vulkan il faut un backend « _12 » pour que NR tourne.
                t = Ini.Set(t, "Upscalers", "Dx12Upscaler", "auto");
                t = Ini.Set(t, "Upscalers", "Dx11Upscaler", "ffx_12");
                t = Ini.Set(t, "Upscalers", "VulkanUpscaler", "ffx_12");
            }
            else
            {
                t = Ini.Set(t, "Upscalers", "Dx12Upscaler", "dlss");
                t = Ini.Set(t, "Upscalers", "Dx11Upscaler", "dlss_12");
            }
            t = Ini.Set(t, "Log", "CrashDump", B(CrashDump));
            if (FrameGen <= 0)
            {
                t = Ini.Set(t, "FrameGen", "Enabled", "false");
                t = Ini.Set(t, "FrameGen", "FGInput", "nofg"); t = Ini.Set(t, "FrameGen", "FGOutput", "nofg");
            }
            else
            {   // génération d'images d'OptiScaler (DLL XeFG / FSR FG déjà dans le dossier OptiScaler)
                t = Ini.Set(t, "FrameGen", "Enabled", "true");
                t = Ini.Set(t, "FrameGen", "FGInput", "upscaler");
                t = Ini.Set(t, "FrameGen", "FGOutput", FrameGenNames[Math.Min(FrameGen, FrameGenNames.Length - 1)]);
            }
            t = Ini.Set(t, "Framerate", "FramerateLimit", FpsLimit > 0 ? D(FpsLimit) : "auto");
            t = Ini.Set(t, "Plugins", "LoadAsiPlugins", "false");
            t = Ini.Set(t, "Log", "LogToFile", B(LogToFile));
            t = Ini.Set(t, "Log", "LogLevel", "2");
            t = Ini.Set(t, "ProcessFilter", "TargetProcessName", "auto");
            string s = "DlssNr";
            t = Ini.Set(t, s, "Enabled", "true");
            t = Ini.Set(t, s, "RunBeforeSR", B(RunBeforeSR));
            t = Ini.Set(t, s, "Passes", Passes.ToString());
            t = Ini.Set(t, s, "WorkingScale", D(WorkingScale));
            t = Ini.Set(t, s, "Preset", Preset < 0 ? "auto" : Preset.ToString());
            t = Ini.Set(t, s, "Style", Style < 0 ? "auto" : Style.ToString());
            t = Ini.Set(t, s, "Intensity", D(Intensity));
            t = Ini.Set(t, s, "LocalStructure", D(LocalStructure));
            t = Ini.Set(t, s, "LocalTone", D(LocalTone));
            t = Ini.Set(t, s, "SkinStructure", D(SkinStructure));
            t = Ini.Set(t, s, "AutoMask", B(AutoMask));
            t = Ini.Set(t, s, "TransferStrength", D(TransferStrength));
            t = Ini.Set(t, s, "ColourStrength", D(ColourStrength));
            t = Ini.Set(t, s, "MaxRatio", D(MaxRatio));
            t = Ini.Set(t, s, "VitEvery", VitEvery.ToString());
            t = Ini.Set(t, s, "VitEveryPlain", VitEvery.ToString());
            t = Ini.Set(t, s, "DetailReuse", B(DetailReuse));
            t = Ini.Set(t, s, "AutoCapture", B(AutoCapture));
            t = Ini.Set(t, s, "NativeLowLatencyBoost", B(ReflexBoost));
            t = Ini.Set(t, s, "SpatialCompression", B(SpatialCompression && !DetailReuse)); // les deux s'excluent
            string lut = "";
            if (!string.IsNullOrEmpty(Lut) && exeDir != null) lut = Path.Combine(exeDir, "OptiScaler", "LUTs", Lut);
            t = Ini.Set(t, s, "LutFile", lut);
            t = Ini.Set(t, s, "LutStrength", D(LutStrength));
            t = Ini.Set(t, s, "SkinProtection", B(SkinProtection));
            t = Ini.Set(t, s, "SkinToneEnabled", B(SkinToneEnabled));
            t = Ini.Set(t, s, "SkinDetail", D(SkinDetail));
            t = Ini.Set(t, s, "SkinColour", D(SkinColour));
            t = Ini.Set(t, s, "EnvironmentDetail", D(EnvDetail));
            t = Ini.Set(t, s, "EnvironmentColour", D(EnvColour));
            return t;
        }

        // ---- importer les réglages modifiés dans le menu du jeu (Inser / Début) vers le profil
        static decimal Dec(string ini, string sec, string key, decimal def)
        {
            string v = Ini.Get(ini, sec, key); decimal r;
            return v != null && decimal.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out r) ? r : def;
        }
        static int Int(string ini, string sec, string key, int def) { return (int)Dec(ini, sec, key, def); }
        static bool Bool(string ini, string sec, string key, bool def)
        {
            string v = Ini.Get(ini, sec, key);
            if (v == null) return def; v = v.ToLowerInvariant();
            return v == "true" || v == "1" ? true : v == "false" || v == "0" ? false : def;
        }

        public void ImportOpti(string ini, bool feeder)
        {
            var d = new NrSettings(); string s = "DlssNr";
            Preset = Int(ini, s, "Preset", -1); Style = Int(ini, s, "Style", -1);
            Passes = Math.Max(1, Math.Min(3, Int(ini, s, "Passes", 1)));
            decimal ws = Dec(ini, s, "WorkingScale", 1.0m);
            if (feeder) FWorkingScale = ws; else WorkingScale = ws;
            if (!feeder) RunBeforeSR = Bool(ini, s, "RunBeforeSR", false);
            Intensity = Dec(ini, s, "Intensity", d.Intensity); LocalStructure = Dec(ini, s, "LocalStructure", d.LocalStructure);
            LocalTone = Dec(ini, s, "LocalTone", d.LocalTone); SkinStructure = Dec(ini, s, "SkinStructure", d.SkinStructure);
            AutoMask = Bool(ini, s, "AutoMask", true);
            TransferStrength = Dec(ini, s, "TransferStrength", 1); ColourStrength = Dec(ini, s, "ColourStrength", 1); MaxRatio = Dec(ini, s, "MaxRatio", 2);
            VitEvery = Math.Max(1, Math.Min(2, Int(ini, s, "VitEvery", 2)));
            DetailReuse = Bool(ini, s, "DetailReuse", false); AutoCapture = Bool(ini, s, "AutoCapture", true);
            ReflexBoost = Bool(ini, s, "NativeLowLatencyBoost", false); SpatialCompression = Bool(ini, s, "SpatialCompression", false);
            string lut = Ini.Get(ini, s, "LutFile"); Lut = string.IsNullOrEmpty(lut) ? "" : Path.GetFileName(lut);
            LutStrength = Dec(ini, s, "LutStrength", 1);
            SkinProtection = Bool(ini, s, "SkinProtection", false); SkinToneEnabled = Bool(ini, s, "SkinToneEnabled", true);
            SkinDetail = Dec(ini, s, "SkinDetail", 1); SkinColour = Dec(ini, s, "SkinColour", 1);
            EnvDetail = Dec(ini, s, "EnvironmentDetail", 1); EnvColour = Dec(ini, s, "EnvironmentColour", 1);
            if (feeder)
            {
                FUseDepth = Bool(ini, s, "NativeDepthFinder", true);
                FSmooth = Bool(ini, s, "NativeUpscalerPass", false);
                int mp = Array.IndexOf(MotionProfiles, (Ini.Get(ini, s, "NativeMotionProfile") ?? "default").ToLowerInvariant());
                FMotionProfile = Math.Max(0, mp);
            }
            string fg = (Ini.Get(ini, "FrameGen", "FGOutput") ?? "nofg").ToLowerInvariant();
            FrameGen = Bool(ini, "FrameGen", "Enabled", false) ? Math.Max(0, Array.IndexOf(FrameGenNames, fg)) : 0;
            FpsLimit = Dec(ini, "Framerate", "FramerateLimit", 0);
        }

        public void ImportReShade(string ini)
        {
            string s = "RenoDX.DLSS5";
            RPreset = Int(ini, s, "NRPreset", 2); RStyle = Int(ini, s, "NRStyle", 1);
            RNeuralUplift = Bool(ini, s, "NeuralUplift", true); RAutoMask = Bool(ini, s, "NRAutoMask", true); RUICorrection = Bool(ini, s, "NRUICorrection", true);
            RGlobalTone = Dec(ini, s, "NRGlobalTone", 0.9m); RLocalStructure = Dec(ini, s, "NRLocalStructure", 0.44m);
            RLocalTone = Dec(ini, s, "NRLocalTone", 1.22m); RSkinStructure = Dec(ini, s, "NRSkinStructure", 1.16m);
            RDiffuseWhiteNits = Int(ini, s, "NRDiffuseWhiteNits", 203);
        }

        // Feeder F5Low : même OptiScaler, mais le mouvement est estimé par OptiScaler lui-même.
        public string BuildFeederIni(string template, Game g)
        {
            var c = Clone();
            c.WorkingScale = FWorkingScale;
            c.RunBeforeSR = false;               // il n'y a pas d'upscaling dans ces jeux
            string t = c.BuildOptiIni(template, g.ExeDir), s = "DlssNr";
            bool vulkanLike = g.Dxvk || (g.Apis.Count == 1 && g.Apis[0] == "Vulkan");
            // la génération d'images passe par l'upscaler virtuel (mode « NR + frame generation » du menu)
            bool viaUpscaler = FPath == 2 || (FPath == 0 && vulkanLike) || FSmooth || FrameGen > 0;
            t = Ini.Set(t, s, "NativeDepthFinder", B(FUseDepth));
            t = Ini.Set(t, s, "NativeMotion", "true");
            t = Ini.Set(t, s, "NativeMotionProfile", MotionProfiles[Math.Max(0, Math.Min(MotionProfiles.Length - 1, FMotionProfile))]);
            if (viaUpscaler)
            {   // mode « NR + frame generation » du menu, sans génération d'images : marche aussi en Vulkan
                t = Ini.Set(t, s, "NativeUpscaler", "true");
                t = Ini.Set(t, s, "NativeInput", "false");
                t = Ini.Set(t, s, "FinishedPicture", "false");
                t = Ini.Set(t, s, "NativeUpscalerPass", B(FSmooth));
            }
            else
            {   // mode « NR only » du menu : le plus léger
                t = Ini.Set(t, s, "NativeUpscaler", "false");
                t = Ini.Set(t, s, "NativeInput", "true");
                t = Ini.Set(t, s, "FinishedPicture", "true");
                t = Ini.Set(t, s, "NativeUpscalerPass", "false");
            }
            return t;
        }

        // Modèle de ReShade.ini et de ReShadePreset.ini du feeder : adapté de la route « Feeder » de 1-Click DLSS5
        // (github.com/reiluisii/1-Click-DLSS5, licence MIT, Copyright (c) 2026 1 Click DLSS 5 Contributors).
        public const string FeederReShadeTemplate =
            "[ADDON]\r\nAddonPath=.\\\r\nDisabledAddons=Generic Depth,Effect Runtime Sync\r\n" +
            "OverlayCollapsed=DLSS 5 Feed 0.12.0@dlss5-feed.addon64,DLSS 5 Neural Rendering@renodx-dlss5.addon64,Generic Depth,Effect Runtime Sync\r\n\r\n" +
            "[GENERAL]\r\nEffectSearchPaths=.\\reshade-shaders\\Shaders\\**\r\nIntermediateCachePath=%CACHE%\r\nNoDebugInfo=1\r\nNoEffectCache=0\r\n" +
            "NoReloadOnInit=0\r\nPerformanceMode=0\r\nPreprocessorDefinitions=DLSS5_MV_PROVIDER=3\r\nPresetPath=.\\ReShadePreset.ini\r\n" +
            "PresetShortcutKeys=\r\nPresetShortcutPaths=\r\nPresetTransitionDuration=1000\r\nSkipLoadingDisabledEffects=1\r\nStartupPresetPath=\r\n" +
            "TextureSearchPaths=.\\reshade-shaders\\Textures\\**\r\n\r\n" +
            "[INPUT]\r\nForceShortcutModifiers=1\r\nInputProcessing=2\r\nKeyEffects=35,0,0,0\r\nKeyFPS=0,0,0,0\r\nKeyFrametime=0,0,0,0\r\n" +
            "KeyNextPreset=0,0,0,0\r\nKeyOverlay=36,0,0,0\r\nKeyPreviousPreset=0,0,0,0\r\nKeyReload=0,0,0,0\r\nKeyScreenshot=44,0,0,0\r\n\r\n" +
            "[OVERLAY]\r\nAutoSavePreset=1\r\nFPSPosition=1\r\nShowForceLoadEffectsButton=1\r\nShowFPS=2\r\nTutorialProgress=4\r\n\r\n" +
            "[RenoDX.DLSS5]\r\nEnableHooks=2\r\nNREnableUpscaling=0\r\n";

        public const string FeederPresetIni =
            "Techniques=Lumenite_Kernel@lumenite_Kernel.fx,DLSS5_Feed@DLSS5_Feed.fx\r\n" +
            "TechniqueSorting=Lumenite_Kernel@lumenite_Kernel.fx,DLSS5_Feed@DLSS5_Feed.fx\r\n" +
            "PreprocessorDefinitions=DLSS5_MV_PROVIDER=3\r\n\r\n[DLSS5_Feed.fx]\r\nPreprocessorDefinitions=DLSS5_MV_PROVIDER=3\r\n";

        public string BuildReShadeIni(string template) { return BuildReShadeIni(template, false); }
        public string BuildReShadeIni(string template, bool feeder)
        {
            string t = template.Replace("%CACHE%", Path.Combine(Path.GetTempPath(), "ReShade")), s = "RenoDX.DLSS5";
            t = Ini.Set(t, s, "EnableHooks", feeder ? "2" : "1");
            if (feeder) t = Ini.Set(t, s, "NREnableUpscaling", "0");
            t = Ini.Set(t, s, "NeuralUplift", RNeuralUplift ? "1" : "0");
            t = Ini.Set(t, s, "NRPreset", RPreset.ToString());
            t = Ini.Set(t, s, "NRStyle", RStyle.ToString());
            t = Ini.Set(t, s, "NRGlobalTone", D(RGlobalTone));
            t = Ini.Set(t, s, "NRLocalStructure", D(RLocalStructure));
            t = Ini.Set(t, s, "NRLocalTone", D(RLocalTone));
            t = Ini.Set(t, s, "NRSkinStructure", D(RSkinStructure));
            t = Ini.Set(t, s, "NRAutoMask", RAutoMask ? "1" : "0");
            t = Ini.Set(t, s, "NRUICorrection", RUICorrection ? "1" : "0");
            t = Ini.Set(t, s, "NRDiffuseWhiteNits", RDiffuseWhiteNits.ToString());
            return t;
        }

        // Sauvegarde simple clé=valeur (champ public -> texte)
        public void SaveTo(List<string> lines)
        {
            foreach (var f in GetType().GetFields())
                lines.Add("set." + f.Name + "=" + Convert.ToString(f.GetValue(this), CultureInfo.InvariantCulture));
        }
        public void LoadFrom(Dictionary<string, string> kv)
        {
            foreach (var f in GetType().GetFields())
            {
                string v; if (!kv.TryGetValue("set." + f.Name, out v)) continue;
                try { f.SetValue(this, Convert.ChangeType(v, f.FieldType, CultureInfo.InvariantCulture)); } catch { }
            }
        }
    }

    public static class Presets
    {
        public static readonly string[] Names =
        {
            "Validé (tes tests 03/04)",
            "Performance (+ de FPS)",
            "Qualité — preset A (non testé)",
            "Sécuritaire (jeux instables)",
            "Effet poussé (2 passes)",
            "Effet doux",
            "FPS max (le plus léger)",
            "Fluidité ×2 (génération d'images XeFG)",
            "AMD Radeon — départ (port vendor-neutral)",
        };
        public static NrSettings Get(int i)
        {
            var s = new NrSettings(); // = valeurs validées
            switch (i)
            {
                case 1: s.WorkingScale = 0.75m; s.VitEvery = 2; s.DetailReuse = true; s.FWorkingScale = 0.5m; break;
                case 2: s.Preset = 1; s.RPreset = 1; s.FWorkingScale = 0.85m; break;
                case 3: s.WorkingScale = 0.66m; s.VitEvery = 2; s.MaxRatio = 1.5m; s.AutoCapture = false;
                        s.FWorkingScale = 0.5m; s.FUseDepth = false; break;
                case 4: s.Passes = 2; s.Intensity = 1.15m; s.LocalStructure = 1.2m;
                        s.RLocalStructure = 0.6m; s.RLocalTone = 1.4m; s.RSkinStructure = 1.3m; break;
                case 6: s.WorkingScale = 0.5m; s.FWorkingScale = 0.5m; s.VitEvery = 2; s.DetailReuse = true; s.AutoCapture = false; s.RPreset = 2; break;
                case 7: s.FrameGen = 1; break;
                case 8: s.Preset = 2; s.WorkingScale = 0.5m; s.FWorkingScale = 0.5m; s.DetailReuse = true; s.AutoCapture = false; s.FrameGen = 0; break;
                case 5: s.Intensity = 0.7m; s.TransferStrength = 0.75m; s.LocalTone = 0.8m;
                        s.RGlobalTone = 0.8m; s.RLocalStructure = 0.3m; s.RLocalTone = 1.0m; s.RSkinStructure = 1.0m; break;
            }
            return s;
        }
    }

    // ------------------------------------------------------------------ manifeste
    public class ManEntry { public string Kind, Rel, Sha, OrigSha; }

    public class Manifest
    {
        public string Mode = "", Proxy = "", Date = "", Exe = "", GpuMode = "";
        public bool Complete;
        public List<ManEntry> Entries = new List<ManEntry>();
        public string ModeLabel
        {
            get
            {
                switch (Mode)
                {
                    case "OPTISCALER": return "OptiScaler F5";
                    case "RENODX": return "RenoDX";
                    case "FEEDER": return "Feeder F5Low";
                    case "FEEDER_RESHADE": return "Feeder ReShade";
                    default: return Mode;
                }
            }
        }
        public bool UsesOptiIni { get { return Mode == "OPTISCALER" || Mode == "FEEDER"; } }

        public static string DirOf(string exeDir) { return Path.Combine(exeDir, AppPaths.StateDirName); }
        public static string FileOf(string exeDir) { return Path.Combine(DirOf(exeDir), "manifest.tsv"); }

        public ManEntry Find(string rel) { return Entries.FirstOrDefault(e => string.Equals(e.Rel, rel, StringComparison.OrdinalIgnoreCase)); }

        public void Save(string exeDir)
        {
            Directory.CreateDirectory(DirOf(exeDir));
            var sb = new StringBuilder();
            sb.AppendLine("#DLSS5-Launcher manifest v1 — ne pas modifier à la main");
            sb.AppendLine("MODE\t" + Mode); sb.AppendLine("PROXY\t" + Proxy); sb.AppendLine("DATE\t" + Date);
            sb.AppendLine("EXE\t" + Exe); sb.AppendLine("GPU\t" + GpuMode); sb.AppendLine("COMPLETE\t" + (Complete ? "1" : "0"));
            foreach (var e in Entries) sb.AppendLine("E\t" + e.Kind + "\t" + e.Rel + "\t" + e.Sha + "\t" + e.OrigSha);
            string tmp = FileOf(exeDir) + ".tmp";
            File.WriteAllText(tmp, sb.ToString(), new UTF8Encoding(false));
            if (File.Exists(FileOf(exeDir))) File.Delete(FileOf(exeDir));
            File.Move(tmp, FileOf(exeDir));
        }

        public static Manifest Load(string exeDir) { return LoadFile(FileOf(exeDir)); }
        public static Manifest LoadFile(string f)
        {
            if (!File.Exists(f)) return null;
            var m = new Manifest();
            foreach (var line in File.ReadAllLines(f, Encoding.UTF8))
            {
                var p = line.Split('\t');
                if (p.Length < 2 || line.StartsWith("#")) continue;
                switch (p[0])
                {
                    case "MODE": m.Mode = p[1]; break;
                    case "PROXY": m.Proxy = p[1]; break;
                    case "DATE": m.Date = p[1]; break;
                    case "EXE": m.Exe = p[1]; break;
                    case "GPU": m.GpuMode = p[1]; break;
                    case "COMPLETE": m.Complete = p[1] == "1"; break;
                    case "E": m.Entries.Add(new ManEntry { Kind = p[1], Rel = p.Length > 2 ? p[2] : "", Sha = p.Length > 3 ? p[3] : "", OrigSha = p.Length > 4 ? p[4] : "" }); break;
                }
            }
            return m;
        }
    }

    public class UserFacingException : Exception { public UserFacingException(string m) : base(m) { } }

    // ------------------------------------------------------------------ installation
    public class Installer
    {
        readonly Game g;
        readonly string exeDir, stateDir;
        Manifest m;

        public Installer(Game game) { g = game; exeDir = game.ExeDir; stateDir = Manifest.DirOf(exeDir); }

        string BackupPath(string rel) { return Path.Combine(stateDir, "backup", rel); }
        string Dst(string rel) { return Path.Combine(exeDir, rel); }

        public static bool IsGameRunning(Game g)
        {
            try
            {
                string n = Path.GetFileNameWithoutExtension(g.ExePath);
                foreach (var p in Process.GetProcessesByName(n))
                {
                    try { if (string.Equals(p.MainModule.FileName, g.ExePath, StringComparison.OrdinalIgnoreCase)) return true; }
                    catch { return true; } // accès refusé : prudence, on considère qu'il roule
                }
            }
            catch { }
            return false;
        }

        public static InstallMode Resolve(Game g, InstallMode wanted)
        {
            if (wanted != InstallMode.Auto) return wanted;
            if (HasGameUpscaler(g)) return InstallMode.OptiScaler; // OptiScaler F5 couvre DLSS, FSR et XeSS
            if (g.Apis.Count == 1 && g.Apis[0] == "OpenGL") return InstallMode.FeederReShade; // F5Low ne gère pas OpenGL
            return InstallMode.Feeder;
        }

        public static bool HasGameUpscaler(Game g)
        {
            return g.Upscalers.Contains("DLSS") || g.Upscalers.Contains("FSR") || g.Upscalers.Contains("XeSS") || g.Upscalers.Contains("Streamline");
        }

        public static string ModeName(InstallMode m)
        {
            switch (m)
            {
                case InstallMode.OptiScaler: return "OptiScaler F5";
                case InstallMode.RenoDX: return "RenoDX";
                case InstallMode.Feeder: return "Feeder F5Low";
                case InstallMode.FeederReShade: return "Feeder ReShade";
                default: return "Auto";
            }
        }

        public static string PickProxy(Game g, string overrideName)
        {
            if (!string.IsNullOrEmpty(overrideName)) return overrideName;
            if (!string.IsNullOrEmpty(g.RecommendedProxy) && !g.TakenProxyNames.Contains(g.RecommendedProxy, StringComparer.OrdinalIgnoreCase)) return g.RecommendedProxy;
            var order = (g.Apis.Count == 1 && g.Apis[0] == "Vulkan")
                ? new[] { "winmm.dll", "version.dll", "dbghelp.dll", "dxgi.dll" }
                : new[] { "dxgi.dll", "winmm.dll", "version.dll", "dbghelp.dll" };
            foreach (var n in order) if (!g.TakenProxyNames.Contains(n, StringComparer.OrdinalIgnoreCase)) return n;
            throw new UserFacingException("Tous les noms de DLL proxy sont déjà pris par d'autres mods dans ce dossier.");
        }

        public static void CheckPayload(InstallMode mode)
        {
            if (Gpu.IsAmdMode && (mode == InstallMode.RenoDX || mode == InstallMode.FeederReShade))
                throw new UserFacingException("RenoDX et le Feeder ReShade passent par le cœur NGX de NVIDIA : impossible sur une carte " + Gpu.Vendor + ".\nUtilise OptiScaler F5 (jeux avec FSR/XeSS/DLSS) ou le Feeder F5Low (jeux sans upscaler).");
            var need = new List<string> { Gpu.NrDllPath };
            if (Gpu.IsAmdMode)
                need.AddRange(new[] { Path.Combine(AppPaths.AmdDir, "nvngx.dll_dlssnr.dll"), Path.Combine(AppPaths.AmdDir, "nr_port.ini") });
            if (mode == InstallMode.OptiScaler || mode == InstallMode.Feeder)
                need.AddRange(new[] { Path.Combine(AppPaths.OptiDir, "OptiScaler.dll"), Path.Combine(AppPaths.OptiDir, "OptiScaler.ini"),
                                      Path.Combine(AppPaths.OptiDir, "nvngx.dll_dlssnr.dll"), Path.Combine(AppPaths.OptiDir, "OptiScaler") });
            else
                need.AddRange(new[] { Path.Combine(AppPaths.RenoDir, "dxgi.dll"), Path.Combine(AppPaths.RenoDir, "renodx-dlss5.addon64"), Path.Combine(AppPaths.RenoDir, "ReShade.ini") });
            if (mode == InstallMode.FeederReShade)
                need.AddRange(new[] { Path.Combine(AppPaths.FeederDir, "dlss5-feed.addon64"), Path.Combine(AppPaths.FeederDir, "dlss5-feed.cfg"),
                                      Path.Combine(AppPaths.FeederDir, "nvngx_dlss.dll"), Path.Combine(AppPaths.FeederDir, "shaders"), Path.Combine(AppPaths.FeederDir, "textures") });
            foreach (var n in need)
                if (!File.Exists(n) && !Directory.Exists(n)) throw new UserFacingException("Fichier manquant dans le payload du launcher :\n" + n);
        }

        // ---- opérations suivies
        void CopyIn(string src, string rel)
        {
            string dst = Dst(rel);
            string srcSha = Hash.ShaCached(src);
            TrackBeforeWrite(rel, srcSha);
            Directory.CreateDirectory(Path.GetDirectoryName(dst));
            if (File.Exists(dst)) File.SetAttributes(dst, FileAttributes.Normal);
            File.Copy(src, dst, true);
            File.SetAttributes(dst, FileAttributes.Normal);
            if (Hash.Sha(dst) != srcSha) throw new UserFacingException("Vérification échouée après copie : " + rel);
            Log.W("  + " + rel);
        }

        void WriteIn(string rel, string text)
        {
            string dst = Dst(rel);
            TrackBeforeWrite(rel, Hash.ShaText(text));
            if (File.Exists(dst)) File.SetAttributes(dst, FileAttributes.Normal);
            Ini.WriteFile(dst, text);
            Log.W("  + " + rel + " (réglages)");
        }

        void TrackBeforeWrite(string rel, string newSha)
        {
            string dst = Dst(rel);
            var e = m.Find(rel);
            if (e != null) { e.Sha = newSha; m.Save(exeDir); return; }
            if (File.Exists(dst))
            {
                string bp = BackupPath(rel);
                Directory.CreateDirectory(Path.GetDirectoryName(bp));
                string orig = Hash.Sha(dst);
                File.Copy(dst, bp, true);
                if (Hash.Sha(bp) != orig) throw new UserFacingException("La sauvegarde de " + rel + " n'a pas pu être vérifiée. Rien n'a été remplacé.");
                m.Entries.Add(new ManEntry { Kind = "REPLACED", Rel = rel, Sha = newSha, OrigSha = orig });
                Log.W("  sauvegarde : " + rel);
            }
            else m.Entries.Add(new ManEntry { Kind = "ADDED", Rel = rel, Sha = newSha });
            m.Save(exeDir); // le manifeste est écrit AVANT la copie : même un plantage reste réversible
        }

        void MoveAway(string rel)
        {
            bool isDir = rel.EndsWith("\\");
            string r = rel.TrimEnd('\\');
            string src = Dst(r), bp = BackupPath(r);
            Directory.CreateDirectory(Path.GetDirectoryName(bp));
            m.Entries.Add(new ManEntry { Kind = isDir ? "MOVEDDIR" : "MOVED", Rel = r, Sha = isDir ? "" : Hash.Sha(src) });
            m.Save(exeDir);
            if (isDir) Directory.Move(src, bp); else File.Move(src, bp);
            Log.W("  mis de côté : " + rel);
        }

        public static string ReShadeProxy(Game g) { return g.Apis.Count == 1 && g.Apis[0] == "OpenGL" ? "opengl32.dll" : "dxgi.dll"; }

        void CopyDirIn(string srcDir, string relDir)
        {
            string parent = Path.GetDirectoryName(relDir);
            if (!string.IsNullOrEmpty(parent) && !Directory.Exists(Dst(parent)))
            {
                m.Entries.Add(new ManEntry { Kind = "CREATEDDIR", Rel = parent });
                m.Save(exeDir);
                Directory.CreateDirectory(Dst(parent));
            }
            if (!Directory.Exists(Dst(relDir)))
            {
                m.Entries.Add(new ManEntry { Kind = "CREATEDDIR", Rel = relDir });
                m.Save(exeDir);
                Directory.CreateDirectory(Dst(relDir));
            }
            foreach (var f in Directory.GetFiles(srcDir, "*", SearchOption.AllDirectories))
                CopyIn(f, Path.Combine(relDir, f.Substring(srcDir.Length + 1)));
        }

        // ---- installation
        public void Install(InstallMode wanted, NrSettings st, string proxyOverride)
        {
            if (g.ExePath == null) throw new UserFacingException("Exe du jeu introuvable.");
            if (!g.Is64) throw new UserFacingException("Ce jeu est 32-bit : OptiScaler F5 et RenoDX demandent un jeu 64-bit.");
            if (g.Installed != null) throw new UserFacingException("Déjà installé ici. Réinitialise d'abord.");
            if (g.OneClickInstalled) throw new UserFacingException("Ce jeu a une installation du 1-Click DLSS5.\nClique d'abord « Réinitialiser le jeu » : il sait maintenant la défaire, puis réinstalle.");
            if (IsGameRunning(g)) throw new UserFacingException("Le jeu roule encore. Ferme-le complètement, puis réessaie.");
            var mode = Resolve(g, wanted);
            CheckPayload(mode);
            st = Stability.Adjust(st, g);
            if (mode == InstallMode.RenoDX && g.TakenProxyNames.Contains("dxgi.dll", StringComparer.OrdinalIgnoreCase))
                throw new UserFacingException("Un dxgi.dll inconnu (autre mod) existe déjà : le mode RenoDX en a besoin. Essaie le mode OptiScaler F5.");
            if (mode == InstallMode.FeederReShade)
            {
                if (g.Dxvk) throw new UserFacingException("Ce jeu utilise dxvk (DX11 traduit en Vulkan) : le Feeder ReShade ne peut pas s'y accrocher.\nUtilise le Feeder F5Low.");
                if (g.Apis.Count == 1 && g.Apis[0] == "Vulkan") throw new UserFacingException("Jeu Vulkan : le Feeder ReShade demande une couche Vulkan installée dans Windows.\nUtilise le Feeder F5Low, fait pour ça.");
                if (g.Apis.Count == 1 && g.Apis[0] == "DX9") throw new UserFacingException("Jeu DX9 : pas encore géré par le launcher.");
                string px = ReShadeProxy(g);
                if (g.TakenProxyNames.Contains(px, StringComparer.OrdinalIgnoreCase))
                    throw new UserFacingException("Un " + px + " d'un autre mod existe déjà : le Feeder ReShade en a besoin. Essaie le Feeder F5Low.");
            }

            // test d'écriture (Program Files demande les droits admin)
            string probe = Path.Combine(exeDir, ".dlss5_write_test");
            File.WriteAllText(probe, "ok"); File.Delete(probe);

            string modeId = mode == InstallMode.OptiScaler ? "OPTISCALER" : mode == InstallMode.RenoDX ? "RENODX" : mode == InstallMode.Feeder ? "FEEDER" : "FEEDER_RESHADE";
            m = new Manifest { GpuMode = (Gpu.IsAmdMode ? Gpu.Vendor + " (port vendor-neutral)" : "NVIDIA" + (Gpu.Series > 0 ? " RTX " + Gpu.Series : "") + " (NGX)") + ", NR " + Gpu.NrLabel, Mode = modeId, Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Exe = g.ExeName };
            m.Save(exeDir);
            Log.W("Installation " + m.ModeLabel + " dans " + exeDir + "  [API " + g.ApiText + (g.Engine != null ? ", " + g.Engine : "") + ", upscalers : " + g.UpscalerText + (wanted == InstallMode.Auto ? ", choix auto" : ", choix manuel") + "]");

            foreach (var f in g.ForeignMods) MoveAway(f);

            // AMD / Intel : la 310.9 d'origine (le port lit ses poids) ; NVIDIA : ta DLL OpenNR validée
            CopyIn(Gpu.NrDllPath, "nvngx_dlssnr.dll");
            if (mode == InstallMode.OptiScaler || mode == InstallMode.Feeder)
            {
                string proxy = PickProxy(g, proxyOverride);
                m.Proxy = proxy; m.Save(exeDir);
                CopyIn(Path.Combine(AppPaths.OptiDir, "OptiScaler.dll"), proxy);
                if (Gpu.IsAmdMode)
                {   // forwarder vendor-neutral : fait tourner le modèle en D3D12 + DirectML, sans NGX
                    CopyIn(Path.Combine(AppPaths.AmdDir, "nvngx.dll_dlssnr.dll"), "nvngx.dll_dlssnr.dll");
                    CopyIn(Path.Combine(AppPaths.AmdDir, "nr_port.ini"), "nr_port.ini");
                }
                else CopyIn(Path.Combine(AppPaths.OptiDir, "nvngx.dll_dlssnr.dll"), "nvngx.dll_dlssnr.dll");
                CopyDirIn(Path.Combine(AppPaths.OptiDir, "OptiScaler"), "OptiScaler");
                string tpl = Ini.ReadFile(Path.Combine(AppPaths.OptiDir, "OptiScaler.ini"));
                WriteIn("OptiScaler.ini", mode == InstallMode.Feeder ? st.BuildFeederIni(tpl, g) : st.BuildOptiIni(tpl, exeDir));
            }
            else if (mode == InstallMode.FeederReShade)
            {
                string proxy = ReShadeProxy(g);
                m.Proxy = proxy; m.Save(exeDir);
                CopyIn(Path.Combine(AppPaths.RenoDir, "dxgi.dll"), proxy);
                CopyIn(Path.Combine(AppPaths.RenoDir, "renodx-dlss5.addon64"), "renodx-dlss5.addon64");
                CopyIn(Path.Combine(AppPaths.FeederDir, "dlss5-feed.addon64"), "dlss5-feed.addon64");
                CopyIn(Path.Combine(AppPaths.FeederDir, "dlss5-feed.cfg"), "dlss5-feed.cfg");
                CopyIn(Path.Combine(AppPaths.FeederDir, "nvngx_dlss.dll"), "nvngx_dlss.dll");
                CopyDirIn(Path.Combine(AppPaths.FeederDir, "shaders"), Path.Combine("reshade-shaders", "Shaders"));
                CopyDirIn(Path.Combine(AppPaths.FeederDir, "textures"), Path.Combine("reshade-shaders", "Textures"));
                WriteIn("ReShade.ini", st.BuildReShadeIni(NrSettings.FeederReShadeTemplate, true));
                WriteIn("ReShadePreset.ini", NrSettings.FeederPresetIni);
            }
            else
            {
                m.Proxy = "dxgi.dll"; m.Save(exeDir);
                CopyIn(Path.Combine(AppPaths.RenoDir, "dxgi.dll"), "dxgi.dll");
                CopyIn(Path.Combine(AppPaths.RenoDir, "renodx-dlss5.addon64"), "renodx-dlss5.addon64");
                WriteIn("ReShade.ini", st.BuildReShadeIni(Ini.ReadFile(Path.Combine(AppPaths.RenoDir, "ReShade.ini"))));
            }
            m.Complete = true; m.Save(exeDir);
            Log.W("Installation terminée (" + m.Entries.Count + " opérations notées dans le manifeste).");
        }

        // ---- appliquer de nouveaux réglages à une installation existante
        public void ApplySettings(NrSettings st)
        {
            m = Manifest.Load(exeDir);
            if (m == null || !m.Complete) throw new UserFacingException("Aucune installation complète du launcher dans ce jeu.");
            if (IsGameRunning(g)) throw new UserFacingException("Ferme le jeu avant de changer les réglages.");
            st = Stability.Adjust(st, g);
            if (m.UsesOptiIni)
            {
                // on repart du modèle propre : un ancien choix (ex. FinishedPicture) ne doit pas traîner
                string tpl = Ini.ReadFile(Path.Combine(AppPaths.OptiDir, "OptiScaler.ini"));
                WriteIn("OptiScaler.ini", m.Mode == "FEEDER" ? st.BuildFeederIni(tpl, g) : st.BuildOptiIni(tpl, exeDir));
            }
            else if (m.Mode == "FEEDER_RESHADE")
            {
                string cur = File.Exists(Dst("ReShade.ini")) ? Ini.ReadFile(Dst("ReShade.ini")) : NrSettings.FeederReShadeTemplate;
                WriteIn("ReShade.ini", st.BuildReShadeIni(cur, true));
            }
            else
            {
                string cur = File.Exists(Dst("ReShade.ini")) ? Ini.ReadFile(Dst("ReShade.ini")) : Ini.ReadFile(Path.Combine(AppPaths.RenoDir, "ReShade.ini"));
                WriteIn("ReShade.ini", st.BuildReShadeIni(cur));
            }
            Log.W("Réglages appliqués à " + g.Name + ".");
        }

        // ---- réinitialisation
        static readonly string[] RuntimeFiles = { "OptiScaler.log", "ReShade.log", "ReShade.log.old", "dlss5-feed.log", "dlss5-feed.ini", "dlss5-feed-host.log", "nr_port.log", "OptiScaler_crash.dmp", "nr_tile_cache.txt" };
        static readonly string[] RuntimeDirs = { "dlssnr-capture" };

        public string ArchiveLogs(string why)
        {
            string dest = Path.Combine(AppPaths.Journaux, Safe(g.Name), DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + "_" + why);
            bool any = false;
            foreach (var f in RuntimeFiles.Concat(new[] { "OptiScaler.ini", "ReShade.ini", "ReShadePreset.ini", "dlss5-feed.cfg" }))
            {
                string p = Dst(f);
                if (!File.Exists(p)) continue;
                Directory.CreateDirectory(dest); File.Copy(p, Path.Combine(dest, f), true); any = true;
            }
            foreach (var d in RuntimeDirs)
            {
                string p = Dst(d);
                if (!Directory.Exists(p)) continue;
                foreach (var f in Directory.GetFiles(p, "*", SearchOption.AllDirectories))
                {
                    string to = Path.Combine(dest, d, f.Substring(p.Length + 1));
                    Directory.CreateDirectory(Path.GetDirectoryName(to)); File.Copy(f, to, true); any = true;
                }
            }
            // log du moteur (Unreal / Unity) : copié seulement, jamais touché
            int ei = 0;
            foreach (var f in EngineLogs.EngineLogFiles(g))
                try { Directory.CreateDirectory(dest); File.Copy(f, Path.Combine(dest, "moteur_" + (ei++ == 0 ? "" : ei + "_") + Path.GetFileName(f)), true); any = true; } catch { }
            // le manifeste aussi : une fiche faite APRÈS le reset saura quelle méthode / quel GPU étaient en place
            try { if (File.Exists(Manifest.FileOf(exeDir))) { Directory.CreateDirectory(dest); File.Copy(Manifest.FileOf(exeDir), Path.Combine(dest, "manifest.tsv"), true); } } catch { }
            return any ? dest : null;
        }

        public List<string> Reset()
        {
            var errors = new List<string>();
            m = Manifest.Load(exeDir);
            if (m == null) throw new UserFacingException("Aucun manifeste du launcher ici : rien à réinitialiser.\n(Pour un jeu Steam, « Vérifier l'intégrité des fichiers » remet tout d'origine.)");
            if (IsGameRunning(g)) throw new UserFacingException("Le jeu roule encore. Ferme-le complètement, puis réessaie.");
            Log.W("Réinitialisation de " + g.Name + " (" + m.ModeLabel + ")");

            string arch = ArchiveLogs("avant-reset");
            if (arch != null) Log.W("  journaux archivés : " + arch);

            foreach (var f in RuntimeFiles)
                if (File.Exists(Dst(f))) try { File.Delete(Dst(f)); } catch (Exception ex) { errors.Add(f + " : " + ex.Message); }
            foreach (var d in RuntimeDirs)
                if (Directory.Exists(Dst(d))) try { Directory.Delete(Dst(d), true); } catch (Exception ex) { errors.Add(d + " : " + ex.Message); }

            var remaining = new List<ManEntry>();
            for (int i = m.Entries.Count - 1; i >= 0; i--)
            {
                var e = m.Entries[i];
                string dst = Dst(e.Rel), bp = BackupPath(e.Rel);
                try
                {
                    switch (e.Kind)
                    {
                        case "ADDED":
                            if (File.Exists(dst)) { File.SetAttributes(dst, FileAttributes.Normal); File.Delete(dst); }
                            break;
                        case "REPLACED":
                            if (!File.Exists(bp)) throw new Exception("sauvegarde absente");
                            if (File.Exists(dst)) File.SetAttributes(dst, FileAttributes.Normal);
                            File.Copy(bp, dst, true);
                            if (!string.IsNullOrEmpty(e.OrigSha) && Hash.Sha(dst) != e.OrigSha) throw new Exception("hash d'origine différent après restauration");
                            break;
                        case "MOVED":
                            if (!File.Exists(bp)) throw new Exception("fichier mis de côté absent");
                            if (File.Exists(dst)) { File.SetAttributes(dst, FileAttributes.Normal); File.Delete(dst); }
                            File.Move(bp, dst);
                            break;
                        case "MOVEDDIR":
                            if (!Directory.Exists(bp)) throw new Exception("dossier mis de côté absent");
                            if (Directory.Exists(dst)) Directory.Delete(dst, true);
                            Directory.Move(bp, dst);
                            break;
                        case "CREATEDDIR":
                            if (Directory.Exists(dst)) Directory.Delete(dst, true);
                            break;
                    }
                    Log.W("  ↺ " + e.Kind.ToLowerInvariant() + " : " + e.Rel);
                }
                catch (Exception ex) { errors.Add(e.Rel + " : " + ex.Message); remaining.Insert(0, e); }
            }

            if (errors.Count == 0)
            {
                try { Directory.Delete(stateDir, true); } catch (Exception ex) { errors.Add(AppPaths.StateDirName + " : " + ex.Message); }
                Log.W("Réinitialisation terminée : dossier du jeu remis comme avant.");
            }
            else
            {
                m.Entries = remaining; m.Complete = false; m.Save(exeDir);
                Log.W("Réinitialisation partielle : " + errors.Count + " problème(s). Le manifeste garde ce qui reste à défaire.");
            }
            return errors;
        }

        public static string Safe(string s) { return Regex.Replace(s ?? "jeu", @"[^\w\-\. ]", "_").Trim(); }
    }

    // ------------------------------------------------------------------ garde-fous de stabilité
    public static class Stability
    {
        public static bool HasRr(Game g) { return g.Upscalers.Contains("DLSS-RR"); }

        // Avec la Ray Reconstruction, OptiScaler fait tourner NR APRÈS RR, à la résolution de sortie : c'est là que tes crashs sont arrivés.
        public static NrSettings Adjust(NrSettings st, Game g)
        {
            if (!st.RrSafe || !HasRr(g) || Gpu.IsAmdMode) return st;
            var s = st.Clone();
            bool changed = false;
            if (s.Preset != 2) { s.Preset = 2; changed = true; }
            if (s.WorkingScale > 0.6m) { s.WorkingScale = 0.6m; changed = true; }
            if (s.Passes > 1) { s.Passes = 1; changed = true; }
            if (changed) Log.W("  profil sûr Ray Reconstruction : Preset B, modèle ≤ 0,6, 1 passe (décoche-le dans Réglages → Stabilité pour l'enlever)");
            return s;
        }

        const string GfxKey = @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers";
        public static int TdrDelay()
        {
            try { using (var k = Registry.LocalMachine.OpenSubKey(GfxKey)) { var v = k == null ? null : k.GetValue("TdrDelay"); return v is int ? (int)v : 0; } }
            catch { return 0; }
        }
        // Délai avant que Windows considère le GPU « bloqué » (défaut 2 s). Demande les droits admin et un redémarrage.
        public static void SetTdrDelay(int seconds)
        {
            using (var k = Registry.LocalMachine.OpenSubKey(GfxKey, true))
            {
                if (k == null) throw new UserFacingException("Clé GraphicsDrivers introuvable.");
                if (seconds <= 0) k.DeleteValue("TdrDelay", false); else k.SetValue("TdrDelay", seconds, RegistryValueKind.DWord);
            }
            Log.W(seconds <= 0 ? "TdrDelay remis par défaut (2 s)." : "TdrDelay réglé à " + seconds + " s.");
        }
    }

    // ------------------------------------------------------------------ performance mesurée (OptiScaler.log)
    public class PerfInfo
    {
        public int Samples;
        public double AvgMs, MinMs, MaxMs;
        public string ModelInput, Preset, When;
        public bool FrameGenSeen;
        public string Summary()
        {
            if (Samples == 0) return null;
            string p = Preset == "1" ? "A (gros modèle)" : Preset == "2" ? "B (petit modèle)" : Preset;
            return "NR ≈ " + AvgMs.ToString("0.0", CultureInfo.GetCultureInfo("fr-CA")) + " ms/image (" + Samples + " mesures, " +
                   MinMs.ToString("0.0", CultureInfo.GetCultureInfo("fr-CA")) + "–" + MaxMs.ToString("0.0", CultureInfo.GetCultureInfo("fr-CA")) + ")" +
                   (ModelInput != null ? ", modèle " + ModelInput : "") + (p != null ? ", preset " + p : "") + (FrameGenSeen ? ", génération d'images vue" : "");
        }
        public string Advice()
        {
            if (Samples == 0) return null;
            if (AvgMs >= 8) return "C'est lourd : à 60 FPS le budget total est 16,7 ms. Pour des FPS : Preset B (≈ 2× moins cher), résolution du modèle 0,67 (≈ 2× moins), ou le préréglage « FPS max ».";
            if (AvgMs >= 5) return "Coût moyen. « Réutilisation du détail » ou VitEvery « une image sur deux » peuvent encore le réduire.";
            return "Coût léger : tu as de la marge pour plus de qualité (Preset A, résolution plus haute, 2 passes).";
        }
    }

    public static class Perf
    {
        public static PerfInfo Read(string exeDir)
        {
            var r = new PerfInfo();
            string p = exeDir == null ? null : Path.Combine(exeDir, "OptiScaler.log");
            if (p == null || !File.Exists(p)) return r;
            var vals = new List<double>();
            try
            {
                using (var fs = new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var rd = new StreamReader(fs))
                {
                    string line;
                    var rxE = new Regex(@"DLSS-NR elapsed: ([0-9.]+) ms total");
                    var rxM = new Regex(@"model input (\d+x\d+).*?\(preset (\d)");
                    while ((line = rd.ReadLine()) != null)
                    {
                        var m = rxE.Match(line);
                        double v;
                        if (m.Success && double.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out v)) vals.Add(v);
                        var mm = rxM.Match(line);
                        if (mm.Success) { r.ModelInput = mm.Groups[1].Value; r.Preset = mm.Groups[2].Value; }
                        if (!r.FrameGenSeen && Regex.IsMatch(line, @"(XeFG|FSRFG|FSR ?FG|OptiFG|DLSSG)\S*::\S*.*(created|Present|interpolat|generated)", RegexOptions.IgnoreCase)
                            && !Regex.IsMatch(line, @"can't|cannot|invalid|not |disabled|nofg", RegexOptions.IgnoreCase)) r.FrameGenSeen = true;
                    }
                }
            }
            catch { }
            if (vals.Count == 0) return r;
            r.Samples = vals.Count; r.AvgMs = vals.Average(); r.MinMs = vals.Min(); r.MaxMs = vals.Max();
            r.When = File.GetLastWriteTime(p).ToString("yyyy-MM-dd HH:mm");
            return r;
        }
    }

    // ------------------------------------------------------------------ fiche de test (documentation)
    public static class Report
    {
        // journal archivé sans manifeste (anciennes versions) : on déduit la méthode d'après les INI
        public static Manifest InferFromArchive(string dir)
        {
            string oi = Path.Combine(dir, "OptiScaler.ini"), ri = Path.Combine(dir, "ReShade.ini");
            if (File.Exists(oi))
            {
                bool feeder = (Ini.Get(Ini.ReadFile(oi), "DlssNr", "NativeMotion") ?? "").ToLowerInvariant() == "true";
                return new Manifest { Mode = feeder ? "FEEDER" : "OPTISCALER", Proxy = "?", Date = "?", Complete = true };
            }
            if (File.Exists(ri))
            {
                bool feeder = Ini.Get(Ini.ReadFile(ri), "RenoDX.DLSS5", "NREnableUpscaling") == "0";
                return new Manifest { Mode = feeder ? "FEEDER_RESHADE" : "RENODX", Proxy = "?", Date = "?", Complete = true };
            }
            return null;
        }

        // dernier dossier de journaux/<jeu> qui contient un log
        public static string LatestArchive(Game g)
        {
            string d = Path.Combine(AppPaths.Journaux, Installer.Safe(g.Name));
            if (!Directory.Exists(d)) return null;
            return Directory.GetDirectories(d).OrderByDescending(x => x)
                .FirstOrDefault(x => File.Exists(Path.Combine(x, "OptiScaler.log")) || File.Exists(Path.Combine(x, "ReShade.log")) || File.Exists(Path.Combine(x, "nr_port.log")));
        }

        static string ReadShared(string p)
        {
            if (!File.Exists(p)) return null;
            using (var fs = new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var rd = new StreamReader(fs)) return rd.ReadToEnd();
        }

        // Lecture des logs pour dire en clair ce qui s'est passé.
        public static List<string> Diagnose(Game g) { return Diagnose(g, g.ExeDir, g.Installed); }
        public static List<string> Diagnose(Game g, string dir, Manifest m)
        {
            var h = new List<string>();
            if (m == null) { h.Add("Rien d'installé par le launcher."); return h; }
            string rs = ReadShared(Path.Combine(dir, "ReShade.log")), os = ReadShared(Path.Combine(dir, "OptiScaler.log")), fl = ReadShared(Path.Combine(dir, "dlss5-feed.log"));
            if (m.Mode == "RENODX" || m.Mode == "FEEDER_RESHADE")
            {
                if (rs == null) h.Add("Pas de ReShade.log : ReShade n'a pas été chargé par le jeu (mauvais nom de proxy, ou le jeu n'a pas encore été lancé).");
                else
                {
                    bool api = Regex.IsMatch(rs, @"d3d11|d3d12|dxgi|vulkan|opengl.*(context|runtime)|Running on", RegexOptions.IgnoreCase) && Regex.IsMatch(rs, @"swap ?chain|runtime|Running on|Initializing.*device", RegexOptions.IgnoreCase);
                    if (Regex.IsMatch(rs, @"loaded from '.*OPENGL32\.dll'", RegexOptions.IgnoreCase) && !Regex.IsMatch(rs, @"swap ?chain|wglMakeCurrent|context", RegexOptions.IgnoreCase))
                        h.Add("ReShade s'est chargé comme opengl32.dll mais le jeu n'utilise pas OpenGL : aucune image n'a été vue. Réinitialise et réinstalle en Auto (ou choisis Feeder F5Low).");
                    else if (!api) h.Add("ReShade s'est chargé mais n'a vu aucun rendu (le jeu a-t-il atteint le menu ?).");
                    else h.Add("ReShade s'est accroché au rendu du jeu.");
                    if (Regex.IsMatch(rs, @"Failed to load add-?on|addon.*fail", RegexOptions.IgnoreCase)) h.Add("Un addon (RenoDX / DLSS5-Feeder) n'a pas pu se charger : voir ReShade.log.");
                    if (Regex.IsMatch(rs, @"error X\d+|failed to compile", RegexOptions.IgnoreCase)) h.Add("Des shaders n'ont pas compilé (voir ReShade.log).");
                }
                if (m.Mode == "FEEDER_RESHADE" && fl == null) h.Add("Pas de dlss5-feed.log : l'addon DLSS5-Feeder n'a pas démarré.");
            }
            else
            {
                if (os == null) h.Add("Pas d'OptiScaler.log : OptiScaler n'a pas été chargé (essaie un autre nom de DLL proxy, ex. winmm.dll).");
                else
                {
                    if (Regex.IsMatch(os, @"model feature created|evaluation succeeded", RegexOptions.IgnoreCase)) h.Add("NR a démarré (« model feature created »).");
                    if (Regex.IsMatch(os, @"BAD00001|FeatureNotSupported", RegexOptions.IgnoreCase)) h.Add("FeatureNotSupported (0xBAD00001) : la DLL NR ne convient pas à ce GPU/montage.");
                    if (Regex.IsMatch(os, @"pass-through|TargetProcessName", RegexOptions.IgnoreCase) && !Regex.IsMatch(os, @"model feature created", RegexOptions.IgnoreCase)) h.Add("OptiScaler est peut-être en mode pass-through (filtre de processus).");
                    if (m.Mode == "FEEDER")
                    {
                        if (Regex.IsMatch(os, @"Native motion|Virtual upscaler", RegexOptions.IgnoreCase)) h.Add("Optical F5Low a démarré (lignes « Native motion » / « Virtual upscaler »).");
                        else h.Add("Aucune ligne « Native motion » : Optical F5Low n'a pas démarré. Si le jeu appelle son propre upscaler, utilise OptiScaler F5 ; sinon essaie Chemin → Via upscaler virtuel.");
                        if (Regex.IsMatch(os, @"no snapshot", RegexOptions.IgnoreCase)) h.Add("Pas de profondeur trouvée (normal dans les menus) : NR tourne au mouvement seulement.");
                    }
                    else if (!Regex.IsMatch(os, @"InitDLSS|Creating DLSS|FSR|XeSS", RegexOptions.IgnoreCase)) h.Add("Le jeu n'a pas appelé d'upscaler : active DLSS/FSR/XeSS dans ses options, ou utilise le Feeder F5Low.");
                }
            }
            if (os != null)
            {
                var hung = Regex.Match(os, @"Device removed reason: (\w+)");
                if (hung.Success)
                {
                    h.Add("CRASH GPU : " + hung.Groups[1].Value + ". Windows a coupé la carte graphique (travail trop long ou bloqué).");
                    if (Regex.IsMatch(os, @"running after RR\+SR")) h.Add("La Ray Reconstruction était active : c'est le point commun de tes crashs (Cyberpunk, Crimson Desert). Coupe la RR dans le jeu et garde le « profil sûr RR ».");
                }
                int rec = Regex.Matches(os, @"model feature created").Count;
                if (rec > 3) h.Add("Le modèle NR a été recréé " + rec + " fois (chaque changement de réglage en jeu, ou l'activation de la RR, le reconstruit). C'est le moment le plus risqué : règle l'effet dans le launcher plutôt qu'en pleine partie.");
                if (Regex.IsMatch(os, @"Failed to map buffer")) h.Add("« Failed to map buffer » à la fin du log : signe d'une perte du GPU ou d'un manque de mémoire vidéo.");
            }
            if (os != null)
            {
                var ue = Regex.Match(os, @"\[C\] Unhandled exception ([0-9A-Fa-f]+)");
                if (ue.Success)
                {
                    string c = ue.Groups[1].Value.ToUpperInvariant().TrimStart('0');
                    if (c == "8000" && g.Engine == "Unreal") h.Add("CRASH : exception 0x8000 levée par Unreal Engine lui-même (son code « crash GPU », à confirmer). Le message exact serait dans le log du moteur, si le jeu en écrit un : voir plus bas.");
                    else if (c == "8001" && g.Engine == "Unreal") h.Add("CRASH : exception 0x8001 d'Unreal (manque de mémoire). Baisse la résolution du modèle ou les textures du jeu.");
                    else h.Add("CRASH : exception non gérée 0x" + c + " (un OptiScaler_crash.dmp a été écrit si l'option est cochée).");
                }
                int none = Regex.Matches(os, @"Depth finder: frame \d+.*none qualifies").Count, reports = Regex.Matches(os, @"Depth finder: frame \d+").Count;
                bool noDepth = Regex.IsMatch(os, @"matched with depth 0\)") && !Regex.IsMatch(os, @"matched with depth [1-9]");
                if (reports >= 10 && none * 10 >= reports * 9 && noDepth)
                {
                    var lk = Regex.Matches(os, @"longest ([0-9.]+) us").Cast<Match>().Select(x => double.Parse(x.Groups[1].Value, CultureInfo.InvariantCulture)).DefaultIfEmpty(0).Max();
                    h.Add("La recherche de profondeur n'a JAMAIS trouvé la profondeur de ce jeu (" + none + "/" + reports + " rapports), mais elle surveillait quand même tous les dessins" +
                          (lk > 0 ? " (blocages jusqu'à " + (lk / 1000).ToString("0.0", CultureInfo.GetCultureInfo("fr-CA")) + " ms)" : "") + ". Décoche « Utiliser la profondeur du jeu » pour ce jeu : même qualité, moins de surcharge.");
                }
            }
            foreach (var el in EngineLogs.EngineLogFiles(g).Take(1))
            {
                var errs = EngineLogs.EngineErrors(el, 3);
                if (errs.Count > 0) h.Add("Log du moteur (" + Path.GetFileName(el) + ") : " + errs[errs.Count - 1].Trim());
            }
            string pl = ReadShared(Path.Combine(dir, "nr_port.log"));
            if (pl != null)
            {
                if (pl.Contains("network: small")) h.Add("Port vendor-neutral (AMD/Intel) actif avec le petit modèle 310.9 : bon réglage.");
                else if (pl.Contains("network: main")) h.Add("Port vendor-neutral sur le GROS modèle : la DLL NR trouvée n'est pas la 310.9 (lent). Vérifie qu'aucune vieille nvngx_dlssnr.dll ne traîne dans un sous-dossier du jeu.");
            }
            else if (m.GpuMode != null && m.GpuMode.StartsWith("AMD") && os != null) h.Add("Pas de nr_port.log : le port vendor-neutral n'a pas démarré (le jeu a-t-il atteint une partie avec FSR/XeSS actif ?).");
            if (h.Count == 0) h.Add("Rien de particulier repéré dans les logs.");
            return h;
        }

        static string Who(string sha) { string n = KnownFiles.Identify(sha); return n == null ? "  (version inconnue)" : "  → " + n; }

        public static string Write(Game g, string gpu)
        {
            var sb = new StringBuilder();
            var m = g.Installed;
            string dir = g.ExeDir, archive = null; bool inferred = false;
            if (m == null)
            {   // rien d'installé (déjà réinitialisé) : on prend le dernier journal archivé de ce jeu
                archive = LatestArchive(g);
                if (archive != null)
                {
                    dir = archive; m = Manifest.LoadFile(Path.Combine(archive, "manifest.tsv"));
                    if (m == null) { m = InferFromArchive(archive); inferred = m != null; }
                }
            }
            sb.AppendLine("# Fiche de test DLSS 5 — " + g.Name);
            sb.AppendLine();
            sb.AppendLine("- Date : " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
            sb.AppendLine("- GPU : " + gpu);
            sb.AppendLine("- Magasin : " + g.Store + (g.AppId != null ? " (AppID " + g.AppId + ")" : ""));
            sb.AppendLine("- Exe : `" + g.ExePath + "`");
            sb.AppendLine("- API détectée : " + g.ApiText + " — Upscalers : " + g.UpscalerText);
            if (m != null && inferred) sb.AppendLine("- Mode : **" + m.ModeLabel + "** (déduit des INI archivés : ce journal date d'une version du launcher qui ne gardait pas le manifeste)");
            else if (m != null) sb.AppendLine("- Mode : **" + m.ModeLabel + "** — proxy `" + m.Proxy + "` — installé le " + m.Date + (string.IsNullOrEmpty(m.GpuMode) ? "" : " — " + m.GpuMode));
            else if (archive != null) sb.AppendLine("- Mode : inconnu (journal archivé par une ancienne version du launcher, sans manifeste)");
            else sb.AppendLine("- Mode : (rien d'installé par le launcher)");
            if (archive != null) sb.AppendLine("- **Source : journal archivé le " + Path.GetFileName(archive).Replace("_avant-reset", "").Replace("_", " à ") + "**, juste avant la réinitialisation (`" + archive + "`)");
            if (g.Note != null) sb.AppendLine("- Note connue : " + g.Note);
            sb.AppendLine();

            sb.AppendLine("## Fichiers installés (SHA-256)");
            sb.AppendLine();
            if (archive != null)
            {   // les fichiers sont déjà retirés, mais le manifeste archivé garde leur hash
                var bins = m == null ? new List<ManEntry>() : m.Entries.Where(e => e.Kind != "MOVED" && e.Kind != "MOVEDDIR" && !string.IsNullOrEmpty(e.Sha) &&
                    (e.Rel.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || e.Rel.EndsWith(".addon64", StringComparison.OrdinalIgnoreCase))).ToList();
                if (bins.Count == 0) sb.AppendLine("(déjà retirés par la réinitialisation, et ce journal n'a pas de manifeste)");
                else sb.AppendLine("(déjà retirés par la réinitialisation ; hashs relus dans le manifeste archivé)");
                foreach (var e in bins) sb.AppendLine("- `" + e.Rel + "` : " + e.Sha + Who(e.Sha));
            }
            foreach (var f in new[] { "nvngx_dlssnr.dll", "nvngx.dll_dlssnr.dll", m != null ? m.Proxy : null, "renodx-dlss5.addon64" })
            {
                if (string.IsNullOrEmpty(f) || archive != null) continue;
                string p = Path.Combine(dir, f);
                if (File.Exists(p)) { string h = Hash.Sha(p); sb.AppendLine("- `" + f + "` : " + h + Who(h)); }
            }
            sb.AppendLine();

            sb.AppendLine("## Payload du launcher (docteur payload)");
            sb.AppendLine();
            try { sb.Append(PayloadDoctor.FicheSection(PayloadDoctor.Check())); } catch (Exception ex) { sb.AppendLine("- vérification impossible : " + ex.Message); }
            sb.AppendLine();

            sb.AppendLine("## Réglages NR actifs");
            sb.AppendLine();
            sb.AppendLine("```ini");
            string oi = Path.Combine(dir, "OptiScaler.ini"), ri = Path.Combine(dir, "ReShade.ini");
            if (m != null && m.UsesOptiIni && File.Exists(oi))
            {
                string t = Ini.ReadFile(oi);
                sb.AppendLine("[DlssNr]");
                var keys = new List<string> { "Enabled", "RunBeforeSR", "Passes", "WorkingScale", "Preset", "Style", "Intensity", "LocalStructure", "LocalTone", "SkinStructure", "AutoMask", "TransferStrength", "ColourStrength", "MaxRatio", "VitEvery", "DetailReuse" };
                if (m.Mode == "FEEDER") keys.AddRange(new[] { "NativeDepthFinder", "NativeMotion", "NativeMotionProfile", "NativeInput", "NativeUpscaler", "NativeUpscalerPass", "FinishedPicture" });
                foreach (var k in keys) sb.AppendLine(k + "=" + Ini.Get(t, "DlssNr", k));
            }
            else if (File.Exists(ri))
            {
                string t = Ini.ReadFile(ri);
                sb.AppendLine("[RenoDX.DLSS5]");
                foreach (var k in new[] { "EnableHooks", "NeuralUplift", "NRPreset", "NRStyle", "NRGlobalTone", "NRLocalStructure", "NRLocalTone", "NRSkinStructure", "NRAutoMask", "NRUICorrection", "NRDiffuseWhiteNits" })
                    sb.AppendLine(k + "=" + Ini.Get(t, "RenoDX.DLSS5", k));
            }
            sb.AppendLine("```");
            sb.AppendLine();

            sb.AppendLine("## Extrait du journal (lignes NR / erreurs)");
            sb.AppendLine();
            sb.AppendLine("```");
            bool found = false;
            foreach (var lf in new[] { "OptiScaler.log", "ReShade.log", "dlss5-feed.log", "nr_port.log" })
            {
                string p = Path.Combine(dir, lf);
                if (!File.Exists(p)) continue;
                found = true;
                sb.AppendLine("--- " + lf + " (" + File.GetLastWriteTime(p).ToString("yyyy-MM-dd HH:mm:ss") + ")");
                var hits = new List<string>();
                using (var fs = new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var rd = new StreamReader(fs))
                {
                    string line;
                    var rx = new Regex(@"dlss.?nr|neural|model feature|bad0000|evaluat|preset|ngx|error|fail|native motion|virtual upscaler|depth finder|feed", RegexOptions.IgnoreCase);
                    while ((line = rd.ReadLine()) != null) if (rx.IsMatch(line)) hits.Add(line.Length > 220 ? line.Substring(0, 220) + "…" : line);
                }
                foreach (var h in hits.Skip(Math.Max(0, hits.Count - 40))) sb.AppendLine(h);
            }
            if (!found) sb.AppendLine("(aucun journal — lance le jeu une fois avant de générer la fiche)");
            sb.AppendLine("```");
            sb.AppendLine();
            var perf = Perf.Read(dir);
            if (perf.Samples > 0)
            {
                sb.AppendLine("## Performance mesurée (OptiScaler.log)");
                sb.AppendLine();
                sb.AppendLine("- " + perf.Summary());
                sb.AppendLine("- " + perf.Advice());
                sb.AppendLine();
            }
            var elogs = EngineLogs.EngineLogFiles(g);
            if (archive != null && elogs.Count == 0)
                elogs = Directory.GetFiles(archive, "moteur_*").ToList();
            if (elogs.Count == 0 && g.Engine != null)
            {
                sb.AppendLine("## Log du moteur du jeu (" + g.Engine + ")");
                sb.AppendLine();
                sb.AppendLine("Introuvable. Beaucoup de jeux " + g.Engine + " en version finale n'écrivent pas de log. Cherché dans :");
                sb.AppendLine();
                foreach (var p in EngineLogs.LastSearched.Distinct().Take(12)) sb.AppendLine("- `" + p + "`");
                sb.AppendLine();
            }
            if (elogs.Count > 0)
            {
                sb.AppendLine("## Log du moteur du jeu (" + g.Engine + ")");
                sb.AppendLine();
                sb.AppendLine("```");
                foreach (var el in elogs)
                {
                    sb.AppendLine("--- " + el + " (" + File.GetLastWriteTime(el).ToString("yyyy-MM-dd HH:mm:ss") + ")");
                    var errs = EngineLogs.EngineErrors(el, 25);
                    if (errs.Count == 0) sb.AppendLine("(aucune ligne d'erreur GPU / crash)");
                    foreach (var e in errs) sb.AppendLine(e);
                }
                sb.AppendLine("```");
                sb.AppendLine();
            }
            sb.AppendLine("## Diagnostic automatique");
            sb.AppendLine();
            foreach (var h in Diagnose(g, dir, m)) sb.AppendLine("- " + h);
            if (archive != null && File.Exists(Path.Combine(archive, "OptiScaler_crash.dmp")))
                sb.AppendLine("- Un dump de crash (`OptiScaler_crash.dmp`) est dans le journal archivé : à garder pour l'analyse.");
            sb.AppendLine();
            sb.AppendLine("## Résultats observés (à remplir)");
            sb.AppendLine();
            sb.AppendLine("- Statut NR dans le menu (Inser / Début) :");
            sb.AppendLine("- Temps GPU NR / FPS :");
            sb.AppendLine("- Qualité visuelle :");
            sb.AppendLine("- Stabilité (durée de la session, crashs) :");
            sb.AppendLine("- Conclusion :");

            Directory.CreateDirectory(AppPaths.Fiches);
            string path = Path.Combine(AppPaths.Fiches, DateTime.Now.ToString("yyyy-MM-dd_HH-mm") + "_" + Installer.Safe(g.Name) + ".md");
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
            Log.W("Fiche de test créée : " + path);
            return path;
        }
    }
}

namespace Dlss5Launcher
{
    // Copie propre du launcher pour quelqu'un d'autre : rien de personnel (profils, journaux, quarantaine, dossiers de jeux).
    public static class FriendCopy
    {
        public static void Make(string dest)
        {
            Directory.CreateDirectory(dest);
            string root = AppPaths.Root;
            string exe = System.Reflection.Assembly.GetExecutingAssembly().Location;
            File.Copy(exe, Path.Combine(dest, "DLSS5-Launcher.exe"));
            foreach (var f in new[] { "LISEZMOI.md" }) if (File.Exists(Path.Combine(root, f))) File.Copy(Path.Combine(root, f), Path.Combine(dest, f));
            CopyTree(Path.Combine(root, "source"), Path.Combine(dest, "source"));
            CopyTree(AppPaths.Payload, Path.Combine(dest, "payload"));
            File.WriteAllText(Path.Combine(dest, "POUR-TON-AMI.txt"), Text, new UTF8Encoding(true));
            // l'anti-cheat est bloqué d'office chez l'ami ; il peut le débloquer lui-même, avec un avertissement
            File.WriteAllLines(Path.Combine(dest, "launcher-config.ini"), new[] { "# Configuration du DLSS 5 Launcher (copie pour un ami)", "bloquerAntiCheat=1" });
            Log.W("Copie pour un ami créée : " + dest);
        }

        // Vérifie le payload de la copie (tout compte : l'ami peut avoir n'importe quelle carte).
        public static List<PItem> Verify(string dest) { return PayloadDoctor.Check(Path.Combine(dest, "payload"), true); }

        static void CopyTree(string src, string dst)
        {
            if (!Directory.Exists(src)) return;
            Directory.CreateDirectory(dst);
            foreach (var f in Directory.GetFiles(src))
            {
                if (Path.GetFileName(f).Equals("_zip", StringComparison.OrdinalIgnoreCase)) continue;
                string t = Path.Combine(dst, Path.GetFileName(f));
                File.Copy(f, t);
                if (new FileInfo(t).Length != new FileInfo(f).Length) throw new IOException("copie incomplète : " + f);
            }
            foreach (var d in Directory.GetDirectories(src))
                if (!Path.GetFileName(d).Equals("_zip", StringComparison.OrdinalIgnoreCase)) CopyTree(d, Path.Combine(dst, Path.GetFileName(d)));
        }

        const string Text =
"DLSS 5 Launcher — à lire en premier\r\n" +
"===================================\r\n\r\n" +
"1. Double-clique DLSS5-Launcher.exe (Windows SmartScreen va avertir : « Informations complémentaires » → « Exécuter quand même »).\r\n" +
"2. Il scanne tes jeux Steam/Epic/GOG. Choisis un jeu, clique « Installer DLSS 5 ». « Réinitialiser le jeu » remet tout comme avant.\r\n\r\n" +
"CARTE NVIDIA RTX 50 (ex. RTX 5070) — détectée automatiquement\r\n" +
"- Le launcher installe la 310.9.16 NVIDIA d'origine (code natif RTX 50) au lieu de la build OpenNR prévue pour les RTX 20/30/40.\r\n" +
"- Si ça ne démarre pas : ajoute la ligne  dllNr=openNR  dans launcher-config.ini, puis réinstalle.\r\n" +
"- Pilote NVIDIA 616.56 ou plus récent requis.\r\n\r\n" +
"CARTE AMD (ex. RX 9070 XT) — détectée automatiquement\r\n" +
"- Le launcher utilise le « port vendor-neutral » d'OptiScaler F5 0.1.32 : il fait tourner le modèle DLSS 5 en DirectX 12 + DirectML,\r\n" +
"  sans NVIDIA. Avec la 310.9.16 d'origine de NVIDIA (petit modèle, Preset B).\r\n" +
"- EXPÉRIMENTAL : personne n'a encore publié de résultat sur une 9070 XT. Sur une RX 6700 XT, le développeur mesurait ~59 ms par image\r\n" +
"  à 50 % de résolution (injouable). RDNA4 devrait être bien plus rapide grâce à ses unités matricielles, mais rien n'est garanti.\r\n" +
"- Méthodes possibles : OptiScaler F5 (jeux avec FSR/XeSS/DLSS — active FSR dans le jeu) et Feeder F5Low (jeux sans upscaler).\r\n" +
"  RenoDX et le Feeder ReShade ne marchent pas sur AMD (ils passent par NVIDIA).\r\n" +
"- Préréglage de départ « AMD Radeon » : modèle à 50 %, réutilisation du détail. Si c'est trop lent : génération d'images FSR FG\r\n" +
"  (onglet Réglages NR → Fluidité et FPS).\r\n" +
"- Pour vérifier : touche Inser en jeu → Neural Rendering → la ligne doit dire « Model backend: vendor-neutral port ».\r\n" +
"  Après la partie, « Fiche de test » lit nr_port.log et dit si le bon modèle (small) tournait.\r\n\r\n" +
"À SAVOIR\r\n" +
"- Jamais dans un jeu en ligne avec anti-cheat (ban possible) : le launcher les marque en rouge et, dans cette copie,\r\n" +
"  refuse d'y installer quoi que ce soit. Pour un jeu solo joué hors ligne, tu peux décocher « Bloquer l'installation\r\n" +
"  dans les jeux avec anti-cheat » (onglet Jeu) : c'est à tes risques. La détection se fait par fichiers : un anti-cheat\r\n" +
"  côté serveur ou installé ailleurs peut passer inaperçu, donc le blocage ne garantit rien.\r\n" +
"- En haut de la fenêtre, « Payload » dit si tous les fichiers sont là et intacts. S'il y a un problème, clique dessus :\r\n" +
"  « Copier le rapport » et envoie-le.\r\n" +
"- Les DLL nvngx_* viennent de NVIDIA et restent sous la licence de NVIDIA ; OptiScaler F5 est un projet libre\r\n" +
"  (github.com/janblade/OptiScaler-F5-DLSSNR-Multipass), d'où vient aussi le port vendor-neutral.\r\n";
    }
}
