using PathEdit.common;
using PathEdit.Parser.Command;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Media;

namespace PathEdit.Parser;
public class PathDrawable {
    public List<PathCommand> Commands;

    public PathDrawable() {
        Commands = new List<PathCommand>();
    }
    public PathDrawable(List<PathCommand> commands) {
        Commands = commands;
    }

    public void Add(IEnumerable<PathCommand> commands) {
        Commands.AddRange(commands);
    }

    public void DrawTo(IGraphics graphics) {
        var prevCommand = default(PathCommand?);
        foreach (var command in Commands) {
            command.DrawTo(graphics, prevCommand);
            prevCommand = command;
        }
        graphics.Fill();
    }

    public PathDrawable Transform(Matrix matrix) {
        var prevCommand = default(PathCommand?);
        foreach (var command in Commands) {
            command.Transform(matrix, prevCommand);
            prevCommand = command;
        }
        return this;
    }

    public string Compose() {
        var sb = new StringBuilder();
        var prevCommand = default(PathCommand?);
        foreach (var command in Commands) {
            command.ComposeTo(sb, prevCommand);
            prevCommand = command;
        }
        return sb.ToString();
    }

    /**
     * H / V コマンドがあると回転時に困るので、それを L コマンドに変換する。
     */
    public PathDrawable PreProcessForRotation() {
        var prevCommand = default(PathCommand?);
        for(int i = 0; i < Commands.Count; i++) {
            var command = Commands[i];
            if (command is LineHorzCommand horz) {
                command = horz.ToLineCommand(prevCommand);
                Commands[i] = command;
            } else if(command is LineVertCommand vert) {
                command = vert.ToLineCommand(prevCommand);
                Commands[i] = command;
            }
            command.ResolveEndPoint(prevCommand);
            prevCommand = command;
        }
        return this;
    }

    public PathDrawable ResolveEndPoint() {
        var prevCommand = default(PathCommand?);
        foreach (var command in Commands) {
            command.ResolveEndPoint(prevCommand);
            prevCommand = command;
        }
        return this;
    }

    public PathDrawable Clone() {
        var list = new List<PathCommand>();
        foreach (var command in Commands) {
            list.Add(command.Clone());
        }
        return new PathDrawable(list);
    }

    public PathDrawable RoundCoordinateValue(int digit) {
        foreach (var command in Commands) {
            command.RoundCoordinateValue(digit);
        }
        return this;
    }

    public static PathDrawable Parse(string pathData) {
        return new PathDrawable(PathParser.Parse(pathData));
    }

    #region Edit Commands
    public PathDrawable RemoveCommand(PathCommand command) {
        Commands.Remove(command);
        return this;
    }

    //public PathDrawable InsertCommand(int index, PathCommand command) {
    //    Commands.Insert(index, command);
    //    return this;
    //}

    public int IndexOf(PathCommand command) {
        return Commands.IndexOf(command);
    }

    public PathDrawable InsertCommand(PathCommand prev, PathCommand command) {
        var index = Commands.IndexOf(prev)+1;
        if(index>=Commands.Count) {
            Commands.Add(command);
        } else {
            Commands.Insert(index, command);
        }
        return this;
    }

    public PathDrawable ReplaceCommand(int index, PathCommand command) {
        try {
            Commands[index] = command;
        } catch(Exception ex) {
            LoggerEx.error(ex);
        }
        return this;
    }

    //public PathDrawable ReplaceCommand(PathCommand at, PathCommand command) {
    //    try
    //    {
    //        var index = Commands.IndexOf(at);
    //        Commands[index] = command;
    //    } catch(Exception ex)
    //    {
    //        Console.WriteLine(ex);
    //    }
    //    return this;
    //}

    #endregion
}
