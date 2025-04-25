using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Configuration;
using MailKit;
using System.Globalization;
using Serilog.Formatting.Display;
using Microsoft.VisualBasic;
using Serilog.Debugging;
using MailKit.Net.Smtp;
using MimeKit;
using MailKit.Security;

namespace EmailTranscriptSink;

/// <summary>
/// EmailTranscriptSink - an emailing sink which stores up log events and send them on demand or when the sink gets disposed
/// </summary>
public class EmailTranscriptSink : ILogEventSink, IDisposable
{
    private readonly string _defaultTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}";
    private readonly string _outputTemplate;
    private readonly MessageTemplateTextFormatter _formatter;
    private readonly IFormatProvider _formatProvider;
    private readonly Func<SmtpClient, MimeMessage, ConnectionSettings, bool> _configAndSend;
    private List<string> _body = new List<string>();

/// <summary>
/// EmailTranscriptSink - public constructor
/// </summary>
/// <param name="configAndSend">
/// A Func&lt;SmtpClient, MailMessage, bool&gt; that configures the SmtpClient and sets the properties of the MailMessage, EXCEPT the body
/// which contains the log events.  You MAY modify the body if needed, but do not have to.  The func returns a bool where FALSE 
/// means to NOT send the email.
/// </param>
/// <param name="formatProvider">A CultureInfo providing localization for formatting.  Defaults to InvariantCulture</param>
/// <param name="outputTemplate">The Serilog message template used to format log events.  
/// Defaults to "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
/// </param>
  public EmailTranscriptSink(
    Func<SmtpClient, MimeMessage, ConnectionSettings, bool> configAndSend,
    IFormatProvider formatProvider = null,
    string outputTemplate = null)
    {
        _configAndSend = configAndSend;
        _formatProvider =_formatProvider ?? CultureInfo.InvariantCulture;
        _outputTemplate = outputTemplate ?? _defaultTemplate;
        _formatter = new MessageTemplateTextFormatter(_outputTemplate, _formatProvider);
    }

/// <summary>
/// Sends the Log and disposes of internal resources
/// </summary>
  public void Dispose()
  {
    SendLog();
    _body = null;
  }

/// <summary>
/// Sends the Email and resets the log to empty.  This will call the configAndSend func to do final setup.  It should return true to send the 
/// email and false to prevent sending this log section.  False prevents the log from being cleared.
/// </summary>
  public void SendLog()
  {
    var builder = new BodyBuilder() {
        TextBody = string.Join("\r\n", _body)
      };

    var connectionSettings = new ConnectionSettings()
    {
      Host = "",
      Port = 25,
      ConnectionSecurity = SecureSocketOptions.Auto
    };

    var client = new SmtpClient();
    MimeMessage msg = new MimeMessage()
    {
        Body = builder.ToMessageBody(),
        Subject = "Log Transcript"
    };

    if (_configAndSend(client, msg, connectionSettings))
    {
      try
      {
        client.Connect(connectionSettings.Host, connectionSettings.Port, connectionSettings.ConnectionSecurity);
        client.Send(msg);
        _body = new List<string>();
      }
      catch (Exception ex)
      {
        SelfLog.WriteLine($"{ex.Message}{Environment.NewLine}{ex.StackTrace}");
      }
    }
  }

/// <summary>
/// Formats a log event and adds it to the email body
/// </summary>
/// <param name="logEvent"></param>
    public void Emit(LogEvent logEvent)
    {
        var sw = new StringWriter();
        _formatter.Format(logEvent, sw);
        _body.Add(sw.ToString());
    }
}

/// <summary>
/// Configuration extension for the EmailTranscriptSink
/// </summary>
public static class EmailTranscriptSinkExtensions
{
    /// <summary>
    /// Configures the sink
    /// </summary>
    /// <param name="loggerConfiguration"></param>
/// <param name="configureAndSend">
/// A Func&lt;SmtpClient, MailMessage, bool&gt; that configures the SmtpClient and sets the properties of the MailMessage, EXCEPT the body
/// which contains the log events.  You MAY modify the body if needed, but do not have to.  The func returns a bool where FALSE 
/// means to NOT send the email.
/// </param>
/// <param name="formatProvider">A CultureInfo providing localization for formatting.  Defaults to InvariantCulture</param>
/// <param name="outputTemplate">The Serilog message template used to format log events.  
/// Defaults to "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
/// </param>
    public static LoggerConfiguration EmailTranscript(
                this LoggerSinkConfiguration loggerConfiguration,
                Func<SmtpClient, MimeMessage, ConnectionSettings, bool> configureAndSend,
                IFormatProvider formatProvider = null,
                string outputTemplate = null)
    {
        return loggerConfiguration.Sink(new EmailTranscriptSink(configureAndSend, formatProvider, outputTemplate));
    }

}

    /// <summary>
    /// 
    /// </summary>
  public class ConnectionSettings 
  {
    /// <summary>
    /// 
    /// </summary>
    public string Host {get; set; }
    /// <summary>
    /// 
    /// </summary>
    public int Port { get;set; }
    /// <summary>
    /// 
    /// </summary>
    public SecureSocketOptions ConnectionSecurity { get; set; }
  }
