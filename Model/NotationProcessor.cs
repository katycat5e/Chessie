using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chessie.Model
{
    public class NotationProcessor
    {
        public static Board CreateBoardFromFEN(string fenCode)
        {
            string[] fields = fenCode.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var squares = new PieceType[64];

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
                        squares[rankIdx * 8 + fileIdx] = PieceType.Empty;
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

                squares[rankIdx * 8 + fileIdx] = piece;
                fileIdx++;
            }

            // next player
            bool blackToMove = false;
            if (fields[1] == "b")
            {
                blackToMove = true;
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
            int? enPassantIndex = null;
            if (fields[3] != "-")
            {
                if (fields[3].Length != 2 || !char.IsLetter(fields[3][0]) || !char.IsDigit(fields[3][1]))
                {
                    throw new ArgumentException($"Invalid en passant square notation: {fields[3]}");
                }

                int fileIndex = fields[3][0] - 'a';
                int rankIndex = fields[3][1] - '1';

                enPassantIndex = rankIndex * 8 + fileIndex;
            }

            // halfmoves

            // move number
            int moveNumber = int.Parse(fields[5]);
            int plyNumber = (moveNumber * 2) + (blackToMove ? 1 : 0);

            return new Board(squares, castling, plyNumber, enPassantIndex, blackToMove);
        }
    }
}
