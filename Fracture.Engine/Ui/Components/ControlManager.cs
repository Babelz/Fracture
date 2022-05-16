using System;
using System.Collections.Generic;
using Fracture.Engine.Ui.Controls;

namespace Fracture.Engine.Ui.Components
{
    /// <summary>
    /// Interface for implementing control enumerators. Enumerators contain
    /// events for changes and allow enumeration of controls.
    /// </summary>
    public interface IControlEnumerator
    {
        #region Events
        event EventHandler<ControlEventArgs> ControlAdded;
        event EventHandler<ControlEventArgs> ControlRemoved;
        #endregion

        #region Indexers
        /// <summary>
        /// Returns control at given index.
        /// </summary>
        IControl this[int index]
        {
            get;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Returns the count of controls.
        /// </summary>
        int ControlsCount
        {
            get;
        }

        /// <summary>
        /// Returns all controls inside this enumerator.
        /// </summary>
        IEnumerable<IControl> Controls
        {
            get;
        }
        #endregion
    }

    /// <summary>
    /// Interface for implementing control collections. Collections
    /// provide storage for children controls.
    /// </summary>
    public interface IControlCollection
    {
        void Add(IControl control);

        void Remove(IControl control);

        void Clear();
    }

    /// <summary>
    /// Interface for implementing control managers. Managers combine functionalities
    /// of control containers and enumerators.
    /// </summary>
    public interface IControlManager : IControlCollection, IControlEnumerator
    {
        // No implementation specific members, joins two interfaces.
    }

    /// <summary>
    /// Class that contains and manages controls.
    /// </summary>
    public sealed class ControlManager : IControlManager
    {
        #region Fields
        private readonly List<IControl> controls;
        #endregion

        #region Events
        public event EventHandler<ControlEventArgs> ControlAdded;
        public event EventHandler<ControlEventArgs> ControlRemoved;
        #endregion

        #region Properties
        public int ControlsCount => controls.Count;

        public IEnumerable<IControl> Controls => controls;
        #endregion

        #region Indexers
        public IControl this[int index] => controls[index];
        #endregion

        public ControlManager() => controls = new List<IControl>();

        public void Add(IControl control)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            if (controls.Contains(control))
                throw new InvalidCastException($"duplicated control {control}");

            controls.Add(control);

            ControlAdded?.Invoke(this, new ControlEventArgs(control));
        }

        public void Remove(IControl control)
        {
            if (!controls.Contains(control))
                throw new InvalidOperationException($"control {control} does not exist");

            controls.Remove(control);

            ControlRemoved?.Invoke(this, new ControlEventArgs(control));
        }

        public void Clear()
        {
            var controlsCopy = new List<IControl>(controls);

            for (var i = 0; i < controlsCopy.Count; i++)
                Remove(controlsCopy[i]);
        }
    }
}