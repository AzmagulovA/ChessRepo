using System.Collections.Generic;
using System.Linq;

namespace ChessLogic
{
    // Класс представляет собой ОДИН ход (узел) в дереве партии.
    public class MoveNode
    {
        // 1. Основная информация о ходе
        public string SanMoveName { get; } // Алгебраическая нотация (например, "Nf3" или "O-O")
        public int MoveNumber { get; } // Номер хода (1, 2, 3...)
        public bool IsWhiteMove { get; } // Кто сделал этот ход

        // 2. Связь с доской
        public string FenAfterMove { get; } // Позиция (FEN) СРАЗУ ПОСЛЕ этого хода
        public Position From { get; } // Откуда пошли (нужно для отрисовки стрелочек)
        public Position To { get; }   // Куда пошли

        // 3. Комментарии
        public string Comment { get; set; } = string.Empty;

        // 4. Навигация по дереву (Самое важное!)
        public MoveNode Parent { get; } // Ссылка на предыдущий ход (назад)
        public List<MoveNode> Variations { get; } // Варианты ходов вперед (NextMoves)

        // Удобное свойство: Главной линией (Main Line) всегда считается нулевой элемент списка
        public MoveNode MainVariation => Variations.Count > 0 ? Variations[0] : null;
        public bool IsMainLine => Parent == null || Parent.Variations.IndexOf(this) == 0;

        // Конструктор для стартовой позиции (Корневой узел)
        public MoveNode(string startFen)
        {
            SanMoveName = "Start";
            MoveNumber = 0;
            FenAfterMove = startFen;
            IsWhiteMove = false;
            Parent = null;
            Variations = new List<MoveNode>();
        }

        // Конструктор для обычного хода
        public MoveNode(string sanName, int moveNumb, bool isWhite, string fen, Position from, Position to, MoveNode parent)
        {
            SanMoveName = sanName;
            MoveNumber = moveNumb;
            IsWhiteMove = isWhite;
            FenAfterMove = fen;
            From = from;
            To = to;
            Parent = parent;
            Variations = new List<MoveNode>();
        }

        // Добавляет новый ход в дерево вариантов. 
        // Возвращает либо уже существующий узел (если такой ход уже был), либо новый.
        public MoveNode AddVariation(string sanName, int moveNumb, bool isWhite, string fen, Position from, Position to)
        {
            // Если такой ход уже делали в этой позиции - просто переходим в него
            MoveNode existing = Variations.FirstOrDefault(v => v.SanMoveName == sanName);
            if (existing != null)
            {
                return existing;
            }

            // Иначе создаем новую ветку
            MoveNode newNode = new MoveNode(sanName, moveNumb, isWhite, fen, from, to, this);
            Variations.Add(newNode);
            return newNode;
        }

        // Печатает сам ход с комментарием (Например: "1. e4 {Хорошее начало}")
        public string GetMoveTextWithComment()
        {
            string movePrefix = IsWhiteMove ? $"{MoveNumber}. " : (IsMainLine ? "" : $"{MoveNumber}... ");
            string commentStr = string.IsNullOrEmpty(Comment) ? "" : $" {{{Comment}}}";

            return $"{movePrefix}{SanMoveName}{commentStr}";
        }
    }
}