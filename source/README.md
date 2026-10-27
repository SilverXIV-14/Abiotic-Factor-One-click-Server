# GATE Control 源码

Windows WPF / C#，使用系统自带的 .NET Framework 编译器，无 NuGet 包。

在本目录执行 `powershell -NoProfile -File .\build.ps1`，输出到上一级 `build\GateControl.exe`。需要 Windows 64 位及 .NET Framework 4.8。`build.ps1` 引用系统 Framework / WPF 程序集，并嵌入界面、48 项沙盒字段和图标。

文件：

- `App.cs`：界面绑定、页面、倒计时与存档操作。
- `Core.cs`：参数、校验、INI 事务写入、DPAPI、进程生命周期及正常退出通道。
- `MainWindow.xaml`：六页中文深色界面。
- `sandbox.tsv`：官方键名、默认值、有效范围和中文标签。
- `Tests.cs`：核心回归测试和真实服务器隔离测试。

开发诊断参数（不要在生产服务器目录执行集成测试）：

```
GateControl.exe --self-test "C:\test\results.txt"
GateControl.exe --render-preview "C:\test\previews"
GateControl.exe --data-dir "C:\test\profile"
GateControl.exe --integration "C:\test\server-copy" "C:\test\profile" "C:\test\integration.txt"
```

`--integration` 会在指定服务器副本中生成独立沙盒配置并选择唯一的中文新世界名、启动两次本地服务器并退出；只可指向可丢弃的副本，不能指向正式目录。成功条件要求进入 Facility、没有存档损坏标志，并由游戏日志确认难度 2 生效。没有玩家参与，不能据此证明世界保存或联网成功。

将程序副本命名为 `GateControl.QA.exe` 时，本地偏好写入程序旁的 `qa-profile`，供交互测试隔离使用。正式发布保持 `GateControl.exe` 名称。

正常退出优先使用 Windows 控制台事件。新版服务器日志窗口不暴露传统控制台时，仅向归属于目标 PID 的日志／控制台窗口发送 WM_CLOSE；核实进程退出前不会重启。定时任务不持久化。单实例互斥锁按当前 Windows 用户隔离。

不修改防火墙、路由器、系统服务或计划任务；不包含游戏资源，不发送遥测。源码中的图标为原创几何图形。

