using System;
using System.Collections.Generic;
using System.Linq;
using Fracture.Engine.Physics.Dynamics;

namespace Fracture.Engine.Physics.Contacts
{
    /// <summary>
    /// List containing last active contacts for single body.
    /// </summary>
    public sealed class ContactList
    {
        #region Fields
        private HashSet<int> oldContacts;
        private HashSet<int> newContacts;
        #endregion

        #region Properties
        /// <summary>
        /// Returns old, leaving contacts.
        /// </summary>
        public IEnumerable<int> LeavingContacts => oldContacts.Where(oldContact => !newContacts.Contains(oldContact));

        /// <summary>
        /// Returns new, entering contacts.
        /// </summary>
        public IEnumerable<int> EnteringContacts => newContacts.Where(newContact => oldContacts.Contains(newContact));

        /// <summary>
        /// Returns current contacts.
        /// </summary>
        public IEnumerable<int> CurrentContacts => oldContacts;

        /// <summary>
        /// Body that owns this contact list.
        /// </summary>
        public int BodyId
        {
            get;
        }
        #endregion

        public ContactList(int bodyId)
        {
            BodyId      = bodyId;
            oldContacts = new HashSet<int>();
            newContacts = new HashSet<int>();
        }

        /// <summary>
        /// Adds new body to contact list.
        /// </summary>
        public void Add(int bodyId)
            => newContacts.Add(bodyId);

        public void Update()
        {
            (oldContacts, newContacts) = (newContacts, oldContacts);

            newContacts.Clear();
        }
    }
}