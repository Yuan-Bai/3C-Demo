# 个人简历

## 基本信息

- 姓名：请填写
- 手机：请填写
- 邮箱：请填写
- 所在城市：请填写
- 求职意向：Unity 游戏客户端开发 / Gameplay 程序 / 3C 角色控制与动画状态机方向
- 作品链接：请填写 GitHub / Gitee / 视频链接 / 网盘构建包链接

## 教育背景

**请填写学校名称，211 本科**  
请填写专业名称 | 本科 | 2020.09 - 2024.06

- 通过大学英语四级、六级
- 获得大学生数学竞赛省级奖项
- 相关基础：C#、Unity、数据结构、线性代数、概率统计、基础算法

## 个人优势

- 211 本科背景，数学基础较好，具备较强的逻辑分析和自学能力。
- 英语四、六级通过，可阅读 Unity Manual、Package 文档和英文技术资料。
- 近两年持续围绕 Unity 游戏客户端方向自学和实践，当前重点项目为第三人称动作移动 Demo。
- 对角色移动、状态切换、动画事件、Root Motion、输入缓冲、坡面处理等 Gameplay 细节有持续实现和调试经验。

## 项目经历

### FlowMotion - Unity 第三人称动作移动 Demo

独立项目 | Unity 2022.3.62f3c1 / URP / C# / Input System / CharacterController / Animator / Cinemachine  
项目时间：2025.05 - 至今

**项目简介**

独立开发的第三人称动作角色移动原型，目标是验证动作游戏中角色移动手感、动画状态衔接、Root Motion 接管和 Camera-relative 控制。项目参考开放世界动作游戏的基础移动体验，当前聚焦移动、停止、Dash、跳跃、下落、落地、坡面等角色运动链路。

**负责内容**

- 搭建 `PlayerInputReader`、`PlayerContext`、`PlayerMotor`、`PlayerAnimationBridge`、`StateMachine<T>` 等核心模块，拆分输入、运动、动画桥接和状态切换职责。
- 实现基于 Camera-relative 的 WASD 移动，支持 Idle / Walk / Run / Sprint / SprintImpulse 等地面移动状态。
- 实现 Dash、MoveStop、Turnback、Jump、DoubleJump、Fall、Land 等动作状态，并通过状态机控制进入、退出和动画完成回调。
- 使用 Unity Input System 读取移动、镜头、缩放、跳跃、Dash、Walk/Run 切换等输入，并记录按下帧、松开帧和长按时间。
- 通过 `PlayerAnimationBridge` 连接 Gameplay 状态机和 Animator，处理 Animator 参数、CrossFade、StateMachineBehaviour 事件、动作请求优先级和超时保护。
- 在 Dash、Stop、Turnback、Jump 等动作中按 XZ、Y、Rotation 维度接管 Root Motion，并在动作退出时回写速度，降低动作结束后的位移断层。
- 使用 `GroundDetector` 检测地面、法线、坡度、落点高度和陡坡状态，配合 `PlayerMotor` 完成坡面投影、重力和陡坡滑落。
- 将移动速度、加速度、旋转速度、Jump Buffer、Coyote Time 等参数收敛到 `PlayerLocomotionConfig`，便于后续调参和测试。

**技术亮点**

- Gameplay 状态机与 Animator Controller 解耦：Animator 负责表现和事件，Gameplay 状态机负责真实逻辑状态。
- Root Motion 与 CharacterController 混合：常规移动由代码控制，关键动作由动画接管，退出时回写速度保持连续性。
- 动作窗口设计：Stop、Dash 后衔接、落地等通过动画事件窗口控制，减少状态抢占和动作打断混乱。
- 输入容错：实现 Jump Buffer 与 Coyote Time，提高跳跃手感和输入响应稳定性。
- 坡面适配：根据地面法线与坡度处理可行走坡面和陡坡滑落，降低角色漂浮、穿地和坡面抖动风险。

**当前成果**

- 已完成基础第三人称移动、Dash、跳跃、下落、落地、移动停止和坡面处理原型。
- 已整理项目架构和手动验收清单，计划补充演示视频、自动化测试和专用测试场景。

## 技能清单

- Unity：生命周期、Prefab/Scene 基础、Animator、StateMachineBehaviour、CharacterController、Physics 检测、ScriptableObject、Input System、Cinemachine、URP 基础。
- Gameplay：角色状态机、移动控制、Root Motion 混合、动作窗口、输入缓冲、土狼时间、坡面检测、基础手感调试。
- C#：面向对象、泛型、委托/事件、协程、集合、异常处理、基础设计模式。
- 工程能力：Git 基础、模块拆分、可调参数配置、手动验收清单、问题复盘。
- 英语与基础：英语四级、六级；数学竞赛省奖；可阅读英文技术文档。

## 空窗说明

2024 年毕业后未进入正式全职岗位，主要时间用于 Unity 游戏客户端方向的自学、项目实践和求职准备。当前以 FlowMotion 项目为主线，系统补齐 Unity Gameplay、C# 基础、数据结构算法和游戏客户端面试知识。

## 自我评价

我希望从 Unity 游戏客户端 / Gameplay 初级岗位开始，优先参与角色控制、3C、动作状态机、玩法原型或客户端基础模块开发。虽然暂无商业项目经验，但我能持续投入、独立拆解问题并完成可运行 Demo；目前最适合承担明确边界内的 Gameplay 功能实现、调试和迭代工作。

## 面试可展开问题

- 为什么 Gameplay 状态机不直接依赖 Animator Controller？
- Root Motion 和 CharacterController 同时使用时如何避免双位移？
- Dash、Stop、Jump 等动作如何处理进入窗口、退出窗口和状态抢占？
- Jump Buffer 和 Coyote Time 如何提升手感？
- 坡面检测为什么使用 SphereCast？如何处理可行走坡面和陡坡？
- 如果后续加入攻击、受击和敌人，状态机应该如何扩展？

## 后续需补充

- 姓名、联系方式、城市、学校、专业、GitHub/Gitee、演示视频、构建包链接。
- FlowMotion 的 60 秒展示视频和 3-5 张动图。
- 2-3 个 EditMode 测试或状态机逻辑测试结果。
- 一段 100 字以内的项目总结，用于 BOSS 直聘 / 拉勾 / 猎聘开场介绍。

