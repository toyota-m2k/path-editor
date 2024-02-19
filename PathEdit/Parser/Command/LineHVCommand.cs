using System.Windows;

namespace PathEdit.Parser.Command;
public abstract class LineHVCommand : PathCommand {
    public LineHVCommand(bool isRelative, Point endPoint)
        : base(isRelative, endPoint) {
    }

    public abstract LineCommand ToLineCommand(PathCommand? prevCommand);
}
