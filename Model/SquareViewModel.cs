using System.Windows;

namespace Chessie.Model
{
    public class SquareViewModel : DependencyObject
    {
        public bool IsDarkSquare { get; }
        public string SquareId { get; } = "??";

        public int Rank;
        public int File;

        public SquareViewModel() { }

        public SquareViewModel(int rank, int file)
        {
            Rank = rank;
            File = file;
            IsDarkSquare = (rank + file) % 2 == 0;
            SquareId = $"{(char)('A' + file)}{rank + 1}";
        }

        public void SetState(PieceType newState)
        {
            PieceSymbol = GetSymbolForPiece(newState);
        }

        private static string GetSymbolForPiece(PieceType state)
        {
            if (state == PieceType.Empty) return string.Empty;

            if (state.HasFlag(PieceType.White))
            {
                return (state & PieceType.PieceMask) switch
                {
                    PieceType.Pawn => "♙",
                    PieceType.Knight => "♘",
                    PieceType.Bishop => "♗",
                    PieceType.Rook => "♖",
                    PieceType.Queen => "♕",
                    PieceType.King => "♔",
                    _ => throw new NotImplementedException()
                };
            }
            else if (state.HasFlag(PieceType.Black))
            {
                return (state & PieceType.PieceMask) switch
                {
                    PieceType.Pawn => "♟",
                    PieceType.Knight => "♞",
                    PieceType.Bishop => "♝",
                    PieceType.Rook => "♜",
                    PieceType.Queen => "♛",
                    PieceType.King => "♚",
                    _ => throw new NotImplementedException()
                };
            }

            return "?";
        }

        public string PieceSymbol
        {
            get { return (string)GetValue(PieceSymbolProperty); }
            set { SetValue(PieceSymbolProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PieceSymbol.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PieceSymbolProperty =
            DependencyProperty.Register("PieceSymbol", typeof(string), typeof(SquareViewModel), new PropertyMetadata("♙"));


        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsSelected.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register("IsSelected", typeof(bool), typeof(SquareViewModel), new PropertyMetadata(false));


        public bool IsValidTarget
        {
            get { return (bool)GetValue(IsValidTargetProperty); }
            set { SetValue(IsValidTargetProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsValidTarget.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsValidTargetProperty =
            DependencyProperty.Register("IsValidTarget", typeof(bool), typeof(SquareViewModel), new PropertyMetadata(false));


        public bool IsInCheck
        {
            get { return (bool)GetValue(IsInCheckProperty); }
            set { SetValue(IsInCheckProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsInCheck.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsInCheckProperty =
            DependencyProperty.Register("IsInCheck", typeof(bool), typeof(SquareViewModel), new PropertyMetadata(false));
    }
}
