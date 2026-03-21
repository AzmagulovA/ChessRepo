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
            if (NextMoves.Count == 0)
                return string.Empty;

            StringBuilder sb = new StringBuilder();
            AnalysMove mainLine = NextMoves[0];

            // 1. Печатаем основной ход текущей ветки
            sb.Append(mainLine.PrintMove());

            // 2. Если есть альтернативные варианты, выводим их в скобках
            if (NextMoves.Count > 1)
            {
                for (int i = 1; i < NextMoves.Count; i++)
                {
                    // Перед альтернативным ходом черных в скобках нужно ставить номер и многоточие (напр. 1... e5)
                    string prefix = NextMoves[i].WhiteMove() ? "" : $"{NextMoves[i].MoveNumb}... ";
                    sb.Append($" ({prefix}{NextMoves[i].PrintMove()} {NextMoves[i].PrintBranch()})");
                }
            }

            // 3. Продолжаем основную линию
            string remainingMainLine = mainLine.PrintBranch();
            if (!string.IsNullOrEmpty(remainingMainLine))
            {
                // Если следующий ход — ход белых, добавляем номер хода для читаемости
                if (mainLine.NextMoves.Count > 0 && mainLine.NextMoves[0].WhiteMove())
                {
                    sb.Append(" ");
                }
                sb.Append(" " + remainingMainLine);
            }

            return sb.ToString().Trim();
        }

        public string PrintMove()
        {
            // Если это ход белых, добавляем номер (1. e4)
            // Если это первый ход в ветке за черных, добавляем (1... e5)
            string prefix = "";
            if (WhiteMove())
            {
                prefix = $"{MoveNumb}. ";
            }

            string comment = string.IsNullOrEmpty(UserComment) ? "" : $" {{{UserComment}}}";
            return $"{prefix}{MoveName}{comment}";
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
