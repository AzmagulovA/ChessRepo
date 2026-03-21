namespace ChessLogic
{
    public class DoublePawn : Move
    {
        public override MoveType Type => MoveType.DoublePawn;
        public override Position FromPos { get; }
        public override Position ToPos { get; }
        private readonly Position skippedPos;

        public DoublePawn(Position from, Position to)
        {
            FromPos = from;
            ToPos = to;
            // Клетка, которую перепрыгнули (нужна для FEN и En Passant)
            skippedPos = new Position((from.Row + to.Row) / 2, from.Column);
        }

        public override bool Execute(Board board)
        {
            Player player = board[FromPos].Color;
            board.SetPawnSkipPosition(player, skippedPos); // Используем наш новый метод
            new NormalMove(FromPos, ToPos).Execute(board);
            return true; // Ход пешкой — ход необратимый
        }
    }
}