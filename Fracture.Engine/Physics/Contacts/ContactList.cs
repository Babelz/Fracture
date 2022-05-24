using System;
using System.Collections.Generic;
using Fracture.Engine.Physics.Dynamics;

namespace Fracture.Engine.Physics.Contacts
{
    /// <summary>
    /// List containing last active contacts for single body.
    /// </summary>
    public sealed class ContactList
    {
        #region Constant fields
        private const int Capacity = 32;
        #endregion

        #region Fields
        private HashSet<Body> oldContacts;
        private HashSet<Body> newContacts;

        private ulong last;
        #endregion

        #region Properties
        /// <summary>
        /// Returns old, leaving contacts.
        /// </summary>
        public IEnumerable<Body> LeavingContacts
        {
            get
            {
                foreach (var oldContact in oldContacts)
                {
                    if (!newContacts.Contains(oldContact))
                        yield return oldContact;
                }
            }
        }

        /// <summary>
        /// Returns new, entering contacts.
        /// </summary>
        public IEnumerable<Body> EnteringContacts
        {
            get
            {
                foreach (var newContact in newContacts)
                {
                    if (oldContacts.Contains(newContact))
                        yield return newContact;
                }
            }
        }

        /// <summary>
        /// Returns current contacts.
        /// </summary>
        public IEnumerable<Body> CurrentContacts
            => oldContacts;

        /// <summary>
        /// Body that owns this contact list.
        /// </summary>
        public Body Body
        {
            get;
        }
        #endregion

        public ContactList(Body body)
        {
            Body        = body ?? throw new ArgumentNullException(nameof(body));
            oldContacts = new HashSet<Body>(Capacity);
            newContacts = new HashSet<Body>(Capacity);
        }
        
        /// <summary>
        /// Adds new body to contact list.
        /// </summary>
        public void Add(Body body, ulong frame)
        {
            if (last < frame)
            {
                // Swap and clear.
                var temp = oldContacts;

                oldContacts = newContacts;
                newContacts = temp;

                newContacts.Clear();

                last = frame;
            }

            newContacts.Add(body);
        }
    }
}
