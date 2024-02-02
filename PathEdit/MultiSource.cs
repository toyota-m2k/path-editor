using PathEdit.Parser;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using static PathEdit.MultiSource;

namespace PathEdit;

//public interface IMultiSource { 
//    public int AddSource(int index, string originalPath, bool setCurrent);
//    public void RemoveSource(int index);
//    public void SetCurrent(int index);
//    public void ReOrder(int from, int to);

//    public string MergedPath { get; }
//    public string MargeSource(int[] selection);
//}

public interface  ISourceInMultiSource {
    public string OriginalPath { get; }
    public IReadOnlyReactiveProperty<bool> IsSelected { get; }
}

public class MultiSource {
    public class Source : ISourceInMultiSource {
        public string OriginalPath { get; }
        public string EditingPath { get; set; }
        public IReadOnlyReactiveProperty<bool> IsSelected { get; }

        public Source(IReadOnlyReactiveProperty<Source?> current, string originalPath = "M 0 0", string? editingPath=null) {
            OriginalPath = originalPath;
            EditingPath = editingPath ?? originalPath;
            IsSelected = current.Select(c => c == this).ToReadOnlyReactiveProperty();
        }
    }

    private static string SafePath(string path) {
        try {
            return PathDrawable.Parse(path).Compose();
        }
        catch (Exception) {
            return "M 0 0";
        }
    }

    private UndoBuffer UndoBuffer { get; } = new();

    private ObservableCollection<Source> _sources;
    public IReadOnlyList<Source> SourceList => _sources;

    private ReactiveProperty<Source?> _currentSource { get; } = new();

    private int _currentIndex = 0;
    public int CurrentIndex {
        get => _currentIndex;
        private set { 
            _currentIndex = value;
            _currentSource.Value = _sources[value];
        }
    }

    /**
     * 選択されていない他のソースのパス（CanvasのDrawでのみ使用）
     */
    public IEnumerable<string> OtherPaths => _sources.Where((_, i) => i != CurrentIndex).Select(c=>c.EditingPath);

    public Source this[int index] => _sources[index];
    public Source Current => _sources[CurrentIndex];


    public IReadOnlyReactiveProperty<bool> IsMulti { get; }
    private ReactiveProperty<int> _sourceCount { get; } = new(1);
    public IReadOnlyReactiveProperty<int> SourceCount => _sourceCount;



    public MultiSource() {
        IsMulti = _sourceCount.Select(c => c > 1).ToReadOnlyReactiveProperty();
        var firstSource = new Source(_currentSource);
        _sources = new ObservableCollection<Source> { firstSource };
        _currentSource.Value = firstSource;
        _sources.CollectionChanged += (s, e) => {
            switch(e.Action) {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    _sourceCount.Value = _sources.Count;
                    break;
                default:
                    break;
            }
        };
    }


    public bool UpdateCurrentPath(string path) {
        path = SafePath(path);
        if(_sources[CurrentIndex].EditingPath == path) return false;
        UndoBuffer.PushSetPath(CurrentIndex, _sources[CurrentIndex].EditingPath, path);
        _sources[CurrentIndex].EditingPath = path;
        return true;
    }

    public bool UpdateCurrentSource(string sourcePath, string? editingPath=null) {
        return UpdateSource(CurrentIndex, sourcePath, editingPath);
    }

    public bool UpdateSource(int index, string sourcePath, string? editingPath=null) {
        sourcePath = SafePath(sourcePath);
        if (_sources[index].OriginalPath == sourcePath && (editingPath==null || _sources[index].EditingPath==editingPath)) return false;
        var orgSource = _sources[index];
        var newSource = new Source(_currentSource, sourcePath, editingPath ?? SafePath(sourcePath));
        _sources[index] = newSource;
        UndoBuffer.PushUpdateSource(index, orgSource, newSource);
        if(index == CurrentIndex) {
            _currentSource.Value = newSource;
        }
        return true;
    }

    public bool SetCurrentIndex(int index) {
        if (index < 0 || index >= _sources.Count || CurrentIndex == index) {
            return false;
        }
        UndoBuffer.PushSetCurrent(CurrentIndex, index);
        CurrentIndex = index;
        return true;
    }
    
    public bool SetCurrentSource(object source) { 
        if(source is Source s) {
            return SetCurrentIndex(_sources.IndexOf(s));
        }
        return false;
    }


    public int AddSource(int index, string originalPath) {
        if (index < 0 || index > _sources.Count) {
            index = _sources.Count;
        }
        if(index == _sources.Count) {
            _sources.Add(new Source(_currentSource, originalPath));
        } else {
            _sources.Insert(index, new Source(_currentSource, originalPath));
        }
        UndoBuffer.PushAddSource(index, originalPath, CurrentIndex);
        CurrentIndex = index;
        return index;
    }

    public void RemoveSource(int index) {
        if(_sources.Count==1 || index<0 || index>=_sources.Count) {
            return;
        }
        var orgIndex = CurrentIndex;
        if (index==CurrentIndex) {
            CurrentIndex--;
            if(CurrentIndex <0) {
                CurrentIndex = 0;
            }
        }
        var removing = _sources[index];
        _sources.RemoveAt(index);
        UndoBuffer.PushRemoveSource(index, removing, orgIndex);
    }

    public void ReOrder(int from, int to) {
        if(from==to || from<0 ||from>=_sources.Count||to<0||to>=_sources.Count && to == from) {
            return;
        }
        var source = _sources[from];
        _sources.RemoveAt(from);
        _sources.Insert(to, source);
        if(from==CurrentIndex) {
            CurrentIndex = to;
        } else if(from<CurrentIndex &&to>=CurrentIndex) {
            CurrentIndex--;
        } else if(from>CurrentIndex &&to<=CurrentIndex) {
            CurrentIndex++;
        }
        UndoBuffer.PushReOrder(from, to);
    }

    public string MergedPath => string.Join(" ", _sources.Select(s => s.EditingPath));

    public string MargeSource(int[] selection) {
        return string.Join(" ", selection.Select(i => _sources[i].EditingPath));
    }

    public IUndoBuffer History => UndoBuffer;
}
