using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chessie.Model
{
    public class NotationProcessor
    {
        public static BoardState CreateBoardFromFEN(string fenCode)
        {
            string[] fields = fenCode.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var board = new BoardState();

            // piece placement
            int rankIdx = SquareCoord.MAX_RANK;
            int fileIdx = SquareCoord.MIN_FILE;
            foreach (char placement in fields[0])
            {
                // empty squares
                if (placement >= '1' && placement <= '8')
                {
                    for (int emptyCount = (placement - '0'); emptyCount > 0; emptyCount--)
                    {
                        board[rankIdx, fileIdx] = PieceType.Empty;
                        fileIdx++;
                    }
                    continue;
                }

                // next rank
                if (placement == '/')
                {
                    rankIdx--;
                    fileIdx = SquareCoord.MIN_FILE;
                    continue;
                }

                PieceType piece = PieceType.Empty;
                char pieceType = placement;

                if (char.IsLower(placement))
                {
                    piece |= PieceType.Black;
                    pieceType = char.ToUpper(placement);
                }
                else
                {
                    piece |= PieceType.White;
                }

                piece |= pieceType switch
                {
                    'P' => PieceType.Pawn,
                    'N' => PieceType.Knight,
                    'B' => PieceType.Bishop,
                    'R' => PieceType.Rook,
                    'Q' => PieceType.Queen,
                    'K' => PieceType.King,
                    _ => throw new ArgumentException($"Invalid FEN placement: {placement}"),
                };

                board[rankIdx, fileIdx] = piece;
                fileIdx++;
            }

            // next player
            if (fields[1] == "b")
            {
                board.BlackToMove = true;
            }
            else if (fields[1] != "w")
            {
                throw new ArgumentException($"Invalid player to move next: {fields[1]}");
            }

            // castling
            var castling = CastleState.None;
            if (fields[2] != "-")
            {
                foreach (char c in fields[2])
                {
                    castling |= c switch
                    {
                        'K' => CastleState.WhiteKingside,
                        'Q' => CastleState.WhiteQueenside,
                        'k' => CastleState.BlackKingside,
                        'q' => CastleState.BlackQueenside,
                        _ => throw new ArgumentException($"Invalid castle availability: {fields[2]}"),
                    };
                }
            }
            
            // en passant square
            if (fields[3] != "-")
            {
                if (fields[3].Length != 2 || !char.IsLetter(fields[3][0]) || !char.IsDigit(fields[3][1]))
                {
                    throw new ArgumentException($"Invalid en passant square notation: {fields[3]}");
                }

                int fileIndex = fields[3][0] - 'a';
                int rankIndex = fields[3][1] - '1';

                board.EnPassantSquare = new SquareCoord(rankIndex, fileIndex);
            }

            // halfmoves

            // move number
            board.MoveNumber = int.Parse(fields[5]);

            return board;
        }
    }
}
