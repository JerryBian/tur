# CLAUDE.md

Project-specific instructions for Claude Code. See @AGENTS.md for full architecture, conventions, and workflow guidance.

## 强制工作流（用户要求，必须遵守）

- **每次改动代码后，必须运行单元测试**：`dotnet test test/ -c Release`，确认全部通过后才能继续下一步或交付；不允许带着失败/跳过的测试收尾。
- 新增功能或修复 bug 时，同步补充对应的 unit test（xunit；测试类命名 `{被测类}Test`，方法命名 `Test_Case_N`；继承 `TestBase` 并使用随机临时目录 fixture，测试结束在 `Dispose` 中清理）。
- 测试必须在 Windows / macOS / Linux 上都能通过（CI 三平台矩阵），避免路径分隔符、大小写敏感等平台假设。
