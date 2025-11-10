using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessLogic
{
    public class Result
    {
        public Player Winner { get; }
        public EndReason Reason { get; }

        public Result(Player winner,EndReason reason)
        {
            Winner = winner;
            Reason = reason;
        }

        public static Result Win(Player winner)
        {
            return new Result(winner,EndReason.Checkmate);
        }

        public static Result Draw(EndReason reason)
        {
            return new Result(Player.None,reason);
        }
        public override string ToString()
        {
            string res = "";
            switch (Winner)
            {
                case Player.None:
                    res = "Ничья.";
                    break;
                case Player.White:
                    res = "Победа Белых.";
                    break;
                case Player.Black:
                    res = "Побед Чёрных.";
                    break;
            }
            switch (Reason)
            {
                case EndReason.Checkmate:
                    break;
                case EndReason.InsufficientMaterial:
                    res = res + " Недостаточно материала для победы.";
                    break;
                case EndReason.FiftyMoveRule:
                    res = res + " Правило 50 ходов.";
                    break;
                case EndReason.Stalemate:
                    res = res + " Пат.";
                    break;
                case EndReason.ThreefoldRepetition:
                    res = res + " Троекратное повторение позиции.";
                    break;
            }
            return res; ;
        }
    }
}
