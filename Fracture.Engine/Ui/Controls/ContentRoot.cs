using Fracture.Engine.Ui.Components;

namespace Fracture.Engine.Ui.Controls
{
    /// <summary>
    /// Container that does not render anything visible and is best used
    /// as root for the user interface.
    /// </summary>
    public sealed class ContentRoot : DynamicContainerControl
    {
        public ContentRoot()
        {
        }

        public ContentRoot(IControlManager children)
            : base(children)
        {
        }
    }
}