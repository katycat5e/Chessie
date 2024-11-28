using System.ComponentModel;
using System.Windows;

namespace Chessie.Model
{
    public class BoardViewModel : DependencyObject
    {
        const int N_RANKS = 8;
        const int N_FILES = 8;
        const int N_SQUARES = N_RANKS * N_FILES;

        private readonly GameManager _game;

        public List<List<SquareViewModel>> Squares { get; }

        //dummy
        public BoardViewModel()
        {
            _game = null!;
            Squares = new List<List<SquareViewModel>>(N_RANKS);
            bool isDarkSquare = true;

            for (int i = 0; i < N_RANKS; i++)
            {
                Squares.Add(new List<SquareViewModel>(N_FILES));

                char file = 'A';
                for (int j = 0; j < N_FILES; j++)
                {
                    Squares[i].Add(new SquareViewModel(i, j));

                    isDarkSquare = !isDarkSquare;
                    file++;
                }

                isDarkSquare = !isDarkSquare;
            }
        }

        public BoardViewModel(GameManager game) : this()
        {
            _game = game;
            _game.SelectedPieceChanged += OnSelectedPieceChanged;
            _game.BoardUpdated += OnBoardUpdated;

            SetBoardState(_game.CurrentState);
        }

        private void OnSelectedPieceChanged(SquareCoord? selection)
        {
            RefreshHighlights();
        }

        private void OnBoardUpdated()
        {
            SetBoardState(_game.CurrentState);
        }

        private void SetBoardState(BoardState newState)
        {
            for (int mapIndex = 0; mapIndex < N_SQUARES; mapIndex++)
            {
                int rank = mapIndex / N_FILES;
                int file = mapIndex % N_FILES;

                var squareState = newState.Squares[mapIndex];
                Squares[rank][file].SetState(squareState);
            }
        }

        private void RefreshHighlights()
        {
            for (int rank = SquareCoord.MIN_RANK; rank <= SquareCoord.MAX_RANK; rank++)
            {
                for (int file = SquareCoord.MIN_FILE; file <= SquareCoord.MAX_FILE; file++)
                {
                    var currentSquare = Squares[rank][file];
                    var coord = new SquareCoord(rank, file);

                    if ((_game.HumanMoves.Count > 0) && _game.HumanMoves.ContainsKey(coord))
                    {
                        currentSquare.IsValidTarget = true;
                    }
                    else
                    {
                        currentSquare.IsValidTarget = false;
                    }

                    currentSquare.IsSelected = (coord == _game.SelectedPiece);
                    currentSquare.IsInCheck = (_game.CheckLocation.HasValue && (coord == _game.CheckLocation));
                }
            }
        }
    }
}
