using Chessie.Core.Model;
using System.Windows;

namespace Chessie.ViewModels
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

            RefreshBoardState();
        }

        private void OnSelectedPieceChanged()
        {
            RefreshHighlights();
        }

        private void OnBoardUpdated()
        {
            RefreshBoardState();
        }

        private void RefreshBoardState()
        {
            for (int mapIndex = 0; mapIndex < N_SQUARES; mapIndex++)
            {
                int rank = mapIndex / N_FILES;
                int file = mapIndex % N_FILES;

                var squareState = _game.Board[mapIndex];
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
                    int coord = rank * 8 + file;

                    if (_game.HumanMoves.Count > 0 && _game.HumanMoves.ContainsKey(coord))
                    {
                        currentSquare.IsValidTarget = true;
                    }
                    else
                    {
                        currentSquare.IsValidTarget = false;
                    }

                    currentSquare.IsSelected = coord == _game.SelectedPiece;
                    currentSquare.IsInCheck = coord == _game.CheckLocation;
                }
            }
        }
    }
}
