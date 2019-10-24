using FluentArgs;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace BenchApp
{
    static class Program
    {
        static void Main(string[] args)
        {
            FluentArgsBuilder
                .NewWithDefaultConfigs()
                .WithApplicationDescription("Benchmark of sqlite3 bulk inserts with different pragmas.")
                .Parameter<int>("-i", "--batchSizeIncrement")
                    .WithDescription("Growth of 'batchSizeBase' parameter in each test set run.")
                    .WithValidation(i => i >= 1)
                    .IsOptionalWithDefault(5_000)
                .Parameter<int>("-b", "--batchSizeBase")
                    .WithDescription("Minimal number of inserts in a transaction, will be incremented by 'batchSizeIncrement' in each test set run.")
                    .WithValidation(b => b >= 1)
                    .IsOptionalWithDefault(5_000)
                .Parameter<int>("-t", "--tableCount")
                    .WithDescription("Number of tables to use for inserts.")
                    .WithValidation(t => t >= 1)
                    .IsOptionalWithDefault(10)
                .Parameter<string>("-s", "--string")
                    .WithDescription("Value to insert (along with sequential integer primary key).")
                    .IsOptionalWithDefault("string")
                .Parameter<int>("-r", "--rowCount")
                    .WithDescription("Total number of rows to insert in each test.")
                    .WithValidation(r => r >= 100)
                    .IsOptionalWithDefault(5_000_000)
                .Parameter<int>("-n", "--number")
                    .WithDescription("Number of test set runs.")
                    .WithValidation(n => n >= 1)
                    .IsOptionalWithDefault(40)
                .Call(n => totalRows => value => tblCount => batchSizeBase => batchSizeIncrement =>
                {
                    Console.WriteLine("batchSize;default;sync-off;jm-wal;jm-off;lm-excl;sync-off&jm-off&lm-excl");
                    for (int i = 0; i < n; i++)
                    {
                        var batchSize = batchSizeBase + batchSizeIncrement * i;
                        var commits = Math.Max(1, totalRows / batchSize);

                        Console.WriteLine(new StringBuilder().AppendJoin(";",
                            batchSize,
                            RunTestSet("default", i, Array.Empty<string>(), tblCount, commits, batchSize, value),
                            RunTestSet("sync-off", i, new[] { "synchronous = OFF" }, tblCount, commits, batchSize, value),
                            RunTestSet("jm-wal", i, new[] { "journal_mode = WAL" }, tblCount, commits, batchSize, value),
                            RunTestSet("jm-off", i, new[] { "journal_mode = OFF" }, tblCount, commits, batchSize, value),
                            RunTestSet("lm-excl", i, new[] { "locking_mode = EXCLUSIVE" }, tblCount, commits, batchSize, value),
                            RunTestSet("sync-off&jm-off&lm-excl", i, new[] {
                                "synchronous = OFF",
                                "journal_mode = OFF",
                                "locking_mode = EXCLUSIVE" }, tblCount, commits, batchSize, value)
                            ));
                    }
                })
                .Parse(args);
        }

        private static double RunTestSet(
            string shortName,
            int runIndex,
            IEnumerable<string> pragmas,
            int tblCount,
            int commits,
            int batchSize,
            object value)
        {
            using var connection = CreateConnection(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                $"{shortName}_{runIndex}.sqlite"),
                tblCount,
                pragmas);
            long lastRev = 0L;
            var commands = PrepareCommands(tblCount, connection, value);
            var seqResult = RunTest(commits, () =>
            {
                InsertInTransaction(connection, commands, batchSize, tblCount, ref lastRev);
            });
            return lastRev / seqResult.TotalSeconds;
        }

        private static TimeSpan RunTest(int repeatCount, Action test)
        {
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < repeatCount; i++)
            {
                test();
            }
            sw.Stop();
            return sw.Elapsed;
        }

        static SqliteConnection CreateConnection(string dataSource, int tblCount, IEnumerable<string> pragmas)
        {
            var connection = new SqliteConnection($"DataSource={dataSource}");
            connection.Open();

            for (int i = 0; i < tblCount; i++)
            {
                using var ddlCommand = new SqliteCommand(
                    $"CREATE TABLE tbl{i} (rev INTEGER PRIMARY KEY, value BLOB);",
                    connection);

                ddlCommand.ExecuteNonQuery();
            }

            foreach (var pragma in pragmas)
            {
                using var cmd = new SqliteCommand($"PRAGMA {pragma};", connection);
                cmd.ExecuteNonQuery();
            }

            return connection;
        }

        static void InsertInTransaction(
            SqliteConnection connection,
            IReadOnlyList<SqliteCommand> commandCache,
            int batchSize,
            int tblCount,
            ref long rev)
        {
            var rowCount = batchSize / tblCount;
            using var transaction = connection.BeginTransaction();
            for (int t = 0; t < tblCount; t++)
            {
                var command = commandCache[t];
                command.Transaction = transaction;
                for (int r = 0; r < rowCount; r++)
                {
                    command.Parameters[0].Value = rev++;
                    command.ExecuteNonQuery();
                }
            }
            transaction.Commit();
        }

        static IReadOnlyList<SqliteCommand> PrepareCommands(int tblCount, SqliteConnection connection, object value)
        {
            var result = new SqliteCommand[tblCount];
            for (int i = 0; i < tblCount; i++)
            {
                using var insertCommand = new SqliteCommand(
                    $"INSERT INTO [tbl{i}](rev,value) VALUES (@rev,@value);",
                    connection);

                insertCommand.Parameters.Add("@rev", SqliteType.Integer);
                insertCommand.Parameters.AddWithValue("@value", value);

                result[i] = insertCommand;
            }
            return result;
        }
    }
}
