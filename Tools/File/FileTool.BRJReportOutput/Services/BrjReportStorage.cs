using FileTool.BRJReportOutput.Models;
using Microsoft.Data.Sqlite;
using ReeYin_V.Core.IOC;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FileTool.BRJReportOutput.Services
{
    public static class BrjReportStorage
    {
        private const string DatabaseName = "DB_BRJReport.db";
        private const string DatabasePassword = "ruiqi.12345";
        private static readonly object SyncRoot = new();
        private static readonly SemaphoreSlim WriteLock = new(1, 1);
        private static bool _isCreated;

        public static string DatabasePath => Path.Combine(PrismProvider.AppBasePath, "Config", DatabaseName);

        public static void EnsureCreated()
        {
            lock (SyncRoot)
            {
                if (_isCreated)
                {
                    return;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);
                SQLitePCL.Batteries_V2.Init();

                using SqlSugarClient db = CreateClient();
                db.DbMaintenance.CreateDatabase();
                db.CodeFirst.SetStringDefaultLength(200).InitTables(typeof(BrjReportRecord), typeof(BrjDefectRecord), typeof(BrjReportSetting));
                EnsureDefectCameraNameColumn(db);
                TryExecuteMaintenanceCommand(db, "CREATE UNIQUE INDEX IF NOT EXISTS idx_brj_report_record_sn ON brj_report_record (SN);");
                TryExecuteMaintenanceCommand(db, "CREATE INDEX IF NOT EXISTS idx_brj_defect_record_sn ON brj_defect_record (SN);");
                MigrateDiameterGroupSettings(db);
                SeedDiameterGroupSettings(db);
                _isCreated = true;
            }
        }

        public static async Task<int> InsertRecordAsync(BrjReportRecord record)
        {
            EnsureCreated();
            await WriteLock.WaitAsync().ConfigureAwait(false);
            try
            {
                using SqlSugarClient db = CreateClient();
                return await db.Insertable(record).ExecuteCommandAsync().ConfigureAwait(false);
            }
            finally
            {
                WriteLock.Release();
            }
        }

        public static async Task SaveRollAsync(BrjReportRecord record, IEnumerable<BrjDefectRecord> defects)
        {
            EnsureCreated();
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            record.SN = record.SN?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(record.SN))
            {
                throw new ArgumentException("BRJ report SN cannot be empty.", nameof(record));
            }

            List<BrjDefectRecord> defectList = (defects ?? Enumerable.Empty<BrjDefectRecord>())
                .Where(item => item != null)
                .ToList();

            await WriteLock.WaitAsync().ConfigureAwait(false);
            try
            {
                using SqlSugarClient db = CreateClient();
                db.Ado.BeginTran();
                try
                {
                    BrjReportRecord? existing = (await db.Queryable<BrjReportRecord>()
                            .Where(item => item.SN == record.SN)
                            .ToListAsync()
                            .ConfigureAwait(false))
                        .FirstOrDefault();

                    if (existing == null)
                    {
                        await db.Insertable(record).ExecuteCommandAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        record.Id = existing.Id;
                        record.CreateTime = existing.CreateTime == default ? record.CreateTime : existing.CreateTime;
                        record.EndTime ??= existing.EndTime;

                        await db.Updateable(record)
                            .Where(item => item.Id == record.Id)
                            .ExecuteCommandAsync()
                            .ConfigureAwait(false);

                        await db.Deleteable<BrjDefectRecord>()
                            .Where(item => item.SN == record.SN)
                            .ExecuteCommandAsync()
                            .ConfigureAwait(false);
                    }

                    if (defectList.Count > 0)
                    {
                        foreach (BrjDefectRecord defect in defectList)
                        {
                            defect.Id = 0;
                            defect.SN = record.SN;
                            if (defect.CreateTime == default)
                            {
                                defect.CreateTime = DateTime.Now;
                            }
                        }

                        await db.Insertable(defectList).ExecuteCommandAsync().ConfigureAwait(false);
                    }

                    db.Ado.CommitTran();
                }
                catch
                {
                    db.Ado.RollbackTran();
                    throw;
                }
            }
            finally
            {
                WriteLock.Release();
            }
        }

        public static async Task<List<BrjReportRecord>> QueryRecordsAsync(DateTime start, DateTime end)
        {
            EnsureCreated();
            using SqlSugarClient db = CreateClient();
            return await db.Queryable<BrjReportRecord>()
                .Where(item => item.CreateTime >= start && item.CreateTime <= end)
                .OrderBy(item => item.CreateTime, OrderByType.Desc)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        public static async Task<List<BrjReportRecord>> QueryRecordsAsync()
        {
            EnsureCreated();
            using SqlSugarClient db = CreateClient();
            return await db.Queryable<BrjReportRecord>()
                .OrderBy(item => item.CreateTime, OrderByType.Desc)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        public static async Task<int> CountRecordsAsync()
        {
            EnsureCreated();
            using SqlSugarClient db = CreateClient();
            return await db.Queryable<BrjReportRecord>()
                .CountAsync()
                .ConfigureAwait(false);
        }

        public static async Task<int> CountRecordsAsync(DateTime start, DateTime end)
        {
            EnsureCreated();
            using SqlSugarClient db = CreateClient();
            return await db.Queryable<BrjReportRecord>()
                .Where(item => item.CreateTime >= start && item.CreateTime <= end)
                .CountAsync()
                .ConfigureAwait(false);
        }

        public static async Task<int> CountRecordsBySNAsync(string sn)
        {
            EnsureCreated();
            using SqlSugarClient db = CreateClient();
            return await db.Queryable<BrjReportRecord>()
                .Where(item => item.SN.Contains(sn))
                .CountAsync()
                .ConfigureAwait(false);
        }

        public static async Task<BrjReportRecord?> QueryRecordAsync(string sn)
        {
            EnsureCreated();
            using SqlSugarClient db = CreateClient();
            return (await db.Queryable<BrjReportRecord>()
                    .Where(item => item.SN == sn)
                    .ToListAsync()
                    .ConfigureAwait(false))
                .FirstOrDefault();
        }

        public static async Task<List<BrjReportRecord>> QueryRecordsAsync(int pageIndex, int pageSize)
        {
            EnsureCreated();
            using SqlSugarClient db = CreateClient();
            return await db.Queryable<BrjReportRecord>()
                .OrderBy(item => item.CreateTime, OrderByType.Desc)
                .ToPageListAsync(pageIndex, pageSize)
                .ConfigureAwait(false);
        }

        public static async Task<List<BrjReportRecord>> QueryRecordsAsync(DateTime start, DateTime end, int pageIndex, int pageSize)
        {
            EnsureCreated();
            using SqlSugarClient db = CreateClient();
            return await db.Queryable<BrjReportRecord>()
                .Where(item => item.CreateTime >= start && item.CreateTime <= end)
                .OrderBy(item => item.CreateTime, OrderByType.Desc)
                .ToPageListAsync(pageIndex, pageSize)
                .ConfigureAwait(false);
        }

        public static async Task<List<BrjReportRecord>> QueryRecordsBySNAsync(string sn, int pageIndex, int pageSize)
        {
            EnsureCreated();
            using SqlSugarClient db = CreateClient();
            return await db.Queryable<BrjReportRecord>()
                .Where(item => item.SN.Contains(sn))
                .OrderBy(item => item.CreateTime, OrderByType.Desc)
                .ToPageListAsync(pageIndex, pageSize)
                .ConfigureAwait(false);
        }

        public static async Task<List<BrjDefectRecord>> QueryDefectsAsync(string sn)
        {
            return await Task.Run(() =>
            {
                EnsureCreated();
                List<BrjDefectRecord> records = new();
                using SqliteConnection connection = new(CreateConnectionString());
                connection.Open();
                using SqliteCommand command = connection.CreateCommand();
                command.CommandText = @"
SELECT [序号], [SN], [缺陷索引], [相机索引], [相机名称], [分段号], [分切号], [类型], [面积/mm^2], [直径/mm], [横位置/mm], [纵位置/m], [缺陷图路径], [创建时间]
FROM brj_defect_record
WHERE [SN] = $sn
ORDER BY [序号];";
                command.Parameters.AddWithValue("$sn", sn);

                using SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    records.Add(ReadDefectRecord(reader));
                }

                return records;
            }).ConfigureAwait(false);
        }

        public static async Task<List<BrjReportSetting>> QueryDiameterGroupSettingsAsync()
        {
            EnsureCreated();
            using SqlSugarClient db = CreateClient();
            return await db.Queryable<BrjReportSetting>()
                .Where(item => item.SettingType == BrjReportSetting.DiameterGroupType)
                .OrderBy(item => item.SortIndex)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        public static async Task SaveDiameterGroupSettingsAsync(IEnumerable<BrjReportSetting> settings)
        {
            EnsureCreated();
            List<BrjReportSetting> list = (settings ?? Enumerable.Empty<BrjReportSetting>())
                .OrderBy(item => item.SortIndex)
                .Select((item, index) => new BrjReportSetting
                {
                    SettingType = BrjReportSetting.DiameterGroupType,
                    SortIndex = index + 1,
                    GroupName = item.GroupName?.Trim() ?? string.Empty,
                    MinDiameterMm = item.MinDiameterMm,
                    MaxDiameterMm = item.MaxDiameterMm,
                    ColorHex = string.IsNullOrWhiteSpace(item.ColorHex) ? "#3858D6" : item.ColorHex.Trim(),
                })
                .ToList();

            await WriteLock.WaitAsync().ConfigureAwait(false);
            try
            {
                using SqlSugarClient db = CreateClient();
                db.Ado.BeginTran();
                try
                {
                    await db.Deleteable<BrjReportSetting>()
                        .Where(item => item.SettingType == BrjReportSetting.DiameterGroupType)
                        .ExecuteCommandAsync()
                        .ConfigureAwait(false);
                    if (list.Count > 0)
                    {
                        await db.Insertable(list).ExecuteCommandAsync().ConfigureAwait(false);
                    }

                    db.Ado.CommitTran();
                }
                catch
                {
                    db.Ado.RollbackTran();
                    throw;
                }
            }
            finally
            {
                WriteLock.Release();
            }
        }

        public static async Task<string> QueryReportOutputDirectoryAsync()
        {
            EnsureCreated();
            using SqlSugarClient db = CreateClient();
            BrjReportSetting? setting = (await db.Queryable<BrjReportSetting>()
                    .Where(item => item.SettingType == BrjReportSetting.ReportOutputType
                        && item.SettingKey == BrjReportSetting.ReportOutputDirectoryKey)
                    .ToListAsync()
                    .ConfigureAwait(false))
                .FirstOrDefault();

            return setting?.SettingValue?.Trim() ?? string.Empty;
        }

        public static async Task SaveReportOutputDirectoryAsync(string directory)
        {
            EnsureCreated();
            string value = directory?.Trim() ?? string.Empty;

            await WriteLock.WaitAsync().ConfigureAwait(false);
            try
            {
                using SqlSugarClient db = CreateClient();
                db.Ado.BeginTran();
                try
                {
                    await db.Deleteable<BrjReportSetting>()
                        .Where(item => item.SettingType == BrjReportSetting.ReportOutputType
                            && item.SettingKey == BrjReportSetting.ReportOutputDirectoryKey)
                        .ExecuteCommandAsync()
                        .ConfigureAwait(false);

                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        await db.Insertable(new BrjReportSetting
                        {
                            SettingType = BrjReportSetting.ReportOutputType,
                            SettingKey = BrjReportSetting.ReportOutputDirectoryKey,
                            SettingValue = value,
                        }).ExecuteCommandAsync().ConfigureAwait(false);
                    }

                    db.Ado.CommitTran();
                }
                catch
                {
                    db.Ado.RollbackTran();
                    throw;
                }
            }
            finally
            {
                WriteLock.Release();
            }
        }

        private static void MigrateDiameterGroupSettings(SqlSugarClient db)
        {
            int count = db.Queryable<BrjReportSetting>()
                .Where(item => item.SettingType == BrjReportSetting.DiameterGroupType)
                .Count();
            if (count > 0 || !IsTableExists(db, "brj_diameter_group_setting"))
            {
                return;
            }

            db.Ado.ExecuteCommand(@"
INSERT INTO brj_report_setting ([配置类型], [配置键], [配置值], [排序], [分组名称], [最小直径mm], [最大直径mm], [颜色])
SELECT 'DiameterGroup', '', '', [排序], [分组名称], [最小直径mm], [最大直径mm], [颜色]
FROM brj_diameter_group_setting;");
        }

        private static bool IsTableExists(SqlSugarClient db, string tableName)
        {
            return db.Ado.GetDataTable(
                "SELECT name FROM sqlite_master WHERE type = 'table' AND name = @tableName;",
                new SugarParameter("@tableName", tableName)).Rows.Count > 0;
        }

        private static void EnsureDefectCameraNameColumn(SqlSugarClient db)
        {
            if (!IsTableExists(db, "brj_defect_record"))
            {
                return;
            }

            System.Data.DataTable columns = db.Ado.GetDataTable("PRAGMA table_info('brj_defect_record');");
            bool hasColumn = columns.Rows
                .Cast<System.Data.DataRow>()
                .Any(row => string.Equals(Convert.ToString(row["name"]), "相机名称", StringComparison.Ordinal));
            if (!hasColumn)
            {
                db.Ado.ExecuteCommand("ALTER TABLE brj_defect_record ADD COLUMN [相机名称] TEXT;");
            }
        }

        private static void SeedDiameterGroupSettings(SqlSugarClient db)
        {
            int count = db.Queryable<BrjReportSetting>()
                .Where(item => item.SettingType == BrjReportSetting.DiameterGroupType)
                .Count();
            if (count == 0)
            {
                db.Insertable(BrjReportSetting.CreateDefaultDiameterGroups()).ExecuteCommand();
            }
        }

        private static void TryExecuteMaintenanceCommand(SqlSugarClient db, string sql)
        {
            try
            {
                db.Ado.ExecuteCommand(sql);
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 11)
            {
                // 旧库损坏时跳过索引维护，实际查询失败由页面状态提示。
            }
        }

        private static SqlSugarClient CreateClient()
        {
            return new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = CreateConnectionString(),
                DbType = DbType.Sqlite,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute,
            });
        }

        private static string CreateConnectionString()
        {
            return new SqliteConnectionStringBuilder
            {
                DataSource = DatabasePath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Password = DatabasePassword,
                DefaultTimeout = 30,
            }.ToString();
        }

        private static BrjDefectRecord ReadDefectRecord(SqliteDataReader reader)
        {
            return new BrjDefectRecord
            {
                Id = ReadInt32(reader, 0),
                SN = ReadString(reader, 1),
                DefectIndex = ReadInt32(reader, 2),
                CameraIndex = ReadInt32(reader, 3),
                CameraName = ReadString(reader, 4),
                SegmentIndex = ReadInt32(reader, 5),
                SlitIndex = ReadInt32(reader, 6),
                DefectType = ReadString(reader, 7),
                AreaMm2 = ReadDouble(reader, 8),
                DiameterMm = ReadDouble(reader, 9),
                PositionXMm = ReadDouble(reader, 10),
                PositionYM = ReadDouble(reader, 11),
                DefectImagePath = ReadString(reader, 12),
                CreateTime = ReadDateTime(reader, 13),
            };
        }

        private static string ReadString(SqliteDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? string.Empty : Convert.ToString(reader.GetValue(index)) ?? string.Empty;
        }

        private static int ReadInt32(SqliteDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? 0 : Convert.ToInt32(reader.GetValue(index), CultureInfo.InvariantCulture);
        }

        private static double ReadDouble(SqliteDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? 0d : Convert.ToDouble(reader.GetValue(index), CultureInfo.InvariantCulture);
        }

        private static DateTime ReadDateTime(SqliteDataReader reader, int index)
        {
            return DateTime.TryParse(ReadString(reader, index), CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime value)
                ? value
                : DateTime.Now;
        }
    }
}
