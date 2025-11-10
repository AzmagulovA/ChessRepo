using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using ChessLogic;
using ChessEngine;
using static ChessUI.MainWindow;
using System.Windows.Ink;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel;
using System.IO;
using Microsoft.Win32;
using System;
using System.Security.Cryptography;

namespace ChessUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private readonly Image[,] pieceImages = new Image[8,8];//картинки для фигур
        private readonly Rectangle[,] highLights = new Rectangle[8, 8];//квадраты возможных ходов
        private readonly Dictionary<Position,Move> moveCache = new Dictionary<Position,Move>();//возможные ходы selected pos
        private string bestMove = "";
        private IStockfish stockfish = new ChessEngine.Core.Stockfish(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"stockfish-windows-x86-64-avx2.exe"));
       // private IStockfish stockfish = new ChessEngine.Core.Stockfish(System.IO.Path.Combine(Environment.CurrentDirectory, @"Assets\stockfish-windows-x86-64-avx2.exe"));
        private GameState gameState ;
        private Position selectedPos = null;
        private string path="";


        public MainWindow()
        {
            InitializeComponent();
            InitializeBoard();
            BestMovesButtonsEnabled();
            gameState = new GameState(Player.White,Board.Initial(),true);//инициализация доски, где первым начинают белые с начального положения фигур
            DrawBoard(gameState.CurrentBoard);
            FillFENTextBox();
            SetBestMoves(gameState.CurrentPlayer);//асинхронная функция 
        }
        public MainWindow(string StartFEN)
        {
            InitializeComponent();
            InitializeBoard();
            BestMovesButtonsEnabled();
            gameState = new GameState(Player.White, Board.Initial(), true);//инициализация доски, где первым начинают белые с начального положения фигур
            FENTextBox.Text = StartFEN;
            FromFENToStartPosition();
            SetBestMoves(gameState.CurrentPlayer);//асинхронная функция 
        }
        private void InitializeBoard()
        {

            for (int row = 0; row < 8; row++)
            {
                for(int col = 0; col < 8; col++)
                {
                    Image image = new Image();
                    pieceImages[row,col] = image;
                    PieceGrid.Children.Add(image);

                    Rectangle highlight = new();
                    highLights[row, col] = highlight;
                    HighLightGrid.Children.Add(highlight);

                }

            }

        }
        private void DrawBoard(Board board)
        {
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    Piece piece = board[row, col];
                    pieceImages[row, col].Source = Images.GetImage(piece);
                    highLights[row, col].Fill = Brushes.Transparent;

                }
            }


        }
        private void OnFromPositionSelected(Position pos)//из выбранной позиции
        {
            IEnumerable<Move> moves = gameState.LegalMovesForPiece(pos);//показываем текущие возможные ходы
            if (moves.Any())
            {
                selectedPos = pos;
                CacheMoves(moves);
                ShowHighlights();
            }
        }

        private void OnToPositionSelected(Position pos)//на выбранную позицию
        {
            HideHighlights();

            if (moveCache.TryGetValue(pos, out Move move))//попытка перейти по выбранной клетке
            {
                selectedPos = null;
                
                if (move.Type == MoveType.PawnPromotion)
                {
                    HandlePromotion(move.FromPos,move.ToPos);
                }
                else
                {
                    HandleMove(move);
                }
            }
            else
            {
                if (selectedPos == pos)//если игрок нажал на фигуру во второй раз, то он хочет скрыть его ходы
                {
                    selectedPos = null;
                }
                else
                {
                    OnFromPositionSelected(pos);//если игрок не нажал на указанные позиции, то значит он хочет сходить за другую фигуру
                }
            }
        }
        private void HandlePromotion(Position from, Position to)
        {
            pieceImages[to.Row, to.Column].Source = Images.GetImage(gameState.CurrentPlayer, PieceType.Pawn);
            pieceImages[from.Row, from.Column].Source = null;

            PromotionMenu promMenu = new PromotionMenu(gameState.CurrentPlayer);
            MenuContainer.Content = promMenu;

            promMenu.PieceSelected += type =>
            {
                MenuContainer.Content = null;
                Move promMove = new PawnPromotion(from, to, type);
                HandleMove(promMove);
            };
        }
        private void HandleMove(Move move)
        {
            gameState.MakeMove(move);//совершение хода
            FillPGN();//запись хода
            RefreshAnalysMoves();
            FillAllInformationAboutMove();//после любого хода следует заполнение информации об этом ходе
            
        }
        private void FillAllInformationAboutMove()
        {
            BestMovesButtonsEnabled();
            FillFENTextBox();//перевод текущей позиции в FEN
            FromFENToPosition();
            ShowLastMoves();//отрисовка последних ходов
            ShowCheck();//отрисовка шаха
            PaintButton(gameState.analysMove);
            if (gameState.IsGameOver())//закончилась игра
            {
                NotifyEndOfGame();
            }
            CommentTextBox.Text = string.Empty;
            FilledCommentTextBox();
        }
        private void FillPGN()//заполнение блока ходов
        {
            PGNTextBox.Text = gameState.MakeFullStrHistory();

        }
        private void ShowCheck()
        {

            if (gameState.CurrentBoard.IsInCheck(gameState.CurrentPlayer))//если текущий игрок под шахом
            {
                Position pos = gameState.CurrentBoard.FindPiece(gameState.CurrentPlayer,PieceType.King);//находим позицию короля 
                highLights[pos.Row, pos.Column].Fill = new SolidColorBrush(Color.FromRgb(234,83,69));//клетка окрашиваеся в красный
            }
        }
        private void BoardGrid_MouseDown(object sender,MouseButtonEventArgs e)//нажатие на сетку
        {
            if (IsMenuOnScreen())
            {
                return;
            }

            Point point = e.GetPosition(BoardGreed);
            Position pos = ToSquarePosition(point);

            if (selectedPos == null)//если ничего не выбрано
            {
                OnFromPositionSelected(pos);
            }
            else//если что-то выбрано
            {
                OnToPositionSelected(pos);
            }
        }
        

        private Position ToSquarePosition(Point point)//метод для определения нажатого квадрата
        {
            double squareSize = BoardGreed.ActualWidth / 8;
            int row = (int)(point.Y / squareSize);
            int col = (int)(point.X / squareSize);
            return new Position(row, col);
        }
        private void CacheMoves(IEnumerable<Move> moves)//ходы для 
        {
            moveCache.Clear();
            foreach (Move move in moves)
            {
                moveCache[move.ToPos] = move;//все to pos ставим пустое значение
            }
        }
        private void FillFENTextBox()
        {
            FENTextBox.Text = gameState.analysMove.FenAfterMove.ToString();
        }
        private void ShowHighlights()
        {
            Color color = Color.FromArgb(150,125,255,125);
            foreach (Position to in moveCache.Keys)
            {
                highLights[to.Row, to.Column].Fill = new SolidColorBrush(color);
            }

        }
        private void HideHighlights()
        {
            foreach (Position to in moveCache.Keys)
            {
                highLights[to.Row, to.Column].Fill = Brushes.Transparent;//все клетки прозрачные
            }
            ShowLastMoves();
        }        
        private void ShowLastMoves()
        {
            
            if (gameState.analysMove.LastFrom != null) 
                highLights[gameState.GetLastMoveFrom().Row, gameState.GetLastMoveFrom().Column].Fill = new SolidColorBrush(Color.FromArgb(159, 188, 255, 17));
            if (gameState.analysMove.LastTo != null)
                highLights[gameState.GetLastMoveTo().Row, gameState.GetLastMoveTo().Column].Fill = new SolidColorBrush(Color.FromArgb(159, 188, 255, 17));

        }
        private bool IsMenuOnScreen()
        {
            return MenuContainer.Content != null;
        }
        private void NotifyEndOfGame()
        {
            gameState.analysMove.UserComment = gameState.Result.ToString();

        }
        private void RestartGame()
        {
            selectedPos = null;
            HideHighlights();
            moveCache.Clear();
            gameState = new GameState(Player.White, Board.Initial(),true);
            DrawBoard(gameState.CurrentBoard);
            FillFENTextBox();
            ClearPGNTextBox();
            

        }
        private void ClearPGNTextBox()
        {
            PGNTextBox.Clear();
        }
        private void Window_Keydown(object sender, KeyEventArgs e)
        {
            if(!IsMenuOnScreen()&& e.Key == Key.Escape)
            {
                ShowPauseMenu();
            }
            if (e.Key == Key.Enter)
            {
                FromFENToPosition();
            }
        }

        private void FromFENToPosition()//если вводится новая нотация, то меняется всё состояние игры
        {
            gameState.analysMove.FenAfterMove = FENTextBox.Text;
            gameState.FromFenToGameBoard();
            DrawBoard(gameState.CurrentBoard);
        }
        private void FromFENToStartPosition()//если вводится новая нотация, то меняется всё состояние игры
        {
            gameState.analysMove = new AnalysMove(FENTextBox.Text);
            gameState.FromFenToGameBoard();
            DrawBoard(gameState.CurrentBoard);
        }
        private void ShowPauseMenu()
        {
            PauseMenu pauseMenu = new PauseMenu();
            MenuContainer.Content = pauseMenu;

            pauseMenu.OptionSelected += option =>
            {
                MenuContainer.Content = null;//скрываем главное окно
                if (option == Option.Restart)
                {
                    RestartGame();
                }
            };
        }

        private void Revers_Click(object sender, RoutedEventArgs e)//кнопка переворота доски
        {
            ReversBoard();
        }
        private void ReversBoard()
        {
            gameState.GameStateReversBoard();
            if (gameState.WatchFromWhite)
                BoardGreed.Background = new ImageBrush(new BitmapImage(new Uri(BaseUriHelper.GetBaseUri(this), "Assets/boardw.png")));
            else
            {
                BoardGreed.Background = new ImageBrush(new BitmapImage(new Uri(BaseUriHelper.GetBaseUri(this), "Assets/boardb.png")));
            }
            DrawBoard(gameState.CurrentBoard);
            ShowLastMoves();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)//кнопка возврата к предыдущему ходу
        {

            if (gameState.analysMove.LastMove != null)//смотрим предыдущий ход 
            { 
                gameState.analysMove = gameState.analysMove.LastMove;
                gameState.CheckForGameOver();
                FillAllInformationAboutMove();
            }

        }
        private void ForwardButton_Click(object sender, RoutedEventArgs e)//кнопка перехода к следующему ходу
        {
            if (gameState.analysMove.NextMoves.Any()) //смотрим следующий ход 
            { 
                gameState.analysMove = gameState.analysMove.NextMoves[0];
                gameState.CheckForGameOver();
                FillAllInformationAboutMove();
            }
        }
        private void RefreshAnalysMoves()
        {
            AnalysWrapPanel.Children.Clear();

            AnalysMove startAnalysMove = gameState.analysMove;
            while (startAnalysMove.LastMove != null)//возвращаемся к первому элементу
            {
                startAnalysMove = startAnalysMove.LastMove;
            }
            AddBranch(startAnalysMove);
        }
        private void AddBranch(AnalysMove branch)
        {
            bool mainOpt = true;
            AnalysMove mainBranch=null;
            if (branch.NextMoves.Count > 0) //если есть последующие ходы
            {
                foreach (AnalysMove futureMove in branch.NextMoves)//проходимся по каждому из них
                {
                    if (branch.NextMoves.Count == 1)//если в ветке 1 элемент
                    {

                        Button button = new Button();
                        button.Background = System.Windows.Media.Brushes.White;
                        button.BorderBrush = System.Windows.Media.Brushes.White;
                        button.Content = futureMove.MoveName;//печатаем его
                        button.Name = "b"+futureMove.GetNumber().ToString();
                        //button.Content = futureMove.GetNumber().ToString();
                        button.Click += AnalysMoveButton_Click;
                        AnalysWrapPanel.Children.Add(button);
                        AddBranch(futureMove);//переходим к его предку
                    }
                    if (branch.NextMoves.Count > 1)//если в ветке больше 1 элемента
                    {
                        if (!mainOpt) //если ветка неосновная, то включаем её в скобки
                        {
                            Button label = new Button();
                            label.BorderBrush = System.Windows.Media.Brushes.White;
                            label.Background = System.Windows.Media.Brushes.White;
                            label.Content = "(";
                            //label.IsEnabled = false;
                            AnalysWrapPanel.Children.Add(label);
                        }

                        Button button = new Button();
                        button.Background = System.Windows.Media.Brushes.White;
                        button.BorderBrush = System.Windows.Media.Brushes.White;
                        button.Name = "b"+futureMove.GetNumber().ToString(); 
                       // button.Content = futureMove.GetNumber().ToString();
                        button.Click += AnalysMoveButton_Click;
                        button.Content = futureMove.MoveName;//печатаем его
                        AnalysWrapPanel.Children.Add(button);

                        if (mainOpt) //если основная ветка, то сохраняем чтобы вывести ее позже
                        { 
                            mainBranch = futureMove;
                        }
                        if (!mainOpt)//рекурсия, если ветка не главная, то печатаем ее до конца
                        { 
                            AddBranch(futureMove);
                        }

                        if (!mainOpt) 
                        {
                            Button label = new Button();
                            label.BorderBrush = System.Windows.Media.Brushes.White;
                            label.Background = System.Windows.Media.Brushes.White;
                            label.Content = ")";
                            //label.IsEnabled = false;
                            AnalysWrapPanel.Children.Add(label);
                        }
                    }
                    mainOpt = false;
                }

                if (mainBranch != null) 
                {
                    AddBranch(mainBranch);
                }

            }


        }

        private void AnalysMoveButton_Click(object sender, RoutedEventArgs e) //Event which will be triggerd on click of ya button
        {

            var button = sender as Button;
            string NameButton = button.Name;
            BestMovesButtonsEnabled();
            NameButton = NameButton.Substring(1);//work
            int NumbButton = Convert.ToInt32(NameButton);
            gameState.SetMoveByNumber(NumbButton);
            //gameState.analysMove = gameState.analysMove.SearchMove(NumbButton);
            //CommentTextBox.Text = gameState.analysMove.UserComment;
            FillAllInformationAboutMove();
            FillPGN();

        }
        
        private void PaintButton(AnalysMove analysMove)
        {
            foreach (Button but in AnalysWrapPanel.Children)
            {
                but.Background = System.Windows.Media.Brushes.White;
            }
            Button a = AnalysWrapPanel.Children.OfType<Button>().FirstOrDefault(x => x.Name == "b" + analysMove.Number.ToString());

            if (a != null) a.Background = new SolidColorBrush(Color.FromArgb(255,200,223,244)); //System.Windows.Media.Brushes.; 
        }
        private void ComputerMoveButton_Click(object sender, RoutedEventArgs e) //выполнение лучшего шахматного хода
        {
            try 
            {
                if (First.IsEnabled == true)
                {
                    string result = First.Name;//лучший ход

                    this.First.IsEnabled = false;

                    this.Second.IsEnabled = false;

                    this.Third.IsEnabled = false;

                    gameState.MakeComputerMove(result);
                    FillPGN();//запись хода
                    RefreshAnalysMoves();
                    FillAllInformationAboutMove();
                }
            }
            catch
            {
                MessageBox.Show("Ошибка! Повторите попытку");
            }
                
           
        }
        private void FilledCommentTextBox()
        {
            CommentTextBox.Text = gameState.analysMove.UserComment;
            Keyboard.ClearFocus();
        }
        private void CommentTextBox_Changed(object sender, TextChangedEventArgs e)
        {
            if (gameState!=null)
            {
                gameState.analysMove.UserComment = CommentTextBox.Text;
                FillPGN();
            }
                
        }
        
        private string SetEvalFromStr(string str,Player CurrentPlayer)
        {
            string res = "";
            if (CurrentPlayer == Player.Black)
            {
                if (str[0]=='-')
                {
                    res = (double.Parse(str.Substring(1))/100).ToString();
                    res = '+' + res;
                }
                else
                {
                    res = (double.Parse(str)/100).ToString();
                    res = '-' + res;
                }
            }
            else
            {
                if(str[0] == '-')
                {
                    res = (double.Parse(str.Substring(1)) / 100).ToString();
                    res = '-' + res;
                }
                else
                {
                    res = (double.Parse(str) / 100).ToString();
                    res = '+' + res;

                }
            }
            return res;
        }
        private async void SetBestMoves(Player player)//паралельно ставим лучшие ходы
        {
            List<List<string>> Text = null;
            Text = await Task.Run(() => GetBestMoves());//передача токена в задачу, которая может быть отменена
            /*if (Text.Count<3)
            {
                this.First.Content = "";
                this.Second.Content = "";
                this.Third.Content = "";
                BestMovesButtonsEnabled();
            }*/
            while (Text.Count>3)//оставляем в коллекции только 3 лучших хода
            {
                Text.RemoveAt(0);
            }
                string Moves = "";
                string firstMoveName = "";
            
            for (int i = 0; i < Text.Count; i++)//проход по строкам
                {
                    if (Text[i][0] == "cp")//первый эелмент строки - оценка?
                    {
                        Moves = SetEvalFromStr(Text[i][1], player) +" ";//преобразуем оценку в число
                        if (i == 0) Ocenka.Content = SetEvalFromStr(Text[i][1], player);
                        firstMoveName = Text[i][2];
                        for (int j = 2; j < Text[i].Count; j++)//проход по словам
                        {
                            Moves = Moves + Text[i][j]+" ";
                        }
                    }
                    else//виден мат 
                    {
                        Moves = "#";
                        firstMoveName = Text[i][2];
                        for (int j = 1; j < Text[i].Count; j++)//проход по словам
                        {
                            Moves = Moves + Text[i][j] + " ";
                        }
                    }
                    switch (i)
                    {
                        case 0:
                        if (First.Name != firstMoveName)
                        {
                            this.First.Content = Moves;
                            this.First.Name = firstMoveName;
                            this.First.IsEnabled = true;
                        }
                        break;
                        case 1:
                        if (Second.Name != firstMoveName)
                        {
                            this.Second.Content = Moves;
                            this.Second.Name = firstMoveName;
                            this.Second.IsEnabled = true;
                        }
                        break;
                        case 2:
                        if (Third.Name != firstMoveName)
                        {
                            this.Third.Content = Moves;
                            this.Third.Name = firstMoveName;
                            this.Third.IsEnabled = true;
                        }
                        break;
                        default:
                            break;
                    }
                }

            SetBestMoves(gameState.CurrentPlayer);


        }
        private  List<List<string>> GetBestMoves()
        {
            try
            {
                return stockfish.GetBestMoves(gameState.analysMove.FenAfterMove);
            }
            catch
            {
                MessageBox.Show("Ошибка! Повторите попытку");
                return null;
            }


        }
        private Point GetPointInBoardGreed(Position pos)
        {
            double squareSize = BoardGreed.ActualWidth / 8; //размер квадрата
            System.Windows.Point point = new Point( (pos.Column) * squareSize + squareSize / 2, (pos.Row) * squareSize + squareSize / 2);

            return point;
        }
        private void MouseEnter(object sender, MouseEventArgs e)//событие наведения мыши на second 
        {
            var button = sender as Button;
            string NameButton = button.Name;
            Position to ;
            Position from;
            if (NameButton.Length==4)
            {
                from = gameState.FromStrToPos(NameButton.Substring(0, 2));
                to = gameState.FromStrToPos(NameButton.Substring(NameButton.Length - 2, 2));
            }
            else
            {
                from = gameState.FromStrToPos(NameButton.Substring(0, 2));
                to = gameState.FromStrToPos(NameButton.Substring(NameButton.Length - 3, 2));

            }
            try
            {
                MakeArrow(from, to);
            }
            catch 
            {
                BestMovesButtonsEnabled();
            }
            
        }
        private void MakeArrow(Position from,Position to)
        {
            
                double squareSize = BoardGreed.ActualWidth / 8;//размер одного квадрата
                System.Windows.Point pointFrom = GetPointInBoardGreed(from);
                System.Windows.Point pointTo = GetPointInBoardGreed(to);

                Line myline = new Line();
                myline.Stroke = Brushes.Red;
                myline.StrokeThickness = 5;

                myline.X1 = pointFrom.X;
                myline.Y1 = pointFrom.Y;
                myline.X2 = pointTo.X;
                myline.Y2 = pointTo.Y;
                BoardGreed.Children.Add(myline);

                double d = Math.Sqrt(Math.Pow(myline.X2 - myline.X1, 2) + Math.Pow(myline.Y2 - myline.Y1, 2));

                double X = pointTo.X - pointFrom.X;
                double Y = pointTo.Y - pointFrom.Y;

                double X3 = pointTo.X - (X / d) * 25;
                double Y3 = pointTo.Y - (Y / d) * 25;

                double Xp = pointTo.Y - pointFrom.Y;
                double Yp = pointFrom.X - pointTo.X;

                double X4 = X3 + (Xp / d) * 5;
                double Y4 = Y3 + (Yp / d) * 5;
                double X5 = X3 - (Xp / d) * 5;
                double Y5 = Y3 - (Yp / d) * 5;

                Line secmyline = new Line
                {
                    Stroke = Brushes.Red,
                    StrokeThickness = 6,
                    X1 = pointTo.X - (X / d) * 10,
                    Y1 = pointTo.Y - (Y / d) * 10,
                    X2 = X4,
                    Y2 = Y4
                };
                BoardGreed.Children.Add(secmyline);

                Line thmyline = new Line
                {
                    Stroke = Brushes.Red,
                    StrokeThickness = 6,
                    X1 = pointTo.X - (X / d) * 10,
                    Y1 = pointTo.Y - (Y / d) * 10,
                    X2 = X5,
                    Y2 = Y5
                };
                BoardGreed.Children.Add(thmyline);
           
        }
        private void MouseLeave(object sender, MouseEventArgs e)//событие наведения мыши на second 
        {
            
             BoardGreed.Children.RemoveRange(BoardGreed.Children.Count - 3, 3);//удаление первых трех элементов (стрелку)
           
        }
        private void BestMovesButtonsEnabled()
        {
            this.First.IsEnabled = false;

            this.Second.IsEnabled = false;

            this.Third.IsEnabled = false;
        }
        private void MouseClick(object sender, RoutedEventArgs e)//нажатие на одну из кнопок с лучшим ходом
        {
            var button = sender as Button;
            string NameButton = button.Name;
                BestMovesButtonsEnabled();
            try 
            { 
                gameState.MakeComputerMove(NameButton);//выполнение хода предложенным компьютером
            }
            catch
            {
                MessageBox.Show("Ошибка! Повторите попытку");
            }
                FillPGN();//запись хода
                RefreshAnalysMoves();
                FillAllInformationAboutMove();
            
        }
        private void SaveFile()
        {
            if (path!="")
            {
                AnalysMove currentMove = gameState.analysMove;
                while (currentMove.LastMove != null)//переход к первому ходу партии
                {
                    currentMove = currentMove.LastMove;
                }
                if (currentMove.FENIsStart())//если начальная позиция
                {
                    System.IO.File.WriteAllText(path, PGNTextBox.Text);
                }
                else//если нет, то записываем в файл позицию с которой всё начиналось
                {
                    string startFEN = currentMove.FenAfterMove;
                    System.IO.File.WriteAllText(path, string.Empty);
                    System.IO.File.AppendAllText(path, "[Variant \"From Position\"]\n");
                    startFEN = "[FEN \"" + startFEN + "\"]\n";
                    System.IO.File.AppendAllText(path, startFEN);
                    System.IO.File.AppendAllText(path, PGNTextBox.Text);
                }
            }
            else
            {
                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.FileName = "Document"; // Default file name
                dlg.DefaultExt = ".text"; // Default file extension
                dlg.Filter = "Text documents (.txt)|*.txt"; // Filter files by extension
                Nullable<bool> result = dlg.ShowDialog();
                if (result == true)
                {
                    path = dlg.FileName;
                }
            }
            
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)//сохранение по выбранному пути
        {
            SaveFile();
            if(path!="")MessageBox.Show("Файл сохранен");
        }

        private void NewPositionSet_Click(object sender, RoutedEventArgs e)
        {
            var newForm = new SetPositionMenu(); //create your new form.
            newForm.Show(); //show the new form.
            this.Close(); //only if you want to close the current form.
        }

        private void OpenNewFile_Click(object sender, RoutedEventArgs e)//открытие нового файла
        {
            if (path=="")//ничего не выбрано
            {
                OpenFileDialog();
            }
            else
            {
                MessageBoxResult result = MessageBox.Show("Сохранить файл?", "ChessAnalyser", MessageBoxButton.YesNoCancel);
                switch (result)
                {
                    case MessageBoxResult.Yes:
                        SaveFile();
                        OpenFileDialog();
                        break;
                    case MessageBoxResult.No:
                        OpenFileDialog();
                        break;
                    case MessageBoxResult.Cancel:
                        break;
                }
            }
            
            
        }
        private void OpenFileDialog()
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = "Text documents (.txt)|*.txt;*.pgn";
            fileDialog.Title = "Выберите анализ...";
            bool? success = fileDialog.ShowDialog();
            if (success == true)
            {
                try
                {

                    path = fileDialog.FileName;
                    string textFromFile = File.ReadAllText(path);//текст из файла
                    gameState.GameStateFromStr(textFromFile);
                    FillPGN();
                    FillAllInformationAboutMove();
                    RefreshAnalysMoves();
                }
                catch
                {
                    MessageBox.Show("Ошибка! Неверное содержание файла");
                }

            }
            else//ничего не выбрано
            {
            }
        }
        private void NewFileMake_Click(object sender, RoutedEventArgs e)
        {
            BestMovesButtonsEnabled();
            SaveFile();
            path = "";
            gameState = new GameState(Player.White, Board.Initial(), true);//инициализация доски, где первым начинают белые с начального положения фигур
            DrawBoard(gameState.CurrentBoard);
            RefreshAnalysMoves();
            FillFENTextBox();//перевод текущей позиции в FEN
            FillPGN();

        }
        
    }

   
}