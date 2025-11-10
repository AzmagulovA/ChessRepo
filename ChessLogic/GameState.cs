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

        public Result Result { get; private set; } = null;//результат партии

        private int noCaptureOrPawnMoves = 0;//подсчет ходов без взятия или хода пешки (иначе ничья)


        public AnalysMove analysMove = new AnalysMove();//текущий анализируемый ход

        public bool WatchFromWhite = true;//переменная для отображения доски со стороны белых

        public void GameStateFromStr(String textFromFile)
        {
            string fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
            string[] moves = new string[0] ;
            string[] lines = textFromFile.Split(new char[] { '\n' });//строки текста
            foreach (string line in lines)
            {
                if (line.Length!=0)
                {
                    string[] words = line.Split(new char[] { ' ' });//слова строки
                    if (words[0] == "[FEN")//если первое слово - fen
                    {
                        fen = line.Split(new char[] { '\"' })[1];
                        analysMove.FenAfterMove = fen;//начальное положение
                    }
                    if (line[0] == '1')//проверка на начало анализа
                    {
                        moves = words;
                    }

                }
                
            }
            CurrentPlayer = Player.White;
            CurrentBoard = Board.Initial();
            WatchFromWhite = true;
            analysMove = new AnalysMove();
            analysMove.FenAfterMove = fen;
            List<int> chronologyOfNumberMoves = new List<int>();
            List<Player> chronologyOfPlayers = new List<Player>();
            FromFenToGameBoard();//инициализация начальной позиции
            string correctMove;
            string userComment="";
            bool commentStart = false;
            if (moves.Length != 0)//проход по ходам
            {
                foreach (string move in moves)
                {
                    correctMove = move;
                    if (correctMove != "")//если строка не пуста
                    {
                        if(!commentStart) commentStart = correctMove.Contains('{');//проверка на начало комментария
                        if (!commentStart)//если не комментарий, то анализируем как положено 
                        {
                            if (correctMove[0] == '(')//начало альтернативного варианта
                            {
                                chronologyOfNumberMoves.Add(analysMove.Number);//запоминаем номер последнего хода основного варианта(к нему надо будет затем вернуться)
                                chronologyOfPlayers.Add(CurrentPlayer);
                                analysMove = analysMove.LastMove;//смотрим ход от которого идет разветвление
                                CurrentPlayer = CurrentPlayer.Opponent();
                                FromFenToGameBoard();
                            }
                            if (correctMove[0] == ')')//конец альтернативного варианта
                            {
                                analysMove = analysMove.SearchMove(chronologyOfNumberMoves.Last());//поиск последнего элемента списка
                                CurrentPlayer = chronologyOfPlayers.Last();
                                chronologyOfNumberMoves.RemoveAt(chronologyOfNumberMoves.Count - 1); //удаление последнего элемента
                                chronologyOfPlayers.RemoveAt(chronologyOfPlayers.Count - 1);
                                FromFenToGameBoard();
                            }
                            while (!char.IsLetter(correctMove[0]))//проверка первого элемента на букву
                            {

                                correctMove = correctMove.Substring(1);//обрезка цифр
                                if (correctMove.Length == 0)//если это была просто цифра, то переходим к следующему
                                {
                                    break;
                                }
                            }
                            if (correctMove.Length != 0)//если строка была не пуста
                            {
                                if (correctMove[correctMove.Length - 1] == '+' || correctMove[correctMove.Length - 1] == '#')//последний элемент шах
                                {
                                    correctMove = correctMove.Remove(correctMove.Length - 1);//обрезка 
                                }
                                MakeMove(correctMove);
                            }
                        }
                        else//комментарий
                        {
                            if(correctMove.Contains('}'))//конец комментария
                            {
                                userComment = userComment + " " + correctMove.Replace("{", "").Replace("}", "");
                                analysMove.UserComment = userComment;
                                commentStart = false;
                                userComment = "";
                            }
                            else
                            {
                                userComment = userComment + " " + correctMove.Replace("{", "").Replace("}", "");
                            }
                        }
                    }
                }
            }



        }
        public GameState(Player player,Board board,bool FromWhite)
        {
            CurrentPlayer = player;
            CurrentBoard = board;
            CurrentBoard.BoardFromWhite = FromWhite;//при генерации доски изначальный вид со стороны белых
            
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
        public void MakeMove(string moveStr)//прием хода, все ходы выполняются с начальной позиции от лица белых
        {
            CurrentBoard.SetPawnPosition(CurrentPlayer, null);
            PieceType piecetype = PieceType.Pawn;
            switch (moveStr[0])//поиск фигуры
            {
                case 'N'://ход конем
                    piecetype = PieceType.Knight;
                    break;
                case 'B'://ход слоном
                    piecetype =  PieceType.Bishop;
                    break;
                case 'Q'://ход ферзем
                    piecetype =  PieceType.Queen;
                    break;
                case 'K'://ход королем
                    piecetype =  PieceType.King;
                    break;
                case 'R'://ход ладьей
                    piecetype =  PieceType.Rook;
                    break;
                default:
                    break;
            }
            IEnumerable<Position> pieces = CurrentBoard.FindAllPieces(CurrentPlayer, piecetype);//поиск всех положений такого цвета и типа фигуры
            if (piecetype is PieceType.Pawn)//если пешка или рокировка
            {
                if (moveStr[0]=='O')//значит рокировка
                {
                    Move move;
                    Position kingPos = CurrentBoard.FindPiece(CurrentPlayer, PieceType.King);
                    if (moveStr.Length == 3)//O-O
                    {
                        move = new Castle(MoveType.CastleKS, kingPos, WatchFromWhite);

                    }
                    else//O-O-O
                    {

                        move = new Castle(MoveType.CastleQS, kingPos, WatchFromWhite);
                    }
                    MakeMove(move);
                }
                else//ход пешкой
                {
                    if (char.IsLetter(moveStr[moveStr.Length-1]))//если последний символ буква, то это превращение
                    {
                        switch (moveStr[moveStr.Length - 1])//проверка фигуры
                        {
                            case 'N'://ход конем
                                piecetype = PieceType.Knight;
                                break;
                            case 'B'://ход слоном
                                piecetype = PieceType.Bishop;
                                break;
                            case 'Q'://ход ферзем
                                piecetype = PieceType.Queen;
                                break;
                            case 'R'://ход ладьей
                                piecetype = PieceType.Rook;
                                break;
                            default:
                                break;
                        }
                        moveStr = moveStr.Substring(0,moveStr.Length - 2);//отсекли 2 последних символа
                    }
                    Position toPos = new Position(moveStr.Substring( moveStr.Length-2 ));//куда
                    List<Move> pretendentMoves = new List<Move>();
                    foreach (Position pos in pieces)
                    {
                        IEnumerable<Move> LegalMoves = LegalMovesForPiece(pos);//список ходов для фигуры определенного типа
                        foreach (Move move in LegalMoves)
                        {
                            if (move.ToPos == toPos)//если фигура может сделать ход в эту клетку
                            {
                                pretendentMoves.Add(move);//добавляем его в список потенциальных ходов
                            }
                        }
                    }
                    switch (pretendentMoves.Count)//смотрим на количесвтов верных ходов
                    {
                        case 0://таких фигур нет - ошибка в описании 
                            MakeMove(pretendentMoves[0]);
                            break;
                        case 4://4 превращения 
                            Move move;
                            if (piecetype != PieceType.Pawn)//превращение
                            {
                                move = new PawnPromotion(pretendentMoves[0].FromPos, pretendentMoves[0].ToPos, piecetype);
                            }
                            else
                            {
                                move = pretendentMoves[0];
                            }
                            MakeMove(move);//этот ход делается
                            break;
                        default:
                            moveStr = moveStr.Replace("x", "");
                            int col = Math.Abs((int)moveStr[0] - 97);
                            IEnumerable<Move> FindMove = pretendentMoves.Where(move => move.FromPos.Column == col);
                            if (piecetype != PieceType.Pawn)//превращение
                            {
                                move = new PawnPromotion(FindMove.First().FromPos, FindMove.First().ToPos, piecetype);
                            }
                            else
                            {
                                move = FindMove.First();
                            }
                            MakeMove(move);//этот ход делается
                            break;

                    }


                }
            }
            else
            {
                Position toPos = new Position(moveStr.Substring(moveStr.Length - 2));//получение позиции от 2 последних букв хода
                List<Move> pretendentMoves = new List<Move>();
               
                foreach (Position pos in pieces)
                {
                    IEnumerable<Move> LegalMoves = LegalMovesForPiece(pos);//список ходов для фигуры определенного типа
                    if (moveStr == "Nf6")
                    {
                        AnalysMove anal = analysMove;
                    }
                    foreach (Move move in LegalMoves)
                    {
                        if (move.ToPos == toPos)//если фигура может сделать ход в эту клетку
                        {
                            pretendentMoves.Add(move);//добавляем его в список потенциальных ходов
                        }
                    }

                }
                
                switch (pretendentMoves.Count)//смотрим на количесвтов фигур способных на этот ход
                {
                    case 0://таких фигур нет - ошибка в описании 
                        MakeMove(pretendentMoves[0]);
                        break;
                    case 1://только одна фигура может сделать этот ход
                        MakeMove(pretendentMoves[0]);//этот ход делается
                        break;
                    default://если ход может выполнить несколько фигур
                        string explanation = "";
                        explanation = moveStr.Substring(1,moveStr.Length-3);//?
                        explanation = explanation.Replace("x", ""); //удаление из строки символа "рубки фигуры"
                        IEnumerable<Move> FindMove;
                        if (explanation.Length==2)
                        {

                            Position from = CurrentBoard.FindPiece(CurrentPlayer, piecetype, explanation);
                            FindMove = pretendentMoves.Where(move => move.FromPos == from);
                        }
                        else
                        {
                            if (char.IsDigit(explanation[0]))//если цифра
                            {
                                int rows = Math.Abs(8 - (int)Char.GetNumericValue(explanation[0]));
                                FindMove = pretendentMoves.Where(move => move.FromPos.Row == rows);

                            }
                            else//если буква
                            {
                                int col = Math.Abs((int)explanation[0] - 97);
                                FindMove = pretendentMoves.Where(move => move.FromPos.Column == col);
                            }
                        }

                        MakeMove(FindMove.First());
                        break;
                
                }
            }


        }
        public void MakeMove(Move move)//совершение хода
        {
            CurrentBoard.SetPawnPosition(CurrentPlayer, null);
            AnalysMove newAnalysMove;
            Position from,to;

            from = WatchFromWhite ? move.FromPos : new Position(Math.Abs(move.FromPos.Row - 7), Math.Abs(move.FromPos.Column - 7));
            to = WatchFromWhite ? move.ToPos : new Position(Math.Abs(move.ToPos.Row - 7), Math.Abs(move.ToPos.Column - 7));

            int newMoveNumb = analysMove.MoveNumb;
            newMoveNumb = CurrentPlayer == Player.Black ? newMoveNumb+1 : newMoveNumb;

            newAnalysMove = new AnalysMove(SetNameAndExecuteMove(move), analysMove, new List<AnalysMove>(), from, to, newMoveNumb);

            if (analysMove.NextMoves.Any(s => s.MoveName == newAnalysMove.MoveName)) //если в ветке есть такой ход с таким именем
            {
                analysMove = analysMove.NextMoves[analysMove.NextMoves.FindIndex(s => s.MoveName == newAnalysMove.MoveName)];//то находим эту ветку и перехдим к ней

                CurrentPlayer = CurrentPlayer.Opponent();
            }
            else //если ход не делался раньше в ветке
            {
                analysMove.NextMoves.Add(newAnalysMove);//добавляем его в 

                CurrentPlayer = CurrentPlayer.Opponent();

                analysMove = newAnalysMove;
            } 
            UpdateStateString();//обновление FEN для текущего analysMove
            CheckForGameOver();
        }
        public void SetMoveByNumber(int numb)
        {
            analysMove = analysMove.SearchMove(numb);
            CheckForGameOver();
        }
        public string MakeFullStrHistory()
        {
            string res="";
            AnalysMove current = this.analysMove;
            while (current.LastMove != null)//возвращаемся к первому элементу
            {
                current = current.LastMove;
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
                                    move = new Castle(MoveType.CastleKS, from, WatchFromWhite);
                                    
                                }
                                else
                                {
                                    move = new Castle(MoveType.CastleQS, from, WatchFromWhite); 
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
            if (WatchFromWhite) res = new Position(Math.Abs((int)Char.GetNumericValue(coords[1]) - 8),Math.Abs(coords[0] - 97) );//со стороны белых
            else
            {
                res = new Position(Math.Abs((int)Char.GetNumericValue(coords[1]) - 1), Math.Abs(coords[0] - 104));
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
        public string FromCoordsToStr(Position pos,bool WatchFromWhite)
        {
            string res = "";
            char letter;
            string digit;

            if (WatchFromWhite) { 
            letter = (char)(97+pos.Column);

            digit = Math.Abs(8 - pos.Row).ToString();
            }
            else
            {
                letter = (char)(104 - pos.Column);

                digit = Math.Abs(1 + pos.Row).ToString();
            }

            res = letter + digit;

            return res;
        }
        public void FromFenToGameBoard()
        {
            // rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1
            
            string sb = analysMove.FenAfterMove;
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
                                    if(counterRow == 1)piece = new Pawn(Player.Black, true,false); //пешка не ходила
                                    else { piece = new Pawn(Player.Black, true, true); }
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
                                    if (counterRow == 6) piece = new Pawn(Player.White, true, false); //пешка не ходила
                                    else { piece = new Pawn(Player.White, true, true); }
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
                            board[counterRow, countercColumn] = piece;
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
                        board.pawnSkipPositions[current.Opponent()] = new Position(Math.Abs(8 - (int)Char.GetNumericValue(sb[i+1])), Math.Abs((int)sb[i]-97));//g6 = 2 6| a6 - 2 0 | a-97 b-98 h-104 g-103| b3 = 4 1
                        break;
                    }
                    
                }
            }
            CurrentPlayer = current;

            if (WatchFromWhite) CurrentBoard = board;
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
            analysMove.FenAfterMove = new StateString(CurrentPlayer, CurrentBoard, WatchFromWhite).ToString();

           
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
