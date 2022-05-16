using System.Text;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;

namespace Fracture.Content.Pipeline.Ui
{
    [ContentTypeWriter]
    public sealed class UiStyleDataWriter : ContentTypeWriter<UiStyleData>
    {
        public UiStyleDataWriter()
        {
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform) => typeof(UiStyleReader).AssemblyQualifiedName;

        protected override void Write(ContentWriter output, UiStyleData value)
        {
            var bytes = Encoding.UTF8.GetBytes(value.Contents);

            output.Write(bytes.Length);
            output.Write(bytes);
        }
    }
}