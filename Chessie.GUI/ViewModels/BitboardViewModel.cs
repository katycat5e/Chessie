using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows;

namespace Chessie.GUI.ViewModels
{
    public class BitboardViewModel : INotifyPropertyChanged
    {
        const int N_RANKS = 8;
        const int N_FILES = 8;

        public event PropertyChangedEventHandler? PropertyChanged;

        private Board _board;
        public Board Board
        {
            get => _board;

            [MemberNotNull(nameof(_board))]
            set
            {
                _board = value;
                _board.StateChanged += OnBoardStateChanged;
                OnBoardStateChanged();
                NotifyArraysChanged();
            }
        }

        public BitboardViewModel()
        {
            Board = new Board();
        }

        public BitboardFilesModel WhitePieces { get; } = new();
        public BitboardFilesModel WhiteThreats { get; } = new();
        public BitboardFilesModel WhiteKingThreats { get; } = new();

        public BitboardFilesModel BlackPieces { get; } = new();
        public BitboardFilesModel BlackThreats { get; } = new();
        public BitboardFilesModel BlackKingThreats { get; } = new();

        private void OnBoardStateChanged()
        {
            var whiteBitboards = Board.GetBitboards(false);

            UnpackBitboard(WhitePieces, Board.WhitePieces.PieceBitboard);
            UnpackBitboard(WhiteThreats, whiteBitboards.Threats);
            UnpackBitboard(WhiteKingThreats, whiteBitboards.KingThreats);

            var blackBitboards = Board.GetBitboards(true);

            UnpackBitboard(BlackPieces, Board.BlackPieces.PieceBitboard);
            UnpackBitboard(BlackThreats, blackBitboards.Threats);
            UnpackBitboard(BlackKingThreats, blackBitboards.KingThreats);

            NotifyArraysChanged();
        }

        private static void UnpackBitboard(BitboardFilesModel squares, ulong bitboard)
        {
            ulong mask = 1;

            for (int rank = 0; rank < squares.Count; rank++)
            {
                for (int file = 0; file < squares[rank].Count; file++)
                {
                    squares[rank][file].FlagSet = (bitboard & mask) != 0;
                    mask <<= 1;
                }
            }
        }

        private void NotifyArraysChanged()
        {
            Notify(nameof(WhitePieces));
            Notify(nameof(WhiteThreats));
            Notify(nameof(WhiteKingThreats));

            Notify(nameof(BlackPieces));
            Notify(nameof(BlackThreats));
            Notify(nameof(BlackKingThreats));
        }

        private void Notify(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        
    }

    public sealed class BitboardCellModel : DependencyObject
    {
        public string CellId { get; }

        public BitboardCellModel()
        {
            CellId = "??";
        }

        public BitboardCellModel(string cellId, bool flagSet = false)
        {
            CellId = cellId;
            FlagSet = flagSet;
        }

        public bool FlagSet
        {
            get { return (bool)GetValue(FlagSetProperty); }
            set { SetValue(FlagSetProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FlagSet.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FlagSetProperty =
            DependencyProperty.Register("FlagSet", typeof(bool), typeof(BitboardCellModel), new PropertyMetadata(false));
    }

    public sealed class BitboardRankModel : ObservableCollection<BitboardCellModel>
    {
        public BitboardRankModel(int rankIndex)
        {
            char rankId = (char)('1' + rankIndex);

            for (int file = 0; file < 8; file++)
            {
                char fileId = (char)('a' + file);
                Add(new BitboardCellModel($"{fileId}{rankId}"));
            }
        }
    }

    public sealed class BitboardFilesModel : ObservableCollection<BitboardRankModel>
    {
        public BitboardFilesModel()
        {
            for (int rank = 0; rank < 8; rank++)
            {
                var rankArr = new BitboardRankModel(rank);
                Add(rankArr);
            }
        }
    }
}
