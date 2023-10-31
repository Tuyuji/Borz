using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace Borz.Core;

public enum LogLevel
{
    //Event is a special case, it's not a log level, its a signal to the log thread.
    Evnt = -1,

    Debug = 0,
    Info = 1,
    Warning = 2,
    Error = 3,
    Fatal = 4
}

public static class MugiLog
{
    public static string TimeFormat = "yyyy/MM/dd HH:mm:ss.ff";
    public static bool ShowTime = false;
    public static LogLevel MinLevel = LogLevel.Info;

    private struct LogInstance
    {
        public LogLevel Level;
        public string Message;
        public dynamic? Data;

        public LogInstance(LogLevel level, string message, dynamic? data = null)
        {
            Level = level;
            Message = message;
            Data = data;
        }
    }

    //The console output stream, aka fd 0 and fd 1
    private static TextWriter _consoleOut;
    private static TextWriter _consoleErrOut;

    //The stream that Console will now write to
    private static MugiWriter _mugiInfoWriter;
    private static MugiWriter _mugiErrorWriter;

    private static readonly ConcurrentQueue<LogInstance> _logQueue = new();
    private static Thread _logThread;
    private static readonly EventWaitHandle _logWaitHandle = new(false, EventResetMode.AutoReset);
    private static uint _levelMaxLength = 0;

    public static void Init(TextWriter? consoleOut = null, TextWriter? consoleErrOut = null)
    {
        //Find the largest log level string length
        var levelNames = Enum.GetNames<LogLevel>();
        foreach (var levelName in levelNames)
            if (levelName.Length > _levelMaxLength)
                _levelMaxLength = (uint)levelName.Length;

        _consoleOut = consoleOut ?? Console.Out;
        _consoleErrOut = consoleErrOut ?? Console.Error;

        _mugiInfoWriter = new MugiWriter(LogLevel.Info);
        _mugiErrorWriter = new MugiWriter(LogLevel.Error);


        _logThread = new Thread(LogThread) { Name = "Log Thread" };
        _logThread.Start();


        //Console.SetOut(_mugiInfoWriter);
        //Console.SetError(_mugiErrorWriter);
    }

    public static void Shutdown()
    {
        //Console.SetOut(_consoleOut);
        //Console.SetError(_consoleErrOut);

        _logQueue.Enqueue(new LogInstance(LogLevel.Evnt, "quit"));
        _logWaitHandle.Set();
        _logThread.Join();
    }

    private static void LogThread()
    {
        while (true)
        {
            if (!_logQueue.TryDequeue(out var instance))
            {
                _logWaitHandle.WaitOne();
                continue;
            }

            var type = instance.Level;
            var message = instance.Message;

            if (type == LogLevel.Evnt)
            {
                var data = instance.Data;
                if (message == "quit")
                    break;
            }

            var writer = type switch
            {
                LogLevel.Error => _consoleErrOut,
                LogLevel.Fatal => _consoleErrOut,
                _ => _consoleOut
            };

            var levelStr = Enum.GetName(type.GetType(), type);
            writer.Write(levelStr);
            //Space this out to the max length
            for (var i = 0; i < _levelMaxLength - levelStr.Length; i++)
                writer.Write(' ');
            writer.Write(' ');
            writer.Write(": ");
            writer.Write(message);
            writer.Write('\n');
        }
    }

    private static void LowLevelWrite(LogLevel level, string message)
    {
        if (!Debugger.IsAttached && level < MinLevel)
            return;

        _logQueue.Enqueue(new LogInstance(level, message));
        _logWaitHandle.Set();
    }

    public static void WriteLog(LogLevel level, string message)
    {
        LowLevelWrite(level, message);
    }

    public static void Debug(string message)
    {
        LowLevelWrite(LogLevel.Debug, message);
    }

    public static void Info(string message)
    {
        LowLevelWrite(LogLevel.Info, message);
    }

    public static void Warning(string message)
    {
        LowLevelWrite(LogLevel.Warning, message);
    }

    public static void Error(string message)
    {
        LowLevelWrite(LogLevel.Error, message);
    }

    public static void Fatal(string message)
    {
        LowLevelWrite(LogLevel.Fatal, message);
    }


    public static void Wait()
    {
        while (!_logQueue.IsEmpty)
        {
            //Wait for the queue to clear...
        }
    }
}

public class MugiWriter : TextWriter
{
    public override Encoding Encoding => Encoding.Default;

    public LogLevel Level { get; set; }

    public MugiWriter(LogLevel level)
    {
        Level = level;
    }

    public override void WriteLine(string? value)
    {
        if (value == null)
            return;
        MugiLog.WriteLog(Level, value);
    }
}