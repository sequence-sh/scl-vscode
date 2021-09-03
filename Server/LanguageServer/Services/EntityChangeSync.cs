using Microsoft.Extensions.Configuration;

namespace LanguageServer.Services
{
    /// <summary>
    /// Basically an OptionsMonitor, but working I hope
    /// </summary>
    public class EntityChangeSync<T> where T : class, new()
    {
        /// <summary>
        /// Create a new EntityChangeSync
        /// </summary>
        public EntityChangeSync(IConfiguration configuration)
        {
            Latest = configuration.Get<T>()?? new T();
        }

        /// <summary>
        /// The latest configured value
        /// </summary>
        public T Latest { get; private set; }

        /// <summary>
        /// The change event handler
        /// </summary>
        public delegate void ChangeEventHandler(object sender, T entity);

        /// <summary>
        /// IS called whenever the value changes
        /// </summary>
        public event ChangeEventHandler? OnChange;

        // Wrap the event in a protected virtual method
        // to enable derived classes to raise the event.

        /// <summary>
        /// The method called whenever the entity changes
        /// </summary>
        /// <param name="entity"></param>
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