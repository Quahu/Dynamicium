using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using Qommon;
using Binder = Microsoft.CSharp.RuntimeBinder.Binder;

namespace Dynamicium
{
    /// <summary>
    ///     Represents a <see langword="dynamic"/> wrapper around a native library,
    ///     dynamically mapping its exports to <see langword="delegate"/>s when methods are invoked.
    /// </summary>
    public class DynamicLibrary : DynamicObject, IDisposable
    {
        /// <summary>
        ///     Gets the handle to this library.
        /// </summary>
        public IntPtr Handle => _handle;

        private readonly IntPtr _handle;
        private readonly Dictionary<string, IntPtr> _procs;
        private readonly Dictionary<IntPtr, Delegate> _delegates;

        /// <summary>
        ///     Instantiates a new <see cref="DynamicLibrary"/> with the specified library name.
        /// </summary>
        /// <param name="name"> The name of the library. </param>
        public DynamicLibrary(string name)
            : this(NativeLibrary.Load(name, typeof(DynamicLibrary).Assembly, null))
        { }

        /// <summary>
        ///     Instantiates a new <see cref="DynamicLibrary"/> with the specified library handle.
        /// </summary>
        /// <param name="handle"> The handle to the library. </param>
        public DynamicLibrary(IntPtr handle)
        {
            Guard.IsNotDefault(handle);

            _handle = handle;
            _procs = new Dictionary<string, IntPtr>();
            _delegates = new Dictionary<IntPtr, Delegate>();
        }

        /// <inheritdoc/>
        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            Guard.IsNotNull(binder);
            args ??= Array.Empty<object>();

            var returnType = (bool) _resultDiscardedProperty.GetValue(binder)
                ? typeof(void)
                : binder.ReturnType;

            var genericTypeArguments = (Type[]) _typeArgumentsProperty.GetValue(binder);
            if (returnType == typeof(object))
            {
                if (genericTypeArguments.Length < 1)
                    Throw.InvalidOperationException($"Method call '{binder.Name}' must specify the return type "
                        + "or discard the result (assigning it to a discard variable does not work), "
                        + "if the method returns 'void'.");

                returnType = genericTypeArguments[^1];
            }

            var types = new Type[args.Length + 1];
            for (var i = 0; i < args.Length; i++)
            {
                args[i] ??= IntPtr.Zero;
                types[i] = args[i].GetType();
            }

            types[^1] = returnType;

            IntPtr proc;
            lock (_procs)
            {
                var name = binder.Name;
                if (!_procs.TryGetValue(name, out proc))
                {
                    if (!NativeLibrary.TryGetExport(_handle, name, out proc))
                    {
                        result = null;
                        return false;
                    }

                    _procs[name] = proc;
                }
            }

            Delegate @delegate;
            lock (_delegates)
            {
                if (!_delegates.TryGetValue(proc, out @delegate))
                {
                    var delegateType = MakeNewCustomDelegate(types);
                    @delegate = Marshal.GetDelegateForFunctionPointer(proc, delegateType);
                    _delegates[proc] = @delegate;
                }
            }

            result = @delegate.DynamicInvoke(args);
            return true;
        }

        /// <summary>
        ///     Disposes of this library, freeing <see cref="Handle"/>.
        /// </summary>
        public void Dispose()
        {
            NativeLibrary.Free(_handle);
        }

        private static readonly PropertyInfo _resultDiscardedProperty;
        private static readonly PropertyInfo _typeArgumentsProperty;

        private static readonly Func<Type[], Type> MakeNewCustomDelegate;

        static DynamicLibrary()
        {
            var binderType = typeof(Binder).Assembly.GetType("Microsoft.CSharp.RuntimeBinder.ICSharpInvokeOrInvokeMemberBinder");
            _resultDiscardedProperty = binderType.GetProperty("ResultDiscarded", BindingFlags.Instance | BindingFlags.Public);
            _typeArgumentsProperty = binderType.GetProperty("TypeArguments", BindingFlags.Instance | BindingFlags.Public);

            var delegateHelpersType = typeof(Expression).Assembly.GetType("System.Linq.Expressions.Compiler.DelegateHelpers");
            var method = delegateHelpersType.GetMethod("MakeNewCustomDelegate", BindingFlags.NonPublic | BindingFlags.Static);
            MakeNewCustomDelegate = method.CreateDelegate<Func<Type[], Type>>();
        }
    }
}
