# C&C 改变世界编辑器 (WAE)
[English](./README.md) · **简体中文**

一款基于现代框架的开源地图和场景编辑器，适用于以下游戏

- 命令与征服：红色警戒2 尤里的复仇
- 命令与征服：泰伯利亚之日
- [命令与征服：泰伯利亚黎明 (DTA)](https://www.moddb.com/mods/the-dawn-of-the-tiberium-age)

## 制作动机

改变世界编辑器是为经典《命令与征服》第二代的游戏设计的全新地图编辑器，旨在取代早在 2000 年初的TS/RA2模组社区开发的旧 FinalSun/FinalAlert2 (FS/FA2) 地图编辑器。

为了让现有的地图作者熟悉，编辑器的设计遵循了 FS/FA2 的UI设计，但进行了现代化和改变，使编辑器的使用更加流畅和高效。

## 项目状态

编辑器包括了几乎所有在FS/FA2中存在的工具以及大量的新工具。许多熟悉的工具都进行了显著的质量改进。这些改进使得使用WAE创建地图比使用旧的FS/FA2地图编辑器更加高效。

新的功能和有用的特性正在不断添加，以使地图制作体验更加流畅和高效。包括诸如缩放功能、光照预览、大大改进的触发器编辑器界面，以及用于快速详细描述大面积区域的地形生成器。

## 系统要求

WAE在下卡==显卡方面比FS/FA2要求更高，但也可以更有效地利用现代硬件，这意味着如果你有一台相当强力的电脑，WAE可以在更好的图形质量下实现更流畅的性能。

要运行WAE，你的系统需要支持以下内容

- .NET 8 桌面运行时
- 兼容 DirectX 11 的GPU\*。建议至少有 2GB 的显存，尽管WAE可以在更少的显存上运行。
- 64 位操作系统。我们不提供 32 位版本，你可以使用源代码自己编译一个。

> \[!NOTE]
>
> 一些Intel的GPU，如HD Graphics 4000，由于存在驱动问题，无法运行WAE。建议使用 AMD 或 Nvidia 的GPU来运行 WAE，更加新的 Intel GPU 经过测试也可以工作。

## 下载

对于大多数终端用户，建议你下载我们的最新官方版本：https://github.com/Rampastring/WorldAlteringEditor/releases

开发版本可以通过我们的自动构建工作流获取，该工作流在每次推送新的提交时运行。你可以从构建工件中下载编辑器：https://github.com/Rampastring/WorldAlteringEditor/actions

如果你正在为泰伯利亚黎明制作地图，编辑器会与模组一起打包。

## 如何贡献

只要你的贡献做得好，我们认为它对编辑器的大部分用户有益，我们就很乐意接受。

在创建拉取请求时，请遵循`Contributions.md`中的指南。

## 许可

编辑器在 GPL-V3下获得许可。如果你创建并发布一个派生或者复刻版本，你需要同时发布你的源代码。请查看LICENSE.txt以获取更多详情。

EA没有认可并且不支持改变世界编辑器。

## 截图

![编辑器的截图](https://github.com/Rampastring/WorldAlteringEditor/raw/master/mapeditor.jpg "地图编辑器截图")

## 介绍视频

[![泰伯利亚黎明场景编辑器介绍](https://github.com/Rampastring/WorldAlteringEditor/raw/master/videopreview.jpg)](https://www.youtube.com/watch?v=jIcr3nCqx7M "泰伯利亚时代的黎明场景编辑器介绍")

改变世界编辑器最初是由泰伯利亚黎明（DTA）的工作人员为他们的模组开发的。后来添加了TS和YR的支持，以提供给第二代C&C社区的其余部分一个映射效率的提升。

[![泰伯利亚黎明场景编辑器介绍](https://github.com/Rampastring/WorldAlteringEditor/raw/master/dtalogo.png)](https://www.moddb.com/mods/the-dawn-of-the-tiberium-age "泰伯利亚时代的黎明主页")