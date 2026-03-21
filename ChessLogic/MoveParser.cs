using System;

namespace ChessLogic
{
    public static class MoveParser
    {
        // Перевод строки вида "a1" в объект Position
        public static Position ToPosition(string coords)
        {
            if (coords.Length < 2) throw new ArgumentException("Invalid coordinates");

            int col = coords[0] - 'a';
            int row = 8 - (int)char.GetNumericValue(coords[1]);
            return new Position(row, col);
        }

        // Перевод Position в строку вида "a1"
        public static string ToCoords(Position pos)
        {
            char col = (char)('a' + pos.Column);
            int row = 8 - pos.Row;
            return $"{col}{row}";
        }

        // Вспомогательный метод для получения колонки из символа ('a' -> 0)
        public static int FileToColumn(char file) => file - 'a';

        // Вспомогательный метод для получения ряда из символа ('8' -> 0)
        public static int RankToRow(char rank) => 8 - (int)char.GetNumericValue(rank);
    }
}