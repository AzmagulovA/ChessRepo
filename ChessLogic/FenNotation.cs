using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessLogic
{
    public class FenNotation
    {
        public static string StartPosition = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        public static bool IsStartPosition(string position) => position.Equals(StartPosition);

        public string Position;

        public bool IsWhiteMove => Position[Position.IndexOf(" ") + 1] == 'w';//ход из fen;

        public FenNotation()
        {
            Position = StartPosition;
        }

        public FenNotation(string position)
        {
            Position = position;
        }

        public GameState ToGameState(bool WatchFromWhite)
        {

            string sb = Position;
            Board board = new Board();
            Player current = new Player();
            int counterRow = 0;
            int countercColumn = 0;
            int spaceCounter = 0;
            Position wKingPos = new Position(0, 0);
            Position bKingPos = new Position(0, 0);
            for (int i = 0; i < sb.Length; i++)
            {
                Piece piece = new Bishop(Player.Black);
                if (sb[i] == ' ')
                {
                    spaceCounter++;
                    i++;
                }
                if (spaceCounter == 0)//если обрабатывается строка
                {
                    if (Char.IsDigit(sb, i))//если цифра
                    {
                        countercColumn = countercColumn + int.Parse(sb[i].ToString());
                    }
                    else//если не цифра
                    {
                        if (sb[i] == '/')
                        {
                            counterRow++;
                            countercColumn = 0;
                        }
                        else
                        {
                        Piece piece1 = Piece.FromCharToPiece(sb[i]);
                        switch (piece1)
                        {
                            case Pawn:
                                if (counterRow == 1 && piece.Color == Player.Black ||
                                    counterRow == 6 && piece.Color == Player.White) 
                                {
                                    piece.HasMoved = false;
                                }
                                break;
                            case King:
                                if (piece.Color == Player.White)
                                    wKingPos = new Position(counterRow, countercColumn);
                                else
                                    bKingPos = new Position(counterRow, countercColumn);
                                break;
                            default: break;
                        }
                            
                            board[counterRow, countercColumn] = piece;
                            countercColumn++;
                        }

                    }

                }
                if (spaceCounter == 1)//если втретили один пробел (указание кто ходит)
                {
                    if (sb[i] == 'w')
                    {
                        current = Player.White;
                    }
                    if (sb[i] == 'b')
                    {
                        current = Player.Black;
                    }
                }
                if (spaceCounter == 2)//если втретили два пробела (рокировки)
                {
                    if (sb[i] == '-')//если прочерк, то пропускаем
                    {
                        board[wKingPos].HasMoved = true;
                        board[bKingPos].HasMoved = true;
                    }
                    if (sb[i] == 'K')
                    {
                        board[wKingPos].HasMoved = false;
                        board[7, 7].HasMoved = false;//ладья не двигалась
                    }
                    if (sb[i] == 'Q')
                    {
                        board[wKingPos].HasMoved = false;
                        board[7, 0].HasMoved = false;//ладья не двигалась
                    }
                    if (sb[i] == 'k')
                    {
                        board[bKingPos].HasMoved = false;
                        board[0, 7].HasMoved = false;//ладья не двигалась
                    }
                    if (sb[i] == 'q')
                    {
                        board[bKingPos].HasMoved = false;
                        board[0, 0].HasMoved = false;//ладья не двигалась
                    }
                }
                if (spaceCounter == 3)//если втретили три пробела (взятие на проходе)
                {
                    if (sb[i] == '-')//если прочерк, то пропускаем
                    {
                        break;
                    }
                    else//если там что-то есть
                    {
                        board.pawnSkipPositions[current.Opponent()] = new Position(Math.Abs(8 - (int)Char.GetNumericValue(sb[i + 1])), Math.Abs((int)sb[i] - 97));//g6 = 2 6| a6 - 2 0 | a-97 b-98 h-104 g-103| b3 = 4 1
                        break;
                    }

                }
            }
            Player CurrentPlayer = current;
            Board CurrentBoard = board;
            if (WatchFromWhite) CurrentBoard = board;
            else
            {
                CurrentBoard = board.ReversBoard(false);
            }
            return new GameState(CurrentPlayer, CurrentBoard, WatchFromWhite);
        }

        public override string ToString()
        {
            return Position;
        }
    }
}
