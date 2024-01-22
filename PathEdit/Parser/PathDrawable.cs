using PathEdit.Parser.Command;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Media;

namespace PathEdit.Parser;
internal class PathDrawable {
    private List<PathCommand> _commands;
    public PathDrawable() {
        _commands = new List<PathCommand>();
    }
    public PathDrawable(List<PathCommand> commands) {
        _commands = commands;
    }

    public void Add(IEnumerable<PathCommand> commands) {
        _commands.AddRange(commands);
    }

    public void DrawTo(IGraphics graphics) {
        var prevCommand = default(PathCommand?);
        foreach (var command in _commands) {
            command.DrawTo(graphics, prevCommand);
            prevCommand = command;
        }
        graphics.Draw();
    }

    public PathDrawable Transform(Matrix matrix) {
        var prevCommand = default(PathCommand?);
        foreach (var command in _commands) {
            command.Transform(matrix, prevCommand);
            prevCommand = command;
        }
        return this;
    }

    public string Compose() {
        var sb = new StringBuilder();
        var prevCommand = default(PathCommand?);
        foreach (var command in _commands) {
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
        for(int i = 0; i < _commands.Count; i++) {
            var command = _commands[i];
            if (command is LineHorzCommand horz) {
                command = horz.ToLineCommand(prevCommand?.LastResolvedPoint.Y ?? 0);
                _commands[i] = command;
            } else if(command is LineVertCommand vert) {
                command = vert.ToLineCommand(prevCommand?.LastResolvedPoint.X ?? 0);
                _commands[i] = command;
            }
            command.ResolveEndPoint(prevCommand);
            prevCommand = command;
        }
        return this;
    }

    public PathDrawable Clone() {
        var list = new List<PathCommand>();
        foreach (var command in _commands) {
            list.Add(command.Clone());
        }
        return new PathDrawable(list);
    }

    public PathDrawable RoundCoordinateValue(int digit) {
        foreach (var command in _commands) {
            command.RoundCoordinateValue(digit);
        }
        return this;
    }

    public static PathDrawable Parse(string pathData) {
        return new PathDrawable(PathParser.Parse(pathData));
    }
}
