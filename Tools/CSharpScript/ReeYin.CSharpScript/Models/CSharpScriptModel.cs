using HalconDotNet;
using ICSharpCode.AvalonEdit;
using ImageTool.Halcon;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.DynamicView;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Logger;
using ReeYin_V.Share;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.Loader;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;

namespace ReeYin.CSharpScript.Models
{
    [Serializable]
    public class CSharpScriptModel : ModelParamBase
    {
        #region Fields
        [JsonIgnore]
        public bool IsDebug { get; set; } = false;

        //脚本代码存储路径
        private string _tempDir = string.Empty;

        [JsonIgnore]
        private string? _lastCompiledFingerprint;

        public string CSharpScriptName = string.Empty;

        public string ScriptText = @"using System;
using HalconDotNet;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ReeYin.CSharpScript;

public class Script: CSharpScriptBase
{
    public override bool ExeModule(string cSharpScriptName)
    {
        CSharpScriptName = cSharpScriptName;
        if( System.Diagnostics.Debugger.IsAttached)
        {
            System.Diagnostics.Debugger.Break();//断点会在这里,前面可自行打断点
        }

        return false;
    }
}";

        #endregion

        #region Properties

        private ObservableCollection<string> _managedDllNames;
        /// <summary>
        /// 必要DLL，按需添加
        /// </summary>
        public ObservableCollection<string> ManagedDllNames
        {
            get { return _managedDllNames; }
            set { _managedDllNames = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isEnableRun;
        /// <summary>
        /// 编译通过才能运行
        /// </summary>
        public bool IsEnableRun
        {
            get { return _isEnableRun; }
            set { _isEnableRun = value; RaisePropertyChanged(); }
        }


        [JsonIgnore]
        public override Func<ExecuteModuleOutput> TriggerModuleRun { get; set; }

        [JsonIgnore]
        private TransmitParam _sltOutputParam;
        /// <summary>
        /// 选中的输出项
        /// </summary>
        [JsonIgnore]
        public TransmitParam SltOutputParam
        {
            get { return _sltOutputParam; }
            set { _sltOutputParam = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private TransmitParam _sltInputParam;
        /// <summary>
        /// 选中的输入参数
        /// </summary>
        [JsonIgnore]
        public TransmitParam SltInputParam
        {
            get { return _sltInputParam; }
            set { _sltInputParam = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _errorInfo;
        [JsonIgnore]
        public string ErrorInfo
        {
            get { return _errorInfo; }
            set { _errorInfo = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private Visibility _errorVisibility;
        [JsonIgnore]
        public Visibility ErrorVisibility
        {
            get { return _errorVisibility; }
            set { _errorVisibility = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _compileStatusText = "等待编译";
        [JsonIgnore]
        public string CompileStatusText
        {
            get { return _compileStatusText; }
            set { _compileStatusText = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _compileStatusBrush = "#5B6472";
        [JsonIgnore]
        public string CompileStatusBrush
        {
            get { return _compileStatusBrush; }
            set { _compileStatusBrush = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _compileOutput = "脚本模板已加载。\r\n可使用 Ctrl+Space 查看可用 API、常用片段和参数插入提示。";
        [JsonIgnore]
        public string CompileOutput
        {
            get { return _compileOutput; }
            set { _compileOutput = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ObservableCollection<TransmitParam> _globalParams;
        [JsonIgnore]
        public ObservableCollection<TransmitParam> GlobalParams
        {
            get { return _globalParams; }
            set { _globalParams = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ObservableCollection<TransmitParam> _customGlobalParams;
        [JsonIgnore]
        public ObservableCollection<TransmitParam> CustomGlobalParams
        {
            get { return _customGlobalParams; }
            set { _customGlobalParams = value; RaisePropertyChanged(); }
        }


        #endregion

        #region Constructor
        public CSharpScriptModel()
        {

        }

        #endregion

        #region OverrideMethods

        /// <summary>
        /// 加载关键参数
        /// </summary>
        /// <returns></returns>
        public override bool LoadKeyParam()
        {
            try
            {
                if (!base.LoadKeyParam())
                {
                    return false;
                }

                ModuleName = Serial.ToString("D3");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载参数异常{ex.StackTrace}");
                return false;
            }
        }

        public override bool OnceInit()
        {
            try
            {
                if (IsOnceInit)
                {
                    return true;
                }

                if (!base.OnceInit())
                {
                    return false;
                }

                // 存放在exe同路径下，不然找不到相关DLL
                _tempDir = PrismProvider.AppBasePath;

                if (TriggerModuleRun == null)
                {
                    TriggerModuleRun = RunModuleSynchronously;
                }

                IsOnceInit = true;
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        #endregion

        #region Methods
        private ExecuteModuleOutput RunModuleSynchronously()
        {
            return ExecuteModule().GetAwaiter().GetResult();
        }


        //[OnDeserialized]
        //internal void OnDeserializedMethod(StreamingContext context)
        //{
        //    OnceInit();
        //    //PrismProvider.ProjectManager.SltCurSolutionItem.NodeParamCaches.Add(CSharpScriptName, this);
        //}

        /// <summary>
        /// 模块执行
        /// </summary>
        /// <returns></returns>
        public async Task<ExecuteModuleOutput> ExecuteModule()
        {
            var (result, time) = SetTimeHelper.SetTimer(() =>
            {
                try
                {
                    #region 检测参数（对链接参数重新赋值）
                    Console.WriteLine($"开始加载参数：{DateTime.Now.ToString($"HH:mm:ss.fff")}");
                    if (!IsDebug)
                        LoadKeyParam();

                    GlobalParams = PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams;
                    Console.WriteLine($"结束加载参数：{DateTime.Now.ToString($"HH:mm:ss.fff")}");
                    #endregion

                    try
                    {
                        if (!RunScript(CSharpScriptName))
                            return NodeStatus.Error;
                        if(PrismProvider.ProjectManager.SltCurSolutionItem.NodesOutputCache != null)
                        {

                        }
                    }
                    catch (TargetInvocationException ex)
                    {
                        MessageBox.Show($"脚本执行异常:\n{ex.InnerException?.Message ?? ex.Message}",
                            "脚本错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return NodeStatus.Error;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"宿主错误:\n{ex}", "Exception", MessageBoxButton.OK, MessageBoxImage.Error);
                        return NodeStatus.Error;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace.ToString());
                    return NodeStatus.Error;
                }
                #region 输出

                Console.WriteLine($"开始输出参数：{DateTime.Now.ToString($"HH:mm:ss.fff")}");

                //执行后对输出参数重新赋值
                var outputCache = PrismProvider.ProjectManager.SltCurSolutionItem.NodesOutputCache;
                if (outputCache != null && outputCache.ContainsKey(CSharpScriptName))
                {
                    foreach (var item in outputCache[CSharpScriptName])
                    {
                        var existing = OutputParams.FirstOrDefault(p => p.Name == item.Name);

                        if (existing != null)
                        {
                            existing.Value = item.Value;
                        }
                        else
                        {
                            OutputParams.Add(item);
                        }
                    }
                }
                else
                {

                }
                Console.WriteLine($"完成赋值时间：{DateTime.Now.ToString($"HH:mm:ss.fff")}");

                if (!UpdateParam())
                {
                    Console.WriteLine($"模块_{Serial}更新参数失败");
                }
                Console.WriteLine($"结束输出参数：{DateTime.Now.ToString($"HH:mm:ss.fff")}");
                #endregion

                return NodeStatus.Success;
            });

            return Output = new ExecuteModuleOutput()
            {
                RunStatus = result,
                RunTime = time,
            };
        }

        public string dllPath = string.Empty /*= Path.Combine(_tempDir, $"{CSharpScriptName}.dll")*/;
        public string sourcePath = string.Empty/* = Path.Combine(_tempDir, $"{CSharpScriptName}.cs")*/;
        public string pdbPath = string.Empty /*= Path.Combine(_tempDir, $"{CSharpScriptName}.pdb")*/;

        /// <summary>
        /// 编译脚本
        /// </summary>
        /// <returns></returns>
        public bool CompileScript()
        {
            try
            {
                UpdateCompileFeedback("正在编译", "正在检查脚本语法、引用 DLL 和运行所需入口...", "#2563EB");
                var LastTime = DateTime.Now;
                string sourceCode = ScriptText ?? string.Empty;
                EnsureScriptPaths();
                CleanupFilesByPrefix(Path.GetFileNameWithoutExtension(dllPath));
                string scriptFingerprint = BuildCompilationFingerprint(sourceCode, ManagedDllNames);
                var time = DateTime.Now.Subtract(LastTime).TotalMilliseconds;
                Console.WriteLine($"Step1耗时：{time}ms");
                LastTime = DateTime.Now;

                File.WriteAllText(sourcePath, sourceCode, Encoding.UTF8);

                var references = BuildReferencesForCompilation();

                var syntaxTree = CSharpSyntaxTree.ParseText(
                    sourceCode,
                    path: sourcePath,
                    encoding: Encoding.UTF8
                );
                time = DateTime.Now.Subtract(LastTime).TotalMilliseconds;
                Console.WriteLine($"Step2耗时：{time}ms");
                LastTime = DateTime.Now;

                //Debug模式下调试，Release只简单编译执行，避免耗时
                if (Debugger.IsAttached)
                {
                    // 必须是 DLL 才能反射创建实例
                    var compilation = CSharpCompilation.Create(
                    assemblyName: "DynamicScript",
                    syntaxTrees: new[] { syntaxTree },
                    references: references,
                    options: new CSharpCompilationOptions(
                        outputKind: OutputKind.DynamicallyLinkedLibrary,
                        optimizationLevel: OptimizationLevel.Debug,
                        allowUnsafe: true,
                        platform: Platform.AnyCpu
                    ));

                    time = DateTime.Now.Subtract(LastTime).TotalMilliseconds;
                    Console.WriteLine($"Step3耗时：{time}ms");
                    LastTime = DateTime.Now;

                    using (var dllStream = File.Create(dllPath))
                    using (var pdbStream = File.Create(pdbPath))
                    {
                        var result = compilation.Emit(dllStream, pdbStream);
                        if (!result.Success)
                        {
                            UpdateCompileFeedback(
                                "编译失败",
                                BuildCompileOutputText("编译失败，存在错误。", result.Diagnostics),
                                "#C53030");
                            IsEnableRun = false;
                            return false;
                        }

                        UpdateCompileResultFeedback(result.Diagnostics);
                    }
                }
                else
                {
                    // 必须是 DLL 才能反射创建实例
                    var compilation = CSharpCompilation.Create(
                    assemblyName: "DynamicScript",
                    syntaxTrees: new[] { syntaxTree },
                    references: references,
                    options: new CSharpCompilationOptions(
                        outputKind: OutputKind.DynamicallyLinkedLibrary,
                        optimizationLevel: OptimizationLevel.Release,
                        allowUnsafe: true,
                        platform: Platform.AnyCpu
                    ));

                    time = DateTime.Now.Subtract(LastTime).TotalMilliseconds;
                    Console.WriteLine($"Step3耗时：{time}ms");
                    LastTime = DateTime.Now;

                    using (var dllStream = File.Create(dllPath))
                    {
                        var result = compilation.Emit(dllStream);
                        if (!result.Success)
                        {
                            UpdateCompileFeedback(
                                "编译失败",
                                BuildCompileOutputText("编译失败，存在错误。", result.Diagnostics),
                                "#C53030");
                            IsEnableRun = false;
                            return false;
                        }

                        UpdateCompileResultFeedback(result.Diagnostics);
                    }
                }
                _lastCompiledFingerprint = scriptFingerprint;
                IsEnableRun = true;
                return true;
            }
            catch (Exception ex)
            {
                UpdateCompileFeedback("编译异常", $"编译过程中发生异常：{ex.Message}\r\n{ex}", "#C53030");
                Logs.LogError($"脚本编译失败：{ex.StackTrace}");
                IsEnableRun = false;
                return false;
            }
        }

        /// <summary>
        /// 运行脚本
        /// </summary>
        public bool RunScript(string inputParameter)
        {
            AssemblyLoadContext? context = null;
            WeakReference? contextWeakRef = null;
            Assembly? assembly = null;
            object? instance = null;
            MethodInfo? method = null;

            try
            {
                var LastTime = DateTime.Now;
                EnsureScriptPaths();
                if (!IsCompiledArtifactCurrent() && !CompileScript())
                {
                    return false;
                }

                var time = DateTime.Now.Subtract(LastTime).TotalMilliseconds;
                Console.WriteLine($"Step4耗时：{time}ms");
                LastTime = DateTime.Now;

                byte[] asmBytes = File.ReadAllBytes(dllPath);
                byte[]? pdbBytes = File.Exists(pdbPath) ? File.ReadAllBytes(pdbPath) : null;

                context = new AssemblyLoadContext(
                    $"ScriptRunner_{GetSafeScriptFilePrefix()}_{System.Guid.NewGuid():N}",
                    isCollectible: true);
                contextWeakRef = new WeakReference(context);

                using (var asmStream = new MemoryStream(asmBytes))
                {
                    if (pdbBytes != null)
                    {
                        using var pdbStream = new MemoryStream(pdbBytes);
                        assembly = context.LoadFromStream(asmStream, pdbStream);
                    }
                    else
                    {
                        assembly = context.LoadFromStream(asmStream);
                    }
                }

                time = DateTime.Now.Subtract(LastTime).TotalMilliseconds;
                Console.WriteLine($"Step5耗时：{time}ms");
                LastTime = DateTime.Now;

                // 👇 约定：脚本必须包含一个名为 "Script" 的 public 类
                Type? scriptType = assembly.GetType("Script");
                if (scriptType == null)
                {
                    UpdateCompileFeedback("执行失败", "未找到名为 'Script' 的公共类。", "#C53030");
                    MessageBox.Show("未找到名为 'Script' 的公共类。", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
                time = DateTime.Now.Subtract(LastTime).TotalMilliseconds;
                Console.WriteLine($"Step6耗时：{time}ms");
                LastTime = DateTime.Now;

                try
                {
                    instance = Activator.CreateInstance(scriptType);
                }
                catch (MissingMethodException)
                {
                    UpdateCompileFeedback("执行失败", "类 'Script' 必须包含公共的无参数构造函数。", "#C53030");
                    MessageBox.Show("类 'Script' 必须包含公共的无参数构造函数。", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
                time = DateTime.Now.Subtract(LastTime).TotalMilliseconds;
                Console.WriteLine($"Step7耗时：{time}ms");
                LastTime = DateTime.Now;

                Type[] parameterTypes = new Type[] { typeof(string) };
                method = scriptType.GetMethod("ExeModule",
                    BindingFlags.Public | BindingFlags.Instance,
                    binder: null,
                    types: parameterTypes,
                    modifiers: null);

                if (method == null)
                {
                    UpdateCompileFeedback("执行失败", "类 'Script' 中未找到 public bool ExeModule(string cSharpScriptName) 方法。", "#C53030");
                    MessageBox.Show("类 'Script' 中未找到 public bool ExeModule(string cSharpScriptName) 方法。", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                if (method.ReturnType != typeof(bool))
                {
                    UpdateCompileFeedback("执行失败", "ExeModule 必须返回 bool 类型。", "#C53030");
                    MessageBox.Show("ExeModule 必须返回 bool 类型。", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                time = DateTime.Now.Subtract(LastTime).TotalMilliseconds;
                Console.WriteLine($"Step8耗时：{time}ms");
                LastTime = DateTime.Now;

                object? scriptResult = method.Invoke(instance, new object[] { inputParameter });
                if (!TryGetScriptSuccess(method.ReturnType, scriptResult, out bool scriptSucceeded, out string resultError))
                {
                    UpdateCompileFeedback("执行失败", resultError, "#C53030");
                    MessageBox.Show(resultError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                string resultStr = scriptResult?.ToString() ?? "null";
                System.Diagnostics.Debug.WriteLine($"脚本执行结果:\n{resultStr}");
                time = DateTime.Now.Subtract(LastTime).TotalMilliseconds;
                Console.WriteLine($"Step9耗时：{time}ms");

                if (!scriptSucceeded)
                {
                    UpdateCompileFeedback("执行失败", $"脚本已执行完成，但返回 false。\r\n返回值：{resultStr}", "#C53030");
                    return false;
                }

                UpdateCompileFeedback("执行完成", $"脚本已执行完成。\r\n返回值：{resultStr}", "#2F855A");
                return true;
            }
            catch (TargetInvocationException ex)
            {
                UpdateCompileFeedback("执行失败", $"脚本执行异常：{ex.InnerException?.Message ?? ex.Message}\r\n{ex.InnerException ?? ex}", "#C53030");
                MessageBox.Show($"脚本执行异常:\n{ex.InnerException?.Message ?? ex.Message}",
                    "脚本错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            catch (Exception ex)
            {
                UpdateCompileFeedback("执行失败", $"宿主错误：{ex.Message}\r\n{ex}", "#C53030");
                MessageBox.Show($"宿主错误:\n{ex}", "Exception", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            finally
            {
                method = null;
                instance = null;
                assembly = null;

                if (context != null)
                {
                    context.Unload();
                    context = null;
                }

                TryCollectAssemblyLoadContext(contextWeakRef);
            }
        }

        public static string BuildCompilationFingerprint(string sourceCode, IEnumerable<string>? managedDllNames)
        {
            var normalizedDllNames = managedDllNames?
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                ?? Enumerable.Empty<string>();

            string payload = $"{sourceCode ?? string.Empty}\n--references--\n{string.Join("\n", normalizedDllNames)}";
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
        }

        public static bool TryGetScriptSuccess(Type? returnType, object? scriptResult, out bool succeeded, out string error)
        {
            succeeded = false;
            error = string.Empty;

            if (returnType != typeof(bool))
            {
                error = "ExeModule 必须返回 bool 类型。";
                return false;
            }

            if (scriptResult is bool boolResult)
            {
                succeeded = boolResult;
                return true;
            }

            error = "ExeModule 返回值不是有效的 bool。";
            return false;
        }

        private void EnsureScriptPaths()
        {
            if (string.IsNullOrWhiteSpace(_tempDir))
            {
                _tempDir = PrismProvider.AppBasePath;
            }

            Directory.CreateDirectory(_tempDir);

            string filePrefix = GetSafeScriptFilePrefix();
            sourcePath = Path.Combine(_tempDir, $"{filePrefix}.cs");
            dllPath = Path.Combine(_tempDir, $"{filePrefix}.dll");
            pdbPath = Path.Combine(_tempDir, $"{filePrefix}.pdb");
        }

        private string GetSafeScriptFilePrefix()
        {
            string filePrefix = !string.IsNullOrWhiteSpace(CSharpScriptName)
                ? CSharpScriptName
                : Serial >= 0 ? Serial.ToString("D3") : "CSharpScript";

            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                filePrefix = filePrefix.Replace(invalidChar, '_');
            }

            return filePrefix;
        }

        private bool IsCompiledArtifactCurrent()
        {
            EnsureScriptPaths();

            if (!File.Exists(dllPath))
            {
                return false;
            }

            if (Debugger.IsAttached && !File.Exists(pdbPath))
            {
                return false;
            }

            string currentFingerprint = BuildCompilationFingerprint(ScriptText ?? string.Empty, ManagedDllNames);
            return string.Equals(_lastCompiledFingerprint, currentFingerprint, StringComparison.Ordinal);
        }

        private static void TryCollectAssemblyLoadContext(WeakReference? contextWeakRef)
        {
            if (contextWeakRef == null)
            {
                return;
            }

            for (int i = 0; contextWeakRef.IsAlive && i < 3; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        private void CleanupFilesByPrefix(string prefix)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_tempDir) || string.IsNullOrWhiteSpace(prefix))
                {
                    return;
                }

                var files = Directory.GetFiles(_tempDir, $"{prefix}*");

                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
        }


        private void CleanupOldFiles()
        {
            try
            {
                var threshold = DateTime.Now.AddMinutes(-1);
                foreach (var file in Directory.GetFiles(_tempDir))
                {
                    if (File.GetCreationTime(file) < threshold)
                    {
                        try { File.Delete(file); } catch { }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// 检测脚本错误
        /// </summary>
        /// <param name="code"></param>
        public void CheckSyntaxErrors(string code)
        {
            try
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(code);

                var references = BuildReferencesForAnalysis();

                var compilation = CSharpCompilation.Create(
                    "TempScript",
                    new[] { syntaxTree },
                    references,
                    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                );

                var diagnostics = compilation.GetDiagnostics()
                    .Where(d => d.Severity >= DiagnosticSeverity.Warning)
                    .ToList();

                var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
                var warnings = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning).ToList();

                if (errors.Any())
                {
                    UpdateCompileFeedback(
                        $"实时检查发现 {errors.Count} 个错误",
                        BuildCompileOutputText("实时语法检查失败，请先修复以下错误。", diagnostics),
                        "#C53030");
                    IsEnableRun = false;
                }
                else if (warnings.Any())
                {
                    UpdateCompileFeedback(
                        $"实时检查通过，存在 {warnings.Count} 条警告",
                        BuildCompileOutputText("实时语法检查通过，但仍有警告。", diagnostics),
                        "#B7791F");
                }
                else
                {
                    UpdateCompileFeedback(
                        "实时检查通过",
                        "实时语法检查通过。\r\n可以直接继续编辑，或点击“编译”生成可执行脚本程序集。",
                        "#2F855A");
                }
            }
            catch (Exception ex)
            {
                UpdateCompileFeedback("分析失败", $"语法分析失败：{ex.Message}\r\n{ex}", "#C53030");
            }
        }


        #region 引用构建
        private List<MetadataReference> BuildReferencesForCompilation()
        {
            var refs = new List<MetadataReference>();

            // 获取当前 AppDomain 中所有已加载的、位于 dotnet/shared 或本地 bin 的程序集
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
                {
                    try
                    {
                        refs.Add(MetadataReference.CreateFromFile(assembly.Location));
                    }
                    catch (Exception ex)
                    {
                        // 忽略无法访问的程序集（如内存中的动态程序集）
                        System.Diagnostics.Debug.WriteLine($"跳过程序集: {assembly.FullName} - {ex.Message}");
                    }
                }
            }
            // ✅ 核心：引用 mscorlib / System.Private.CoreLib
            refs.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
            AddCoreReferences(refs);    // mscorlib, System.Console 等
            AddWpfReferences(refs);      // ⭐ 新增 WPF 支持
            AddLocalManagedDlls(refs);
            return refs;
        }

        private List<MetadataReference> BuildReferencesForAnalysis()
        {
            var refs = new List<MetadataReference>();
            AddCoreReferences(refs);
            AddWpfReferences(refs);
            AddLocalManagedDlls(refs, analysisMode: true);
            return refs;
        }

        private void AddCoreReferences(List<MetadataReference> refs)
        {
            // 基础引用
            refs.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
            refs.Add(MetadataReference.CreateFromFile(typeof(Uri).Assembly.Location));      // System.Runtime
            refs.Add(MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)); // System.Linq

            // 常见系统程序集
            AddReferenceIfExists(refs, "System.Runtime");
            AddReferenceIfExists(refs, "System.Console");
            AddReferenceIfExists(refs, "System.Collections");
            AddReferenceIfExists(refs, "System.Linq");
            AddReferenceIfExists(refs, "System.Threading");
            AddReferenceIfExists(refs, "System.IO");
            AddReferenceIfExists(refs, "System.Windows.Forms"); // HALCON 可能需要
            AddReferenceIfExists(refs, "System.Drawing");
            // ⭐ 关键：引用主程序自身（.exe）
            string exePath = Assembly.GetExecutingAssembly().Location;
            refs.Add(MetadataReference.CreateFromFile(exePath));

        }

        /// <summary>
        /// 加载本地DLL
        /// </summary>
        /// <param name="refs"></param>
        /// <param name="analysisMode"></param>
        private void AddLocalManagedDlls(List<MetadataReference> refs, bool analysisMode = false)
        {
            string exeDir = PrismProvider.AppBasePath;

            if (ManagedDllNames == null || ManagedDllNames.Count == 0)
            {
                return;
            }

            foreach (string dllName in ManagedDllNames)
            {
                string dllPath = Path.Combine(exeDir, dllName);
                if (!File.Exists(dllPath)) continue;

                try
                {
                    // 验证是否为托管程序集
                    using var stream = File.OpenRead(dllPath);
                    using var peReader = new System.Reflection.PortableExecutable.PEReader(stream);
                    if (peReader.HasMetadata)
                    {
                        refs.Add(MetadataReference.CreateFromFile(dllPath));
                    }
                }
                catch (BadImageFormatException)
                {
                    // 非托管 DLL，跳过
                }
                catch (Exception ex)
                {
                    if (!analysisMode)
                        System.Diagnostics.Debug.WriteLine($"加载 {dllName} 失败: {ex}");
                }
            }
        }

        /// <summary>
        /// 判断指定路径的 DLL 是否为有效的托管程序集（.NET 程序集）
        /// </summary>
        /// <param name="dllPath">DLL 文件的完整路径</param>
        /// <returns>若文件存在且是有效的托管程序集，则返回 true；否则返回 false</returns>
        public bool IsManagedDllValid(string dllName)
        {
            string exeDir = PrismProvider.AppBasePath;
            string dllPath = Path.Combine(exeDir, dllName);

            if (string.IsNullOrEmpty(dllPath) || !File.Exists(dllPath))
                return false;

            try
            {
                using var stream = File.OpenRead(dllPath);
                using var peReader = new System.Reflection.PortableExecutable.PEReader(stream);

                // 检查是否包含 .NET 元数据（即是否为托管程序集）
                if (!peReader.HasMetadata)
                    return false;

                // 可选：进一步尝试加载元数据以验证完整性
                var metadataReader = peReader.GetMetadataReader();
                // 如果能成功获取 MetadataReader，通常说明结构有效
                // （某些损坏的 DLL 可能在后续读取时报错，但这里已足够用于初步验证）

                return true;
            }
            catch (BadImageFormatException)
            {
                // 不是有效的 PE 文件 或 非托管 DLL
                return false;
            }
            catch (IOException)
            {
                // 文件被占用或无法访问
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                // 无权限访问
                return false;
            }
            catch
            {
                // 其他异常（如元数据损坏等），视为无效
                return false;
            }
        }

        private void AddWpfReferences(List<MetadataReference> refs)
        {
            try
            {
                // 通过已加载的 WPF 类型获取其所在程序集路径
                var windowsBasePath = typeof(System.Windows.Application).Assembly.Location;
                var presentationCorePath = typeof(System.Windows.Media.Brush).Assembly.Location;
                var presentationFrameworkPath = typeof(System.Windows.Controls.Button).Assembly.Location;

                refs.Add(MetadataReference.CreateFromFile(windowsBasePath));
                refs.Add(MetadataReference.CreateFromFile(presentationCorePath));
                refs.Add(MetadataReference.CreateFromFile(presentationFrameworkPath));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"无法加载 WPF 引用: {ex}");
                // 可选：抛出异常或记录日志
            }
        }

        private void AddReferenceIfExists(List<MetadataReference> refs, string assemblyName)
        {
            try
            {
                var asm = Assembly.Load(assemblyName);
                if (!string.IsNullOrEmpty(asm.Location))
                    refs.Add(MetadataReference.CreateFromFile(asm.Location));
            }
            catch
            {
                // 忽略无法加载的程序集
            }
        }

        private void UpdateCompileResultFeedback(IEnumerable<Diagnostic> diagnostics)
        {
            var diagnosticList = diagnostics
                .Where(d => d.Severity >= DiagnosticSeverity.Warning)
                .ToList();
            var warningCount = diagnosticList.Count(d => d.Severity == DiagnosticSeverity.Warning);

            var summary = warningCount > 0
                ? $"编译成功，存在 {warningCount} 条警告"
                : "编译成功";

            var detailPrefix = warningCount > 0
                ? "编译成功，程序集已更新。请留意以下警告。"
                : $"编译成功，程序集已输出到：{dllPath}";

            UpdateCompileFeedback(
                summary,
                BuildCompileOutputText(detailPrefix, diagnosticList),
                warningCount > 0 ? "#B7791F" : "#2F855A");
        }

        private string BuildCompileOutputText(string header, IEnumerable<Diagnostic> diagnostics)
        {
            var formattedDiagnostics = diagnostics
                .Where(d => d.Severity >= DiagnosticSeverity.Warning)
                .Select(FormatDiagnostic)
                .ToList();

            if (formattedDiagnostics.Count == 0)
            {
                return header;
            }

            return $"{header}\r\n\r\n{string.Join("\r\n", formattedDiagnostics)}";
        }

        private string FormatDiagnostic(Diagnostic diagnostic)
        {
            var locationText = "";
            if (diagnostic.Location != Location.None && diagnostic.Location.IsInSource)
            {
                var lineSpan = diagnostic.Location.GetLineSpan();
                var line = lineSpan.StartLinePosition.Line + 1;
                var column = lineSpan.StartLinePosition.Character + 1;
                var fileName = Path.GetFileName(lineSpan.Path);
                locationText = $"[{fileName}:{line},{column}] ";
            }

            var level = diagnostic.Severity switch
            {
                DiagnosticSeverity.Error => "错误",
                DiagnosticSeverity.Warning => "警告",
                DiagnosticSeverity.Info => "信息",
                _ => "提示",
            };

            return $"{level} {diagnostic.Id} {locationText}{diagnostic.GetMessage()}";
        }

        private void UpdateCompileFeedback(string summary, string details, string brush)
        {
            CompileStatusText = summary;
            CompileStatusBrush = brush;
            CompileOutput = string.IsNullOrWhiteSpace(details)
                ? "暂无编译输出。"
                : details;

            ErrorInfo = CompileOutput;
            ErrorVisibility = Visibility.Visible;
        }
        #endregion


        #endregion
    }
}
