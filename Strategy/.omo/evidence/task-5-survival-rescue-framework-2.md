# Task 5 — 极限救援策略项目创建与实现

> 执行时间：2026-08-22  
> 执行范围：STEP 1（项目创建）+ STEP 2（策略实现）+ STEP 3（构建验证）

---

## 1. 项目结构

```
StrategyExtremeRescue\
├── StrategyExtremeRescue.csproj          （从 StrategyBallMoving.csproj 复制修改）
├── StrategyExtremeRescue.cs              （完整策略实现）
├── Properties\
│   └── AssemblyInfo.cs                   （AssemblyTitle/Product 改为 StrategyExtremeRescue）
├── bin\
│   └── Release\
│       └── StrategyExtremeRescue.dll     ← 构建产物
└── obj\Release\...
```

### csproj 关键修改点（相对 StrategyBallMoving）

| 字段 | 原值 | 新值 |
|------|------|------|
| `AssemblyName` | StrategyBallMoving | StrategyExtremeRescue |
| `ProjectGuid` | {17927D65-...} | {A1B2C3D4-E5F6-7890-ABCD-EF1234567890} |
| `Compile Include` | StrategyBallMoving.cs | StrategyExtremeRescue.cs |
| Release `PlatformTarget` | （缺失）| x86 |

---

## 2. 状态机说明

### 角色判断

```
FishCntPerTeam == 2  →  进攻方（2条警察鱼，index 0/1）
FishCntPerTeam == 1  →  防守方（1条恐怖分子，index 0）
```

### 进攻方状态转换图（每条鱼独立）

```
         ┌──────────────────────────────────────────────────────────────────┐
         │                      初始化                                      │
         ▼                                                                  │
   ┌──────────┐   距离人质 < 120mm    ┌──────────────┐                     │
   │ SeekBall │ ─────────────────────► │ CarryToGoal  │                     │
   └──────────┘                       └──────────────┘                     │
         ▲                                  │                               │
         │  fishToBall > 280mm              │ 人质丢失                      │
         │  (人质丢失)                      └──────────────────────────────┘│
         │                                                                  │
         │   stuckTimer > 60 (任意状态)                                     │
         ▼                                                                  │
   ┌──────────┐   unstuckTimer >= 60   ┌──────────────────────────────────┐│
   │ Unstuck  │ ─────────────────────► │ 恢复到触发前的状态（prevState）   ││
   └──────────┘                       └──────────────────────────────────┘│
```

### 状态详情

| 状态 | 触发条件 | 行为 | 退出条件 |
|------|---------|------|---------|
| SeekBall | 初始 / 人质丢失后 | 向最近未锁定人质移动（含绕障） | 鱼→人质距离 < 120mm |
| CarryToGoal | 到达人质 120mm 内 | 站到人质背后推球进安全区（含绕障） | 人质丢失（>280mm）→ 回 SeekBall |
| Unstuck | 连续 60 帧位移 < 5mm | 前30帧右转(TCode=11)，后30帧左转(TCode=3) | 60帧后恢复 prevState |

---

## 3. 人质位置获取方式（运行时确认）

### 获取优先级

```csharp
// 优先：圆形障碍物列表（极限救援规则：人质为圆形仿真障碍物）
if (mission.EnvRef.ObstaclesRound != null && mission.EnvRef.ObstaclesRound.Count >= 2)
{
    hx[i] = mission.EnvRef.ObstaclesRound[i].PositionMm.X;
    hz[i] = mission.EnvRef.ObstaclesRound[i].PositionMm.Z;
}
// 备选：Balls 列表（部分版本可能在此）
else if (mission.EnvRef.Balls != null && mission.EnvRef.Balls.Count >= 2)
{
    hx[i] = mission.EnvRef.Balls[i].PositionMm.X;
    hz[i] = mission.EnvRef.Balls[i].PositionMm.Z;
}
```

### 规则文件依据（task-1-survival-rescue-framework-2.md）

> "2个圆形仿真人质（非水球，为圆形仿真障碍物/人质对象）"

→ **主用 `mission.EnvRef.ObstaclesRound`**，`Balls` 为备选。

### 安全区获取

优先读 `HtMissionVariables["SafeZone_X/Z"]` 或 `["SafeArea_X/Z"]`；  
读取失败时默认 `(1800.0, 0.0)`（场地右侧）。  
下半场交换时自动取反 X 坐标。

---

## 4. 构建结果

```
MSBuild 版本：适用于 .NET Framework MSBuild 版本 18.8.2+ce25c0108
配置：Release
目标框架：v3.5
平台目标：x86

输出：
  StrategyExtremeRescue -> E:\progaram\URWPGSim2D\Strategy\StrategyExtremeRescue\bin\Release\StrategyExtremeRescue.dll

错误数：0
警告数：0
退出码：0（成功）
```

**DLL 路径**：`E:\progaram\URWPGSim2D\Strategy\StrategyExtremeRescue\bin\Release\StrategyExtremeRescue.dll`

---

## 5. 关键实现说明

### 工具函数（全部内联，无外部依赖）

| 函数 | 说明 |
|------|------|
| `MoveToXZ` | Bang-Bang 时间最优控制，从 BaseAction.FastMoveTo 移植，直接操作局部 `decisions` |
| `Detour` | 路径障碍物检测（投影法），偏移目标到障碍物侧边外 600mm |
| `GetSafeZoneCenter` | 安全区读取 + 下半场镜像 |
| `Dist` | 纯欧氏距离 |
| `NormRad` | 角度归一化到 (-PI, PI] |

### 常量来源

- `vTable[16]`：来自 `GlobalVar.vTable`（task-3 evidence 确认）
- 障碍物坐标：来自 task-1 evidence 规则文件
- 场地半尺寸：4500×3000 → HalfX=2250, HalfZ=1500

### MUST NOT 遵守情况

- ✅ 未修改 StrategyBallMoving 任何文件
- ✅ 未引用 GlobalVar 或 StrategyNew2V2 类型
- ✅ 未添加额外外部依赖（仅 System, URWPGSim2D.Common, StrategyLoader）
- ✅ 每帧所有 decisions 均赋值（帧首重置为 VCode=0, TCode=7）
- ✅ VCode 和 TCode 限制在 0-14 范围内
