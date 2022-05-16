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
            
            var document   = XDocument.Load(ms);
            var playlists  = document.Element("Playlist");
            var animations = new Dictionary<string, SpriteAnimationFrames>();
            var textures   = new Dictionary<string, Texture2D>();
                
            foreach (var animation in playlists.Elements("Animation"))
            {
                var animationName = animation.Attribute("name")!.Value;
                var baseDuration  = TimeSpan.Zero;
                var texturePath   = playlists.Attribute("texture")?.Value ?? playlists!.Attribute("texture")!.Value;

                if (string.IsNullOrEmpty(texturePath))
                    throw new InvalidOperationException($"missing texture path for animation in file {input.AssetName}");
                
                if (animation.Attribute("duration") != null)
                    baseDuration = TimeSpan.ParseExact(animation.Attribute("duration")!.Value, @"mm\:ss\:FFFF", CultureInfo.InvariantCulture);

                var frameSources   = new List<Rectangle>();
                var frameDurations = new List<TimeSpan>();

                foreach (var frame in animation.Elements("Frame"))
                {
                    var frameDuration = baseDuration;

                    if (animation.Attribute("duration") != null)
                        frameDuration = TimeSpan.ParseExact(animation.Attribute("duration")!.Value, @"mm\:ss\:FFFF", CultureInfo.InvariantCulture);

                    var x = int.Parse(frame.Attribute("x")!.Value);
                    var y = int.Parse(frame.Attribute("y")!.Value);
                    var w = int.Parse(frame.Attribute("w")!.Value);
                    var h = int.Parse(frame.Attribute("h")!.Value);
                        
                    frameSources.Add(new Rectangle(x, y, w, h));

                    frameDurations.Add(frameDuration);
                }

                animations[animationName] = new SpriteAnimationFrames(frameSources.ToArray(), frameDurations.ToArray());
                textures[animationName]   = input.ContentManager.Load<Texture2D>(texturePath);
            }

            return new SpriteAnimationPlaylist(input.AssetName, textures, animations);
        }
    }
}
