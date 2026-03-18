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
        public FenNotation FenAfterMove { get; set; }//Fen-нотация 
        public string MoveName { get; set; }//имя 
        public static int Counter = 0;
        public int Number = 0;
        public int MoveNumb { get; set; }
        public AnalysMove PreviousMove;// ссылка на предыдущий ход
        public List<AnalysMove> NextMoves;//ссылки на следующие ходы 

        public string UserComment = "";

        public Position LastFrom = null;//последняя позиция откуда
        public Position LastTo = null;//позиция куда

        public AnalysMove()
        {
            FenAfterMove = new FenNotation();
            MoveNumb = 1;
            MoveName = string.Empty;
            NextMoves = new List<AnalysMove>();
            Counter++;
            Number = Counter;
        }
        public AnalysMove(string FEN)
        {
            FenAfterMove = new FenNotation(FEN);
            MoveNumb = 1;
            MoveName = string.Empty;
            NextMoves = new List<AnalysMove>();
            Counter++;
            Number = Counter;
        }
        public string GetNumber()
        {
            return Number.ToString();
        }
        public AnalysMove(string moveName, AnalysMove lastMove, List<AnalysMove> nextMoves, Position lastFrom, Position lastTo, int moveNumb)
        {
            FenAfterMove = new FenNotation();
            Counter++;
            Number = Counter;
            MoveName = moveName;
            NextMoves = nextMoves;
            PreviousMove = lastMove;
            LastFrom = lastFrom;
            LastTo = lastTo;
            MoveNumb = moveNumb;
        }
        public bool WhiteMove()
        {
            return FenAfterMove.IsWhiteMove;
        }
        public string PrintBranch()
        {
            bool mainOpt = true;
            AnalysMove mainBranch = null;
            if (NextMoves.Count > 0) //если есть последующие ходы
            {
                string res = "";
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
            return string.Empty;
        }
        public string PrintMove()
        {
            if (string.IsNullOrEmpty(UserComment))//если не пусто
                return MoveName;
            else
                return string.Concat(MoveName, " {" + UserComment + "} ");
        }

        public AnalysMove SearchMove(int numb)
        {
            AnalysMove root = this;

            while (root.PreviousMove != null)
            {
                root = root.PreviousMove;
            }

            return root.SearchInBranch(numb);
        }

        public AnalysMove SearchInBranch(int numb)
        {
            if (Number == numb)
                return this;

            for (int i = 0; i < NextMoves.Count; i++)
            {
                AnalysMove foundMove = NextMoves[i].SearchInBranch(numb);

                if (foundMove != null)
                    return foundMove;
            }

            return null;
        }
    }
}
