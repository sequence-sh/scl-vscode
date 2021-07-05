using Microsoft.Extensions.Configuration;

namespace LanguageServer.Services
{
    /// <summary>
    /// Basically an OptionsMonitor, but working I hope
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EntityChangeSync<T> where T : class
    {
        public EntityChangeSync(IConfiguration configuration)
        {
            Latest = configuration.Get<T>();
        }

        public T Latest { get; private set; }

        // Declare the delegate (if using non-generic pattern).
        public delegate void ChangeEventHandler(object sender, T entity);

        // Declare the event.
        public event ChangeEventHandler? OnChange;

        // Wrap the event in a protected virtual method
        // to enable derived classes to raise the event.
        public virtual void EntityHasChanged(T entity)
        {
            Latest = entity;
            // Raise the event in a thread-safe manner using the ?. operator.
            OnChange?.Invoke(this, entity);
        }

        /// <summary>
        /// Returns whether the entity has changed
        /// </summary>
        public bool TryUpdate(T newEntity)
        {
            if (newEntity.Equals(Latest))
                return false;
            EntityHasChanged(newEntity);
            return true;
        }
    }
}