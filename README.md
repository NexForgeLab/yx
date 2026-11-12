# 余响测绘

竖屏、单手、一次只靠自己拍出的声呐看见路的水道测绘。这是测试期的可玩垂直切片：免费、无广告、无内购、不联网、不上架。

> 每拍一次声呐，路就亮一截，沉睡的东西也会醒一分。

## 用哪个 Unity

- 编辑器：**Unity 6000.3.28f1**（Unity 6.3 LTS）
- 变更集：**8f082d2fe476**
- 记录在 `ProjectSettings/ProjectVersion.txt`
- 渲染：默认内置管线 + 着色器 `Yx/Block`，打开就能 Play。需要 2D URP 时，菜单选 **余响测绘 → 启用 2D URP**（包 `com.unity.render-pipelines.universal` 17.3.0）
- 输入：旧输入管理器 + 自绘虚拟摇杆。没有引用 Input System 包
- 画面：竖屏，参考分辨率 1080×1920，Gamma，Android 只打 ARM64 / IL2CPP，最低 API 25

## 怎么打开并开一局

1. 用上面的编辑器打开本仓库根目录。
2. 等 Package Manager 把 `com.unity.ugui` 和 URP 拉完。包还在导的时候也可以先 Play，世界用内置着色器。
3. 打开 `Assets/Scenes/SurveyOffice.unity`。编辑器里直接按 Play 也会从这一场开始（菜单 **余响测绘 → 从测稿室开局** 可关掉）。
4. Game 视图选竖屏 **1080×1920**。默认几乎全黑，这是对的。
5. 测稿室：**开始测绘** / **继续测绘** / **上次残页** / **设置**。
6. 水道里：左摇杆走，右下 **拍声**，右上 **憋声**。设置里可以换成左手。

编辑器键盘：WASD 或方向键移动，空格或 J 拍声，左 Shift 或 K 憋声，Esc 暂停。Esc 不会退出。

点开 Game 视图以外的地方会失焦：这一局自动暂停，并写入本地存档。回到游戏后点 **继续测绘**。杀进程后再进测稿室，用 **继续测绘** 接着画。

## 一局里有什么

- 4 间水道房，每间由 2 或 3 段预制舱室竖向接上，外加竖井。闸门测绘到 70 打开。
- 第 5 间是无名厅，头目 **对图者**。几乎看不见，拍到才露出弱点，并朝声源直线冲。打中 3 次弱点即胜，没有第二血条。
- 三种普通生物（寻声鱼、壁苔、空腔）+ 深物 + 对图者。
- 扰动用涟漪环和档位字（安静 / 警醒 / 苏醒）表示，不给玩家看原始数字。
- 空气默认 10:00，设置里可改 8 / 10 / 12 分钟。已测绘且还在声呐余辉里的格子按 1 倍掉空气，黑暗里 1.5 倍。三格生命。
- 12 张回声模组，每张最多 3 级。选牌每次 3 张：过一间房一张，每 8 次有效拍声再一张。**群噪** 是唯一会自己拍的牌，牌面上写着扰动会涨得很快。
- 开局池子 6 张。每成功 3 局，永久多放进 1 张，直到 12。残页只记走到哪一间、扰动档位、死因。
- 扰动还在「安静」时，每局一次免费重试。没有广告，也不付钱。
- 第一局前 30 秒有教学。第一拍一定会打到壁苔。
- 结算三行：死因或测稿完成、用得最多的模组、下一局一句提示。

设置还有：震动、左手镜像、余辉粒子、画质（标准 / 省电，都锁 30 帧；省电用 1 像素格子并关掉粒子）。

## 打 Android 包

本环境没有 Unity 编辑器，没有打出 APK。在本机：

1. File → Build Settings，平台选 Android。两场都在列表里，`SurveyOffice` 为 0。
2. Player Settings 已是竖屏、ARM64、IL2CPP、包名 `com.nexforgelab.yuxiangcehui`、最低 SDK 25、不申请网络权限。
3. 不要打开 Run In Background，也不要开 Predictive Back。返回键在局里是暂停。
4. Build 出 APK 即可。不要上架。

## 规则测试

模拟层是纯 C#，不依赖 Unity：

```bash
dotnet test Tests/Yx.Sim.Tests/Yx.Sim.Tests.csproj
```

## 目录

- `Assets/Yx/Runtime/Sim` 规则与房间拼接
- `Assets/Yx/Runtime/Run` 一局流程
- `Assets/Yx/Runtime/UI` 测稿室与控件
- `Assets/Yx/Runtime/View` 色块、声呐、程序音
- `Assets/Yx/Runtime/Rooms` 舱室预制体读取
- `Assets/Yx/Runtime/Save` 本地 `yuxiang_save.json`
- `Assets/Yx/Resources/SegmentPrefabs` 舱室预制体（运行时盖过内置表）
- `Assets/Scenes/SurveyOffice.unity` 测稿室
- `Assets/Scenes/Waterway.unity` 水道

美术是程序生成的色块和短线，字体是 OFL 的 Noto Sans SC（见 `Assets/Yx/Resources/Fonts/OFL.txt`）。没有第三方素材包。

## 和设计稿之间还缺什么

- 没有在真机上打过 APK，也没有测过具体机型的 30 帧。
- 画面和声音都是占位：色块、短线、合成鼓点。没有正式立绘，也没有配音。
- 开局只能见到池子里已解锁的模组。一张新档不会在选牌里同时出现全部 12 张。
- 没有 iOS 工程、没有商店图、没有账号、广告、内购、抽卡、联机、无尽模式。
- 中文若显示成方框，确认字体 `Fonts/NotoSansSC-Regular` 已导入，并且 Game 视图不是被别的字体盖掉。
