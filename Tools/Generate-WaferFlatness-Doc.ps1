$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.IO.Compression.FileSystem

$desktop = [Environment]::GetFolderPath('Desktop')
$outputPath = Join-Path $desktop 'WaferFlatness_SensorDataCollection说明.docx'
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("WaferFlatnessDoc_" + [System.Guid]::NewGuid().ToString("N"))
$docRoot = Join-Path $tempRoot 'docx'

New-Item -ItemType Directory -Path $docRoot | Out-Null
New-Item -ItemType Directory -Path (Join-Path $docRoot '_rels') | Out-Null
New-Item -ItemType Directory -Path (Join-Path $docRoot 'docProps') | Out-Null
New-Item -ItemType Directory -Path (Join-Path $docRoot 'word') | Out-Null

$contentTypes = @'
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
  <Override PartName="/docProps/core.xml" ContentType="application/vnd.openxmlformats-package.core-properties+xml"/>
  <Override PartName="/docProps/app.xml" ContentType="application/vnd.openxmlformats-officedocument.extended-properties+xml"/>
</Types>
'@

$rels = @'
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
  <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties" Target="docProps/core.xml"/>
  <Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties" Target="docProps/app.xml"/>
</Relationships>
'@

$created = (Get-Date).ToUniversalTime().ToString("s") + "Z"
$core = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<cp:coreProperties xmlns:cp="http://schemas.openxmlformats.org/package/2006/metadata/core-properties" xmlns:dc="http://purl.org/dc/elements/1.1/" xmlns:dcterms="http://purl.org/dc/terms/" xmlns:dcmitype="http://purl.org/dc/dcmitype/" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <dc:title>WaferFlatness SensorDataCollection 说明</dc:title>
  <dc:subject>ModelParamBase 与 DialogViewModelBase 说明</dc:subject>
  <dc:creator>Codex</dc:creator>
  <cp:lastModifiedBy>Codex</cp:lastModifiedBy>
  <dcterms:created xsi:type="dcterms:W3CDTF">$created</dcterms:created>
  <dcterms:modified xsi:type="dcterms:W3CDTF">$created</dcterms:modified>
</cp:coreProperties>
"@

$app = @'
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Properties xmlns="http://schemas.openxmlformats.org/officeDocument/2006/extended-properties" xmlns:vt="http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes">
  <Application>Microsoft Office Word</Application>
</Properties>
'@

function Escape-XmlText {
    param([string]$Text)
    if ($null -eq $Text) { return '' }
    return [System.Security.SecurityElement]::Escape($Text)
}

function New-ParagraphXml {
    param(
        [string]$Text,
        [switch]$Title,
        [switch]$Heading
    )

    $escaped = Escape-XmlText $Text

    if ($Title) {
        return "<w:p><w:pPr><w:jc w:val=`"center`"/></w:pPr><w:r><w:rPr><w:b/><w:sz w:val=`"32`"/><w:rFonts w:eastAsia=`"Microsoft YaHei`" w:ascii=`"Calibri`" w:hAnsi=`"Calibri`"/></w:rPr><w:t xml:space=`"preserve`">$escaped</w:t></w:r></w:p>"
    }

    if ($Heading) {
        return "<w:p><w:r><w:rPr><w:b/><w:sz w:val=`"28`"/><w:rFonts w:eastAsia=`"Microsoft YaHei`" w:ascii=`"Calibri`" w:hAnsi=`"Calibri`"/></w:rPr><w:t xml:space=`"preserve`">$escaped</w:t></w:r></w:p>"
    }

    return "<w:p><w:r><w:rPr><w:sz w:val=`"22`"/><w:rFonts w:eastAsia=`"Microsoft YaHei`" w:ascii=`"Calibri`" w:hAnsi=`"Calibri`"/></w:rPr><w:t xml:space=`"preserve`">$escaped</w:t></w:r></w:p>"
}

$paragraphs = @(
    @{ Text = 'Custom.WaferFlatnessMeasure 中 SensorDataCollectionModel 与 SensorDataCollectionViewModel 说明'; Title = $true },
    @{ Text = '一、ModelParamBase 在这个项目中的职责'; Heading = $true },
    @{ Text = 'ModelParamBase 不是要求子类把所有成员都重写一遍，而是定义了一个流程节点模型的运行约定。子类只需要在关键生命周期节点上补上自己的业务逻辑。' },
    @{ Text = '它本身已经提供了 Serial、Guid、moduleInputParam、moduleOutputParam、InputParams、OutputParams、RecipeParams、OutputParamResource、Output、TriggerModuleRun 等通用成员，用来承接节点输入输出、配方参数、运行状态和缓存同步。' },
    @{ Text = '二、SensorDataCollectionModel 需要重点实现或正确配置的内容'; Heading = $true },
    @{ Text = '1. OnceInit()：这是整个程序运行周期内的一次性初始化入口。' },
    @{ Text = '原因：基类 OnceInit() 只负责节点缓存注册、节点删除事件订阅和配方同步；而 SensorDataCollectionModel 还要在这里完成事件自动订阅、FlatCalib_Algorithm 与 Flatness_Algorithm 创建、动态结果页注册，以及把 TriggerModuleRun 绑定到自己的执行逻辑。没有这些，模块虽然存在，但不会真正具备晶圆平整度采集和处理能力。' },
    @{ Text = '2. TriggerModuleRun 与实际 ExecuteModule()：框架真正执行节点时，最终走的是 TriggerModuleRun。' },
    @{ Text = '原因：SensorDataCollectionModel 虽然没有覆盖基类同名 ExecuteModule 的签名，但它在 OnceInit() 中把 TriggerModuleRun 指向了自己的 ExecuteModule()，这样模块执行时才会根据 SltTriggerPicIndex 分别走 CSV 导入、文件方式或传感器采集，并在完成后刷新输出参数、更新节点输出缓存和全局参数。' },
    @{ Text = '3. LoadKeyParam()：这是参数加载入口。' },
    @{ Text = '原因：基类默认会做 TransferParamSync()、应用配方参数以及同步带 InputParam 标记的输入值。当前子类在此基础上额外保证 MeasureParam 一定存在，并同步到 ALGO 中，这样界面上修改的测量参数才能真正作用到后续平面度、平行度和 TTV 计算。' },
    @{ Text = '4. Dispose()：这是资源释放入口。' },
    @{ Text = '原因：基类 Dispose() 会清理节点缓存、全局参数、配方映射和动态视图。当前 SensorDataCollectionModel 没有额外释放动作，所以只调用 base.Dispose()。即便如此，这个覆盖点仍然重要，因为一旦后续增加传感器事件句柄、缓存文件或长生命周期对象，就应该继续在这里追加释放逻辑。' },
    @{ Text = '5. 用 OutputParam 标记需要暴露给流程图或其它模块的输出参数。' },
    @{ Text = '原因：基类会通过反射扫描 OutputParam 特性，自动生成 OutputParamResource，并把这些字段或属性变成界面可选择的输出项。当前模型中的 FilePath、PreDatas、PreDataCount、LastPreDatasCsvPath、LastParallelismValue、LastUpSurfaceFlatnessValue、LastDownSurfaceFlatnessValue、LastTtvValue、LastThicknessMinValue、LastThicknessMaxValue 都属于这类输出。没有标记，它们就不会出现在输出参数选择区，也无法传递给下游节点。' },
    @{ Text = '6. 如需接收上游节点参数，则要定义带 InputParam 的 TransmitParam 成员。' },
    @{ Text = '原因：基类 LoadKeyParam() 会扫描所有带 InputParam 的 TransmitParam，并从 moduleInputParam 同步实际值。当前 SensorDataCollectionModel 没有定义任何 InputParam，这说明它在 WaferFlatnessMeasure 中更像一个采集源头模块，而不是依赖上游图像或结果的消费模块。' },
    @{ Text = '7. 如需把参数纳入配方管理，则要用 ReassignParam 标记。' },
    @{ Text = '原因：基类 OnceInit() 会触发配方同步，底层会通过 RecipeParamCollectorAdapter 反射调用 ReeYin_V.Share.ReassignParamCollector 收集这些标记项。当前模型只把 IsLinkVisibility 和 AcquisitionMode 标成 ReassignParam，表示它们会随模块配置或配方保存和恢复。' },
    @{ Text = '三、SensorDataCollectionModel 中的重要参数分类'; Heading = $true },
    @{ Text = '1. 节点框架参数：Serial、Guid、moduleInputParam、moduleOutputParam、InputParams、OutputParams、RecipeParams、Output、OutputParamResource。' },
    @{ Text = '作用：这些主要由基类维护，用于节点编号、输入输出缓存、运行状态显示、输出资源收集和配方参数同步。子类通常不自己重写，但必须遵循它们的使用方式。' },
    @{ Text = '2. 采集与设备相关参数：Models、SltModel、SltModelName、SltTriggerPicIndex、StartCollect、StopCollect、StopAndDispose、StartEventName、StopEventName、StopAndDisposeEventName。' },
    @{ Text = '作用：决定当前选中的传感器、采集模式以及外部事件如何触发开始采集、停止采集和停止后处理。' },
    @{ Text = '3. 数据与结果参数：PreDatas、ValidCollect、DownValidCollect、LastPreDatasCsvPath、LastParallelismValue、LastUpSurfaceFlatnessValue、LastDownSurfaceFlatnessValue、LastTtvValue、LastThicknessMinValue、LastThicknessMaxValue、ResultPointCloud、LastThicknessRawPointCloud、LastThicknessPointCloud、LastThicknessNormalPointCloud。' },
    @{ Text = '作用：承接预处理后的点数据、算法结果和点云展示结果，其中部分通过 OutputParam 暴露给流程图。' },
    @{ Text = '4. 算法配置参数：MeasureParam。' },
    @{ Text = '作用：界面直接绑定到 MeasureParam.Interpolate、Resolution、Function、Epsilon、Smooth、OuterRadius、InnerRadius；模型中又会把它同步给 ALGO，所以它是“界面参数”和“算法实参”之间的桥梁。' },
    @{ Text = '5. 存储与过滤参数：IsUsingChannel4RangeFilter、Channel4FilterMin、Channel4FilterMax、DownSurfaceFilterMin、DownSurfaceFilterMax、IsSavePreDatasToCsv、PreDatasCsvDirectory、PointCloudOutputDirectory。' },
    @{ Text = '作用：控制预处理数据过滤、CSV 导出和点云导出位置。' },
    @{ Text = '四、DialogViewModelBase 在这个项目中的职责'; Heading = $true },
    @{ Text = 'DialogViewModelBase 负责的是参数页弹窗生命周期，而不是采集业务本身。它实现了 Prism 的 IDialogAware，用来统一处理弹窗打开、初始化、关闭和结果回传。' },
    @{ Text = '它提供了 Name、Serial、Title、Visibility、Icon、Param、Guid、ModelParam 等通用属性，用来接收外部传入的弹窗参数并与具体页面绑定。' },
    @{ Text = 'OnDialogOpened() 会从 DialogParameters 中读取 Guid、Title、Serial、Visibility、Icon、Param 等值，然后调用虚方法 InitParam()，把“读取弹窗参数”和“具体页面初始化”这两件事分开。' },
    @{ Text = 'InitModelParam<T>() 是它提供的标准初始化入口。这个方法会自动完成 ResolveModelParam、Serial 写入、ModelParam 赋值、OnceInit()、LoadSpecificConfig()、InitOutputParamResource(Guid)、TransferParam()、IsDebug = true 等通用动作。' },
    @{ Text = 'LoadSpecificConfig() 的作用是给模型挂接通用图像窗口 mWindowH，并用 Serial 生成模块名。对需要图像或结果展示的模块，这是一个统一的基础设施入口。' },
    @{ Text = 'CloseDialog() 则通过 RequestClose 把 ButtonResult 和 dialogParameters 回传给调用方，所以页面确认或取消后，外部模块编辑器可以拿到最新的 ModelParam。' },
    @{ Text = '五、SensorDataCollectionViewModel 如何使用 DialogViewModelBase'; Heading = $true },
    @{ Text = 'SensorDataCollectionViewModel 继承 DialogViewModelBase 后，获得了完整的弹窗生命周期和公共参数承接能力，但它当前没有完全走基类推荐的 InitModelParam<T>() 流程，而是自己重写了 InitParam()。' },
    @{ Text = '当前 InitParam() 的做法是：先把 Param 转成 SensorDataCollectionModel，若没有则新建；然后调用本地 Init() 构建输出资源；再恢复已选择的传感器；最后调用 ModelParam.TransferParam()。' },
    @{ Text = '它的 Init() 里通过 OutputParamCollector.GetDataPoints(typeof(SensorDataCollectionModel)) 手动收集所有带 OutputParam 的成员，填充到 ModelParam.OutputParamResource 中，并初始化 SltOutputParamName。' },
    @{ Text = '页面 Loaded 事件会触发 LoadCommand；LoadCommand 中又会调用 RestoreSelectedSensor()、ModelParam.LoadKeyParam()，并在隐藏模式下自动关闭弹窗返回结果。也就是说，这个 ViewModel 把一部分初始化放在了 InitParam()，另一部分放在了 Loaded 后执行。' },
    @{ Text = 'OnDialogClosed() 中把 ModelParam.IsDebug 设回 false，用于退出调试态。' },
    @{ Text = 'GeneralCommand、LoadCommand、DataOperateCommand 则负责具体页面交互，例如切换传感器、开始采集、停止采集、打开文件选择器、选择输出参数、确认回传 ModelParam 等。' },
    @{ Text = '六、两层基类在这个模块里的分工'; Heading = $true },
    @{ Text = 'ModelParamBase 解决的是“模块怎么作为流程节点运行、传参与输出”的问题。' },
    @{ Text = 'DialogViewModelBase 解决的是“模块参数页怎么作为弹窗打开、初始化和关闭”的问题。' },
    @{ Text = '在 Custom.WaferFlatnessMeasure 中，SensorDataCollectionModel 负责采集、预处理、算法调用和输出结果管理；SensorDataCollectionViewModel 负责把这些模型能力组织成可编辑、可确认、可回传的配置页面。' },
    @{ Text = '七、补充说明'; Heading = $true },
    @{ Text = '同项目中的 WaferFlatnessConfigViewModel 使用方式更标准，它在 InitParam() 中直接调用 ModelParam = InitModelParam<SensorMotionControlModel>()，然后再调用 ModelParam.LoadKeyParam()。相比之下，SensorDataCollectionViewModel 目前保留了较多历史写法。' },
    @{ Text = '如果后续希望统一风格，可以考虑把 SensorDataCollectionViewModel 也逐步改成 InitModelParam<SensorDataCollectionModel>() 的模式，这样会与 DialogViewModelBase 的设计更一致。' }
)

$bodyBuilder = New-Object System.Text.StringBuilder
foreach ($paragraph in $paragraphs) {
    [void]$bodyBuilder.AppendLine((New-ParagraphXml -Text $paragraph.Text -Title:([bool]$paragraph.Title) -Heading:([bool]$paragraph.Heading)))
}

$document = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:document xmlns:wpc="http://schemas.microsoft.com/office/word/2010/wordprocessingCanvas" xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006" xmlns:o="urn:schemas-microsoft-com:office:office" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:m="http://schemas.openxmlformats.org/officeDocument/2006/math" xmlns:v="urn:schemas-microsoft-com:vml" xmlns:wp14="http://schemas.microsoft.com/office/word/2010/wordprocessingDrawing" xmlns:wp="http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing" xmlns:w10="urn:schemas-microsoft-com:office:word" xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main" xmlns:w14="http://schemas.microsoft.com/office/word/2010/wordml" xmlns:w15="http://schemas.microsoft.com/office/word/2012/wordml" xmlns:wpg="http://schemas.microsoft.com/office/word/2010/wordprocessingGroup" xmlns:wpi="http://schemas.microsoft.com/office/word/2010/wordprocessingInk" xmlns:wne="http://schemas.microsoft.com/office/word/2006/wordml" xmlns:wps="http://schemas.microsoft.com/office/word/2010/wordprocessingShape" mc:Ignorable="w14 w15 wp14">
  <w:body>
$($bodyBuilder.ToString())
    <w:sectPr>
      <w:pgSz w:w="11906" w:h="16838"/>
      <w:pgMar w:top="1440" w:right="1440" w:bottom="1440" w:left="1440" w:header="720" w:footer="720" w:gutter="0"/>
    </w:sectPr>
  </w:body>
</w:document>
"@

[System.IO.File]::WriteAllText((Join-Path $docRoot '[Content_Types].xml'), $contentTypes, [System.Text.UTF8Encoding]::new($false))
[System.IO.File]::WriteAllText((Join-Path $docRoot '_rels\.rels'), $rels, [System.Text.UTF8Encoding]::new($false))
[System.IO.File]::WriteAllText((Join-Path $docRoot 'docProps\core.xml'), $core, [System.Text.UTF8Encoding]::new($false))
[System.IO.File]::WriteAllText((Join-Path $docRoot 'docProps\app.xml'), $app, [System.Text.UTF8Encoding]::new($false))
[System.IO.File]::WriteAllText((Join-Path $docRoot 'word\document.xml'), $document, [System.Text.UTF8Encoding]::new($false))

if (Test-Path $outputPath) {
    Remove-Item -LiteralPath $outputPath -Force
}

$zipPath = Join-Path $tempRoot 'document.zip'
[System.IO.Compression.ZipFile]::CreateFromDirectory($docRoot, $zipPath)
Move-Item -LiteralPath $zipPath -Destination $outputPath

Remove-Item -LiteralPath $tempRoot -Recurse -Force

Write-Output $outputPath
