using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Shattered.Content.Graphics;

namespace Fracture.Content.Pipeline.Graphics
{
    public sealed class SpriteAnimationPlaylistReader : ContentTypeReader<SpriteAnimationPlaylist>
    {
        public SpriteAnimationPlaylistReader()
        {
        }

        protected override SpriteAnimationPlaylist Read(ContentReader input, SpriteAnimationPlaylist existingInstance)
        {
            var length = input.ReadInt32();
            var bytes  = input.ReadBytes(length);

            using var ms = new MemoryStream(bytes);
            
            var xDoc = XDocument.Load(ms);

            var xAnimation   = xDoc.Element("Playlist");
            var texturePath  = xAnimation!.Attribute("texture")!.Value;
            var animations   = new Dictionary<string, SpriteAnimationFrames>();
                
            foreach (var animation in xAnimation.Elements("Animation"))
            {
                var name         = animation.Attribute("name")!.Value;
                var baseDuration = TimeSpan.Zero;

                if (animation.Attribute("duration") != null)
                    baseDuration = TimeSpan.ParseExact(animation.Attribute("duration")!.Value, @"mm\:ss\:FFFF", CultureInfo.InvariantCulture);

                var frames    = new List<Rectangle>();
                var durations = new List<TimeSpan>();

                foreach (var frame in animation.Elements("Frame"))
                {
                    var frameDuration = baseDuration;

                    if (animation.Attribute("duration") != null)
                        frameDuration = TimeSpan.ParseExact(animation.Attribute("duration")!.Value, @"mm\:ss\:FFFF", CultureInfo.InvariantCulture);

                    var x = int.Parse(frame.Attribute("x")!.Value);
                    var y = int.Parse(frame.Attribute("y")!.Value);
                    var w = int.Parse(frame.Attribute("w")!.Value);
                    var h = int.Parse(frame.Attribute("h")!.Value);
                        
                    frames.Add(new Rectangle(x, y, w, h));

                    durations.Add(frameDuration);
                }

                animations.Add(name, new SpriteAnimationFrames(frames.ToArray(), durations.ToArray()));
            }

            return new SpriteAnimationPlaylist(input.ContentManager.Load<Texture2D>(texturePath), animations);
        }
    }
}
