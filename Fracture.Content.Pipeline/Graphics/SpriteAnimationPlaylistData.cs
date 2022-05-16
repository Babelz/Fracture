using System;

namespace Fracture.Content.Pipeline.Graphics
{
    public sealed class SpriteAnimationPlaylistData
    {
        #region Properties
        public string Filename
        {
            get;
        }

        public string Contents
        {
            get;
        }
        #endregion

        public SpriteAnimationPlaylistData(string filename, string contents)
        {
            Filename = !string.IsNullOrEmpty(filename) ? filename : throw new ArgumentNullException(nameof(filename));
            Contents = !string.IsNullOrEmpty(contents) ? contents : throw new ArgumentNullException(nameof(contents));
        }
    }
}