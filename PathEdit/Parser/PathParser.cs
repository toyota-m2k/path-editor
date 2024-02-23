using PathEdit.Parser.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PathEdit.Parser;
internal class PathParser {
    static Regex pathPattern = new Regex("""(?<cmd>[MmLlHhVvCcSsQqTtAaZz])(?<params>[-eE.,\s\d]*)""");
    static Regex paramsPattern = new Regex("""([+-]?(?:\d*\.)?\d+(?:[Ee][+-]?\d+)?)""");

    /**
     * パス文字列が正しいかどうかをチェックする
     * いつかPathCommandリストを作らないで、チェックだけするようにしたい。
     */
    public static bool Check(string pathString) {
        try {
            var list = Parse(pathString);
            return list.Count>0;
        } catch(Exception) {
            return false;
        }
    }

    public static List<PathCommand> Parse(string pathString) {
        var commands = new List<PathCommand>();
        var matches = pathPattern.Matches(pathString);
        if(matches.Count==0) {
            throw new Exception($"Invalid path {pathString}");
        }
        foreach (Match match in matches) {
            var cmd = match.Groups["cmd"].Value.Trim();
            if(string.IsNullOrEmpty(cmd)) {
                throw new Exception($"Invalid path {pathString}");
            }   
            var paramStr = match.Groups["params"].Value;
            var paramList = paramsPattern.Matches(paramStr).Select(m => { 
                var d = double.Parse(m.Value); 
                if(double.IsNaN(d)||double.IsInfinity(d)) {
                    throw new Exception($"Invalid parameter {m.Value}");
                }
                return d;
            }).ToList();

            switch(cmd) { 
                case "M":
                case "m":
                    commands.AddRange(MoveCommand.Parse(cmd, paramList));
                    break;
                case "L":
                case "l": {
                    commands.AddRange(LineCommand.Parse(cmd, paramList));
                    break;
                }
                case "H":
                case "h": {
                    commands.AddRange(LineHorzCommand.Parse(cmd, paramList));
                    break;
                }
                case "V":
                case "v": {
                    commands.AddRange(LineVertCommand.Parse(cmd, paramList));
                    break;
                }
                case "C":
                case "c": {
                    commands.AddRange(BezierCubicCommand.Parse(cmd, paramList));
                    break;
                }
                case "S":
                case "s": {
                    commands.AddRange(SmoothBezierCubicCommand.Parse(cmd, paramList));
                    break;
                }
                case "Q":
                case "q": {
                        commands.AddRange(BezierQuadraticCommand.Parse(cmd, paramList));
                        break;
                }
                case "T":
                case "t": {
                    commands.AddRange(SmoothBezierQuadraticCommand.Parse(cmd, paramList));
                    break;
                }
                case "A":
                case "a": {
                    commands.AddRange(ArcCommand.Parse(cmd, paramList));
                    break;
                }
                case "Z":
                case "z": {
                    commands.AddRange(CloseCommand.Parse(cmd, paramList));
                    break;
                }
                default: {
                    throw new Exception($"Unknown command {cmd}");
                    }
            }
            
        }
        return commands;
    }
}
