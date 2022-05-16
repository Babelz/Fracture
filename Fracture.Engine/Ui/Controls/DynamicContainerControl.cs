using Fracture.Engine.Ui.Components;

namespace Fracture.Engine.Ui.Controls
{
    /// <summary>
    /// Interface for implementing dynamic container controls. New controls
    /// can be added to dynamic container explicitly.
    /// </summary>
    public interface IDynamicContainerControl : IStaticContainerControl, IControlCollection
    {
        new void Clear();
    }

    public abstract class DynamicContainerControl : StaticContainerControl, IDynamicContainerControl
    {
        protected DynamicContainerControl()
        {
        }

        protected DynamicContainerControl(IControlManager children)
            : base(children)
        {
        }

        public virtual void Add(IControl control) => Children.Add(control);

        public virtual void Remove(IControl control) => Children.Remove(control);
    }
}