using System.Windows;
using System.Windows.Controls;

namespace shop_pc.window
{
    public partial class ReceiptWindow : Window
    {
        public ReceiptWindow(string text)
        {
            InitializeComponent();
            ReceiptText.Text = text;
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new PrintDialog();
            if (dialog.ShowDialog() == true)
            {
                dialog.PrintVisual(ReceiptText, "Чек");
            }
        }
    }
}