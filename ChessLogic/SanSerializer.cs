using ChessLogic;

public static class SanSerializer
{
    public static string Serialize(Move move, Board board)
    {
        if (move is Castle castle)
            return castle.Type == MoveType.CastleKS ? "O-O" : "O-O-O";

        Piece piece = board[move.FromPos];
        string name = GetPiecePrefix(piece.Type);
        name += GetAmbiguityQualifier(move, board);

        if (!board.IsEmpty(move.ToPos) || move is EnPassant)
        {
            if (piece.Type == PieceType.Pawn)
                name += MoveParser.ToCoords(move.FromPos)[0]; 
            name += "x";
        }

        name += MoveParser.ToCoords(move.ToPos);

        if (move is PawnPromotion prom)
            name += "=" + GetPiecePrefix(prom.newType);

        return name;
    }

    private static string GetPiecePrefix(PieceType type) => type switch
    {
        PieceType.Pawn => "",
        PieceType.Knight => "N",
        PieceType.Bishop => "B",
        PieceType.Rook => "R",
        PieceType.Queen => "Q",
        PieceType.King => "K",
        _ => ""
    };

    private static string GetAmbiguityQualifier(Move move, Board board)
    {
        Piece piece = board[move.FromPos];
        if (piece.Type == PieceType.Pawn) return string.Empty;

        var alternativeSources = board.PiecePositionsFor(piece.Color)
            .Where(pos => pos != move.FromPos && board[pos].Type == piece.Type)
            .Where(pos => board[pos].GetMoves(pos, board).Any(m => m.ToPos == move.ToPos))
            .ToList();

        if (alternativeSources.Count == 0) return string.Empty;

        bool sameFile = alternativeSources.Any(pos => pos.Column == move.FromPos.Column);
        bool sameRank = alternativeSources.Any(pos => pos.Row == move.FromPos.Row);

        if (!sameFile) return MoveParser.ToCoords(move.FromPos)[0].ToString();
        if (!sameRank) return MoveParser.ToCoords(move.FromPos)[1].ToString();

        return MoveParser.ToCoords(move.FromPos);
    }
}