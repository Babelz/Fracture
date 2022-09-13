using System.Text;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;

namespace Fracture.Content.Pipeline.Graphics
{
    [ContentTypeWriter]
    public sealed class SpriteAnimationPlaylistDataWriter : ContentTypeWriter<SpriteAnimationPlaylistData>
    {
        public SpriteAnimationPlaylistDataWriter()
        {
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
            => typeof(SpriteAnimationPlaylistReader).AssemblyQualifiedName;

        protected override void Write(ContentWriter output, SpriteAnimationPlaylistData value)
        {
            var bytes = Encoding.UTF8.GetBytes(value.Contents);

            output.Write(bytes.Length);
            output.Write(bytes);
        }
    }
}