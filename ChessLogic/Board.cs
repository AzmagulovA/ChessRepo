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
        public bool BoardFromWhite = true;//переменная для отображения доски(изначально со стороны белых)

        private readonly Piece[,] pieces = new Piece[8, 8];


        public Dictionary<Player, Position> pawnSkipPositions = new Dictionary<Player, Position>()
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

        public Position GetPawnSkipPosition(Player player)
        {
            return pawnSkipPositions[player];

        }
        public void SetPawnPosition(Player player, Position pos)
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
            this[0, 0] = new Rook(Player.Black);
            this[0, 1] = new Knight(Player.Black);
            this[0, 2] = new Bishop(Player.Black);
            this[0, 3] = new Queen(Player.Black);
            this[0, 4] = new King(Player.Black, BoardFromWhite);
            this[0, 5] = new Bishop(Player.Black);
            this[0, 6] = new Knight(Player.Black);
            this[0, 7] = new Rook(Player.Black);

            this[1, 0] = new Pawn(Player.Black, BoardFromWhite);
            this[1, 1] = new Pawn(Player.Black, BoardFromWhite);
            this[1, 2] = new Pawn(Player.Black, BoardFromWhite);
            this[1, 3] = new Pawn(Player.Black, BoardFromWhite);
            this[1, 4] = new Pawn(Player.Black, BoardFromWhite);
            this[1, 5] = new Pawn(Player.Black, BoardFromWhite);
            this[1, 6] = new Pawn(Player.Black, BoardFromWhite);
            this[1, 7] = new Pawn(Player.Black, BoardFromWhite);


            this[7, 0] = new Rook(Player.White);
            this[7, 1] = new Knight(Player.White);
            this[7, 2] = new Bishop(Player.White);
            this[7, 3] = new Queen(Player.White);
            this[7, 4] = new King(Player.White, BoardFromWhite);
            this[7, 5] = new Bishop(Player.White);
            this[7, 6] = new Knight(Player.White);
            this[7, 7] = new Rook(Player.White);

            this[6, 0] = new Pawn(Player.White, BoardFromWhite);
            this[6, 1] = new Pawn(Player.White, BoardFromWhite);
            this[6, 2] = new Pawn(Player.White, BoardFromWhite);
            this[6, 3] = new Pawn(Player.White, BoardFromWhite);
            this[6, 4] = new Pawn(Player.White, BoardFromWhite);
            this[6, 5] = new Pawn(Player.White, BoardFromWhite);
            this[6, 6] = new Pawn(Player.White, BoardFromWhite);
            this[6, 7] = new Pawn(Player.White, BoardFromWhite);
        }
        public static bool IsInside(Position pos)
        {
            return pos.Row >= 0 && pos.Row <= 7 && pos.Column >= 0 && pos.Column <= 7;
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
        public Position FindPiece(Player color, PieceType type, String Coords)//поиск фигуры с подсказкой
        {
            if (Coords.Length == 2)//если просто 2 символа, то надо просто конвертировать в позицию 
            {
                return new Position(Math.Abs(8 - (int)Char.GetNumericValue(Coords[1])), Math.Abs((int)Coords[0] - 97));
            }
            else
            {
                if (char.IsDigit(Coords[0]))//если цифра
                {
                    int rows = Math.Abs(8 - (int)Char.GetNumericValue(Coords[0]));
                    return PiecePositionsFor(color).Where(pos => pos.Row == rows).First(pos => this[pos].Type == type);
                }
                else//если буква
                {
                    int col = Math.Abs((int)Coords[0] - 97);

                    return PiecePositionsFor(color).Where(pos => pos.Column == col).First(pos => this[pos].Type == type);
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
        public Board ReversBoard(bool WatchFromWhite)
        {
            Board NewBoard = new Board();

            NewBoard.BoardFromWhite = WatchFromWhite;//значение доски перевернуто

            NewBoard.pawnSkipPositions = pawnSkipPositions;

            if (pawnSkipPositions[Player.White] != null) NewBoard.pawnSkipPositions[Player.White] = new Position(Math.Abs(pawnSkipPositions[Player.White].Row - 7),Math.Abs(pawnSkipPositions[Player.White].Column - 7));
            if (pawnSkipPositions[Player.Black] != null) NewBoard.pawnSkipPositions[Player.Black] = new Position(Math.Abs(pawnSkipPositions[Player.Black].Row - 7), Math.Abs(pawnSkipPositions[Player.Black].Column - 7));

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    NewBoard[i, j] = this[Math.Abs(i - 7), Math.Abs(j - 7)];//переврот элементов

                    if (NewBoard[i, j] != null)
                    {
                        if (NewBoard[i, j].Type == PieceType.Pawn)//если пешка, то идет в противоположном направлении
                        {
                            Pawn ReversPawn = new Pawn(NewBoard[i, j].Color, WatchFromWhite, NewBoard[i, j].HasMoved);
                            NewBoard[i, j] = ReversPawn;
                        }
                        if (NewBoard[i, j].Type == PieceType.King)//если король, то новые рокировки
                        {
                            King ReversKing = new King(NewBoard[i, j].Color, WatchFromWhite, NewBoard[i, j].HasMoved);
                            NewBoard[i, j] = ReversKing;
                        }
                    }


                }
            }
            return NewBoard;
        }

    }
}
