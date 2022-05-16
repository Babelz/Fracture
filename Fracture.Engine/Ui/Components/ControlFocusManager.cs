using System;
using System.Collections.Generic;
using System.Linq;
using Fracture.Common;
using Fracture.Engine.Core;
using Fracture.Engine.Core.Primitives;
using Fracture.Engine.Input.Devices;
using Fracture.Engine.Ui.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Fracture.Engine.Ui.Components
{
    /// <summary>
    /// Context used by focus managers.
    /// </summary>
    public sealed class ControlFocusManagerContext
    {
        #region Properties
        /// <summary>
        /// Control focused by the keyboard or escape key.
        /// </summary>
        public IControl Keyboard
        {
            get;
            set;
        }

        /// <summary>
        /// Control focused by the mouse position or click.
        /// </summary>
        public IControl Mouse
        {
            get;
            set;
        }
        #endregion

        public ControlFocusManagerContext()
        {
        }
    }

    /// <summary>
    /// Abstract base class for handling control focus based on the input source of type <see cref="T"/>.
    /// </summary>
    public abstract class ControlFocusManager<T>
    {
        #region Protected
        protected ControlFocusManagerContext Context
        {
            get;
        }

        protected IStaticContainerControl Root
        {
            get;
        }

        /// <summary>
        /// Is this manager running focus updates.
        /// </summary>
        protected bool Updating
        {
            get;
            private set;
        }
        #endregion

        protected ControlFocusManager(IStaticContainerControl root, ControlFocusManagerContext context)
        {
            Root    = root;
            Context = context ?? throw new ArgumentNullException(nameof(context));

            HookContainerEventHandlers(root);
        }

        #region Event handlers
        private void Container_ControlRemoved(object sender, ControlEventArgs e)
        {
            var control = e.Control;

            if (control is IStaticContainerControl container) UnhookContainerEventHandlers(container);
            else
            {
                control.FocusChanged   -= Control_FocusChanged;
                control.EnabledChanged -= Control_EnabledChanged;

                UnhookCustomEvents(control);
            }
        }

        private void Container_ControlAdded(object sender, ControlEventArgs e)
        {
            var control = e.Control;

            if (control is IStaticContainerControl container) HookContainerEventHandlers(container);
            else
            {
                control.FocusChanged   += Control_FocusChanged;
                control.EnabledChanged += Control_EnabledChanged;

                HookCustomEvents(control);
            }
        }

        private void Control_EnabledChanged(object sender, EventArgs e)
        {
            var control = (IControl)sender;

            if (control.Enabled) return;

            Disabled(control);
        }

        private void Control_FocusChanged(object sender, EventArgs e)
        {
            var control = (IControl)sender;

            if (control.HasFocus) Focused(control);
            else Defocused(control);
        }
        #endregion

        protected void HookContainerEventHandlers(IStaticContainerControl container)
        {
            container.ControlAdded   += Container_ControlAdded;
            container.ControlRemoved += Container_ControlRemoved;

            foreach (var control in container.Controls)
            {
                control.FocusChanged   += Control_FocusChanged;
                control.EnabledChanged += Control_EnabledChanged;

                if (control is IStaticContainerControl inner) HookContainerEventHandlers(inner);

                HookCustomEvents(control);
            }
        }

        protected virtual void UnhookContainerEventHandlers(IStaticContainerControl container)
        {
            container.ControlAdded   -= Container_ControlAdded;
            container.ControlRemoved -= Container_ControlRemoved;

            foreach (var control in container.Controls)
            {
                control.FocusChanged   -= Control_FocusChanged;
                control.EnabledChanged -= Control_EnabledChanged;

                if (control is IStaticContainerControl inner) UnhookContainerEventHandlers(inner);

                UnhookCustomEvents(control);
            }
        }

        protected abstract void HookCustomEvents(IControl control);
        protected abstract void UnhookCustomEvents(IControl control);

        /// <summary>
        /// Method called when control in the container has been disabled.
        /// </summary>
        protected abstract void Disabled(IControl control);

        /// <summary>
        /// Method called when control in the container has been focused.
        /// </summary>
        protected abstract void Focused(IControl control);

        /// <summary>
        /// Method called when control in the container has been defocused.
        /// </summary>
        protected abstract void Defocused(IControl control);

        protected abstract void InternalUpdate(IGameEngineTime time, T device);

        /// <summary>
        /// Update focus state by device state.
        /// </summary>
        public void Update(IGameEngineTime time, T device)
        {
            Updating = true;

            InternalUpdate(time, device);

            Updating = false;
        }
    }

    /// <summary>
    /// Focus manager that handles focus based on mouse input.
    /// </summary>
    public sealed class ControlMouseFocusManager : ControlFocusManager<IMouseDevice>
    {
        public ControlMouseFocusManager(IStaticContainerControl root, ControlFocusManagerContext context)
            : base(root, context)
        {
        }

        #region Event handlers
        private void Control_MouseInputEnabledChanged(object sender, EventArgs e)
        {
            // If not currently focused control, return.
            if (!ReferenceEquals(Context.Mouse, sender)) return;

            // If the control stopped accepting mouse input 
            // and it is the focused one, defocus it.
            if (!Context.Mouse.AcceptsMouseInput)
                Context.Mouse.Defocus();
        }
        #endregion

        /// <summary>
        /// Attempt to focus a control in given container. Returns boolean declaring
        /// if a control was focused.
        /// </summary>
        private bool Focus(IStaticContainerControl container, Rectf mouse)
        {
            // Container not enabled or does not accept mouse input,
            // can't focus.
            if (!container.Enabled || !container.AcceptsMouseInput)
                return false;

            // Not inside container, can't have focus to anything then.
            if (!Rectf.Intersects(container.CollisionBoundingBox, mouse))
                return false;

            if (container.UseRenderTarget)
                mouse = new Rectf(mouse.Position, mouse.Bounds);

            for (var i = container.ControlsCount - 1; i >= 0; i--)
            {
                var child = container[i];

                if (!child.Enabled ||
                    !child.Visible ||
                    !child.VisibleFromParent ||
                    !child.AcceptsMouseInput ||
                    !Rectf.Intersects(child.CollisionBoundingBox, mouse))
                    continue;

                if (child is IStaticContainerControl inner)
                {
                    // Keep track of the currently focused control.
                    var last = Context.Mouse;

                    // Attempt to focus to inner container.
                    if (Focus(inner, mouse))
                        return true;

                    // If the currently focused control changed, focus has changed.
                    // In other case, keep looking for the control that could contain the
                    // focus.
                    var current = Context.Mouse;

                    if (!ReferenceEquals(current, last))
                        return true;

                    // If no focus was applied to any child control, apply focus to current container.
                    Context.Mouse?.Defocus();

                    inner.Focus();
                }
                else
                {
                    // Do not refocus if focused control has not changed.
                    if (ReferenceEquals(Context.Mouse, child))
                        return true;

                    Context.Mouse?.Defocus();

                    child.Focus();
                }
            }

            return false;
        }

        protected override void Defocused(IControl control)
        {
            // If the focused control did not get defocused, skip focus changes.
            if (!ReferenceEquals(Context.Mouse, control)) return;

            Context.Mouse = null;
        }

        protected override void Disabled(IControl control)
        {
            // If the focused control did not get disabled, skip focus changes.
            if (!ReferenceEquals(Context.Mouse, control)) return;

            control.Defocus();
        }

        protected override void Focused(IControl control)
        {
            if (!Updating) return;

            if (Context.Mouse?.HasFocus ?? false)
                Context.Mouse.Defocus();

            Context.Mouse = control;
        }

        protected override void HookCustomEvents(IControl control)
            => control.MouseInputEnabledChanged += Control_MouseInputEnabledChanged;

        protected override void UnhookCustomEvents(IControl control)
            => control.MouseInputEnabledChanged -= Control_MouseInputEnabledChanged;

        protected override void InternalUpdate(IGameEngineTime time, IMouseDevice device)
        {
            // If no click has happened we don't update focus. 
            // Focus requires a click to be applied.
            if (!device.IsButtonPressed(MouseButton.Left)) return;

            // Defocus keyboards focus if mouse is gaining the focus.
            Context.Keyboard?.Defocus();

            // Find next focused control.
            Focus(Root, new Rectf(UiCanvas.ToLocalUnits(device.GetPosition().ToVector2()), UiCanvas.ToLocalUnits(Vector2.One)));
        }
    }

    public sealed class ControlKeyboardFocusManager : ControlFocusManager<IKeyboardDevice>
    {
        public ControlKeyboardFocusManager(IStaticContainerControl root, ControlFocusManagerContext context)
            : base(root, context)
        {
        }

        #region Event handlers
        private void Control_KeyboardInputEnabledChanged(object sender, EventArgs e)
        {
            // If not currently focused control, return.
            if (!ReferenceEquals(Context.Keyboard, sender)) return;

            // If the control stopped accepting mouse input 
            // and it is the focused one, defocus it.
            if (!Context.Keyboard.AcceptsKeyboardInput)
                Context.Keyboard.Defocus();
        }
        #endregion

        private static IEnumerable<IControl> Controls(IStaticContainerControl container)
        {
            // Return the container being queried.
            yield return container;

            for (var i = 0; i < container.ControlsCount; i++)
            {
                if (container[i] is IStaticContainerControl inner)
                {
                    // Query all controls. This will also return the
                    // target control.
                    var controls = Controls(inner);

                    foreach (var control in controls)
                        yield return control;
                }
                else
                {
                    // Not a container, return next.
                    yield return container[i];
                }
            }
        }

        private static void Focus(IStaticContainerControl container, int index)
        {
            var controls = Controls(container).Where(c => c.VisibleFromParent).ToList();

            if (controls.Count == 0) return;

            var next = -1;

            if (index == controls.Max(c => c.TabIndex) || index == -1)
            {
                var candidates = controls.Where(c => c.TabIndex >= 0).ToList();

                if (candidates.Any())
                    next = candidates.Min(c => c.TabIndex);
            }
            else
            {
                var candidates = controls.Where(c => c.TabIndex > index).ToList();

                if (candidates.Any())
                {
                    candidates = controls.Where(c => c.TabIndex >= 0).ToList();

                    if (candidates.Count() != 0)
                        next = candidates.Min(c => c.TabIndex);
                }
                else
                {
                    next = candidates.Min(c => c.TabIndex);
                }
            }

            var control = controls.FirstOrDefault(c => next == c.TabIndex);

            if (control == null)
                throw new FatalRuntimeException("could not focus on any control");

            control.Focus();
        }

        protected override void Defocused(IControl control)
        {
            // If the focused control did not get defocused, skip focus changes.
            if (!ReferenceEquals(Context.Keyboard, control)) return;

            Context.Keyboard = null;
        }

        protected override void Disabled(IControl control)
        {
            // If the focused control did not get disabled, skip focus changes.
            if (!ReferenceEquals(Context.Mouse, control)) return;

            control.Defocus();
        }

        protected override void Focused(IControl control)
        {
            if (!Updating) return;

            if (Context.Keyboard?.HasFocus ?? false)
                Context.Keyboard.Defocus();

            Context.Keyboard = control;
        }

        protected override void HookCustomEvents(IControl control)
            => control.KeyboardInputEnabledChanged += Control_KeyboardInputEnabledChanged;

        protected override void UnhookCustomEvents(IControl control)
            => control.KeyboardInputEnabledChanged -= Control_KeyboardInputEnabledChanged;

        protected override void InternalUpdate(IGameEngineTime time, IKeyboardDevice device)
        {
            // Escape was pressed, defocus current.
            if (device.IsKeyPressed(Keys.Escape))
            {
                Context.Keyboard?.Defocus();

                return;
            }

            // No tab pressed, skip updates.
            if (!device.IsKeyPressed(Keys.Tab)) return;

            // Find next focused control.
            var container = Context.Mouse as IStaticContainerControl ?? Root;
            var index     = Context.Keyboard?.TabIndex ?? -1;

            if (Context.Keyboard != null && !container.Controls.Contains(Context.Keyboard))
                Context.Keyboard.Defocus();
            else
                index = Context.Mouse?.TabIndex ?? container.TabIndex;

            Context.Mouse?.Defocus();

            Focus(container, index);
        }
    }
}