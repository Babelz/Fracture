using System.IO;
using System.Xml.Linq;
using Microsoft.Xna.Framework.Content.Pipeline;

namespace Fracture.Content.Pipeline.Graphics
{
    [ContentImporter(".sapl", DefaultProcessor = "No Processing Required", DisplayName = "Sprite animation playlist importer")]
    public sealed class SpriteAnimationPlaylistDataImporter : ContentImporter<SpriteAnimationPlaylistData>
    {
        public SpriteAnimationPlaylistDataImporter()
        {
        }

        public override SpriteAnimationPlaylistData Import(string filename, ContentImporterContext context)
        {
            using var sr = new StreamReader(filename);
            
            return new SpriteAnimationPlaylistData(filename, XDocument.Load(sr).ToString());
        }
    }
}
