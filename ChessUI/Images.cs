using System.Windows.Media;
using System.Windows.Media.Imaging;
using ChessLogic;

namespace ChessUI
{
    public static  class Images
    {
        private static readonly Dictionary<PieceType, ImageSource> whiteSources = new()
        {
            { PieceType.Pawn, LoadImage("Assets/wpawn.png")},
            { PieceType.Bishop, LoadImage("Assets/wbishop.png")},
            { PieceType.Knight, LoadImage("Assets/whorse.png")},
            { PieceType.Rook, LoadImage("Assets/wrook.png")},
            { PieceType.King, LoadImage("Assets/wking.png")},
            { PieceType.Queen, LoadImage("Assets/wQueen.png")},

        };
        private static readonly Dictionary<PieceType, ImageSource> blackSources = new()
        {
            { PieceType.Pawn, LoadImage("Assets/bpawn.png")},
            { PieceType.Bishop, LoadImage("Assets/bbishop.png")},
            { PieceType.Knight, LoadImage("Assets/bhorse.png")},
            { PieceType.Rook, LoadImage("Assets/brook.png")},
            { PieceType.King, LoadImage("Assets/bking.png")},
            { PieceType.Queen, LoadImage("Assets/bQueen.png")},

        };
        private static ImageSource LoadImage(string filePath)
        {
            return new BitmapImage(new Uri(filePath, UriKind.Relative));
        }
        public static ImageSource GetImage(Player color,PieceType type)//возврат картинки для фигуры
        {
            return color switch
            {
                Player.White => whiteSources[type],
                Player.Black => blackSources[type],
                _ => null
            };
        }
        public static ImageSource GetImage(Piece piece)
        {
            if (piece == null)
            {
                return null;
            }
            //if(WatchBoardFromWhite)//если все ка
            return GetImage(piece.Color,piece.Type);
            //else { return GetImage(piece.Color.Opponent(), piece.Type); }
        }
    }
}
