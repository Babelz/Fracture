namespace Fracture.Tiled
{
    public enum Orientation
    {
        Orthogonal = 0, 
        Isometric, 
        Staggered,
        Hexagonal
    }
    
    /// <summary>
    /// Model representing Tiled json format map.
    /// 
    /// https://doc.mapeditor.org/en/stable/reference/json-map-format/ 
    /// </summary>
    public sealed class Map
    {
        #region Properties
        /// <summary>
        /// Hex-formatted color (#RRGGBB or #AARRGGBB) (optional).
        /// </summary>
        public string BackgroundColor
        {
            get;
            set;
        }

        /// <summary>
        /// The compression level to use for tile layer data (defaults to -1, which means to use the algorithm default).
        /// </summary>
        public int CompressionLevel
        {
            get;
            set;
        }

        /// <summary>
        /// Number of tile rows.
        /// </summary>
        public int Height
        {
            get;
            set;
        }

        /// <summary>
        /// Length of the side of a hex tile in pixels (hexagonal maps only).
        /// </summary>
        public int HexSideLength
        {
            get;
            set;
        }

        /// <summary>
        /// Whether the map has infinite dimensions.
        /// </summary>
        public bool Infinite
        {
            get;
            set;
        }

        /// <summary>
        /// Array of Layers.
        /// </summary>
        public object[] Layers
        {
            get;
            set;
        }

        /// <summary>
        /// Auto-increments for each layer.
        /// </summary>
        public int NextLayerId
        {
            get;
            set;
        }

        /// <summary>
        /// Auto-increments for each placed object.
        /// </summary>
        public int NextObjectId
        {
            get;
            set;
        }

        /// <summary>
        /// Orthogonal, isometric, staggered or hexagonal.
        /// </summary>
        public Orientation Orientation
        {
            get;
            set;
        }

        /// <summary>
        /// Array of Properties.
        /// </summary>
        public object[] Properties
        {
            get;
            set;
        }

        /// <summary>
        /// Right-down (the default), right-up, left-down or left-up (currently only supported for orthogonal maps).
        /// </summary>
        public RenderOrder RenderOrder
        {
            get;
            set;
        }

        /// <summary>
        /// X or y (staggered / hexagonal maps only).
        /// </summary>
        public StaggerAxis StaggerAxis
        {
            get;
            set;
        }

        /// <summary>
        /// Odd or even (staggered / hexagonal maps only).
        /// </summary>
        public StaggerIndex StaggerIndex
        {
            get;
            set;
        }

        /// <summary>
        /// The Tiled version used to save the file.
        /// </summary>
        public string TiledVersion
        {
            get;
            set;
        }

        /// <summary>
        /// Map grid height.
        /// </summary>
        public int TileHeight
        {
            get;
            set;
        }

        /// <summary>
        /// Array of Tile sets.
        /// </summary>
        public object[] TileSets
        {
            get;
            set;
        }

        /// <summary>
        /// Map grid width.
        /// </summary>
        public int TileWidth
        {
            get;
            set;
        }

        /// <summary>
        /// Map (since 1.0).
        /// </summary>
        public string Type
        {
            get;
            set;
        }

        /// <summary>
        /// The JSON format version (previously a number, saved as string since 1.6).
        /// </summary>
        public string Version
        {
            get;
            set;
        }

        /// <summary>
        /// Number of tile columns.
        /// </summary>
        public int Width
        {
            get;
            set;
        }
        #endregion

        public Map()
        {
        }
    }
}