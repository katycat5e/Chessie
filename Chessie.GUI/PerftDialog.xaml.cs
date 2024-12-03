using Chessie.Core.Model;
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
using System.Windows.Shapes;

namespace Chessie
{
    /// <summary>
    /// Interaction logic for PerftDialog.xaml
    /// </summary>
    public partial class PerftDialog : Window
    {
        public Board? Board;

        public PerftDialog()
        {
            InitializeComponent();
        }

        private void RunPerft_Click(object sender, RoutedEventArgs e)
        {
            int depth = (int)DepthSelect.Value;
            ulong result = Perft(Board!, depth);
            NodeDisplay.Content = result;
        }

        private static ulong Perft(Board board, int depth)
        {
            if (depth == 0) return 1;

            ulong nodeCount = 0;

            var moves = BoardCalculator.GetAllValidMoves(board, board.BlackToMove).ToList();
            foreach (var move in moves )
            {
                board.ApplyMove(move);
                nodeCount += Perft(board, depth - 1);
                board.UndoLastMove();
            }

            return nodeCount;
        }
    }
}
