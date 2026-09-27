using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Storage.Server
{
    internal static class ServerTelemetry
    {
        public const string Name = "Storage.Server";

        public static readonly ActivitySource ActivitySource = new(Name);

        public static readonly Meter Meter = new(Name);

        public static readonly Counter<long> CommandsProcessed = Meter.CreateCounter<long>(
                name: "storage.commands.processed",
                unit: "{command}",
                description: "Number of processed commands"
            );

        public static readonly Histogram<double> CommandDuration = Meter.CreateHistogram<double>(
                name: "storage.command.duration",
                unit: "ms",
                description: "Command processing duration"
            );

    }
}
