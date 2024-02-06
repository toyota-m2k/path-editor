using PathEdit.Parser;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using static PathEdit.MultiSource;

namespace PathEdit;

public interface IUndoBuffer {
    IReadOnlyReactiveProperty<bool> CanUndo { get; }
    IReadOnlyReactiveProperty<bool> CanRedo { get; }
    void Undo(EditorViewModel vm);
    void Redo(EditorViewModel vm);
}

public class UndoBuffer : IUndoBuffer {
    public enum Operation {
        AddSource,
        RemoveSource,
        UpdateSource,
        SetCurrent,
        ReOrder,
        SetPath,
    }
    public interface IOperation {
        Operation Operation { get; }
        void Undo(EditorViewModel vm);
        void Redo(EditorViewModel vm);
    }
    
    class SetPathOperation : IOperation {
        public Operation Operation => Operation.AddSource;
        private int Index { get; }
        private string BeforePath { get; }
        private string AfterPath { get; }

        public SetPathOperation(int index, string before, string after) {
            Index = index;
            BeforePath = before;
            AfterPath = after;
        }
        public void Undo(EditorViewModel vm) {
            vm.EditingPathDrawable.Value = PathDrawable.Parse(BeforePath);
        }
        public void Redo(EditorViewModel vm) {
            vm.EditingPathDrawable.Value = PathDrawable.Parse(AfterPath);
        }
    }

    class UpdateSourceOperation : IOperation {
        public Operation Operation => Operation.UpdateSource;
        private int Index { get; }
        private Source Before { get; }
        private Source After { get; }

        public UpdateSourceOperation(int index, Source before, Source after) {
            Index = index;
            Before = before;
            After = after;
        }


        public void Undo(EditorViewModel vm) {
            vm.Sources.UpdateSource(Index, Before.OriginalPath, Before.EditingPath);
        }
        public void Redo(EditorViewModel vm) {
            vm.Sources.UpdateSource(Index, After.OriginalPath, After.EditingPath);
        }
    }

    class AddSourceOperation : IOperation {
        public Operation Operation => Operation.AddSource;
        private int OrgIndex { get; }
        private int Index { get; }
        private string Path { get; }

        public AddSourceOperation(int index, string path, int orgIndex) {
            Index = index;
            Path = path;
            OrgIndex = orgIndex;
        }
        public void Undo(EditorViewModel vm) {
            vm.Sources.RemoveSource(Index);
            vm.Sources.SetCurrentIndex(OrgIndex);
        }
        public void Redo(EditorViewModel vm) {
            vm.Sources.AddSource(Index, Path);
        }
    }


    class RemoveSourceOperation : IOperation {
        public Operation Operation => Operation.RemoveSource;
        private int OrgIndex { get; }
        private int Index { get; }
        private MultiSource.Source RemovedSource { get; }

        public RemoveSourceOperation(int index, MultiSource.Source source, int orgIndex) {
            Index = index;
            RemovedSource = source;
            OrgIndex = orgIndex;
        }
        public void Undo(EditorViewModel vm) {
            vm.Sources.AddSource(Index, RemovedSource.OriginalPath);
            vm.Sources.SetCurrentIndex(OrgIndex);
        }
        public void Redo(EditorViewModel vm) {
            vm.Sources.RemoveSource(Index);
        }
    }

    class SetCurrentOperation : IOperation {
        public Operation Operation => Operation.SetCurrent;
        private int BeforeIndex { get; }
        private int AfterIndex { get; }

        public SetCurrentOperation(int before, int after) {
            BeforeIndex = before;
            AfterIndex = after;
        }
        public void Undo(EditorViewModel vm) {
            vm.Sources.SetCurrentIndex(BeforeIndex);
        }
        public void Redo(EditorViewModel vm) {
            vm.Sources.SetCurrentIndex(AfterIndex);
        }
    }

    class ReOrderOperation : IOperation {
        public Operation Operation => Operation.ReOrder;
        private int FromIndex { get; }
        private int ToIndex { get; }

        public ReOrderOperation(int from, int to) {
            FromIndex = from;
            ToIndex = to;
        }
        public void Undo(EditorViewModel vm) {
            vm.Sources.ReOrder(ToIndex, FromIndex);
        }
        public void Redo(EditorViewModel vm) {
            vm.Sources.ReOrder(FromIndex, ToIndex);
        }
    }

    private List<IOperation> UndoList { get; } = new();
    private int UndoIndex = -1;
    private bool operating = false;

    private ReactiveProperty<bool> _canUndo { get; } = new();
    private ReactiveProperty<bool> _canRedo { get; } = new();
    public IReadOnlyReactiveProperty<bool> CanUndo => _canUndo;
    public IReadOnlyReactiveProperty<bool> CanRedo => _canRedo;

    private void UpdateState() {
        _canUndo.Value = 0 <= UndoIndex;
        _canRedo.Value = UndoIndex + 1 < UndoList.Count;
    }

    private void Push(Func<IOperation> fn) {
        if (operating) {
            return;
        }
        if (UndoIndex + 1 < UndoList.Count) {
            UndoList.RemoveRange(UndoIndex + 1, UndoList.Count - UndoIndex - 1);
        }
        UndoList.Add(fn());
        UndoIndex++;
        UpdateState();
    }

    public void PushAddSource(int index, string path, int orgIndex) {
        Push(()=>new AddSourceOperation(index, path, orgIndex));
    }
    public void PushRemoveSource(int index, MultiSource.Source source, int orgIndex) {
        Push(() => new RemoveSourceOperation(index, source, orgIndex));
    }
    public void PushSetCurrent(int before, int after) {
        Push(() => new SetCurrentOperation(before, after));
    }
    public void PushReOrder(int from, int to) {
        Push(() => new ReOrderOperation(from, to));
    }
    public void PushSetPath(int index, string before, string after) {
        Push(() => new SetPathOperation(index, before, after));
    }
    public void PushUpdateSource(int index, Source before, Source after) {
        Push(() => new UpdateSourceOperation(index, before, after));
    }
    public void Undo(EditorViewModel vm) {
        if (operating) {
            return;
        }
        if (0 <= UndoIndex) {
            operating = true;
            UndoList[UndoIndex].Undo(vm);
            operating = false;
            UndoIndex--;
            UpdateState();
        }
    }

    public void Redo(EditorViewModel vm) {
        if (operating) {
            return;
        }
        if (UndoIndex + 1 < UndoList.Count) {
            operating = true;
            UndoIndex++;
            UndoList[UndoIndex].Redo(vm);
            operating = false;
            UpdateState();
        }
    }
}
