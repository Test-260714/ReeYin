# ACSPL+ 指令中文速查说明

来源：`E:\individual\控制卡\ACS\ACSPL-Commands-Variables-Reference-Guide.pdf`。

- 原文档：ACSPL+ Commands & Variables Reference Guide
- 原文档版本：3.13.01
- 原文档发布日期：February 2023
- 整理日期：2026-05-20
- 覆盖范围：第 2 章 ACSPL+ Commands（113 条/项）和第 6 章 Terminal Commands（58 条/项）。变量、函数、错误码未在本文展开。

> 说明：本文用于项目内快速检索，命令名保持英文，中文说明为摘要整理；实际编程时请以原厂 PDF 中对应页的语法、参数、开关和固件支持范围为准。

## 1. ACSPL+ 程序指令

ACSPL+ 程序指令可写入控制器程序缓冲区，也可通过支持的接口下发执行。

### 1.1 轴管理指令

用于轴使能、故障清除、轴组、回零、电子齿轮/跟随、运动中止以及安全响应配置。

| 指令 | 中文说明 | PDF 页 |
| --- | --- | --- |
| `BREAK` | 立即结束指定轴当前运动，不按常规减速曲线停车；若运动队列中已有下一段运动，则转入下一段。 | 49 |
| `COMMUT` | 执行无刷直流/交流伺服电机的自动换相。 | 50 |
| `CONNECT` | 定义参考位置 RPOS 的计算公式，用于将轴参考位置与变量或表达式建立关系。 | 52 |
| `CSCREATE` | 创建新的本地坐标系，用于局部坐标运动。 | 54 |
| `CSDESTROY` | 取消当前活动的本地坐标系。 | 58 |
| `DEPENDS` | 定义电机与轴之间的逻辑依赖关系。 | 59 |
| `DISABLE/DISABLEALL` | 关闭一个或多个驱动；DISABLEALL 对全部轴执行关闭驱动操作。 | 60 |
| `ENABLE/ENABLE ALL` | 使能一个或多个驱动。 | 61 |
| `ENCINIT` | 配置编码器参数；在缓冲区中执行时会等待初始化完成。 | 62 |
| `ENCREAD` | 读取编码器参数，例如分辨率、最大频率等。 | 64 |
| `FCLEAR` | 清除故障状态。 | 65 |
| `FOLLOW` | 将轴切换到从动/跟随模式。 | 66 |
| `GO` | 启动之前用 /w 选项创建并等待执行的运动。 | 66 |
| `GROUP` | 定义轴组，用于多轴协调运动。 | 67 |
| `HALT` | 按三阶减速轮廓和 DEC 减速度终止一个或多个运动；HALT ALL 作用于全部轴。 | 68 |
| `HOME` | 按预定义回零方法执行回零，可传入回零速度、最大距离、偏移、限流等参数。 | 69 |
| `IMM` | 在运动过程中在线修改运动参数。 | 71 |
| `KILL/KILLALL` | 按二阶减速轮廓和 KDEC 减速度终止运动；KILLALL 作用于全部轴。 | 72 |
| `SAFETYCONF` | 配置一个或多个轴的故障处理方式。 | 74 |
| `SAFETYGROUP` | 创建安全轴组；组内任一轴触发故障时，按组处理相关轴的响应。 | 75 |
| `SET` | 设置反馈位置 FPOS、参考位置 RPOS 或轴位置 APOS 的当前值。 | 76 |
| `SPLIT/SPLITALL` | 拆分轴组；SPLITALL 拆分全部轴组。 | 76 |
| `UNFOLLOW` | 将轴从从动/跟随模式切回普通模式。 | 77 |

### 1.2 交互与通信指令

用于文本输出、通道输入输出、向主机触发中断或等待外部触发。

| 指令 | 中文说明 | PDF 页 |
| --- | --- | --- |
| `DISP` | 生成 ASCII 输出字符串，并发送到 DISPCH 指定的默认通信通道。 | 80 |
| `INP` | 从指定通信通道读取数据到整型数组，常用于特殊输入设备或 Modbus 主站通信。 | 85 |
| `INTERRUPT` | 向主机产生可捕获的中断。 | 87 |
| `INTERRUPTEX` | 产生扩展中断，功能类似 INTERRUPT，但携带/处理信息更丰富。 | 89 |
| `SEND` | 类似 DISP，但可显式指定输出通信通道。 | 90 |
| `TRIGGER` | 设置触发条件；条件满足后控制器向主机发出中断。 | 92 |
| `OUTP` | 将整型数组中的数据发送到指定通信通道。 | 93 |

### 1.3 PEG 与 MARK 指令

用于位置事件发生器 PEG、MARK 输入锁存、PEG 输出映射和 PEG 延时控制。

| 指令 | 中文说明 | PDF 页 |
| --- | --- | --- |
| `ASSIGNMARK` | 将 MARK 输入映射到指定编码器，使不同物理输入可触发编码器锁存。 | 96 |
| `ASSIGNPEG` | 将 PEG 引擎映射到编码器，并分配可作为 PEG 脉冲输出的通用输出。 | 97 |
| `ASSIGNPOUTS` | 将 PEG 引擎输出、快速通用输出等信号映射到物理输出引脚。 | 99 |
| `GETPEGCOUNT` | 返回指定 PEG 引擎的脉冲计数。 | 101 |
| `PEG_I` | 设置增量式 PEG 模式参数，例如脉宽、首点、间隔、末点等。 | 101 |
| `PEG_R` | 设置随机 PEG 模式参数，通过位置数组/状态数组定义 PEG 输出。 | 104 |
| `STARTPEG` | 启动指定 PEG 引擎上的 PEG 过程，常与 PEG_I/PEG_R 的 /w 延迟启动配合使用。 | 108 |
| `STOPPEG` | 立即停止指定 PEG 引擎上的 PEG 过程。 | 108 |
| `SETPEGDELAY` | 设置指定 PEG 引擎的 PEG 脉冲、状态上升沿、状态下降沿延时。 | 109 |

### 1.4 杂项与数据传输指令

用于轴别名、数据采集、Flash 文件读写、Servo Processor 数据交换、SPI 和处理器使用率查询。

| 指令 | 中文说明 | PDF 页 |
| --- | --- | --- |
| `AXISDEF` | 为一个或多个轴定义别名，后续命令中可用别名代替轴号。 | 111 |
| `DC` | 按固定采样率采集标准变量或用户变量数据；可同步或不同步运动。 | 113 |
| `STOPDC` | 立即终止 DC 或 SPDC 数据采集。 | 115 |
| `READ` | 从控制器非易失 Flash 存储中的文件读取数据到用户数组或变量。 | 116 |
| `SPDC` | 以 Servo Processor 快速周期采集指定 SP 变量，典型采样率最高约 20 kHz。 | 117 |
| `STOPSPDC` | 终止指定 Servo Processor 上的 SPDC 数据采集。 | 119 |
| `WRITE` | 将数组或标量变量写入控制器非易失 Flash 存储文件。 | 120 |
| `SPINJECT` | 启动从 MPU 到 Servo Processor 的实时数据注入。 | 121 |
| `STOPINJECT` | 停止指定 Servo Processor 上正在运行的数据注入过程。 | 122 |
| `SPICFG` | 配置并初始化 SPI 接口。 | 122 |
| `SPIWRITE` | 执行一次 SPI 事务，可在从机模式或单主站事务模式下发送/接收 SPI 字。 | 124 |
| `SPRT` | 启动从 MPU 到指定 Servo Processor 的实时数据传输。 | 125 |
| `SPRTSTOP` | 停止指定 SP 上的循环实时数据传输过程。 | 129 |
| `USAGESP` | 读取 EtherCAT 节点对应 Servo Processor 在前一控制周期中的最大使用率。 | 129 |

### 1.5 运动指令

用于点到点、JOG、分段路径、扩展分段、平滑路径、NURBS、主从运动和跟踪运动。

| 指令 | 中文说明 | PDF 页 |
| --- | --- | --- |
| `ARC1 (MSEG)` | 在 MSEG...ENDS 分段运动中添加圆弧段，通过圆心、终点和方向定义圆弧。 | 131 |
| `ARC1 (XSEG)` | 在 XSEG...ENDS 扩展分段运动中添加由圆心、终点和方向定义的圆弧段。 | 132 |
| `ARC1 (BSEG)` | 在 BSEG...ENDS 混合分段运动中添加圆弧段。 | 135 |
| `ARC2 (MSEG)` | 在 MSEG...ENDS 分段运动中添加圆弧段，通过圆心和旋转角定义圆弧。 | 136 |
| `ARC2 (XSEG)` | 在 XSEG...ENDS 扩展分段运动中添加由圆心和旋转角定义的圆弧段。 | 137 |
| `ARC2 (BSEG)` | 在 BSEG...ENDS 混合分段运动中添加由圆心和旋转角定义的圆弧段。 | 140 |
| `BPTP` | 使用 MotionBoost 特性生成点到点运动轮廓，可按最短时间或指定时间运动。 | 140 |
| `BPTPCalc` | 根据期望运动时间和距离计算/设置 VEL、ACC、JERK 等运动变量。 | 143 |
| `BSEG...ENDS` | 定义混合分段运动块，后续 ARC1/ARC2/LINE 等段在该块内组成路径。 | 144 |
| `JOG` | 创建无固定终点的恒速连续运动。 | 145 |
| `LINE (MSEG)` | 在 MSEG...ENDS 中添加直线段。 | 146 |
| `LINE (XSEG)` | 在 XSEG...ENDS 中添加从当前位置到目标点的直线段。 | 147 |
| `LINE (BSEG)` | 在 BSEG...ENDS 混合分段运动中添加直线段。 | 150 |
| `MASTER` | 定义主从运动关系中的主值公式，通常与 SLAVE 配合使用。 | 151 |
| `MPOINT` | 以数组方式向 MPTP、PATH 或 PVSPLINE 运动提供一组目标点。 | 152 |
| `MPTP...ENDS` | 按 POINT/MPOINT 给定的多个点依次执行多点定位运动。 | 157 |
| `MSEG...ENDS` | 启动二维分段运动，路径由后续 LINE/ARC1/ARC2 等段命令定义。 | 160 |
| `PATH...ENDS` | 按 POINT/MPOINT 给定点创建任意路径运动，点间使用线性插值。 | 162 |
| `POINT` | 向 MPTP、PATH 或 PVSPLINE 运动序列添加一个目标点。 | 164 |
| `PROJECTION` | 为 MSEG 扩展三维分段能力，通过变换矩阵在用户定义平面上生成线/圆弧。 | 167 |
| `PTP` | 点到点运动，使一个或多个轴运动到指定目标位置。 | 169 |
| `PVSPLINE...ENDS` | 创建位置-速度样条路径，点间采用三次样条插值。 | 171 |
| `SLAVE` | 启动基于 MASTER 定义的主从锁定运动，可为位置锁定或速度锁定。 | 174 |
| `STOPPER` | 在 MSEG 段间插入分段分隔，使拐点处平滑降速到零再加速，避免速度突变。 | 175 |
| `TRACK` | 启动跟踪运动；当目标位置变量 TPOS 改变时生成新的点到点运动。 | 176 |
| `XSEG...ENDS` | 扩展分段运动块，支持拐角检测、限速、速度规划和多种段选项。 | 177 |
| `NURBS` | 创建 NURBS 曲线运动。 | 187 |
| `NPOINT` | 为 NURBS 运动添加下一个控制点和节点。 | 189 |
| `SPATH` | 启动路径平滑运动。 | 191 |
| `SEGMENT` | 向 SPATH 运动生成器添加新的控制点/运动段。 | 194 |
| `SMOVE` | 执行带过渡点平滑的目标定位，连续 SMOVE 会在方向变化处生成平滑过渡。 | 195 |
| `SPTP` | 多轴点到点运动命令，使用四阶运动轮廓。 | 195 |
| `ARC1/ARC2/LINE 选项` | 说明 ARC1、ARC2、LINE 在不同分段运动中的选项和开关，不是独立运动命令。 | 196 |

### 1.6 程序流程指令

用于变量赋值、条件判断、循环、等待、跳转、子程序调用和自动例程。

| 指令 | 中文说明 | PDF 页 |
| --- | --- | --- |
| `Assignment Command` | 赋值语句，将表达式结果写入变量、数组元素或结构成员。 | 198 |
| `BLOCK...END` | 将一组 ACSPL+ 命令合并为一个控制器周期内执行的块。 | 200 |
| `CALL` | 调用子程序。 | 201 |
| `GOTO` | 将程序执行流程跳转到指定标签或位置。 | 201 |
| `IF, ELSEIF, ELSE...END` | 条件分支结构。 | 202 |
| `INPUT` | 暂停程序执行并等待用户输入。 | 204 |
| `LOOP...END` | 循环结构。 | 205 |
| `ON...RET` | 自动例程结构；当指定条件满足时中断当前程序，执行例程后返回。 | 206 |
| `TILL` | 等待指定表达式为非零/真后继续执行。 | 207 |
| `WAIT` | 按指定毫秒数延时。 | 208 |
| `WHILE...END` | while 循环结构。 | 208 |
| `SWITCH Statement` | switch 多分支选择结构。 | 209 |

### 1.7 程序管理指令

用于控制 ACSPL+ 缓冲区程序的启动、停止、暂停、恢复以及自动例程启停。

| 指令 | 中文说明 | PDF 页 |
| --- | --- | --- |
| `DISABLEON` | 禁止缓冲区中的自动例程触发。 | 210 |
| `ENABLEON` | 允许缓冲区中的自动例程触发。 | 211 |
| `PAUSE` | 暂停指定缓冲区中的程序执行。 | 211 |
| `RESUME` | 恢复指定缓冲区中已暂停的程序。 | 211 |
| `START` | 启动指定缓冲区中的程序执行。 | 212 |
| `STOP/STOPALL` | 停止指定缓冲区或全部缓冲区的程序执行。 | 213 |

### 1.8 EtherNet/IP 支持指令

用于 EtherNet/IP Assembly 映射、属性读取和变量索引/标签查询。

| 指令 | 中文说明 | PDF 页 |
| --- | --- | --- |
| `EIPGETATTR` | 返回指定 EtherNet/IP 类属性或实例属性的值。 | 214 |
| `EIPGETIND1` | 返回一维数组变量在 Assembly 中的第一索引。 | 215 |
| `EIPGETIND2` | 返回二维数组变量在 Assembly 中的第一索引。 | 216 |
| `EIPGETTAG` | 返回指定标准变量或用户友好变量的标签号。 | 217 |
| `EIPSETASM` | 设置 EtherNet/IP Assembly 配置。 | 217 |

### 1.9 激光控制指令

用于使能或停止激光脉冲发生过程。

| 指令 | 中文说明 | PDF 页 |
| --- | --- | --- |
| `LCENABLE` | 按当前参数使能脉冲发生过程；频率或占空比为零时等待有效值后输出。 | 218 |
| `LCDISABLE` | 停止脉冲发生过程，包括 tickle 脉冲。 | 218 |

### 1.10 输入整形指令

用于启停指定轴的 Input Shaping 算法。

| 指令 | 中文说明 | PDF 页 |
| --- | --- | --- |
| `INSHAPEON` | 启动指定轴的输入整形算法，输出为输入信号与整形脉冲的卷积结果。 | 219 |
| `INSHAPEOFF` | 停止指定轴的输入整形算法。 | 220 |

## 2. HOME 预定义回零方法

以下条目不是独立指令，而是 `HOME` 指令的 HomingMethod 参数常用预定义值。

| 方法号 | 中文说明 | PDF 页 |
| --- | --- | --- |
| `1` | 负限位开关 + index 脉冲回零 | 78 |
| `2` | 正限位开关 + index 脉冲回零 | 78 |
| `17` | 负限位开关回零 | 78 |
| `18` | 正限位开关回零 | 78 |
| `33/34` | index 脉冲回零 | 78 |
| `37` | 以当前位置作为零点 | 79 |
| `50` | 负向硬限位 + index 脉冲回零，ACS 专用 | 79 |
| `51` | 正向硬限位 + index 脉冲回零，ACS 专用 | 79 |
| `52` | 负向硬限位回零，ACS 专用 | 79 |
| `53` | 正向硬限位回零，ACS 专用 | 79 |

## 3. Communication Terminal 指令

Terminal 指令只适用于 SPiiPlus MMI Application Studio 的 Communication Terminal，不属于 ACSPL+ 程序语言，不能直接写入 ACSPL+ 程序缓冲区作为程序语句。Terminal 指令区分大小写。

### 3.1 查询类 Terminal 指令

| 指令/语法 | 中文说明 | PDF 页 |
| --- | --- | --- |
| `?[ACSPL+ variable]` | 查询标准变量当前值。 | 882 |
| `?[ACSPL+ variable ACSPL+ variable]...` | 一次查询多个标准变量当前值。 | 882 |
| `?[buffer]:[local variable]` | 查询指定程序缓冲区中的局部用户变量或数组。 | 882 |
| `?[array_variable(index)]` | 查询数组变量指定元素；索引括号可按手册规则省略。 | 882 |
| `?[global array_variable[(index)]]` | 查询全局用户变量/数组，带索引时查询单个元素。 | 882 |
| `?[matrix_variable(row)(column)]` | 查询二维矩阵变量的指定元素。 | 882 |
| `?#` | 查询全部程序缓冲区状态。 | 883 |
| `?$[axis number]` | 查询指定轴电机状态。 | 883 |
| `?$` | 查询全部电机状态。 | 883 |
| `?VR` | 查询固件版本。 | 883 |
| `?SN` | 查询控制器序列号和硬件版本。 | 883 |
| `??[error_code]` | 查询错误码编号；带错误码时返回对应错误描述。 | 883 |
| `??[variable_name]` | 查询变量简要描述。 | 883 |
| `?D/` | 以十进制格式输出查询结果。 | 884 |
| `?X/` | 以十六进制格式输出整型变量。 | 884 |
| `?B/` | 以二进制格式输出整型变量。 | 884 |
| `?E/` | 以扩展宽度格式输出实数，适合很大或很小的数值。 | 884 |
| `?{C format}variable` | 使用 C 风格格式字符串自定义输出格式。 | 885 |

### 3.2 程序缓冲区与变量管理 Terminal 指令

| 指令/语法 | 中文说明 | PDF 页 |
| --- | --- | --- |
| `#buffer_number[I][line_number]` | 打开指定程序缓冲区，或使用单独 # 关闭当前缓冲区；I 表示插入模式。 | 887 |
| `#buffer_numberDline_number[,line_number]` | 删除缓冲区中的指定行或行范围。 | 888 |
| `#buffer_number{F|FI}/search_string[,line_number]` | 查找指定文本；FI 为区分大小写查找。 | 889 |
| `#buffer_numberL[line_number]` | 列出缓冲区全部内容或指定行/行范围。 | 890 |
| `#RESET` | 将控制器复位到出厂默认状态。 | 892 |
| `#VGR [group_name]` | 列出 ACSPL+ 变量分组。 | 892 |
| `#VSD [group_name]` | 列出 ACSPL+ 变量及简短说明。 | 893 |
| `#VS / #VSG [group_name]` | 列出 ACSPL+ 标准变量名，可按分组筛选。 | 893 |
| `#VSF / #VSGF [group_name]` | 列出标准变量名及更完整的变量信息，可按分组筛选。 | 894 |
| `#[buffer_no]VG / #[buffer_no]VGF [variable_name]` | 列出全局变量名；VGF 还显示类型、元素数量等信息。 | 895 |
| `#[buffer_no]VL / #[buffer_no]VLF [variable_name]` | 列出局部变量名；VLF 还显示类型、元素数量等信息。 | 895 |
| `#[buffer_number]V / #[buffer_number]VF` | 列出指定缓冲区可见的用户变量；VF 输出更完整信息。 | 896 |
| `#VSPservo_number` | 列出指定 Servo Processor 可用的 SP 变量。 | 897 |
| `#VST / #VSGT [group_name]` | 列出结构类型相关变量/分组信息。 | 897 |
| `#VSTF / #VSGTF [group_name] / #VSDT [group_name]` | 列出结构类型变量的名称、类型、元素数量等详细信息。 | 898 |
| `#VGV [global_var]` | 删除通过通信终端设置的全局变量。 | 898 |
| `#VGS / #VGSF [Static_Var]` | 列出当前定义的 STATIC 变量；VGSF 输出更完整信息。 | 898 |

### 3.3 程序运行与调试 Terminal 指令

| 指令/语法 | 中文说明 | PDF 页 |
| --- | --- | --- |
| `#buffer_numberC` | 编译指定缓冲区程序；##C 可编译全部缓冲区。 | 900 |
| `#buffer_numberX[line_number]` | 执行指定缓冲区程序，可指定起始行。 | 901 |
| `#buffer_numberS / #buffer_numberSR` | 停止程序；SR 表示停止并复位/反编译到未编译状态。 | 902 |
| `#buffer_numberP` | 暂停指定缓冲区程序；##P 可暂停全部缓冲区。 | 902 |
| `#buffer_numberXSline_number` | 单步执行指定行。 | 903 |
| `#buffer_numberXD` | 以调试模式执行到下一个断点。 | 904 |
| `#buffer_numberBSline_number` | 在指定行设置断点。 | 904 |
| `#buffer_numberBR[line_number]` | 清除指定行断点或清除全部断点。 | 905 |

### 3.4 系统类 Terminal 指令

| 指令/语法 | 中文说明 | PDF 页 |
| --- | --- | --- |
| `#SI` | 输出控制器系统信息，包括序列号、固件版本、配置、名称和 SP 程序等。 | 906 |
| `#SIR/Section|ALL/Key|ALL/` | 输出系统信息报告，可按 Section 和 Key 查询。 | 910 |
| `#MEMORY` | 查询 RAM/缓冲区/变量内存可用情况。 | 922 |
| `#IR` | 执行完整性检查并输出文件大小、校验和等完整性报告。 | 922 |
| `#U` | 监控 MPU 使用率，返回最大、平均、最小占用百分比。 | 924 |
| `#TD` | 列出控制器 Flash 存储中的用户自定义变量和数组名称。 | 925 |
| `#SC` | 输出安全系统配置，包括安全组和各电机故障响应配置。 | 925 |
| `#ETHERCAT` | 输出 EtherCAT 从站及网络变量信息。 | 927 |
| `#ETHERCAT2` | 输出第二套/增强格式 EtherCAT 网络信息。 | 936 |
| `#ECMAPREP [/0 /1]` | 显示通过 ECIN、ECOUT、ECEXTIN、ECEXTOUT 映射的变量报告。 | 937 |
| `#CC` | 输出当前通信通道信息。 | 938 |
| `#PLC` | 输出 SPiiPlus PLC 共存与运行信息；仅在系统运行 SPiiPlus PLC 时有效。 | 939 |
| `#LOG` | 显示控制器保存的重要事件日志，默认最多保留最近 500 条事件。 | 940 |
| `#LOG HOST_TICKS` | 设置日志时间偏移，使日志时间与主机毫秒计数对齐。 | 941 |
| `#LOGP buffer_number` | 显示 START/s 仿真运行时检测到的 G-code 运行时错误。 | 942 |

## 4. 使用注意事项

- 运动类命令执行前通常需要确认驱动已 `ENABLE`、故障已 `FCLEAR`、必要轴已完成 `HOME`，并确认限位、急停、安全组配置符合设备状态。
- `HALT`、`KILL`、`BREAK`、`DISABLE` 的停止行为不同：涉及设备安全时应按减速模型、驱动状态和队列行为选择。
- `MSEG`、`XSEG`、`BSEG`、`PATH`、`PVSPLINE`、`NURBS`、`SPATH` 等块式运动必须按手册要求用对应段命令补充路径，并以 `ENDS` 结束。
- PEG、MARK、EtherCAT、EtherNet/IP、SPI、Servo Processor 相关指令与具体控制器型号、固件版本和硬件映射强相关，使用前应核对原厂手册页码和设备配置。
- 原文档总表中出现的 `ECRESCAN` 在 PDF 目录中位于 EtherCAT Functions 章节（第 651 页），本文未展开函数章节。
