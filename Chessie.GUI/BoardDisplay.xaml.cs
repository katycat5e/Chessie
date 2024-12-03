using Chessie.Core.Model;
using Chessie.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Chessie
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
