# Wafer Point Temperature CSV Design

Date: 2026-06-24

## Goal

When Custom.WaferFlatnessMeasure runs point collection, record all configured PLC temperature addresses for each measured coordinate point into one CSV file.

## Confirmed Mapping

Every coordinate point reads these eight float PLC addresses:

| Column | Address |
| --- | --- |
| 1 | D160 |
| 2 | D162 |
| 3 | D164 |
| 4 | D166 |
| 5 | D170 |
| 6 | D172 |
| 7 | D174 |
| 8 | D176 |

## Design

- Use the same PLC read pattern as HardWareTool.PLC: create PLCParaInfoModel with ParaType Float and call PLCBase.ReadPLCPara.
- Resolve the first configured PLC from ConfigKey.PLCConfig, matching HardWareTool.PLC defaults.
- For each point row in PreDatas, read D160/D162/D164/D166/D170/D172/D174/D176 and write the values into that point's CSV row.
- CSV columns: PointIndex, X, Y, D160, D162, D164, D166, D170, D172, D174, D176.
- Read failures are non-fatal: the point row is preserved and failed temperature cells are left empty.
- Expose the generated path through LastPointTemperatureCsvPath.

## Verification

- Unit-style console tests cover eight address reads per point and CSV shape.
- Build Custom.WaferFlatnessMeasure after implementation.
