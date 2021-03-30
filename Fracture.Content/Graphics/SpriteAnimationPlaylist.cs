using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

namespace Shattered.Content.Graphics
{
    public sealed class SpriteAnimationPlaylist
    {
        #region Properties
        public Texture2D Texture
        {
            get;
        }

        public Dictionary<string, SpriteAnimationFrames> Animations
        {
            get;
        }
        #endregion

        public SpriteAnimationPlaylist(Texture2D texture, Dictionary<string, SpriteAnimationFrames> animations)
        {
            Texture    = texture ?? throw new ArgumentNullException(nameof(texture));
            Animations = animations ?? throw new ArgumentNullException(nameof(animations));
        }
    }
}
