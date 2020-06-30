using System;

namespace PaZword.Api
{
    /// <summary>
    /// Provides an <see cref="IEquatable{T}"/> implementation that does a minimal equality of 2 objects,
    /// and an additional <see cref="ExactEquals"/> method that compares the entire state of the object.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IExactEquatable<T> : IEquatable<T>
    {
        /// <summary>
        /// Indicates whether the current object is exactly equal to another object of the same type by comparing
        /// it's state (but not reference equality).
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>Returns <code>true</code> if the current object is equal to the other parameter.</returns>
        bool ExactEquals(T other);
    }
}
