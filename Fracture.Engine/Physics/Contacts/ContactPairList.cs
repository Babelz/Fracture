using System.Linq;
using Fracture.Common.Collections;
using Fracture.Engine.Physics.Dynamics;

namespace Fracture.Engine.Physics.Contacts
{
    public sealed class ContactPairList
    {
        #region Constant fields
        // Bucket size for contact array.
        private const int BucketSize = 32;
        #endregion

        #region Fields
        // Array containing indices to contacts.
        private readonly LinearGrowthArray<Body> pairs;
        
        // Flags field for each contact index to determine if
        // the contact was handled.
        private readonly LinearGrowthArray<bool> handled;
        #endregion

        #region Properties
        public int Count
        {
            get;
            private set;
        }

        public int Handled
        {
            get;
            private set;
        }

        /// <summary>
        /// Body that owns the contact list.
        /// </summary>
        public Body Body
        {
            get;
        }
        #endregion

        public ContactPairList(Body body)
        {
            Body = body;

            pairs   = new LinearGrowthArray<Body>(BucketSize);
            handled = new LinearGrowthArray<bool>(BucketSize);
        }

        public bool Paired(Body pair)
            => pairs.Contains(pair);

        public void Pair(Body pair)
        {
            if (Count >= pairs.Length)
            {
                pairs.Grow();
                handled.Grow();
            }

            pairs.Insert(Count, pair);
            handled.Insert(Count, false);

            Count++;
        }

        public bool IsHandled(int index)
            => handled.AtIndex(index);

        public void MarkHandled(int index)
        {
            handled.Insert(index, true);

            Handled++;
        }
        
        public Body AtIndex(int index)
            => pairs.AtIndex(index);

        /// <summary>
        /// Clears the contact list.
        /// </summary>
        public void Clear()
        {
            Count   = 0;
            Handled = 0;
        }
    }
}
