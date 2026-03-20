using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessLogic
{
    public class Pawn : Piece
    {
        public override PieceType Type => PieceType.Pawn;//override - переназначенный тип базового класса
        public override Player Color { get; set; }

        private readonly Direction moveDirection;

        public Pawn(Player color, bool hasMoved = false)
        {
            Color = color;
            HasMoved = hasMoved;
            if (GameState.WatchFromWhite == color is Player.White)
                moveDirection = Direction.North;
            else
                moveDirection = Direction.South;
        }
        public override Piece Copy()
        {
            return new Pawn(Color, HasMoved);
        }
        private static bool CanMoveTo(Position pos, Board board)
        {
            return Board.IsInside(pos) && board.IsEmpty(pos);//метод на движение вперед пешки(можно идти только если никого нет)
        }
        private bool CanCaptureAt(Position pos,Board board)
        {
            if (!Board.IsInside(pos)||board.IsEmpty(pos))
            {
                return false;
            }
            return board[pos].Color != Color;
        }

        private static IEnumerable<Move> PromotionMoves(Position from,Position to)
        {
            yield return new PawnPromotion(from,to,PieceType.Knight);
            yield return new PawnPromotion(from, to, PieceType.Rook);
            yield return new PawnPromotion(from, to, PieceType.Bishop);
            yield return new PawnPromotion(from, to, PieceType.Queen);

        }

        private IEnumerable<Move> ForwardMoves(Position from,Board board)
        {
            Position oneMovePos = from + moveDirection;
            if (CanMoveTo(oneMovePos,board))
            {
                if(oneMovePos.IsPromotionRow())//если это клетки преращения
                {
                    foreach (Move promMove in PromotionMoves(from, oneMovePos))
                    {
                        yield return promMove;
                    }
                }
                else
                {
                    yield return new NormalMove(from, oneMovePos);
                }                
                Position twoMovePos = oneMovePos + moveDirection;
                if(!HasMoved && CanMoveTo(twoMovePos, board))//двойной ход только если до этого не двигалась и при этом можо сходить на 2 клетки
                {
                    yield return new DoublePawn(from,twoMovePos);
                }

            }
        }
        private IEnumerable<Move> DiagonalMoves(Position from,Board board)
        {
            foreach(Direction dir in new Direction[] {Direction.West,Direction.East })
            {
                Position to = from + moveDirection + dir;

                if (board.GetPawnSkipPosition(Color.Opponent()) is Position skipPos && to == skipPos)
                {
                    yield return new EnPassant(from, to);

                }
                else if (CanCaptureAt(to, board))//если можно бить по диагонали
                {
                    if (to.IsPromotionRow())//если это клетки преращения
                    {
                        foreach (Move promMove in PromotionMoves(from, to))
                        {
                            yield return promMove;
                        }
                    }
                    else
                    {
                        yield return new NormalMove(from, to);
                    }
                }
            }
        }
        public override IEnumerable<Move> GetMoves(Position from,Board board)
        {
            IEnumerable<Move> forwardMoves = ForwardMoves(from, board);
            IEnumerable<Move> diagonalMoves = DiagonalMoves(from, board);
            return forwardMoves.Concat(diagonalMoves);
            //return ForwardMoves(from, board).Concat(DiagonalMoves(from,board));//все ходы пешки, это передние ходы + диагональные

        }

        public override bool CanCaptureOpponentKing(Position from, Board board)
        {
            return DiagonalMoves(from, board).Any(move => 
            {
                Piece piece = board[move.ToPos];
                return piece!=null && piece.Type == PieceType.King;
            }) ;
        }
    }
}
