# Task 2 — DLL 构建映射决策

> 来源：项目文件读取 + 实际 MSBuild 构建验证  
> 构建工具：`D:\microsoft visual studio\MSBuild\Current\Bin\MSBuild.exe`  
> 验证时间：2026-08-22

---

## 决策摘要

| 比赛项目 | 项目目录 | 最终 DLL | 状态 |
|---------|---------|---------|------|
| 生存挑战 | `StrategyFightForLive\` | `StrategyFightForLive.dll` | 已有项目，直接实现 |
| 极限救援 | `StrategyExtremeRescue\`（新建） | `StrategyExtremeRescue.dll` | 从 StrategyBallMoving 复制结构 |

---

## 1. 生存挑战 DLL 映射

| 属性 | 值 |
|-----|---|
| 项目文件 | `StrategyFightForLive\StrategyFightForLive.csproj` |
| AssemblyName | `StrategyFightForLive` |
| RootNamespace | `URWPGSim2D.Strategy` |
| OutputType | `Library` |
| TargetFrameworkVersion | `v3.5` |
| OutputPath (Release) | `bin\Release\` |
| PlatformTarget (Debug) | `x86` |
| PlatformTarget (Release) | **未指定 → 默认 AnyCPU/MSIL**（需修改） |
| 最终 DLL 路径 | `E:\progaram\URWPGSim2D\Strategy\StrategyFightForLive\bin\Release\StrategyFightForLive.dll` |
| 源策略文件 | `StrategyFightForLive.cs` |
| 引用 DLL 路径 | `E:\progaram\URWPGSim2D\URWPGSim2D\bin\` |

**验证结果**：`msbuild StrategyFightForLive.csproj /p:Configuration=Release` 构建成功，产出 DLL。出现 MSB3270 警告（Release 为 MSIL，引用为 x86）。

---

## 2. 极限救援 DLL 映射

| 属性 | 值 |
|-----|---|
| 项目文件 | `StrategyExtremeRescue\StrategyExtremeRescue.csproj`（从 StrategyBallMoving 复制） |
| AssemblyName | `StrategyExtremeRescue` |
| RootNamespace | `URWPGSim2D.Strategy` |
| OutputType | `Library` |
| TargetFrameworkVersion | `v3.5` |
| OutputPath (Release) | `bin\Release\` |
| PlatformTarget (Debug) | `x86` |
| PlatformTarget (Release) | `x86`（在创建时直接写入，修正 BallMoving 的遗漏） |
| 最终 DLL 路径 | `E:\progaram\URWPGSim2D\Strategy\StrategyExtremeRescue\bin\Release\StrategyExtremeRescue.dll` |
| 源策略文件 | `StrategyExtremeRescue.cs`（从 StrategyBallMoving.cs 重命名） |
| 引用 DLL 路径 | `E:\progaram\URWPGSim2D\URWPGSim2D\bin\`（与 BallMoving 相同） |

**创建步骤**：
1. 复制 `StrategyBallMoving\` → `StrategyExtremeRescue\`
2. 重命名 `StrategyBallMoving.csproj` → `StrategyExtremeRescue.csproj`
3. 修改 csproj：AssemblyName、ProjectGuid（新 GUID）、Compile Include 文件名
4. Release PropertyGroup 中增加 `<PlatformTarget>x86</PlatformTarget>`
5. 重命名 `StrategyBallMoving.cs` → `StrategyExtremeRescue.cs`

---

## 3. 构建命令

```powershell
# 生存挑战
& "D:\microsoft visual studio\MSBuild\Current\Bin\MSBuild.exe" `
  "E:\progaram\URWPGSim2D\Strategy\StrategyFightForLive\StrategyFightForLive.csproj" `
  /p:Configuration=Release /v:m

# 极限救援
& "D:\microsoft visual studio\MSBuild\Current\Bin\MSBuild.exe" `
  "E:\progaram\URWPGSim2D\Strategy\StrategyExtremeRescue\StrategyExtremeRescue.csproj" `
  /p:Configuration=Release /v:m
```

---

## 4. 需要修改的地方

| 文件 | 修改内容 | 必要性 |
|-----|---------|--------|
| `StrategyFightForLive.csproj` | Release PropertyGroup 增加 `<PlatformTarget>x86</PlatformTarget>` | 消除 MSB3270 警告，与引用 DLL 架构一致 |
| `StrategyExtremeRescue.csproj`（新建时） | Release PropertyGroup 直接写入 `<PlatformTarget>x86</PlatformTarget>` | 从一开始就保持正确 |

---

## 5. 其他发现

- `StrategyBallMoving\bin\Debug\` 中有一个 `StrategyFightForLive.dll` 的旧拷贝，来历不明，不影响新构建。
- `microsoft.netframework.referenceassemblies.net35` nuget 包 1.0.3 存在，可作为 FrameworkPathOverride 备用（StrategyNew2V2 已使用此方案）。
- `Strategy.sln` 目前只含 StrategyNew2V2，生存挑战和极限救援项目需要单独 msbuild 命令或加入 sln。
