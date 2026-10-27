using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace GateControl {
 public static class Tests {
  static readonly List<string> results=new List<string>();
  static void Check(string name,bool condition){if(!condition)throw new Exception("FAIL: "+name);results.Add("PASS: "+name);}
  static void Throws(string name,Action action){try{action();}catch{results.Add("PASS: "+name);return;}throw new Exception("FAIL: "+name+" did not reject invalid input");}
  public static int Run(string report){try{string root=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(report)),"unit-fixture-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(Path.Combine(root,"AbioticFactor","Content","Paks"));Directory.CreateDirectory(Path.GetDirectoryName(Paths.Exe(root)));File.WriteAllBytes(Paths.Exe(root),new byte[]{0});
   var options=SandboxOption.All();Check("48 distinct documented sandbox keys",options.Count==48&&options.Select(x=>x.Key).Distinct().Count()==48);foreach(var o in options)Check("valid official default "+o.Key,o.Normalize(o.Default)!="");
   Throws("sandbox minimum enforced",()=>options.First(x=>x.Key=="EnemyHealthMultiplier").Normalize("0.1"));Throws("sandbox maximum enforced",()=>options.First(x=>x.Key=="ItemStackSizeMultiplier").Normalize("31"));Throws("enum validity",()=>options.First(x=>x.Key=="GameDifficulty").Normalize("4"));Throws("reject NaN",()=>options.First(x=>x.Kind=="number").Normalize("NaN"));
   var p=new Profile{Root=root,Name="测试房间 & 同事",World="World A",ServerPassword="secret & test",AdminPassword="admin-test",Ip="127.0.0.1"};Launch.Validate(p);string args=Launch.Arguments(p,false);Check("quoted unicode room name",args.Contains("-SteamServerName=\"测试房间 & 同事\""));Check("documented binding argument",args.Contains("-MultiHome=127.0.0.1"));Check("no unsupported async/n steam arguments",!args.Contains("NoAsync")&&!args.Contains("NOSTEAM"));Check("preview masks passwords",!Launch.Arguments(p,true).Contains("secret & test")&&!Launch.Arguments(p,true).Contains("admin-test"));
   Check("log password redaction",!Launch.Redact("-ServerPassword=\"secret & test\" -AdminPassword=admin-test",p).Contains("secret"));Check("DPAPI roundtrip",Secrets.Decrypt(Secrets.Encrypt("密码 123 &"))=="密码 123 &");string profilePath=Path.Combine(root,"profile.xml");Files.Save(profilePath,p);Check("encrypted persistence",!File.ReadAllText(profilePath).Contains("secret &amp; test")&&!File.ReadAllText(profilePath).Contains("admin-test"));Check("profile roundtrip",Files.Load<Profile>(profilePath).ServerPassword=="secret & test");
   Throws("world traversal rejected",()=>Paths.ValidateWorld("../outside"));Throws("reserved device rejected",()=>Paths.ValidateWorld("CON.txt"));Throws("trailing dot rejected",()=>Paths.ValidateWorld("World."));p.Players=25;Throws("max players limit",()=>Launch.Validate(p));p.Players=6;p.QueryPort=p.Port;Throws("duplicate ports rejected",()=>Launch.Validate(p));p.QueryPort=27015;p.Ip="203.0.113.50";Throws("nonlocal binding rejected",()=>Launch.Validate(p));p.Ip="";p.Name="a\" -NOSTEAM";Throws("quote injection rejected",()=>Launch.Validate(p));p.Name="test";
   string ini="; keep comment\r\n[Other]\r\nKey=42\r\n[SandboxSettings]\r\nGameDifficulty=1\r\nFutureRule=abc\r\nGameDifficulty=2\r\n";string merged=Ini.Merge(ini,"SandboxSettings",new[]{new SettingValue("GameDifficulty","3")});Check("preserve unknown sections and keys",merged.Contains("Key=42")&&merged.Contains("FutureRule=abc")&&merged.Contains("; keep comment"));Check("replace duplicate edited keys",merged.Split(new[]{"GameDifficulty="},StringSplitOptions.None).Length==2);Check("INI idempotence",Ini.Merge(merged,"SandboxSettings",new[]{new SettingValue("GameDifficulty","3")})==merged);
   string admins=Ini.ReplaceModerators("[Moderators]\nModerator=old\nSomething=keep\n[BannedPlayers]\nBannedPlayer=76561198000000000\n",new[]{"76561198000000001","76561198000000002"});Check("admin preserve ban list",admins.Contains("BannedPlayer=76561198000000000")&&admins.Contains("Something=keep")&&!admins.Contains("Moderator=old"));Check("admin duplicate keys supported",admins.Contains("Moderator=76561198000000001")&&admins.Contains("Moderator=76561198000000002"));
   p.Sandbox=options.Select(x=>new SettingValue(x.Key,x.Default)).ToList();p.SandboxContext=Paths.Context(p);p.ManageSandbox=true;
   Ini.SaveSandbox(p);string first=File.ReadAllText(Paths.Sandbox(p));
   Check("saving new-world rules never creates world directory",!Directory.Exists(Paths.World(p)));
   Check("sandbox override lives outside SaveGames",File.Exists(Paths.ManagedSandbox(p))&&!Paths.ManagedSandbox(p).Contains("SaveGames"));
   Check("documented relative SandboxIniPath is generated",Launch.Arguments(p,false).Contains("-SandboxIniPath=\"Config/WindowsServer/GateControl/"));
   string identity=Paths.Context(p);Check("sandbox identity remains compatible with 1.0",identity==Path.GetFullPath(Paths.WorldSandbox(p)).ToLowerInvariant());
   p.ManageSandbox=false;Check("explicitly saved rules remain active",Launch.Arguments(p,false).Contains("-SandboxIniPath="));p.ManageSandbox=true;
   p.Sandbox.First(x=>x.Key=="GameDifficulty").Value="2";Ini.SaveSandbox(p);
   string cfgFolder=Path.GetDirectoryName(Paths.ManagedSandbox(p));Check("INI automatic backup",Directory.GetFiles(cfgFolder,"*.bak").Length==1);Check("INI backup keeps original",File.ReadAllText(Directory.GetFiles(cfgFolder,"*.bak")[0])==first);
   Directory.CreateDirectory(Paths.World(p));File.WriteAllText(Paths.WorldSandbox(p),"[SandboxSettings]\nGameDifficulty=1\n");
   Throws("legacy config-only world is blocked before launch",()=>Paths.CheckWorldBeforeStart(p));
   Check("preflight preserves legacy world files",File.Exists(Paths.WorldSandbox(p)));
   File.WriteAllText(Path.Combine(Paths.World(p),"WorldSave.sav"),"unit-test-fixture");Paths.CheckWorldBeforeStart(p);
   var health=new ServerHealth();health.Observe("Load map complete /Game/Maps/ServerEntry\nMatch State Changed from EnteringMap to WaitingToStart");Check("ServerEntry is never treated as playable readiness",!health.Ready);
   health.Observe("World save Integrity State: Corrupt\nLoad map complete /Game/Maps/Facility");Check("corruption overrides map-loaded text",health.SaveCorrupt&&!health.Ready);
   health.Observe("Load map complete /Game/Maps/Facility");Check("failure remains sticky for the process",!health.Ready);
   health.Reset();health.Observe("LogGlobalStatus: UEngine::LoadMap Load map complete /Game/Maps/Facility\n");Check("Facility map completion establishes local readiness",health.Ready&&!health.SaveCorrupt);
   var copied=p.Clone();copied.World="已有世界";Directory.CreateDirectory(Paths.World(copied));File.WriteAllText(Paths.WorldSandbox(copied),"[SandboxSettings]\nFutureRule=keep\nGameDifficulty=1\n");
   string originalWorldIni=File.ReadAllText(Paths.WorldSandbox(copied));Ini.SaveSandbox(copied);
   Check("first external config preserves unknown world rules",File.ReadAllText(Paths.ManagedSandbox(copied)).Contains("FutureRule=keep"));
   Check("external config creation leaves original INI intact",File.ReadAllText(Paths.WorldSandbox(copied))==originalWorldIni);
   p.World="Different";Throws("sandbox cannot silently follow a different world",()=>Launch.Validate(p));
   var task=new Countdown();task.Set(1,true,true);Check("countdown not early",!task.TakeDue(DateTime.UtcNow));Check("countdown fires once",task.TakeDue(DateTime.UtcNow.AddMinutes(2))&&!task.TakeDue(DateTime.UtcNow.AddMinutes(3)));task.Set(1,false,false);task.Cancel();Check("cancel removes schedule",!task.DueUtc.HasValue&&!task.Repeat);Throws("invalid repeating shutdown rejected",()=>task.Set(1,false,true));Throws("invalid delay rejected",()=>task.Set(Double.NaN,false,false));
   string source=Path.Combine(root,"world source");Directory.CreateDirectory(source);File.WriteAllText(Path.Combine(source,"World.sav"),"fixture");string dest=Path.Combine(root,"world copied");Files.CopyTree(source,dest);Check("migration copies without deleting source",File.ReadAllText(Path.Combine(dest,"World.sav"))=="fixture"&&File.Exists(Path.Combine(source,"World.sav")));Throws("copy refuses overwrite",()=>Files.CopyTree(source,dest));Check("valid server directory detection",Paths.NormalizeRoot(Path.GetDirectoryName(Paths.Exe(root)))==root);
   results.Add("TOTAL PASS: "+results.Count);File.WriteAllLines(report,results,Encoding.UTF8);return 0;
  }catch(Exception e){results.Add(e.ToString());File.WriteAllLines(report,results,Encoding.UTF8);return 1;}}
  public static int Integration(string root,string data,string report){
   var lines=new List<string>();var runner=new ServerRunner(data);
   try{
    Directory.CreateDirectory(data);
    var p=new Profile{Root=Paths.NormalizeRoot(root),Name="GATE Local Verification",World="卡斯卡德_验证_"+Guid.NewGuid().ToString("N").Substring(0,8),Ip="127.0.0.1",Port=17777,QueryPort=27025,Lan=true,Platform="PC",ServerPassword="local-verification-only",Console=false,ManageSandbox=true};
    p.SandboxContext=Paths.Context(p);p.Sandbox=SandboxOption.All().Select(x=>new SettingValue(x.Key,x.Default)).ToList();p.Sandbox.First(x=>x.Key=="GameDifficulty").Value="2";
    Files.Save(Path.Combine(data,"profile.xml"),p);Ini.SaveSandbox(p);
    if(Directory.Exists(Paths.World(p)))throw new Exception("Regression: rule save created world directory before game launch");
    lines.Add("PASS: pre-launch sandbox save did not create world folder");
    lines.Add("World: "+p.World);
    for(int cycle=1;cycle<=2;cycle++){
     runner.Start(p);lines.Add("Cycle "+cycle+": started PID "+runner.Process.Id);
     for(int t=0;t<90;t++){
      Thread.Sleep(1000);if(t==2)runner.HideConsole();
      if(!runner.Alive)throw new Exception("Server exited before Facility readiness, code="+runner.Process.ExitCode);
      try{runner.RefreshHealth();}catch(IOException){}
      if(runner.Health.SaveCorrupt)throw new Exception("Save corruption detected; ServerEntry is not a successful start");
      if(runner.Health.Ready)break;
     }
     if(!runner.Health.Ready)throw new Exception("No Facility map completion within 90 seconds");
     lines.Add("Cycle "+cycle+": actual /Game/Maps/Facility completion confirmed, no save-corruption marker");
     Thread.Sleep(5000);runner.RefreshHealth();
     if(runner.Health.SaveCorrupt)throw new Exception("Late save-corruption marker");
     if(!runner.StopAsync().GetAwaiter().GetResult())throw new Exception("Normal stop did not complete: "+runner.LastStopError);
     lines.Add("Cycle "+cycle+": process exited after normal close signal, code="+runner.Process.ExitCode);runner.Clear();
     string logPath=Path.Combine(Paths.Saved(p.Root),"Logs","AbioticFactor.log");
     string completedLog=File.ReadAllText(logPath);
     if(!completedLog.Contains("Key GameDifficulty sandbox option modified value: 2"))throw new Exception("Game did not confirm difficulty override");
     lines.Add("Cycle "+cycle+": game itself confirmed GameDifficulty modified value 2");
     File.Copy(logPath,Path.Combine(Path.GetDirectoryName(report),"integration-v1.0.1-cycle-"+cycle+".log"),true);
    }
    lines.Add("External sandbox GameDifficulty="+Ini.Read(File.ReadAllText(Paths.ManagedSandbox(p)),"SandboxSettings")["GameDifficulty"]);
    int saves=Directory.Exists(Paths.World(p))?Directory.GetFiles(Paths.World(p),"*.sav",SearchOption.AllDirectories).Length:0;
    lines.Add("World .sav files generated by game: "+saves);
    lines.Add("PASS: new Chinese-named world reached Facility twice, with sandbox override and no corruption");
    lines.Add("Scope: local isolated server; no joined player, gameplay persistence, or public connectivity claim.");
    File.WriteAllLines(report,lines,Encoding.UTF8);return 0;
   }catch(Exception e){lines.Add(e.ToString());if(runner.Alive){try{bool stopped=runner.StopAsync().GetAwaiter().GetResult();lines.Add("Cleanup normal stop: "+stopped);if(!stopped){runner.Process.Kill();lines.Add("Forced cleanup of isolated verification process only.");}}catch(Exception cleanup){lines.Add("Cleanup failure: "+cleanup.Message);}}File.WriteAllLines(report,lines,Encoding.UTF8);return 1;}
  }
 }
}
