using ACS.SPiiPlusNET;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReeYin_V.Hardware.ControlCard.ACS.App;

public sealed class AcsProgramBufferStatus
{
    public AcsProgramBufferStatus(int bufferNo, ProgramStates state, int error)
    {
        BufferNo = bufferNo;
        State = state;
        Error = error;
    }

    public int BufferNo { get; }

    public ProgramStates State { get; }

    public int Error { get; }

    public bool IsRunning => (State & ProgramStates.ACSC_PST_RUN) != 0;

    public bool IsCompiled => (State & ProgramStates.ACSC_PST_COMPILED) != 0;

    public string StateText => State.ToString();

    public string ErrorText => Error.ToString();

    public string Summary =>
        $"Buffer={BufferNo}; 状态={StateText}; 错误={ErrorText}; 运行={(IsRunning ? "是" : "否")}; 已编译={(IsCompiled ? "是" : "否")}";
}

public partial class AcsControlCard
{
    private const string RequiredLciDeclaration = "global LCI lc !定义激光变量";
    private const string RequiredPiDeclaration = "global REAL pi = 3.141592653589793";

    public bool TryUploadProgramBuffer(int bufferNo, out string script, out string message)
    {
        script = string.Empty;
        if (!TryPrepareProgramBuffer(bufferNo, out var buffer, out message))
        {
            return false;
        }

        try
        {
            script = _api.UploadBuffer(buffer) ?? string.Empty;
            message = $"Buffer {bufferNo} 读取完成。";
            return true;
        }
        catch (Exception ex)
        {
            message = $"读取 Buffer {bufferNo} 失败: {ex.Message}";
            Console.WriteLine($"ACS {message}");
            return false;
        }
    }

    public bool TryLoadProgramBuffer(int bufferNo, string script, out string message)
    {
        if (!TryPrepareProgramBuffer(bufferNo, out var buffer, out message))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(script))
        {
            message = "ACSPL+ 脚本不能为空。";
            return false;
        }

        try
        {
            _api.LoadBuffer(buffer, script);
            message = $"Buffer {bufferNo} 下载完成。";
            return true;
        }
        catch (Exception ex)
        {
            message = $"下载 Buffer {bufferNo} 失败: {ex.Message}";
            Console.WriteLine($"ACS {message}");
            return false;
        }
    }

    public bool TryAppendProgramBuffer(int bufferNo, string script, out string message)
    {
        if (!TryPrepareProgramBuffer(bufferNo, out var buffer, out message))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(script))
        {
            message = "ACSPL+ 脚本不能为空。";
            return false;
        }

        try
        {
            _api.AppendBuffer(buffer, script);
            message = $"Buffer {bufferNo} 追加完成。";
            return true;
        }
        catch (Exception ex)
        {
            message = $"追加 Buffer {bufferNo} 失败: {ex.Message}";
            Console.WriteLine($"ACS {message}");
            return false;
        }
    }

    public bool TryCompileProgramBuffer(int bufferNo, out string message)
    {
        if (!TryPrepareProgramBuffer(bufferNo, out var buffer, out message))
        {
            return false;
        }

        try
        {
            _api.CompileBuffer(buffer);
            message = $"Buffer {bufferNo} 编译完成。";
            return true;
        }
        catch (Exception ex)
        {
            message = $"编译 Buffer {bufferNo} 失败: {ex.Message}; {FormatProgramDiagnostics(bufferNo)}";
            Console.WriteLine($"ACS {message}");
            return false;
        }
    }

    public bool TryRunProgramBuffer(int bufferNo, string? label, out string message)
    {
        if (!TryPrepareProgramBuffer(bufferNo, out var buffer, out message))
        {
            return false;
        }

        try
        {
            _api.RunBuffer(buffer, string.IsNullOrWhiteSpace(label) ? null : label.Trim());
            message = $"Buffer {bufferNo} 已启动。";
            return true;
        }
        catch (Exception ex)
        {
            message = $"运行 Buffer {bufferNo} 失败: {ex.Message}; {FormatProgramDiagnostics(bufferNo)}";
            Console.WriteLine($"ACS {message}");
            return false;
        }
    }

    public bool TryStopProgramBuffer(int bufferNo, out string message)
    {
        if (!TryPrepareProgramBuffer(bufferNo, out var buffer, out message))
        {
            return false;
        }

        try
        {
            _api.StopBuffer(buffer);
            message = $"Buffer {bufferNo} 已停止。";
            return true;
        }
        catch (Exception ex)
        {
            message = $"停止 Buffer {bufferNo} 失败: {ex.Message}; {FormatProgramDiagnostics(bufferNo)}";
            Console.WriteLine($"ACS {message}");
            return false;
        }
    }

    public bool TryClearProgramBuffer(int bufferNo, out string message)
    {
        if (!TryPrepareProgramBuffer(bufferNo, out var buffer, out message))
        {
            return false;
        }

        try
        {
            _api.ClearBuffer(buffer, 1, 100000);
            message = $"Buffer {bufferNo} 已清空。";
            return true;
        }
        catch (Exception ex)
        {
            message = $"清空 Buffer {bufferNo} 失败: {ex.Message}; {FormatProgramDiagnostics(bufferNo)}";
            Console.WriteLine($"ACS {message}");
            return false;
        }
    }

    public bool TryGetProgramBufferStatus(int bufferNo, out AcsProgramBufferStatus status, out string message)
    {
        status = new AcsProgramBufferStatus(bufferNo, default, 0);
        if (!TryPrepareProgramBuffer(bufferNo, out _, out message))
        {
            return false;
        }

        if (!TryGetProgramState(bufferNo, out var state, out message))
        {
            return false;
        }

        if (!TryGetProgramError(bufferNo, out var error, out message))
        {
            return false;
        }

        status = new AcsProgramBufferStatus(bufferNo, state, error);
        message = status.Summary;
        return true;
    }

    public string GetProgramBufferDiagnostics(int bufferNo)
    {
        if (!IsConnected)
        {
            return "ACS 控制卡未连接。";
        }

        return TryToProgramBuffer(bufferNo, out _, out var message)
            ? FormatProgramDiagnostics(bufferNo)
            : message;
    }

    public bool TryGetDBufferNo(out int bufferNo, out string message)
    {
        bufferNo = 0;
        if (!IsConnected)
        {
            message = "ACS 控制卡未连接。";
            return false;
        }

        try
        {
            bufferNo = (int)_api.GetDBufferIndex();
            if (!TryToProgramBuffer(bufferNo, out _, out message))
            {
                return false;
            }

            message = $"D-Buffer 编号为 {bufferNo}。";
            return true;
        }
        catch (Exception ex)
        {
            message = $"读取 D-Buffer 编号失败: {ex.Message}";
            Console.WriteLine($"ACS {message}");
            return false;
        }
    }

    private bool TryPrepareProgramBuffer(int bufferNo, out ProgramBuffer buffer, out string message)
    {
        if (!TryToProgramBuffer(bufferNo, out buffer, out message))
        {
            return false;
        }

        if (!IsConnected)
        {
            message = "ACS 控制卡未连接。";
            return false;
        }

        return true;
    }

    private static bool TryToProgramBuffer(int bufferNo, out ProgramBuffer buffer, out string message)
    {
        if (bufferNo < 0 || bufferNo > 64)
        {
            buffer = ProgramBuffer.ACSC_NONE;
            message = $"Buffer {bufferNo} 无效，有效范围为 0..64。";
            return false;
        }

        buffer = (ProgramBuffer)bufferNo;
        message = string.Empty;
        return true;
    }

    private bool RunProgramBuffer(int bufferNo, out string message)
    {
        if (!TryToProgramBuffer(bufferNo, out var buffer, out message))
        {
            return false;
        }

        try
        {
            _api.RunBuffer(buffer, null);
            message = $"Buffer {bufferNo} 已启动。";
            return true;
        }
        catch (Exception ex)
        {
            message = $"运行 Buffer {bufferNo} 失败: {ex.Message}";
            Console.WriteLine($"ACS {message}");
            return false;
        }
    }

    private bool WaitProgramBufferEnd(int bufferNo, int timeout, out string message)
    {
        if (!TryToProgramBuffer(bufferNo, out var buffer, out message))
        {
            return false;
        }

        try
        {
            _api.WaitProgramEnd(buffer, timeout);
            message = $"Buffer {bufferNo} 运行结束。";
            return true;
        }
        catch (Exception ex)
        {
            message = $"等待 Buffer {bufferNo} 运行结束失败: {ex.Message}; {FormatProgramDiagnostics(bufferNo)}";
            Console.WriteLine($"ACS {message}");
            return false;
        }
    }

    private bool StopProgramBuffer(int bufferNo, out string message)
    {
        if (!TryToProgramBuffer(bufferNo, out var buffer, out message))
        {
            return false;
        }

        try
        {
            _api.StopBuffer(buffer);
            message = $"Buffer {bufferNo} 已停止。";
            return true;
        }
        catch (Exception ex)
        {
            message = $"停止 Buffer {bufferNo} 失败: {ex.Message}";
            Console.WriteLine($"ACS {message}");
            return false;
        }
    }

    private bool TryGetProgramState(int bufferNo, out ProgramStates state, out string message)
    {
        state = default;
        if (!TryToProgramBuffer(bufferNo, out var buffer, out message))
        {
            return false;
        }

        try
        {
            state = _api.GetProgramState(buffer);
            message = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            message = $"读取 Buffer {bufferNo} 状态失败: {ex.Message}";
            return false;
        }
    }

    private bool TryGetProgramError(int bufferNo, out int error, out string message)
    {
        error = 0;
        if (!TryToProgramBuffer(bufferNo, out var buffer, out message))
        {
            return false;
        }

        try
        {
            error = _api.GetProgramError(buffer);
            message = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            message = $"读取 Buffer {bufferNo} 错误码失败: {ex.Message}";
            return false;
        }
    }

    private string FormatProgramDiagnostics(int bufferNo)
    {
        var stateText = TryGetProgramState(bufferNo, out var state, out var stateMessage)
            ? state.ToString()
            : stateMessage;
        var errorText = TryGetProgramError(bufferNo, out var error, out var errorMessage)
            ? error.ToString()
            : errorMessage;

        return $"Buffer={bufferNo}; 状态={stateText}; 错误={errorText}";
    }


    public bool TryEnsureDBufferLciDeclarations(out string message)
    {
        try
        {
            if (!TryGetDBufferNo(out var dBufferNo, out var dBufferMessage))
            {
                message = dBufferMessage;
                return false;
            }

            if (!TryUploadProgramBuffer(dBufferNo, out var script, out var uploadMessage))
            {
                message = $"{dBufferMessage}{Environment.NewLine}{uploadMessage}";
                return false;
            }

            var updatedScript = BuildDBufferLciDeclarationScript(script, out var declarationsChanged);
            if (!declarationsChanged)
            {
                message = $"{dBufferMessage}{Environment.NewLine}{uploadMessage}{Environment.NewLine}D-Buffer 已包含 LCI 全局声明。";
                return true;
            }

            if (!TryLoadProgramBuffer(dBufferNo, updatedScript, out var downloadMessage))
            {
                message = $"{dBufferMessage}{Environment.NewLine}{uploadMessage}{Environment.NewLine}{downloadMessage}";
                return false;
            }

            if (!TryCompileProgramBuffer(dBufferNo, out var compileMessage))
            {
                message = $"{dBufferMessage}{Environment.NewLine}{uploadMessage}{Environment.NewLine}{downloadMessage}{Environment.NewLine}{compileMessage}";
                return false;
            }

            if (!TryUploadProgramBuffer(dBufferNo, out var verifiedScript, out var verifyMessage))
            {
                message = $"{dBufferMessage}{Environment.NewLine}{uploadMessage}{Environment.NewLine}{downloadMessage}{Environment.NewLine}{compileMessage}{Environment.NewLine}{verifyMessage}";
                return false;
            }

            if (!ContainsRequiredDBufferDeclarations(verifiedScript))
            {
                message = $"{dBufferMessage}{Environment.NewLine}{uploadMessage}{Environment.NewLine}{downloadMessage}{Environment.NewLine}{compileMessage}{Environment.NewLine}{verifyMessage}{Environment.NewLine}D-Buffer 写入后回读仍缺少 LCI 全局声明。";
                return false;
            }

            message = $"{dBufferMessage}{Environment.NewLine}{uploadMessage}{Environment.NewLine}{downloadMessage}{Environment.NewLine}{compileMessage}{Environment.NewLine}{verifyMessage}{Environment.NewLine}D-Buffer LCI 全局声明已写入并编译。";
            return true;
        }
        catch (Exception ex)
        {
            message = $"D-Buffer LCI 全局声明检查/写入失败: {ex.Message}";
            Console.WriteLine($"ACS {message}");
            return false;
        }
    }

    private void EnsureDBufferLciDeclarations()
    {
        EnsureDBufferLciDeclarations(out _);
    }

    private bool EnsureDBufferLciDeclarations(out string message)
    {
        var ok = TryEnsureDBufferLciDeclarations(out message);
        Console.WriteLine(ok
            ? $"ACS {message}"
            : $"ACS D-Buffer LCI declaration ensure failed: {message}");
        return ok;
    }

    private static string BuildDBufferLciDeclarationScript(string script, out bool changed)
    {
        changed = false;
        var lines = (script ?? string.Empty)
            .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None)
            .ToList();

        if (lines.Count == 1 && lines[0].Length == 0)
        {
            lines.Clear();
        }

        var result = new List<string>
        {
            RequiredLciDeclaration,
            RequiredPiDeclaration
        };

        foreach (var line in lines)
        {
            if (IsRequiredDBufferDeclaration(line))
            {
                continue;
            }

            result.Add(line);
        }

        changed = !lines.SequenceEqual(result);
        return string.Join(Environment.NewLine, result);
    }

    private static bool IsRequiredDBufferDeclaration(string line)
    {
        return IsLciDeclaration(line) || IsPiDeclaration(line);
    }

    private static bool IsLciDeclaration(string line)
    {
        return NormalizeAcsplDeclaration(line) == "GLOBAL LCI LC";
    }

    private static bool IsPiDeclaration(string line)
    {
        var normalized = NormalizeAcsplDeclaration(line);
        return normalized == "GLOBAL REAL PI"
            || normalized.StartsWith("GLOBAL REAL PI=", StringComparison.Ordinal);
    }

    private static bool ContainsRequiredDBufferDeclarations(string script)
    {
        return ContainsExactAcsplDeclaration(script, RequiredLciDeclaration)
            && ContainsExactAcsplDeclaration(script, RequiredPiDeclaration);
    }

    private static bool ContainsExactAcsplDeclaration(string script, string declaration)
    {
        foreach (var line in script.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None))
        {
            if (line.Trim() == declaration)
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsAcsplDeclaration(string script, string declaration)
    {
        var expected = NormalizeAcsplDeclaration(declaration);
        foreach (var line in script.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
        {
            if (NormalizeAcsplDeclaration(line) == expected)
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizeAcsplDeclaration(string value)
    {
        var commentIndex = value.IndexOf('!');
        var code = commentIndex >= 0 ? value[..commentIndex] : value;
        var normalized = string.Join(
            " ",
            code.Replace("=", " = ")
                .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries));

        return normalized
            .Replace(" = ", "=")
            .ToUpperInvariant();
    }

}
