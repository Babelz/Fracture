using System;
using Microsoft.Xna.Framework;

namespace Fracture.Client.Content.Graphics
{
    public sealed class SpriteAnimationFrames
    {
        #region Properties
        public Rectangle[] Frames
        {
            get;
        }
        public TimeSpan[] Durations
        {
            get;
        }
        #endregion

        public SpriteAnimationFrames(Rectangle[] frames, TimeSpan[] durations)
        {
            Frames    = frames ?? throw new ArgumentNullException(nameof(frames));
            Durations = durations ?? throw new ArgumentNullException(nameof(durations));
        }
    }
}
