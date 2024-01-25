using PathEdit.Parser.Command;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Media;

namespace PathEdit.Parser;
internal class PathDrawable {
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
        graphics.Draw();
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
                command = horz.ToLineCommand(prevCommand?.LastResolvedPoint.Y ?? 0);
                Commands[i] = command;
            } else if(command is LineVertCommand vert) {
                command = vert.ToLineCommand(prevCommand?.LastResolvedPoint.X ?? 0);
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
}
