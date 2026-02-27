namespace CryptmasterAccess
{
    /// <summary>
    /// Direction math utility for converting between absolute (grid) and relative (player-facing) directions.
    /// Game convention: 0=left, 1=up, 2=right, 3=down.
    /// Relative convention: 0=ahead, 1=right, 2=behind, 3=left.
    /// </summary>
    public static class DirectionHelper
    {
        /// <summary>
        /// Converts an absolute grid direction to a direction relative to the player's facing.
        /// </summary>
        /// <param name="absoluteDir">Grid direction (0=left, 1=up, 2=right, 3=down)</param>
        /// <param name="lookDirection">Player's current facing (same convention)</param>
        /// <returns>Relative direction (0=ahead, 1=right, 2=behind, 3=left)</returns>
        public static int AbsoluteToRelative(int absoluteDir, int lookDirection)
        {
            return (absoluteDir - lookDirection + 4) % 4;
        }

        /// <summary>
        /// Gets the localized name for a relative direction.
        /// </summary>
        public static string GetRelativeName(int relativeDir)
        {
            switch (relativeDir)
            {
                case 0: return Loc.Get("dir_ahead");
                case 1: return Loc.Get("dir_right");
                case 2: return Loc.Get("dir_behind");
                case 3: return Loc.Get("dir_left");
                default: return Loc.Get("dir_unknown");
            }
        }

        /// <summary>
        /// Gets the localized name for an absolute (cardinal) direction.
        /// Game convention: 0=left(west), 1=up(north), 2=right(east), 3=down(south).
        /// </summary>
        public static string GetAbsoluteName(int absoluteDir)
        {
            switch (absoluteDir)
            {
                case 0: return Loc.Get("dir_west");
                case 1: return Loc.Get("dir_north");
                case 2: return Loc.Get("dir_east");
                case 3: return Loc.Get("dir_south");
                default: return Loc.Get("dir_unknown");
            }
        }

        /// <summary>
        /// Gets the adjacent MapPiece in the given absolute direction.
        /// </summary>
        /// <param name="piece">Current map piece</param>
        /// <param name="absoluteDir">Direction (0=left, 1=up, 2=right, 3=down)</param>
        /// <returns>Adjacent MapPiece or null</returns>
        public static MapPiece GetAdjacentPiece(MapPiece piece, int absoluteDir)
        {
            if (piece == null) return null;

            switch (absoluteDir)
            {
                case 0: return piece.leftPiece;
                case 1: return piece.upPiece;
                case 2: return piece.rightPiece;
                case 3: return piece.downPiece;
                default: return null;
            }
        }

        /// <summary>
        /// Returns the inverse of a direction (opposite direction).
        /// Matches GameManager.InverseMapVal: 0↔2, 1↔3.
        /// </summary>
        public static int InverseDirection(int dir)
        {
            switch (dir)
            {
                case 0: return 2;
                case 1: return 3;
                case 2: return 0;
                case 3: return 1;
                default: return 0;
            }
        }
    }
}
