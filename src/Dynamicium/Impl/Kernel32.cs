using System.Runtime.Versioning;

namespace Dynamicium
{
    /// <summary>
    ///     Represents a <see cref="DynamicLibrary"/> instance for <c>kernel32.dll</c>.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public sealed class Kernel32 : DynamicLibrary
    {
        /// <summary>
        ///     Instantiates a new <see cref="Kernel32"/>.
        /// </summary>
        public Kernel32()
            : base("kernel32")
        { }
    }
}
