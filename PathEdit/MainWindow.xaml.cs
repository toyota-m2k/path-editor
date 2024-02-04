using Microsoft.UI.Xaml;
using System;
using System.IO;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PathEdit {
    /**
     * メインウィンドウ
     */
    public sealed partial class MainWindow : Window {

        public MainWindow() {
            this.InitializeComponent();
            this.Title = "SVG Path Editor";
            this.AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/program-icon.ico"));
        }

    }
}
