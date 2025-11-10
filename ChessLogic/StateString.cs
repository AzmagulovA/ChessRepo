using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ChessLogic
{
    public class StateString
    {
        private readonly StringBuilder sb = new StringBuilder();
        public StateString(Player currentPlayer,Board board,bool FromWhite) 
        {
            AddPiecePlacement(board, FromWhite);
            sb.Append(' ');
            AddCurrentPlayer(currentPlayer);
            sb.Append(' ');
            AddCastlingRights(board);
            sb.Append(' ');
            AddEnPassant(board,currentPlayer);

        }

        public override string ToString()
        {
            return sb.ToString();
        }
        private static char PieceChar(Piece piece)
        {
            char c = piece.Type switch
            {
                PieceType.Pawn => 'p',
                PieceType.Bishop => 'b',
                PieceType.Knight => 'n',
                PieceType.Rook => 'r',
                PieceType.Queen => 'q',
                PieceType.King => 'k',
                _ => ' '

            };
            if (piece.Color == Player.White)
            {
                return char.ToUpper(c);
            }
            return c;
        }
       
        private void AddRowData(Board board, int row, bool FromWhite)
        {
            int empty = 0;
            int helperc;
            row = FromWhite ? row : Math.Abs(row - 7);
            for (int c = 0; c < 8; c++)
            {
                helperc = FromWhite ? c : Math.Abs(c - 7);
                if (board[row, helperc] == null)
                {
                    empty++;
                    continue;
                }
                if (empty > 0)
                {
                    sb.Append(empty);
                    empty = 0;
                }
                sb.Append(PieceChar(board[row, helperc]));
                }
            if (empty > 0)
            {
                sb.Append(empty);
            }
           
        }

        private void AddPiecePlacement(Board board, bool FromWhite)
        {
                for (int r = 0; r < 8; r++)
                {
                    if (r != 0)
                    {
                        sb.Append('/');
                    }
                    AddRowData(board, r,FromWhite);
                }
        }
        private void AddCurrentPlayer(Player currentPlayer)
        {
            if (currentPlayer == Player.White)
            {
                sb.Append('w');
            }
            else
            {
                sb.Append('b');
            }
        }

        private void AddCastlingRights(Board board)
        {
            bool castleWKS = board.CastleRightKS(Player.White);
            bool castleWQS = board.CastleRightQS(Player.White);
            bool castleBKS = board.CastleRightKS(Player.Black);
            bool castleBQS = board.CastleRightQS(Player.Black);

            if (!(castleBKS||castleBQS||castleWKS||castleWQS))
            {
                sb.Append('-');
                return;
            }
            if (castleWKS)
            {
                sb.Append('K');
            }
            if (castleWQS)
            {
                sb.Append('Q');
            }
            if (castleBKS)
            {
                sb.Append('k');
            }
            if (castleBQS)
            {
                sb.Append('q');
            }
        }

        private void AddEnPassant(Board board, Player currentPlayer)
        {
            if (!board.CanCaptureEnPassant(currentPlayer))
            {
                sb.Append('-');
                return;
            }
            Position pos = board.GetPawnSkipPosition(currentPlayer.Opponent());
            char file = (char)('a' + pos.Column);
            int rank = 8 - pos.Row;
            sb.Append(file);
            sb.Append(rank);
        }
    }
}
