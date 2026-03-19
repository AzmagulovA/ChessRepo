using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessLogic
{
    public abstract class Piece//аbstract - неполный класс, предназначенный для обозначения базового класаа для других классов
    {
        public abstract PieceType Type { get; }
        public abstract Player Color { get; set; }//цвет
        public bool HasMoved { get; set; } = false;// 

        public abstract Piece Copy();

        public abstract IEnumerable<Move> GetMoves(Position from, Board board);

        protected IEnumerable<Position> MovePositionInDir(Position from,Board board,Direction dir)//проверка достижимости клетки фигурой
        {
            for(Position pos = from + dir; Board.IsInside(pos); pos += dir)
            {
                if (board.IsEmpty(pos))//если клетка пуста в каком-то допустимом напрвлении, то она достижима
                {
                    yield return pos;
                    continue;
                }
                Piece piece = board[pos];//иначе на ней кто-то стоит и мы проверяем ее на цвет
                if (piece.Color != Color)
                {
                    yield return pos;//и если цвет чужой, то мы можем поразить эту клетку
                }
                yield break;
            }
        }
        protected IEnumerable<Position> MovePositionInDirs(Position from,Board board, Direction[] directions)//все возможные ходы в заданных направлениях
        {
            return directions.SelectMany(dir => MovePositionInDir(from,board,dir));
        }
        public virtual bool CanCaptureOpponentKing(Position from,Board board)
        {
            return GetMoves(from, board).Any(move =>
            {
                Piece piece = board[move.ToPos];
                return piece!=null && piece.Type == PieceType.King;
            });
        }

        public static Piece FromCharToPiece(char ch)
        {
            Piece piece;
            Player pieceColor = char.IsLower(ch) ? Player.Black : Player.White;
            switch (char.ToLower(ch))
            {
                case 'p': piece = new Pawn(pieceColor); break;
                case 'b': piece = new Bishop(pieceColor); break;
                case 'n': piece = new Knight(pieceColor); break;
                case 'r': piece = new Rook(pieceColor, true); break;//изначально указываем что ладьи двигались
                case 'q': piece = new Queen(pieceColor); break;
                case 'k': piece = new King(pieceColor, true);break;
                default: piece = null; break;
            }
            return piece;
        }

        public override string ToString()
        {
            String typePiece = Type switch
            {

                PieceType.Bishop => "B",
                PieceType.Knight => "N",
                PieceType.Rook => "R",
                PieceType.Queen => "Q",
                _ => " "
            };
            return typePiece;
        }
    }
}
