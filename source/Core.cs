using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Win32;

namespace GateControl {
 public class SettingValue { public string Key; public string Value; public SettingValue(){} public SettingValue(string k,string v){Key=k;Value=v;} }
 public class Profile {
  public string Root="", Name="GATE · 同事生存计划", World="Cascade", Ip="", Platform="";
  public int Players=6, Port=7777, QueryPort=27015;
  public bool Lan=false, LocalIps=false, Perf=true, Console=false, ManageSandbox=false;
  public string ServerSecret="", AdminSecret="", SandboxContext="";
  public List<SettingValue> Sandbox=new List<SettingValue>();
  [XmlIgnore] public string ServerPassword { get {return Secrets.Decrypt(ServerSecret);} set {ServerSecret=Secrets.Encrypt(value);} }
  [XmlIgnore] public string AdminPassword { get {return Secrets.Decrypt(AdminSecret);} set {AdminSecret=Secrets.Encrypt(value);} }
  public Profile Clone(){var s=new XmlSerializer(typeof(Profile));using(var m=new MemoryStream()){s.Serialize(m,this);m.Position=0;return (Profile)s.Deserialize(m);}}
 }
 public class ActiveRecord {public int Pid;public long StartTicks;public string Exe;public string Root;}
 public static class Secrets {
  public static string Encrypt(string s){return String.IsNullOrEmpty(s)?"":Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(s),null,DataProtectionScope.CurrentUser));}
  public static string Decrypt(string s){if(String.IsNullOrEmpty(s))return "";try{return Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(s),null,DataProtectionScope.CurrentUser));}catch{throw new InvalidDataException("无法解密保存的密码。请使用原 Windows 用户运行，或重新设置密码。");}}
 }
 public static class Files {
  public static T Load<T>(string path) where T:new(){if(!File.Exists(path))return new T();var settings=new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null};using(var r=XmlReader.Create(path,settings))return (T)new XmlSerializer(typeof(T)).Deserialize(r);}
  public static void Save<T>(string path,T value){var s=new XmlSerializer(typeof(T));using(var m=new MemoryStream()){s.Serialize(m,value);Atomic(path,m.ToArray(),false);}}
  public static void Atomic(string path,byte[] bytes,bool backup){Directory.CreateDirectory(Path.GetDirectoryName(path));string tmp=path+"."+Guid.NewGuid().ToString("N")+".tmp";try{File.WriteAllBytes(tmp,bytes);if(File.Exists(path)){string bak=backup?path+"."+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff")+"."+Guid.NewGuid().ToString("N").Substring(0,6)+".bak":null;File.Replace(tmp,path,bak,true);}else File.Move(tmp,path);}finally{if(File.Exists(tmp))File.Delete(tmp);}}
  public static string Resource(string name){using(var s=Assembly.GetExecutingAssembly().GetManifestResourceStream(name))using(var r=new StreamReader(s,Encoding.UTF8))return r.ReadToEnd();}
  public static void EnsureNoLinks(string path){string p=Path.GetFullPath(path);while(!String.IsNullOrEmpty(p)){if((Directory.Exists(p)||File.Exists(p))&&(File.GetAttributes(p)&FileAttributes.ReparsePoint)!=0)throw new IOException("为避免写入链接指向的其他位置，请选择普通目录："+p);p=Path.GetDirectoryName(p);}}
  public static void CopyTree(string source,string dest){EnsureNoLinks(source);Directory.CreateDirectory(dest);foreach(string f in Directory.GetFiles(source)){EnsureNoLinks(f);File.Copy(f,Path.Combine(dest,Path.GetFileName(f)),false);}foreach(string d in Directory.GetDirectories(source))CopyTree(d,Path.Combine(dest,Path.GetFileName(d)));}
  public static void ValidateTree(string source){EnsureNoLinks(source);foreach(string f in Directory.GetFiles(source))EnsureNoLinks(f);foreach(string d in Directory.GetDirectories(source))ValidateTree(d);}
 }
 public class SandboxOption {
  public string Key,Title,Group,Default,Min,Max,Kind,Hint;
  public string Normalize(string value){
   if(Kind=="bool"){bool b;if(!Boolean.TryParse(value,out b))throw new ArgumentException(Title+"：请选择开启或关闭。");return b?"True":"False";}
   if(Kind.Contains("=")){if(!Kind.Split('|').Any(x=>x.Split('=')[0]==value))throw new ArgumentException(Title+"：不在官方枚举值中。");return value;}
   decimal n;if(!Decimal.TryParse(value,NumberStyles.AllowLeadingSign|NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out n))throw new ArgumentException(Title+"：请输入数字，小数点使用英文句点。");
   if(Kind=="int"&&Decimal.Truncate(n)!=n)throw new ArgumentException(Title+"：请输入整数。");
   if(Kind=="int"&&(n<Int32.MinValue||n>Int32.MaxValue))throw new ArgumentException(Title+"：超出整数范围。");
   if(Min!=""&&n<Decimal.Parse(Min,CultureInfo.InvariantCulture)||Max!=""&&n>Decimal.Parse(Max,CultureInfo.InvariantCulture))throw new ArgumentException(Title+"：官方范围为 "+Min+"–"+Max+"。");
   return n.ToString("0.################",CultureInfo.InvariantCulture);
  }
  public static List<SandboxOption> All(){return Files.Resource("sandbox.tsv").Split(new[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries).Select(line=>{var p=line.Split('\t');return new SandboxOption{Key=p[0],Title=p[1],Group=p[2],Default=p[3],Min=p[4],Max=p[5],Kind=p[6],Hint=p[7]};}).ToList();}
 }
 public static class Ini {
  static bool Header(string line,out string name){var t=line.Trim();name="";if(t.StartsWith("[")&&t.EndsWith("]")){name=t.Substring(1,t.Length-2);return true;}return false;}
  static string Key(string line){string t=line.TrimStart();if(t.StartsWith(";")||t.StartsWith("#"))return "";int i=t.IndexOf('=');return i<0?"":t.Substring(0,i).Trim();}
  public static Dictionary<string,string> Read(string text,string section){var r=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);bool inside=false;foreach(string line in text.Replace("\r","").Split('\n')){string name;if(Header(line,out name)){inside=String.Equals(section,name,StringComparison.OrdinalIgnoreCase);continue;}if(inside){string k=Key(line);if(k!="")r[k]=line.Substring(line.IndexOf('=')+1).Trim();}}return r;}
  public static string Merge(string text,string section,IEnumerable<SettingValue> entries){var data=entries.ToList();var keys=new HashSet<string>(data.Select(x=>x.Key),StringComparer.OrdinalIgnoreCase);return Rewrite(text,section,keys,data);}
  public static string ReplaceModerators(string text,IEnumerable<string> ids){return Rewrite(text,"Moderators",new HashSet<string>(new[]{"Moderator"},StringComparer.OrdinalIgnoreCase),ids.Select(x=>new SettingValue("Moderator",x)).ToList());}
  static string Rewrite(string text,string section,HashSet<string> keys,List<SettingValue> data){var lines=text.Replace("\r","").TrimEnd('\n').Split('\n');var output=new List<string>();bool inside=false,inserted=false,found=false;foreach(var line in lines){string name;if(Header(line,out name)){if(inside&&!inserted){output.AddRange(data.Select(x=>x.Key+"="+x.Value));inserted=true;}inside=String.Equals(name,section,StringComparison.OrdinalIgnoreCase);if(inside)found=true;output.Add(line);continue;}if(inside&&keys.Contains(Key(line)))continue;output.Add(line);}if(inside&&!inserted){output.AddRange(data.Select(x=>x.Key+"="+x.Value));inserted=true;}if(!found){output.Add("["+section+"]");output.AddRange(data.Select(x=>x.Key+"="+x.Value));}return String.Join("\r\n",output).TrimEnd()+"\r\n";}
  public static void SaveSandbox(Profile p){
   // The game treats a pre-created world folder without a real save as corrupt.
   // Always write launcher rules outside SaveGames, using its documented override.
   string source=Paths.Sandbox(p),path=Paths.ManagedSandbox(p);
   Files.EnsureNoLinks(source);Files.EnsureNoLinks(path);
   string before=File.Exists(source)?File.ReadAllText(source):"";
   var values=p.Sandbox.ToDictionary(x=>x.Key,x=>x.Value,StringComparer.OrdinalIgnoreCase);
   var validated=new List<SettingValue>();
   foreach(var s in SandboxOption.All()){string v;if(values.TryGetValue(s.Key,out v))validated.Add(new SettingValue(s.Key,s.Normalize(v)));}
   string after=Merge(before,"SandboxSettings",validated);
   if(!File.Exists(path)||after!=before)Files.Atomic(path,new UTF8Encoding(true).GetBytes(after),true);
  }
 }
 public static class Paths {
  public const string Shipping="AbioticFactorServer-Win64-Shipping.exe";
  public static string Exe(string root){return Path.Combine(root,"AbioticFactor","Binaries","Win64",Shipping);}
  public static bool Valid(string root){try{return File.Exists(Exe(root))&&Directory.Exists(Path.Combine(root,"AbioticFactor","Content","Paks"));}catch{return false;}}
  public static string Saved(string root){return Path.Combine(root,"AbioticFactor","Saved");}
  public static string Worlds(string root){return Path.Combine(Saved(root),"SaveGames","Server","Worlds");}
  public static string World(Profile p){ValidateWorld(p.World);return Path.Combine(Worlds(p.Root),p.World);}
  public static string WorldSandbox(Profile p){return Path.Combine(World(p),"SandboxSettings.ini");}
  public static string Sandbox(Profile p){string managed=ManagedSandbox(p);return File.Exists(managed)?managed:WorldSandbox(p);}
  public static string ManagedSandboxRelative(Profile p){
   ValidateWorld(p.World);
   using(var hash=SHA256.Create())return "Config/WindowsServer/GateControl/"+BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(p.World.ToLowerInvariant()))).Replace("-","").ToLowerInvariant()+".ini";
  }
  public static string ManagedSandbox(Profile p){return Path.Combine(Saved(p.Root),ManagedSandboxRelative(p).Replace('/',Path.DirectorySeparatorChar));}
  public static string Admin(string root){return Path.Combine(Saved(root),"SaveGames","Server","Admin.ini");}
  public static string Context(Profile p){return Path.GetFullPath(WorldSandbox(p)).ToLowerInvariant();}
  public static void CheckWorldBeforeStart(Profile p){
   string world=World(p);if(!Directory.Exists(world))return;
   Files.EnsureNoLinks(world);
   if(Directory.GetDirectories(world).Length>0)return;
   var files=Directory.GetFiles(world);
   if(files.All(f=>String.Equals(Path.GetFileName(f),"SandboxSettings.ini",StringComparison.OrdinalIgnoreCase)||Regex.IsMatch(Path.GetFileName(f),@"^SandboxSettings\.ini\..+\.bak$",RegexOptions.IgnoreCase)))
    throw new InvalidDataException("世界目录“"+p.World+"”已存在，但只有沙盒配置或为空，没有生成真正的世界存档。这可能由 1.0 版提前创建目录造成。请保留旧目录，在服务器设置中换一个全新世界名称，再读取沙盒规则并启动；不要删除已有进度的存档。");
  }
  public static string NormalizeRoot(string input){string p=Path.GetFullPath(input.Trim().Trim('"'));if(File.Exists(p))p=Path.GetDirectoryName(p);for(int i=0;i<5&&!String.IsNullOrEmpty(p);i++){if(Valid(p))return p.TrimEnd(Path.DirectorySeparatorChar);p=Path.GetDirectoryName(p);}throw new DirectoryNotFoundException("没有找到完整的专用服务器。请选择含 AbioticFactorServer.exe 和 AbioticFactor 文件夹的目录。");}
  public static void ValidateWorld(string value){if(String.IsNullOrWhiteSpace(value)||value=="."||value==".."||value.IndexOfAny(Path.GetInvalidFileNameChars())>=0||value.EndsWith(".")||value.EndsWith(" ")||value.Length>100||Regex.IsMatch(value,@"^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(?:\.|$)",RegexOptions.IgnoreCase))throw new ArgumentException("世界名称必须是有效文件夹名，不能包含路径、结尾空格或保留设备名称。");}
  public static string Detect(){var candidates=new List<string>{AppDomain.CurrentDomain.BaseDirectory,Environment.CurrentDirectory};string steam="";using(var key=Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam")){if(key!=null)steam=Convert.ToString(key.GetValue("SteamPath",""));}if(steam!=""){candidates.Add(Path.Combine(steam,"steamapps","common","Abiotic Factor Dedicated Server"));string libraries=Path.Combine(steam,"steamapps","libraryfolders.vdf");if(File.Exists(libraries))foreach(Match m in Regex.Matches(File.ReadAllText(libraries),"\"path\"\\s+\"([^\"]+)\""))candidates.Add(Path.Combine(m.Groups[1].Value.Replace("\\\\","\\"),"steamapps","common","Abiotic Factor Dedicated Server"));}foreach(var d in DriveInfo.GetDrives()){if(d.DriveType==DriveType.Fixed)candidates.Add(Path.Combine(d.RootDirectory.FullName,"SteamLibrary","steamapps","common","Abiotic Factor Dedicated Server"));}return candidates.FirstOrDefault(Valid)??"";}
 }
 public static class Launch {
  public static IEnumerable<string> LocalAddresses(){return NetworkInterface.GetAllNetworkInterfaces().Where(n=>n.OperationalStatus==OperationalStatus.Up).SelectMany(n=>n.GetIPProperties().UnicastAddresses).Where(a=>a.Address.AddressFamily==AddressFamily.InterNetwork).Select(a=>a.Address.ToString()).Concat(new[]{"127.0.0.1","0.0.0.0"}).Distinct();}
  static void SafeText(string value,string title){if(value.Any(c=>Char.IsControl(c)||c=='"'))throw new ArgumentException(title+"不能包含双引号或控制字符。");if(value.EndsWith("\\"))throw new ArgumentException(title+"不能以反斜杠结尾。");}
  public static void Validate(Profile p){if(!Paths.Valid(p.Root))throw new ArgumentException("请先选择有效的专用服务器目录。");if(String.IsNullOrWhiteSpace(p.Name))throw new ArgumentException("请填写房间名称。");SafeText(p.Name,"房间名称");SafeText(p.ServerPassword,"房间密码");SafeText(p.AdminPassword,"管理员密码");Paths.ValidateWorld(p.World);if(p.Players<1||p.Players>24)throw new ArgumentException("玩家上限必须为 1–24。");if(p.Port<1||p.Port>65535||p.QueryPort<1||p.QueryPort>65535||p.Port==p.QueryPort)throw new ArgumentException("端口必须在 1–65535 之间，且游戏与查询端口不能相同。");if(!new[]{"","PC","Playstation","Xbox"}.Contains(p.Platform))throw new ArgumentException("平台设置无效。");IPAddress ip;if(p.Ip!=""&&(!IPAddress.TryParse(p.Ip,out ip)||ip.AddressFamily!=AddressFamily.InterNetwork||!LocalAddresses().Contains(p.Ip)))throw new ArgumentException("绑定 IP 必须是本机已有的 IPv4 地址（或 0.0.0.0）。");if(p.ManageSandbox&&p.SandboxContext!=Paths.Context(p))throw new ArgumentException("世界或路径已改变。请到沙盒规则页读取当前世界后再应用设置。");}
  static string Value(string key,string value){return "-"+key+"=\""+value+"\"";}
  public static string Arguments(Profile p,bool redact){var a=new List<string>{"-log","-newconsole",Value("SteamServerName",p.Name),"-MaxServerPlayers="+p.Players,"-PORT="+p.Port,"-QUERYPORT="+p.QueryPort,Value("WorldSaveName",p.World)};if(p.ManageSandbox||File.Exists(Paths.ManagedSandbox(p)))a.Add(Value("SandboxIniPath",Paths.ManagedSandboxRelative(p)));if(p.ServerPassword!="")a.Add(Value("ServerPassword",redact?"******":p.ServerPassword));if(p.AdminPassword!="")a.Add(Value("AdminPassword",redact?"******":p.AdminPassword));if(p.Ip!="")a.Add("-MultiHome="+p.Ip);if(p.Platform!="")a.Add("-PlatformLimited="+p.Platform);if(p.Lan)a.Add("-LANOnly");if(p.LocalIps)a.Add("-UseLocalIPs");if(p.Perf)a.Add("-useperfthreads");return String.Join(" ",a);}
  public static string Redact(string log,Profile p){var result=Regex.Replace(log,"(?i)(-?(?:ServerPassword|AdminPassword)\\s*=\\s*)(?:\"[^\"]*\"|[^\\s]+)","$1******");if(p!=null){foreach(string s in new[]{p.ServerPassword,p.AdminPassword})if(s!="")result=result.Replace(s,"******");}return result;}
  public static void CheckPorts(Profile p){foreach(int port in new[]{p.Port,p.QueryPort}){using(var socket=new Socket(AddressFamily.InterNetwork,SocketType.Dgram,ProtocolType.Udp)){socket.ExclusiveAddressUse=true;try{socket.Bind(new IPEndPoint(p.Ip==""?IPAddress.Any:IPAddress.Parse(p.Ip),port));}catch(SocketException){throw new IOException("UDP 端口 "+port+" 无法绑定，可能已被占用。");}}}}
  public static bool OtherServer(string root){foreach(var p in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(Paths.Shipping))){using(p){try{if(!p.HasExited&&String.Equals(p.MainModule.FileName,Paths.Exe(root),StringComparison.OrdinalIgnoreCase))return true;}catch(System.ComponentModel.Win32Exception){throw new IOException("无法检查已有服务器进程，请先关闭外部启动的服务器。");}catch(InvalidOperationException){}}}return false;}
 }
 public class Countdown {
  public DateTime? DueUtc; public double Minutes; public bool Restart,Repeat;
  public void Set(double minutes,bool restart,bool repeat){if(Double.IsNaN(minutes)||Double.IsInfinity(minutes)||minutes<0.05||minutes>10080)throw new ArgumentException("倒计时范围为 0.05–10080 分钟（3 秒至 7 天）。");if(repeat&&!restart)throw new ArgumentException("循环计划仅适用于重启。");Minutes=minutes;Restart=restart;Repeat=repeat;DueUtc=DateTime.UtcNow.AddMinutes(minutes);}
  public bool TakeDue(DateTime now){if(DueUtc.HasValue&&now>=DueUtc.Value){DueUtc=null;return true;}return false;}
  public void Cancel(){DueUtc=null;Repeat=false;}
 }
 public class ServerRunner {
  public Process Process;public Profile RunningProfile;public string DataDir; public bool Stopping; public string LastStopError="";
  public readonly ServerHealth Health=new ServerHealth();
  public bool Alive {get{try{return Process!=null&&!Process.HasExited;}catch{return false;}}}
  public ServerRunner(string dataDir){DataDir=dataDir;}
  public bool Recover(Profile p){string file=Path.Combine(DataDir,"active.xml");if(!File.Exists(file))return false;var a=Files.Load<ActiveRecord>(file);try{var proc=System.Diagnostics.Process.GetProcessById(a.Pid);if(proc.StartTime.ToUniversalTime().Ticks==a.StartTicks&&String.Equals(proc.MainModule.FileName,a.Exe,StringComparison.OrdinalIgnoreCase)&&String.Equals(a.Exe,Paths.Exe(p.Root),StringComparison.OrdinalIgnoreCase)){Process=proc;RunningProfile=p.Clone();return true;}proc.Dispose();}catch(ArgumentException){}catch(InvalidOperationException){}return false;}
  public void Start(Profile p){if(Alive)throw new InvalidOperationException("服务器已经运行。");Launch.Validate(p);if(Launch.OtherServer(p.Root))throw new InvalidOperationException("同一目录的服务器已被其他程序启动，请先将其关闭。");Launch.CheckPorts(p);Files.EnsureNoLinks(Paths.Saved(p.Root));Paths.CheckWorldBeforeStart(p);if(p.ManageSandbox)Ini.SaveSandbox(p);var start=new ProcessStartInfo(Paths.Exe(p.Root),Launch.Arguments(p,false)){WorkingDirectory=Path.GetDirectoryName(Paths.Exe(p.Root)),UseShellExecute=false,CreateNoWindow=false,WindowStyle=p.Console?ProcessWindowStyle.Normal:ProcessWindowStyle.Hidden};Health.Reset();RunningProfile=p.Clone();Process=System.Diagnostics.Process.Start(start);Stopping=false;SaveActive();}
  public void RefreshHealth(){
   if(!Alive||RunningProfile==null)return;
   string path=Path.Combine(Paths.Saved(RunningProfile.Root),"Logs","AbioticFactor.log");
   if(!File.Exists(path)||File.GetLastWriteTimeUtc(path)<Process.StartTime.ToUniversalTime())return;
   using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete)){
    long start=Math.Max(0,stream.Length-512*1024);stream.Seek(start,SeekOrigin.Begin);
    using(var reader=new StreamReader(stream,Encoding.UTF8,true)){if(start>0)reader.ReadLine();Health.Observe(reader.ReadToEnd());}
   }
  }
  void SaveActive(){var a=new ActiveRecord{Pid=Process.Id,StartTicks=Process.StartTime.ToUniversalTime().Ticks,Exe=Paths.Exe(RunningProfile.Root),Root=RunningProfile.Root};Files.Save(Path.Combine(DataDir,"active.xml"),a);}
  public async Task<bool> StopAsync(){if(!Alive)return true;Stopping=true;LastStopError="";try{var p=Process;bool signaled=false;var psi=new ProcessStartInfo(Assembly.GetExecutingAssembly().Location,"--signal-stop "+p.Id+" "+p.StartTime.ToUniversalTime().Ticks+" \""+p.MainModule.FileName+"\""){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden};using(var helper=System.Diagnostics.Process.Start(psi)){bool helperDone=await Task.Run(()=>helper.WaitForExit(5000));if(!helperDone){helper.Kill();LastStopError="signal helper timeout";}else if(helper.ExitCode==0)signaled=true;else LastStopError="signal helper code="+helper.ExitCode;}if(!Alive)return true;if(!signaled){signaled=NativeConsole.CloseServerWindows(p.Id);if(!signaled)return false;}bool exited=await Task.Run(()=>p.WaitForExit(45000));if(!exited)LastStopError="server exit timeout";return exited;}finally{Stopping=false;}}
  public void HideConsole(){if(!Alive||RunningProfile.Console)return;var p=Process;NativeConsole.HideServerWindows(p.Id);var psi=new ProcessStartInfo(Assembly.GetExecutingAssembly().Location,"--hide-console "+p.Id+" "+p.StartTime.ToUniversalTime().Ticks+" \""+p.MainModule.FileName+"\""){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden};using(var helper=System.Diagnostics.Process.Start(psi)){} }
  public void Clear(){if(Process!=null){Process.Dispose();Process=null;}string file=Path.Combine(DataDir,"active.xml");if(File.Exists(file))File.Delete(file);}
 }
 public class ServerHealth {
  public bool Ready,SaveCorrupt;
  public void Reset(){Ready=false;SaveCorrupt=false;}
  public void Observe(string text){
   if(Regex.IsMatch(text,@"World save Integrity State:\s*Corrupt|Dedicated Server will shut down in 5 minutes due to world save corruption|Failed to restore backup: No Backup Found|world save is in a non-playable state",RegexOptions.IgnoreCase)){SaveCorrupt=true;Ready=false;return;}
   if(!SaveCorrupt&&Regex.IsMatch(text,@"Load map complete /Game/Maps/Facility(?:\s|$)",RegexOptions.IgnoreCase))Ready=true;
  }
 }
 public static class NativeConsole {
  [DllImport("kernel32.dll",SetLastError=true)]static extern bool AttachConsole(uint pid);
  [DllImport("kernel32.dll")]static extern bool FreeConsole();
  [DllImport("kernel32.dll")]static extern bool SetConsoleCtrlHandler(IntPtr handler,bool add);
  [DllImport("kernel32.dll",SetLastError=true)]static extern bool GenerateConsoleCtrlEvent(uint e,uint group);
  [DllImport("kernel32.dll")]static extern uint GetConsoleProcessList(uint[] list,uint count);
  [DllImport("kernel32.dll")]static extern IntPtr GetConsoleWindow();
  [DllImport("user32.dll")]static extern bool ShowWindow(IntPtr window,int command);
  delegate bool EnumWindowProc(IntPtr window,IntPtr param);
  [DllImport("user32.dll")]static extern bool EnumWindows(EnumWindowProc callback,IntPtr param);
  [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr window,out uint pid);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetClassName(IntPtr window,StringBuilder name,int count);
  [DllImport("user32.dll")]static extern bool PostMessage(IntPtr window,uint message,IntPtr wParam,IntPtr lParam);
  static List<IntPtr> LogWindows(int pid){var result=new List<IntPtr>();EnumWindows((window,param)=>{uint owner;GetWindowThreadProcessId(window,out owner);if(owner==(uint)pid){var name=new StringBuilder(256);GetClassName(window,name,name.Capacity);if(name.ToString().IndexOf("log",StringComparison.OrdinalIgnoreCase)>=0||name.ToString().IndexOf("console",StringComparison.OrdinalIgnoreCase)>=0)result.Add(window);}return true;},IntPtr.Zero);return result;}
  public static bool CloseServerWindows(int pid){bool sent=false;foreach(var window in LogWindows(pid))sent=PostMessage(window,0x0010,IntPtr.Zero,IntPtr.Zero)||sent;return sent;}
  public static void HideServerWindows(int pid){foreach(var window in LogWindows(pid))ShowWindow(window,0);}
  public static string WindowClasses(int pid){var names=new List<string>();EnumWindows((window,param)=>{uint owner;GetWindowThreadProcessId(window,out owner);if(owner==(uint)pid){var name=new StringBuilder(256);GetClassName(window,name,name.Capacity);names.Add(name.ToString());}return true;},IntPtr.Zero);return String.Join(",",names);}
  public static int Run(string[] args){try{int pid=Int32.Parse(args[1]);long ticks=Int64.Parse(args[2]);using(var p=Process.GetProcessById(pid)){if(p.StartTime.ToUniversalTime().Ticks!=ticks||!String.Equals(p.MainModule.FileName,args[3],StringComparison.OrdinalIgnoreCase))return 2;FreeConsole();if(!AttachConsole((uint)pid))return 3;SetConsoleCtrlHandler(IntPtr.Zero,true);try{if(args[0]=="--hide-console"){ShowWindow(GetConsoleWindow(),0);return 0;}var ids=new uint[32];uint count=GetConsoleProcessList(ids,(uint)ids.Length);uint self=(uint)Process.GetCurrentProcess().Id;if(count>ids.Length||ids.Take((int)count).Any(id=>id!=self&&id!=(uint)pid))return 4;if(!GenerateConsoleCtrlEvent(0,0))return 5;Thread.Sleep(900);return 0;}finally{FreeConsole();}}}catch{return 6;}}
 }
}
