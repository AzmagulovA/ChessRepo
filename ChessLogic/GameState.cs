using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessLogic
{
    public class GameState
    {
        public Board CurrentBoard { get; private set; }
        public Player CurrentPlayer { get; private set; }
        public Result Result { get; private set; }

        public AnalysMove CurrentAnalysisNode { get; private set; }

        private int noCaptureOrPawnMoves = 0;

        public GameState(Player player, Board board)
        {
            CurrentPlayer = player;
            CurrentBoard = board;
            CurrentAnalysisNode = new AnalysMove();
            UpdateStateString();
        }

        public void MakeMove(Move move)
        {
            string moveName = SanSerializer.Serialize(move, CurrentBoard);
            CurrentBoard.SetPawnSkipPosition(CurrentPlayer, null);
            bool isIrreversible = move.Execute(CurrentBoard);
            moveName = AppendCheckSuffix(moveName);
            UpdateHistory(move, moveName);
            CurrentPlayer = CurrentPlayer.Opponent();
            UpdateCounters(isIrreversible);
            CheckForGameOver();
            UpdateStateString();
        }

        public void MakeMove(string moveStr)
        {
            var legalMoves = AllLegalMovesFor(CurrentPlayer);
            Move foundMove = MatchMoveFromStr(moveStr, legalMoves);

            if (foundMove != null)
            {
                MakeMove(foundMove);
            }
            else
            {
                throw new ArgumentException($"Ход {moveStr} недопустим в текущей позиции.");
            }
        }
        private Move MatchMoveFromStr(string moveStr, IEnumerable<Move> legalMoves)
        {
            string cleanStr = moveStr.Replace("+", "").Replace("#", "");

            foreach (Move move in legalMoves)
            {
                string candidateName = SanSerializer.Serialize(move, CurrentBoard);

                if (candidateName.Replace("+", "").Replace("#", "") == cleanStr)
                {
                    return move;
                }
            }

            return null;
        }

        private void UpdateHistory(Move move, string moveName)
        {
            var existingMove = CurrentAnalysisNode.NextMoves
                .FirstOrDefault(m => m.MoveName == moveName);

            if (existingMove != null)
            {
                CurrentAnalysisNode = existingMove;
            }
            else
            {
                int nextMoveNumb = CurrentPlayer == Player.Black
                    ? CurrentAnalysisNode.MoveNumb + 1
                    : CurrentAnalysisNode.MoveNumb;

                var newNode = new AnalysMove(
                    moveName,
                    CurrentAnalysisNode,
                    new List<AnalysMove>(),
                    move.FromPos,
                    move.ToPos,
                    nextMoveNumb);

                CurrentAnalysisNode.NextMoves.Add(newNode);
                CurrentAnalysisNode = newNode;
            }
        }

        private string AppendCheckSuffix(string moveName)
        {
            if (!CurrentBoard.IsInCheck(CurrentPlayer.Opponent()))
                return moveName;

            return AllLegalMovesFor(CurrentPlayer.Opponent()).Any()
                ? moveName + "+"
                : moveName + "#";
        }

        public IEnumerable<Move> LegalMovesForPiece(Position pos)
        {
            if (CurrentBoard.IsEmpty(pos) || CurrentBoard[pos].Color != CurrentPlayer)
                return Enumerable.Empty<Move>();

            return CurrentBoard[pos].GetMoves(pos, CurrentBoard)
                .Where(move => move.IsLegal(CurrentBoard));
        }

        public IEnumerable<Move> AllLegalMovesFor(Player player)
        {
            return CurrentBoard.PiecePositionsFor(player)
                .SelectMany(pos => CurrentBoard[pos].GetMoves(pos, CurrentBoard))
                .Where(move => move.IsLegal(CurrentBoard));
        }

        private void CheckForGameOver()
        {
            if (!AllLegalMovesFor(CurrentPlayer).Any())
            {
                Result = CurrentBoard.IsInCheck(CurrentPlayer)
                    ? Result.Win(CurrentPlayer.Opponent())
                    : Result.Draw(EndReason.Stalemate);
            }
            else if (noCaptureOrPawnMoves >= 100) // 50 полных ходов
            {
                Result = Result.Draw(EndReason.FiftyMoveRule);
            }
            else
            {
                Result = null;
            }
        }

        private void UpdateCounters(bool isIrreversible)
        {
            if (isIrreversible) noCaptureOrPawnMoves = 0;
            else noCaptureOrPawnMoves++;
        }

        private void UpdateStateString()
        {
            CurrentAnalysisNode.FenAfterMove.Position =
                new StateString(CurrentPlayer, CurrentBoard, true).ToString();
        }

        public void GoToMove(int moveId)
        {
            var target = CurrentAnalysisNode.SearchMove(moveId);
            if (target != null)
            {
                CurrentAnalysisNode = target;
            }
        }

        public void LoadFromFen(string fen)
        {
            if (string.IsNullOrWhiteSpace(fen)) return;

            string[] parts = fen.Split(' ');
            if (parts.Length < 3) return;

            PlacePiecesFromFen(parts[0]);

            CurrentPlayer = (parts[1] == "w") ? Player.White : Player.Black;

            ApplyCastlingRights(parts[2]);

            if (parts.Length >= 4 && parts[3] != "-")
            {
                Position epPos = MoveParser.ToPosition(parts[3]);
                CurrentBoard.SetPawnSkipPosition(CurrentPlayer, epPos);
            }
        }


        private void ApplyCastlingRights(string rights)
        {
            if (rights == "-") return;

            if (rights.Contains('K')) TryEnableCastling(7, 4, 7, 7); // Король e1, Ладья h1
            if (rights.Contains('Q')) TryEnableCastling(7, 4, 7, 0); // Король e1, Ладья a1

            if (rights.Contains('k')) TryEnableCastling(0, 4, 0, 7); // Король e8, Ладья h8
            if (rights.Contains('q')) TryEnableCastling(0, 4, 0, 0); // Король e8, Ладья a8
        }

        private void PlacePiecesFromFen(string piecePlacement)
        {
            CurrentBoard.ClearBoard();

            int row = 0;
            int col = 0;

            foreach (char ch in piecePlacement)
            {
                if (ch == '/')
                {
                    row++;
                    col = 0;
                }
                else if (char.IsDigit(ch))
                {
                    col += (int)char.GetNumericValue(ch);
                }
                else
                {
                    Piece piece = Piece.FromCharToPiece(ch);
                    if (piece != null)
                    {
                        piece.HasMoved = true;
                        CurrentBoard[row, col] = piece;
                    }
                    col++;
                }
            }
        }

        private void TryEnableCastling(int kingRow, int kingCol, int rookRow, int rookCol)
        {
            Piece king = CurrentBoard[kingRow, kingCol];
            Piece rook = CurrentBoard[rookRow, rookCol];

            if (king != null && king.Type == PieceType.King)
            {
                king.HasMoved = false;
            }

            if (rook != null && rook.Type == PieceType.Rook)
            {
                rook.HasMoved = false;
            }
        }

        public void SetMoveByNumber(int number)
        {
            AnalysMove target = CurrentAnalysisNode.SearchMove(number);
            if (target != null)
            {
                CurrentAnalysisNode = target;
                LoadFromFen(target.FenAfterMove.Position);
            }
        }
        public bool IsGameOver()
        {
            return Result != null;
        }
    }
}