using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Fracture.Common.Util;

namespace Fracture.Common.Di
{
    /// <summary>
    /// Represents a dependency in the DI kernel.
    /// </summary>
    public sealed class Dependency : IEquatable<Dependency>
    {
        #region Fields
        /// <summary>
        /// Types this dependency can be assigned to.
        /// </summary>
        private readonly HashSet<Type> types;

        /// <summary>
        /// Actual value of the dependency.
        /// </summary>
        private readonly object value;

        /// <summary>
        /// Is this a strict dependency. If so the dependency
        /// can be assigned to strict types.
        /// </summary>
        private readonly bool strict;
        #endregion

        public Dependency(object value, Type [] types, bool strict)
        {
            Debug.Assert(types?.Length != 0);

            this.value  = value ?? throw new ArgumentNullException(nameof(value));
            this.types  = new HashSet<Type>(types ?? throw new ArgumentNullException(nameof(types)));
            this.strict = strict;
        }

        public T Cast<T>()
            => (T)value;

        public bool Castable<T>()
            => Castable(typeof(T));

        public bool Castable(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            var containedInTypes = types.Contains(type);

            if (strict)
                return containedInTypes;

            return containedInTypes || types.Any(type.IsAssignableFrom);
        }

        public bool ReferenceEquals(object other)
            => ReferenceEquals(value, other);

        public bool Equals(Dependency other)
            => other != null && ReferenceEquals(other.value);

        public override bool Equals(object obj)
            => Equals(obj as Dependency);

        public override string ToString()
            => $"{value.GetType().Name}";

        public override int GetHashCode()
            => HashUtils.Create()
                        .Append(types);
    }
}