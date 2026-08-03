using System.Data.Common;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AuthService.Api.Infrastructure.Observability;

public sealed class DbMetricsInterceptor : DbCommandInterceptor
{
    private readonly ConditionalWeakTable<DbCommand, Stopwatch> _timers = new();

    public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        _timers.AddOrUpdate(command, Stopwatch.StartNew());
        return result;
    }

    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        if (_timers.TryGetValue(command, out var timer)) AuthTelemetry.DbCommandDuration.Record(timer.Elapsed.TotalMilliseconds);
        return result;
    }
}
