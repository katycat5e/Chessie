using System.ComponentModel;

namespace Chessie.Model
{
    using MoveDictionary = Dictionary<SquareCoord, Move>;

    public class GameManager : INotifyPropertyChanged
    {
        public event Action<SquareCoord?>? SelectedPieceChanged;
        public event Action? BoardUpdated;
        public event PropertyChangedEventHandler? PropertyChanged;

        public BoardState CurrentState { get; private set; }
        public string TurnIndicator
        {
            get
            {
                if (CheckMate)
                {
                    return CurrentState.BlackToMove ? "White Wins by Checkmate" : "Black Wins by Checkmate";
                }
                return CurrentState.BlackToMove ? "Black to Move" : "White to Move";
            }
        }

        private SquareCoord? _selectedPiece;
        public SquareCoord? SelectedPiece
        {
            get => _selectedPiece;
            set
            {
                if (value == _selectedPiece) return;
                _selectedPiece = value;

                AvailableMoves.Clear();
                if (value != null)
                {
                    foreach (var move in BoardCalculator.GetValidMovesForPiece(CurrentState, value.Value))
                    {
                        AvailableMoves.Add(move.End, move);
                    }
                }

                SelectedPieceChanged?.Invoke(value);
            }
        }

        public SquareCoord? CheckLocation { get; private set; }
        public bool CheckMate { get; private set; }

        public MoveDictionary AvailableMoves { get; } = new MoveDictionary();

        public ObservableStack<MoveRecord> PreviousMoves { get; } = new ObservableStack<MoveRecord>();
        
        public GameManager()
        {
            StartNewGame();
        }

        private static readonly PieceType[] START_STATE =
        {
            Piece.R, Piece.N, Piece.B, Piece.Q, Piece.K, Piece.B, Piece.N, Piece.R,
            Piece.P, Piece.P, Piece.P, Piece.P, Piece.P, Piece.P, Piece.P, Piece.P,

            Piece.X, Piece.X, Piece.X, Piece.X, Piece.X, Piece.X, Piece.X, Piece.X,
            Piece.X, Piece.X, Piece.X, Piece.X, Piece.X, Piece.X, Piece.X, Piece.X,
            Piece.X, Piece.X, Piece.X, Piece.X, Piece.X, Piece.X, Piece.X, Piece.X,
            Piece.X, Piece.X, Piece.X, Piece.X, Piece.X, Piece.X, Piece.X, Piece.X,

            Piece.p, Piece.p, Piece.p, Piece.p, Piece.p, Piece.p, Piece.p, Piece.p,
            Piece.r, Piece.n, Piece.b, Piece.q, Piece.k, Piece.b, Piece.n, Piece.r,
        };

        public void StartNewGame()
        {
            CurrentState = new BoardState(START_STATE)
            {
                CastleState = CastleState.All,
            };
            SelectedPiece = null;
            CheckLocation = null;
            CheckMate = false;
            PreviousMoves.Clear();

            NotifyBoardChanged();
            SelectedPieceChanged?.Invoke(null);
        }

        public void ApplyGameState(BoardState newState)
        {
            CurrentState = newState;
            SelectedPiece = null;
            PreviousMoves.Clear();

            if (BoardCalculator.IsCheck(CurrentState, CurrentState.BlackToMove, out var checkedKing))
            {
                CheckLocation = checkedKing;

                if (BoardCalculator.IsMate(CurrentState, CurrentState.BlackToMove))
                {
                    CheckMate = true;
                }
            }
            else
            {
                CheckLocation = null;
            }

            NotifyBoardChanged();
            SelectedPieceChanged?.Invoke(null);
        }

        public bool IsMovablePiece(SquareCoord square)
        {
            var squareState = CurrentState[square];

            return (squareState != PieceType.Empty) && (squareState.IsBlackPiece() == CurrentState.BlackToMove);
        }

        public static bool IsPromotion(PieceType piece, SquareCoord destination)
        {
            return piece.IsPieceType(PieceType.Pawn) && (destination.IsInLastRank || destination.IsInFirstRank);
        }

        public void MakeMove(Move move, PieceType? promotion = null)
        {
            var previousState = CurrentState;
            CurrentState = CurrentState.ApplyMove(move, promotion);

            bool isCheck, isMate = false;
            if (isCheck = BoardCalculator.IsCheck(CurrentState, CurrentState.BlackToMove, out var checkedKing))
            {
                CheckLocation = checkedKing;

                if (isMate = BoardCalculator.IsMate(CurrentState, CurrentState.BlackToMove))
                {
                    CheckMate = true;
                }
            }
            else
            {
                CheckLocation = null;
            }

            var record = new MoveRecord(previousState, move, AvailableMoves, promotion, isCheck, isMate);
            PreviousMoves.Push(record);

            NotifyBoardChanged();
        }

        private void NotifyBoardChanged()
        {
            BoardUpdated?.Invoke();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TurnIndicator)));
        }
    }
}
