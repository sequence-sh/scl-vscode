using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace LanguageServer.Services
{
    public class ReactiveSource<T, TOption> where TOption : class
    {
        public readonly Func<TOption, Task<T>> CreateFunc;

        public readonly EntityChangeSync<TOption> OptionsMonitor;

        public ReactiveSource(Func<TOption, Task<T>> createFunc, EntityChangeSync<TOption> optionsMonitor)
        {
            CreateFunc = createFunc;
            OptionsMonitor = optionsMonitor;

            Value = new AsyncLazy(() => createFunc(optionsMonitor.Latest));
            OptionsMonitor.OnChange += (_, newValue)=> Value = new AsyncLazy(() => createFunc(newValue));
        }


        public AsyncLazy Value { get; private set; }


        public class AsyncLazy: Lazy<Task<T>>
        {
            public AsyncLazy(Func<Task<T>> taskFactory) :
                base(taskFactory)
            {
            }

            public TaskAwaiter<T> GetAwaiter()
            {
                return Value.GetAwaiter();
            }
        }
    }
}