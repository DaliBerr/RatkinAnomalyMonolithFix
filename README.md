# RatkinAnomalyMonolithFix

支持 RimWorld 1.5、1.6、1.7，需要 Anomaly DLC、对应版本的 Harmony 和 Ratkin Anomaly+（fxz.ratkinanomaly.update，工坊 3293914637）。

在鼠族结局完成前选择断开虚空，会结束原版虚空地图流程并保留巨石，让玩家继续体验紫罗兰之夜；鼠族结局结束后，巨石按原有设计崩毁并应用断开虚空的结局效果。

本次更新保留三种翻译及原有存档字段。结局回调会在 Ratkin 清除 actor 前保存参与者引用，处理原英雄引用丢失的情况；完成结局时直接收尾，避免重复关闭虚空地图或重播字幕。新游戏会重置静态状态，防止前一个存档的结局状态串入新游戏。

## 构建与验证

三个版本共用 1.6/Source 的生产代码。1.5/1.6 针对 .NET Framework 4.7.2、Harmony 2 构建，1.7 针对 .NET 10、Harmony 3 构建。鼠族结局方法在运行时定位，避免把某个 Ratkin 版本的原版程序集版本硬绑到其他游戏版本。依赖本体不会被修改或打包。

```powershell
./build.ps1
dotnet run --project Tests/Regression/Regression.csproj -c Release
dotnet run --project Tests/Compatibility/Compatibility.csproj -c Release
./build.ps1 -Deploy
```

旧版本原版参考默认来自各版本 ExternalMods；1.7 来自当前游戏根目录，Harmony 3 来自工坊 2009463077/1.7/Assemblies。可以通过 RimWorldManagedPath、HarmonyAssemblyPath 覆盖构建参考。

兼容性验证默认读取相邻 Rimworld_Assmb/RatkinAnomaly/3293914637 的三个版本 DLL，并核对真实 EndPlay、actor、原版结局方法签名和加载目录。回归测试直接链接生产代码，使用少量引擎替身验证结局路线、XML 存档、空引用和重复回调。这些检查不等于在实际游戏中完整通关测试。

部署前把旧安装移到 Build/backups，失败时恢复，保留原工坊 PublishedFileId。原版与鼠族依赖、缓存、历史输出和安装备份不会上传 GitHub。

English: Supports RimWorld 1.5, 1.6 and 1.7. Preserves the monolith after the original Disrupt the Link ending so the Ratkin ending remains available, then collapses it after the Ratkin ending. Retains legacy save keys and handles missing hero references without replaying the original victory credits.
