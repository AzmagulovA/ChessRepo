using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ChessLogic
{
    public class Board
    {
        public bool BoardFromWhite = GameState.WatchFromWhite;//переменная для отображения доски(изначально со стороны белых)

        private readonly Piece[,] pieces = new Piece[8, 8];


        public Dictionary<Player, Position?> pawnSkipPositions = new Dictionary<Player, Position?>()
        {
            { Player.White, null},
            { Player.Black,null}

        };
        public Piece this[int row, int col]
        {
            get { return pieces[row, col]; }
            set { pieces[row, col] = value; }
        }
        public Piece this[Position pos]
        {
            get { return this[pos.Row, pos.Column]; }
            set { this[pos.Row, pos.Column] = value; }
        }

        public Position? GetPawnSkipPosition(Player player)
        {
            return pawnSkipPositions[player];

        }
        public void SetPawnPosition(Player player, Position? pos)
        {
            pawnSkipPositions[player] = pos;
        }

        public static Board Initial()
        {
            Board board = new Board();
            board.BoardFromWhite = true;
            board.AddStartPieces();
            return board;
        }
        private void AddStartPieces()
        {
            AddBackRank(0, Player.Black);
            AddPawnRow(1, Player.Black);
            AddPawnRow(6, Player.White);
            AddBackRank(7, Player.White);
        }

        private void AddBackRank(int row, Player color)
        {
            this[row, 0] = new Rook(color);
            this[row, 1] = new Knight(color);
            this[row, 2] = new Bishop(color);
            this[row, 3] = new Queen(color);
            this[row, 4] = new King(color);
            this[row, 5] = new Bishop(color);
            this[row, 6] = new Knight(color);
            this[row, 7] = new Rook(color);
        }

        private void AddPawnRow(int row, Player color)
        {
            for (int col = 0; col < 8; col++)
            {
                this[row, col] = new Pawn(color);
            }
        }
        public static bool IsInside(Position? pos)
        {
            return pos != null && pos.Row >= 0 && pos.Row <= 7 && pos.Column >= 0 && pos.Column <= 7;
        }
        public bool IsEmpty(Position pos)//проверка на пустоту клетки
        {
            return this[pos] == null;
        }

        public IEnumerable<Position> PiecePositions()//возврат всех позиций в которых фигуры
        {
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    Position pos = new Position(r, c);
                    if (!IsEmpty(pos)) yield return pos;
                }
            }
        }
        public IEnumerable<Position> PiecePositionsFor(Player player)//местоположение фигур своего цвета
        {
            return PiecePositions().Where(pos => this[pos].Color == player);
        }

        public bool IsInCheck(Player player)//под шахом
        {
            return PiecePositionsFor(player.Opponent()).Any(pos =>
            {
                Piece piece = this[pos];
                return piece.CanCaptureOpponentKing(pos, this);
            });
        }
        public Board Copy()
        {
            Board copy = new Board();

            foreach (Position pos in PiecePositions())
            {
                copy[pos] = this[pos].Copy();
            }
            return copy;
        }

        public Counting CountPieces()//подсчет всех фигур на доске
        {
            Counting counting = new Counting();
            foreach (Position pos in PiecePositions())
            {
                Piece piece = this[pos];
                counting.Increment(piece.Color, piece.Type);
            }
            return counting;
        }
        public bool InsufficientMaterial()
        {
            Counting counting = CountPieces();

            return IsKingBishopVKing(counting) || IsKingBishopVKingBishop(counting) || IsKingKnightVKing(counting) || IsKingVKing(counting);
        }

        private static bool IsKingVKing(Counting counting)//только 2 короля
        {
            return counting.TotalCount == 2;
        }

        private static bool IsKingBishopVKing(Counting counting)
        {
            return counting.TotalCount == 3 && (counting.White(PieceType.Bishop) == 1 || counting.Black(PieceType.Bishop) == 1);//2 короля + слон
        }
        private static bool IsKingKnightVKing(Counting counting)
        {
            return counting.TotalCount == 3 && (counting.White(PieceType.Knight) == 1 || counting.Black(PieceType.Knight) == 1);//2 короля + конь
        }
        private bool IsKingBishopVKingBishop(Counting counting)
        {
            if (counting.TotalCount != 4)
            {
                return false;
            }
            if (counting.White(PieceType.Bishop) != 1 || counting.Black(PieceType.Bishop) != 1)
            {
                return false;
            }
            Position wBishopPos = FindPiece(Player.White, PieceType.Bishop);
            Position bBishopPos = FindPiece(Player.Black, PieceType.Bishop);

            return wBishopPos.SquarePlayer() == bBishopPos.SquarePlayer();//определяем на одной ли клеке слоны

        }

        public Position FindPiece(Player color, PieceType type)
        {
            return PiecePositionsFor(color).First(pos => this[pos].Type == type);
        }
        public Position FindPiece(Player color, PieceType type, string coords)//поиск фигуры с подсказкой
        {
            if (coords.Length == 2)//если просто 2 символа, то надо просто конвертировать в позицию
            {
                return new Position(coords);
            }
            else
            {
                if (char.IsDigit(coords[0]))//если цифра
                {
                    int row = 8 - (int)char.GetNumericValue(coords[0]);
                    return PiecePositionsFor(color).First(pos => pos.Row == row && this[pos].Type == type);
                }
                else//если буква
                {
                    int col = coords[0] - 'a';
                    return PiecePositionsFor(color).First(pos => pos.Column == col && this[pos].Type == type);
                }
            }

        }
        public IEnumerable<Position> FindAllPieces(Player color, PieceType type)//коллекция всех фигур определенного типа
        {
            return PiecePositionsFor(color).Where(pos => this[pos].Type == type);
        }

        private bool IsUnmovedKingAndRook(Position kingPos, Position rookPos)
        {
            if (IsEmpty(kingPos) || (IsEmpty(rookPos)))
            {
                return false;
            }

            Piece king = this[kingPos];
            Piece rook = this[rookPos];

            return king.Type == PieceType.King && rook.Type == PieceType.Rook &&
                !king.HasMoved && !rook.HasMoved;
        }
        public bool CastleRightKS(Player player)//рокировка в короткую
        {
            if (BoardFromWhite)
            {
                return player switch
                {
                    Player.White => IsUnmovedKingAndRook(new Position(7, 4), new Position(7, 7)),
                    Player.Black => IsUnmovedKingAndRook(new Position(0, 4), new Position(0, 7)),
                    _ => false
                };
            }
            else
            {
                return player switch
                {
                    Player.Black => IsUnmovedKingAndRook(new Position(7, 3), new Position(7, 0)),
                    Player.White => IsUnmovedKingAndRook(new Position(0, 3), new Position(0, 0)),
                    _ => false
                };
            }

        }

        public bool CastleRightQS(Player player)//в длинную
        {
            if (BoardFromWhite)
            {
                return player switch
                {
                    Player.White => IsUnmovedKingAndRook(new Position(7, 4), new Position(7, 0)),
                    Player.Black => IsUnmovedKingAndRook(new Position(0, 4), new Position(0, 0)),
                    _ => false
                };
            }
            else
            {
                return player switch
                {
                    Player.Black => IsUnmovedKingAndRook(new Position(7, 3), new Position(7, 7)),
                    Player.White => IsUnmovedKingAndRook(new Position(0, 3), new Position(0, 7)),
                    _ => false
                };
            }

        }

        private bool HasPawnInPosition(Player player, Position[] pawnPositions, Position skipPos)
        {
            foreach (Position pos in pawnPositions.Where(IsInside))
            {
                Piece piece = this[pos];
                if (piece == null || piece.Color != player || piece.Type != PieceType.Pawn)
                {
                    continue;
                }

                EnPassant move = new EnPassant(pos, skipPos);
                if (move.IsLegal(this))
                {
                    return true;
                }
            }
            return false;
        }

        public bool CanCaptureEnPassant(Player player)
        {
            Position skipPos = GetPawnSkipPosition(player.Opponent());
            if (skipPos == null)
            {
                return false;
            }
            Position[] pawnPositions;
            if (BoardFromWhite)
            {
                pawnPositions = player switch
                {

                    Player.White => new Position[] { skipPos + Direction.SouthWest, skipPos + Direction.SouthEast },
                    Player.Black => new Position[] { skipPos + Direction.NorthWest, skipPos + Direction.NorthEast },
                    _ => Array.Empty<Position>()
                };
            }
            else
            {
                pawnPositions = player switch
                {

                    Player.Black => new Position[] { skipPos + Direction.SouthWest, skipPos + Direction.SouthEast },
                    Player.White => new Position[] { skipPos + Direction.NorthWest, skipPos + Direction.NorthEast },
                    _ => Array.Empty<Position>()
                };
            }

            return HasPawnInPosition(player, pawnPositions, skipPos);
        }
        public bool RightBoard(Player CurrentPlayer)//проверка на корректность доски составленным пользователем
        {
            Counting counter = CountPieces();
            return RightCountOfKings(counter) && PawnsNotOnFirstAndLastLines() && !OpponentInCheck(CurrentPlayer);
        }
        public bool RightCountOfKings(Counting counter)//количество королей с обеих сторон равно одному
        {
            return ((counter.Black(PieceType.King) == 1) && (counter.White(PieceType.King) == 1));
        }
        public bool PawnsNotOnFirstAndLastLines()//количество королей с обеих сторон равно одному
        {
            for (int i = 0; i < 8; i++)
            {
                if ((!IsEmpty(new Position(0, i)) && this[0, i].Type == PieceType.Pawn) ||
                    (!IsEmpty(new Position(7, i)) && (this[7, i].Type == PieceType.Pawn)))
                {
                    return false;
                }
            }
            return true;
        }

        public void FillCastles(bool WK, bool WQ, bool BK, bool BQ)
        {
            if (BoardFromWhite)
            {
                if (this[7, 7] != null) this[7, 7].HasMoved = (!WK) ? true : false;
                if (this[7, 0] != null) this[7, 0].HasMoved = (!WQ) ? true : false;
                if (this[0, 7] != null) this[0, 7].HasMoved = (!BK) ? true : false;
                if (this[0, 0] != null) this[0, 0].HasMoved = (!BQ) ? true : false;
            }
            else
            {
                if (this[0, 0] != null) this[0, 0].HasMoved = (!WK) ? true : false;
                if (this[0, 7] != null) this[0, 7].HasMoved = (!WQ) ? true : false;
                if (this[7, 0] != null) this[7, 0].HasMoved = (!BK) ? true : false;
                if (this[7, 7] != null) this[7, 7].HasMoved = (!BQ) ? true : false;

            }

        }
        public bool OpponentInCheck(Player CurrentPlayer)
        {
            return (IsInCheck(CurrentPlayer.Opponent()));
        }
        public Board ReversBoard(bool watchFromWhite)
        {
            Board newBoard = new Board { BoardFromWhite = watchFromWhite };

            foreach (var kvp in pawnSkipPositions)
            {
                if (kvp.Value != null)
                {
                    newBoard.pawnSkipPositions[kvp.Key] = new Position(7 - kvp.Value.Row, 7 - kvp.Value.Column);
                }
            }

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    Piece? piece = this[7 - r, 7 - c];
                    if (piece != null)
                    {
                        newBoard[r, c] = piece.Type switch
                        {
                            PieceType.Pawn => new Pawn(piece.Color, piece.HasMoved),
                            PieceType.King => new King(piece.Color, piece.HasMoved),
                            _ => piece.Copy()
                        };
                    }
                }
            }
            return newBoard;
        }

    }
}
