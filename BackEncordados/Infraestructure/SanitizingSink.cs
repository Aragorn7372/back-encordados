using BackEncordados.Common.Utils;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace BackEncordados.Infraestructure;

public class SanitizingSink : ILogEventSink
{
    private readonly MessageTemplateTextFormatter _formatter;

    public SanitizingSink(string outputTemplate, IFormatProvider? formatProvider)
    {
        _formatter = new MessageTemplateTextFormatter(outputTemplate, formatProvider);
    }

    public void Emit(LogEvent logEvent)
    {
        if (logEvent.Properties.Count == 0)
        {
            _formatter.Format(logEvent, Console.Out);
            return;
        }

        var sanitized = logEvent.Properties
            .Select(p => new LogEventProperty(p.Key, SanitizeValue(p.Value)));

        var sanitizedEvent = new LogEvent(
            logEvent.Timestamp,
            logEvent.Level,
            logEvent.Exception,
            logEvent.MessageTemplate,
            sanitized);

        _formatter.Format(sanitizedEvent, Console.Out);
    }

    private static LogEventPropertyValue SanitizeValue(LogEventPropertyValue value)
    {
        if (value is ScalarValue { Value: string str })
            return new ScalarValue(str.SanitizeForLog());
        return value;
    }
}
