using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace PathEdit.common;

public enum LogLevel {
    DEBUG,
    INFO,
    WARN,
    ERROR,
}

public interface ILogTracer {
    void trace(LogLevel level, string message);
}

internal class ConsoleLogger : ILogTracer {
    public void trace(LogLevel level, string message) {
        Debug.WriteLine($"[{level}]: {message}");
    }
}

public static class Logger {
    public static ILogTracer Tracer { get; set; } = new ConsoleLogger();
    
    private static string safeFormat(string fmt, params object[] args) {
        try {
            if(args.Length>0) {
                return string.Format(fmt, args);
            } else {
                return fmt;
            }
        } catch(Exception) {
            return fmt;
        }
    }

    public static void error(string fmt, params object[] args) {
        Tracer?.trace(LogLevel.ERROR, safeFormat(fmt, args));
    }
    public static void error(Exception e, string fmt, params object[] args) {
        var msg = safeFormat(fmt, args);
        if(!string.IsNullOrEmpty(msg)) {
            msg = msg + "\n" + e.ToString();
        } else {
            msg = e.ToString();
        }
        Tracer?.trace(LogLevel.ERROR, msg);
    }
    public static void error(Exception e) {
        error(e, "");
    }
    public static void warn(string fmt, params object[] args) {
        Tracer?.trace(LogLevel.WARN, safeFormat(fmt, args));
    }
    public static void info(string fmt, params object[] args) {
        Tracer?.trace(LogLevel.INFO, safeFormat(fmt, args));
    }
    [Conditional("DEBUG")]
    public static void debug(string fmt, params object[] args) {
        Tracer?.trace(LogLevel.DEBUG, safeFormat(fmt, args));
    }
}

/**
 * 呼び出し元情報を出力する拡張ロガー
 */
public class LoggerEx {
    static string getFileName(string path) {
        try {
            return System.IO.Path.GetFileName(path);
        }
        catch (Exception) {
            return path;
        }
    }

    public static string composeMessage(string msg, string prefix, string filePath, string memberName, int sourceLineNumber) {
        try {
            return $"{prefix}: {getFileName(filePath)}({sourceLineNumber}):{memberName}() {msg}";
        } catch(Exception) {
            Debug.Assert(false, "logging message cannot be composed.");
            return msg;
        }
    }

    public static void error(string msg, string prefix = "", [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0) {
        Logger.error(composeMessage(msg, prefix, filePath, memberName, sourceLineNumber));
    }
    public static void error(Exception e, string prefix = "", [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0) {
        Logger.error(e, composeMessage("", prefix, filePath, memberName, sourceLineNumber));
    }
    public static void error(string msg, Exception e, string prefix = "", [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0) {
        Logger.error(e, composeMessage(msg, prefix, filePath, memberName, sourceLineNumber));
    }

    public static void warn(string msg, string prefix = "", [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0) {
        Logger.warn(composeMessage(msg, prefix, filePath, memberName, sourceLineNumber));
    }

    public static void info(string msg, string prefix = "", [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0) {
        Logger.info(composeMessage(msg, prefix, filePath, memberName, sourceLineNumber));
    }

    public static void debug(string msg, string prefix = "", [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0) {
        Logger.debug(composeMessage(msg, prefix, filePath, memberName, sourceLineNumber));
    }


    public string Prefix { get; }

    public LoggerEx(string prefix) {
        Prefix = prefix;
    }
    public LoggerEx(string prefix, LoggerEx parent) {
        Prefix = $"{parent.Prefix}.{prefix}";
    }

    public void error(string msg, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0) {
        Logger.error(composeMessage(msg, Prefix, filePath, memberName, sourceLineNumber));
    }
    public void error(Exception e, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0) {
        Logger.error(e, composeMessage("", Prefix, filePath, memberName, sourceLineNumber));
    }
    public void error(string msg, Exception e, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0) {
        Logger.error(e, composeMessage(msg, Prefix, filePath, memberName, sourceLineNumber));
    }

    public void warn(string msg, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0) {
        Logger.warn(composeMessage(msg, filePath, Prefix, memberName, sourceLineNumber));
    }

    public void info(string msg, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0) {
        Logger.info(composeMessage(msg, filePath, Prefix, memberName, sourceLineNumber));
    }

    [Conditional("DEBUG")]
    public void debug(string msg, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0) {
        Logger.debug(composeMessage(msg, filePath, Prefix, memberName, sourceLineNumber));
    }
}

// 時間計測用クラス
//
// (Start)                    (End)
//    |--Lap--|---Lap---|--Lap--|
//    |---------Total-----------|
//
// 使い方
// var t = new TimeLogger("ほげ"); // (Start)
// function1();
//  t.WriteLap("function-1の処理時間");
// function2();
//  t.WriteLap("function-2の処理時間");
// function3();
//  t.WriteLap("function-3の処理時間");
//  t.WriteTotal("合計時間");  // (End)
// 
public class TimeLogger {
    string prefix;
    int start;
    int prev;

    public TimeLogger(string prefix) {
        this.prefix = prefix;
        start = prev = Environment.TickCount;
    }

    public int Lap {
        get {
            int c = Environment.TickCount;
            int d = c - prev;
            prev = c;
            return d;
        }
    }

    public int Total => Environment.TickCount - start;

    public void Reset() {
        prev = Environment.TickCount;
    }

    [Conditional("DEBUG")]
    public void WriteLap(string label) {
        Logger.debug($"{prefix} - {label}: {Lap} ms");
    }

    [Conditional("DEBUG")]
    public void WriteTotal(string label = "total") {
        Logger.debug($"{prefix} - {label}: {Total} ms");
    }

    public static TimeLogger CreateInstance(string prefix) {
#if DEBUG
        return new TimeLogger(prefix);
#else
        return null;
#endif
    }
}

/**
 * 関数などのスコープに入った、出た、をログに記録するためのツール
 * 使い方・・・監視したいスコープの一番外側で、using(new ScopeLogger()){}を定義する。
 * 
 * void SomeFunc() {
 *  using(new ScopeLogger("Sample", "SomeFunc") {
 *      ... SomeFuncの処理
 *  }
 * }
 */
public class ScopeLogger : IDisposable {
    private static long SerialNoSource = 0;
    private long SerialNo;
    private string Prefix;
    private string ScopeName;
    public ScopeLogger(string prefix, string scopeName) {
        SerialNo = Interlocked.Increment(ref SerialNoSource);
        Prefix = prefix;
        ScopeName = scopeName;
        Output("enter {");
    }

    public void Dispose() {
        Output("exit }");
    }

    [Conditional("DEBUG")]
    public void Output(string msg) {
        Logger.debug($"{Prefix}-{ScopeName} (#{SerialNo}): {msg}");
    }
}
