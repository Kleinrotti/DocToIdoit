using System;

namespace DocToIdoit
{
    /// <summary>
    /// Defines functions to interact with i-doit.
    /// </summary>
    internal interface IIdoitWorker : IDisposable
    {
        /// <summary>
        /// Create a new idoit object.
        /// </summary>
        /// <param name="product"></param>
        /// <returns>True on success, false if object already exists.</returns>
        bool CreateObject(Product product);
    }
}