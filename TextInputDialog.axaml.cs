using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Ersm_Animation_App
{
    public partial class TextInputDialog : Window
    {
        public string ResultText { get; private set; } = string.Empty;

        public TextInputDialog()
        {
            InitializeComponent();
            InputTextBox.AttachedToVisualTree += (s, e) => InputTextBox.Focus();
        }

        private void BtnOk_Click(object? sender, RoutedEventArgs e)
        {
            ResultText = InputTextBox.Text ?? string.Empty;
            Close(ResultText);
        }

        private void BtnCancel_Click(object? sender, RoutedEventArgs e)
        {
            Close(null);
        }
    }
}
