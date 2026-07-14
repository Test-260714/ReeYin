using Microsoft.Win32;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace ReeYin.LicenseTool;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        ExpireDatePicker.SelectedDate = DateTime.Today.AddYears(1);
        OutputPathTextBox.Text = Path.Combine(GetDefaultOutputDirectory(), "license.json");
        ReadCurrentMachineCode();
        UpdateLicenseTypeInputs();
    }

    private static string GetDefaultOutputDirectory()
    {
        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        return string.IsNullOrWhiteSpace(desktop) ? Environment.CurrentDirectory : desktop;
    }

    private void ReadCurrentMachineButton_Click(object sender, RoutedEventArgs e)
    {
        ReadCurrentMachineCode();
    }

    private void BrowseOutputButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "保存 License 文件",
            Filter = "License 文件 (*.json)|*.json|所有文件 (*.*)|*.*",
            FileName = string.IsNullOrWhiteSpace(OutputPathTextBox.Text)
                ? "license.json"
                : Path.GetFileName(OutputPathTextBox.Text),
            InitialDirectory = Directory.Exists(Path.GetDirectoryName(OutputPathTextBox.Text))
                ? Path.GetDirectoryName(OutputPathTextBox.Text)
                : GetDefaultOutputDirectory()
        };

        if (dialog.ShowDialog(this) == true)
        {
            OutputPathTextBox.Text = dialog.FileName;
        }
    }

    private void BrowsePrivateKeyButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择 RSA 私钥 XML 文件",
            Filter = "XML 文件 (*.xml)|*.xml|所有文件 (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) == true)
        {
            PrivateKeyFileTextBox.Text = dialog.FileName;
        }
    }

    private void UseCurrentMachineCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (MachineCodeTextBox == null)
        {
            return;
        }

        bool useCurrentMachine = UseCurrentMachineCheckBox.IsChecked == true;
        MachineCodeTextBox.IsReadOnly = useCurrentMachine;
        MachineCodeTextBox.Opacity = useCurrentMachine ? 0.72 : 1.0;
    }

    private void LicenseTypeRadio_Checked(object sender, RoutedEventArgs e)
    {
        UpdateLicenseTypeInputs();
    }

    private void GenerateButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var request = BuildRequest();
            var result = LicenseGenerator.Generate(request);

            StatusTextBlock.Foreground = GetResourceBrush("PrimaryBrush", Brushes.SeaGreen);
            StatusTextBlock.Text =
                $"生成成功: {result.OutputPath}\n" +
                $"机器码: {result.License.MachineCode}\n" +
                $"类型: {result.License.Type}\n" +
                $"到期: {(result.License.ExpireTime.HasValue ? result.License.ExpireTime.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) : "永久")}\n" +
                $"模块: {(result.License.NormalizedModules.Count == 0 ? "ALL" : string.Join(", ", result.License.NormalizedModules))}";
        }
        catch (Exception ex)
        {
            StatusTextBlock.Foreground = Brushes.IndianRed;
            StatusTextBlock.Text = $"生成失败: {ex.Message}";
        }
    }

    private LicenseGenerationRequest BuildRequest()
    {
        bool useCurrentMachine = UseCurrentMachineCheckBox.IsChecked == true;
        string machineCode = MachineCodeTextBox.Text.Trim();
        string customerName = CustomerNameTextBox.Text.Trim();
        string version = VersionTextBox.Text.Trim();
        string outputPath = OutputPathTextBox.Text.Trim();

        if (!useCurrentMachine && string.IsNullOrWhiteSpace(machineCode))
        {
            throw new InvalidOperationException("请输入机器码，或勾选使用本机机器码。");
        }

        if (string.IsNullOrWhiteSpace(customerName))
        {
            throw new InvalidOperationException("请输入客户名称。");
        }

        if (string.IsNullOrWhiteSpace(version))
        {
            throw new InvalidOperationException("请输入版本。");
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new InvalidOperationException("请选择输出文件。");
        }

        return new LicenseGenerationRequest
        {
            UseCurrentMachine = useCurrentMachine,
            MachineCode = machineCode,
            CustomerName = customerName,
            Modules = LicenseGenerator.ParseModules(ModulesTextBox.Text),
            Version = version,
            ExpireTimeUtc = BuildExpireTimeUtc(),
            IsTrial = TrialRadio.IsChecked == true,
            OutputPath = outputPath,
            PrivateKeyFile = string.IsNullOrWhiteSpace(PrivateKeyFileTextBox.Text)
                ? null
                : PrivateKeyFileTextBox.Text.Trim()
        };
    }

    private DateTime? BuildExpireTimeUtc()
    {
        if (PermanentRadio.IsChecked == true)
        {
            return null;
        }

        if (TrialRadio.IsChecked == true)
        {
            if (!int.TryParse(TrialDaysTextBox.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int days) || days <= 0)
            {
                throw new InvalidOperationException("试用天数必须是大于 0 的整数。");
            }

            return DateTime.UtcNow.AddDays(days);
        }

        DateTime selectedDate = ExpireDatePicker.SelectedDate
            ?? throw new InvalidOperationException("请选择到期日期。");

        if (!TimeSpan.TryParse(ExpireTimeTextBox.Text.Trim(), CultureInfo.InvariantCulture, out TimeSpan time))
        {
            throw new InvalidOperationException("到期时间格式无效，请使用 HH:mm:ss。");
        }

        DateTime localExpireTime = DateTime.SpecifyKind(selectedDate.Date.Add(time), DateTimeKind.Local);
        return localExpireTime.ToUniversalTime();
    }

    private void ReadCurrentMachineCode()
    {
        try
        {
            MachineCodeTextBox.Text = LicenseGenerator.GetCurrentMachineCode();
            StatusTextBlock.Foreground = GetResourceBrush("MainPrimaryTextIconBrush", Brushes.Gray);
            StatusTextBlock.Text = "已读取本机机器码。";
        }
        catch (Exception ex)
        {
            StatusTextBlock.Foreground = Brushes.IndianRed;
            StatusTextBlock.Text = $"读取机器码失败: {ex.Message}";
        }
    }

    private void UpdateLicenseTypeInputs()
    {
        if (ExpireDatePicker == null)
        {
            return;
        }

        bool isPermanent = PermanentRadio.IsChecked == true;
        bool isTrial = TrialRadio.IsChecked == true;

        ExpireDatePicker.IsEnabled = !isPermanent && !isTrial;
        ExpireTimeTextBox.IsEnabled = !isPermanent && !isTrial;
        TrialDaysTextBox.IsEnabled = isTrial;
    }

    private Brush GetResourceBrush(string key, Brush fallback)
    {
        return TryFindResource(key) as Brush ?? fallback;
    }
}
