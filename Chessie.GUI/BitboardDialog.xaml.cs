using Chessie.GUI.ViewModels;
using System.Windows;

namespace Chessie.GUI
{
    /// <summary>
    /// Interaction logic for BitboardDialog.xaml
    /// </summary>
    public partial class BitboardDialog : Window
    {
        public BitboardViewModel ViewModel { get; }

        public BitboardDialog()
        {
            ViewModel = new BitboardViewModel();
            InitializeComponent();
        }

        public BitboardDialog(Board board)
        {
            ViewModel = new BitboardViewModel();
            ViewModel.Board = board;
            InitializeComponent();
        }
    }
}
