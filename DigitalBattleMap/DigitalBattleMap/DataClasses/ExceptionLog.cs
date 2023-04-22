using System;
using System.Collections.Generic;
using System.Linq;

namespace DigitalBattleMap.DataClasses;

public class ExceptionLog
{
    public ExceptionLog(Exception exception)
    {
        DateTime = System.DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss");
        Message = exception.Message;
        Type = exception.GetType().ToString();
        StackTrace = exception.StackTrace?.Split("\r\n").Select(e => e.TrimStart());
        Source = exception.Source;
    }

    public string DateTime { get; set; }
    public string Type { get; set; }
    public string Source { get; set; }
    public string Message { get; set; }
    public IEnumerable<string> StackTrace { get; set;}
}
