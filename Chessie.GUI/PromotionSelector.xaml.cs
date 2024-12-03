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
    /// Interaction logic for PromotionSelector.xaml
    /// </summary>
    public partial class PromotionSelector : Window
    {
        public PieceType SelectedPromotion { get; private set; } = PieceType.Queen;

        public PromotionSelector()
        {
            InitializeComponent();
        }

        public static PieceType PromptPromotion(Window owner, PieceType color)
        {
            var dialog = new PromotionSelector()
            {
                Owner = owner,
            };

            dialog.ShowDialog();

            return dialog.SelectedPromotion | color;
        }

        private void QueenButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedPromotion = PieceType.Queen;
            Close();
        }

        private void RookButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedPromotion = PieceType.Rook;
            Close();
        }

        private void BishopButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedPromotion = PieceType.Bishop;
            Close();
        }

        private void KnightButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedPromotion = PieceType.Knight;
            Close();
        }
    }
}
