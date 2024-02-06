using Microsoft.UI.Xaml;
using PathEdit.common;
using System;
using System.Diagnostics;
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
            Version? AppVersion = null;
            this.InitializeComponent();
            if (RuntimeHelper.IsMSIX) {
                var package = Windows.ApplicationModel.Package.Current;
                AppName = package.DisplayName;
                var v = package.Id.Version;
                AppVersion = new(v.Major, v.Minor, v.Build, v.Revision);
            }
            else {
                AppName = "SVG Path Editor (u)";
            }
            var assembly = Assembly.GetExecutingAssembly();
            var assemblyName = assembly.GetName();
            var assemblyVersion = assemblyName.Version ?? new Version(0, 0, 0, 0);
            if (AppVersion == null) {
                AppVersion = assemblyVersion;
            } else {
                // プロジェクトプロパティ(csproj)の「アセンブリバージョン」と、Package.manifestの <Identity ... Version> の値は、
                // 常に同じ値に設定しておくこと。
                Debug.Assert(AppVersion == assemblyVersion);
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
