using EntityCore.Common;
using EntityCore.Common.Cache;
using EntityCore.Common.Cosmos;
using Microsoft.Cosmos.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using VcClient;
using System.Runtime.Caching;
using ScopeResult = System.Collections.Generic.List<System.Collections.Generic.List<object>>;


namespace SatoriBrowser.Common.Utilities
{
    public class IScope
    {
        private static MemoryCache<Script, ScopeResult> cache =
            new MemoryCache<Script, ScopeResult>(ExecuteScript, TimeSpan.FromMinutes(30), false, false,
                _ => "iScope-" + _.script
            );

        private static MemoryDependencyCache<Script, ScopeResult> cache_ =
            new MemoryDependencyCache<Script, ScopeResult>(ExecuteScript, TimeSpan.FromDays(1),
                _ => "iScope-" + _.script
            );

        private static MemoryCache<Script, ScopeResult> versionedGraphResultsCache =
            new MemoryCache<Script, ScopeResult>(ExecuteScript, TimeSpan.FromDays(1), false, false,
                _ => "iScope-" + _.script
            );

        public class Script
        {
            public string script { get; private set; }

            public Script(string scriptTemplate, params object[] parameters)
            {
                script = string.Format(scriptTemplate, parameters.Select(p => Quote(p)).ToArray());

                if (!script.EndsWith("OUTPUT TO CONSOLE;"))
                    script += "OUTPUT TO CONSOLE;";
            }

            private static object Quote(object value)
            {
                return value is string
                    ? (value as string).Replace("\"", "\\\"")
                    : value;
            }
        }

        public static ScopeResult ExecuteScript(Script script, bool ignoreCache = false)
        {
            return cache.Get(script, ignoreCache);
        }


        private class CosmosStreamMonitor : IDependencyMonitor
        {
            private string path;
            private DateTime timestamp;

            public CosmosStreamMonitor(string path)
            {
                this.path = path;
            }

            public bool Changed(DateTime since, DateTime now)
            {
                if (timestamp > since)
                    return true;

                timestamp = VC.GetStreamInfo("https://cosmos08.osdinfra.net/cosmos/Knowledge/" + path, false).PublishedUpdateTime;

                return timestamp > since;
            }
        }


        private static ConcurrentDictionary<string, IDependencyMonitor> monitors = new ConcurrentDictionary<string, IDependencyMonitor>();

        private static IDependencyMonitor GetDependencyMonitor(string path)
        {
            return monitors.GetOrAdd(path, (path_) => new ThrottledMonitor(new CosmosStreamMonitor(path_), TimeSpan.FromMinutes(1)));
        }


        public static ScopeResult ExecuteScript(Script script, string sourceStreamPath, bool isVersionedStream = false)
        {
            if (isVersionedStream)
                return versionedGraphResultsCache.Get(script);
            return cache_.Get(script, GetDependencyMonitor(sourceStreamPath));
        }


        private static ScopeResult ExecuteScript(Script script)
        {
            IDataReader reader = Control.RetryTimes(2,
                () => getReader(script.script),
                e => Thread.Sleep(100));

            using (reader)
            {
                var rows = new ScopeResult();
                while (reader.Read())
                {
                    var row = new List<object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                        row.Add(reader[i]);

                    rows.Add(row);
                }
                return rows;
            }
        }


        private static IDataReader getReader(string script)
        {
            Guid? jobId = null;

            try
            {
                var watch = Stopwatch.StartNew();

                var compiler = new Microsoft.Cosmos.Client.ScopeCompiler {
                    Target = CompilerTarget.Interactive
                };
                compiler.Settings.Vc = new CompilerSettings.VcSettings
                {
                    Path = "https://cosmos08.osdinfra.net/cosmos/Knowledge/",
                    Certificate = Cosmos.Cosmos.GetCertificate()
                };
                compiler.Settings.Execution = new CompilerSettings.ExecutionSettings();
                compiler.Settings.Execution.RuntimeVersion = "iscope_default";

                var scopeScript = new ScopeScript {
                    Contents = script,
                    Name = "knowledge.microsoft.com"
                };

                var offset = watch.ElapsedMilliseconds;

                var job = compiler.Execute(scopeScript);
                jobId = job.JobId;

                var result = job.Result.Console;

                /*Ap.Counters.iScopeQueryLatency.Set((ulong)watch.ElapsedMilliseconds);
                Ap.Counters.iScopeMicroTokens.IncrementBy((uint)job.Result.MicroTokenCost);
                Ap.Counters.iScopeQueries.Increment();

                Ap.LogInfo("iScopeMicroTokenCost", string.Format("JobId:{0}, Cost:{1} Latency:{2} Machine:{3} StartTime:{4}", jobId, job.Result.MicroTokenCost, watch.ElapsedMilliseconds - offset, job.Result.ServerMachine, job.Result.StartTime));
                */
                return result;
            }
            catch (Exception ex)
            {
                Exceptions.Handle(ex, e =>
                {
                    //Ap.Counters.iScopeErrors.Increment();

                    var ce = ex as CompilationException;
                    string details = ce != null
                        ? string.Format("JobId {0}, Server {1}, Version: {2};", ce.Results.JobId, ce.Results.ServerMachine, ce.Results.RuntimeVersion)
                        : jobId != null
                            ? string.Format("JobId {0}", jobId.Value)
                            : "no JobId assigned yet";
                    string message = "iScope - " + script + " - " + details;

                    CosmosExceptionLogger.Log(e, message);

                    return new Exception(message, e);
                });
            }

            return Control.ShouldNeverBeHere<IDataReader>();
        }

        private class CosmosExceptionLogger
        {
            private const string cosmosExceptionLogPath = "https://cosmos08.osdinfra.net/cosmos/Knowledge.upload/local/Tools/SatoriBrowser/Exceptions/exception_log.tsv";

            private static EntityCore.Common.Cosmos.CosmosStream log = new EntityCore.Common.Cosmos.CosmosStream(cosmosExceptionLogPath);

            public static void Log(Exception e, string message)
            {
                Control.SafeExec(() => log.Append(
                    System.Environment.MachineName,
                    DateTime.Now.ToString(),
                    EntityCore.Common.Cosmos.CosmosStream.Encode(message),
                    EntityCore.Common.Cosmos.CosmosStream.Encode(e.ToString()))
                );
            }
        }
    }
}
