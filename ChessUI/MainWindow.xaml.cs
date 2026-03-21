using ChessEngine;
using ChessLogic;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
        private static string enginePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Engines", "stockfish.exe");
        private IStockfish stockfish = new ChessEngine.Core.Stockfish(enginePath);
        private static GameState gameState ;
        private Position selectedPos = null;
        private bool isBoardReversed = false;

        private string path="";


        public MainWindow()
        {
            InitializeComponent();
            InitializeBoard();
            BestMovesButtonsEnabled();
            gameState = new GameState(Player.White,Board.Initial());//инициализация доски, где первым начинают белые с начального положения фигур
            DrawBoard(gameState.CurrentBoard);
            FillFENTextBox();
            SetBestMoves(gameState.CurrentPlayer);//асинхронная функция 
        }
        public MainWindow(string StartFEN)
        {
            InitializeComponent();
            InitializeBoard();
            BestMovesButtonsEnabled();
            gameState = new GameState(Player.White, Board.Initial());//инициализация доски, где первым начинают белые с начального положения фигур
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
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    // Если доска перевернута, берем фигуры с противоположных индексов
                    int logicRow = isBoardReversed ? 7 - r : r;
                    int logicCol = isBoardReversed ? 7 - c : c;

                    Piece piece = board[logicRow, logicCol];
                    pieceImages[r, c].Source = Images.GetImage(piece);
                    highLights[r, c].Fill = Brushes.Transparent;
                }
            }
        }
        private void OnFromPositionSelected(Position pos)
        {
            IEnumerable<Move> moves = gameState.LegalMovesForPiece(pos);
            if (moves.Any())
            {
                selectedPos = pos;
                CacheMoves(moves);
                ShowHighlights();
            }
        }

        private void OnToPositionSelected(Position pos)
        {
            HideHighlights();

            if (moveCache.TryGetValue(pos, out Move move))
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
                if (selectedPos == pos)
                {
                    selectedPos = null;
                }
                else
                {
                    OnFromPositionSelected(pos);
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
            gameState.MakeMove(move); 
            UpdateUI();
        }

        private void UpdateUI()
        {
            DrawBoard(gameState.CurrentBoard);
            FillPGN();
            FillFENTextBox();
            RefreshAnalysMoves(); 
            ShowLastMoves();
            ShowCheck();

            if (gameState.Result != null) NotifyEndOfGame();
        }
        private void FillAllInformationAboutMove()
        {
            BestMovesButtonsEnabled();
            FillFENTextBox();
            DrawBoard(gameState.CurrentBoard); // Перерисовываем доску
            ShowLastMoves();                   // Подсвечиваем клетки
            ShowCheck();                       // Проверяем шах
            PaintButton(gameState.CurrentAnalysisNode);

            if (gameState.IsGameOver())
            {
                NotifyEndOfGame();
            }

            FilledCommentTextBox();
        }

        private void FillFENTextBox()
        {
            FENTextBox.Text = gameState.CurrentAnalysisNode.FenAfterMove.Position;
        }

        private void NotifyEndOfGame()
        {
            gameState.CurrentAnalysisNode.UserComment = gameState.Result.ToString();
        }
        private void FillPGN()
        {
            AnalysMove root = gameState.CurrentAnalysisNode;
            while (root.PreviousMove != null) root = root.PreviousMove;

            PGNTextBox.Text = root.PrintBranch();

        }
        private void ShowCheck()
        {
            if (gameState.CurrentBoard.IsInCheck(gameState.CurrentPlayer))
            {
                Position pos = gameState.CurrentBoard.FindPiece(gameState.CurrentPlayer,PieceType.King);
                highLights[pos.Row, pos.Column].Fill = new SolidColorBrush(Color.FromRgb(234,83,69));
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

            if (selectedPos == null)
            {
                OnFromPositionSelected(pos);
            }
            else
            {
                OnToPositionSelected(pos);
            }
        }


        private Position ToSquarePosition(Point point)
        {
            double squareSize = BoardGreed.ActualWidth / 8;
            int r = (int)(point.Y / squareSize); 
            int c = (int)(point.X / squareSize); 

            if (isBoardReversed)
            {
                return new Position(7 - r, 7 - c); 
            }

           
            return new Position(r, c);
        }


        private void CacheMoves(IEnumerable<Move> moves)
        {
            moveCache.Clear();
            foreach (Move move in moves)
            {
                moveCache[move.ToPos] = move;
            }
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
                highLights[to.Row, to.Column].Fill = Brushes.Transparent;
            }
            ShowLastMoves();
        }        
        private void ShowLastMoves()
        {

            var node = gameState.CurrentAnalysisNode;
            if (node.LastFrom == null || node.LastTo == null) return;

            HighlightLogicalSquare(node.LastFrom, new SolidColorBrush(Color.FromArgb(159, 188, 255, 17)));
            HighlightLogicalSquare(node.LastTo, new SolidColorBrush(Color.FromArgb(159, 188, 255, 17)));
        }
        private void HighlightLogicalSquare(Position pos, Brush brush)
        {
            int r = isBoardReversed ? 7 - pos.Row : pos.Row;
            int c = isBoardReversed ? 7 - pos.Column : pos.Column;
            highLights[r, c].Fill = brush;
        }

        private bool IsMenuOnScreen()
        {
            return MenuContainer.Content != null;
        }

        private void RestartGame()
        {
            selectedPos = null;
            HideHighlights();
            moveCache.Clear();
            gameState = new GameState(Player.White, Board.Initial());
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

        private void FromFENToPosition()
        {
            gameState.CurrentAnalysisNode.FenAfterMove.Position = FENTextBox.Text;
            DrawBoard(gameState.CurrentBoard);
        }

        private void FromFENToStartPosition()
        {
            gameState = new GameState(Player.White, Board.Initial());
            gameState.CurrentAnalysisNode.FenAfterMove.Position = FENTextBox.Text;
            DrawBoard(gameState.CurrentBoard);
        }
        private void ShowPauseMenu()
        {
            PauseMenu pauseMenu = new PauseMenu();
            MenuContainer.Content = pauseMenu;

            pauseMenu.OptionSelected += option =>
            {
                MenuContainer.Content = null;
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
            isBoardReversed = !isBoardReversed;

            string asset = isBoardReversed ? "boardb.png" : "boardw.png";
            BoardGreed.Background = new ImageBrush(new BitmapImage(new Uri($"pack://application:,,,/Assets/{asset}")));
            FillAllInformationAboutMove();
        }


        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (gameState.CurrentAnalysisNode.PreviousMove != null)
            {
                gameState.SetMoveByNumber(gameState.CurrentAnalysisNode.PreviousMove.Number);
                FillAllInformationAboutMove();
            }
        }


        private void ForwardButton_Click(object sender, RoutedEventArgs e)//кнопка перехода к следующему ходу
        {
            if (gameState.CurrentAnalysisNode.PreviousMove != null)
            {
                gameState.SetMoveByNumber(gameState.CurrentAnalysisNode.Number + 1);
                FillAllInformationAboutMove();
            }
        }

        private void RefreshAnalysMoves()
        {
            AnalysWrapPanel.Children.Clear();

            AnalysMove startAnalysMove = gameState.CurrentAnalysisNode;
            while (startAnalysMove.PreviousMove != null)//возвращаемся к первому элементу
            {
                startAnalysMove = startAnalysMove.PreviousMove;
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
                        button.Name = "b" + futureMove.GetNumber();
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
                        button.Name = "b" + futureMove.GetNumber();
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

        private void AnalysMoveButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            int moveNumber = int.Parse(button.Name.Substring(1));

            gameState.SetMoveByNumber(moveNumber);
            FillAllInformationAboutMove();

            PaintButton(gameState.CurrentAnalysisNode);
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
        private void ComputerMoveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (First.IsEnabled)
                {
                    string moveSan = First.Name; 

                    UpdateUI();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка хода движка: {ex.Message}");
            }
        }
        private void FilledCommentTextBox()
        {
            CommentTextBox.Text = gameState.CurrentAnalysisNode.UserComment;
        }

        private void CommentTextBox_Changed(object sender, TextChangedEventArgs e)
        {
            if (gameState?.CurrentAnalysisNode != null)
            {
                gameState.CurrentAnalysisNode.UserComment = CommentTextBox.Text;
                // Обновляем PGN, так как комментарии — его часть
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
                return stockfish.GetBestMoves(gameState.CurrentAnalysisNode.FenAfterMove.Position);
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
        private void MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Button button && button.Name.Length >= 4)
            {
                // Используем ChessParser вместо старого FromStrToPos
                Position from = MoveParser.ToPosition(button.Name.Substring(0, 2));
                Position to = MoveParser.ToPosition(button.Name.Substring(button.Name.Length - 2, 2));

                MakeArrow(from, to);
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
        private void MouseClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                string moveStr = button.Name; // Здесь обычно лежит ход в формате "e2e4"
                BestMovesButtonsEnabled();

                try
                {
                    gameState.MakeMove(moveStr); 
                    UpdateUI();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Не удалось выполнить ход компьютера: {ex.Message}");
                }
            }
        }
        private void SaveFile()
        {
            AnalysMove root = gameState.CurrentAnalysisNode;
            while (root.PreviousMove != null) root = root.PreviousMove;
            string pgnOutput = root.PrintBranch();

            if (string.IsNullOrEmpty(path))
            {
                SaveFileDialog dlg = new SaveFileDialog { Filter = "PGN files (*.pgn)|*.pgn|Text files (*.txt)|*.txt" };
                if (dlg.ShowDialog() == true) path = dlg.FileName;
                else return;
            }

            File.WriteAllText(path, pgnOutput);
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
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
            OpenFileDialog fileDialog = new OpenFileDialog { Filter = "Chess Analysis (*.pgn;*.txt)|*.pgn;*.txt" };
            if (fileDialog.ShowDialog() == true)
            {
                try
                {
                    path = fileDialog.FileName;
                    string content = File.ReadAllText(path);

                    UpdateUI();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки: {ex.Message}");
                }
            }
        }
        private void NewFileMake_Click(object sender, RoutedEventArgs e)
        {
            BestMovesButtonsEnabled();
            SaveFile();
            path = string.Empty;
            gameState = new GameState(Player.White, Board.Initial());//инициализация доски, где первым начинают белые с начального положения фигур
            DrawBoard(gameState.CurrentBoard);
            RefreshAnalysMoves();
            FillFENTextBox();//перевод текущей позиции в FEN
            FillPGN();

        }
        
    }

   
}