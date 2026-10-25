// Licence MIT (voir LICENSE-MIT du dépôt travail-avec-ia). Tests automatiques du DLSS 5 Launcher, écrits par Claude (Anthropic).
using System; using System.IO; using System.Linq; using System.Collections.Generic; using Dlss5Launcher;
class T {
  static int fails=0;
  static void Ok(bool c,string m){ Console.WriteLine((c?"  OK   ":"  FAIL ")+m); if(!c) fails++; }
  static Dictionary<string,string> Tree(string root){ var d=new Dictionary<string,string>(); foreach(var f in Directory.GetFiles(root,"*",SearchOption.AllDirectories)) d[f.Substring(root.Length)]=Hash.Sha(f); foreach(var x in Directory.GetDirectories(root,"*",SearchOption.AllDirectories)) d[x.Substring(root.Length)+"/"]="DIR"; return d; }
  static bool Same(Dictionary<string,string> a, Dictionary<string,string> b){ if(a.Count!=b.Count){ Console.WriteLine("    diff count "+a.Count+" vs "+b.Count+" : "+string.Join(",",a.Keys.Except(b.Keys).Concat(b.Keys.Except(a.Keys)))); return false;} foreach(var k in a.Keys) if(!b.ContainsKey(k)||b[k]!=a[k]){Console.WriteLine("    diff "+k); return false;} return true; }
  static Game Make(string root, string exeSrc){
    if(Directory.Exists(root)) Directory.Delete(root,true);
    var bin=Path.Combine(root,"Game","Binaries","Win64"); Directory.CreateDirectory(bin);
    File.Copy(exeSrc, Path.Combine(bin,"Game-Win64-Shipping.exe"));
    File.WriteAllText(Path.Combine(root,"Game.exe"),"MZ stub");
    Directory.CreateDirectory(Path.Combine(bin,"D3D12")); File.WriteAllText(Path.Combine(bin,"D3D12","D3D12Core.dll"),"x");
    var pl=Path.Combine(root,"Engine","Plugins","Marketplace","DLSS","Binaries","ThirdParty","Win64"); Directory.CreateDirectory(pl); File.WriteAllText(Path.Combine(pl,"nvngx_dlss.dll"),"dlss");
    var g=new Game{Name="Faux Jeu",Store="Manuel",Root=root}; Scanner.Analyze(g); return g;
  }
  static void Main(string[] a){
    string exe=a[0], reshade=a[1], work=a[2];
    // PE
    var pe=Pe.Read(reshade); Ok(pe.Ok && pe.Is64, "PE ReShade lu, 64-bit"); Console.WriteLine("    imports: "+string.Join(",",pe.Imports.Take(12)));
    Ok(Scanner.IsOptiOrReShade(reshade),"ReShade dxgi.dll reconnu par FileVersionInfo");
    // 1. jeu propre
    var g=Make(Path.Combine(work,"jeu1"),exe);
    Ok(g.ExePath.EndsWith("Game-Win64-Shipping.exe"),"exe principal = Shipping ("+g.ExeName+")");
    Ok(g.Is64,"64-bit"); Ok(g.Apis.Contains("DX12"),"DX12 détecté"); Ok(g.Upscalers.Contains("DLSS"),"DLSS détecté dans le plugin UE");
    Ok(g.Status==GameStatus.Compatible,"statut Compatible ("+g.StatusText+")");
    var before=Tree(g.Root);
    new Installer(g).Install(InstallMode.Auto,new NrSettings(),null); Scanner.Analyze(g);
    Ok(g.Status==GameStatus.Installe && g.Installed.Proxy=="dxgi.dll","installé OptiScaler via dxgi.dll");
    string ini=Ini.ReadFile(Path.Combine(g.ExeDir,"OptiScaler.ini"));
    Ok(Ini.Get(ini,"DlssNr","Enabled")=="true" && Ini.Get(ini,"DlssNr","Preset")=="2" && Ini.Get(ini,"DlssNr","WorkingScale")=="1" && Ini.Get(ini,"FrameGen","Enabled")=="false" && Ini.Get(ini,"FrameGen","FGInput")=="nofg","INI généré correctement (sections respectées)");
    Ok(File.Exists(Path.Combine(g.ExeDir,"OptiScaler","D3D12_OptiScaler","D3D12Core.dll")),"dossier OptiScaler copié");
    Ok(g.Upscalers.Count==1,"le dossier OptiScaler installé ne fausse pas la détection ("+g.UpscalerText+")");
    var s2=Presets.Get(1); new Installer(g).ApplySettings(s2);
    ini=Ini.ReadFile(Path.Combine(g.ExeDir,"OptiScaler.ini")); Ok(Ini.Get(ini,"DlssNr","WorkingScale")=="0.75" && Ini.Get(ini,"DlssNr","VitEvery")=="2","Appliquer réglages (Performance)");
    File.WriteAllText(Path.Combine(g.ExeDir,"OptiScaler.log"),"DLSS-NR model feature created\nother line\n");
    Directory.CreateDirectory(Path.Combine(g.ExeDir,"dlssnr-capture")); File.WriteAllText(Path.Combine(g.ExeDir,"dlssnr-capture","a.png"),"img");
    var rep=Report.Write(g,"RTX test"); Ok(File.ReadAllText(rep).Contains("model feature created"),"fiche de test avec extrait du log");
    var errs=new Installer(g).Reset(); Scanner.Analyze(g);
    Ok(errs.Count==0,"reset sans erreur"); Ok(Same(before,Tree(g.Root)),"dossier IDENTIQUE à avant (hash par hash)");
    Ok(Directory.GetDirectories(AppPaths.Journaux,"*",SearchOption.AllDirectories).Any(dd=>File.Exists(Path.Combine(dd,"OptiScaler.log"))),"logs archivés dans journaux");
    // 2. jeu avec mods existants
    g=Make(Path.Combine(work,"jeu2"),exe);
    var d=g.ExeDir; File.Copy(reshade,Path.Combine(d,"dxgi.dll")); File.WriteAllText(Path.Combine(d,"ReShade.ini"),"[RenoDX.DLSS5]\nNRPreset=1\n");
    File.WriteAllText(Path.Combine(d,"nvngx_dlssnr.dll"),"vieille dll 310.8"); File.WriteAllText(Path.Combine(d,"version.dll"),"CET loader");
    Directory.CreateDirectory(Path.Combine(d,"OptiScaler")); File.WriteAllText(Path.Combine(d,"OptiScaler","vieux.txt"),"old");
    Scanner.Analyze(g); before=Tree(g.Root);
    Ok(g.Status==GameStatus.AutreMod,"statut Autre mod ("+string.Join(",",g.ForeignMods)+")");
    Ok(g.TakenProxyNames.Contains("version.dll") && !g.ForeignMods.Contains("version.dll"),"version.dll (CET) protégé, pas déplacé");
    new Installer(g).Install(InstallMode.OptiScaler,new NrSettings(),null); Scanner.Analyze(g);
    Ok(Hash.Sha(Path.Combine(d,"dxgi.dll"))==Hash.Sha(Path.Combine(AppPaths.OptiDir,"OptiScaler.dll")),"OptiScaler installé en dxgi.dll après mise de côté de ReShade");
    Ok(File.ReadAllText(Path.Combine(d,"version.dll"))=="CET loader","version.dll intact");
    Ok(!File.Exists(Path.Combine(d,"ReShade.ini")),"ReShade.ini mis de côté");
    errs=new Installer(g).Reset(); Ok(errs.Count==0 && Same(before,Tree(g.Root)),"reset : mods d'origine restaurés à l'identique");
    // 3. RenoDX
    g=Make(Path.Combine(work,"jeu3"),exe); before=Tree(g.Root);
    var st=new NrSettings(); st.RLocalTone=1.5m;
    new Installer(g).Install(InstallMode.RenoDX,st,null); Scanner.Analyze(g);
    var r=Ini.ReadFile(Path.Combine(g.ExeDir,"ReShade.ini"));
    Ok(g.Installed.Mode=="RENODX" && Ini.Get(r,"RenoDX.DLSS5","NRLocalTone")=="1.5" && Ini.Get(r,"INPUT","KeyOverlay")=="36,0,0,0","RenoDX installé + réglage écrit");
    Ok(new Installer(g).Reset().Count==0 && Same(before,Tree(g.Root)),"reset RenoDX identique");
    // 4. install interrompue (simulée) -> reset quand même
    g=Make(Path.Combine(work,"jeu4"),exe); before=Tree(g.Root);
    File.Move(Path.Combine(AppPaths.OptiDir,"nvngx.dll_dlssnr.dll"),Path.Combine(work,"tmp.dll"));
    try{ new Installer(g).Install(InstallMode.OptiScaler,new NrSettings(),null); Ok(false,"aurait dû refuser (payload manquant)"); }catch(UserFacingException e){ Ok(true,"payload manquant détecté avant de toucher au jeu"); }
    Ok(Same(before,Tree(g.Root)),"jeu intact après refus");
    File.Move(Path.Combine(work,"tmp.dll"),Path.Combine(AppPaths.OptiDir,"nvngx.dll_dlssnr.dll"));
    // crash au milieu: on retire le dossier OptiScaler du payload après l'avoir validé -> simuler via manifeste partiel
    var ins=new Installer(g); new Installer(g).Install(InstallMode.OptiScaler,new NrSettings(),null);
    var m=Manifest.Load(g.ExeDir); m.Complete=false; m.Save(g.ExeDir); Scanner.Analyze(g);
    Ok(g.Status==GameStatus.Incomplet,"install incomplète détectée");
    Ok(new Installer(g).Reset().Count==0 && Same(before,Tree(g.Root)),"reset d'une install incomplète OK");
    // ===== FEEDER =====
    Console.WriteLine("-- Feeder --");
    g=Make(Path.Combine(work,"feed1"),exe);
    File.Delete(Directory.GetFiles(g.Root,"nvngx_dlss.dll",SearchOption.AllDirectories)[0]);
    Scanner.Analyze(g);
    Ok(g.Status==GameStatus.FeederPossible,"jeu DX12 sans upscaler = Feeder possible ("+g.StatusText+")");
    Ok(Installer.Resolve(g,InstallMode.Auto)==InstallMode.Feeder,"Auto choisit Feeder F5Low");
    before=Tree(g.Root);
    new Installer(g).Install(InstallMode.Auto,new NrSettings(),null); Scanner.Analyze(g);
    ini=Ini.ReadFile(Path.Combine(g.ExeDir,"OptiScaler.ini"));
    Ok(g.Installed.Mode=="FEEDER" && g.Installed.ModeLabel=="Feeder F5Low","installé en Feeder F5Low");
    Ok(Ini.Get(ini,"DlssNr","NativeMotion")=="true" && Ini.Get(ini,"DlssNr","NativeInput")=="true" && Ini.Get(ini,"DlssNr","FinishedPicture")=="true"
       && Ini.Get(ini,"DlssNr","NativeUpscaler")=="false" && Ini.Get(ini,"DlssNr","NativeDepthFinder")=="true" && Ini.Get(ini,"DlssNr","WorkingScale")=="0.67"
       && Ini.Get(ini,"DlssNr","RunBeforeSR")=="false" && Ini.Get(ini,"DlssNr","Enabled")=="true","INI DX12 = mode NR only (+ echelle 0.67, profondeur)");
    Ok(g.Upscalers.Count==0,"nos fichiers installes ne faussent pas la detection");
    var fs=new NrSettings(); fs.FSmooth=true; fs.FUseDepth=false; new Installer(g).ApplySettings(fs);
    ini=Ini.ReadFile(Path.Combine(g.ExeDir,"OptiScaler.ini"));
    Ok(Ini.Get(ini,"DlssNr","NativeUpscaler")=="true" && Ini.Get(ini,"DlssNr","NativeUpscalerPass")=="true" && Ini.Get(ini,"DlssNr","FinishedPicture")=="false" && Ini.Get(ini,"DlssNr","NativeInput")=="false" && Ini.Get(ini,"DlssNr","NativeDepthFinder")=="false","Appliquer : lissage -> upscaler virtuel, profondeur coupee");
    File.WriteAllText(Path.Combine(g.ExeDir,"OptiScaler.log"),"Native motion: running\nVirtual upscaler: Evaluate 1 -> pass\n");
    rep=Report.Write(g,"RTX"); var rt=File.ReadAllText(rep);
    Ok(rt.Contains("NativeMotion=true") && rt.Contains("Native motion: running"),"fiche de test feeder (cles Native* + log)");
    Ok(new Installer(g).Reset().Count==0 && Same(before,Tree(g.Root)),"reset Feeder F5Low identique");
    var gv=new Game{Name="vk",Apis=new List<string>{"Vulkan"}};
    var tpl=Ini.ReadFile(Path.Combine(AppPaths.OptiDir,"OptiScaler.ini"));
    var vi=new NrSettings().BuildFeederIni(tpl,gv);
    Ok(Ini.Get(vi,"DlssNr","NativeUpscaler")=="true" && Ini.Get(vi,"DlssNr","FinishedPicture")=="false","Vulkan : chemin upscaler virtuel");
    g=Make(Path.Combine(work,"feed2"),exe);
    File.Delete(Directory.GetFiles(g.Root,"nvngx_dlss.dll",SearchOption.AllDirectories)[0]);
    File.WriteAllText(Path.Combine(g.ExeDir,"dxgi.dll"),"dxvk dxgi"); File.WriteAllText(Path.Combine(g.ExeDir,"d3d11.dll"),"dxvk d3d11");
    Scanner.Analyze(g); before=Tree(g.Root);
    Ok(g.Dxvk,"dxvk detecte");
    new Installer(g).Install(InstallMode.Feeder,new NrSettings(),null); Scanner.Analyze(g);
    Ok(g.Installed.Proxy!="dxgi.dll" && File.ReadAllText(Path.Combine(g.ExeDir,"dxgi.dll"))=="dxvk dxgi","dxvk : OptiScaler sous "+g.Installed.Proxy+", dxgi.dll de dxvk intact");
    ini=Ini.ReadFile(Path.Combine(g.ExeDir,"OptiScaler.ini"));
    Ok(Ini.Get(ini,"DlssNr","NativeUpscaler")=="true","dxvk : chemin upscaler virtuel");
    try{ new Installer(g).Reset(); Scanner.Analyze(g); new Installer(g).Install(InstallMode.FeederReShade,new NrSettings(),null); Ok(false,"ReShade feeder aurait du refuser dxvk"); }
    catch(UserFacingException){ Ok(true,"Feeder ReShade refuse dxvk proprement"); }
    Ok(Same(before,Tree(g.Root)),"jeu dxvk intact apres refus");
    g=Make(Path.Combine(work,"feed3"),exe);
    File.Delete(Directory.GetFiles(g.Root,"nvngx_dlss.dll",SearchOption.AllDirectories)[0]);
    Directory.Delete(Path.Combine(g.ExeDir,"D3D12"),true);
    Directory.CreateDirectory(Path.Combine(g.ExeDir,"reshade-shaders","Shaders")); File.WriteAllText(Path.Combine(g.ExeDir,"reshade-shaders","Shaders","vieux.fx"),"old");
    Scanner.Analyze(g); g.Apis.Clear(); g.Apis.Add("OpenGL"); before=Tree(g.Root);
    Ok(Installer.Resolve(g,InstallMode.Auto)==InstallMode.FeederReShade,"OpenGL : Auto choisit Feeder ReShade");
    new Installer(g).Install(InstallMode.Auto,new NrSettings(),null);
    var d3=g.ExeDir; var ri=Ini.ReadFile(Path.Combine(d3,"ReShade.ini"));
    Ok(File.Exists(Path.Combine(d3,"opengl32.dll")) && File.Exists(Path.Combine(d3,"dlss5-feed.addon64")) && File.Exists(Path.Combine(d3,"nvngx_dlss.dll"))
       && File.Exists(Path.Combine(d3,"reshade-shaders","Shaders","DLSS5_Feed.fx")) && File.Exists(Path.Combine(d3,"reshade-shaders","Textures","t.png"))
       && !File.Exists(Path.Combine(d3,"reshade-shaders","Shaders","vieux.fx")),"Feeder ReShade : proxy opengl32.dll + addons + shaders (anciens mis de cote)");
    Ok(Ini.Get(ri,"RenoDX.DLSS5","EnableHooks")=="2" && Ini.Get(ri,"RenoDX.DLSS5","NREnableUpscaling")=="0" && Ini.Get(ri,"RenoDX.DLSS5","NRPreset")=="2"
       && Ini.Get(ri,"GENERAL","PresetPath")==".\\ReShadePreset.ini" && !ri.Contains("%CACHE%"),"ReShade.ini feeder correct");
    Ok(File.ReadAllText(Path.Combine(d3,"ReShadePreset.ini")).Contains("Lumenite_Kernel@lumenite_Kernel.fx,DLSS5_Feed@DLSS5_Feed.fx"),"ReShadePreset.ini (Lumenite + DLSS5_Feed)");
    Scanner.Analyze(g); g.Apis.Clear(); g.Apis.Add("OpenGL");
    var st2=new NrSettings(); st2.RLocalTone=1.6m; new Installer(g).ApplySettings(st2);
    ri=Ini.ReadFile(Path.Combine(d3,"ReShade.ini"));
    Ok(Ini.Get(ri,"RenoDX.DLSS5","NRLocalTone")=="1.6" && Ini.Get(ri,"RenoDX.DLSS5","EnableHooks")=="2","Appliquer reglages au Feeder ReShade");
    File.WriteAllText(Path.Combine(d3,"dlss5-feed.log"),"feed ok"); File.WriteAllText(Path.Combine(d3,"ReShade.log"),"rs");
    Ok(new Installer(g).Reset().Count==0 && Same(before,Tree(g.Root)),"reset Feeder ReShade identique (anciens shaders restaures)");
    // ===== NETTOYAGE UNIVERSEL =====
    Console.WriteLine("-- Nettoyage universel --");
    // A. installation 1-Click (DIRECT) avec manifeste
    g=Make(Path.Combine(work,"oc1"),exe); d=g.ExeDir;
    File.WriteAllText(Path.Combine(d,"nvngx_dlss.dll"),"dlss ORIGINAL du jeu");
    File.WriteAllText(Path.Combine(d,"README.md"),"readme du jeu");
    var pristine=Tree(g.Root);
    string bk=Path.Combine(d,"_DLSS5_Backup"); string pref="originals/1111-2222";
    Directory.CreateDirectory(Path.Combine(bk,"originals","1111-2222"));
    File.Copy(Path.Combine(d,"nvngx_dlss.dll"),Path.Combine(bk,"originals","1111-2222","nvngx_dlss.dll"));
    File.WriteAllText(Path.Combine(d,"nvngx_dlss.dll"),"dlss 310 du 1-click");
    File.Copy(reshade,Path.Combine(d,"dxgi.dll")); File.WriteAllText(Path.Combine(d,"renodx-dlss5.addon64"),"addon");
    File.WriteAllText(Path.Combine(d,"nvngx_dlssnr.dll"),"nr 1click"); File.WriteAllText(Path.Combine(d,"ReShade.ini"),"[RenoDX.DLSS5]\nEnableHooks=1\n");
    File.WriteAllText(Path.Combine(bk,"manifest.json"),"{\n \"version\": 1,\n \"backupPrefix\": \""+pref+"\",\n \"route\": \"DIRECT\",\n \"game\": { \"dir\": \"x\", \"exe\": \"Game-Win64-Shipping.exe\" },\n \"replaced\": { \"rel\": \"nvngx_dlss.dll\", \"oldVersion\": \"3.7\" },\n \"added\": [ \"renodx-dlss5.addon64\", \"nvngx_dlssnr.dll\", \"ReShade.ini\" ],\n \"addedDirs\": [],\n \"reshade\": { \"installedByUs\": true, \"file\": \"dxgi.dll\", \"filesAdded\": [] }\n}");
    Scanner.Analyze(g); var after1=Tree(g.Root);
    Ok(g.OneClickInstalled && g.Status==GameStatus.AutreMod,"1-Click détecté ("+g.CleanPlan.Count+" traces)");
    try{ new Installer(g).Install(InstallMode.OptiScaler,new NrSettings(),null); Ok(false,"install aurait dû refuser"); }catch(UserFacingException){ Ok(true,"install refusée tant que le 1-Click est là"); }
    var cr=Cleaner.Clean(g); Scanner.Analyze(g);
    Ok(cr.Errors.Count==0 && cr.Restored==1,"1-Click défait d'après son manifeste ("+cr.Moved+" en quarantaine, "+cr.Restored+" original remis)");
    Ok(Same(pristine,Tree(g.Root)),"jeu IDENTIQUE à avant le 1-Click (nvngx_dlss.dll d'origine revenu)");
    Ok(!g.OneClickInstalled && g.CleanPlan.Count==0 && g.Status==GameStatus.Compatible,"plus aucune trace, statut "+g.StatusText);
    var q1=Cleaner.LastQuarantine(g); Ok(q1!=null,"quarantaine trouvée");
    Ok(Cleaner.Undo(q1).Count==0 && Same(after1,Tree(g.Root)),"Annuler le nettoyage : état 1-Click restauré à l'identique");
    Cleaner.Clean(g); Ok(Same(pristine,Tree(g.Root)),"re-nettoyage OK");
    // B. installation OptiScaler manuelle (setup_windows.bat) + mods légitimes à garder
    g=Make(Path.Combine(work,"man1"),exe); d=g.ExeDir;
    File.WriteAllText(Path.Combine(d,"version.dll"),"CET loader"); File.WriteAllText(Path.Combine(d,"README.md"),"readme jeu");
    File.WriteAllText(Path.Combine(d,"dinput8.dll"),"ASI loader");
    pristine=Tree(g.Root);
    File.Copy(reshade,Path.Combine(d,"winmm.dll"));
    File.WriteAllText(Path.Combine(d,"OptiScaler.ini"),"x"); File.WriteAllText(Path.Combine(d,"nvngx.dll_dlssnr.dll"),"fwd"); File.WriteAllText(Path.Combine(d,"nvngx_dlssnr.dll"),"nr inconnue");
    File.WriteAllText(Path.Combine(d,"Remove_OptiScaler.bat"),"x"); File.WriteAllText(Path.Combine(d,"setup_windows.bat"),"x"); File.WriteAllText(Path.Combine(d,"SHA256SUMS.txt"),"x");
    Directory.CreateDirectory(Path.Combine(d,"OptiScaler","LUTs")); File.WriteAllText(Path.Combine(d,"OptiScaler","LUTs","a.cube"),"x");
    Directory.CreateDirectory(Path.Combine(d,"Licenses")); File.WriteAllText(Path.Combine(d,"Licenses","XeSS_LICENSE.txt"),"x");
    File.WriteAllText(Path.Combine(d,"OptiScaler.log"),"log");
    File.WriteAllText(Path.Combine(g.Root,"OptiScaler.ini"),"posé au mauvais endroit");
    Scanner.Analyze(g);
    Ok(g.CleanPlan.Count>=11,"install manuelle détectée ("+g.CleanPlan.Count+" éléments)");
    Ok(!g.CleanPlan.Any(i=>i.Path.EndsWith("version.dll")||i.Path.EndsWith("README.md")||i.Path.EndsWith("dinput8.dll")),"CET version.dll, ASI loader et README du jeu épargnés");
    cr=Cleaner.Clean(g);
    Ok(cr.Errors.Count==0 && Same(pristine,Tree(g.Root)),"nettoyage manuel : jeu identique à avant (mods légitimes intacts)");
    Ok(cr.NeedValidate,"vérification Steam proposée (une DLL NR a été retirée)");
    // C. notre install, puis le 1-Click par-dessus
    g=Make(Path.Combine(work,"mix1"),exe); d=g.ExeDir; pristine=Tree(g.Root);
    new Installer(g).Install(InstallMode.OptiScaler,new NrSettings(),null);
    bk=Path.Combine(d,"_DLSS5_Backup"); Directory.CreateDirectory(Path.Combine(bk,"originals","g")); File.Copy(Path.Combine(d,"dxgi.dll"),Path.Combine(bk,"originals","g","dxgi.dll"));
    File.Copy(reshade,Path.Combine(d,"dxgi.dll"),true); File.WriteAllText(Path.Combine(d,"renodx-dlss5.addon64"),"addon");
    File.WriteAllText(Path.Combine(bk,"manifest.json"),"{\"backupPrefix\":\"originals/g\",\"replaced\":[{\"rel\":\"dxgi.dll\"}],\"added\":\"renodx-dlss5.addon64\",\"addedDirs\":[],\"reshade\":{\"installedByUs\":false}}");
    Scanner.Analyze(g);
    Ok(g.Installed!=null && g.OneClickInstalled,"launcher + 1-Click par-dessus détectés");
    cr=Cleaner.Clean(g); Scanner.Analyze(g);
    Ok(cr.Errors.Count==0 && Same(pristine,Tree(g.Root)),"les deux défaits dans le bon ordre : jeu identique à l'origine");
    // ===== DÉTECTION UNITY / UNREAL + AIO =====
    Console.WriteLine("-- Unity / Unreal / AIO --");
    {
      string ur=Path.Combine(work,"unity1"); Directory.CreateDirectory(ur);
      File.Copy(exe,Path.Combine(ur,"LiS.exe")); File.WriteAllText(Path.Combine(ur,"UnityCrashHandler64.exe"),"x");
      File.WriteAllText(Path.Combine(ur,"UnityPlayer.dll"),"pas un vrai PE");
      Directory.CreateDirectory(Path.Combine(ur,"LiS_Data","Managed")); File.WriteAllText(Path.Combine(ur,"LiS_Data","app.info"),"Square Enix\nLife is Strange: Before the Storm\n");
      var ug=new Game{Name="LiS",Store="Steam",Root=ur}; Scanner.Analyze(ug);
      Ok(ug.Engine=="Unity" && ug.Apis.Count==1 && ug.Apis[0]=="DX11" && ug.ApiSource==null,"Unity sans log : DX11 par défaut (pas OpenGL) — "+ug.ApiText);
      Ok(Installer.Resolve(ug,InstallMode.Auto)==InstallMode.Feeder,"Unity sans upscaler : Auto → Feeder F5Low");
      Ok(Installer.ReShadeProxy(ug)=="dxgi.dll","Feeder ReShade forcé : proxy dxgi.dll (pas opengl32)");
      string local=Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
      string pl=Path.Combine(Path.GetDirectoryName(local.TrimEnd('/')),"LocalLow","Square Enix","Life is Strange_ Before the Storm"); Directory.CreateDirectory(pl);
      File.WriteAllText(Path.Combine(pl,"Player.log"),"[Subsystems] path D:/x/LiS_Data/UnitySubsystems\nDirect3D:\n    Version:  Direct3D 12.0 [level 12.1]\n");
      Scanner.Analyze(ug);
      Ok(ug.Apis.Count==1 && ug.Apis[0]=="DX12" && ug.ApiSource=="log du jeu","Player.log lu : DX12 confirmé");
      File.WriteAllText(Path.Combine(pl,"Player.log"),"Direct3D:\n    Version:  Direct3D 11.0 [level 11.1]\n"); Scanner.Analyze(ug);
      Ok(ug.Apis[0]=="DX11" && ug.ApiSource!=null,"Player.log lu : DX11 confirmé");
      // Unreal
      string er=Path.Combine(work,"ue1"); var eb=Path.Combine(er,"Pagoda","Binaries","Win64"); Directory.CreateDirectory(eb); Directory.CreateDirectory(Path.Combine(er,"Engine","Binaries"));
      File.Copy(exe,Path.Combine(eb,"PagodaSteam-Win64-Shipping.exe"));
      var eg=new Game{Name="DaD",Root=er}; Scanner.Analyze(eg);
      Ok(eg.Engine=="Unreal" && eg.Apis.Contains("DX12") && eg.Apis.Contains("DX11"),"Unreal sans log : DX12/DX11");
      string ul=Path.Combine(local,"Pagoda","Saved","Logs"); Directory.CreateDirectory(ul); File.WriteAllText(Path.Combine(ul,"Pagoda.log"),"LogRHI: Using Default RHI: D3D12\n");
      Scanner.Analyze(eg); Ok(eg.Apis.Count==1 && eg.Apis[0]=="DX12" && eg.ApiSource!=null,"log Unreal lu : DX12 confirmé");
      // AIO
      string ar=Path.Combine(work,"aio1"); Directory.CreateDirectory(ar); File.Copy(exe,Path.Combine(ar,"Game.exe"));
      File.WriteAllText(Path.Combine(ar,"README.txt"),"readme du jeu"); var apr=Tree(ar);
      File.WriteAllText(Path.Combine(ar,"README_FIRST.txt"),"DLSS5 ReShade AIO - 64-BIT INSTALL\n"); File.WriteAllText(Path.Combine(ar,"standalone-dlssnr.addon64"),"x");
      Directory.CreateDirectory(Path.Combine(ar,"licenses")); File.WriteAllText(Path.Combine(ar,"licenses","DLSS5-ReShade-AIO-LICENSE.txt"),"x");
      Directory.CreateDirectory(Path.Combine(ar,"_1Click_DLSS5_Backup"));
      Directory.CreateDirectory(Path.Combine(ar,"reshade-shaders","Shaders")); File.WriteAllText(Path.Combine(ar,"reshade-shaders","Shaders","DLSS5_AIO_Feed.fx"),"x");
      var ag=new Game{Name="AIO",Root=ar}; Scanner.Analyze(ag);
      Ok(ag.CleanPlan.Count==5 && ag.Status==GameStatus.AutreMod,"traces AIO + _1Click_DLSS5_Backup détectées ("+ag.CleanPlan.Count+")");
      var acr=Cleaner.Clean(ag); Ok(acr.Errors.Count==0 && Same(apr,Tree(ar)),"AIO nettoyé : jeu identique (README du jeu gardé)");
    }
    // ===== FLUIDITÉ / PERSONNALISATION =====
    Console.WriteLine("-- Fluidite / personnalisation --");
    {
      var tplF=Ini.ReadFile(Path.Combine(AppPaths.OptiDir,"OptiScaler.ini"));
      var s0=new NrSettings(); var i0=s0.BuildOptiIni(tplF,"/jeu");
      Ok(Ini.Get(i0,"FrameGen","Enabled")=="false" && Ini.Get(i0,"FrameGen","FGOutput")=="nofg" && Ini.Get(i0,"Framerate","FramerateLimit")=="auto" && Ini.Get(i0,"DlssNr","LutFile")=="","défauts : pas de FG, pas de limite, pas de LUT");
      var s1=new NrSettings(); s1.FrameGen=1; s1.FpsLimit=118; s1.ReflexBoost=true; s1.SpatialCompression=true; s1.Lut="AgX-Blender.cube"; s1.LutStrength=0.6m;
      s1.SkinProtection=true; s1.SkinColour=0.3m; s1.Preset=1; s1.Style=2; s1.Intensity=1.3m; s1.WorkingScale=0.8m;
      var i1=s1.BuildOptiIni(tplF,"/jeu/bin");
      Ok(Ini.Get(i1,"FrameGen","Enabled")=="true" && Ini.Get(i1,"FrameGen","FGInput")=="upscaler" && Ini.Get(i1,"FrameGen","FGOutput")=="xefg","XeFG : Enabled + FGInput=upscaler + FGOutput=xefg");
      Ok(Ini.Get(i1,"Framerate","FramerateLimit")=="118" && Ini.Get(i1,"DlssNr","NativeLowLatencyBoost")=="true" && Ini.Get(i1,"DlssNr","SpatialCompression")=="true","limite FPS 118, Reflex Boost, compression des bords");
      Ok(Ini.Get(i1,"DlssNr","LutFile")==Path.Combine("/jeu/bin","OptiScaler","LUTs","AgX-Blender.cube") && Ini.Get(i1,"DlssNr","LutStrength")=="0.6","LUT en chemin absolu + force");
      Ok(Ini.Get(i1,"DlssNr","SkinProtection")=="true" && Ini.Get(i1,"DlssNr","SkinColour")=="0.3" && Ini.Get(i1,"DlssNr","EnvironmentDetail")=="1","protection peau / décor");
      var sc=new NrSettings(); sc.SpatialCompression=true; sc.DetailReuse=true;
      Ok(Ini.Get(sc.BuildOptiIni(tplF,"/j"),"DlssNr","SpatialCompression")=="false","compression coupée quand Réutilisation du détail est active");
      var s3=new NrSettings(); s3.FrameGen=2; var gx=new Game{Name="x",Apis=new List<string>{"DX12"},Root="/j",ExePath="/j/g.exe"};
      var i3=s3.BuildFeederIni(tplF,gx);
      Ok(Ini.Get(i3,"DlssNr","NativeUpscaler")=="true" && Ini.Get(i3,"DlssNr","FinishedPicture")=="false" && Ini.Get(i3,"FrameGen","FGOutput")=="fsrfg","Feeder + FSR FG : passe par l'upscaler virtuel");
      var imp=new NrSettings(); imp.ImportOpti(i1,false);
      Ok(imp.FrameGen==1 && imp.FpsLimit==118 && imp.ReflexBoost && imp.Lut=="AgX-Blender.cube" && imp.LutStrength==0.6m && imp.Preset==1 && imp.Style==2 && imp.Intensity==1.3m && imp.WorkingScale==0.8m && imp.SkinProtection && imp.SkinColour==0.3m,"Importer depuis le jeu : aller-retour exact");
      var auto=new NrSettings(); auto.ImportOpti(tplF,true);
      Ok(auto.Preset==-1 && auto.FWorkingScale==1.0m && auto.FrameGen==0,"import d'un INI « auto » : valeurs par défaut d'OptiScaler (échelle 1,0)");
      var rimp=new NrSettings(); rimp.ImportReShade("[RenoDX.DLSS5]\nNRPreset=1\nNRLocalTone=1.6\nNRUICorrection=0\n");
      Ok(rimp.RPreset==1 && rimp.RLocalTone==1.6m && !rimp.RUICorrection,"import ReShade.ini");
      var pd=Path.Combine(work,"perf1"); Directory.CreateDirectory(pd);
      File.WriteAllText(Path.Combine(pd,"OptiScaler.log"),
        "[18:09:48.909807] [I] DlssNr_Dx12::Dispatch DLSS-NR running after SR: target 2560x1440, model input 2560x1440 (main network 1280x720), guides 2560x1440 (preset 1, intensity 1.4, style 1, build epoch 49188)\n"+
        "[18:09:55.464729] [I] DlssNr_Dx12::Dispatch DLSS-NR elapsed: 12.14 ms total, 11.80 ms model, 0.34 ms surrounding work (3%; intervals may include other GPU work)\n"+
        "[18:10:05.836036] [I] DlssNr_Dx12::Dispatch DLSS-NR elapsed: 12.12 ms total, 11.77 ms model, 0.35 ms surrounding work\n");
      var pi=Perf.Read(pd);
      Ok(pi.Samples==2 && Math.Abs(pi.AvgMs-12.13)<0.01 && pi.ModelInput=="2560x1440" && pi.Preset=="1","lecture du coût NR réel (12,13 ms, preset A, 2560x1440)");
      Ok(pi.Advice().Contains("Preset B"),"conseil FPS proposé : "+pi.Summary());
      var old=new NrSettings(); old.LoadFrom(new Dictionary<string,string>{{"set.FrameGenOff","True"},{"set.Preset","2"}});
      Ok(old.FrameGen==0 && old.Preset==2,"ancienne config relue sans problème");
    }
    // ===== AMD / STABILITÉ / COPIE AMI =====
    Console.WriteLine("-- AMD / stabilite / copie --");
    {
      // profil sûr RR
      var rg=Make(Path.Combine(work,"rr1"),exe);
      File.WriteAllText(Path.Combine(rg.ExeDir,"nvngx_dlssd.dll"),"rr"); Scanner.Analyze(rg);
      Ok(Stability.HasRr(rg),"Ray Reconstruction détectée");
      var sa=new NrSettings(); sa.Preset=1; sa.WorkingScale=1.0m; sa.Passes=2;
      var before2=Tree(rg.Root);
      new Installer(rg).Install(InstallMode.OptiScaler,sa,null);
      var ri2=Ini.ReadFile(Path.Combine(rg.ExeDir,"OptiScaler.ini"));
      Ok(Ini.Get(ri2,"DlssNr","Preset")=="2" && Ini.Get(ri2,"DlssNr","WorkingScale")=="0.6" && Ini.Get(ri2,"DlssNr","Passes")=="1","profil sûr RR : Preset A/1.0/2 passes → B/0.6/1");
      Ok(sa.Preset==1,"le profil du jeu lui-même n'est pas modifié");
      Scanner.Analyze(rg);
      File.WriteAllText(Path.Combine(rg.ExeDir,"OptiScaler.log"),
        "[18:43:05.073198] [I] DlssNr_Dx12::Dispatch DLSS-NR model feature created from x\n[18:43:05.073205] [I] DlssNr_Dx12::Dispatch DLSS-NR running after RR+SR: target 2560x1440, model input 2560x1440 (main network 1280x720), guides 1706x960 (preset 1, intensity 0.9, style 1, build epoch 14301)\n"+
        "[18:43:05.177206] [E] Util::GetDeviceRemovedReason Device removed reason: DXGI_ERROR_DEVICE_HUNG\n");
      var dg=Report.Diagnose(rg);
      Ok(dg.Any(x=>x.Contains("DXGI_ERROR_DEVICE_HUNG")) && dg.Any(x=>x.Contains("Ray Reconstruction")),"diagnostic : crash DEVICE_HUNG + RR expliqués");
      var sb2=new NrSettings(); sb2.RrSafe=false; sb2.Preset=1; new Installer(rg).ApplySettings(sb2);
      Ok(Ini.Get(Ini.ReadFile(Path.Combine(rg.ExeDir,"OptiScaler.ini")),"DlssNr","Preset")=="1","profil sûr décoché : Preset A respecté");
      Ok(new Installer(rg).Reset().Count==0 && Same(before2,Tree(rg.Root)),"reset (logs + dump archivés) identique");
      // AMD
      Gpu.Vendor="AMD";
      var ag2=Make(Path.Combine(work,"amd1"),exe); var ab=Tree(ag2.Root);
      new Installer(ag2).Install(InstallMode.OptiScaler,Presets.Get(8),null);
      var ad=ag2.ExeDir; var ai=Ini.ReadFile(Path.Combine(ad,"OptiScaler.ini"));
      Ok(File.ReadAllText(Path.Combine(ad,"nvngx.dll_dlssnr.dll"))=="port-amd" && File.Exists(Path.Combine(ad,"nr_port.ini")) && File.ReadAllText(Path.Combine(ad,"nvngx_dlssnr.dll"))=="nr-310.9-origine","AMD : port vendor-neutral + nr_port.ini + 310.9 d'origine installés");
      Ok(Ini.Get(ai,"Upscalers","Dx12Upscaler")=="auto" && Ini.Get(ai,"Upscalers","Dx11Upscaler")=="ffx_12" && Ini.Get(ai,"Upscalers","VulkanUpscaler")=="ffx_12","AMD : FSR4/ffx en DX12, ffx_12 en DX11/Vulkan (pas de DLSS)");
      Ok(Ini.Get(ai,"DlssNr","WorkingScale")=="0.5" && Ini.Get(ai,"DlssNr","Preset")=="2","AMD : préréglage de départ (0,5, Preset B)");
      Scanner.Analyze(ag2); Ok(ag2.Installed.GpuMode.StartsWith("AMD"),"manifeste note le mode AMD");
      File.WriteAllText(Path.Combine(ad,"nr_port.log"),"backend: vendor-neutral\nnetwork: small (310.9 CC_SMALL, 31 blocks)\n");
      Ok(Report.Diagnose(ag2).Any(x=>x.Contains("petit modèle")),"diagnostic AMD : nr_port.log « network: small » reconnu");
      Ok(new Installer(ag2).Reset().Count==0 && Same(ab,Tree(ag2.Root)),"reset AMD identique (nr_port.log nettoyé)");
      Scanner.Analyze(ag2);
      try{ new Installer(ag2).Install(InstallMode.RenoDX,new NrSettings(),null); Ok(false,"RenoDX aurait dû être refusé sur AMD"); }catch(UserFacingException){ Ok(Same(ab,Tree(ag2.Root)),"RenoDX refusé proprement sur AMD"); }
      Gpu.Vendor="NVIDIA";
      // copie pour un ami
      Directory.CreateDirectory(Path.Combine(AppPaths.Root,"profils")); File.WriteAllText(Path.Combine(AppPaths.Root,"profils","x.ini"),"perso");
      var fc=Path.Combine(work,"ami","DLSS5-Launcher"); FriendCopy.Make(fc);
      Ok(File.Exists(Path.Combine(fc,"payload","amd","nvngx.dll_dlssnr.dll")) && File.Exists(Path.Combine(fc,"POUR-TON-AMI.txt")) && !Directory.Exists(Path.Combine(fc,"profils")) && !Directory.Exists(Path.Combine(fc,"journaux")) && !File.Exists(Path.Combine(fc,"launcher-config.ini")),"copie pour un ami : payload + mode AMD, rien de personnel");
    }
    // ===== RTX 50 =====
    Console.WriteLine("-- RTX 50 --");
    {
      Gpu.Vendor="NVIDIA"; Gpu.Series=50;
      var r5=Make(Path.Combine(work,"rtx50"),exe); var b5=Tree(r5.Root);
      new Installer(r5).Install(InstallMode.OptiScaler,new NrSettings(),null); Scanner.Analyze(r5);
      Ok(File.ReadAllText(Path.Combine(r5.ExeDir,"nvngx_dlssnr.dll"))=="nr-310.9-origine" && Hash.Sha(Path.Combine(r5.ExeDir,"nvngx.dll_dlssnr.dll"))==Hash.Sha(Path.Combine(AppPaths.OptiDir,"nvngx.dll_dlssnr.dll")),"RTX 50 : 310.9.16 d'origine + forwarder NVIDIA normal");
      Ok(r5.Installed.GpuMode.Contains("RTX 50") && r5.Installed.GpuMode.Contains("d'origine"),"manifeste : "+r5.Installed.GpuMode);
      Ok(new Installer(r5).Reset().Count==0 && Same(b5,Tree(r5.Root)),"reset RTX 50 identique");
      Gpu.Series=40; Ok(Gpu.NrDllPath==AppPaths.NrDll,"RTX 40 : ta build OpenNR");
      Gpu.Series=50; Gpu.ForceNr="openNR"; Ok(Gpu.NrDllPath==AppPaths.NrDll,"dllNr=openNR force OpenNR sur RTX 50");
      Gpu.ForceNr=null; Gpu.Series=0;
    }
    // ===== CRASH SPECIES UNKNOWN =====
    Console.WriteLine("-- crash Unreal / profondeur --");
    {
      string su=Path.Combine(work,"su1"); var sb3=Path.Combine(su,"SpeciesUnknown","Binaries","Win64"); Directory.CreateDirectory(sb3);
      File.Copy(exe,Path.Combine(sb3,"SpeciesUnknown-Win64-Shipping.exe"));
      var sg=new Game{Name="Species: Unknown",Root=su}; Scanner.Analyze(sg);
      new Installer(sg).Install(InstallMode.Feeder,new NrSettings(),null); Scanner.Analyze(sg);
      var lg=new System.Text.StringBuilder();
      for(int i=0;i<30;i++){ lg.Append("[23:33:45.197652] [I] GenericDepthDx12::Install::<lambda_1>::operator () Depth finder: frame 51"+i+", picture 2560x1440, 6 depth buffer(s) in use, none qualifies\n");
        lg.Append("[23:33:45.1] [I] Depth finder:   draws that waited for the finder's lock since the last report: 166633 of 767769, 379.52 ms in all, longest 2076.2 us\n");
        lg.Append("[x] [I] native::NativeDriver::LogDiagnostics Native motion frames: of 512100 presents, picture ready 512100, flow valid 512099 (matched with depth 0), tr\n"); }
      lg.Append("[23:34:07.127322] [C] Unhandled exception 8000 at 7FFAA46241CA, writing D:\\x\\OptiScaler_crash.dmp\n");
      File.WriteAllText(Path.Combine(sg.ExeDir,"OptiScaler.log"),lg.ToString());
      string local=Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
      string ul2=Path.Combine(local,"SpeciesUnknown","Saved","Logs"); Directory.CreateDirectory(ul2);
      File.WriteAllText(Path.Combine(ul2,"SpeciesUnknown.log"),"LogInit: ok\n[2026.10.24-23.34.06:900][  0]LogD3D12RHI: Error: GPU crash detected: DXGI_ERROR_DEVICE_REMOVED\n[2026.10.24-23.34.06:901]LogD3D12RHI: Error: Device removed reason: DXGI_ERROR_DEVICE_HUNG\n");
      var dg2=Report.Diagnose(sg);
      foreach(var x in dg2) Console.WriteLine("     · "+x);
      Ok(dg2.Any(x=>x.Contains("0x8000") && x.Contains("Unreal")),"exception 0x8000 Unreal reconnue");
      Ok(dg2.Any(x=>x.Contains("JAMAIS trouvé") && x.Contains("2,1 ms")),"profondeur jamais trouvée + blocages 2,1 ms signalés");
      Ok(dg2.Any(x=>x.Contains("DEVICE_HUNG")),"erreur du log Unreal remontée");
      var rp=File.ReadAllText(Report.Write(sg,"RTX"));
      Ok(rp.Contains("Log du moteur du jeu (Unreal)") && rp.Contains("GPU crash detected"),"fiche : section log du moteur");
      new Installer(sg).Reset();
      Ok(Directory.GetFiles(AppPaths.Journaux,"moteur_SpeciesUnknown.log",SearchOption.AllDirectories).Length>0,"log Unreal archivé au reset (copie)");
      Ok(File.Exists(Path.Combine(ul2,"SpeciesUnknown.log")),"log Unreal d'origine laissé en place");
      Directory.Delete(Path.Combine(local,"SpeciesUnknown"),true);
    }
    // ===== FICHE APRÈS RESET =====
    Console.WriteLine("-- fiche apres reset --");
    {
      string ar2=Path.Combine(work,"arch1"); var ab2=Path.Combine(ar2,"Jeu","Binaries","Win64"); Directory.CreateDirectory(ab2);
      File.Copy(exe,Path.Combine(ab2,"Jeu-Win64-Shipping.exe"));
      var gg=new Game{Name="Jeu Archive "+Guid.NewGuid().ToString("N").Substring(0,6),Root=ar2}; Scanner.Analyze(gg);
      new Installer(gg).Install(InstallMode.Feeder,new NrSettings(),null); Scanner.Analyze(gg);
      File.WriteAllText(Path.Combine(gg.ExeDir,"OptiScaler.log"),"[x] [I] DlssNr_Dx12::Dispatch DLSS-NR elapsed: 6.00 ms total, 5 ms model\n[x] [I] Native motion frames: of 10 presents\n[23:34:07.1] [C] Unhandled exception 8000 at 7FF, writing x\n");
      File.WriteAllText(Path.Combine(gg.ExeDir,"OptiScaler_crash.dmp"),"dmp");
      new Installer(gg).Reset(); Scanner.Analyze(gg);
      var la=Report.LatestArchive(gg);
      Ok(la!=null && File.Exists(Path.Combine(la,"manifest.tsv")) && File.Exists(Path.Combine(la,"OptiScaler_crash.dmp")),"reset : journal archivé avec le manifeste et le dump");
      var fr=File.ReadAllText(Report.Write(gg,"RTX"));
      Ok(fr.Contains("Source : journal archivé") && fr.Contains("Feeder F5Low") && fr.Contains("NativeMotion=true"),"fiche APRÈS reset : méthode et réglages retrouvés dans le journal");
      Ok(fr.Contains("NR ≈ 6") && fr.Contains("0x8000") && fr.Contains("dump de crash"),"fiche APRÈS reset : coût NR, crash et dump retrouvés");
    }
    // 5. 32-bit refusé
    Console.WriteLine(fails==0?"\nTOUS LES TESTS PASSENT":"\n"+fails+" ÉCHEC(S)");
  }
}
