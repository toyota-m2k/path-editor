using Microsoft.UI.Xaml;
using Reactive.Bindings;
using System;
using PathEdit.Parser;
using PathEdit.Graphics;
using Windows.UI;
using System.Reactive.Linq;
using PathEdit.common;
using Microsoft.Graphics.Canvas;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PathEdit {
    /**
     * メインウィンドウ
     */
    public sealed partial class MainWindow : Window {

        public MainWindow() {
            this.InitializeComponent();

        }

    }
}
