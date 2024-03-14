[English](./Contributing.md) · **简体中文**

# 如何提交贡献

这个文件列出了在项目中使用的贡献指南。请注意，**为了保证提交内容的准确性，所有的提交必须使用英文**。

### 提交风格指南

提交以大写字母开头，不以标点符号结束。

正确的：
```
Treat usernames as case-insensitive in user collections
```

错误的：
```
treat usernames as case-insensitive in user collections.
```

在提交消息中使用现在时的祈使语气，而不是过去时。

正确的：
```
Add null-check for GameMode
```Mode添加空值检查
```

错误的：
```
Added null-check for GameMode
```

### 游戏支持

在编写代码或计划功能时，请考虑到WAE支持多种游戏：Tiberian Sun和Red Alert 2以及所有的mod。尽可能使功能对所有游戏都有益。

WAE 通过单个可执行文件支持所有目标游戏。所有提交的代码 _必须_ 与所有游戏兼容。如果某个游戏中没有特定的功能，请使该功能可以在编辑器的游戏配置中禁用。

有3个分支：`master`是用于Dawn of the Tiberium Age，`tsclient`是用于Tiberian Sun (CnCNet Client版本)，`yr`是用于Yuri's Revenge。从代码上看，这些分支是相同的，但`tsclient`和`yr`有一个额外的提交，使它们与`master`相比有不同的INI配置。

### 拉取请求

确保你的拉取请求的范围定义明确。拉取请求可能需要大量的开发者时间来审查，非常大的拉取请求或范围定义不明确的拉取请求可能难以审查。

一个拉取请求应该 _只实现一个功能_ 或 _修复一个bug_ ，除非有充分的理由将更改组合在一起。

在拉取请求中，不要大规模地重构现有代码的风格，除非重构的代码符合拉取请求的范围（功能或bug修复）。相反，如果你想仅仅为了重构或消除技术债务而重构现有的代码，那么为此创建一个次要的拉取请求。

**在创建你的拉取请求之前，确保你的代码和提交符合这个风格指南。**

范围定义不明确的拉取请求或者其他不符合这个指南的拉取请求可能会被工作人员拒绝并关闭。

### 代码风格指南

我们建立了一些代码风格规则以保持一致性。请在提交代码之前检查你的代码风格。
- 我们使用空格而不是制表符来缩进代码。
- 大括号总是要放在新的一行。这样做的一个原因是在多行体的情况下，清楚地分隔代码块头和体的结束：
```cs
if (SomeReallyLongCondition() ||
    ThatSplitsIntoMultipleLines())
{
    DoSomethingHere();
    DoSomethingMore();
}
```
- 无括号的代码块体只能在代码块头和体都是单行的情况下制作。在无括号的块中不允许有分成多行的语句和嵌套的无括号块：
```cs
// OK
if (Something())
    DoSomething();

// OK
if (SomeReallyLongCondition() ||
    ThatSplitsIntoMultipleLines())
{
    DoSomething();
}

// OK
if (SomeCondition())
{
    if (SomeOtherCondition())
        DoSomething();
}

// OK
if (SomeCondition())
{
    return VeryLongExpression()
        || ThatSplitsIntoMultipleLines();
}
```
- 只有空的大括号块可以在同一行上放置开括号和闭括号（如果适当）。
- 如果你使用`if`-`else`，你应该让所有的代码块都有括号或无括号，以保持一致性。
- 代码应该有空行，以便于阅读。使用空行将代码分割成逻辑部分。必须使用空行来分隔：
  - `return`语句（除非除了该语句外只有一行代码）；
  - 在后续代码中使用的局部变量赋值（你不应该在只在后续代码块中使用的一行局部变量赋值后放一个空行）；
  - 代码块（无括号或不）或任何使用代码块的东西（函数或钩子定义，类，命名空间等）
```cs
// OK
int localVar = Something();
if (SomeConditionUsing(localVar))
    ...

// OK
int localVar = Something();
int anotherLocalVar = OtherSomething();

if (SomeConditionUsing(localVar, anotherLocalVar))
    ...

// OK
int localVar = Something();

if (SomeConditionUsing(localVar))
    ...

if (SomeOtherConditionUsing(localVar))
    ...

localVar = OtherSomething();

// OK
if (SomeCondition())
{
    Code();
    OtherCode();

    return;
}

// OK
if (SomeCondition())
{
    SmallCode();
    return;
}
```
- 当变量的类型从代码中明显或类型不相关时，使用`var`与局部变量。永远不要与基本类型一起使用`var`。
```cs
// OK
var list = new List<int>();

// Not OK
var something = 6;
```
- 空的大括号块之间必须放一个空格。
- 局部变量，函数/方法参数和私有类字段以`camelCase`命名，并使用描述性名称，如`houseType`用于局部`HouseType`变量。
- 类，命名空间和属性总是以`PascalCase`写。
- 可以通过INI标签设置的类字段应该与相关的INI标签命名相同。

注意：提交风格指南并不详尽，可能会在未来由共同维护的社区进行调整。