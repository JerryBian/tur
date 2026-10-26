using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tur.Logging
{
    /// <summary>
    /// Writes one NDJSON record per log item to stdout, for machine consumers
    /// (pipes, subprocess callers). Never writes colors or control characters.
    /// </summary>
    public class JsonAppender : BlockingAppender
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly bool _verbose;
        private readonly TextWriter _writer;

        public JsonAppender(bool verbose, TextWriter writer = null)
        {
            _verbose = verbose;
            _writer = writer;
        }

        protected override void Handle(TurLogItem item)
        {
            if (item.LogLevel == TurLogLevel.Trace && !_verbose)
            {
                return;
            }

            JsonLogRecord record = new()
            {
                Ts = DateTimeOffset.Now.ToString("o"),
                Level = item.LogLevel.ToString().ToLowerInvariant(),
                Message = item.Message,
                Suffix = item.Suffix,
                Error = item.Error?.ToString()
            };

            try
            {
                (_writer ?? Console.Out).WriteLine(JsonSerializer.Serialize(record, _jsonOptions));
            }
            catch { }
        }

        private sealed class JsonLogRecord
        {
            [JsonPropertyName("ts")]
            public string Ts { get; set; }

            [JsonPropertyName("level")]
            public string Level { get; set; }

            [JsonPropertyName("message")]
            public string Message { get; set; }

            [JsonPropertyName("suffix")]
            public string Suffix { get; set; }

            [JsonPropertyName("error")]
            public string Error { get; set; }
        }
    }
}
