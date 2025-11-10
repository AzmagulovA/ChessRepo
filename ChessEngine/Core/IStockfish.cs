using System.Collections.Generic;
using ChessEngine.Models;

namespace ChessEngine
{
    public interface IStockfish
    {
        int Depth { get; set; }
        int SkillLevel { get; set; }
        void SetFenPosition(string fenPosition);
        List<List<string>> GetBestMoves(string FEN);
        
    }
}