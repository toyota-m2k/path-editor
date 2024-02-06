using Microsoft.UI.Xaml;
using PathEdit.common;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PathEdit {
    /**
     * メインウィンドウ
     */
    public sealed partial class MainWindow : Window {

        public MainWindow() {
            string AppName;
            Version AppVersion;
            this.InitializeComponent();
            if (RuntimeHelper.IsMSIX) {
                var package = Windows.ApplicationModel.Package.Current;
                AppName = package.DisplayName;
                var v = package.Id.Version;
                AppVersion = new(v.Major, v.Minor, v.Build, v.Revision);
            }
            else {
                var assemblyName = Assembly.GetExecutingAssembly().GetName();
                AppName = assemblyName.Name ?? "who am i?";
                AppVersion = assemblyName.Version ?? new Version(0, 0, 0, 0);
            }
            var title = $"{AppName} v{AppVersion}";

#if DEBUG
            title += " <debug>";
#endif
            this.Title = title;
            this.AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/program-icon.ico"));
        }

    }
}
