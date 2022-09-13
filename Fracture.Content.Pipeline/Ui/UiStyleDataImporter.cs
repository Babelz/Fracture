using System.IO;
using Microsoft.Xna.Framework.Content.Pipeline;

namespace Fracture.Content.Pipeline.Ui
{
    [ContentImporter(".uis", DefaultProcessor = "No Processing Required", DisplayName = "Style importer")]
    public sealed class UiStyleDataImporter : ContentImporter<UiStyleData>
    {
        public UiStyleDataImporter()
        {
        }

        public override UiStyleData Import(string filename, ContentImporterContext context)
        {
            using var sr = new StreamReader(filename);

            return new UiStyleData(sr.ReadToEnd());
        }
    }
}