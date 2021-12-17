using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace LanguageServer.Services;

/// <summary>
/// Creates an object asynchronously
/// </summary>
public class ReactiveSource<T, TOption> where TOption : class, new()
{
    /// <summary>
    /// The function for creating the T
    /// </summary>
    public readonly Func<TOption, Task<T>> CreateFunc;

    /// <summary>
    /// The OptionsMonitor
    /// </summary>
    public readonly EntityChangeSync<TOption> OptionsMonitor;

    /// <summary>
    /// Create a new ReactiveSource
    /// </summary>
    public ReactiveSource(Func<TOption, Task<T>> createFunc, EntityChangeSync<TOption> optionsMonitor)
    {
        CreateFunc = createFunc;
        OptionsMonitor = optionsMonitor;

        Value = new AsyncLazy(() => createFunc(optionsMonitor.Latest));
        OptionsMonitor.OnChange += (_, newValue)=> Value = new AsyncLazy(() => createFunc(newValue));
    }

    /// <summary>
    /// The AsyncLazy
    /// </summary>
    public AsyncLazy Value { get; private set; }


    /// <summary>
    /// An asynchronous lazy object
    /// </summary>
    public class AsyncLazy: Lazy<Task<T>>
    {
        /// <summary>
        /// Create a new AsyncLazy
        /// </summary>
        public AsyncLazy(Func<Task<T>> taskFactory) :
            base(taskFactory)
        {
        }

        /// <summary>
        /// Get the Task Awaiter
        /// </summary>
        public TaskAwaiter<T> GetAwaiter()
        {
            return Value.GetAwaiter();
        }
    }
}