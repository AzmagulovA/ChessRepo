using ChessLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Resources;
using System.Windows.Shapes;

namespace ChessUI
{
    /// <summary>
    /// Логика взаимодействия для SetPositionMenu.xaml
    /// </summary>
    public partial class SetPositionMenu : Window
    {
        private Piece selectedPiece;

        Board Board = Board.Initial();
        Position from;
        Position to;
        bool WatchFromWhite = true;
        Player CurrentPlayer = Player.White;
        bool OptionIsSelected = false;
        private readonly Image[,] pieceImages = new Image[8, 8];//картинки для фигур
        public SetPositionMenu()
        {
            InitializeComponent();
            SelectPlayerMove.ItemsSource = new string[]
            {
            "Ход Белых",
            "Ход Чёрных"
            };
            SelectPlayerMove.SelectedIndex = 0;//по стандарту первые ходят белые
            InitializeBoard();
            DrawBoard(Board);

        }
        private void InitializeBoard()
        {

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    Image image = new Image();
                    pieceImages[row, col] = image;
                    PieceGrid.Children.Add(image);
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

                }
            }


        }
        private Position ToSquarePosition(Point point)//метод для определения нажатого квадрата
        {
            double squareSize = BoardGreed.ActualWidth / 8;
            int row = (int)(point.Y / squareSize);
            int col = (int)(point.X / squareSize);
            return new Position(row, col);
        }

        private void MouseClick(object sender, RoutedEventArgs e)//выбор фигуры
        {
            var button = sender as Button;
            foreach (var child in UpUniformGrid.Children)
            {
                button = child as Button;
                button.Background = Brushes.Transparent;
            }
            foreach (var child in DownUniformGrid.Children)
            {
                button = child as Button;
                button.Background = Brushes.Transparent;
            }
            button = sender as Button;
            button.Background = new SolidColorBrush(Color.FromArgb(255, 67, 132, 208));
            string NameButton = button.Name;
            
            switch (NameButton)
            {
                case "BKIng":
                    selectedPiece = new King(Player.Black,true);
                    OptionIsSelected = true;
                    break;
                case "BQueen":
                    selectedPiece = new Queen(Player.Black);
                    OptionIsSelected = true;
                    break;
                case "BRook":
                    selectedPiece = new Rook(Player.Black);
                    OptionIsSelected = true;
                    break;
                case "BBishop":
                    selectedPiece = new Bishop(Player.Black);
                    OptionIsSelected = true;
                    break;
                case "BKnight":
                    selectedPiece = new Knight(Player.Black);
                    OptionIsSelected = true;
                    break;
                case "BPawn":
                    selectedPiece = new Pawn(Player.Black,true);
                    OptionIsSelected = true;
                    break;
                case "WKing":
                    selectedPiece = new King(Player.White, true);
                    OptionIsSelected = true;
                    break;
                case "WQueen":
                    selectedPiece = new Queen(Player.White);
                    OptionIsSelected = true;
                    break;
                case "WRook":
                    selectedPiece = new Rook(Player.White);
                    OptionIsSelected = true;
                    break;
                case "WBishop":
                    selectedPiece = new Bishop(Player.White);
                    OptionIsSelected = true;
                    break;
                case "WKnight":
                    selectedPiece = new Knight(Player.White);
                    OptionIsSelected = true;
                    break;
                case "WPawn":
                    selectedPiece = new Pawn(Player.White,true);
                    OptionIsSelected = true;
                    break;
                case "Delete":
                    selectedPiece = null;
                    OptionIsSelected = true;
                    break;
                case "WDelete":
                    selectedPiece = null;
                    OptionIsSelected = true;
                    break;
                default:
                    OptionIsSelected = false;
                    selectedPiece = null;
                    break;
            }
        }
        private void FillFENTextBox()
        {
            Board.FillCastles((bool)WKCastleCheckBox.IsChecked, (bool)WQCastleCheckBox.IsChecked, (bool)BKCastleCheckBox.IsChecked, (bool)BQCastleCheckBox.IsChecked);
            FENTextBox.Text = new StateString(CurrentPlayer, Board, WatchFromWhite).ToString();
            if (Board.RightBoard(CurrentPlayer)) SetPosButton.IsEnabled = true;
            else
            {
                SetPosButton.IsEnabled = false;
            }
        }
        private void SetPosButton_Click(object sender, RoutedEventArgs e)
        {
            var newForm = new MainWindow(FENTextBox.Text); //create your new form.
            newForm.Show(); //show the new form.
            this.Close(); //only if you want to close the current form.
        }

        private void BoardGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)//нажатие на кнопку
        {
            Point point = e.GetPosition(BoardGreed);
            Position pos = ToSquarePosition(point);

            if (OptionIsSelected)
            {
                Board[pos] = selectedPiece;
                DrawBoard(Board);
                FillFENTextBox();
            }
            else
            {
                from = pos;
            }
        }

        private void BoardGrid_MouseMove(object sender, MouseEventArgs e)
        {
            Point point;
            Position pos;

            point = e.GetPosition(BoardGreed);
            pos = ToSquarePosition(point);

            if (System.Windows.Input.Mouse.LeftButton == MouseButtonState.Pressed)//если зажата левая кнопка
            {
                if (OptionIsSelected)
                {
                    Board[pos] = selectedPiece;
                    DrawBoard(Board);
                    FillFENTextBox();
                }
            }
        }

        private void BoardGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)//поднятие кнопки
        {
            Point point = e.GetPosition(BoardGreed);
            Position pos = ToSquarePosition(point);

            if ((!OptionIsSelected)&&(pos!=from))
            {
                Board[pos] = Board[from];
                Board[from] = null;
                from = null;
                DrawBoard(Board);
                FillFENTextBox();
            }
        }
        private void SelectPlayerMove_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (SelectPlayerMove.SelectedIndex == 0)
            {
                CurrentPlayer = Player.White;
            }
            else
            {
                CurrentPlayer = Player.Black;
            }
            FillFENTextBox();
        }

        private void SetReversBoard_Click(object sender, RoutedEventArgs e)
        {
            ReversBoard();
        }
        private void ReversBoard()
        {
            WatchFromWhite = !WatchFromWhite;
            if (WatchFromWhite)
                BoardGreed.Background = new ImageBrush(new BitmapImage(new Uri(BaseUriHelper.GetBaseUri(this), "Assets/boardw.png")));
            else
            {
                BoardGreed.Background = new ImageBrush(new BitmapImage(new Uri(BaseUriHelper.GetBaseUri(this), "Assets/boardb.png")));
            }
            Board = Board.ReversBoard(WatchFromWhite);
            DrawBoard(Board);
            FillFENTextBox();
        }
        private void SetCleanPos_Click(object sender, RoutedEventArgs e)
        {
            Board = new Board();
            DrawBoard(Board);
            FillFENTextBox();

        }

        private void SetStartPos_Click(object sender, RoutedEventArgs e)
        {
            Board = Board.Initial();
            WatchFromWhite = true;
            BoardGreed.Background = new ImageBrush(new BitmapImage(new Uri(BaseUriHelper.GetBaseUri(this), "Assets/boardw.png")));
            DrawBoard(Board);
            FillFENTextBox();
        }

        private void CastleCheckBox_Click(object sender, RoutedEventArgs e)
        {
            FillFENTextBox();
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            var newForm = new MainWindow(); //create your new form.
            newForm.Show(); //show the new form.
            this.Close(); //only if you want to close the current form.
        }
    }
}
