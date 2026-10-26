// Licence MIT (voir LICENSE-MIT du dépôt travail-avec-ia). Code écrit par Claude (Anthropic) pour On-100-K-Lisse.
// DLSS5 Launcher — interface (WinForms, thème sombre, en français).
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;

namespace Dlss5Launcher
{
    static class Theme
    {
        public static readonly Color Bg = Color.FromArgb(18, 20, 24);
        public static readonly Color Panel = Color.FromArgb(27, 30, 36);
        public static readonly Color Panel2 = Color.FromArgb(36, 40, 48);
        public static readonly Color Line = Color.FromArgb(52, 57, 68);
        public static readonly Color Text = Color.FromArgb(230, 232, 236);
        public static readonly Color Dim = Color.FromArgb(150, 156, 168);
        public static readonly Color Green = Color.FromArgb(118, 185, 0);
        public static readonly Color Yellow = Color.FromArgb(230, 190, 60);
        public static readonly Color Red = Color.FromArgb(235, 90, 80);
        public static readonly Color Blue = Color.FromArgb(90, 170, 255);
        public static readonly Color Orange = Color.FromArgb(240, 140, 50);
        public static readonly Color Cyan = Color.FromArgb(70, 200, 215);
        public static readonly Font UI = new Font("Segoe UI", 9.5f);
        public static readonly Font UIBold = new Font("Segoe UI Semibold", 9.5f);
        public static readonly Font Title = new Font("Segoe UI Semibold", 16f);
        public static readonly Font H2 = new Font("Segoe UI Semibold", 13f);
        public static readonly Font Mono = new Font("Consolas", 9f);

        public static Color ForStatus(GameStatus s)
        {
            switch (s)
            {
                case GameStatus.Compatible: return Green;
                case GameStatus.ATester: return Yellow;
                case GameStatus.FeederPossible: return Cyan;
                case GameStatus.Installe: return Blue;
                case GameStatus.Bloque: return Red;
                case GameStatus.AutreMod: case GameStatus.Incomplet: return Orange;
                default: return Dim;
            }
        }
        public static int Rank(GameStatus s)
        {
            switch (s)
            {
                case GameStatus.Installe: return 0; case GameStatus.Incomplet: return 1; case GameStatus.Compatible: return 2;
                case GameStatus.ATester: return 3; case GameStatus.FeederPossible: return 4; case GameStatus.AutreMod: return 5; case GameStatus.PeuProbable: return 5;
                case GameStatus.Bloque: return 6; default: return 7;
            }
        }

        public static Button Btn(string text, Color accent, int w = 150, int h = 34)
        {
            var b = new Button
            {
                Text = text, Width = w, Height = h, FlatStyle = FlatStyle.Flat, BackColor = Panel2, ForeColor = Text,
                Font = UIBold, Cursor = Cursors.Hand, Margin = new Padding(0, 0, 8, 8), UseVisualStyleBackColor = false
            };
            b.FlatAppearance.BorderColor = accent; b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(48, 54, 64);
            return b;
        }
        public static void Dark(Control c)
        {
            c.BackColor = Panel2; c.ForeColor = Text; c.Font = UI;
        }
    }

    public class MainForm : Form
    {
        // données
        List<Game> games = new List<Game>();
        List<string> customFolders = new List<string>();
        NrSettings settings = new NrSettings();
        string gpuText;
        Game selected;
        volatile bool busy;

        // contrôles
        ListView list;
        TextBox search, journal, details;
        Label lblGame, lblStatus, lblGpu, lblFooter, lblNote, lblPayload;
        CheckBox chkAntiCheat;
        bool blockAntiCheat;                         // launcher-config.ini : bloquerAntiCheat=1 (d'office dans la copie pour un ami)
        List<PItem> payloadItems;
        ComboBox cbMode, cbProxy, cbPreset, cbStep, cbOpti;
        List<string> optiFolders = new List<string>();
        bool optiGuard;
        NrSettings defaults = new NrSettings();    // réglages par défaut (jeux sans profil)
        string defaultPresetName = Presets.Names[0], presetName = Presets.Names[0];
        Label lblProfile;
        Button bImport, bMakeDefault, bResetProfile;
        static string ProfilesDir { get { return Path.Combine(AppPaths.Root, "profils"); } }
        static string ProfilePath(Game g) { return Path.Combine(ProfilesDir, Installer.Safe(g.Name) + ".ini"); }
        Button bInstall, bReset, bLaunch, bFolder, bLog, bReport, bScan, bAdd, bApply, bJournaux, bUndo, bHistory, bManual, bAdvice;
        Panel pageJeu, pageReglages, pageJournal;
        Button tabJeu, tabReglages, tabJournal;
        ProgressBar progress;
        readonly List<Action> refreshers = new List<Action>();
        ToolTip tips = new ToolTip { AutoPopDelay = 20000, InitialDelay = 300 };

        public MainForm()
        {
            Text = "DLSS 5 Launcher";
            Width = 1320; Height = 840; MinimumSize = new Size(1100, 700);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.Bg; ForeColor = Theme.Text; Font = Theme.UI;
            AutoScaleMode = AutoScaleMode.Dpi;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            bool old; gpuText = Gpu.Describe(out old);
            ForcedGpuFromConfig();
            bool firstRun = true;
            try { firstRun = !File.Exists(AppPaths.Config) || !File.ReadAllText(AppPaths.Config).Contains("presetName="); } catch { }
            LoadConfig();
            if (firstRun && Gpu.IsAmdMode) { defaults = Presets.Get(8); defaultPresetName = Presets.Names[8]; settings = defaults.Clone(); presetName = defaultPresetName; }

            BuildHeader();
            BuildBody();
            Log.Line += l => { if (IsHandleCreated) BeginInvoke((Action)(() => journal.AppendText(l + Environment.NewLine))); };
            Shown += (s, e) =>
            {
                Log.W("Launcher démarré — " + gpuText + (IsAdmin() ? " — mode administrateur" : ""));
                if (old) Log.W("ATTENTION : pilote NVIDIA plus vieux que 616.56 (minimum demandé par OptiScaler F5).");
                CheckPayloadAtStart();
                FillOpti();
                list.Focus();
                StartScan();
            };
            FormClosing += (s, e) =>
            {
                if (watchGame != null && e.CloseReason == CloseReason.UserClosing &&
                    Ask("Une partie de " + watchGame.Name + " est surveillée.\n\nSi tu fermes le launcher, elle sera quand même notée au prochain démarrage (si OptiScaler a écrit son log), mais la fiche automatique en cas de crash ne sera pas faite.\n\nFermer quand même ?", MessageBoxIcon.Question) != DialogResult.Yes)
                { e.Cancel = true; return; }
                SaveConfig();
            };
        }

        // =============================================================== en-tête
        void BuildHeader()
        {
            var head = new Panel { Dock = DockStyle.Top, Height = 100, BackColor = Theme.Panel, Padding = new Padding(18, 10, 18, 10) };
            var title = new Label { Text = "DLSS 5 Launcher", Font = Theme.Title, ForeColor = Theme.Text, AutoSize = true, Location = new Point(18, 10) };
            lblGpu = new Label { Text = gpuText + "   ·   DLL NR : " + Gpu.NrLabel + (Gpu.IsAmdMode ? " + port vendor-neutral 0.1.32" : ""), ForeColor = Gpu.IsAmdMode ? Theme.Orange : Theme.Dim, AutoSize = true, Location = new Point(21, 44) };
            var accent = new Panel { Dock = DockStyle.Bottom, Height = 2, BackColor = Theme.Green };
            lblPayload = new Label { Text = "Payload : vérification…", ForeColor = Theme.Dim, AutoSize = true, Location = new Point(21, 68), Cursor = Cursors.Hand };
            lblPayload.Click += (s, e) => ShowDoctor();
            tips.SetToolTip(lblPayload, "Docteur payload : vérifie que les fichiers de payload\\ sont tous là, intacts (SHA-256)\net dans la version testée. Clique pour voir le rapport.");

            var right = new FlowLayoutPanel { Dock = DockStyle.Right, Width = 620, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Padding = new Padding(0, 10, 0, 0), BackColor = Theme.Panel };
            search = new TextBox { Width = 230, Margin = new Padding(0, 6, 10, 0), BorderStyle = BorderStyle.FixedSingle };
            Theme.Dark(search);
            SetCue(search, "Rechercher un jeu…");
            search.TextChanged += (s, e) => FillList();
            bScan = Theme.Btn("⟳  Scanner", Theme.Green, 130);
            bScan.Click += (s, e) => StartScan();
            bAdd = Theme.Btn("+  Ajouter un dossier", Theme.Line, 190);
            bAdd.Click += (s, e) => AddFolder();
            var bFriend = Theme.Btn("Copie pour un ami", Theme.Line, 160);
            bFriend.Click += (s, e) => DoFriendCopy();
            tips.SetToolTip(bFriend, "Crée une copie propre du launcher (exe + payload, y compris le mode AMD),\nsans tes profils, journaux, quarantaine ni dossiers de jeux.");
            right.Width = 720; search.Width = 170; bAdd.Width = 170; bScan.Width = 115; bFriend.Width = 150;
            right.Controls.AddRange(new Control[] { search, bScan, bAdd, bFriend });

            head.Controls.Add(right); head.Controls.Add(title); head.Controls.Add(lblGpu); head.Controls.Add(lblPayload); head.Controls.Add(accent);
            Controls.Add(head);

            var foot = new Panel { Dock = DockStyle.Bottom, Height = 28, BackColor = Theme.Panel };
            lblFooter = new Label { Dock = DockStyle.Fill, ForeColor = Theme.Dim, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(12, 0, 0, 0) };
            progress = new ProgressBar { Dock = DockStyle.Right, Width = 220, Style = ProgressBarStyle.Continuous, Visible = false };
            foot.Controls.Add(lblFooter); foot.Controls.Add(progress);
            Controls.Add(foot);
        }

        // =============================================================== corps
        void BuildBody()
        {
            var split = new SplitContainer { Dock = DockStyle.Fill, BackColor = Theme.Line, SplitterWidth = 2, FixedPanel = FixedPanel.None };
            Controls.Add(split);
            split.BringToFront();
            split.Panel1.BackColor = Theme.Bg; split.Panel2.BackColor = Theme.Bg;
            split.Panel1.Padding = new Padding(14, 14, 8, 14);
            HandleCreated += (s, e) => { try { split.SplitterDistance = (int)(Width * 0.50); } catch { } };

            list = new ListView
            {
                Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, MultiSelect = false, HideSelection = false,
                BackColor = Theme.Panel, ForeColor = Theme.Text, BorderStyle = BorderStyle.None, Font = Theme.UI, HeaderStyle = ColumnHeaderStyle.Nonclickable
            };
            list.Columns.Add("Jeu", 250); list.Columns.Add("Magasin", 70); list.Columns.Add("Statut", 150);
            list.Columns.Add("API", 85); list.Columns.Add("Upscalers", 140);
            list.SelectedIndexChanged += (s, e) =>
            {
                var ns = list.SelectedItems.Count > 0 ? (Game)list.SelectedItems[0].Tag : null;
                if (ns != selected) { CommitStep(); selected = ns; LoadProfile(selected); LoadStep(); LoadOpti(); }
                ShowGame();
            };
            list.DoubleClick += (s, e) => { if (selected != null) OpenFolder(); };
            var legend = new Label
            {
                Dock = DockStyle.Bottom, Height = 46, ForeColor = Theme.Dim, Padding = new Padding(2, 8, 0, 0),
                Text = "Vert : DLSS + DX12  ·  Jaune : à tester  ·  Cyan : sans upscaler → Feeder  ·  Bleu : installé\nRouge : anti-cheat (ban possible)  ·  Orange : autre mod présent  ·  Gris : non compatible"
            };
            split.Panel1.Controls.Add(list); split.Panel1.Controls.Add(legend);

            // onglets maison (les TabControl Windows ne se mettent pas en sombre)
            var right = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 14, 14, 14), BackColor = Theme.Bg };
            split.Panel2.Controls.Add(right);
            var tabs = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, BackColor = Theme.Bg, WrapContents = false };
            tabJeu = Theme.Btn("Jeu", Theme.Green, 120, 34); tabReglages = Theme.Btn("Réglages NR", Theme.Line, 140, 34); tabJournal = Theme.Btn("Journal", Theme.Line, 120, 34);
            tabs.Controls.AddRange(new Control[] { tabJeu, tabReglages, tabJournal });
            var host = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Panel, Padding = new Padding(18) };
            right.Controls.Add(host); right.Controls.Add(tabs);

            pageJeu = BuildPageJeu(); pageReglages = BuildPageReglages(); pageJournal = BuildPageJournal();
            foreach (var p in new[] { pageJeu, pageReglages, pageJournal }) { p.Dock = DockStyle.Fill; host.Controls.Add(p); }
            tabJeu.Click += (s, e) => ShowPage(0); tabReglages.Click += (s, e) => ShowPage(1); tabJournal.Click += (s, e) => ShowPage(2);
            ShowPage(0);
            ShowGame();
        }

        void ShowPage(int i)
        {
            var pages = new[] { pageJeu, pageReglages, pageJournal }; var tb = new[] { tabJeu, tabReglages, tabJournal };
            for (int k = 0; k < 3; k++)
            {
                pages[k].Visible = k == i;
                tb[k].FlatAppearance.BorderColor = k == i ? Theme.Green : Theme.Line;
                tb[k].BackColor = k == i ? Color.FromArgb(40, 52, 30) : Theme.Panel2;
            }
        }

        Panel BuildPageJeu()
        {
            var p = new Panel { BackColor = Theme.Panel, AutoScroll = true };
            var flow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Theme.Panel };
            p.Controls.Add(flow);

            lblGame = new Label { Font = Theme.H2, AutoSize = true, ForeColor = Theme.Text, Margin = new Padding(0, 0, 0, 4) };
            lblStatus = new Label { Font = Theme.UIBold, AutoSize = true, Margin = new Padding(0, 0, 0, 10) };
            details = new TextBox
            {
                Multiline = true, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = Theme.Panel, ForeColor = Theme.Dim,
                Font = Theme.UI, Width = 560, Height = 205, ScrollBars = ScrollBars.None, Margin = new Padding(0, 0, 0, 6), TabStop = false
            };
            lblNote = new Label { AutoSize = true, MaximumSize = new Size(560, 0), ForeColor = Theme.Yellow, Margin = new Padding(0, 0, 0, 12) };

            var opts = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, BackColor = Theme.Panel, Margin = new Padding(0, 0, 0, 12) };
            cbMode = new SafeCombo { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300, FlatStyle = FlatStyle.Flat };
            cbMode.Items.AddRange(new object[] {
                "Auto  (choisit selon le jeu)",
                "OptiScaler F5  — jeux avec DLSS/FSR/XeSS, menu Inser",
                "RenoDX / ReShade  — jeux DLSS natifs, menu Début",
                "Feeder F5Low  — jeux SANS upscaler, DX11/DX12/Vulkan",
                "Feeder ReShade  — alternative, aussi OpenGL" });
            cbMode.SelectedIndexChanged += (s, e) => ShowGame();
            cbMode.SelectedIndex = 0;
            cbProxy = new SafeCombo { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300, FlatStyle = FlatStyle.Flat };
            cbProxy.Items.AddRange(new object[] { "Auto (recommandé)", "dxgi.dll", "winmm.dll", "version.dll", "dbghelp.dll", "d3d12.dll" });
            cbProxy.SelectedIndex = 0;
            Theme.Dark(cbMode); Theme.Dark(cbProxy);
            tips.SetToolTip(cbProxy, "Nom sous lequel OptiScaler est chargé par le jeu.\ndxgi.dll marche presque partout ; winmm.dll pour Vulkan ;\ndbghelp.dll pour cohabiter avec d'autres chargeurs (Cyberpunk).\nIgnoré en mode RenoDX (toujours dxgi.dll).");
            opts.Controls.Add(new Label { Text = "Méthode :", AutoSize = true, ForeColor = Theme.Text, Margin = new Padding(0, 6, 10, 6) }, 0, 0);
            opts.Controls.Add(cbMode, 1, 0);
            opts.Controls.Add(new Label { Text = "DLL proxy :", AutoSize = true, ForeColor = Theme.Text, Margin = new Padding(0, 6, 10, 6) }, 0, 1);
            opts.Controls.Add(cbProxy, 1, 1);
            cbStep = new SafeCombo { DropDownStyle = ComboBoxStyle.DropDown, Width = 300, FlatStyle = FlatStyle.Flat };
            cbStep.Items.AddRange(Steps.Suggestions);
            Theme.Dark(cbStep);
            tips.SetToolTip(cbStep, "Protocole de test A/B : nomme l'étape que tu testes (une variable à la fois).\n" +
                "Chaque partie jouée ensuite est rangée sous cette étape dans l'historique,\npuis « Historique » compare les étapes entre elles.\n" +
                "Choisis une suggestion ou écris la tienne ; vide = pas de protocole.\nUne étape sans DLSS 5 (vanille) : « Noter une partie à la main » après avoir joué.");
            cbStep.SelectionChangeCommitted += (s, e) => { if (cbStep.SelectedItem != null) cbStep.Text = (string)cbStep.SelectedItem; CommitStep(); };
            cbStep.Leave += (s, e) => CommitStep();
            cbStep.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { CommitStep(); e.SuppressKeyPress = true; } };
            cbOpti = new SafeCombo { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300, FlatStyle = FlatStyle.Flat };
            Theme.Dark(cbOpti);
            tips.SetToolTip(cbOpti, "Version d'OptiScaler F5 pour ce jeu (OptiScaler F5 et Feeder F5Low).\n" +
                "Ajoute une version en copiant son zip dans payload\\optiscaler-f5-<version> (ex. optiscaler-f5-0.1.34).\n" +
                "La 0.1.32 reste la version testée. Le choix compte à la prochaine installation :\nsi le jeu est déjà installé, réinitialise puis réinstalle.");
            cbOpti.SelectedIndexChanged += (s, e) => OptiChanged();
            opts.Controls.Add(new Label { Text = "OptiScaler :", AutoSize = true, ForeColor = Theme.Text, Margin = new Padding(0, 6, 10, 6) }, 0, 2);
            opts.Controls.Add(cbOpti, 1, 2);
            opts.Controls.Add(new Label { Text = "Étape de test :", AutoSize = true, ForeColor = Theme.Text, Margin = new Padding(0, 6, 10, 6) }, 0, 3);
            opts.Controls.Add(cbStep, 1, 3);

            var row1 = new FlowLayoutPanel { AutoSize = true, WrapContents = false, BackColor = Theme.Panel, Margin = new Padding(0) };
            bInstall = Theme.Btn("⚡  Installer DLSS 5", Theme.Green, 230, 46);
            bInstall.BackColor = Color.FromArgb(46, 70, 20); bInstall.Font = new Font("Segoe UI Semibold", 11f);
            bInstall.Click += (s, e) => DoInstall();
            bReset = Theme.Btn("↺  Réinitialiser le jeu", Theme.Red, 230, 46);
            bReset.Click += (s, e) => DoReset();
            row1.Controls.AddRange(new Control[] { bInstall, bReset });

            var row2 = new FlowLayoutPanel { AutoSize = true, WrapContents = true, MaximumSize = new Size(600, 0), BackColor = Theme.Panel, Margin = new Padding(0, 4, 0, 0) };
            bLaunch = Theme.Btn("▶  Lancer le jeu", Theme.Line, 150); bLaunch.Click += (s, e) => LaunchGame();
            bFolder = Theme.Btn("Ouvrir le dossier", Theme.Line, 150); bFolder.Click += (s, e) => OpenFolder();
            bLog = Theme.Btn("Ouvrir le log NR", Theme.Line, 150); bLog.Click += (s, e) => OpenLog();
            bReport = Theme.Btn("📝  Fiche de test", Theme.Blue, 150); bReport.Click += (s, e) => DoReport();
            bJournaux = Theme.Btn("Journaux archivés", Theme.Line, 150); bJournaux.Click += (s, e) => { Directory.CreateDirectory(AppPaths.Journaux); Process.Start("explorer.exe", "\"" + AppPaths.Journaux + "\""); };
            bUndo = Theme.Btn("Annuler le nettoyage", Theme.Orange, 150); bUndo.Click += (s, e) => DoUndo();
            tips.SetToolTip(bUndo, "Remet en place les fichiers que le dernier nettoyage a mis en quarantaine\n(dossier « quarantaine » du launcher).");
            bHistory = Theme.Btn("📈  Historique", Theme.Blue, 150); bHistory.Click += (s, e) => ShowHistory(selected);
            tips.SetToolTip(bHistory, "Toutes tes parties de ce jeu : durée, coût NR, preset, DLL, crash.\nCompare les montages entre eux (ex. Preset A contre Preset B).\nFichier complet : historique.csv (s'ouvre dans Excel).");
            bManual = Theme.Btn("✎  Partie à la main", Theme.Line, 150); bManual.Click += (s, e) => DoManualSession(selected);
            tips.SetToolTip(bManual, "Note une partie que le launcher ne peut pas lire lui-même :\nune étape « vanille » (sans DLSS 5), ou RenoDX / Feeder ReShade.\nDurée, crash ou non, et une note.");
            bAdvice = Theme.Btn("🩺  Conseils", Theme.Green, 150); bAdvice.Click += (s, e) => ShowAdvice(selected);
            tips.SetToolTip(bAdvice, "Ce que la dernière partie conseille (profondeur jamais trouvée, crash GPU, NR trop lourd…),\n" +
                "à appliquer en un clic au profil de ce jeu — et au jeu s'il est installé.");
            row2.Controls.AddRange(new Control[] { bLaunch, bFolder, bLog, bReport, bAdvice, bHistory, bManual, bJournaux, bUndo });
            tips.SetToolTip(bReport, "Crée une fiche Markdown (jeu, mode, hashes, réglages, extrait du log)\ndans le dossier fiches-de-test — pratique pour ta documentation.");
            tips.SetToolTip(bReset, "Remet le dossier du jeu exactement comme avant l'installation,\nd'après le manifeste. Les logs sont archivés avant.");

            var help = new Label
            {
                AutoSize = true, MaximumSize = new Size(580, 0), ForeColor = Theme.Dim, Margin = new Padding(0, 14, 0, 0),
                Text = "Jeux avec DLSS/FSR/XeSS : active l'upscaler dans les options et coupe la génération d'images.\n" +
                       "OptiScaler F5 : Inser → Neural Rendering.   RenoDX : Début → Add-ons.\n" +
                       "Jeux sans upscaler (cyan) : Feeder F5Low → Inser → Optical F5Low.   Feeder ReShade → Début.\n" +
                       "Ferme toujours le jeu complètement avant d'installer, de réinitialiser ou de changer les réglages."
            };

            chkAntiCheat = new CheckBox
            {
                Text = "Bloquer l'installation dans les jeux avec anti-cheat", AutoSize = true, ForeColor = Theme.Text, BackColor = Theme.Panel,
                Checked = blockAntiCheat, Margin = new Padding(2, 10, 0, 0), Cursor = Cursors.Hand
            };
            tips.SetToolTip(chkAntiCheat, "Coché : le launcher refuse d'installer quoi que ce soit dans un jeu où il voit un anti-cheat\n(EasyAntiCheat, BattlEye…). Coché d'office dans une copie pour un ami.\nLa détection se fait par fichiers : un anti-cheat côté serveur ou installé ailleurs peut passer inaperçu.");
            chkAntiCheat.CheckedChanged += (s, e) => AntiCheatToggled();
            flow.Controls.AddRange(new Control[] { lblGame, lblStatus, details, lblNote, opts, row1, row2, chkAntiCheat, help });
            return p;
        }

        Panel BuildPageJournal()
        {
            var p = new Panel { BackColor = Theme.Panel };
            journal = new TextBox { Multiline = true, ReadOnly = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical, BackColor = Color.FromArgb(14, 16, 19), ForeColor = Theme.Text, Font = Theme.Mono, BorderStyle = BorderStyle.None, WordWrap = false };
            p.Controls.Add(journal);
            return p;
        }

        // =============================================================== réglages
        Panel BuildPageReglages()
        {
            var p = new Panel { BackColor = Theme.Panel, AutoScroll = true };
            var flow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Theme.Panel };
            p.Controls.Add(flow);

            var top = new FlowLayoutPanel { AutoSize = true, WrapContents = false, BackColor = Theme.Panel, Margin = new Padding(0, 0, 0, 6) };
            top.Controls.Add(new Label { Text = "Préréglage :", AutoSize = true, Margin = new Padding(0, 9, 8, 0) });
            cbPreset = new SafeCombo { DropDownStyle = ComboBoxStyle.DropDownList, Width = 260, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 4, 10, 0) };
            Theme.Dark(cbPreset);
            cbPreset.Items.AddRange(Presets.Names); cbPreset.Items.Add("Personnalisé");
            SelectPresetName(presetName);
            cbPreset.SelectionChangeCommitted += (s, e) =>
            {
                if (cbPreset.SelectedIndex < Presets.Names.Length) { settings = Presets.Get(cbPreset.SelectedIndex); RefreshControls(); }
                presetName = (string)cbPreset.SelectedItem; SaveCurrent();
            };
            top.Controls.Add(cbPreset);
            lblProfile = new Label { AutoSize = true, Font = Theme.UIBold, ForeColor = Theme.Green, Margin = new Padding(0, 0, 0, 6) };
            flow.Controls.Add(lblProfile);
            flow.Controls.Add(top);
            var brow = new FlowLayoutPanel { AutoSize = true, WrapContents = true, MaximumSize = new Size(620, 0), BackColor = Theme.Panel, Margin = new Padding(0, 4, 0, 4) };
            bApply = Theme.Btn("Appliquer au jeu sélectionné", Theme.Green, 230);
            bApply.Click += (s, e) => DoApply();
            bImport = Theme.Btn("Importer depuis le jeu", Theme.Blue, 190);
            bImport.Click += (s, e) => DoImport();
            tips.SetToolTip(bImport, "Tu as ajusté l'effet en direct dans le menu Inser (ou Début) et sauvegardé ?\nCe bouton relit l'INI du jeu et garde ces valeurs dans le profil du jeu.");
            bMakeDefault = Theme.Btn("Utiliser par défaut", Theme.Line, 160);
            bMakeDefault.Click += (s, e) => { defaults = settings.Clone(); defaultPresetName = presetName; SaveConfig(); Info("Ces réglages seront ceux de départ pour les jeux qui n'ont pas encore de profil."); };
            tips.SetToolTip(bMakeDefault, "Les jeux sans profil à eux partiront de ces réglages.");
            bResetProfile = Theme.Btn("Profil → défauts", Theme.Line, 150);
            bResetProfile.Click += (s, e) => DoResetProfile();
            tips.SetToolTip(bResetProfile, "Oublie le profil de ce jeu et revient aux réglages par défaut.");
            brow.Controls.AddRange(new Control[] { bApply, bImport, bMakeDefault, bResetProfile });
            flow.Controls.Add(brow);
            flow.Controls.Add(new Label { AutoSize = true, MaximumSize = new Size(600, 0), ForeColor = Theme.Dim, Margin = new Padding(0, 0, 0, 10),
                Text = "Chaque jeu a son propre profil de réglages, gardé automatiquement. Ils servent à la prochaine installation ; pour un jeu déjà installé, clique « Appliquer au jeu sélectionné » (jeu fermé). Passe la souris sur un réglage pour son explication." });

            var stab = Section(flow, "Stabilité");
            Check(stab, "Profil sûr pour la Ray Reconstruction", () => settings.RrSafe, v => settings.RrSafe = v,
                "Dans les jeux avec Ray Reconstruction (Cyberpunk, Crimson Desert…), NR tourne après la RR à la résolution de sortie.\n" +
                "Tes 2 crashs du 24 oct. (et ceux du 19) sont arrivés là, en Preset A pleine résolution.\nCoché : le launcher force Preset B, modèle ≤ 0,6 et 1 passe dans ces jeux seulement.");
            Check(stab, "Créer un dump en cas de crash", () => settings.CrashDump, v => settings.CrashDump = v,
                "OptiScaler écrit OptiScaler_crash.dmp au moment d'un crash (archivé au reset) — utile pour un rapport aux développeurs.");
            var tdrBtn = Theme.Btn("", Theme.Line, 260, 30);
            Action tdrRefresh = () => { int d = Stability.TdrDelay(); tdrBtn.Text = d >= 8 ? "Tolérance GPU : " + d + " s (remettre 2 s)" : "Tolérance GPU : 2 s (passer à 8 s)"; };
            tdrRefresh();
            tdrBtn.Click += (s, e) => DoTdr(tdrRefresh);
            Row(stab, "Délai avant « GPU bloqué » (Windows)", tdrBtn,
                "Windows coupe le pilote si une image prend plus de 2 s (DXGI_ERROR_DEVICE_HUNG). La création du modèle NR pendant une scène lourde peut dépasser ça.\n" +
                "8 s laisse plus de marge. Réglage Windows (TdrDelay) : demande les droits admin et un redémarrage, réversible avec le même bouton.\nÇa ne corrige pas un vrai bug, ça évite seulement les coupures sur des pics.");

            var perfS = Section(flow, "Fluidité et FPS");
            Combo(perfS, "Génération d'images", new[] { "Désactivée (le plus stable)", "XeFG ×2 — toutes les API", "FSR FG ×2 — DX12 seulement" },
                () => settings.FrameGen, v => settings.FrameGen = v,
                "Double les images affichées : beaucoup plus fluide. Utilise les DLL XeFG / FSR FG déjà dans le dossier OptiScaler.\n" +
                "Ajoute un peu de latence (Reflex est activé tout seul) et peut faire « trembler » le HUD.\n" +
                "Garde le jeu au-dessus de ~50 FPS avant génération pour un bon résultat. Coupe la génération d'images du jeu lui-même.\nExpérimental : pas encore testé sur ta config.");
            Num(perfS, "Limite de FPS (0 = aucune)", 0, 500, 1, 0, () => settings.FpsLimit, v => settings.FpsLimit = v,
                "Plafond d'images par seconde (via Reflex). Un plafond un peu sous ton minimum habituel donne une image\nbeaucoup plus régulière (moins de saccades). Avec génération d'images : c'est le total affiché.");
            Check(perfS, "Reflex Boost", () => settings.ReflexBoost, v => settings.ReflexBoost = v,
                "Le GPU garde ses fréquences hautes entre les images : un peu moins de latence, un peu plus de consommation.");
            Check(perfS, "Compresser les bords de l'image", () => settings.SpatialCompression, v => settings.SpatialCompression = v,
                "Le centre reste net à 100 %, les bords sont travaillés en plus petit : moins de coût GPU, bords un peu plus doux.\nNe se combine pas avec « Réutilisation du détail » (celle-ci gagne).");

            var opti = Section(flow, "OptiScaler F5  —  section [DlssNr] de OptiScaler.ini");
            Combo(opti, "Modèle (Preset)", new[] { "Auto", "0 · la DLL choisit", "1 · Preset A — gros modèle", "2 · Preset B — petit modèle (validé)" },
                () => settings.Preset + 1, v => settings.Preset = v - 1,
                "Preset B = petit modèle, environ deux fois moins cher, un peu plus doux. C'est lui qui a marché sur ta 4070 (test 03).\nPreset A = gros modèle : pas encore testé avec la DLL OpenNR.");
            Combo(opti, "Style", new[] { "Auto", "0 · standard", "1 · naturel", "2 · cinématique" }, () => settings.Style + 1, v => settings.Style = v - 1,
                "Le « look » appliqué par le modèle. Changer le style reconstruit l'effet (petite pause possible).");
            Num(opti, "Passes", 1, 3, 1, 0, () => settings.Passes, v => settings.Passes = (int)v,
                "Nombre de fois que le modèle repasse sur l'image. 2 = environ 2× le coût GPU, 3 = 3×. Commence à 1.");
            Num(opti, "Résolution du modèle (WorkingScale)", 0.25m, 1.5m, 0.05m, 2, () => settings.WorkingScale, v => settings.WorkingScale = v,
                "Fraction de l'image sur laquelle le modèle travaille. Le coût baisse avec le carré : 0,75 ≈ 56 % du coût.\nAu-dessus de 1,0 : sur-échantillonnage (DX12/Vulkan), plus cher.");
            Check(opti, "NR avant l'upscaling (RunBeforeSR)", () => settings.RunBeforeSR, v => settings.RunBeforeSR = v,
                "Oui : le modèle reçoit l'image en résolution interne (moins cher), puis DLSS l'agrandit. C'est la config validée.");
            Num(opti, "Intensité", 0, 2, 0.05m, 2, () => settings.Intensity, v => settings.Intensity = v, "Force globale de l'effet. 1,0 = défaut du modèle.");
            Num(opti, "Structure locale (détail)", 0, 2, 0.05m, 2, () => settings.LocalStructure, v => settings.LocalStructure = v, "Accentuation des détails et du micro-contraste.");
            Num(opti, "Ton local (éclairage)", 0, 2, 0.05m, 2, () => settings.LocalTone, v => settings.LocalTone = v, "Force des changements d'éclairage local.");
            Num(opti, "Structure de la peau", -1, 2, 0.05m, 2, () => settings.SkinStructure, v => settings.SkinStructure = v, "-1 = suit la structure locale (défaut du modèle).");
            Check(opti, "Masque automatique (AutoMask)", () => settings.AutoMask, v => settings.AutoMask = v, "Protège certaines zones (HUD, etc.) automatiquement.");
            Num(opti, "Force du transfert", 0, 1.5m, 0.05m, 2, () => settings.TransferStrength, v => settings.TransferStrength = v,
                "0 = image du jeu intacte, 1 = image du modèle, au-dessus de 1 = va encore plus loin.");
            Num(opti, "Force de la couleur", 0, 1, 0.05m, 2, () => settings.ColourStrength, v => settings.ColourStrength = v,
                "0 = garde exactement les teintes du jeu (seule la luminosité change), 1 = couleurs du modèle.");
            Num(opti, "Éclaircissement max (MaxRatio)", 1, 4, 0.1m, 2, () => settings.MaxRatio, v => settings.MaxRatio = v,
                "Limite de combien un pixel peut être éclairci. Baisse-le si des lumières vives font des « cellules » colorées.");
            Combo(opti, "Calcul du réseau (VitEvery)", new[] { "chaque image (validé)", "une image sur deux (+ rapide)" }, () => settings.VitEvery - 1, v => settings.VitEvery = v + 1,
                "Une image sur deux = moins de coût GPU, un peu moins stable en mouvement.");
            Check(opti, "Réutilisation du détail (DetailReuse)", () => settings.DetailReuse, v => settings.DetailReuse = v,
                "Réutilise une partie du travail d'une image à l'autre pour gagner des FPS. Peut laisser des traînées.");
            Check(opti, "Captures avant/après automatiques", () => settings.AutoCapture, v => settings.AutoCapture = v, "Écrit 8 images avant/après dans dlssnr-capture (archivées au reset).");
            Check(opti, "Écrire OptiScaler.log", () => settings.LogToFile, v => settings.LogToFile = v, "Nécessaire pour la fiche de test et le diagnostic.");

            var feed = Section(flow, "Feeder F5Low  —  jeux sans upscaler (le modèle ci-dessus s'applique aussi)");
            Num(feed, "Résolution du modèle (feeder)", 0.25m, 1.0m, 0.05m, 2, () => settings.FWorkingScale, v => settings.FWorkingScale = v,
                "Ici le modèle travaille sur l'image finale en pleine résolution, donc c'est plus cher qu'avec DLSS.\n0,67 ≈ le même nombre de pixels que dans ton test 03 (≈ 3,5 ms sur ta 4070). 0,5 = encore plus léger.");
            Check(feed, "Utiliser la profondeur du jeu", () => settings.FUseDepth, v => settings.FUseDepth = v,
                "OptiScaler cherche le tampon de profondeur du jeu pour mieux juger le mouvement.\nDécoche si tu vois des blocs noirs ou des artefacts bizarres : il marche aussi sans (mouvement seulement).");
            Combo(feed, "Chemin", new[] { "Auto (NR seul ; upscaler virtuel en Vulkan)", "NR seul — le plus léger", "Via upscaler virtuel — Vulkan / secours" },
                () => settings.FPath, v => settings.FPath = v,
                "NR seul = mode « NR only » du menu Optical F5Low.\nVia upscaler virtuel = mode « NR + frame generation » sans génération d'images : obligatoire en Vulkan,\net bonne solution de secours si « NR seul » ne démarre pas.");
            Check(feed, "Lisser le scintillement (+1,5 à 3 ms)", () => settings.FSmooth, v => settings.FSmooth = v,
                "Fait passer l'image dans l'upscaler à la même taille : n'ajoute pas de détail,\nmais stabilise les jeux avec un anti-aliasing faible. Force le chemin « upscaler virtuel ».");
            Combo(feed, "Profil de mouvement", new[] { "défaut", "anime / dessin", "réaliste", "sport (plan large)", "sport (panoramique)" },
                () => settings.FMotionProfile, v => settings.FMotionProfile = v,
                "Ajuste l'estimation du mouvement au style du jeu. « défaut » est le plus testé.");

            var col = Section(flow, "Couleur et peau  (OptiScaler F5 et Feeder F5Low)");
            var luts = new List<string> { "Aucun" };
            try { luts.AddRange(Directory.GetFiles(Path.Combine(AppPaths.OptiDir, "OptiScaler", "LUTs"), "*.cube").Select(Path.GetFileName).OrderBy(x => x)); } catch { }
            var lutArr = luts.ToArray();
            Combo(col, "Étalonnage (LUT)", lutArr.Select(x => x.Replace(".cube", "")).ToArray(),
                () => Math.Max(0, Array.IndexOf(lutArr, string.IsNullOrEmpty(settings.Lut) ? "Aucun" : settings.Lut)),
                v => settings.Lut = v <= 0 ? "" : lutArr[v],
                "Un « look » de couleur appliqué à l'image AVANT que le modèle la voie (fichiers .cube du dossier OptiScaler\\LUTs).\nAgX = rendu filmique doux ; JP2499DRT = looks pellicule. « Aucun » ne coûte rien.");
            Num(col, "Force de l'étalonnage", 0, 1, 0.05m, 2, () => settings.LutStrength, v => settings.LutStrength = v, "0 = sans effet, 1 = LUT complet.");
            Check(col, "Protéger peau / décor séparément", () => settings.SkinProtection, v => settings.SkinProtection = v,
                "Active des réglages séparés pour la peau et le décor (sélection par couleur, pas un vrai détecteur de visages :\nle bois ou le sable peuvent être pris pour de la peau).");
            Check(col, "Permettre les changements de teinte de peau", () => settings.SkinToneEnabled, v => settings.SkinToneEnabled = v, "Décoche pour garder la couleur de peau du jeu.");
            Num(col, "Peau : détail / lumière", 0, 1, 0.05m, 2, () => settings.SkinDetail, v => settings.SkinDetail = v, "1 = effet complet sur la peau, 0 = peau d'origine.");
            Num(col, "Peau : couleur", 0, 1, 0.05m, 2, () => settings.SkinColour, v => settings.SkinColour = v, "1 = couleur du modèle, 0 = couleur du jeu.");
            Num(col, "Décor : détail / lumière", 0, 1, 0.05m, 2, () => settings.EnvDetail, v => settings.EnvDetail = v, "Force de l'effet sur tout ce qui n'est pas de la peau.");
            Num(col, "Décor : couleur", 0, 1, 0.05m, 2, () => settings.EnvColour, v => settings.EnvColour = v, "Force des changements de couleur sur le décor.");

            var reno = Section(flow, "RenoDX / ReShade  —  [RenoDX.DLSS5] (aussi utilisé par le Feeder ReShade)");
            Combo(reno, "Modèle (NRPreset)", new[] { "0 · la DLL choisit", "1 · Preset A", "2 · Preset B (validé)" }, () => settings.RPreset, v => settings.RPreset = v, "Même idée que dans OptiScaler. Le test 04 utilisait 2.");
            Combo(reno, "Style (NRStyle)", new[] { "0", "1 (défaut)", "2" }, () => settings.RStyle, v => settings.RStyle = v, "Style du modèle côté RenoDX. 1 = valeur du payload 1-Click.");
            Check(reno, "Neural Uplift", () => settings.RNeuralUplift, v => settings.RNeuralUplift = v, "Active l'effet DLSS 5 de RenoDX.");
            Num(reno, "Ton global", 0, 2, 0.05m, 2, () => settings.RGlobalTone, v => settings.RGlobalTone = v, "Défaut 0,9.");
            Num(reno, "Structure locale", 0, 2, 0.02m, 2, () => settings.RLocalStructure, v => settings.RLocalStructure = v, "Défaut 0,44.");
            Num(reno, "Ton local", 0, 3, 0.02m, 2, () => settings.RLocalTone, v => settings.RLocalTone = v, "Défaut 1,22.");
            Num(reno, "Structure de la peau", 0, 3, 0.02m, 2, () => settings.RSkinStructure, v => settings.RSkinStructure = v, "Défaut 1,16.");
            Check(reno, "Masque automatique", () => settings.RAutoMask, v => settings.RAutoMask = v, "Protège certaines zones automatiquement.");
            Check(reno, "Correction de l'interface (UI)", () => settings.RUICorrection, v => settings.RUICorrection = v, "Évite que le HUD soit modifié par l'effet.");
            UpdateProfileLabel();
            Num(reno, "Blanc de référence (nits)", 80, 1000, 1, 0, () => settings.RDiffuseWhiteNits, v => settings.RDiffuseWhiteNits = (int)v, "Pour le HDR. 203 = référence standard.");
            return p;
        }

        TableLayoutPanel Section(FlowLayoutPanel flow, string title)
        {
            flow.Controls.Add(new Label { Text = title, Font = Theme.UIBold, ForeColor = Theme.Green, AutoSize = true, Margin = new Padding(0, 14, 0, 6) });
            var t = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, BackColor = Theme.Panel, Margin = new Padding(0, 0, 0, 4) };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 290)); t.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            flow.Controls.Add(t);
            return t;
        }
        void Row(TableLayoutPanel t, string label, Control c, string tip)
        {
            var l = new Label { Text = label, AutoSize = true, ForeColor = Theme.Text, Margin = new Padding(0, 6, 8, 4) };
            int r = t.RowCount++;
            t.Controls.Add(l, 0, r); t.Controls.Add(c, 1, r);
            tips.SetToolTip(l, tip); tips.SetToolTip(c, tip);
        }
        void Changed() { presetName = "Personnalisé"; SelectPresetName(presetName); SaveCurrent(); }

        void SelectPresetName(string name)
        {
            if (cbPreset == null) return;
            int i = cbPreset.Items.IndexOf(name);
            refreshing = true; cbPreset.SelectedIndex = i >= 0 ? i : cbPreset.Items.Count - 1; refreshing = false;
        }
        void RefreshControls() { foreach (var r in refreshers) r(); }

        // ---- profils par jeu
        void LoadProfile(Game g)
        {
            settings = defaults.Clone(); presetName = defaultPresetName;
            if (g != null && File.Exists(ProfilePath(g)))
                try
                {
                    var kv = ReadKv(ProfilePath(g));
                    settings.LoadFrom(kv);
                    string pn; if (kv.TryGetValue("presetName", out pn)) presetName = pn;
                }
                catch { }
            RefreshControls(); SelectPresetName(presetName); UpdateProfileLabel();
        }
        void SaveCurrent()
        {
            if (selected == null) { defaults = settings.Clone(); defaultPresetName = presetName; SaveConfig(); UpdateProfileLabel(); return; }
            try
            {
                Directory.CreateDirectory(ProfilesDir);
                var lines = new List<string> { "# Profil DLSS 5 Launcher — " + selected.Name, "presetName=" + presetName };
                settings.SaveTo(lines);
                File.WriteAllLines(ProfilePath(selected), lines);
            }
            catch { }
            UpdateProfileLabel();
        }
        void UpdateProfileLabel()
        {
            if (lblProfile == null) return;
            if (selected == null) lblProfile.Text = "Réglages par défaut (aucun jeu sélectionné)";
            else lblProfile.Text = "Profil de : " + selected.Name + (File.Exists(ProfilePath(selected)) ? "" : "   (réglages par défaut pour l'instant)");
        }
        static Dictionary<string, string> ReadKv(string path)
        {
            var kv = new Dictionary<string, string>();
            foreach (var l in File.ReadAllLines(path))
            {
                int eq = l.IndexOf('='); if (eq <= 0 || l.StartsWith("#")) continue;
                kv[l.Substring(0, eq)] = l.Substring(eq + 1);
            }
            return kv;
        }
        void DoResetProfile()
        {
            if (selected == null) return;
            try { if (File.Exists(ProfilePath(selected))) File.Delete(ProfilePath(selected)); } catch { }
            LoadProfile(selected);
        }
        void DoImport()
        {
            var g = selected; if (g == null || g.Installed == null) return;
            try
            {
                string oi = Path.Combine(g.ExeDir, "OptiScaler.ini"), ri = Path.Combine(g.ExeDir, "ReShade.ini");
                if (g.Installed.UsesOptiIni && File.Exists(oi)) settings.ImportOpti(Ini.ReadFile(oi), g.Installed.Mode == "FEEDER");
                else if (File.Exists(ri)) settings.ImportReShade(Ini.ReadFile(ri));
                else { Info("Aucun INI à importer dans ce jeu."); return; }
                presetName = "Personnalisé"; RefreshControls(); SelectPresetName(presetName); SaveCurrent();
                Log.W("Réglages importés depuis " + g.Name + ".");
                Info("Réglages du jeu importés dans le profil de " + g.Name + ".\nIls seront réappliqués tels quels à la prochaine installation.");
            }
            catch (Exception ex) { Error("Import impossible : " + ex.Message); }
        }
        bool refreshing;
        void Combo(TableLayoutPanel t, string label, string[] items, Func<int> get, Action<int> set, string tip)
        {
            var c = new SafeCombo { DropDownStyle = ComboBoxStyle.DropDownList, Width = 260, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 2, 0, 2) };
            Theme.Dark(c); c.Items.AddRange(items);
            Action rf = () => { refreshing = true; c.SelectedIndex = Math.Max(0, Math.Min(items.Length - 1, get())); refreshing = false; };
            rf(); refreshers.Add(rf);
            c.SelectedIndexChanged += (s, e) => { if (refreshing) return; set(c.SelectedIndex); Changed(); };
            Row(t, label, c, tip);
        }
        void Num(TableLayoutPanel t, string label, decimal min, decimal max, decimal step, int dec, Func<decimal> get, Action<decimal> set, string tip)
        {
            var n = new SafeNum { Minimum = min, Maximum = max, Increment = step, DecimalPlaces = dec, Width = 110, Margin = new Padding(0, 2, 0, 2), BorderStyle = BorderStyle.FixedSingle };
            Theme.Dark(n);
            Action rf = () => { refreshing = true; n.Value = Math.Max(min, Math.Min(max, get())); refreshing = false; };
            rf(); refreshers.Add(rf);
            n.ValueChanged += (s, e) => { if (refreshing) return; set(n.Value); Changed(); };
            Row(t, label, n, tip);
        }
        void Check(TableLayoutPanel t, string label, Func<bool> get, Action<bool> set, string tip)
        {
            var c = new CheckBox { AutoSize = true, Margin = new Padding(0, 6, 0, 2), ForeColor = Theme.Text, FlatStyle = FlatStyle.Flat };
            Action rf = () => { refreshing = true; c.Checked = get(); refreshing = false; };
            rf(); refreshers.Add(rf);
            c.CheckedChanged += (s, e) => { if (refreshing) return; set(c.Checked); Changed(); };
            Row(t, label, c, tip);
        }

        // =============================================================== scan
        void StartScan()
        {
            if (busy) return;
            SetBusy(true, "Scan des bibliothèques…");
            string keep = selected != null ? selected.ExePath ?? selected.Root : null;
            var t = new Thread(() =>
            {
                var found = new List<Game>();
                try
                {
                    found.AddRange(Scanner.Steam());
                    found.AddRange(Scanner.Epic());
                    found.AddRange(Scanner.Gog());
                    found.AddRange(Scanner.Xbox());
                    foreach (var f in customFolders) if (Directory.Exists(f)) found.Add(Scanner.Manual(f));
                    found = found.GroupBy(x => x.Root.TrimEnd(new[] { '\\', '/' }).ToLowerInvariant()).Select(x => x.First()).ToList();
                    for (int i = 0; i < found.Count; i++)
                    {
                        int k = i; var g = found[i];
                        BeginInvoke((Action)(() => { progress.Maximum = found.Count; progress.Value = k; lblFooter.Text = "Analyse " + (k + 1) + "/" + found.Count + " : " + g.Name; }));
                        try { Scanner.Analyze(g); } catch (Exception ex) { Log.W("Analyse impossible pour " + g.Name + " : " + ex.GetType().Name + " — " + ex.Message); g.Status = GameStatus.NonAnalyse; }
                    }
                }
                catch (Exception ex) { Log.W("Erreur de scan : " + ex.GetType().Name + " — " + ex.Message + " | " + (ex.StackTrace ?? "").Replace("\r\n", " / ")); }
                BeginInvoke((Action)(() =>
                {
                    games = found;
                    FillList();
                    int c = games.Count(x => x.Status == GameStatus.Compatible), a = games.Count(x => x.Status == GameStatus.ATester), ins = games.Count(x => x.Status == GameStatus.Installe);
                    Log.W("Scan terminé : " + games.Count + " jeux — " + c + " compatibles, " + a + " à tester, " + ins + " installés.");
                    SetBusy(false, games.Count + " jeux  ·  " + c + " compatibles  ·  " + a + " à tester  ·  " + ins + " installés");
                    if (keep != null) Select(games.FirstOrDefault(x => x.ExePath == keep || x.Root == keep));
                    var snapshot = games.ToList();
                    new Thread(() =>
                    {   // historique : reprend les journaux pas encore notés et la dernière partie des jeux installés
                        try { int n = History.Backfill(snapshot); if (n > 0) Log.W("Historique : " + n + " partie(s) notée(s) ou mise(s) à jour."); BeginInvoke((Action)ShowGame); }
                        catch (Exception ex) { Log.W("Historique : " + ex.Message); }
                    }) { IsBackground = true }.Start();
                }));
            }) { IsBackground = true };
            t.Start();
        }

        void FillList()
        {
            string q = search.ForeColor == Theme.Dim ? "" : search.Text.Trim();
            list.BeginUpdate();
            list.Items.Clear();
            foreach (var g in games.Where(x => q == "" || x.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                                   .OrderBy(x => Theme.Rank(x.Status)).ThenBy(x => x.Name))
            {
                var it = new ListViewItem(new[] { g.Name, g.Store, g.StatusText, g.ApiText, g.UpscalerText }) { Tag = g, ForeColor = Theme.ForStatus(g.Status), BackColor = Theme.Panel };
                list.Items.Add(it);
                if (g == selected) it.Selected = true;
            }
            list.EndUpdate();
        }

        void Select(Game g)
        {
            if (g == null) return;
            foreach (ListViewItem it in list.Items) if (it.Tag == g) { it.Selected = true; it.EnsureVisible(); }
        }

        void ShowGame()
        {
            if (bApply == null || bInstall == null || details == null) return; // pas encore construit
            var g = selected;
            bool has = g != null;
            foreach (var b in new[] { bInstall, bReset, bLaunch, bFolder, bLog, bReport, bImport, bResetProfile, bUndo }) if (b != null) b.Enabled = has && !busy;
            if (bHistory != null) bHistory.Enabled = true;
            if (cbStep != null) cbStep.Enabled = has;
            if (bManual != null) bManual.Enabled = has && !busy;
            if (bAdvice != null) bAdvice.Enabled = has && !busy;
            if (cbOpti != null) cbOpti.Enabled = has && optiFolders.Count > 1;
            if (bLaunch != null && has) bLaunch.Text = watchGame == g ? "◉  Partie surveillée" : g.Store == "Xbox" ? "▶  Surveiller la partie" : "▶  Lancer le jeu";
            if (!has)
            {
                lblGame.Text = "Choisis un jeu dans la liste"; lblStatus.Text = ""; lblNote.Text = "";
                details.Text = "Le scan lit tes bibliothèques Steam (et Epic/GOG si présents).\r\nUn jeu manque ? « Ajouter un dossier » en haut.";
                return;
            }
            lblGame.Text = g.Name;
            lblStatus.Text = "● " + g.StatusText; lblStatus.ForeColor = Theme.ForStatus(g.Status);
            var lines = new List<string>
            {
                "Magasin : " + g.Store + (g.AppId != null ? "  (AppID " + g.AppId + ")" : ""),
                "Exe : " + (g.ExePath ?? "introuvable"),
                "Architecture : " + (g.ExePath == null ? "?" : g.Is64 ? "64-bit" : "32-bit") + "    API : " + g.ApiText + (g.Apis.Count > 0 ? (g.ApiSource != null ? " (confirmé par le log du jeu)" : " (déduit)") : "") + (g.Engine != null ? "    Moteur : " + g.Engine : ""),
                "Upscalers trouvés : " + g.UpscalerText,
            };
            if (g.ExePath != null && g.Installed == null) lines.Add("Méthode choisie en Auto : " + Installer.ModeName(Installer.Resolve(g, InstallMode.Auto)) + (cbMode.SelectedIndex > 0 ? "   (forcée : " + Installer.ModeName(SelectedMode()) + ")" : ""));
            if (g.Dxvk) lines.Add("dxvk détecté (DX11 → Vulkan) : OptiScaler ne sera pas nommé dxgi.dll.");
            if (Stability.HasRr(g) && !Gpu.IsAmdMode) lines.Add("Ray Reconstruction présente : NR tourne alors après la RR (plus lourd ; cause de tes crashs). " + (settings.RrSafe ? "Profil sûr RR actif." : "Profil sûr RR DÉSACTIVÉ.") + " Le plus stable : couper la RR dans le jeu.");
            if (g.AntiCheat.Count > 0) lines.Add("Anti-cheat : " + string.Join(", ", g.AntiCheat));
            if (g.ForeignMods.Count > 0) lines.Add("Mods existants (seront mis de côté puis restaurés) : " + string.Join(", ", g.ForeignMods));
            if (g.TakenProxyNames.Count > 0) lines.Add("DLL d'autres mods à ne pas toucher : " + string.Join(", ", g.TakenProxyNames));
            if (g.OneClickInstalled) lines.Add("Installation 1-Click DLSS5 détectée : « Réinitialiser » la défait d'après son manifeste.");
            if (g.CleanPlan.Count > 0) lines.Add("Traces d'autres mods/launchers : " + g.CleanPlan.Count + " élément(s) — « Réinitialiser » peut les nettoyer.");
            if (g.Installed != null) lines.Add("Installé : " + g.Installed.ModeLabel + " via " + g.Installed.Proxy + " le " + g.Installed.Date + (g.Installed.Complete ? "" : "  (INCOMPLET — réinitialise)") +
                                               (g.Installed.UsesOptiIni ? "  —  OptiScaler " + OptiVersions.Version(string.IsNullOrEmpty(g.Installed.Opti) ? OptiVersions.Default : g.Installed.Opti) : ""));
            string chosenOpti = optiFolders.Count > 1 ? OptiVersions.For(g.Name) : OptiVersions.Default;
            if (chosenOpti != OptiVersions.Default && (g.Installed == null || g.Installed.Opti != chosenOpti))
                lines.Add("OptiScaler choisi pour ce jeu : " + OptiVersions.Label(chosenOpti) + (g.Installed != null ? "  → réinitialise puis réinstalle pour l'utiliser" : ""));
            if (watchGame == g) lines.Add("Partie surveillée : elle sera notée (et la fiche faite en cas de crash) quand le jeu se fermera.");
            details.Text = string.Join("\r\n", lines);
            try
            {
                string arch = g.Installed == null ? Report.LatestArchive(g) : null;
                var pf = g.Installed != null ? Perf.Read(g.ExeDir) : arch != null ? Perf.Read(arch) : null;
                if (pf != null && pf.Samples > 0) { lines.Add((arch != null ? "Dernière session (journal archivé) : " : "Dernière session : ") + pf.Summary()); lines.Add("→ " + pf.Advice()); details.Text = string.Join("\r\n", lines); }
                string step = Steps.Current(g.Name);
                if (step != "") { lines.Add("Étape de test : « " + step + " » — les prochaines parties y seront rangées."); details.Text = string.Join("\r\n", lines); }
                var hist = History.For(g);
                if (hist.Count > 0)
                {
                    int cr = hist.Count(x => x.Crashed);
                    lines.Add("Historique : " + hist.Count + " partie" + (hist.Count > 1 ? "s" : "") + ", " + History.Hours(hist.Sum(x => x.Minutes)) + (cr > 0 ? ", " + cr + " crash" + (cr > 1 ? "s" : "") : ", aucun crash détecté") + "  (bouton « Historique »)");
                    details.Text = string.Join("\r\n", lines);
                }
            }
            catch { }
            lblNote.Text = g.Note ?? "";
            bInstall.Enabled = !busy && g.Installed == null && g.ExePath != null;
            bReset.Enabled = !busy && (g.Installed != null || g.CleanPlan.Count > 0 || g.OneClickInstalled);
            bUndo.Enabled = !busy && Cleaner.LastQuarantine(g) != null;
            bApply.Enabled = !busy && g.Installed != null && g.Installed.Complete;
            bImport.Enabled = !busy && g.Installed != null && g.Installed.Complete && g.Installed.Mode != "RENODX_NONE";
            bResetProfile.Enabled = File.Exists(ProfilePath(g));
        }

        // =============================================================== actions
        void DoInstall()
        {
            var g = selected; if (g == null || busy) return;
            var mode = SelectedMode();
            var eff = Installer.Resolve(g, mode);
            bool gameUps = Installer.HasGameUpscaler(g);
            if ((eff == InstallMode.Feeder || eff == InstallMode.FeederReShade) && gameUps &&
                Ask("Ce jeu a son propre upscaler (" + g.UpscalerText + ").\nLe feeder se met en retrait dès que le jeu l'utilise : OptiScaler F5 est le bon choix ici.\n\nInstaller le feeder quand même ?", MessageBoxIcon.Question) != DialogResult.Yes) return;
            if ((eff == InstallMode.OptiScaler || eff == InstallMode.RenoDX) && !gameUps &&
                Ask("Aucun DLSS/FSR/XeSS trouvé : " + Installer.ModeName(eff) + " n'aura rien à intercepter.\nLe Feeder F5Low est fait pour ce genre de jeu.\n\nInstaller " + Installer.ModeName(eff) + " quand même ?", MessageBoxIcon.Question) != DialogResult.Yes) return;
            string proxy = cbProxy.SelectedIndex <= 0 ? null : (string)cbProxy.SelectedItem;

            if (g.AntiCheat.Count > 0 && blockAntiCheat)
            {
                Error("Installation bloquée : ce jeu contient un anti-cheat (" + string.Join(", ", g.AntiCheat) + ").\n\n" +
                      "Injecter une DLL dans un jeu en ligne peut mener à un BANNISSEMENT de ton compte.\n\n" +
                      "Si c'est un jeu solo joué hors ligne et que tu acceptes ce risque, décoche « Bloquer l'installation dans les jeux avec anti-cheat » (sous les boutons).");
                return;
            }
            if (!PayloadOkFor(eff, g)) return;
            if (g.AntiCheat.Count > 0 &&
                Ask("Ce jeu contient un anti-cheat (" + string.Join(", ", g.AntiCheat) + ").\n\nInjecter une DLL peut mener à un BANNISSEMENT dans un jeu en ligne.\nContinue seulement pour un jeu solo joué hors ligne (ou avec l'anti-cheat désactivé).\n\nInstaller quand même ?", MessageBoxIcon.Warning) != DialogResult.Yes) return;
            if (g.Status == GameStatus.PeuProbable &&
                Ask("Ce jeu semble être DX9 ou d'une API non gérée : ça ne marchera probablement pas.\n\nInstaller quand même pour tester ?", MessageBoxIcon.Question) != DialogResult.Yes) return;
            if (mode == InstallMode.RenoDX && !g.Upscalers.Contains("DLSS") &&
                Ask("Le mode RenoDX a besoin du DLSS natif du jeu, et aucun nvngx_dlss.dll n'a été trouvé.\n\nContinuer quand même ?", MessageBoxIcon.Question) != DialogResult.Yes) return;
            if (g.ForeignMods.Count > 0 &&
                Ask("Des fichiers d'un autre mod DLSS/ReShade sont présents :\n\n  " + string.Join("\n  ", g.ForeignMods) +
                    "\n\nIls seront MIS DE CÔTÉ dans _DLSS5_Launcher\\backup (pas supprimés) et remis en place au moment de la réinitialisation.\n\nContinuer ?", MessageBoxIcon.Question) != DialogResult.Yes) return;

            var st = settings.Clone();
            RunJob("Installation dans " + g.Name + "…", () => new Installer(g).Install(mode, st, proxy), g,
                () =>
                {
                    var m = g.Installed;
                    Info("DLSS 5 installé dans " + g.Name + " (" + (m != null ? m.ModeLabel + ", " + m.Proxy : "") + ").\n\n" + HowTo(m) +
                         "\nSi quelque chose cloche : ferme le jeu et clique « Réinitialiser le jeu ».");
                });
        }

        InstallMode SelectedMode()
        {
            switch (cbMode.SelectedIndex)
            {
                case 1: return InstallMode.OptiScaler;
                case 2: return InstallMode.RenoDX;
                case 3: return InstallMode.Feeder;
                case 4: return InstallMode.FeederReShade;
                default: return InstallMode.Auto;
            }
        }

        static string HowTo(Manifest m)
        {
            string mode = m == null ? "" : m.Mode;
            if (mode == "FEEDER")
                return "1. Lance le jeu. Garde l'anti-aliasing du jeu tel quel, pas besoin de DLSS.\n" +
                       "2. Entre en partie (pas juste le menu : la profondeur se trouve en jeu).\n" +
                       "3. Touche Inser → page Optical F5Low : le statut doit dire que le mouvement est estimé, et Neural Rendering doit être « Running ».\n" +
                       "4. Touche \\ (barre oblique inverse) : active/désactive NR pour comparer.\n";
            if (mode == "FEEDER_RESHADE")
                return "1. Lance le jeu, sans upscaler (résolution native).\n" +
                       "2. Touche Début → onglet Add-ons → DLSS 5 Feed et DLSS 5 Neural Rendering.\n" +
                       "3. Touche Fin : active/désactive les effets pour comparer.\n" +
                       "Le premier lancement compile les shaders : ça peut prendre une minute.\n";
            if (mode == "RENODX")
                return "1. Lance le jeu et active DLSS dans les options.\n2. Touche Début → Add-ons → DLSS 5 Neural Rendering.\n";
            return "1. Lance le jeu et active DLSS (ou FSR/XeSS) dans les options.\n2. Touche Inser → page Neural Rendering.\n";
        }

        void DoReset()
        {
            var g = selected; if (g == null || busy) return;
            if (g.Installed == null || g.OneClickInstalled) { DoClean(g); return; }
            if (Ask("Réinitialiser " + g.Name + " ?\n\nLe launcher enlève ce qu'il a ajouté, remet les fichiers d'origine (vérifiés par hash) et archive les logs dans « journaux ».", MessageBoxIcon.Question) != DialogResult.Yes) return;
            List<string> errs = null; string autoFiche = null;
            RunJob("Réinitialisation de " + g.Name + "…", () =>
                {
                    // la fiche de test est faite AVANT le reset : logs, réglages et DLL sont encore en place
                    try { autoFiche = Report.Write(g, gpuText); } catch (Exception ex) { Log.W("Fiche automatique impossible : " + ex.Message); }
                    errs = new Installer(g).Reset();
                }, g,
                () =>
                {
                    string fm = autoFiche != null ? "\n\nFiche de test créée automatiquement avant le reset :\n" + Path.GetFileName(autoFiche) : "";
                    if ((errs == null || errs.Count == 0) && (g.CleanPlan.Count > 0 || g.OneClickInstalled))
                    {
                        if (Ask("Installation du launcher défaite.\n\nIl reste des traces d'autres mods ou launchers dans ce jeu (" + Math.Max(1, g.CleanPlan.Count) + " élément(s)).\nLes nettoyer aussi ?", MessageBoxIcon.Question) == DialogResult.Yes) DoClean(g);
                        return;
                    }
                    if (errs == null || errs.Count == 0) Info("Dossier de " + g.Name + " remis comme avant. Les logs ont été archivés dans « journaux »." + fm);
                    else Error("Réinitialisation partielle — " + errs.Count + " problème(s) :\n\n" + string.Join("\n", errs.Take(12)) +
                               "\n\nLe manifeste garde ce qui reste à défaire : ferme tout programme qui utilise le jeu et réessaie." +
                               (g.Store == "Steam" ? "\nEn dernier recours : Steam → Propriétés → Fichiers installés → Vérifier l'intégrité." : ""));
                });
        }

        void DoClean(Game g)
        {
            var plan = g.CleanPlan;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Réinitialisation universelle de " + g.Name + "\n");
            if (g.Installed != null) sb.AppendLine("• L'installation du launcher sera défaite d'abord (manifeste exact).");
            if (g.OneClickInstalled) sb.AppendLine("• L'installation du 1-Click DLSS5 sera défaite d'après SON manifeste (originaux remis).");
            if (plan.Count > 0)
            {
                sb.AppendLine("• Ces restes de mods seront mis en QUARANTAINE (pas supprimés) :");
                foreach (var it in plan.Take(14)) sb.AppendLine("     " + Short(g, it.Path) + (it.IsDir ? "\\" : "") + "   — " + it.Reason);
                if (plan.Count > 14) sb.AppendLine("     … et " + (plan.Count - 14) + " autre(s)");
            }
            sb.AppendLine("\nTout est récupérable avec « Annuler le nettoyage ». Continuer ?");
            if (Ask(sb.ToString(), MessageBoxIcon.Question) != DialogResult.Yes) return;
            CleanResult res = null; string autoFiche2 = null;
            RunJob("Nettoyage de " + g.Name + "…", () =>
            {
                if (g.Installed != null) try { autoFiche2 = Report.Write(g, gpuText); } catch { }
                res = Cleaner.Clean(g);
            }, g, () =>
            {
                string msg = (autoFiche2 != null ? "Fiche de test créée automatiquement avant le reset : " + Path.GetFileName(autoFiche2) + "\n\n" : "") + "Nettoyage terminé : " + res.Moved + " élément(s) en quarantaine" + (res.Restored > 0 ? ", " + res.Restored + " original(aux) du jeu remis" : "") + ".";
                if (res.Errors.Count > 0) msg += "\n\nÀ vérifier :\n" + string.Join("\n", res.Errors.Take(10));
                if (res.NeedValidate && g.Store == "Steam" && g.AppId != null)
                {
                    msg += "\n\nDes DLL que le jeu avait peut-être d'origine (DLSS, NR, streamline) ont été retirées.\nLancer la vérification des fichiers Steam pour remettre les originaux manquants ?";
                    if (Ask(msg, MessageBoxIcon.Question) == DialogResult.Yes)
                        try { Process.Start(new ProcessStartInfo("steam://validate/" + g.AppId) { UseShellExecute = true }); } catch { }
                }
                else
                {
                    if (res.NeedValidate) msg += "\n\nSi le jeu ne démarre plus : répare-le avec son launcher (vérification des fichiers).";
                    Info(msg);
                }
            });
        }

        void DoUndo()
        {
            var g = selected; if (g == null || busy) return;
            string q = Cleaner.LastQuarantine(g);
            if (q == null) return;
            if (Ask("Annuler le nettoyage du " + Path.GetFileName(q) + " ?\n\nLes fichiers en quarantaine retournent dans le jeu, et les originaux remis sont retirés.", MessageBoxIcon.Question) != DialogResult.Yes) return;
            List<string> errs = null;
            RunJob("Annulation du nettoyage…", () => { errs = Cleaner.Undo(q); }, g, () =>
            {
                if (errs.Count == 0) Info("Nettoyage annulé : le jeu est revenu à l'état d'avant.");
                else Error("Annulation partielle :\n\n" + string.Join("\n", errs.Take(12)));
            });
        }

        void DoTdr(Action refresh)
        {
            int cur = Stability.TdrDelay();
            int target = cur >= 8 ? 0 : 8;
            if (Ask(target > 0 ? "Passer le délai « GPU bloqué » de Windows de 2 s à 8 s ?\n\nRéglage système (HKLM\\...\\GraphicsDrivers\\TdrDelay). Effet après un redémarrage. Réversible avec le même bouton."
                               : "Remettre le délai « GPU bloqué » de Windows à sa valeur par défaut (2 s) ?\nEffet après un redémarrage.", MessageBoxIcon.Question) != DialogResult.Yes) return;
            try { Stability.SetTdrDelay(target); refresh(); Info("C'est fait. Redémarre Windows pour que ça s'applique."); }
            catch (Exception ex)
            {
                if ((ex is UnauthorizedAccessException || ex is System.Security.SecurityException) && !IsAdmin())
                { if (Ask("Il faut les droits administrateur pour ce réglage.\n\nRelancer le launcher en administrateur ?", MessageBoxIcon.Warning) == DialogResult.Yes) RestartAsAdmin(); }
                else Error("Impossible : " + ex.Message);
            }
        }

        void DoFriendCopy()
        {
            if (busy) return;
            using (var d = new FolderBrowserDialog { Description = "Où créer la copie pour ton ami ? (un dossier « DLSS5-Launcher » y sera créé)" })
            {
                if (d.ShowDialog(this) != DialogResult.OK) return;
                string dest = Path.Combine(d.SelectedPath, "DLSS5-Launcher");
                if (Directory.Exists(dest)) { Error("Un dossier DLSS5-Launcher existe déjà là. Choisis un autre endroit."); return; }
                List<PItem> chk = null;
                RunJob("Copie pour un ami (copie puis vérification des SHA-256)…", () => { FriendCopy.Make(dest); chk = FriendCopy.Verify(dest); }, null, () =>
                {
                    var bad = chk == null ? new List<PItem>() : chk.Where(x => x.State != PState.Ok).ToList();
                    string ver = chk == null ? "" : bad.Count == 0
                        ? "\n\nDocteur payload : la copie est complète et identique au payload testé (" + chk.Count + " éléments vérifiés)."
                        : "\n\nATTENTION, docteur payload : " + PayloadDoctor.Verdict(chk) + " dans la copie :\n" + string.Join("\n", bad.Take(8).Select(x => "  " + x.StateText + " : " + x.Rel)) + "\nCorrige ton payload avant d'envoyer la copie.";
                    Info("Copie prête dans :\n" + dest + "\n\nDonne tout le dossier à ton ami. Il double-clique DLSS5-Launcher.exe : sur une Radeon, le mode AMD s'active tout seul.\n" +
                         "L'installation dans les jeux avec anti-cheat y est bloquée d'office.\nLis aussi « POUR-TON-AMI.txt » dedans." + ver);
                    try { Process.Start("explorer.exe", "\"" + dest + "\""); } catch { }
                });
            }
        }

        string forcedGpu, forcedNr;
        void ForcedGpuFromConfig()
        {
            try
            {
                if (!File.Exists(AppPaths.Config)) return;
                foreach (var l in File.ReadAllLines(AppPaths.Config))
                {
                    if (l.StartsWith("dllNr=")) { string v = l.Substring(6).Trim(); if (v == "openNR" || v == "origine") { Gpu.ForceNr = v; forcedNr = v; } }
                    if (l.StartsWith("forcerGpu=")) { string v = l.Substring(10).Trim(); if (v == "AMD" || v == "NVIDIA" || v == "Intel") { Gpu.Vendor = v; forcedGpu = v; gpuText += "  [forcé : " + v + "]"; } }
                }
            }
            catch { }
        }

        static string Short(Game g, string p)
        {
            string r = g.Root.TrimEnd('\\', '/');
            return p.StartsWith(r, StringComparison.OrdinalIgnoreCase) ? "…" + p.Substring(r.Length) : p;
        }

        void DoApply()
        {
            var g = selected; if (g == null || busy) return;
            var st = settings.Clone();
            RunJob("Application des réglages…", () => new Installer(g).ApplySettings(st), g, () => Info("Réglages appliqués à " + g.Name + ". Ils prendront effet au prochain lancement du jeu."));
        }

        void DoReport()
        {
            var g = selected; if (g == null) return;
            try
            {
                string p = Report.Write(g, gpuText);
                Process.Start(new ProcessStartInfo("notepad.exe", "\"" + p + "\"") { UseShellExecute = true });
            }
            catch (Exception ex) { Error("Impossible de créer la fiche : " + ex.Message); }
        }

        void RunJob(string label, Action work, Game g, Action onOk)
        {
            SetBusy(true, label);
            var t = new Thread(() =>
            {
                Exception err = null;
                try { work(); } catch (Exception ex) { err = ex; }
                if (g != null) try { Scanner.Analyze(g); } catch { }
                BeginInvoke((Action)(() =>
                {
                    SetBusy(false, "Prêt");
                    FillList(); if (g != null) Select(g); ShowGame();
                    if (err == null) { onOk(); return; }
                    Log.W("ERREUR : " + err.Message);
                    if (err is UnauthorizedAccessException && !IsAdmin())
                    {
                        if (Ask("Windows refuse l'écriture dans le dossier du jeu (souvent dans Program Files).\n\nRelancer le launcher en administrateur ?", MessageBoxIcon.Warning) == DialogResult.Yes) RestartAsAdmin();
                    }
                    else Error(err is UserFacingException ? err.Message : "Erreur : " + err.Message +
                        (g != null && g.Installed != null ? "\n\nTout ce qui a été fait est noté dans le manifeste : « Réinitialiser le jeu » peut le défaire." : ""));
                }));
            }) { IsBackground = true };
            t.Start();
        }

        void LaunchGame()
        {
            var g = selected; if (g == null) return;
            if (watchGame == g) { Info("La partie de " + g.Name + " est déjà surveillée."); return; }
            if (watchGame != null && Ask("Une partie de " + watchGame.Name + " est déjà surveillée. Arrêter de la surveiller pour " + g.Name + " ?", MessageBoxIcon.Question) != DialogResult.Yes) return;
            CommitStep();
            History.RecordLive(g);   // le prochain lancement écrase OptiScaler.log : on note la partie d'avant
            if (g.Store == "Xbox")
                Info("Lance " + g.Name + " depuis l'app Xbox (le lancer directement contournerait sa licence).\n\nLe launcher surveille : dès que le jeu démarre, puis quand il se ferme, la partie sera notée.");
            else
                try
                {
                    if (g.Store == "Steam" && g.AppId != null) Process.Start(new ProcessStartInfo("steam://rungameid/" + g.AppId) { UseShellExecute = true });
                    else Process.Start(new ProcessStartInfo(g.ExePath) { WorkingDirectory = g.ExeDir, UseShellExecute = true });
                    Log.W("Lancement de " + g.Name);
                }
                catch (Exception ex) { Error("Lancement impossible : " + ex.Message); return; }
            StartWatch(g);
        }

        // =============================================================== surveillance de la partie
        Game watchGame; Process watchProc; DateTime watchAsked, watchStart; bool watchExited; DateTime watchExitAt;
        System.Windows.Forms.Timer watchTimer;

        void StartWatch(Game g)
        {
            watchGame = g; watchProc = null; watchAsked = DateTime.Now; watchExited = false;
            if (watchTimer == null) { watchTimer = new System.Windows.Forms.Timer { Interval = 3000 }; watchTimer.Tick += (s, e) => WatchTick(); }
            watchTimer.Start();
            Log.W("Surveillance de " + g.Name + " : en attente du démarrage du jeu (" + g.ExeName + ").");
            lblFooter.Text = "En attente du démarrage de " + g.Name + "…";
            ShowGame();
        }

        void StopWatch() { if (watchTimer != null) watchTimer.Stop(); watchGame = null; watchProc = null; ShowGame(); }

        void WatchTick()
        {
            var g = watchGame; if (g == null) { watchTimer.Stop(); return; }
            try
            {
                if (watchProc == null)
                {
                    watchProc = FindGameProcess(g);
                    if (watchProc != null)
                    {
                        try { watchStart = watchProc.StartTime; } catch { watchStart = DateTime.Now; }
                        Log.W("Surveillance : " + g.Name + " a démarré à " + watchStart.ToString("HH:mm:ss") + ".");
                    }
                    else if ((DateTime.Now - watchAsked).TotalMinutes > 10)
                    {
                        Log.W("Surveillance : " + g.Name + " n'a pas démarré en 10 minutes, j'arrête de surveiller.");
                        lblFooter.Text = "Surveillance arrêtée : " + g.Name + " n'a pas démarré."; StopWatch();
                    }
                    return;
                }
                if (!watchExited)
                {
                    bool exited; try { exited = watchProc.HasExited; } catch { exited = FindGameProcess(g) == null; }
                    if (!exited) { if (!busy) lblFooter.Text = "Partie en cours : " + g.Name + " — " + History.Hours((DateTime.Now - watchStart).TotalMinutes) + " (surveillée)"; return; }
                    watchExited = true; watchExitAt = DateTime.Now;   // on laisse quelques secondes au log pour finir de s'écrire
                    return;
                }
                if ((DateTime.Now - watchExitAt).TotalSeconds < 12)
                {   // certains jeux se relancent eux-mêmes (lanceur, anti-triche) : si un nouveau processus apparaît, on continue
                    var again = FindGameProcess(g);
                    if (again != null) { watchProc = again; watchExited = false; Log.W("Surveillance : " + g.Name + " s'est relancé, je continue."); }
                    return;
                }
                watchTimer.Stop();
                int code = int.MinValue; try { code = watchProc.ExitCode; } catch { }
                DateTime start = watchStart, end = watchExitAt;
                watchGame = null; watchProc = null;
                OnGameClosed(g, start, end, code);
            }
            catch (Exception ex) { Log.W("Surveillance : " + ex.Message); StopWatch(); }
        }

        static Process FindGameProcess(Game g)
        {
            if (g.ExePath == null) return null;
            foreach (var p in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(g.ExePath)))
            {
                try { if (!string.Equals(p.MainModule.FileName, g.ExePath, StringComparison.OrdinalIgnoreCase)) continue; } catch { }
                return p;
            }
            return null;
        }

        void OnGameClosed(Game g, DateTime start, DateTime end, int code)
        {
            double minutes = (end - start).TotalMinutes;
            Log.W("Surveillance : " + g.Name + " fermé après " + History.Hours(minutes) + (code != int.MinValue ? " (code de sortie " + code + ")" : "") + ".");
            try { Scanner.Analyze(g); } catch { }
            string log = g.ExeDir == null ? null : Path.Combine(g.ExeDir, "OptiScaler.log");
            bool fresh = log != null && File.Exists(log) && File.GetLastWriteTime(log) >= start.AddMinutes(-1);
            if (fresh)
            {
                var s = History.RecordLive(g);
                if (s != null && s.Crashed)
                {
                    string fiche = null; try { fiche = Report.Write(g, gpuText); } catch (Exception ex) { Log.W("Fiche automatique impossible : " + ex.Message); }
                    ShowGame(); FlashAndFront();
                    Info("Crash détecté dans " + g.Name + " : " + s.Crash + ", après " + History.Hours(s.Minutes) + " de jeu.\n\n" +
                         (fiche != null ? "Fiche de test créée automatiquement : " + Path.GetFileName(fiche) + "\n\n" : "") +
                         "Tu peux réinitialiser le jeu tout de suite : rien ne sera perdu. « 🩺 Conseils » dit quoi changer.");
                }
                else lblFooter.Text = "Partie notée : " + g.Name + ", " + History.Hours(minutes) + (s != null && s.Samples > 0 ? ", NR ≈ " + s.Avg.ToString("0.0", CultureInfo.GetCultureInfo("fr-CA")) + " ms" : "") + ", aucun crash détecté.";
            }
            else
            {
                // pas de log OptiScaler pour cette partie (vanille, RenoDX, Feeder ReShade) : on demande
                string step = Steps.Current(g.Name);
                FlashAndFront();
                var ans = MessageBox.Show(this, g.Name + " s'est fermé après " + History.Hours(minutes) + (step != "" ? " (étape « " + step + " »)" : "") + ".\n\n" +
                    "Le launcher ne peut pas lire cette partie lui-même" + (g.Installed == null ? " (aucun mod installé)" : "") + ".\n\nLe jeu a-t-il planté ?\n\n" +
                    "Oui = crash, Non = tout s'est bien passé, Annuler = ne pas noter la partie.",
                    "DLSS 5 Launcher", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                lblFooter.Text = ans == DialogResult.Cancel ? "Partie de " + g.Name + " non notée." : "Partie notée : " + g.Name + ", " + History.Hours(minutes) + (ans == DialogResult.Yes ? ", crash." : ", aucun crash.");
                if (ans != DialogResult.Cancel)
                    History.RecordManual(g.Name, start, Math.Max(1, Math.Round(minutes)), ans == DialogResult.Yes ? "crash (signalé à la fermeture)" : History.NoCrash, step,
                                         "durée mesurée par le launcher" + (g.Installed == null ? ", jeu sans DLSS 5" : "") + (code != int.MinValue && code != 0 ? ", code de sortie " + code : ""));
            }
            ShowGame();
        }

        [DllImport("user32.dll")] static extern bool FlashWindow(IntPtr hwnd, bool invert);
        void FlashAndFront()
        {
            try { if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal; Activate(); FlashWindow(Handle, true); } catch { }
        }

        // =============================================================== versions d'OptiScaler
        void FillOpti()
        {
            optiFolders = OptiVersions.Folders();
            optiGuard = true;
            cbOpti.Items.Clear();
            foreach (var f in optiFolders) cbOpti.Items.Add(OptiVersions.Label(f));
            if (optiFolders.Count == 0) cbOpti.Items.Add("(aucune dans payload\\)");
            optiGuard = false;
            LoadOpti();
        }
        void LoadOpti()
        {
            if (cbOpti == null) return;
            if (optiFolders.Count == 0) { optiGuard = true; cbOpti.SelectedIndex = 0; optiGuard = false; return; }
            string f = selected == null ? OptiVersions.Default : OptiVersions.For(selected.Name);
            int i = Math.Max(0, optiFolders.IndexOf(f));
            optiGuard = true; cbOpti.SelectedIndex = i; optiGuard = false;
        }
        void OptiChanged()
        {
            if (optiGuard || selected == null || cbOpti.SelectedIndex < 0 || cbOpti.SelectedIndex >= optiFolders.Count) return;
            string f = optiFolders[cbOpti.SelectedIndex];
            if (f == OptiVersions.For(selected.Name)) return;
            try { OptiVersions.Set(selected.Name, f); } catch (Exception ex) { Error(ex.Message); return; }
            if (selected.Installed != null && selected.Installed.UsesOptiIni && selected.Installed.Opti != f)
                Info("C'est noté pour " + selected.Name + ". La version installée reste en place : réinitialise puis réinstalle pour passer à " + OptiVersions.Label(f) + ".");
            ShowGame();
        }

        // =============================================================== conseils
        void ShowAdvice(Game g)
        {
            if (g == null) return;
            List<Fix> fixes;
            try { fixes = Report.Fixes(g, settings); } catch (Exception ex) { Error("Lecture des logs impossible : " + ex.Message); return; }
            if (fixes.Count == 0) { Info("Rien à conseiller pour " + g.Name + " d'après sa dernière partie (log d'OptiScaler, ou le dernier journal archivé)."); return; }
            var f = new Form { Text = "Conseils — " + g.Name, Width = 760, Height = 470, StartPosition = FormStartPosition.CenterParent, BackColor = Theme.Panel, ForeColor = Theme.Text, Font = Theme.UI, FormBorderStyle = FormBorderStyle.FixedDialog, MinimizeBox = false, MaximizeBox = false, ShowIcon = false };
            var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(16), BackColor = Theme.Panel };
            flow.Controls.Add(new Label { Text = "D'après la dernière partie. Les cases cochées changent le profil de ce jeu" + (g.Installed != null && g.Installed.Complete ? ", puis le jeu installé." : " (prochaine installation)."), AutoSize = true, MaximumSize = new Size(700, 0), ForeColor = Theme.Dim, Margin = new Padding(0, 0, 0, 10) });
            var boxes = new List<KeyValuePair<CheckBox, Fix>>();
            foreach (var x in fixes)
            {
                if (x.Apply != null)
                {
                    var cb = new CheckBox { Text = x.Text, Checked = true, AutoSize = true, ForeColor = Theme.Text, Font = Theme.UIBold, Margin = new Padding(0, 4, 0, 0) };
                    flow.Controls.Add(cb); boxes.Add(new KeyValuePair<CheckBox, Fix>(cb, x));
                }
                else flow.Controls.Add(new Label { Text = "• " + x.Text, AutoSize = true, ForeColor = Theme.Yellow, Font = Theme.UIBold, Margin = new Padding(0, 6, 0, 0) });
                flow.Controls.Add(new Label { Text = x.Why, AutoSize = true, MaximumSize = new Size(690, 0), ForeColor = Theme.Dim, Margin = new Padding(20, 0, 0, 6) });
            }
            var chkStep = new CheckBox { AutoSize = true, Checked = true, ForeColor = Theme.Text, Margin = new Padding(0, 12, 0, 0) };
            flow.Controls.Add(chkStep);
            Action upd = () =>
            {
                var sel = boxes.Where(b => b.Key.Checked).Select(b => b.Value.Short).ToList();
                chkStep.Text = "Ranger les prochaines parties sous l'étape « après : " + (sel.Count == 0 ? "…" : string.Join(", ", sel)) + " »";
                chkStep.Enabled = sel.Count > 0;
            };
            foreach (var b in boxes) b.Key.CheckedChanged += (s, e) => upd();
            upd();
            var bar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 52, BackColor = Theme.Panel, Padding = new Padding(16, 6, 10, 0) };
            var bOk = Theme.Btn("Appliquer", Theme.Green, 140); var bNo = Theme.Btn("Fermer", Theme.Line, 110);
            bOk.Enabled = boxes.Count > 0;
            bar.Controls.AddRange(new Control[] { bOk, bNo });
            f.Controls.Add(flow); f.Controls.Add(bar);
            bNo.Click += (s, e) => f.Close();
            bOk.Click += (s, e) =>
            {
                var chosen = boxes.Where(b => b.Key.Checked).Select(b => b.Value).ToList();
                if (chosen.Count == 0) return;
                foreach (var x in chosen) x.Apply(settings);
                presetName = "Personnalisé"; SelectPresetName(presetName);
                SaveCurrent(); RefreshControls();
                if (chkStep.Checked) Steps.Set(g.Name, "après : " + string.Join(", ", chosen.Select(x => x.Short)));
                LoadStep();
                Log.W("Conseils appliqués à " + g.Name + " : " + string.Join(" ; ", chosen.Select(x => x.Text)));
                f.Close();
                if (g.Installed != null && g.Installed.Complete) DoApply();
                else Info("Profil de " + g.Name + " mis à jour. Ces réglages seront utilisés à la prochaine installation.");
            };
            f.ShowDialog(this);
        }
        void OpenFolder()
        {
            var g = selected; if (g == null) return;
            string d = g.ExeDir ?? g.Root;
            Process.Start("explorer.exe", "\"" + d + "\"");
        }
        void OpenLog()
        {
            var g = selected; if (g == null || g.ExeDir == null) return;
            foreach (var n in new[] { "OptiScaler.log", "ReShade.log" })
            {
                string p = Path.Combine(g.ExeDir, n);
                if (File.Exists(p)) { Process.Start(new ProcessStartInfo("notepad.exe", "\"" + p + "\"") { UseShellExecute = true }); return; }
            }
            Info("Pas encore de log dans ce jeu. Lance-le une fois avec DLSS 5 installé.");
        }
        void AddFolder()
        {
            using (var d = new FolderBrowserDialog { Description = "Choisis le dossier d'installation du jeu (celui qui contient l'exe ou ses sous-dossiers)" })
            {
                if (d.ShowDialog(this) != DialogResult.OK) return;
                if (!customFolders.Contains(d.SelectedPath, StringComparer.OrdinalIgnoreCase)) customFolders.Add(d.SelectedPath);
                SaveConfig(); StartScan();
            }
        }

        // =============================================================== protocole de test
        void LoadStep() { if (cbStep != null) cbStep.Text = selected == null ? "" : Steps.Current(selected.Name); }
        void CommitStep()
        {
            if (cbStep == null || selected == null) return;
            try { string before = Steps.Current(selected.Name); Steps.Set(selected.Name, cbStep.Text); if (before != Steps.Current(selected.Name)) ShowGame(); }
            catch (Exception ex) { Log.W("Étape de test : " + ex.Message); }
        }

        void DoManualSession(Game g)
        {
            if (g == null) return;
            CommitStep();
            var f = new Form { Text = "Noter une partie à la main — " + g.Name, Width = 560, Height = 360, StartPosition = FormStartPosition.CenterParent, BackColor = Theme.Panel, ForeColor = Theme.Text, Font = Theme.UI, FormBorderStyle = FormBorderStyle.FixedDialog, MinimizeBox = false, MaximizeBox = false, ShowIcon = false };
            var t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(16), BackColor = Theme.Panel };
            Func<string, Label> lab = x => new Label { Text = x, AutoSize = true, ForeColor = Theme.Text, Margin = new Padding(0, 8, 10, 6) };
            var tStart = new TextBox { Width = 200, Text = DateTime.Now.AddHours(-1).ToString("yyyy-MM-dd HH:mm") }; Theme.Dark(tStart);
            var nMin = new SafeNum { Width = 100, Minimum = 1, Maximum = 2000, Value = 60 }; Theme.Dark(nMin);
            var cCrash = new SafeCombo { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300 }; Theme.Dark(cCrash);
            cCrash.Items.AddRange(new object[] { History.NoCrash, "crash du jeu (retour au bureau)", "gel / écran noir", "crash GPU (pilote réinitialisé)", "autre (voir la note)" }); cCrash.SelectedIndex = 0;
            var cStep = new SafeCombo { DropDownStyle = ComboBoxStyle.DropDown, Width = 300, Text = Steps.Current(g.Name) }; Theme.Dark(cStep);
            cStep.Items.AddRange(Steps.Suggestions); cStep.Text = Steps.Current(g.Name);
            var tNote = new TextBox { Width = 300, Text = g.Installed == null ? "jeu sans DLSS 5" : "" }; Theme.Dark(tNote);
            t.Controls.Add(lab("Début (AAAA-MM-JJ HH:MM) :"), 0, 0); t.Controls.Add(tStart, 1, 0);
            t.Controls.Add(lab("Durée (minutes) :"), 0, 1); t.Controls.Add(nMin, 1, 1);
            t.Controls.Add(lab("Crash :"), 0, 2); t.Controls.Add(cCrash, 1, 2);
            t.Controls.Add(lab("Étape de test :"), 0, 3); t.Controls.Add(cStep, 1, 3);
            t.Controls.Add(lab("Note :"), 0, 4); t.Controls.Add(tNote, 1, 4);
            var bar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, BackColor = Theme.Panel, Padding = new Padding(16, 6, 10, 0) };
            var bOk = Theme.Btn("Noter la partie", Theme.Green, 160); var bNo = Theme.Btn("Annuler", Theme.Line, 110);
            bar.Controls.AddRange(new Control[] { bOk, bNo });
            f.Controls.Add(t); f.Controls.Add(bar);
            bNo.Click += (s, e) => f.Close();
            bOk.Click += (s, e) =>
            {
                DateTime st;
                if (!DateTime.TryParseExact(tStart.Text.Trim(), "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out st))
                { Error("Début illisible : écris-le comme 2026-10-25 19:30."); return; }
                History.RecordManual(g.Name, st, (double)nMin.Value, (string)cCrash.SelectedItem, cStep.Text.Trim(), tNote.Text.Trim());
                Log.W("Partie notée à la main : " + g.Name + ", " + nMin.Value + " min, " + cCrash.SelectedItem + (cStep.Text.Trim() != "" ? ", étape « " + cStep.Text.Trim() + " »" : ""));
                f.Close(); ShowGame();
            };
            f.AcceptButton = bOk; f.CancelButton = bNo;
            f.ShowDialog(this);
        }

        // =============================================================== historique
        void ShowHistory(Game g)
        {
            var f = new Form { Text = "Historique des parties", Width = 1180, Height = 640, StartPosition = FormStartPosition.CenterParent, BackColor = Theme.Panel, ForeColor = Theme.Text, Font = Theme.UI, MinimizeBox = false, ShowIcon = false };
            var box = new TextBox { Multiline = true, ReadOnly = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Both, WordWrap = false, BackColor = Color.FromArgb(14, 16, 19), ForeColor = Theme.Text, Font = Theme.Mono, BorderStyle = BorderStyle.None };
            var bar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, BackColor = Theme.Panel, Padding = new Padding(10, 8, 10, 0) };
            var bAll = Theme.Btn(g == null ? "Ce jeu" : "Tous les jeux", Theme.Line, 150);
            var bCsv = Theme.Btn("Ouvrir historique.csv", Theme.Green, 190);
            var bCopy = Theme.Btn("Copier", Theme.Blue, 110);
            var bClose = Theme.Btn("Fermer", Theme.Line, 110);
            bar.Controls.AddRange(new Control[] { bAll, bCsv, bCopy, bClose });
            bAll.Visible = g != null;
            f.Controls.Add(box); f.Controls.Add(bar);
            bool all = g == null;
            Action fill = () =>
            {
                var sb = new System.Text.StringBuilder();
                var everything = History.Load();
                var groups = all ? everything.GroupBy(x => x.Game).OrderBy(x => x.Key).ToList()
                                 : everything.Where(x => x.Key.StartsWith(Installer.Safe(g.Name) + "|", StringComparison.Ordinal)).GroupBy(x => x.Game).ToList();
                if (groups.Count == 0) sb.AppendLine(all ? "Aucune partie notée pour l'instant." : "Aucune partie notée pour " + g.Name + ".\r\n\r\nUne partie entre dans l'historique quand le launcher retrouve son OptiScaler.log : au démarrage, au reset,\r\nà la fiche de test, ou juste avant « Lancer le jeu ».");
                foreach (var grp in groups)
                {
                    var l = grp.OrderBy(x => x.Start).ToList();
                    sb.AppendLine("■ " + grp.Key + " — " + l.Count + " partie" + (l.Count > 1 ? "s" : "") + ", " + History.Hours(l.Sum(x => x.Minutes)) + " de jeu");
                    sb.AppendLine();
                    var cs = History.CompareSteps(l);
                    if (cs.Count > 0)
                    {
                        sb.AppendLine("  Par étape de test (protocole A/B) :");
                        foreach (var c in cs) sb.AppendLine("    " + c);
                        sb.AppendLine();
                    }
                    sb.AppendLine("  Par montage :");
                    var cm = History.Compare(l);
                    if (cm.Count == 0) sb.AppendLine("    (aucune partie avec une mesure du NR)");
                    foreach (var c in cm) sb.AppendLine("    " + c);
                    sb.AppendLine();
                    sb.AppendLine("  Parties :");
                    foreach (var x in l) sb.AppendLine("    " + History.Line(x));
                    sb.AppendLine();
                }
                sb.AppendLine("Les parties avec OptiScaler (OptiScaler F5, Feeder F5Low) sont notées toutes seules. Les autres (vanille, RenoDX, Feeder ReShade) :");
                sb.AppendLine("bouton « Partie à la main ». OptiScaler réécrit son log à chaque lancement : deux parties jouées sans repasser par le launcher");
                sb.AppendLine("(démarrage, « Lancer le jeu », fiche ou reset) n'en font qu'une, la dernière.");
                box.Text = sb.ToString();
                box.SelectionLength = 0;
                f.Text = all ? "Historique — tous les jeux" : "Historique — " + g.Name;
            };
            bAll.Click += (s, e) => { if (g == null) return; all = !all; bAll.Text = all ? "Ce jeu" : "Tous les jeux"; fill(); };
            bCsv.Click += (s, e) => { try { if (File.Exists(History.FilePath)) Process.Start(new ProcessStartInfo(History.FilePath) { UseShellExecute = true }); else Info("Pas encore d'historique."); } catch (Exception ex) { Error(ex.Message); } };
            bCopy.Click += (s, e) => { try { Clipboard.SetText(box.Text); bCopy.Text = "Copié ✓"; } catch { } };
            bClose.Click += (s, e) => f.Close();
            if (g != null && g.Installed != null) History.RecordLive(g);
            fill();
            f.Shown += (s, e) => { box.SelectionLength = 0; bClose.Focus(); };
            f.ShowDialog(this);
        }

        // =============================================================== docteur payload
        void CheckPayloadAtStart() { RunDoctor(false); }

        // Vérifie payload\ en arrière-plan (≈ 800 Mo à hasher la première fois) et met l'en-tête à jour.
        void RunDoctor(bool full, Action then = null)
        {
            if (full) Hash.ClearCache();
            lblPayload.Text = "Payload : vérification…"; lblPayload.ForeColor = Theme.Dim;
            new Thread(() =>
            {
                List<PItem> items = null;
                try { items = PayloadDoctor.Check(); } catch (Exception ex) { Log.W("Docteur payload : erreur — " + ex.Message); }
                BeginInvoke((Action)(() =>
                {
                    payloadItems = items;
                    if (items == null) { lblPayload.Text = "Payload : vérification impossible (voir Journal)"; lblPayload.ForeColor = Theme.Red; }
                    else
                    {
                        var bad = items.Where(x => x.Needed && x.State != PState.Ok).ToList();
                        lblPayload.Text = "Payload : " + PayloadDoctor.Verdict(items) + (bad.Count > 0 ? "  —  clique pour le rapport" : "");
                        lblPayload.ForeColor = bad.Count == 0 ? Theme.Green : bad.Any(x => x.Blocking) ? Theme.Red : Theme.Yellow;
                        Log.W("Docteur payload : " + PayloadDoctor.Verdict(items));
                        foreach (var x in bad) Log.W("  " + x.StateText + " : " + x.Rel + (x.Detail != null ? " — " + x.Detail : ""));
                    }
                    if (then != null) then();
                }));
            }) { IsBackground = true }.Start();
        }

        void ShowDoctor()
        {
            var f = new Form { Text = "Docteur payload", Width = 1060, Height = 640, StartPosition = FormStartPosition.CenterParent, BackColor = Theme.Panel, ForeColor = Theme.Text, Font = Theme.UI, MinimizeBox = false, ShowIcon = false };
            var box = new TextBox { Multiline = true, ReadOnly = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Both, WordWrap = false, BackColor = Color.FromArgb(14, 16, 19), ForeColor = Theme.Text, Font = Theme.Mono, BorderStyle = BorderStyle.None };
            var bar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, BackColor = Theme.Panel, Padding = new Padding(10, 8, 10, 0) };
            var bAgain = Theme.Btn("⟳  Revérifier tout", Theme.Green, 170);
            var bCopy = Theme.Btn("Copier le rapport", Theme.Blue, 170);
            var bOpen = Theme.Btn("Ouvrir payload\\", Theme.Line, 150);
            var bClose = Theme.Btn("Fermer", Theme.Line, 110);
            bar.Controls.AddRange(new Control[] { bAgain, bCopy, bOpen, bClose });
            f.Controls.Add(box); f.Controls.Add(bar);
            Action fill = () => box.Text = payloadItems == null ? "Vérification en cours…" : PayloadDoctor.Report(payloadItems, false).Replace("\n", "\r\n").Replace("\r\r", "\r");
            fill();
            bAgain.Click += (s, e) => { box.Text = "Vérification en cours (calcul des SHA-256)…"; bAgain.Enabled = false; RunDoctor(true, () => { FillOpti(); if (!f.IsDisposed) { fill(); box.SelectionLength = 0; bAgain.Enabled = true; } }); };
            bCopy.Click += (s, e) => { try { Clipboard.SetText(box.Text); bCopy.Text = "Copié ✓"; } catch { } };
            bOpen.Click += (s, e) => { try { Process.Start("explorer.exe", "\"" + AppPaths.Payload + "\""); } catch { } };
            bClose.Click += (s, e) => f.Close();
            f.Shown += (s, e) => { box.SelectionLength = 0; bClose.Focus(); };   // pas tout le texte surligné à l'ouverture
            if (payloadItems == null) RunDoctor(false, () => { if (!f.IsDisposed) fill(); });
            f.ShowDialog(this);
        }

        // Avant une installation : un fichier manquant ou abîmé bloque, une autre version demande confirmation.
        bool PayloadOkFor(InstallMode mode, Game g)
        {
            List<PItem> pb;
            string of = OptiVersions.For(g.Name);
            bool usesOpti = mode == InstallMode.OptiScaler || mode == InstallMode.Feeder;
            try { pb = PayloadDoctor.ProblemsFor(mode, usesOpti ? of : OptiVersions.Default); } catch (Exception ex) { Log.W("Docteur payload : " + ex.Message); return true; }
            if (pb.Count == 0 && usesOpti && of != OptiVersions.Default)
                return Ask("Ce jeu utilisera " + OptiVersions.Label(of) + ", pas la 0.1.32 testée avec le launcher.\n\n" +
                           "Les réglages du launcher restent les mêmes (vérifié sur ses sections [DlssNr]), mais le reste n'a pas encore été validé.\n" +
                           "Astuce : mets une étape de test (ex. « OptiScaler " + OptiVersions.Version(of) + " ») pour comparer avec la 0.1.32.\n\nInstaller avec cette version ?", MessageBoxIcon.Question) == DialogResult.Yes;
            if (pb.Count == 0) return true;
            string list = string.Join("\n", pb.Select(x => "  " + x.StateText + " : " + x.Rel + (x.Detail != null ? "\n      " + x.Detail : "")));
            if (pb.Any(x => x.Blocking))
            {
                Error("Le payload du launcher n'est pas complet pour " + Installer.ModeName(mode) + " :\n\n" + list +
                      "\n\nRien n'a été installé. Recopie ces fichiers dans payload\\ (clique « Payload » en haut pour le rapport complet).");
                return false;
            }
            return Ask("Le payload n'est pas exactement celui qui a été testé :\n\n" + list +
                       "\n\nÇa peut marcher (souvent une mise à jour), mais si quelque chose cloche, ce n'est peut-être pas un bug du launcher.\nLa fiche de test le notera.\n\nInstaller quand même ?", MessageBoxIcon.Warning) == DialogResult.Yes;
        }

        bool acGuard;
        void AntiCheatToggled()
        {
            if (acGuard) return;
            if (!chkAntiCheat.Checked && blockAntiCheat &&
                Ask("Débloquer l'installation dans les jeux avec anti-cheat ?\n\n" +
                    "Injecter une DLL dans un jeu protégé par un anti-cheat peut mener à un BANNISSEMENT DÉFINITIF de ton compte, dès la prochaine connexion en ligne.\n\n" +
                    "Ne le fais que pour un jeu solo joué hors ligne (ou avec l'anti-cheat désactivé par le jeu lui-même). Le launcher te redemandera confirmation à chaque installation.\n\nDébloquer quand même ?", MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                acGuard = true; chkAntiCheat.Checked = true; acGuard = false; return;
            }
            blockAntiCheat = chkAntiCheat.Checked;
            Log.W("Blocage anti-cheat : " + (blockAntiCheat ? "activé" : "désactivé par l'utilisateur"));
            SaveConfig();
        }

        void SetBusy(bool b, string text)
        {
            busy = b; progress.Visible = b; lblFooter.Text = text;
            bScan.Enabled = !b; bAdd.Enabled = !b;
            ShowGame();
            UseWaitCursor = b;
        }

        // =============================================================== utilitaires
        DialogResult Ask(string msg, MessageBoxIcon icon) { return MessageBox.Show(this, msg, "DLSS 5 Launcher", MessageBoxButtons.YesNo, icon, MessageBoxDefaultButton.Button2); }
        void Info(string msg) { MessageBox.Show(this, msg, "DLSS 5 Launcher", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        void Error(string msg) { MessageBox.Show(this, msg, "DLSS 5 Launcher", MessageBoxButtons.OK, MessageBoxIcon.Error); }

        static bool IsAdmin()
        {
            try { return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator); } catch { return false; }
        }
        void RestartAsAdmin()
        {
            try { SaveConfig(); Process.Start(new ProcessStartInfo(Application.ExecutablePath) { Verb = "runas", UseShellExecute = true }); Application.Exit(); }
            catch { }
        }

        static void SetCue(TextBox t, string cue)
        {
            t.Text = cue; t.ForeColor = Theme.Dim;
            t.GotFocus += (s, e) => { if (t.ForeColor == Theme.Dim) { t.Text = ""; t.ForeColor = Theme.Text; } };
            t.LostFocus += (s, e) => { if (t.Text == "") { t.ForeColor = Theme.Dim; t.Text = cue; } };
        }

        void LoadConfig()
        {
            try
            {
                if (!File.Exists(AppPaths.Config)) return;
                var kv = new Dictionary<string, string>();
                foreach (var l in File.ReadAllLines(AppPaths.Config))
                {
                    int eq = l.IndexOf('='); if (eq <= 0) continue;
                    string k = l.Substring(0, eq), v = l.Substring(eq + 1);
                    if (k == "dossier") customFolders.Add(v);
                    else if (k == "preset") { int i; if (int.TryParse(v, out i) && i >= 0 && i < 6) defaultPresetName = Presets.Names[i]; else if (int.TryParse(v, out i)) defaultPresetName = "Personnalisé"; }
                    else if (k == "presetName") defaultPresetName = v;
                    else if (k == "bloquerAntiCheat") blockAntiCheat = v.Trim() == "1";
                    else kv[k] = v;
                }
                defaults.LoadFrom(kv);
                // ancienne config : « couper la génération d'images » est remplacé par le choix FrameGen
                settings = defaults.Clone(); presetName = defaultPresetName;
            }
            catch { }
        }
        void SaveConfig()
        {
            try
            {
                var lines = new List<string> { "# Configuration du DLSS 5 Launcher" };
                foreach (var f in customFolders) lines.Add("dossier=" + f);
                lines.Add("presetName=" + defaultPresetName);
                if (forcedGpu != null) lines.Add("forcerGpu=" + forcedGpu);
                if (forcedNr != null) lines.Add("dllNr=" + forcedNr);
                lines.Add("bloquerAntiCheat=" + (blockAntiCheat ? "1" : "0"));
                defaults.SaveTo(lines);
                File.WriteAllLines(AppPaths.Config, lines);
            }
            catch { }
        }
    }

    // La molette fait défiler la page, jamais changer une valeur par accident (bug signalé : la page arrêtait de
    // défiler et la case changeait quand la souris passait dessus). Les flèches, le clavier et le clic marchent toujours.
    static class WheelGuard
    {
        public const int WM_MOUSEWHEEL = 0x020A;
        public static void ScrollParent(Control c, IntPtr wParam)
        {
            int delta = (short)((long)wParam >> 16 & 0xFFFF);
            for (var p = c.Parent; p != null; p = p.Parent)
            {
                var sp = p as ScrollableControl;
                if (sp != null && sp.AutoScroll && sp.DisplayRectangle.Height > sp.ClientSize.Height)
                {
                    int y = -sp.AutoScrollPosition.Y - delta;   // une encoche = 120 px environ
                    sp.AutoScrollPosition = new Point(-sp.AutoScrollPosition.X, Math.Max(0, y));
                    return;
                }
            }
        }
    }
    class SafeCombo : ComboBox
    {
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WheelGuard.WM_MOUSEWHEEL && !DroppedDown) { WheelGuard.ScrollParent(this, m.WParam); return; }
            base.WndProc(ref m);
        }
    }
    class SafeNum : NumericUpDown
    {
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WheelGuard.WM_MOUSEWHEEL) { WheelGuard.ScrollParent(this, m.WParam); return; }
            base.WndProc(ref m);
        }
        protected override void OnMouseWheel(MouseEventArgs e)
        {   // au cas où la molette arrive par le champ texte interne
            var h = e as HandledMouseEventArgs; if (h != null) h.Handled = true;
            WheelGuard.ScrollParent(this, (IntPtr)(e.Delta << 16));
        }
    }

    static class Program
    {
        [DllImport("user32.dll")] static extern bool SetProcessDPIAware();

        [STAThread]
        static void Main()
        {
            try { SetProcessDPIAware(); } catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("fr-CA");
            Application.ThreadException += (s, e) => MessageBox.Show("Erreur inattendue : " + e.Exception.Message, "DLSS 5 Launcher", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Application.Run(new MainForm());
        }
    }
}
