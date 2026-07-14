using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ReeYin.CSharpScript.Models;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace ReeYin.CSharpScript.Views
{
    /// <summary>
    /// CSharpScriptView.xaml 的交互逻辑
    /// </summary>
    public partial class CSharpScriptView : UserControl
    {
        #region Fields
        private readonly DispatcherTimer _debounceTimer;
        private CompletionWindow? _completionWindow;

        /// <summary>
        /// 模块配置
        /// </summary>
        public CSharpScriptModel ModelParam { get; private set; } = null!;
        #endregion

        #region Constructor
        public CSharpScriptView()
        {
            InitializeComponent();

            _debounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _debounceTimer.Tick += OnDebounceTimerTick;

            ConfigureEditor();
            ScriptEditor.TextChanged += ScriptEditor_TextChanged;
            ScriptEditor.TextArea.TextEntered += ScriptEditor_TextArea_TextEntered;
            ScriptEditor.TextArea.TextEntering += ScriptEditor_TextArea_TextEntering;
            ScriptEditor.PreviewKeyDown += ScriptEditor_PreviewKeyDown;
        }
        #endregion

        #region Methods
        private void ConfigureEditor()
        {
            ScriptEditor.Options.ConvertTabsToSpaces = true;
            ScriptEditor.Options.IndentationSize = 4;
            ScriptEditor.Options.HighlightCurrentLine = true;
            ScriptEditor.Options.EnableHyperlinks = false;
            ScriptEditor.Options.EnableEmailHyperlinks = false;
        }

        private void StartDebounce()
        {
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }

        private void OnDebounceTimerTick(object? sender, EventArgs e)
        {
            _debounceTimer.Stop();
            if (ModelParam == null)
            {
                return;
            }

            ModelParam.CheckSyntaxErrors(ScriptEditor.Text);
        }

        private void ScriptEditor_TextChanged(object sender, EventArgs e)
        {
            if (ModelParam != null)
            {
                ModelParam.ScriptText = ScriptEditor.Text;
                ModelParam.IsEnableRun = false;
            }

            StartDebounce();
        }

        private void ScriptEditor_TextArea_TextEntered(object sender, TextCompositionEventArgs e)
        {
            if (ShouldOpenCompletion(e.Text))
            {
                ShowCompletion(forceOpen: e.Text == ".");
            }
        }

        private void ScriptEditor_TextArea_TextEntering(object sender, TextCompositionEventArgs e)
        {
            if (_completionWindow == null || string.IsNullOrEmpty(e.Text))
            {
                return;
            }

            char inputChar = e.Text[0];
            if (!IsWordCharacter(inputChar) && inputChar != '.')
            {
                _completionWindow.CompletionList.RequestInsertion(e);
            }
        }

        private void ScriptEditor_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab && Keyboard.Modifiers == ModifierKeys.None)
            {
                if (TryExpandSnippet())
                {
                    e.Handled = true;
                    return;
                }
            }

            if (e.Key == Key.Space && Keyboard.Modifiers == ModifierKeys.Control)
            {
                ShowCompletion(forceOpen: true);
                e.Handled = true;
            }
        }

        private bool ShouldOpenCompletion(string inputText)
        {
            if (string.IsNullOrWhiteSpace(inputText))
            {
                return false;
            }

            if (inputText == ".")
            {
                return true;
            }

            if (!IsWordCharacter(inputText[0]))
            {
                return false;
            }

            var context = GetCompletionContext();
            int minimumLength = context.IsMemberAccess ? 1 : 2;
            return context.CurrentWord.Length >= minimumLength && BuildCompletionItems(context)
                .Any(item => MatchesCompletionItem(item, context.CurrentWord));
        }

        private void ShowCompletion(bool forceOpen)
        {
            if (_completionWindow != null)
            {
                _completionWindow.Close();
                _completionWindow = null;
            }

            var context = GetCompletionContext();
            var items = BuildCompletionItems(context);
            if (!forceOpen && !string.IsNullOrWhiteSpace(context.CurrentWord))
            {
                items = items
                    .Where(item => MatchesCompletionItem(item, context.CurrentWord))
                    .ToList();
            }

            if (items.Count == 0)
            {
                return;
            }

            _completionWindow = new CompletionWindow(ScriptEditor.TextArea)
            {
                StartOffset = ScriptEditor.CaretOffset - context.CurrentWord.Length
            };

            foreach (var item in items)
            {
                _completionWindow.CompletionList.CompletionData.Add(
                    new ScriptCompletionData(item.DisplayText, item.InsertText, item.Description, item.CaretBacktrack));
            }

            _completionWindow.Closed += (_, _) => _completionWindow = null;
            _completionWindow.Show();
        }

        private List<CompletionItem> BuildCompletionItems(CompletionContext context)
        {
            if (context.IsMemberAccess)
            {
                var memberItems = BuildMemberCompletionItems(context.OwnerIdentifier);
                if (memberItems.Count > 0)
                {
                    return memberItems;
                }
            }

            var items = new List<CompletionItem>
            {
                new("pub", "public", "输入 pub 后按 Tab，可直接展开为 public。"),
                new("prop", CreatePropertySnippetText(), "输入 prop 后按 Tab，可展开为属性模板。"),
                new("public", "public", "C# 访问修饰符。"),
                new("private", "private", "C# 访问修饰符。"),
                new("protected", "protected", "C# 访问修饰符。"),
                new("internal", "internal", "C# 访问修饰符。"),
                new("class", "class", "定义类型。"),
                new("namespace", "namespace", "定义命名空间。"),
                new("override", "override", "重写基类成员。"),
                new("Console", "Console", "控制台输出与输入相关 API。"),
                new("Math", "Math", "数学计算相关静态方法。"),
                new("DateTime", "DateTime", "日期时间相关静态与实例成员。"),
                new("Environment", "Environment", "运行环境信息与特殊目录。"),
                new("MessageBox", "MessageBox", "WPF 消息框。"),
                new("bool", "bool", "布尔类型。"),
                new("var", "var", "局部变量类型推断。"),
                new("null", "null", "空值字面量。"),
                new("true", "true", "布尔真值。"),
                new("false", "false", "布尔假值。"),
                new("public class", "public class ", "快速输入 public class。"),
                new("public override bool ExeModule", "public override bool ExeModule(string cSharpScriptName)", "脚本入口方法签名。"),
                new("UploadOutput", "UploadOutput(\"输出名\", value)", "写入当前脚本节点的输出参数。"),
                new("GetCurLastInputValue", "GetCurLastInputValue(\"输入名\")", "读取上一节点输入参数的当前值。"),
                new("GetGlobalInputValue", "GetGlobalInputValue(\"全局变量名\")", "读取全局变量的当前值。"),
                new("GetCurCustomInputValue", "GetCurCustomInputValue(\"自定义变量名\")", "读取自定义全局变量的当前值。"),
                new("SetCurCustomInputValue", "SetCurCustomInputValue(\"自定义变量名\", null)", "设置自定义全局变量。"),
                new("CSharpScriptName", "CSharpScriptName", "当前脚本节点的唯一名称。"),
                new("return true;", "return true;", "脚本执行成功时返回 true。"),
                new("return false;", "return false;", "脚本执行失败时返回 false。"),
                new("if", "if ()\r\n{\r\n    \r\n}", "插入 if 语句块。"),
                new("try", "try\r\n{\r\n    \r\n}\r\ncatch (Exception ex)\r\n{\r\n    MessageBox.Show(ex.Message);\r\n    return false;\r\n}", "插入基础异常处理模板。"),
                new("Script Template",
@"public class Script : CSharpScriptBase
{
    public override bool ExeModule(string cSharpScriptName)
    {
        CSharpScriptName = cSharpScriptName;

        return true;
    }
}", "插入最小可运行脚本模板。"),
                new("using System;", "using System;", "插入 System 命名空间。"),
                new("using System.Linq;", "using System.Linq;", "插入 Linq 命名空间。"),
                new("using ReeYin.CSharpScript;", "using ReeYin.CSharpScript;", "插入脚本基类命名空间。"),
                new("using HalconDotNet;", "using HalconDotNet;", "插入 Halcon 命名空间。"),
            };

            foreach (var param in EnumerateParams(ModelParam?.InputParams))
            {
                items.Add(new CompletionItem(
                    $"{param.Name} (输入)",
                    $"GetCurLastInputValue(\"{ToCSharpStringLiteral(param.Name)}\")",
                    $"读取上一节点输入: {param.Name} ({param.Type})"));
            }

            foreach (var param in EnumerateParams(ModelParam?.GlobalParams))
            {
                items.Add(new CompletionItem(
                    $"{param.Name} (全局)",
                    $"GetGlobalInputValue(\"{ToCSharpStringLiteral(param.Name)}\")",
                    $"读取全局变量: {param.Name} ({param.Type})"));
            }

            foreach (var param in EnumerateParams(ModelParam?.CustomGlobalParams))
            {
                items.Add(new CompletionItem(
                    $"{param.Name} (自定义全局读取)",
                    $"GetCurCustomInputValue(\"{ToCSharpStringLiteral(param.Name)}\")",
                    $"读取自定义全局变量: {param.Name} ({param.Type})"));
                items.Add(new CompletionItem(
                    $"{param.Name} (自定义全局写入)",
                    $"SetCurCustomInputValue(\"{ToCSharpStringLiteral(param.Name)}\", null)",
                    $"设置自定义全局变量: {param.Name} ({param.Type})"));
            }

            foreach (var param in EnumerateParams(ModelParam?.OutputParams))
            {
                items.Add(new CompletionItem(
                    $"{param.Name} (输出)",
                    $"UploadOutput(\"{ToCSharpStringLiteral(param.Name)}\", null)",
                    $"写入输出参数: {param.Name} ({param.Type})"));
            }

            return items
                .GroupBy(item => item.InsertText)
                .Select(group => group.First())
                .OrderBy(item => item.DisplayText, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static List<CompletionItem> BuildMemberCompletionItems(string? ownerIdentifier)
        {
            if (string.IsNullOrWhiteSpace(ownerIdentifier))
            {
                return [];
            }

            return ownerIdentifier switch
            {
                "Console" => new List<CompletionItem>
                {
                    new("Write", "Write()", "写入文本但不换行。", 1),
                    new("WriteLine", "WriteLine()", "写入文本并换行。", 1),
                    new("ReadLine", "ReadLine()", "读取一行输入。"),
                    new("ReadKey", "ReadKey()", "读取一个按键。"),
                    new("Clear", "Clear()", "清空控制台。"),
                    new("Beep", "Beep()", "发出提示音。"),
                    new("SetCursorPosition", "SetCursorPosition(0, 0)", "设置控制台光标位置。", 4),
                    new("ForegroundColor", "ForegroundColor", "前景色属性。"),
                    new("BackgroundColor", "BackgroundColor", "背景色属性。"),
                    new("Title", "Title", "控制台标题。"),
                    new("Error", "Error", "错误输出流。"),
                    new("Out", "Out", "标准输出流。"),
                },
                "Math" => new List<CompletionItem>
                {
                    new("Abs", "Abs()", "返回绝对值。", 1),
                    new("Max", "Max(, )", "返回较大值。", 3),
                    new("Min", "Min(, )", "返回较小值。", 3),
                    new("Round", "Round()", "四舍五入。", 1),
                    new("Floor", "Floor()", "向下取整。", 1),
                    new("Ceiling", "Ceiling()", "向上取整。", 1),
                    new("Sqrt", "Sqrt()", "平方根。", 1),
                    new("Pow", "Pow(, )", "幂运算。", 3),
                    new("Sin", "Sin()", "正弦。", 1),
                    new("Cos", "Cos()", "余弦。", 1),
                    new("PI", "PI", "圆周率常量。"),
                },
                "DateTime" => new List<CompletionItem>
                {
                    new("Now", "Now", "当前本地时间。"),
                    new("Today", "Today", "今天日期。"),
                    new("UtcNow", "UtcNow", "当前 UTC 时间。"),
                    new("Parse", "Parse()", "从字符串解析时间。", 1),
                    new("TryParse", "TryParse(, out _)", "尝试解析时间。", 9),
                    new("AddDays", "AddDays()", "增加天数。", 1),
                    new("AddHours", "AddHours()", "增加小时。", 1),
                    new("AddMinutes", "AddMinutes()", "增加分钟。", 1),
                    new("ToString", "ToString()", "转成字符串。", 1),
                },
                "Environment" => new List<CompletionItem>
                {
                    new("CurrentDirectory", "CurrentDirectory", "当前工作目录。"),
                    new("MachineName", "MachineName", "计算机名。"),
                    new("NewLine", "NewLine", "换行字符串。"),
                    new("UserName", "UserName", "当前用户名。"),
                    new("GetFolderPath", "GetFolderPath()", "获取特殊目录。", 1),
                    new("TickCount", "TickCount", "系统启动后的毫秒数。"),
                },
                "MessageBox" => new List<CompletionItem>
                {
                    new("Show", "Show()", "显示消息框。", 1),
                },
                _ => [],
            };
        }

        private static bool MatchesCompletionItem(CompletionItem item, string currentWord)
        {
            return item.DisplayText.StartsWith(currentWord, StringComparison.OrdinalIgnoreCase);
        }

        private static IEnumerable<TransmitParam> EnumerateParams(IEnumerable<TransmitParam> parameters)
        {
            return parameters ?? Enumerable.Empty<TransmitParam>();
        }

        private bool TryExpandSnippet()
        {
            string currentWord = GetCurrentWord();
            if (string.IsNullOrWhiteSpace(currentWord))
            {
                return false;
            }

            switch (currentWord)
            {
                case "pub":
                    ReplaceCurrentWord("public");
                    return true;

                case "prop":
                    ExpandPropertySnippet();
                    return true;

                default:
                    return false;
            }
        }

        private string GetCurrentWord()
        {
            return GetCompletionContext().CurrentWord;
        }

        private CompletionContext GetCompletionContext()
        {
            if (ScriptEditor.Document == null)
            {
                return new CompletionContext(string.Empty, null, false);
            }

            int offset = ScriptEditor.CaretOffset;
            int start = offset;

            while (start > 0)
            {
                char currentChar = ScriptEditor.Document.GetCharAt(start - 1);
                if (!IsWordCharacter(currentChar))
                {
                    break;
                }

                start--;
            }

            string currentWord = offset > start
                ? ScriptEditor.Document.GetText(start, offset - start)
                : string.Empty;

            int dotIndex = start - 1;
            while (dotIndex >= 0 && char.IsWhiteSpace(ScriptEditor.Document.GetCharAt(dotIndex)))
            {
                dotIndex--;
            }

            if (dotIndex >= 0 && ScriptEditor.Document.GetCharAt(dotIndex) == '.')
            {
                int ownerEnd = dotIndex;
                int ownerStart = ownerEnd;

                while (ownerStart > 0 && IsWordCharacter(ScriptEditor.Document.GetCharAt(ownerStart - 1)))
                {
                    ownerStart--;
                }

                if (ownerEnd > ownerStart)
                {
                    string ownerIdentifier = ScriptEditor.Document.GetText(ownerStart, ownerEnd - ownerStart);
                    return new CompletionContext(currentWord, ownerIdentifier, true);
                }
            }

            return new CompletionContext(currentWord, null, false);
        }

        private static bool IsWordCharacter(char currentChar)
        {
            return char.IsLetterOrDigit(currentChar) || currentChar == '_';
        }

        private static string ToCSharpStringLiteral(string? value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"");
        }

        private void ReplaceCurrentWord(string replacement)
        {
            int offset = ScriptEditor.CaretOffset;
            int start = offset;

            while (start > 0)
            {
                char currentChar = ScriptEditor.Document.GetCharAt(start - 1);
                if (!IsWordCharacter(currentChar))
                {
                    break;
                }

                start--;
            }

            _completionWindow?.Close();
            ScriptEditor.Document.Replace(start, offset - start, replacement);
            ScriptEditor.CaretOffset = start + replacement.Length;
        }

        private void ExpandPropertySnippet()
        {
            int offset = ScriptEditor.CaretOffset;
            int start = offset;

            while (start > 0)
            {
                char currentChar = ScriptEditor.Document.GetCharAt(start - 1);
                if (!IsWordCharacter(currentChar))
                {
                    break;
                }

                start--;
            }

            string snippetText = CreatePropertySnippetText();
            int propertyNameOffset = snippetText.IndexOf("PropertyName", StringComparison.Ordinal);

            _completionWindow?.Close();
            ScriptEditor.Document.Replace(start, offset - start, snippetText);

            if (propertyNameOffset >= 0)
            {
                ScriptEditor.Select(start + propertyNameOffset, "PropertyName".Length);
                ScriptEditor.TextArea.Caret.Offset = start + propertyNameOffset + "PropertyName".Length;
            }
            else
            {
                ScriptEditor.CaretOffset = start + snippetText.Length;
            }
        }

        private static string CreatePropertySnippetText()
        {
            return
@"public object PropertyName
{
    get;
    set;
}";
        }

        private void InsertTextAtCaret(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            int insertOffset = ScriptEditor.SelectionStart;
            ScriptEditor.Focus();
            ScriptEditor.Document.Replace(ScriptEditor.SelectionStart, ScriptEditor.SelectionLength, text);
            ScriptEditor.CaretOffset = insertOffset + text.Length;
        }
        #endregion

        #region Commands
        private void ManagedDllButton_Click(object sender, RoutedEventArgs e)
        {
            ManagedDllPopup.IsOpen = true;
        }

        private void CloseManagedDllPopupButton_Click(object sender, RoutedEventArgs e)
        {
            ManagedDllPopup.IsOpen = false;
        }

        /// <summary>
        /// 用来传递内容，执行在Model操作
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RunScript_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ModelParam == null)
                {
                    return;
                }

                ModelParam.ScriptText = ScriptEditor.Text;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"宿主错误:\n{ex}", "Exception", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveScript_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = "Script.cs",
                DefaultExt = ".cs",
                Filter = "C# Files (.cs)|*.cs|All files (*.*)|*.*"
            };

            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                File.WriteAllText(dlg.FileName, ScriptEditor.Text);
                MessageBox.Show("脚本已保存。", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// 拿到配置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            ModelParam = PrismProvider.ProjectManager.GetNodeParamCacheValue<CSharpScriptModel>(CSharpScriptModel.ModuleName);
            if (ModelParam == null)
            {
                MessageBox.Show("未找到当前 CSharpScript 节点参数，请重新打开配置窗口。", "脚本配置错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ScriptEditor.Text = ModelParam.ScriptText;
            ModelParam.CheckSyntaxErrors(ScriptEditor.Text);
        }

        private void DataGrid_LastInputMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("确定要插入此参数吗? ", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.No)
            {
                return;
            }

            if (ModelParam?.SltInputParam == null)
            {
                return;
            }

            InsertTextAtCaret($"GetCurLastInputValue(\"{ToCSharpStringLiteral(ModelParam.SltInputParam.Name)}\")");
        }

        /// <summary>
        /// 输出
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OutputParams_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("确定要插入此参数吗? ", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.No)
            {
                return;
            }

            if (ModelParam?.SltOutputParam == null)
            {
                return;
            }

            InsertTextAtCaret($"UploadOutput(\"{ToCSharpStringLiteral(ModelParam.SltOutputParam.Name)}\", null)");
        }

        /// <summary>
        /// 获取选中全局参数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GlobalParam_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("确定要插入此参数吗? ", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.No)
            {
                return;
            }

            if (ModelParam?.SltInputParam == null)
            {
                return;
            }

            InsertTextAtCaret($"GetGlobalInputValue(\"{ToCSharpStringLiteral(ModelParam.SltInputParam.Name)}\")");
        }

        private void CustomGlobal_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ModelParam?.SltInputParam == null)
            {
                return;
            }

            MessageBoxResult getOrSet = MessageBox.Show("选择是获取参数，选择否设定参数? ", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (getOrSet != MessageBoxResult.No)
            {
                InsertTextAtCaret($"GetCurCustomInputValue(\"{ToCSharpStringLiteral(ModelParam.SltInputParam.Name)}\")");
            }
            else
            {
                InsertTextAtCaret($"SetCurCustomInputValue(\"{ToCSharpStringLiteral(ModelParam.SltInputParam.Name)}\", null)");
            }
        }
        #endregion

        private void ScriptEditor_LostFocus(object sender, RoutedEventArgs e)
        {
            if (ModelParam != null)
            {
                ModelParam.ScriptText = ScriptEditor.Text;
            }
        }

        private sealed record CompletionContext(string CurrentWord, string? OwnerIdentifier, bool IsMemberAccess);

        private sealed record CompletionItem(string DisplayText, string InsertText, string Description, int CaretBacktrack = 0);

        private sealed class ScriptCompletionData : ICompletionData
        {
            public ScriptCompletionData(string displayText, string insertText, string description, int caretBacktrack)
            {
                Text = insertText;
                Content = displayText;
                Description = description;
                CaretBacktrack = caretBacktrack;
            }

            public ImageSource? Image => null;

            public string Text { get; }

            public object Content { get; }

            public object Description { get; }

            public int CaretBacktrack { get; }

            public double Priority => 0;

            public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
            {
                textArea.Document.Replace(completionSegment, Text);
                textArea.Caret.Offset = completionSegment.Offset + Text.Length - CaretBacktrack;
            }
        }
    }
}
