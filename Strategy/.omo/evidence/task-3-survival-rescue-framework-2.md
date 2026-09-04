# Task 3 — 可复用 helper 边界与共享状态隔离

> 来源：BaseAction.cs（8015行全文）、GlobalVar.cs（225行）、Strategy.cs（前300行）分析  
> 分析时间：2026-08-22

---

## 1. 可复用纯工具函数清单

以下函数可以安全地逐字复制到各任务项目内的 `StrategyHelper.cs`（或直接内联）。

| 函数名 | 文件:行 | 依赖 GlobalVar | 副作用 | 说明 |
|--------|--------|---------------|--------|------|
| `equal` | BaseAction.cs:109 | 否 | 无 | 纯数值比较 |
| `FormatAngle` | BaseAction.cs:114 | 否 | 无 | 角度规整（度数，纯算术） |
| `FormatAngleRad` | BaseAction.cs:126 | 仅 `GlobalVar.PI`（= Math.PI） | 无 | 替换为 `Math.PI` 后完全纯 |
| `GetAngle` | BaseAction.cs:138 | 仅 `GlobalVar.PI` | 无 | 返回角度（度），替换 PI 后纯 |
| `GetAngleRad` | BaseAction.cs:142 | 否 | 无 | 返回弧度，纯 `Math.Atan2` |
| `GetLineAcrossPoint` | BaseAction.cs:146 | 仅 `GlobalVar.PI`（局部 `pI`） | 无 | 直线交点，替换 PI 后纯 |
| `IsAngleDuring` | BaseAction.cs:235 | 否（调用 `FormatAngle`） | 无 | 纯角度范围判断 |
| `Compute_Distance` | BaseAction.cs:268 | 否 | 无 | 纯欧氏距离 |
| `Angle_V` | BaseAction.cs:6601 | 否（硬编码阈值） | 无 | TCode 量化，完全纯 |
| `GetCornerSelection` | BaseAction.cs:328 | 否 | 无 | 纯坐标区域判断 |
| `IsInMyUnArea_01` | BaseAction.cs:303 | 否 | 无 | 区域检测 |
| `IsInHome` | BaseAction.cs:313 | 否 | 无 | 区域检测 |
| `IsInOppUnArea_01` | BaseAction.cs:318 | 否 | 无 | 区域检测 |
| `IsInOppHome_Area` | BaseAction.cs:323 | 否 | 无 | 区域检测 |
| `IsInOppForbiddenArea` | BaseAction.cs:534 | 否 | 无 | 区域检测 |
| `IsInCornerChoseBallArea` | BaseAction.cs:539 | 否 | 无 | 区域检测 |
| `IsInUnBall` | BaseAction.cs:5189 | 否 | 无 | 区域检测 |

> **关键说明**：`GlobalVar.PI = 3.1415926535897931`，与 `Math.PI` 数值完全相同。复制时将 `GlobalVar.PI` 替换为 `Math.PI` 即可消除外部依赖，无任何精度损失。

---

## 2. 可复用但需注入依赖的运动函数

以下函数不是纯函数，但逻辑可以复制后用局部参数代替 GlobalVar 引用：

| 函数名 | 文件:行 | 读取 GlobalVar | 写入 GlobalVar | 复用方式 |
|--------|--------|--------------|--------------|---------|
| `SafeApproachSpeed` | BaseAction.cs:26 | MyTeam.Fishes（位置/速度） | 无（只读） | 改为参数传入 fishPos, fishVel |
| `FastMoveTo` | BaseAction.cs:51 | MyTeam.Fishes, PI, vTable | decisions[fishId] | 在各项目的局部 decisions/fishes 上运行 |
| `StayForDecision` | BaseAction.cs:256 | decisions[] | decisions[] | 最小依赖，直接内联 |
| `KeepV` | BaseAction.cs:5146 | MyTeam.Fishes, vTable, tTable | decisions | 局部化后复用 |
| `KeepVelocity` | BaseAction.cs:1797 | MyTeam.Fishes, vTable | decisions | 局部化后复用 |
| `Angle` | BaseAction.cs:399 | MyTeam.Fishes, tTable, 局部 `pi=3.14` | decisions | 注意局部 pi=3.14（不精确），复用时建议改 Math.PI |
| `Angle_Stay` | BaseAction.cs:5071 | MyTeam.Fishes, tTable | decisions | 局部化后复用 |
| `Position` | BaseAction.cs:5533 | MyTeam.Fishes, Fish_centertohead, vTable, tTable, PI | decisions | 核心运动函数，建议复制 |
| `position3` | BaseAction.cs:5674 | 同 Position | decisions | 同上 |

---

## 3. 不可共享的有状态逻辑

### 3.1 BaseAction 实例字段（每个项目自己的 BaseAction 实例持有）

| 字段名 | 用途 | 原因 |
|--------|------|------|
| `filterBallPx/Pz/Vx/Vz/Init[9]` | α-β 滤波器球轨迹估计 | 跨帧持续状态，不能共享 |
| `stalemateCount[2]` | 相持检测计数 | 跨帧，不能共享 |
| `ramCount[2]` | 冲撞持续帧计数 | 跨帧，不能共享 |
| `lockBallId[2]` | 锁球状态 | 核心运球状态，绝对不能共享 |
| `sweepOrbitCount[2]` | 横扫绕位帧计数 | 跨帧，不能共享 |
| `boundary_dribbleact_constdir` | 边界运球方向锁 | 跨帧，不能共享 |
| `current_w` / `previous_w` | 历史转弯档位 | 跨帧，不能共享 |

### 3.2 GlobalVar 可变静态字段（**绝不跨项目共用**）

**运行时注入**（每帧刷新）：
- `MyTeam`, `OppTeam`, `mission`, `balls`, `decisions`, `teamId`, `oppTeamId`, `Paishu`, `MyTeamLast`

**跨帧球分配状态**：
- `Bringing_BallID[2]`, `willbring_ballid[2]`, `SingleBringing_BallID[2]`, `is_ball_choosed[9]`

**分数/球状态**：
- `leftScore`, `rightScore`, `leftScoreCopy`, `rightScoreCopy`, `b0_l..b8_r`

**行为状态机**（全部不可共享）：
- `interfereBallStage*`, `chooseinterfereBallStage*`, `Rescue_boundary_*`, `Dribbleboundary_zt*`, `Dribble_boundaryf_zt*`, `boundary_dribbleact`, `corner`, `corner_help`, `Circle_last2min_tag`, `dead`, `deadjudge`, `locktime*`, `lanqiuFlag`, `last2min_*` 等

**历史轨迹**：
- `enemyx0/1`, `enemyz0/1`, `enemydirection0/1`, `prefish`, `error_record`, `recordfishz` 等

---

## 4. 可安全内联为局部常量的 GlobalVar 字段

以下字段在 `Init()`（空方法）之后**从不被写入**，可内联为各项目的局部 `readonly` 常量：

| 字段名 | 值 | 复制方式 |
|--------|---|---------|
| `PI` | `3.1415926535897931` | 替换为 `Math.PI` |
| `vTable[16]` | `{0.0, 9.02, 31.55, 60.4, 88.35, 110.66, 132.78, 152.16, 172.9, 204.65, 268.52, 289.33, 295.66, 293.99, 303.69, 314.51}` | `private static readonly double[] vTable = {...}` |
| `tTable[16]` | `{-0.3552, -0.2921, -0.22, -0.1731, -0.1235, -0.0784, -0.0469, 0.0, 0.0469, 0.0784, 0.1235, 0.1731, 0.22, 0.2921, 0.3438, 0.3438}` | `private static readonly double[] tTable = {...}` |
| `Fish_centertohead` | `0x69 = 105` | `const int Fish_centertohead = 105` |
| `Fish_length` | `422.8f` | `const float Fish_length = 422.8f` |
| `Fishbody_width` | `0x2c = 44` | `const int Fishbody_width = 44` |
| `Fishhead_Radius` | `30` | `const int Fishhead_Radius = 30` |
| `Fins_width` | `0x2d = 45` | `const int Fins_width = 45` |
| `tail_length` | `64.8f` | `const float tail_length = 64.8f` |
| `tail_width` | `0x69 = 105` | `const int tail_width = 105` |
| `Radius` | `0x3a = 58` | `const int Radius = 58` |
| `Yard_halflength` | `0x834 = 2100` | `const int Yard_halflength = 2100` |
| `Yard_halfwidth` | `0x5dc = 1500` | `const int Yard_halfwidth = 1500` |
| `Goal_line` | `-2100` | `const int Goal_line = -2100` |
| `Opp_Goal_line` | `0x834 = 2100` | `const int Opp_Goal_line = 2100` |

> **注意**：`tTable[15] = 0.3438`（与 [14] 相同，是原始代码的一个细节），复制时原样保留。

---

## 5. 推荐复用策略

### 生存挑战项目（StrategyFightForLive）
复制以下到 `FightForLiveHelper.cs`（同项目内，命名空间 `URWPGSim2D.Strategy`）：
- 所有"纯工具函数"（第1节全部）
- `FastMoveTo`, `SafeApproachSpeed`, `Position`, `position3`, `StayForDecision`, `Angle`, `Angle_V`
- 内联 `vTable`, `tTable`, `PI` 为局部 readonly 常量
- 维护独立的跨帧字段（`lockBallId`, `filterBall*` 等）作为 Strategy 类的私有成员

### 极限救援项目（StrategyExtremeRescue）
同上，额外加入：
- `LockCarryingBall` 逻辑（BaseAction.cs:7283，依赖 lockBallId 实例字段，直接复制逻辑后用局部字段）
- `IsInStalemate` + `UnstickFish` 逻辑（BaseAction.cs:6923/6961，防卡死）
- `UpdateBallFilter` + `PredictBallPosition` 可选（BaseAction.cs:7356/6733，α-β 滤波预测球位置，提升运球精度）

### 隔离原则
**每个项目维护自己独立的 `GlobalVar`-等效字段**（直接作为 Strategy 类的私有/静态成员），不引用另一个项目的 GlobalVar 类。两个项目运行时完全独立：各自的 decisions、Bringing_BallID、lockBallId、状态机变量均不共用。
