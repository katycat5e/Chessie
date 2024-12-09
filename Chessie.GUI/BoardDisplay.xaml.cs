using Chessie.GUI.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Chessie.GUI
{
    /// <summary>
    /// Interaction logic for BoardDisplay.xaml
    /// </summary>
    public partial class BoardDisplay : UserControl
    {
        public event EventHandler<SquareActionEventArgs>? SquareMousedown;
        public event EventHandler<SquareActionEventArgs>? SquareDrop;

        public BoardDisplay()
        {
            InitializeComponent();
        }

        private void SquareDisplay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var square = (SquareViewModel)((Control)sender).DataContext;
            SquareMousedown?.Invoke(this, new SquareActionEventArgs(square.Rank, square.File));
        }

        private void SquareDisplay_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var square = (SquareDisplay)sender;

                var data = new DataObject(DataFormats.Text, ((SquareViewModel)square.DataContext).PieceSymbol);
                DragDrop.DoDragDrop(square, data, DragDropEffects.Move);
            }
        }

        private void SquareDisplay_Drop(object sender, DragEventArgs e)
        {
            var square = (SquareViewModel)((Control)sender).DataContext;
            SquareDrop?.Invoke(this, new SquareActionEventArgs(square.Rank, square.File));
        }
    }

    public class SquareActionEventArgs : EventArgs
    {
        public readonly int Rank;
        public readonly int File;

        public SquareCoord Coordinate => new(Rank, File);

        public SquareActionEventArgs(int rank, int file)
        {
            Rank = rank;
            File = file;
        }
    }
}
