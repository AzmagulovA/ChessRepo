using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ChessLogic
{
    public class AnalysMove
    {
        public string FenAfterMove { get; set; }//Fen-нотация 
        public string MoveName { get; set; }//имя 
        public static int Counter = 0;
        public int Number = 0;
        public int MoveNumb;
        public AnalysMove LastMove { get; set; } = null;// ссылка на предыдущий ход
        public List<AnalysMove> NextMoves { get; set; } = new List<AnalysMove>();//ссылки на следующие ходы ObservableCollection??? - оповещение внешних объектов об измененнии

        public string UserComment = "";

        public Position LastFrom = null;//последняя позиция откуда
        public Position LastTo = null;//позиция куда

        public AnalysMove()
        {
            FenAfterMove = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
            MoveNumb = 1;
            MoveName = string.Empty;
            NextMoves = new List<AnalysMove>();
            Counter++;
            Number = Counter;
            LastMove = null;
            LastFrom = null;
            LastTo = null;
        }
        public AnalysMove(string FEN)
        {
            FenAfterMove = FEN;
             MoveNumb = 1;
            MoveName = string.Empty;
            NextMoves = new List<AnalysMove>();
            Counter++;
            Number = Counter;
            LastMove = null;
            LastFrom = null;
            LastTo = null;
        }
        public int GetNumber()
        {
            return Number;
        }
        public AnalysMove(string moveName, AnalysMove lastMove, List<AnalysMove> nextMoves, Position lastFrom, Position lastTo, int moveNumb)
        {
            FenAfterMove = " ";
            Counter++;
            Number = Counter;
            MoveName = moveName;
            NextMoves = nextMoves;
            LastMove = lastMove;
            LastFrom = lastFrom;
            LastTo = lastTo;
            MoveNumb = moveNumb;
        }
        public bool WhiteMove()
        {
            return FenAfterMove[FenAfterMove.IndexOf(" ") + 1] == 'w';//ход из fen
        }
        public string PrintBranch()
        {
            string res = "";
            bool mainOpt = true;
            AnalysMove mainBranch = null;
            if (NextMoves.Count > 0) //если есть последующие ходы
            {
                foreach (AnalysMove futureMove in NextMoves)//проходимся по каждому из них
                {
                    if (NextMoves.Count == 1)
                    {
                        res = res + futureMove.PrintMove() + " ";
                        res = res + futureMove.PrintBranch();
                    }
                    if (NextMoves.Count > 1)
                    {
                        if (!mainOpt) res = res + " (";//если ветка неосновная, то включаем её в скобки
                        res = res + futureMove.PrintMove() + " ";//печатаем текущий ход
                        if (mainOpt) mainBranch = futureMove;//если основная ветка, то сохраняем чтобы вывести ее позже
                        if (!mainOpt) res = res + futureMove.PrintBranch();//рекурсия, если ветка не главная, то печатаем ее до конца
                        if (!mainOpt) res = res + ") ";
                    }
                    mainOpt = false;
                }

                if (mainBranch != null) res = res + mainBranch.PrintBranch();

            }
            return res;
        }
        public string PrintMove()
        {
            string res = "";

            res = res + MoveName;

            if (UserComment != "")//если не пусто
            {
                res = res + " {" + UserComment + "} ";
            }

            return res;
        }
        public AnalysMove SearchMove(int Numb)
        {
            AnalysMove res = this;

            while (res.LastMove != null)//возвращаемся к первому элементу
            {
                res = res.LastMove;
            }
            res = res.SearchInBranch(Numb);
            return res;
        }
        public AnalysMove SearchInBranch(int Numb)
        {
            AnalysMove res = this;

            if (res.Number == Numb)
            {
                return res;
            }
            else
            {
                if (res.NextMoves.Count != 0)
                {
                    foreach (AnalysMove Moves in res.NextMoves)
                    {
                        res = Moves.SearchInBranch(Numb);
                        if (res.Number == Numb)
                        {
                            return res;
                        }

                    }
                }
            }
            return res;
        }
        public bool FENIsStart()
        {
            return FenAfterMove == "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        }

    }
}
