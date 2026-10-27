# 沙盒参数完整参考

本表按 v1.0.1 的 `source/sandbox.tsv` 生成，描述该版本界面与校验规则，不代表后续游戏版本的最新规则。数值小数点使用英文句点；布尔值为 True / False。无完整上下界的整数项仅检查 32 位整数类型。

| 分组 | 键名 | 名称 | 默认值 | 可选值或校验范围 | 说明 |
| --- | --- | --- | --- | --- | --- |
| 世界 | `GameDifficulty` | 游戏难度 | 1 | 1=标准 / 2=困难 / 3=末日 | 影响敌人数量、反应和攻击性。 |
| 世界 | `HardcoreMode` | 硬核模式 | False | True / False | 角色只有一次生命，并强制末日难度。 |
| 世界 | `LootRespawnEnabled` | 资源重新生成 | False | True / False | 不包含家具；可能影响资源平衡。 |
| 世界 | `PowerSocketsOffAtNight` | 夜间插座断电 | True | True / False | 关闭后，夜间插座仍有电。 |
| 世界 | `DayNightCycleState` | 昼夜模式 | 0 | 0=正常循环 / 1=永远白天 / 2=永远夜晚 | 固定昼夜可能影响特定事件与物品。 |
| 世界 | `DayNightCycleSpeedMultiplier` | 昼夜流速 | 1 | 0.1–3 | 数值越大，游戏内时间越快。 |
| 世界 | `WeatherFrequency` | 天气频率 | 3 | 1=从不 / 2=偶尔 / 3=正常 / 4=频繁 / 5=每天 | 天气仍受世界进度限制。 |
| 世界 | `SinkRefillRate` | 水槽补水速度 | 1 | 0–10 | 0 表示不补水。 |
| 世界 | `FoodSpoilSpeedMultiplier` | 食物腐败速度 | 1 | 0–2 | 0 表示停止腐败。 |
| 世界 | `RefrigerationEffectivenessMultiplier` | 冷藏效率 | 1 | 0–2 | 调整保温与冷藏效果。 |
| 世界 | `StorageByTag` | 容器物品类型限制 | True | True / False | 例如冰箱只允许放入食物。 |
| 世界 | `StructuralSupportLimit` | 结构堆叠限制 | 5 | 32 位整数；未定义完整游戏范围 | 0 表示取消限制；官方未给出完整数值范围。 |
| 世界 | `BridgeSupports` | 桥梁支撑要求 | 2 | 0=无需额外支撑 / 1=较少支撑 / 2=默认支撑 | 控制桥梁所需支撑。 |
| 世界 | `HomeWorlds` | 允许家园世界 | True | True / False | 允许进入免费或 DLC 家园世界。 |
| 世界 | `InvisibleRadiation` | 隐藏辐射视觉效果 | False | True / False | 开启后需要使用盖革计数器判断辐射。 |
| 世界 | `TaintedSinkWater` | 污染水槽水源 | False | True / False | 将原本干净的水槽水变为污染水。 |
| 世界 | `RadiationDealsDamage` | 辐射直接造成伤害 | False | True / False | 提高辐射区域危险程度。 |
| 敌人 | `EnemySpawnRate` | 敌人重生速度 | 1 | 0.5–3 | 数值越大，敌人重生越快。 |
| 敌人 | `EnemyHealthMultiplier` | 敌人生命倍率 | 1 | 0.75–3 | 调整敌人的生命值。 |
| 敌人 | `EnemyPlayerDamageMultiplier` | 敌人对玩家伤害 | 1 | 0.25–3 | 调整玩家受到的敌人伤害。 |
| 敌人 | `EnemyDeployableDamageMultiplier` | 敌人对建筑伤害 | 1 | 0–5 | 0 表示不造成此类伤害。 |
| 敌人 | `DetectionSpeedMultiplier` | 敌人发现速度 | 1 | 0.1–3 | 数值越大，敌人越容易快速发现玩家。 |
| 敌人 | `EnemyAccuracy` | 敌人精准度 | 2 | 0=极低 / 1=低 / 2=默认 / 3=高 / 4=精准 | 影响敌人的射击精度。 |
| 敌人 | `ApocalypticAbilities` | 启用末日敌人能力 | False | True / False | 非末日难度也启用这些能力。 |
| 敌人 | `MaximizeEnemySpawns` | 启用所有敌人生成点 | False | True / False | 不受难度或玩家人数影响。 |
| 玩家 | `DamageToAlliesMultiplier` | 友军伤害倍率 | 0.5 | 0–3 | 0 表示关闭友军伤害。 |
| 玩家 | `HungerSpeedMultiplier` | 饥饿速度 | 1 | 0–2 | 数值越大，进食需求越频繁。 |
| 玩家 | `ThirstSpeedMultiplier` | 口渴速度 | 1 | 0–2 | 数值越大，饮水需求越频繁。 |
| 玩家 | `FatigueSpeedMultiplier` | 疲劳速度 | 1 | 0–2 | 调整角色疲劳增长速度。 |
| 玩家 | `ContinenceSpeedMultiplier` | 如厕需求速度 | 1 | 0–2 | 调整角色如厕需求增长速度。 |
| 玩家 | `BonusPerkPoints` | 额外特性点数 | 0 | 32 位整数；未定义完整游戏范围 | 可为负数；官方未给出完整数值范围。 |
| 玩家 | `PlayerXPGainMultiplier` | 经验获取倍率 | 1 | 0–3 | 0 表示无法获得技能经验。 |
| 玩家 | `ItemStackSizeMultiplier` | 物品堆叠倍率 | 1 | 1–30 | 不影响原本不可堆叠的物品。 |
| 玩家 | `ItemWeightMultiplier` | 物品重量倍率 | 1 | 0–5 | 数值越小，物品越轻。 |
| 玩家 | `ItemDurabilityMultiplier` | 物品耐久倍率 | 1 | 0.1–10 | 调整物品最大耐久。 |
| 玩家 | `DurabilityLossOnDeathMultiplier` | 死亡耐久损失 | 0.1 | 0–1 | 0.1 表示损失 10% 耐久。 |
| 玩家 | `ShowDeathMessages` | 显示死亡消息 | True | True / False | 向其他玩家显示死亡通知。 |
| 玩家 | `AllowRecipeSharing` | 允许配方分享 | True | True / False | 允许玩家互相分享配方。 |
| 玩家 | `AllowPagers` | 允许呼叫器 | True | True / False | 控制呼叫器是否可用。 |
| 玩家 | `AllowTransmog` | 允许护甲幻化 | True | True / False | 控制护甲外观修改。 |
| 玩家 | `DisableResearchMinigame` | 跳过研究小游戏 | False | True / False | 开启后直接解锁研究配方。 |
| 玩家 | `DeathPenalties` | 死亡物品惩罚 | 1 | 0=保留全部 / 1=保留装备与快捷栏 / 2=仅保留快捷栏 / 3=仅保留装备 / 4=掉落全部 / 5=销毁全部 | 也会影响脱困功能后的物品处理。 |
| 玩家 | `FirstTimeStartingWeapon` | 首次出生武器 | 0 | 0=无 / 1=简易小刀 / 2=桌腿 / 3=厨刀 / 4=初始警棍 / 5=管制棍 / 6=随机 | 在职业初始物品之外额外发放。 |
| 玩家 | `HostAccessPlayerCorpses` | 管理员可访问死亡背包 | True | True / False | 实际行为可能受其他沙盒选项影响。 |
| 玩家 | `AllowCharacterReset` | 允许角色重置 | True | True / False | 允许使用游戏内重置职业与技能的机制。 |
| 玩家 | `BaseInventorySize` | 基础背包容量 | 12 | 32 位整数；未定义完整游戏范围 | 未装备背包时的容量；官方未给出完整数值范围。 |
| 玩家 | `PlayerFurnitureDestruction` | 家具损坏后直接摧毁 | False | True / False | 关闭时，家具耗尽耐久后打包。 |
| 玩家 | `AllowIronMode` | 允许铁人职业 | True | True / False | 需要启用硬核模式。 |
