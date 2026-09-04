# Task 4 — 生存挑战策略实现报告

> 实现时间：2026-08-22  
> 文件：`StrategyFightForLive\StrategyFightForLive.cs`

---

## 1. 状态机说明

### 1.1 角色切换（进攻/防守）

策略通过帧计数器估算比赛半场时间，决定当前角色：

```
halfFrames = (300 * 1000) / msPerCycle   // 5分钟 = 300秒
isFirstHalf = (frameCount <= halfFrames)
isAttacker = (teamId == 0) ? isFirstHalf : !isFirstHalf
```

- 上半场：teamId=0 为进攻方，teamId=1 为防守方
- 下半场（frameCount > halfFrames）：双方角色交换
- `frameCount` 从第1帧开始递增，每帧 +1，首次调用时读取 `msPerCycle`

### 1.2 进攻方逻辑（isAttacker == true）

| 鱼编号 | 角色 | 行为 |
|--------|------|------|
| 1号（索引0）| 捉捕手（特殊鱼） | 追击对方最近的躲避手（2/3/4号），调用 MoveTo + AvoidObstacles，VCode ≤ 15 |
| 2/3/4号（索引1..3）| 不上场 | 停靠在场地左侧边缘 (-1400, -900/0/900)，VCode ≤ 8 |

捉捕手目标选择：遍历对方 1..3 号鱼（常规鱼/躲避手），取距离最近者。

### 1.3 防守方逻辑（isAttacker == false）

| 鱼编号 | 角色 | 行为 |
|--------|------|------|
| 1号（索引0）| 防御手（特殊鱼） | 移动到对方捉捕手与己方最近躲避手连线的 40% 处（拦截点），形成阻挡，VCode ≤ 15 |
| 2/3/4号（索引1..3）| 躲避手（常规鱼） | 巡游分配的场地角落，危险时逃离，VCode ≤ 8 |

**躲避手角落巡游：**
- 每条躲避手分配不同初始角点（逆时针循环：左上→右上→右下→左下）
- 到达角点 150mm 内时切换到下一角点
- 若对方捉捕手在目标角点 500mm 内，改去对角角点
- 若与捉捕手距离 < 400mm（危险），立即逃离（沿远离方向 600mm）

---

## 2. 工具函数实现

| 函数 | 说明 |
|------|------|
| `Distance(x1,z1,x2,z2)` | 欧氏距离，纯算术 |
| `GetAngleRad(x1,z1,x2,z2)` | 方向角弧度，`Math.Atan2(dz,dx)` |
| `FormatAngleRad(ang)` | 规整到 (-π, π]，无外部依赖 |
| `Clamp(val,min,max)` | 值域限制 |
| `MoveTo(fish,tx,tz,maxVCode,ref dec)` | Bang-Bang 速度控制 + 比例转向，按 maxVCode 限速 |
| `AvoidObstacles(...)` | 路径穿越障碍物检测，侧向偏移目标点 |

### MoveTo TCode 符号约定

场地坐标 Z 轴朝下，Atan2(dz,dx) 正值表示目标在鱼右侧（顺时针）：
- angErr > 0 → 右转 → TCode > 7（tTable 正角速度）
- angErr < 0 → 左转 → TCode < 7（tTable 负角速度）

### VCode / TCode 范围验证

| 参数 | 有效范围 | 代码保证 |
|------|--------|---------|
| VCode（特殊鱼1号）| 1..15 | `maxVCode=15`，`if(vCode>15) vCode=15` |
| VCode（常规鱼2/3/4号）| 1..8 | `maxVCode=8`，`if(vCode>8) vCode=8` |
| TCode | 0..14 | 硬编码选项：0,3,5,7,9,11,14 |

---

## 3. 构建结果

**命令：**
```
& "D:\microsoft visual studio\MSBuild\Current\Bin\MSBuild.exe" 
  "E:\progaram\URWPGSim2D\Strategy\StrategyFightForLive\StrategyFightForLive.csproj"
  /p:Configuration=Release /v:m
```

**结果：**
- Exit code: 0
- 错误数：**0**
- 警告数：4（均为 MSB3270 架构不匹配警告，pre-existing，与代码无关）
- 输出 DLL：`E:\progaram\URWPGSim2D\Strategy\StrategyFightForLive\bin\Release\StrategyFightForLive.dll`

---

## 4. 已修复的类型问题

| 问题 | 原因 | 修复 |
|------|------|------|
| `Team` 缺少类型参数 | URWPGSim2D.Common 中 `Team` 是泛型 `Team<TFish>` | 改为 `Team<RoboFish>` |
| `RectangularObstacle.CenterPositionMm` 不存在 | 该类继承自 `RectangularStatic`，中心坐标字段名为 `PositionMm` | 改为 `obs.PositionMm.X/Z` |

---

## 5. 外部依赖

- 无 GlobalVar 引用
- 无额外 using（仅 System, System.Collections.Generic, Microsoft.Xna.Framework, URWPGSim2D.Common, URWPGSim2D.StrategyLoader）
- 所有状态（frameCount, evaderCorner, msPerCycle）作为 Strategy 类私有成员维护
