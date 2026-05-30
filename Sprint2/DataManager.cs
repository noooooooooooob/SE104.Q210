using System.Collections.Generic;

namespace Sprint2
{
    public static class DataManager
    {
        public static List<string> LoaiGames =
            new List<string>()
            {
                "Action",
                "Adventure",
                "RPG",
                "FPS",
                "Sport"
            };

        public static List<Game> DanhSachGame =
            new List<Game>();

        public static int TuoiGameToiDa = 10;
    }
}
