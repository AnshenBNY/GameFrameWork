# My project (5) - 架构评估与开发指南

本 README 面向当前工程阶段（Unity TPS 原型 + GameFramework 核心脚本），目标是让后续开发更容易上手，并保证代码可维护、可扩展。

---

## 1. 项目现状概览

当前工程已具备以下核心能力：

- 关卡运行：`LevelDefinition + LevelManager` 支持地图、触发器、怪物/物资刷出与阶段推进。
- 战斗闭环：`ShootBehaviour / BasicEnemyController -> CombatDamageManager -> ActorStatsComponent`。
- 技能系统：`SkillDefinition + SkillCaster + SkillLoadout`，支持单体/范围、伤害/治疗、冷却。
- 阵营与目标筛选：`FactionComponent + CombatTargetingUtility`。
- UI 基础：`UIRootInstaller + UIManager + GameplayHintUI + WeaponUIManager`。
- 事件通信：`GameEventBus`（全局）与 `UIEventChannel`（UI 专用）。
- 依赖注入：`RuntimeContext` 集中持有 Player / UI / Level 引用。
- TPS 脚本：已并入 `GameFramework.TPS.*` 命名空间（相机模块为 `GameFramework.TPS.ThirdPersonCamera`，避免与 `UnityEngine.Camera` 冲突）。

当前处于**架构收口后的可扩展原型阶段**：主干链路完整，运行时 `Find/Tag` 查找已基本移除。

---

## 2. 目录与模块职责

核心代码位于 `Assets/GameFramework/Scripts`：

- `Core`
  - `GameBootstrap`：启动入口，拉起首关。
  - `RuntimeContext`：运行时依赖注入（Player / UIManager / LevelManager / PickupHud）。
  - `GameEventBus`：跨系统事件总线。
  - `CursorStateController`：鼠标锁定控制。
- `Level`
  - `LevelDefinition / LevelPhaseData`：关卡静态配置（ScriptableObject）。
  - `LevelManager`：编排层，协调下列三个子模块。
  - `LevelSpawnService`：地图 / 物资 / 怪物 / 触发器生成。
  - `LevelPhaseFlow`：阶段状态机（到达触发点、防守波次、结算）。
  - `LevelRuntimeRegistry`：运行时对象登记、死亡追踪、清理。
  - `LevelRuntimeTrigger`：触发器转事件。
  - `LevelResultListener`：胜负监听（玩家死亡 -> 失败）。
  - `LevelDebugHud`：运行时调试 HUD。
- `Combat`
  - `CombatDamageManager`：统一伤害入口与结算。
  - `CombatRuntimeConfigProvider + CombatDamageConfig`：运行时配置注入。
  - `CombatRaycastUtility`：射线命中过滤。
  - `HitEffectManager`：命中特效统一处理。
  - `FactionComponent / CombatLayers / CombatTargetingUtility`：敌我与命中规则。
- `Stats`
  - `ActorStatsComponent`：生命、伤害承载、状态效果聚合。
  - `AttributeSet / StatusEffectData`：属性与 Buff/Debuff 数据。
- `Skill`
  - `SkillDefinition`：技能数据定义。
  - `SkillCaster`：技能施放逻辑与冷却。
  - `SkillLoadout`：技能槽装配。
- `AI`
  - `BasicEnemyController`：目标解析（经 `RuntimeContext`）、追击、攻击、动画驱动。
- `TPS`
  - `Player`（`GameFramework.TPS.Player`）：行为树式玩家控制（Move/Aim/Shoot/Cover）。
  - `ThirdPersonCamera`（`GameFramework.TPS.ThirdPersonCamera`）：第三人称相机。
  - `Weapon`（`GameFramework.TPS.Weapon`）：武器拾取与 HUD 联动。
  - `UI`（`GameFramework.TPS.UI`）：HUD 与提示 UI。
  - `Demo / FX`：示例与特效辅助脚本。
- `Editor`
  - 运行模板、图层、预制体辅助工具。

---

## 3. 主体运行线路（主干时序）

### 3.1 启动到关卡运行

1. 场景启动（BuildSettings 首场景：`Assets/Scenes/SampleScene.unity`，或使用 `Tools/GameFramework/Create Runtime Scene Template` 生成模板）。
2. `RuntimeContext`（`DefaultExecutionOrder -100`）解析 Player / LevelManager / UI 引用。
3. `GameBootstrap.Start()` 或 `LevelManager.Start()` 调用 `StartLevel()`。
4. `LevelManager` 编排：
   - `LevelRuntimeRegistry.ClearAll()` 清理旧对象；
   - `LevelSpawnService` 实例化 map / item / monster / trigger；
   - `LevelPhaseFlow.EnterPhase(0)` 进入首阶段；
   - 广播 `GameEventBus.RaiseLevelStarted(levelId)`。

### 3.2 战斗伤害链路

1. 玩家射击 / 敌人攻击 -> 创建 `DamageContext`。
2. `CombatDamageManager.ApplyDamage()` 统一结算。
3. `ActorStatsComponent.ApplyFinalDamage()` 扣血并派发 `OnDamaged / OnDied`。
4. 死亡时 `GameEventBus.RaiseActorDied(actor)`。
5. `LevelRuntimeRegistry` 更新怪物列表 -> `LevelPhaseFlow` 推进波次/阶段。
6. `LevelResultListener` 检测玩家死亡 -> `FailLevel()`。

### 3.3 UI 链路

1. `UIRootInstaller` 自动创建 PickupHUD / GameplayHintHUD / ScreenHUD。
2. `UIManager` 与 `PickupHUD` 注册到 `RuntimeContext`。
3. `InteractiveWeapon` / `HintManagement` 从 `RuntimeContext` 获取 Player 与 HUD。
4. 提示消息走 `UIEventChannel` -> `GameplayHintUI`。

### 3.4 RuntimeContext 注入关系

```
GF_Root
├── RuntimeContext  ← Player, LevelManager（Inspector 绑定）
├── LevelManager    ← 编排 Spawn / Phase / Registry
├── LevelResultListener
└── LevelDebugHud

UIRoot
├── UIManager       ← 启动时 RegisterUiManager
├── UIRootInstaller ← 创建 UI 并 RegisterPickupHud
└── ScreenHUD / WeaponUIManager

Player（Tag: Player）
└── ShootBehaviour / BasicBehaviour / ...

InteractiveWeapon / HintManagement / BasicEnemyController
└── 只读 RuntimeContext.Instance，不做 Find/Tag
```

---

## 4. 通信方式

| 方式 | 用途 | 示例 |
|------|------|------|
| `GameEventBus` | 跨系统解耦 | 关卡开始/完成、实体死亡、枪声告警 |
| `UIEventChannel` | UI 领域事件 | 显示/隐藏提示文本 |
| `RuntimeContext` | 依赖注入 | Player、UIManager、LevelManager |
| 直接调用 | 同链路强关联 | `SkillCaster -> CombatDamageManager` |
| ~~Find/Tag~~ | 已收敛 | 仅 `RuntimeContext.ResolveReferences()` 启动阶段 Tag 兜底 |

---

## 5. GameEventBus 事件契约

| 事件 | 参数 | 发布方 | 订阅方 |
|------|------|--------|--------|
| `OnLevelStarted` | `levelId` | `LevelManager` | `LevelDebugHud` |
| `OnLevelCompleted` | `levelId` | `LevelManager` | `LevelDebugHud` |
| `OnLevelFailed` | `levelId` | `LevelManager` | `LevelDebugHud` |
| `OnActorDied` | `GameObject actor` | `ActorStatsComponent` | `LevelManager`, `LevelResultListener` |
| `OnLevelTriggerEntered` | `triggerId, actor` | `LevelRuntimeTrigger` | `LevelManager` |
| `OnMonsterSpawned` | `GameObject monster` | `LevelSpawnService` | （可扩展：AI 管理器） |
| `OnGunshotAlert` | `Vector3 origin` | `ShootBehaviour` | （可扩展：AI 警戒） |

---

## 6. 各模块开发指南

### 6.1 新增关卡

1. 创建 `LevelDefinition`，配置 map / spawn points / phases。
2. 场景 `GF_Root` 上确保有 `RuntimeContext` + `LevelManager`，并绑定 Player 与 LevelDefinition。
3. 触发到达类阶段：在 `LevelPhaseData` 设置 `requiredTriggerId`。
4. 联调：挂载 `LevelDebugHud` 观察 phase 与怪物列表。

**扩展刷怪逻辑**：改 `LevelSpawnService`，不要直接改 `LevelManager` 编排层。

**扩展阶段规则**：改 `LevelPhaseFlow`，通过回调与 `LevelManager` 衔接。

### 6.2 新增敌人

1. 预制体包含 `ActorStatsComponent`、`FactionComponent(Enemy)`、`BasicEnemyController`。
2. 缺组件时 `LevelSpawnService.EnsureMonsterRuntimeComponents()` 会补齐，但建议在预制体上显式配置。
3. AI 目标默认从 `RuntimeContext.PlayerTransform` 获取，无需 Tag 查找。

### 6.3 新增技能

1. 创建 `SkillDefinition`，配置目标模式与阵营过滤。
2. 挂到 `SkillLoadout`，由行为层调用 `SkillCaster.TryCast()`。
3. 伤害/治疗分别走 `CombatDamageManager` / `ActorStatsComponent`。

### 6.4 新增武器

1. 配置 `InteractiveWeapon`（type、mode、damage、弹药、sprite）。
2. 模型需有 `muzzle` 子节点。
3. 拾取依赖 `RuntimeContext`（Player + WeaponUIManager + PickupHud）。

### 6.5 新增 UI

1. 提示类走 `UIEventChannel`。
2. 新 UI 组件在 `OnEnable/OnDisable` 订阅/反订阅。
3. 通过 `UIManager` 或 `RuntimeContext` 注册，避免 `FindObjectOfType`。

### 6.6 新增状态效果

1. 扩展 `StatusEffectData` 与 `ActorStatsComponent.RecalculateAttributes()`。
2. 保持属性重算唯一入口。

---

## 7. 场景配置清单

使用自定义场景时，至少确认：

| 对象 | 必需组件 | 绑定项 |
|------|----------|--------|
| `GF_Root` | `RuntimeContext`, `LevelManager`, `GameBootstrap` | Player, LevelManager, LevelDefinition |
| `UIRoot` | `UIManager`, `UIRootInstaller` | （自动创建子 UI） |
| `Player` | `BasicBehaviour`, `ShootBehaviour`, `ActorStatsComponent` | Tag = Player |
| `Main Camera` | `ThirdPersonOrbitCam` | player 引用 |

快捷生成：`Tools -> GameFramework -> Create Runtime Scene Template`

---

## 8. 架构演进状态

### 已完成（M1）

- [x] TPS 脚本并入 `GameFramework.TPS.*` 命名空间
- [x] `RuntimeContext` 注入 Player / UI / Level
- [x] UI / Weapon / Demo / Level / AI 移除运行时 Find/Tag
- [x] `LevelManager` 拆分为 Spawn / PhaseFlow / Registry

### 待做（M2 / M3）

- [ ] 接口边界（`IDamageable`、`ILevelFlow` 等）
- [ ] 统一 `GameConfig` 配置入口
- [ ] 技能效果管线扩展（位移、控制）
- [ ] AI 状态机层（巡逻/脱战）
- [ ] 核心模块测试 + asmdef 分层

---

## 9. 开发约定

- 新代码放入 `GameFramework.*` 命名空间。
- 跨模块引用优先 `RuntimeContext` 或 `GameEventBus`，禁止业务层 `GameObject.Find`。
- 关卡扩展改 `LevelSpawnService` / `LevelPhaseFlow`，不改 `LevelManager` 公共 API。
- 数据用 ScriptableObject，运行时只读。
- 改动关卡/战斗/技能链路后做 PlayMode 回归。
