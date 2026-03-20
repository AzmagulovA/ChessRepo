using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ChessLogic
{
    public class GameState
    {
        public Board CurrentBoard { get; set; }//доска в текущий момент
        public Player CurrentPlayer { get; private set; }//ход игрока

        public Result? Result { get; private set; } = null;//результат партии

        private int noCaptureOrPawnMoves = 0;//подсчет ходов без взятия или хода пешки (иначе ничья)


        public AnalysMove analysMove = new AnalysMove();//текущий анализируемый ход

        public static bool WatchFromWhite = true;//переменная для отображения доски со стороны белых

        public void GameStateFromStr(string textFromFile)
        {
            (string fen, string[] moves) = ParsePgn(textFromFile);

            ResetGame(fen);

            if (moves.Length > 0)
            {
                ApplyMovesFromPgn(moves);
            }
        }

        private (string fen, string[] moves) ParsePgn(string textFromFile)
        {
            string fen = FenNotation.StartPosition;
            string[] moves = Array.Empty<string>();
            string[] lines = textFromFile.Split(['\n'], StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();
                if (trimmedLine.StartsWith("[FEN"))
                {
                    fen = trimmedLine.Split('\"')[1];
                }
                else if (trimmedLine.Length > 0 && char.IsDigit(trimmedLine[0]))
                {
                    moves = trimmedLine.Split([' '], StringSplitOptions.RemoveEmptyEntries);
                }
            }
            return (fen, moves);
        }

        private void ResetGame(string fen)
        {
            CurrentPlayer = Player.White;
            CurrentBoard = Board.Initial();
            WatchFromWhite = true;
            analysMove = new AnalysMove { FenAfterMove = { Position = fen } };
            FromFenToGameBoard();
        }

        private void ApplyMovesFromPgn(string[] moves)
        {
            List<int> chronologyOfNumberMoves = new List<int>();
            List<Player> chronologyOfPlayers = new List<Player>();
            string userComment = "";
            bool commentStart = false;

            foreach (string move in moves)
            {
                if (string.IsNullOrWhiteSpace(move)) continue;

                if (!commentStart && move.Contains('{')) commentStart = true;

                if (!commentStart)
                {
                    if (move.StartsWith('('))
                    {
                        chronologyOfNumberMoves.Add(analysMove.Number);
                        chronologyOfPlayers.Add(CurrentPlayer);
                        analysMove = analysMove.PreviousMove ?? analysMove;
                        CurrentPlayer = CurrentPlayer.Opponent();
                        FromFenToGameBoard();
                    }

                    ProcessNormalMove(move);

                    if (move.EndsWith(')'))
                    {
                        analysMove = analysMove.SearchMove(chronologyOfNumberMoves.Last()) ?? analysMove;
                        CurrentPlayer = chronologyOfPlayers.Last();
                        chronologyOfNumberMoves.RemoveAt(chronologyOfNumberMoves.Count - 1);
                        chronologyOfPlayers.RemoveAt(chronologyOfPlayers.Count - 1);
                        FromFenToGameBoard();
                    }
                }
                else
                {
                    userComment = (userComment + " " + move.Replace("{", "").Replace("}", "")).Trim();
                    if (move.Contains('}'))
                    {
                        analysMove.UserComment = userComment;
                        commentStart = false;
                        userComment = "";
                    }
                }
            }
        }

        private void ProcessNormalMove(string move)
        {
            string cleanMove = move.Trim('(', ')');
            while (cleanMove.Length > 0 && !char.IsLetter(cleanMove[0]))
            {
                cleanMove = cleanMove.Substring(1);
            }

            if (cleanMove.Length > 0)
            {
                cleanMove = cleanMove.TrimEnd('+', '#');
                MakeMove(cleanMove);
            }
        }
        public GameState(Player player,Board board)
        {
            CurrentPlayer = player;
            CurrentBoard = board;
            
        }
        public IEnumerable<Move> LegalMovesForPiece(Position pos)//возможные ходы для выбранной фигуры
        {
            if(CurrentBoard.IsEmpty(pos)|| CurrentBoard[pos].Color != CurrentPlayer)//если клетка доски пуста или там не наша фигура, то нет доступных ходов
            {
                return Enumerable.Empty<Move>();
            }
            Piece piece = CurrentBoard[pos];//иначе берем эту фигуру и смотрим доступные ходы
            IEnumerable<Move> moveCandidates= piece.GetMoves(pos,CurrentBoard);
            return moveCandidates.Where(move => move.IsLegal(CurrentBoard));

        }
        public void MakeMove(string moveStr)
        {
            CurrentBoard.SetPawnPosition(CurrentPlayer, null);

            if (moveStr.StartsWith('O'))
            {
                MakeCastleMove(moveStr);
                return;
            }

            PieceType pieceType = GetPieceTypeFromChar(moveStr[0]);
            string remainingStr = (pieceType == PieceType.Pawn) ? moveStr : moveStr.Substring(1);

            PieceType promotionType = PieceType.Pawn;
            if (char.IsLetter(remainingStr[^1]) && remainingStr.Length >= 2 && remainingStr[^2] == '=')
            {
                promotionType = GetPieceTypeFromChar(remainingStr[^1]);
                remainingStr = remainingStr.Substring(0, remainingStr.Length - 2);
            }
            else if (char.IsLetter(remainingStr[^1]) && pieceType == PieceType.Pawn && remainingStr.Length >= 2)
            {
                // Support older notation where promotion piece is just appended
                promotionType = GetPieceTypeFromChar(remainingStr[^1]);
                remainingStr = remainingStr.Substring(0, remainingStr.Length - 1);
            }

            Position toPos = new Position(remainingStr.Substring(remainingStr.Length - 2));
            string explanation = remainingStr.Length > 2 ? remainingStr.Substring(0, remainingStr.Length - 2).Replace("x", "") : "";

            var candidateMoves = GetCandidateMoves(pieceType, toPos, explanation);

            if (!candidateMoves.Any()) return;

            Move selectedMove = candidateMoves.First();
            if (promotionType != PieceType.Pawn)
            {
                selectedMove = new PawnPromotion(selectedMove.FromPos, selectedMove.ToPos, promotionType);
            }

            MakeMove(selectedMove);
        }

        private void MakeCastleMove(string moveStr)
        {
            Position kingPos = CurrentBoard.FindPiece(CurrentPlayer, PieceType.King);
            MoveType castleType = moveStr.Length == 3 ? MoveType.CastleKS : MoveType.CastleQS;
            MakeMove(new Castle(castleType, kingPos));
        }

        private PieceType GetPieceTypeFromChar(char c)
        {
            return c switch
            {
                'N' => PieceType.Knight,
                'B' => PieceType.Bishop,
                'R' => PieceType.Rook,
                'Q' => PieceType.Queen,
                'K' => PieceType.King,
                _ => PieceType.Pawn
            };
        }

        private IEnumerable<Move> GetCandidateMoves(PieceType type, Position toPos, string explanation)
        {
            var pieces = CurrentBoard.FindAllPieces(CurrentPlayer, type);
            var candidateMoves = new List<Move>();

            foreach (var pos in pieces)
            {
                foreach (var move in LegalMovesForPiece(pos))
                {
                    if (move.ToPos == toPos && IsMatchingExplanation(move.FromPos, explanation))
                    {
                        candidateMoves.Add(move);
                    }
                }
            }
            return candidateMoves;
        }

        private bool IsMatchingExplanation(Position from, string explanation)
        {
            if (string.IsNullOrEmpty(explanation)) return true;

            if (explanation.Length == 2)
            {
                return from == new Position(explanation);
            }
            if (char.IsDigit(explanation[0]))
            {
                int row = 8 - (int)char.GetNumericValue(explanation[0]);
                return from.Row == row;
            }
            else
            {
                int col = explanation[0] - 'a';
                return from.Column == col;
            }
        }
        public void MakeMove(Move move)
        {
            CurrentBoard.SetPawnPosition(CurrentPlayer, null);

            Position from = WatchFromWhite ? move.FromPos : new Position(7 - move.FromPos.Row, 7 - move.FromPos.Column);
            Position to = WatchFromWhite ? move.ToPos : new Position(7 - move.ToPos.Row, 7 - move.ToPos.Column);

            int newMoveNumb = CurrentPlayer == Player.Black ? analysMove.MoveNumb + 1 : analysMove.MoveNumb;

            string moveName = SetNameAndExecuteMove(move);
            AnalysMove newAnalysMove = new AnalysMove(moveName, analysMove, new List<AnalysMove>(), from, to, newMoveNumb);

            AnalysMove? existingMove = analysMove.NextMoves.FirstOrDefault(s => s.MoveName == moveName);

            if (existingMove != null)
            {
                analysMove = existingMove;
            }
            else
            {
                analysMove.NextMoves.Add(newAnalysMove);
                analysMove = newAnalysMove;
            }

            CurrentPlayer = CurrentPlayer.Opponent();
            UpdateStateString();
            CheckForGameOver();
        }
        public void SetMoveByNumber(int numb)
        {
            AnalysMove? foundMove = analysMove.SearchMove(numb);
            if (foundMove != null)
            {
                analysMove = foundMove;
            }
            CheckForGameOver();
        }
        public string MakeFullStrHistory()
        {
            string res="";
            AnalysMove current = this.analysMove;
            while (current.PreviousMove != null)//возвращаемся к первому элементу
            {
                current = current.PreviousMove;
            }
            if (!current.WhiteMove())//Ход черных
            {
                res = "1... ";
            }
            res =res + current.PrintBranch();
            return res;

        }
        public void MakeComputerMove(string strMove)//совершение хода
        {
            Move move ;
            Position from;
            Position to;
            if (strMove.Length==4)//обычный ход
            {
                from = FromStrToPos(strMove.Substring(0,2));//координаты относительно следящего за доской
                to = FromStrToPos(strMove.Substring(strMove.Length-2, 2));
                if (CurrentBoard[from].Type!=PieceType.Pawn && CurrentBoard[from].Type != PieceType.King)//если это не король и не пешка, то делаем обычный ход
                {
                    move = new NormalMove(from, to);
                    MakeMove(move) ;
                }
                else//если пешка или король
                {
                    if (CurrentBoard[from].Type == PieceType.King)//если король
                    {
                        int difference = Math.Abs(from.Column - to.Column);
                        switch (difference)
                        {
                            case 2:
                                if ((strMove=="e1g1")||(strMove == "e8g8"))
                                {
                                    move = new Castle(MoveType.CastleKS, from);
                                    
                                }
                                else
                                {
                                    move = new Castle(MoveType.CastleQS, from); 
                                }
                                break;
                            
                            default:
                                move = new NormalMove(from, to);
                                break;
                        }
                        
                        MakeMove(move);
                    }
                    else {
                        int difference = Math.Abs(from.Row - to.Row);
                        switch (difference)
                        {
                            case 2:
                                move = new DoublePawn(from,to);
                                break;
                            default:
                                move = new NormalMove(from, to);
                                break;
                        }

                        MakeMove(move);
                    }

                }
            }
            else//превращение
            {
                char promotionChar = strMove[strMove.Length-1];//превращение в фигуру
                from = FromStrToPos(strMove.Substring(0, 2));
                to = FromStrToPos(strMove.Substring(strMove.Length - 3, 2));

                PieceType promotion = PieceType.Queen;

                switch (promotionChar)
                {
                    case 'q':
                        promotion = PieceType.Queen;
                        break;
                    case 'b':
                        promotion = PieceType.Bishop;
                        break;
                    case 'n':
                        promotion = PieceType.Knight;
                        break;
                    case 'r':
                        promotion = PieceType.Rook;
                        break;
                }

                move = new PawnPromotion(from,to,promotion);
                MakeMove(move);

            }


        }
        public Position FromStrToPos(string coords)
        {
            Position res;
            if (WatchFromWhite)
            {
                res = new Position(coords);
            }
            else
            {
                int row = (int)char.GetNumericValue(coords[1]) - 1;
                int col = 'h' - coords[0];
                res = new Position(row, col);
            }
            return res;
        }
        public string SetNameAndExecuteMove(Move move)
            {
            string MoveName = "";
            Piece pieceMakeMove = CurrentBoard[move.FromPos];//фигура которая делает ход
            if (CurrentPlayer == Player.White)//если ход белых, то указываем еще и текущий ход
            {
                MoveName = MoveName + (analysMove.MoveNumb).ToString() + ".";
            }
            switch (pieceMakeMove.Type)
            {
                case PieceType.Pawn:
                    MoveName = MoveName + SetNameMovePawn(move);//если пешка то устанавливаем свой откуда и куда
                    break;
                case PieceType.King:
                    MoveName = MoveName + SetNameMoveKing(move);//если король то можно указать рокировку
                    break;
                default:
                    MoveName = MoveName + SetNameMoveBRQN(move);//для всех прочих фигур
                    break;
            }
            bool captureOrPawn = move.Execute(CurrentBoard);//выполнение хода

            if (move.Type == MoveType.PawnPromotion)//если это превращение пешки
            {
                char typePiece = CurrentBoard[move.ToPos].Type switch
                {
                    PieceType.Bishop => 'B',
                    PieceType.Knight => 'N',
                    PieceType.Rook => 'R',
                    PieceType.Queen => 'Q',
                    _ => ' '
                };
                MoveName = MoveName + "=" + typePiece;//то указываем в какую фигуру превратилась пешка
            }

            if (CurrentBoard.IsInCheck(CurrentPlayer.Opponent()))//если шах
            {
                if (AllLegalMovesFor(CurrentPlayer.Opponent()).Any())//есть куда сходить, то указываем просто шах
                {
                    MoveName = MoveName + "+";
                }
                else // иначе это мат
                {
                    MoveName = MoveName + "#";
                }
            }

            if (captureOrPawn)
            {
                noCaptureOrPawnMoves = 0;
            }
            else
            {
                noCaptureOrPawnMoves++;
            }
            return MoveName;
        }
        public string SetNameMovePawn(Move move)
        {
            string Name = "";

            if (!CurrentBoard.IsEmpty(move.ToPos)|| move.Type == MoveType.EnPassant)//рубка или взятие на проходе
            {
                 Name = FromCoordsToStr(move.FromPos, WatchFromWhite)[0] + "x" ;
            }

            Name = Name + FromCoordsToStr(move.ToPos,WatchFromWhite);


            return Name;
        }

        public string SetNameMoveKing(Move move)
        {
            if (move.Type == MoveType.CastleKS)
            {
                return "O-O";
            }
            if (move.Type == MoveType.CastleQS)
            {
                return "O-O-O";
            }

            string Name = "K";

            Name = Name + FromCoordsToStr(move.ToPos, WatchFromWhite);

            return Name;
        }

        public string SetNameMoveBRQN(Move move)
        {
            string Name = "";

            Counting counting = CurrentBoard.CountPieces();//подсчет всех элементов на доске

            Piece piece = CurrentBoard[move.FromPos];

            IEnumerable<Position> PositionsOfPices = new List<Position>();

            List<Position> PositionsOfPiecesThatCanMove = new List<Position>();

            Player pieceColor = piece.Color;

            bool takePiece = !CurrentBoard.IsEmpty(move.ToPos);

            Name = piece.ToString();

            
            if(counting.ColorCount(piece.Color, piece.Type) > 1)//если таких фигур на доске много
            {
                PositionsOfPices = CurrentBoard.PiecePositionsFor(piece.Color).Where(pos => CurrentBoard[pos].Type == piece.Type);//все позиции таких же фигур +
                foreach (Position position in PositionsOfPices)//позиции всех таких же фигур
                {
                    IEnumerable<Move> posMoves = LegalMovesForPiece(position);//все возможные ходы фигуры
                    foreach (Move pieceMove in posMoves)//смотрим ходы фигуры
                    {
                        if (pieceMove.ToPos == move.ToPos)//если она тоже достигает конечной точки
                        {
                            PositionsOfPiecesThatCanMove.Add(position);//то добавляем эту фигуру в коллекцию
                        }
                    }
                }
                if (PositionsOfPiecesThatCanMove.Count()==2)
                {
                    if (PositionsOfPiecesThatCanMove[0].Row == PositionsOfPiecesThatCanMove[1].Row)//если буквы равны
                    {
                        Name = Name + FromCoordsToStr(move.FromPos, WatchFromWhite)[1];//для различия ставим цифры
                    }
                    else
                    {
                        Name = Name + FromCoordsToStr(move.FromPos, WatchFromWhite)[0];//для различия ставим буквы
                    }
                }
                else
                {
                    bool digitChecker = false;
                    bool letterChecker = false;
                    foreach (Position pos in PositionsOfPiecesThatCanMove)
                    {
                        if (move.FromPos != pos)//если это не исходная координта
                        {
                            letterChecker = (move.FromPos.Column == pos.Column)? true : letterChecker;
                            digitChecker = (move.FromPos.Row == pos.Row) ? true : digitChecker;
                        }
                    }
                    Name = digitChecker ? Name + FromCoordsToStr(move.FromPos, WatchFromWhite)[0]: Name;
                    Name = letterChecker ? Name + FromCoordsToStr(move.FromPos, WatchFromWhite)[1] : Name;
                    
                }
            }
            Name = takePiece? Name = Name + 'x' + FromCoordsToStr(move.ToPos, WatchFromWhite) : Name + FromCoordsToStr(move.ToPos, WatchFromWhite);
            return Name;
        }
        public string FromCoordsToStr(Position pos, bool WatchFromWhite)
        {
            if (WatchFromWhite)
            {
                return pos.ToString();
            }
            else
            {
                char letter = (char)('h' - pos.Column);
                int digit = pos.Row + 1;
                return $"{letter}{digit}";
            }
        }
        public void FromFenToGameBoard()
        {
            
            string sb = analysMove.FenAfterMove.Position;
            Board board = new Board();
            Player current = Player.White;
            int counterRow = 0;
            int countercColumn = 0;
            int spaceCounter = 0;
            Position wKingPos = new Position(0, 0);
            Position bKingPos = new Position(0, 0);
            for (int i = 0; i < sb.Length; i++)
            {
                Piece? piece = null;
                if (sb[i] == ' ') 
                { 
                    spaceCounter++;
                    i++;
                }
                if (spaceCounter == 0)//если обрабатывается строка
                {
                    if (!Char.IsNumber(sb.ToString(), i))//если не цифра
                    {
                        if (sb[i] == '/')
                        {
                            counterRow++;
                            countercColumn = 0;
                        }
                        else
                        {
                            switch (sb[i])
                            {
                                case 'p': 
                                    if(counterRow == 1)piece = new Pawn(Player.Black, false); //пешка не ходила
                                    else { piece = new Pawn(Player.Black, true); }
                                    break;
                                case 'b': piece = new Bishop(Player.Black); break;
                                case 'n': piece = new Knight(Player.Black); break;
                                case 'r': piece = new Rook(Player.Black,true); break;//изначально указываем что ладьи двигались
                                case 'q': piece = new Queen(Player.Black); break;
                                case 'k': 
                                    piece = new King(Player.Black, true);
                                    bKingPos = new Position(counterRow, countercColumn);//запомнили позиции королей(для рокировок)
                                    break;

                                case 'P':
                                    if (counterRow == 6) piece = new Pawn(Player.White, false); //пешка не ходила
                                    else { piece = new Pawn(Player.White, true); }
                                    break;
                                case 'B': piece = new Bishop(Player.White); break;
                                case 'N': piece = new Knight(Player.White); break;
                                case 'R': piece = new Rook(Player.White,true); break;
                                case 'Q': piece = new Queen(Player.White); break;
                                case 'K': 
                                    piece = new King(Player.White, true);
                                    wKingPos = new Position(counterRow, countercColumn);
                                    break;
                                default: break;
                            }
                            if (piece != null)
                            {
                                board[counterRow, countercColumn] = piece;
                            }
                            countercColumn++;
                        }

                    }
                    else//если цифра
                    {
                        countercColumn = countercColumn + int.Parse(sb[i].ToString());
                    }

                }
                if (spaceCounter==1)//если втретили один пробел (указание кто ходит)
                {
                    if (sb[i]=='w')
                    {
                        current = Player.White; 
                    }
                    if (sb[i] == 'b')
                    {
                        current = Player.Black ;
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
                        board[7,7].HasMoved = false;//ладья не двигалась
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
                        board.pawnSkipPositions[current.Opponent()] = new Position(sb.Substring(i, 2));
                        break;
                    }
                    
                }
            }
            CurrentPlayer = current;

            if (WatchFromWhite) 
                CurrentBoard = board;
            else
            {
                CurrentBoard = board.ReversBoard(false);
            }

        }
        private IEnumerable<Move> AllLegalMovesFor(Player player)//все легальные возможные ходы игрока
        {
            IEnumerable<Move> moveCandidates = CurrentBoard.PiecePositionsFor(player).SelectMany(pos =>
            {
                Piece piece = CurrentBoard[pos];
                return piece.GetMoves(pos, CurrentBoard);
            });

            return moveCandidates.Where(move => move.IsLegal(CurrentBoard));

        }
        public void CheckForGameOver()//проверка на окончание партии после каждого хода
        {
            if (!AllLegalMovesFor(CurrentPlayer).Any())//если у игрока нет ходов
            {
                if (CurrentBoard.IsInCheck(CurrentPlayer))//и король под шахом
                {
                    Result = Result.Win(CurrentPlayer.Opponent());//победа противника

                }
                else
                {
                    Result = Result.Draw(EndReason.Stalemate);//пат
                }
            }
            else if (CurrentBoard.InsufficientMaterial())
            {
                Result = Result.Draw(EndReason.InsufficientMaterial);
            }
            else if (FiftyMoveRule())
            {
                Result = Result.Draw(EndReason.FiftyMoveRule);
            }
            else
            {
                Result = null;
            }
        }
        public bool IsGameOver()
        {
            return Result != null;
        }

        private bool FiftyMoveRule()
        {
            int fullMoves = noCaptureOrPawnMoves / 2;
            return fullMoves == 50;
        }

        private void UpdateStateString()
        {
            analysMove.FenAfterMove.Position = new StateString(CurrentPlayer, CurrentBoard, WatchFromWhite).ToString();

           
        }

        public void GameStateReversBoard()
        {
            WatchFromWhite = !WatchFromWhite;//поменяли на обратное значение просмотр доски

            CurrentBoard = CurrentBoard.ReversBoard(WatchFromWhite);
        }
        public Position GetLastMoveTo()
        {
            Position res;
            if (WatchFromWhite)
            {
                res = analysMove.LastTo;
            }
            else
            {
                res = new Position(Math.Abs(analysMove.LastTo.Row-7), Math.Abs(analysMove.LastTo.Column-7));
            }
            return res;
        }
        
        public Position GetLastMoveFrom()
        {
            
            Position res;
            if (WatchFromWhite)
            {
                res = analysMove.LastFrom;
            }
            else
            {
                res = new Position(Math.Abs(analysMove.LastFrom.Row - 7), Math.Abs(analysMove.LastFrom.Column - 7));
            }
            return res;
        }
    }
}
