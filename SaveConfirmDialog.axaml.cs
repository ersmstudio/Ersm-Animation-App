using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Threading.Tasks;

namespace Ersm_Animation_App
{
    public enum SaveConfirmResult
    {
        Save,
        DontSave,
        Cancel
    }

    public partial class SaveConfirmDialog : Window
    {
        public SaveConfirmResult Result { get; private set; } = SaveConfirmResult.Cancel;

        public SaveConfirmDialog()
        {
            InitializeComponent();
        }

        private void BtnSave_Click(object? sender, RoutedEventArgs e)
        {
            Result = SaveConfirmResult.Save;
            Close(Result);
        }

        private void BtnDontSave_Click(object? sender, RoutedEventArgs e)
        {
            Result = SaveConfirmResult.DontSave;
            Close(Result);
        }

        private void BtnCancel_Click(object? sender, RoutedEventArgs e)
        {
            Result = SaveConfirmResult.Cancel;
            Close(Result);
        }

        public static async Task<SaveConfirmResult> ShowDialog(Window owner)
        {
            var dialog = new SaveConfirmDialog();
            return await dialog.ShowDialog<SaveConfirmResult>(owner);
        }
    }
}
