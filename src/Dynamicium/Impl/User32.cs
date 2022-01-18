using System.Runtime.Versioning;

namespace Dynamicium
{
    /// <summary>
    ///     Represents a <see cref="DynamicLibrary"/> instance for <c>user32.dll</c>.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public sealed class User32 : DynamicLibrary
    {
        /// <summary>
        ///     Instantiates a new <see cref="User32"/>.
        /// </summary>
        public User32()
            : base("user32")
        { }
    }
}
