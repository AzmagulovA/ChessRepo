using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessLogic
{
    public class King:Piece
    {
        public override PieceType Type => PieceType.King;
        public override Player Color { get; set; }
        private static readonly Direction[] dirs = new Direction[]
        {
            Direction.North,
            Direction.West,
            Direction.South,
            Direction.East,
            Direction.NorthEast,
            Direction.NorthWest,
            Direction.SouthWest,
            Direction.SouthEast
        };
        public King(Player color,  bool hasMoved = false)
        {
            Color = color;
            HasMoved = hasMoved;

        }

        private static bool AllEmpty(IEnumerable<Position> positions,Board board)
        {
            return positions.All(pos=> board.IsEmpty(pos));
        }

        private bool CanCastleKingSide(Position from, Board board)
        {
            if (HasMoved) return false;
            // Короткая рокировка: для белых это h1 (7,7), для черных h8 (0,7)
            Position rookPos = new Position(from.Row, 7);
            var between = new[] { new Position(from.Row, 5), new Position(from.Row, 6) };
            return IsUnMovedRook(rookPos, board) && between.All(board.IsEmpty);
        }

        private bool CanCastleQueenSide(Position from, Board board)
        {
            if (HasMoved) return false;
            // Длинная рокировка: для белых a1 (7,0), для черных a8 (0,0)
            Position rookPos = new Position(from.Row, 0);
            var between = new[] { new Position(from.Row, 1), new Position(from.Row, 2), new Position(from.Row, 3) };
            return IsUnMovedRook(rookPos, board) && between.All(board.IsEmpty);
        }

        private static bool IsUnMovedRook(Position pos, Board board)
        {
            Piece p = board[pos];
            return p != null && p.Type == PieceType.Rook && !p.HasMoved;
        }

        public override Piece Copy()
        {
            return new King(Color, HasMoved);
        }
        private IEnumerable<Position> MovePositions(Position from,Board board)
        {
            foreach (Direction dir in dirs)
            {
                Position to = from + dir;
                if (!Board.IsInside(to))//если позиция выходит за пределы доски, то сразу к следующему
                {
                    continue;
                }
                if (board.IsEmpty(to) || board[to].Color!=Color)
                {
                    yield return to;
                }
            }
        }
        public override IEnumerable<Move> GetMoves(Position from, Board board)
        {
            foreach (Position to in MovePositions(from,board))
            {
                yield return new NormalMove(from, to);
            }
            if (CanCastleKingSide(from,board))
            {
                yield return new Castle(MoveType.CastleKS, from);
            }
            if (CanCastleQueenSide(from,board))
            {
                yield return new Castle(MoveType.CastleQS, from);
            }

        }
        public override bool CanCaptureOpponentKing(Position from, Board board)
        {
            return MovePositions(from, board).Any(to =>
            {
                Piece piece = board[to];
                return piece != null && piece.Type == PieceType.King;
            });
        }
    }
}
