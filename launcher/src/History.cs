// Licence MIT (voir LICENSE-MIT du dépôt travail-avec-ia). Code écrit par Claude (Anthropic) pour On-100-K-Lisse.
// DLSS5 Launcher — historique des sessions : une ligne par partie (OptiScaler.log), dans historique.csv.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Dlss5Launcher
{
    public class Session
    {
        public string Key, Game, Card, Method, Dll, Preset, Model, Crash, Source, Step = "", Note = "", Opti = "";
        public string MethodText { get { return Method + (string.IsNullOrEmpty(Opti) ? "" : " (" + Opti + ")"); } }
        public DateTime Start, End;
        public double Minutes, Avg, Min, Max, P95, First, Last;
        public int Samples, Creations;

        public string PresetText { get { return Preset == "1" ? "A" : Preset == "2" ? "B" : string.IsNullOrEmpty(Preset) ? "?" : Preset; } }
        public bool Crashed { get { return !string.IsNullOrEmpty(Crash) && Crash != History.NoCrash; } }
        // dérive pendant la session : coût du dernier quart des mesures par rapport au premier
        public double Drift { get { return First > 0 ? (Last - First) / First : 0; } }
    }

    public static class History
    {
        public const string NoCrash = "aucun détecté";
        static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-CA");
        static readonly object Lk = new object();
        public static string FilePath { get { return Path.Combine(AppPaths.Root, "historique.csv"); } }

        static readonly string[] Cols = { "Début", "Fin", "Durée (min)", "Jeu", "Carte", "Méthode", "OptiScaler", "DLL NR", "Preset", "Modèle",
            "NR moyen (ms)", "NR min", "NR max", "NR p95", "NR début de partie", "NR fin de partie", "Mesures", "Créations du modèle", "Crash", "Étape", "Note", "Source", "Clé" };

        // ------------------------------------------------------------ lecture d'un log
        // dir : dossier de l'exe (session en cours ou dernière partie) ou journal archivé. m : manifeste (peut être null).
        public static Session FromLog(string gameName, string dir, Manifest m, string source)
        {
            string log = Path.Combine(dir, "OptiScaler.log");
            if (!File.Exists(log)) return null;
            string text;
            using (var fs = new FileStream(log, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var rd = new StreamReader(fs)) text = rd.ReadToEnd();
            var ts = Regex.Matches(text, @"^\[(\d\d):(\d\d):(\d\d)(?:\.\d+)?\]", RegexOptions.Multiline);
            if (ts.Count == 0) return null;
            TimeSpan t0 = Tod(ts[0]), t1 = Tod(ts[ts.Count - 1]);
            TimeSpan dur = t1 - t0; if (dur < TimeSpan.Zero) dur += TimeSpan.FromDays(1);   // partie passée minuit
            DateTime end = File.GetLastWriteTime(log);
            end = end.Date + t1 > end.AddMinutes(5) ? end.Date.AddDays(-1) + t1 : end.Date + t1;   // l'heure du log fait foi
            var s = new Session { Game = gameName, Start = end - dur, End = end, Minutes = dur.TotalMinutes, Source = source };
            s.Key = Installer.Safe(gameName) + "|" + s.Start.ToString("yyyy-MM-dd HH:mm:ss");

            var pf = Perf.Read(dir);
            s.Samples = pf.Samples; s.Preset = pf.Preset; s.Model = pf.ModelInput;
            if (pf.Samples > 0)
            {
                var v = pf.Values; s.Avg = pf.AvgMs; s.Min = pf.MinMs; s.Max = pf.MaxMs;
                var sorted = v.OrderBy(x => x).ToList(); s.P95 = sorted[Math.Min(sorted.Count - 1, (int)Math.Ceiling(sorted.Count * 0.95) - 1)];
                int q = Math.Max(1, v.Count / 4);
                s.First = v.Take(q).Average(); s.Last = v.Skip(v.Count - q).Average();
            }
            s.Creations = Regex.Matches(text, @"model feature created").Count;
            s.Crash = CrashType(text);
            s.Step = Steps.At(gameName, s.End);
            var ov = Regex.Match(text, @"OptiScaler v(\d+\.\d+\.\d+)");   // 1re ligne du log : « OptiScaler v0.1.32-final … loaded »
            if (ov.Success) s.Opti = ov.Groups[1].Value;
            s.Method = m != null ? m.ModeLabel : "inconnue";
            s.Card = m != null && !string.IsNullOrEmpty(m.Card) ? m.Card : m != null && !string.IsNullOrEmpty(m.GpuMode) ? m.GpuMode : "inconnue";
            var e = m == null ? null : m.Entries.FirstOrDefault(x => x.Rel.Equals("nvngx_dlssnr.dll", StringComparison.OrdinalIgnoreCase) && x.Kind != "MOVED");
            s.Dll = e == null ? "inconnue" : KnownFiles.Identify(e.Sha) ?? ("version inconnue (" + (e.Sha.Length > 8 ? e.Sha.Substring(0, 8) : e.Sha) + "…)");
            return s;
        }

        static TimeSpan Tod(Match x) { return new TimeSpan(int.Parse(x.Groups[1].Value), int.Parse(x.Groups[2].Value), int.Parse(x.Groups[3].Value)); }

        public static string CrashType(string os)
        {
            var hung = Regex.Match(os, @"Device removed reason: (\w+)");
            if (hung.Success) return "crash GPU (" + hung.Groups[1].Value + ")";
            var ex = Regex.Match(os, @"Unhandled exception (\w+)");
            if (ex.Success) return "exception 0x" + ex.Groups[1].Value + (ex.Groups[1].Value == "8000" || ex.Groups[1].Value == "8001" ? " (Unreal)" : "");
            string tail = os.Length > 20000 ? os.Substring(os.Length - 20000) : os;
            if (Regex.IsMatch(tail, @"Failed to map buffer") && Regex.IsMatch(tail, @"timestampData is null")) return "perte du GPU probable (Failed to map buffer)";
            return NoCrash;
        }

        // ------------------------------------------------------------ fichier
        public static List<Session> Load()
        {
            var r = new List<Session>();
            lock (Lk)
            {
                if (!File.Exists(FilePath)) return r;
                string[] lines;
                try { lines = File.ReadAllLines(FilePath, Encoding.UTF8); } catch { return r; }
                if (lines.Length < 2) return r;
                var head = lines[0].TrimStart('﻿').Split(';');
                Func<string[], string, string> col = (p, n) => { int i = Array.IndexOf(head, n); return i >= 0 && i < p.Length ? p[i] : ""; };
                foreach (var l in lines.Skip(1))
                {
                    var p = l.Split(';'); if (p.Length < 5) continue;
                    var s = new Session
                    {
                        Start = Dt(col(p, "Début")), End = Dt(col(p, "Fin")), Minutes = Num(col(p, "Durée (min)")), Game = col(p, "Jeu"), Card = col(p, "Carte"),
                        Method = col(p, "Méthode"), Opti = col(p, "OptiScaler"), Dll = col(p, "DLL NR"), Preset = col(p, "Preset") == "A" ? "1" : col(p, "Preset") == "B" ? "2" : col(p, "Preset"),
                        Model = col(p, "Modèle"), Avg = Num(col(p, "NR moyen (ms)")), Min = Num(col(p, "NR min")), Max = Num(col(p, "NR max")), P95 = Num(col(p, "NR p95")),
                        First = Num(col(p, "NR début de partie")), Last = Num(col(p, "NR fin de partie")), Samples = (int)Num(col(p, "Mesures")),
                        Creations = (int)Num(col(p, "Créations du modèle")), Crash = col(p, "Crash"), Step = col(p, "Étape"), Note = col(p, "Note"), Source = col(p, "Source"), Key = col(p, "Clé")
                    };
                    if (!string.IsNullOrEmpty(s.Key)) r.Add(s);
                }
            }
            return r;
        }

        static void Save(List<Session> all)
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(";", Cols));
            foreach (var s in all.OrderBy(x => x.Start))
                sb.AppendLine(string.Join(";", new[] {
                    s.Start.ToString("yyyy-MM-dd HH:mm"), s.End.ToString("yyyy-MM-dd HH:mm"), N(s.Minutes, "0"), s.Game, s.Card, s.Method, s.Opti, s.Dll, s.Samples > 0 ? s.PresetText : "", s.Model ?? "",
                    N(s.Avg), N(s.Min), N(s.Max), N(s.P95), N(s.First), N(s.Last), s.Samples.ToString(), s.Creations.ToString(), s.Crash, s.Step, s.Note, s.Source, s.Key }.Select(Clean)));
            string tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, sb.ToString(), new UTF8Encoding(true));   // BOM : Excel l'ouvre avec les accents
            if (File.Exists(FilePath)) File.Delete(FilePath);
            File.Move(tmp, FilePath);
        }

        // Ajoute la session, ou la met à jour si elle y est déjà (même jeu, même heure de début).
        public static Session Record(Session s)
        {
            if (s == null) return null;
            lock (Lk)
            {
                var all = Load();
                var old = all.FirstOrDefault(x => x.Key == s.Key);
                if (old != null)
                {
                    // une partie déjà notée depuis le dossier du jeu garde ses infos ; le journal archivé devient la source
                    if (s.Samples < old.Samples && old.Source.StartsWith("journal")) return old;
                    if (string.IsNullOrEmpty(s.Note)) s.Note = old.Note;
                    all.Remove(old);
                }
                all.Add(s);
                try { Save(all); } catch (Exception ex) { Log.W("Historique : écriture impossible — " + ex.Message); }
            }
            return s;
        }

        public static Session RecordLive(Game g)
        {
            try { return g.ExeDir == null ? null : Record(FromLog(g.Name, g.ExeDir, g.Installed, "dossier du jeu")); }
            catch (Exception ex) { Log.W("Historique (" + g.Name + ") : " + ex.Message); return null; }
        }

        public static Session RecordArchive(string gameName, string archiveDir)
        {
            try
            {
                var m = Manifest.LoadFile(Path.Combine(archiveDir, "manifest.tsv")) ?? Report.InferFromArchive(archiveDir);
                return Record(FromLog(gameName, archiveDir, m, "journal " + Path.GetFileName(archiveDir)));
            }
            catch (Exception ex) { Log.W("Historique (" + gameName + ") : " + ex.Message); return null; }
        }

        // Au démarrage : reprend les journaux archivés pas encore notés, et la dernière partie des jeux installés.
        public static int Backfill(IEnumerable<Game> games)
        {
            int n = 0;
            var known = new HashSet<string>(Load().Select(x => x.Key.Split('|')[0] + "|" + x.Source));
            var byFolder = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var g in games) byFolder[Installer.Safe(g.Name)] = g.Name;
            if (Directory.Exists(AppPaths.Journaux))
                foreach (var gd in Directory.GetDirectories(AppPaths.Journaux))
                {
                    string folder = Path.GetFileName(gd), name;
                    if (!byFolder.TryGetValue(folder, out name)) name = folder;
                    foreach (var a in Directory.GetDirectories(gd))
                        if (!known.Contains(Installer.Safe(name) + "|journal " + Path.GetFileName(a)) && File.Exists(Path.Combine(a, "OptiScaler.log")))
                            if (RecordArchive(name, a) != null) n++;
                }
            foreach (var g in games.Where(x => x.Installed != null))
                if (RecordLive(g) != null) n++;
            return n;
        }

        public static List<Session> For(Game g)
        {
            string k = Installer.Safe(g.Name) + "|";
            return Load().Where(x => x.Key.StartsWith(k, StringComparison.Ordinal)).OrderBy(x => x.Start).ToList();
        }

        // ------------------------------------------------------------ résumés
        // Regroupe les parties d'un jeu par montage (méthode, DLL, preset, modèle) pour comparer, par exemple, Preset A et B.
        public static List<string> Compare(List<Session> ss)
        {
            var r = new List<string>();
            foreach (var grp in ss.Where(x => x.Samples > 0).GroupBy(x => x.MethodText + " · " + DllShort(x.Dll) + " · Preset " + x.PresetText + (string.IsNullOrEmpty(x.Model) ? "" : " · modèle " + x.Model)).OrderBy(x => x.Key))
            {
                var l = grp.ToList();
                double minutes = l.Sum(x => x.Minutes);
                double avg = l.Sum(x => x.Avg * x.Samples) / Math.Max(1, l.Sum(x => x.Samples));
                int crashes = l.Count(x => x.Crashed);
                r.Add(grp.Key + " : " + l.Count + " partie" + (l.Count > 1 ? "s" : "") + ", " + Hours(minutes) + ", NR ≈ " + N(avg) + " ms" +
                      (crashes > 0 ? ", " + crashes + " crash" + (crashes > 1 ? "s" : "") + " (" + Hours(minutes / crashes) + " de jeu par crash)" : ", aucun crash détecté"));
            }
            return r;
        }

        // Protocole A/B : une ligne par étape, dans l'ordre des étapes, puis un verdict prudent.
        public static List<string> CompareSteps(List<Session> ss)
        {
            var r = new List<string>();
            var steps = ss.Where(x => !string.IsNullOrEmpty(x.Step)).GroupBy(x => x.Step).OrderBy(x => x.Key, StringComparer.CurrentCultureIgnoreCase).ToList();
            if (steps.Count == 0) return r;
            foreach (var st in steps)
            {
                var l = st.ToList(); double minutes = l.Sum(x => x.Minutes); int crashes = l.Count(x => x.Crashed);
                int meas = l.Sum(x => x.Samples);
                var methods = l.Select(x => x.Samples > 0 ? x.MethodText + " · Preset " + x.PresetText : x.Method).Distinct().ToList();
                r.Add("« " + st.Key + " » : " + l.Count + " partie" + (l.Count > 1 ? "s" : "") + ", " + Hours(minutes) +
                      (meas > 0 ? ", NR ≈ " + N(l.Sum(x => x.Avg * x.Samples) / meas) + " ms" : "") +
                      (crashes > 0 ? ", " + crashes + " crash" + (crashes > 1 ? "s" : "") + " (un toutes les " + Hours(minutes / crashes) + ")" : ", aucun crash") +
                      "  —  " + string.Join(" / ", methods));
            }
            // verdict : la première étape qui plante, alors que toutes celles d'avant n'ont jamais planté
            for (int i = 0; i < steps.Count; i++)
            {
                var l = steps[i].ToList(); int c = l.Count(x => x.Crashed);
                if (c == 0) continue;
                if (i == 0) { r.Add("→ Ça plante dès la première étape (« " + steps[0].Key + " ») : le problème est déjà là sans ce qu'ajoutent les étapes suivantes."); break; }
                var before = steps.Take(i).SelectMany(x => x).ToList();
                if (before.Any(x => x.Crashed)) break;
                double per = l.Sum(x => x.Minutes) / c, prev = before.Sum(x => x.Minutes);
                string first = "→ Premier crash à l'étape « " + steps[i].Key + " ». Les étapes d'avant n'ont aucun crash en " + Hours(prev) + " de jeu.";
                // sans crash pendant 3 fois le temps moyen avant un crash : le hasard n'explique plus vraiment (≈ 5 % de chances)
                if (prev >= 3 * per) r.Add(first + " C'est donc probablement ce qu'ajoute cette étape qui fait planter (à confirmer en rejouant).");
                else if (prev >= per) r.Add(first + " C'est un indice, pas encore une preuve : à cette étape, ça plante environ toutes les " + Hours(per) +
                                            " ; pour conclure, il faudrait environ " + Hours(3 * per) + " de jeu sans crash aux étapes d'avant.");
                else r.Add(first + " Mais c'est trop peu pour conclure : à cette étape, ça plante environ toutes les " + Hours(per) +
                           " ; il faudrait environ " + Hours(3 * per) + " de jeu sans crash aux étapes d'avant.");
                break;
            }
            if (!steps.Any(x => x.Any(y => y.Crashed))) r.Add("→ Aucun crash à aucune étape pour l'instant (" + Hours(ss.Where(x => !string.IsNullOrEmpty(x.Step)).Sum(x => x.Minutes)) + " de jeu).");
            return r;
        }

        // Partie notée à la main (ex. étape « vanille », sans OptiScaler : aucun log à lire).
        public static Session RecordManual(string gameName, DateTime start, double minutes, string crash, string step, string note)
        {
            var s = new Session
            {
                Game = gameName, Start = start, End = start.AddMinutes(minutes), Minutes = minutes, Method = "sans DLSS 5", Dll = "aucune", Card = Gpu.Name ?? "inconnue",
                Preset = "", Model = "", Crash = string.IsNullOrEmpty(crash) ? NoCrash : crash, Step = step ?? "", Note = note ?? "", Source = "noté à la main"
            };
            s.Key = Installer.Safe(gameName) + "|" + start.ToString("yyyy-MM-dd HH:mm:ss");
            return Record(s);
        }

        public static string Line(Session s)
        {
            return s.Start.ToString("yyyy-MM-dd HH:mm") + "  " + Hours(s.Minutes).PadLeft(7) + "  " + (s.Samples > 0 ? ("NR " + N(s.Avg) + " ms").PadRight(12) : "pas de mesure").PadRight(12) +
                   (string.IsNullOrEmpty(s.Step) ? "" : "  [" + s.Step + "]") +
                   (s.Samples > 0 ? "  Preset " + s.PresetText + (string.IsNullOrEmpty(s.Model) ? "" : " " + s.Model) : "") + "  " + s.MethodText + (s.Crashed ? "  ← " + s.Crash : "") +
                   (s.Samples >= 8 && s.Drift >= 0.2 ? "  (NR +" + (s.Drift * 100).ToString("0", Fr) + " % en fin de partie)" : "") +
                   (string.IsNullOrEmpty(s.Note) ? "" : "  — " + s.Note);
        }

        public static string DllShort(string dll)
        {
            if (dll == null || dll == "inconnue") return "DLL inconnue";
            if (dll.Contains("OpenNR")) return "openNR";
            if (dll.Contains("d'origine")) return "NR d'origine";
            return dll;
        }

        public static string Hours(double minutes)
        {
            if (minutes < 60) return minutes.ToString("0", Fr) + " min";
            return ((int)(minutes / 60)) + " h " + ((int)(minutes % 60)).ToString("00");
        }

        static string N(double v) { return N(v, "0.0"); }
        static string N(double v, string f) { return v.ToString(f, Fr); }
        static double Num(string s) { double v; return double.TryParse(s, NumberStyles.Float, Fr, out v) ? v : 0; }
        static DateTime Dt(string s) { DateTime d; return DateTime.TryParseExact(s, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out d) ? d : DateTime.MinValue; }
        static string Clean(string s) { return (s ?? "").Replace(";", ",").Replace("\r", " ").Replace("\n", " "); }
    }

    // Étapes de test (protocole A/B) : l'étape choisie pour un jeu, avec l'heure du choix (etapes.tsv).
    // Une partie prend l'étape qui était active à sa fin.
    public static class Steps
    {
        public static readonly string[] Suggestions = { "1 - Vanille (aucun mod)", "2 - DLSS 5 seul", "3 - DLSS 5 + mods", "Preset A", "Preset B" };
        static readonly object Lk = new object();
        public static string FilePath { get { return Path.Combine(AppPaths.Root, "etapes.tsv"); } }

        static List<string[]> All()
        {
            var r = new List<string[]>();
            lock (Lk)
            {
                if (!File.Exists(FilePath)) return r;
                try { foreach (var l in File.ReadAllLines(FilePath, Encoding.UTF8)) { var p = l.Split('\t'); if (p.Length >= 3 && !l.StartsWith("#")) r.Add(p); } } catch { }
            }
            return r;
        }

        public static string At(string gameName, DateTime t)
        {
            string k = Installer.Safe(gameName);
            string cur = "";
            foreach (var p in All())
            {
                DateTime d;
                if (p[0] == k && DateTime.TryParseExact(p[1], "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out d) && d <= t) cur = p[2];
            }
            return cur;
        }
        public static string Current(string gameName) { return At(gameName, DateTime.Now.AddSeconds(1)); }

        public static void Set(string gameName, string step)
        {
            step = (step ?? "").Replace("\t", " ").Replace(";", ",").Trim();
            if (step == Current(gameName)) return;
            lock (Lk)
            {
                if (!File.Exists(FilePath)) File.WriteAllText(FilePath, "# jeu<TAB>depuis<TAB>étape — écrit par le launcher\r\n", new UTF8Encoding(true));
                File.AppendAllText(FilePath, Installer.Safe(gameName) + "\t" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\t" + step + "\r\n", new UTF8Encoding(false));
            }
            Log.W("Étape de test de " + gameName + " : " + (step == "" ? "(aucune)" : step));
        }
    }
}
