using System;
using System.Collections.Concurrent;

namespace Logic.LockManager
{
    /// <summary>
    /// Generic thread-safe lock manager for entities of type T.
    /// Manages locks using ConcurrentDictionary to ensure thread safety across multiple users.
    /// Supports both Guid and string-based entity IDs.
    /// </summary>
    /// <typeparam name="T">The entity type marker (e.g., Contracts, Accounts)</typeparam>
    public class EntityLockManager<T>
    {
        /// <summary>
        /// Thread-safe dictionary storing entity locks for Guid keys.
        /// Key: Entity ID (Guid), Value: User who holds the lock (string)
        /// </summary>
        private readonly ConcurrentDictionary<Guid, string> _locks;

        /// <summary>
        /// Thread-safe dictionary storing entity locks for string keys.
        /// Key: Entity ID (string), Value: User who holds the lock (string)
        /// </summary>
        private readonly ConcurrentDictionary<string, string> _stringLocks;

        public EntityLockManager()
        {
            _locks = new ConcurrentDictionary<Guid, string>();
            _stringLocks = new ConcurrentDictionary<string, string>();
        }

        /// <summary>
        /// Attempts to acquire a lock on the specified entity for the given user.
        /// </summary>
        /// <param name="entityId">The unique identifier of the entity to lock</param>
        /// <param name="user">The user attempting to acquire the lock</param>
        /// <returns>
        /// True if the lock was successfully acquired or the user already holds the lock;
        /// False if another user already holds the lock
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when user is null or empty</exception>
        public bool TryLock(Guid entityId, string user)
        {
            if (string.IsNullOrWhiteSpace(user))
                throw new ArgumentNullException(nameof(user), "User cannot be null or empty");

            // Try to add the lock. If it already exists, check if it's held by the same user
            var lockHolder = _locks.AddOrUpdate(
                entityId,
                user, // Value to add if key doesn't exist
                (key, existingUser) => existingUser == user ? user : existingUser // Keep existing if different user
            );

            // Return true if we got the lock or we already had it
            return lockHolder == user;
        }

        /// <summary>
        /// Attempts to release a lock on the specified entity.
        /// Only the user who holds the lock can release it.
        /// </summary>
        /// <param name="entityId">The unique identifier of the entity to unlock</param>
        /// <param name="user">The user attempting to release the lock</param>
        /// <returns>
        /// True if the lock was successfully released or no lock existed;
        /// False if the lock is held by a different user
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when user is null or empty</exception>
        public bool Unlock(Guid entityId, string user)
        {
            if (string.IsNullOrWhiteSpace(user))
                throw new ArgumentNullException(nameof(user), "User cannot be null or empty");

            // Try to get the current lock holder
            if (!_locks.TryGetValue(entityId, out string currentHolder))
            {
                // No lock exists, consider this a successful unlock
                return true;
            }

            // Only allow the lock holder to unlock
            if (currentHolder != user)
            {
                return false;
            }

            // Remove the lock
            return _locks.TryRemove(entityId, out _);
        }

        /// <summary>
        /// Checks if the specified entity is currently locked.
        /// </summary>
        /// <param name="entityId">The unique identifier of the entity to check</param>
        /// <returns>True if the entity is locked; False otherwise</returns>
        public bool IsLocked(Guid entityId)
        {
            return _locks.ContainsKey(entityId);
        }

        /// <summary>
        /// Gets the user who currently holds the lock on the specified entity.
        /// </summary>
        /// <param name="entityId">The unique identifier of the entity to check</param>
        /// <returns>The user who holds the lock, or null if no lock exists</returns>
        public string GetLockHolder(Guid entityId)
        {
            _locks.TryGetValue(entityId, out string lockHolder);
            return lockHolder;
        }

        /// <summary>
        /// Checks if the specified user holds the lock on the specified entity.
        /// </summary>
        /// <param name="entityId">The unique identifier of the entity to check</param>
        /// <param name="user">The user to check</param>
        /// <returns>True if the user holds the lock; False otherwise</returns>
        /// <exception cref="ArgumentNullException">Thrown when user is null or empty</exception>
        public bool IsLockedByUser(Guid entityId, string user)
        {
            if (string.IsNullOrWhiteSpace(user))
                throw new ArgumentNullException(nameof(user), "User cannot be null or empty");

            return GetLockHolder(entityId) == user;
        }

        /// <summary>
        /// Forces the removal of a lock regardless of who holds it.
        /// This method should be used with caution, typically for administrative purposes.
        /// </summary>
        /// <param name="entityId">The unique identifier of the entity to force unlock</param>
        /// <returns>True if a lock was removed; False if no lock existed</returns>
        public bool ForceUnlock(Guid entityId)
        {
            return _locks.TryRemove(entityId, out _);
        }

        /// <summary>
        /// Gets the total number of active locks for this entity type.
        /// </summary>
        /// <returns>The number of active locks</returns>
        public int GetActiveLockCount()
        {
            return _locks.Count + _stringLocks.Count;
        }

        // ===== STRING-BASED METHODS =====

        /// <summary>
        /// Attempts to acquire a lock on the specified entity for the given user.
        /// </summary>
        /// <param name="entityId">The unique identifier of the entity to lock (string)</param>
        /// <param name="user">The user attempting to acquire the lock</param>
        /// <returns>
        /// True if the lock was successfully acquired or the user already holds the lock;
        /// False if another user already holds the lock
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when user is null or empty</exception>
        public bool TryLock(string entityId, string user)
        {
            if (string.IsNullOrWhiteSpace(user))
                throw new ArgumentNullException(nameof(user), "User cannot be null or empty");
            if (string.IsNullOrWhiteSpace(entityId))
                throw new ArgumentNullException(nameof(entityId), "EntityId cannot be null or empty");

            // Try to add the lock. If it already exists, check if it's held by the same user
            var lockHolder = _stringLocks.AddOrUpdate(
                entityId,
                user, // Value to add if key doesn't exist
                (key, existingUser) => existingUser == user ? user : existingUser // Keep existing if different user
            );

            // Return true if we got the lock or we already had it
            return lockHolder == user;
        }

        /// <summary>
        /// Attempts to release a lock on the specified entity.
        /// Only the user who holds the lock can release it.
        /// </summary>
        /// <param name="entityId">The unique identifier of the entity to unlock (string)</param>
        /// <param name="user">The user attempting to release the lock</param>
        /// <returns>
        /// True if the lock was successfully released or no lock existed;
        /// False if the lock is held by a different user
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when user is null or empty</exception>
        public bool Unlock(string entityId, string user)
        {
            if (string.IsNullOrWhiteSpace(user))
                throw new ArgumentNullException(nameof(user), "User cannot be null or empty");
            if (string.IsNullOrWhiteSpace(entityId))
                throw new ArgumentNullException(nameof(entityId), "EntityId cannot be null or empty");

            // Try to get the current lock holder
            if (!_stringLocks.TryGetValue(entityId, out string currentHolder))
            {
                // No lock exists, consider this a successful unlock
                return true;
            }

            // Only allow the lock holder to unlock
            if (currentHolder != user)
            {
                return false;
            }

            // Remove the lock
            return _stringLocks.TryRemove(entityId, out _);
        }

        /// <summary>
        /// Checks if the specified entity is currently locked.
        /// </summary>
        /// <param name="entityId">The unique identifier of the entity to check (string)</param>
        /// <returns>True if the entity is locked; False otherwise</returns>
        public bool IsLocked(string entityId)
        {
            if (string.IsNullOrWhiteSpace(entityId))
                return false;
            return _stringLocks.ContainsKey(entityId);
        }

        /// <summary>
        /// Gets the user who currently holds the lock on the specified entity.
        /// </summary>
        /// <param name="entityId">The unique identifier of the entity to check (string)</param>
        /// <returns>The user who holds the lock, or null if no lock exists</returns>
        public string GetLockHolder(string entityId)
        {
            if (string.IsNullOrWhiteSpace(entityId))
                return null;
            _stringLocks.TryGetValue(entityId, out string lockHolder);
            return lockHolder;
        }

        /// <summary>
        /// Checks if the specified user holds the lock on the specified entity.
        /// </summary>
        /// <param name="entityId">The unique identifier of the entity to check (string)</param>
        /// <param name="user">The user to check</param>
        /// <returns>True if the user holds the lock; False otherwise</returns>
        /// <exception cref="ArgumentNullException">Thrown when user is null or empty</exception>
        public bool IsLockedByUser(string entityId, string user)
        {
            if (string.IsNullOrWhiteSpace(user))
                throw new ArgumentNullException(nameof(user), "User cannot be null or empty");
            if (string.IsNullOrWhiteSpace(entityId))
                return false;

            return GetLockHolder(entityId) == user;
        }

        /// <summary>
        /// Forces the removal of a lock regardless of who holds it.
        /// This method should be used with caution, typically for administrative purposes.
        /// </summary>
        /// <param name="entityId">The unique identifier of the entity to force unlock (string)</param>
        /// <returns>True if a lock was removed; False if no lock existed</returns>
        public bool ForceUnlock(string entityId)
        {
            if (string.IsNullOrWhiteSpace(entityId))
                return false;
            return _stringLocks.TryRemove(entityId, out _);
        }

        /// <summary>
        /// Removes all locks held by the specified user.
        /// Useful for cleanup when a user logs out or session expires.
        /// </summary>
        /// <param name="user">The user whose locks should be removed</param>
        /// <returns>The number of locks that were removed</returns>
        /// <exception cref="ArgumentNullException">Thrown when user is null or empty</exception>
        public int RemoveAllLocksForUser(string user)
        {
            if (string.IsNullOrWhiteSpace(user))
                throw new ArgumentNullException(nameof(user), "User cannot be null or empty");

            int removedCount = 0;
            
            // Handle Guid-based locks
            var guidKeysToRemove = new System.Collections.Generic.List<Guid>();
            foreach (var kvp in _locks)
            {
                if (kvp.Value == user)
                {
                    guidKeysToRemove.Add(kvp.Key);
                }
            }
            foreach (var key in guidKeysToRemove)
            {
                if (_locks.TryRemove(key, out string removedUser) && removedUser == user)
                {
                    removedCount++;
                }
            }

            // Handle string-based locks
            var stringKeysToRemove = new System.Collections.Generic.List<string>();
            foreach (var kvp in _stringLocks)
            {
                if (kvp.Value == user)
                {
                    stringKeysToRemove.Add(kvp.Key);
                }
            }
            foreach (var key in stringKeysToRemove)
            {
                if (_stringLocks.TryRemove(key, out string removedUser) && removedUser == user)
                {
                    removedCount++;
                }
            }

            return removedCount;
        }
    }
}
