using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace HalstedMetricsCounter
{
    public partial class MainWindow : Window
    {
        string _path = null;

        public MainWindow() { InitializeComponent(); }

        void Choose_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Выберите файл с исходным кодом",
                Filter = "Rust files (*.rs)|*.rs|Все файлы (*.*)|*.*",
                CheckFileExists = true
            };
            if (dlg.ShowDialog() == true)
            {
                _path = dlg.FileName;
                FilePathBox.Text = _path;
                AnalyzeButton.IsEnabled = true;
                StatusText.Text = "Файл выбран. Нажмите «Анализировать».";
            }
        }

        void Analyze_Click(object sender, RoutedEventArgs e)
        {
            if (_path == null || !File.Exists(_path))
            {
                StatusText.Text = "Файл не найден.";
                return;
            }
            try
            {
                string code = File.ReadAllText(_path);
                var result = HalsteadAnalyzer.Analyze(code);
                var rows = HalsteadAnalyzer.BuildRows(result);
                var win = new ResultsWindow(result, rows);
                win.Owner = this;
                win.Show();
                Hide();
            }
            catch (Exception ex)
            {
                StatusText.Text = "Ошибка: " + ex.Message;
            }
        }

        public void ComeBack()
        {
            Show();
            Activate();
        }
    }
}