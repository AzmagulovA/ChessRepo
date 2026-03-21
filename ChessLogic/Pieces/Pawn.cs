using ChessLogic;

public class Pawn : Piece
{
    public override PieceType Type => PieceType.Pawn;
    public override Player Color { get; set; }
    private readonly Direction moveDir;

    public Pawn(Player color, bool hasMoved = false)
    {
        Color = color;
        HasMoved = hasMoved;
        // Белые идут к 0 ряду (North), черные к 7 ряду (South)
        moveDir = (color == Player.White) ? Direction.North : Direction.South;
    }

    public override Piece Copy() => new Pawn(Color, HasMoved);

    private IEnumerable<Move> ForwardMoves(Position from, Board board)
    {
        Position oneStep = from + moveDir;
        if (!Board.IsInside(oneStep) || !board.IsEmpty(oneStep)) yield break;

        if (oneStep.Row == 0 || oneStep.Row == 7) // Ряды превращения
            foreach (var m in PromotionMoves(from, oneStep)) yield return m;
        else
            yield return new NormalMove(from, oneStep);

        Position twoSteps = oneStep + moveDir;
        if (!HasMoved && Board.IsInside(twoSteps) && board.IsEmpty(twoSteps))
            yield return new DoublePawn(from, twoSteps);
    }

    private IEnumerable<Move> DiagonalMoves(Position from, Board board)
    {
        foreach (var sideDir in new[] { Direction.West, Direction.East })
        {
            Position to = from + moveDir + sideDir;
            if (!Board.IsInside(to)) continue;

            if (to == board.GetPawnSkipPosition(Color.Opponent()))
                yield return new EnPassant(from, to);
            else if (!board.IsEmpty(to) && board[to].Color != Color)
            {
                if (to.Row == 0 || to.Row == 7)
                    foreach (var m in PromotionMoves(from, to)) yield return m;
                else
                    yield return new NormalMove(from, to);
            }
        }
    }

    public override IEnumerable<Move> GetMoves(Position from, Board board)
        => ForwardMoves(from, board).Concat(DiagonalMoves(from, board));

    private static IEnumerable<Move> PromotionMoves(Position from, Position to)
    {
        yield return new PawnPromotion(from, to, PieceType.Queen);
        yield return new PawnPromotion(from, to, PieceType.Rook);
        yield return new PawnPromotion(from, to, PieceType.Bishop);
        yield return new PawnPromotion(from, to, PieceType.Knight);
    }
}