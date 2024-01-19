using System.Collections.Generic;
using System.Text;
using System.Windows.Media;

namespace PathEdit.Parser;
internal class PathDrawable {
    private readonly List<PathCommand> _commands;
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

    public static PathDrawable Parse(string pathData) {
        return new PathDrawable(PathParser.Parse(pathData));
    }
}
