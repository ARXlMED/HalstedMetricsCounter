using System.Collections.Generic;
using System.Windows;

namespace HalstedMetricsCounter
{
    public partial class ResultsWindow : Window
    {
        public ResultsWindow(HalsteadAnalyzer.Result result,
                             List<HalsteadRow> rows)
        {
            InitializeComponent();
            ResultGrid.ItemsSource = rows;

            SummaryText.Text =
                $"Словарь программы: η = η1 + η2 = {result.Eta1} + {result.Eta2} = {result.Eta}\n" +
                $"Длина программы:   N = N1 + N2 = {result.N1} + {result.N2} = {result.N}\n" +
                $"Объем программы:   V = N · log2 η = {result.N} · log2 {result.Eta} = {(int)result.V}";

            Closing += (s, e) =>
            {
                if (Owner is MainWindow mw && !mw.IsVisible) mw.ComeBack();
            };
        }

        void Back_Click(object sender, RoutedEventArgs e)
        {
            if (Owner is MainWindow mw) mw.ComeBack();
            Close();
        }
    }
}