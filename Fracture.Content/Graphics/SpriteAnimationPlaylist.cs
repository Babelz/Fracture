using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

namespace Shattered.Content.Graphics
{
    public sealed class SpriteAnimationPlaylist
    {
        #region Fields
        private readonly Dictionary<string, Texture2D> textures;
        private readonly Dictionary<string, SpriteAnimationFrames> animations;
        #endregion

        #region Properties
        public string Name
        {
            get;
        }
        #endregion

        public SpriteAnimationPlaylist(string name, Dictionary<string, Texture2D> textures, Dictionary<string, SpriteAnimationFrames> animations)
        {
            Name = !string.IsNullOrEmpty(name) ? name : throw new ArgumentNullException(nameof(name));

            this.textures   = textures ?? throw new ArgumentNullException(nameof(textures));
            this.animations = animations ?? throw new ArgumentNullException(nameof(animations));
        }

        public Texture2D GetTexture(string animationName)
            => textures[animationName];

        public SpriteAnimationFrames GetAnimation(string animationName)
            => animations[animationName];
    }
}