using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessLogic
{
    public class King:Piece
    {
        public override PieceType Type => PieceType.King;//override - переназначенный тип базового класса
        public bool WatchFromWhite = true;//изначально вид со стороны белых
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
        public King(Player color, bool watchFromWhite)
        {
            Color = color;
            WatchFromWhite = watchFromWhite;

        }
        public King(Player color, bool watchFromWhite, bool hasMoved)
        {
            Color = color;
            WatchFromWhite = watchFromWhite;
            HasMoved = hasMoved;

        }
        private static bool IsUnMovedRook(Position pos,Board board)
        {
            if (board.IsEmpty(pos))
            {
                return false;
            }
            Piece piece = board[pos];
            return piece.Type == PieceType.Rook && !piece.HasMoved;
        }

        private static bool AllEmpty(IEnumerable<Position> positions,Board board)
        {
            return positions.All(pos=> board.IsEmpty(pos));
        }
        private bool CanCastleKingSide(Position from,Board board)
        {
            if (HasMoved)
            {
                return false;
            }
            Position rookPos;
            Position[] beetweePositions;
            if (WatchFromWhite) {
                rookPos = new Position(from.Row, 7);
                beetweePositions = new Position[] { new(from.Row, 5), new(from.Row, 6) }; 
            }
            else
            {
                rookPos = new Position(from.Row, 0);
                beetweePositions = new Position[] { new(from.Row, 1), new(from.Row, 2) };
            }

            return IsUnMovedRook(rookPos,board)&&AllEmpty(beetweePositions,board);
        }
        private bool CanCastleQueenSide(Position from, Board board)
        {
            if (HasMoved)
            {
                return false;
            }
            Position rookPos;
            Position[] betweenPositions;
            if (WatchFromWhite)
            {
                rookPos = new Position(from.Row, 0);
                betweenPositions = new Position[] { new(from.Row, 1), new(from.Row, 2), new(from.Row, 3) };
            }
            else
            {
                rookPos = new Position(from.Row, 7);
                betweenPositions = new Position[] { new(from.Row, 4), new(from.Row, 5), new(from.Row, 6) };
            }
            

            return IsUnMovedRook(rookPos,board)&&AllEmpty(betweenPositions,board);
        }
        public override Piece Copy()
        {
            King copy = new King(Color,WatchFromWhite);
            copy.HasMoved = HasMoved;
            return copy;
        }
        public override Piece ReversCopy()
        {
            King copy = new King(Color, !WatchFromWhite);
            copy.HasMoved = HasMoved;
            return copy;
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
                yield return new Castle(MoveType.CastleKS, from,WatchFromWhite);
            }
            if (CanCastleQueenSide(from,board))
            {
                yield return new Castle(MoveType.CastleQS, from, WatchFromWhite);
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
