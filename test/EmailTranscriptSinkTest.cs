using EmailTranscriptSink;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Formatting.Display;
using Serilog.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog.Configuration;
using Xunit;
using Xunit.Abstractions;
using Serilog;
namespace test;

/// <summary>
/// 
/// </summary>
public class EmailTranscriptSinkTest
{
    /// <summary>
    /// 
    /// </summary>
    [Fact]
    public void Test1()
    {
        string body = "";
        string subject = "";
        Log.Logger = new LoggerConfiguration().WriteTo.EmailTranscript((client, msg, connectionSettings) =>
        {
            body = msg.GetTextBody(MimeKit.Text.TextFormat.Plain);
            subject = msg.Subject;
            return false;
        }).CreateLogger();

        Log.Information("Test INF");
        Log.Warning("Test WRN");
        Log.Error("Test ERR");
        Log.CloseAndFlush();

        Assert.Contains("INF", body);
        Assert.Contains("WRN", body);
        Assert.Contains("ERR", body);
        Assert.True(subject == "Log Transcript");
    }
}
