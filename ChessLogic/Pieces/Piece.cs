using ChessLogic;

public abstract class Piece
{
    public abstract PieceType Type { get; }
    public abstract Player Color { get; set; }
    public bool HasMoved { get; set; } = false;

    public abstract Piece Copy();
    public abstract IEnumerable<Move> GetMoves(Position from, Board board);

    // Вспомогательный метод: можно ли занять клетку (пусто или враг)
    protected bool CanCaptureAt(Position pos, Board board) =>
        Board.IsInside(pos) && (board.IsEmpty(pos) || board[pos].Color != Color);

    protected IEnumerable<Position> MovePositionInDir(Position from, Board board, Direction dir)
    {
        for (Position pos = from + dir; Board.IsInside(pos); pos += dir)
        {
            if (board.IsEmpty(pos))
            {
                yield return pos;
                continue;
            }
            if (board[pos].Color != Color) yield return pos;
            yield break;
        }
    }

    protected IEnumerable<Position> MovePositionInDirs(Position from, Board board, Direction[] directions)
        => directions.SelectMany(dir => MovePositionInDir(from, board, dir));

    public virtual bool CanCaptureOpponentKing(Position from, Board board)
        => GetMoves(from, board).Any(m => board[m.ToPos]?.Type == PieceType.King);

    public static Piece FromCharToPiece(char ch)
    {
        Piece piece;
        Player pieceColor = char.IsLower(ch) ? Player.Black : Player.White;
        switch (char.ToLower(ch))
        {
            case 'p': piece = new Pawn(pieceColor); break;
            case 'b': piece = new Bishop(pieceColor); break;
            case 'n': piece = new Knight(pieceColor); break;
            case 'r': piece = new Rook(pieceColor, true); break;//изначально указываем что ладьи двигались
            case 'q': piece = new Queen(pieceColor); break;
            case 'k': piece = new King(pieceColor, true); break;
            default: piece = null; break;
        }
        return piece;
    }

    public override string ToString() => Type switch
    {
        PieceType.Pawn => "",
        _ => Type.ToString()[0].ToString() // Берем первую букву типа (B, N, R, Q, K)
    };


}