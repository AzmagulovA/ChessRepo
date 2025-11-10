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
using System.Windows.Shapes;

namespace ChessUI
{
    /// <summary>
    /// Логика взаимодействия для GameOverMenu.xaml
    /// </summary>
    public partial class GameOverMenu : UserControl
    {
        public event Action<Option> OptionSelected;
        public GameOverMenu(GameState gameState)
        {
            InitializeComponent();
            Result result = gameState.Result;
            WinnerText.Text = GetWinnerText(result.Winner);
            ReasonText.Text = GetReasonText(result.Reason,gameState.CurrentPlayer);
        }
        private static string GetWinnerText(Player Winner)
        {
            return Winner switch
            {
                Player.White => "Победа белых!",
                Player.Black => "Победа чёрных!",
                _ => "Ничья!"
            };
            
        }
        private static string PlayerString(Player player)
        {
            return player switch
            {
                Player.White => "Белые",
                Player.Black => "Чёрные",
                _ => ""

            };
        }
        private static string GetReasonText(EndReason reason,Player currentPlayer)
        {
            return reason switch
            {
                EndReason.Stalemate => $"Пат - {PlayerString(currentPlayer)} не могут ходить",
                EndReason.Checkmate => $"Мат - {PlayerString(currentPlayer)} не могут ходить",
                EndReason.FiftyMoveRule => "Правило 50 ходов",
                EndReason.InsufficientMaterial=> "Недостаток материала",
                EndReason.ThreefoldRepetition => "Троекртаное повторение позиции",
                _ => ""
            };
        }

        private void Restart_Click(object sender, RoutedEventArgs e)
        {
            OptionSelected?.Invoke(Option.Restart);

        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {

            OptionSelected?.Invoke(Option.Exit);
        }
    }
}
