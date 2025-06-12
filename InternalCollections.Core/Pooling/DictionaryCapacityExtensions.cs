using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace InternalCollections.Pooling;

/// <summary>
/// Provides extension methods to access internal dictionary metadata.
/// </summary>
internal static class DictionaryCapacityExtensions
{

    /// <summary>
    /// Gets the internal capacity of a <see cref="Dictionary{TKey, TValue}"/>.
    /// In .NET 9+, this uses the public <c>Capacity</c> property; in earlier versions, uses reflection.
    /// </summary>
    /// <typeparam name="TKey">The type of dictionary keys.</typeparam>
    /// <typeparam name="TValue">The type of dictionary values.</typeparam>
    /// <param name="dictionary">The dictionary instance.</param>
    /// <returns>The capacity (number of internal entry slots).</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetCapacity<TKey, TValue>(this Dictionary<TKey, TValue>? dictionary)
        where TKey : notnull
    {
        if (dictionary == null)
        {
            return 0;
        }

#if NET9_0_OR_GREATER
        return dictionary.Capacity;
#else
        return CapacityGetter<TKey, TValue>.GetInternalCapacity(dictionary);
#endif
    }

#if !NET9_0_OR_GREATER
    static class CapacityGetter<TKey, TValue>
        where TKey : notnull
    {
#if NET8_0
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_entries")]
        public static extern Array GetEntriesField(Dictionary<TKey, TValue> dictionary);
#else
        public static readonly FieldInfo EntriesField = typeof(Dictionary<TKey, TValue>)
            .GetField("_entries", BindingFlags.NonPublic | BindingFlags.Instance)!;
#endif

        public static int GetInternalCapacity(Dictionary<TKey, TValue> dictionary)
        {
#if NET8_0
            var entries = GetEntriesField(dictionary)!;
#else
            var entries = (Array)EntriesField.GetValue(dictionary)!;
#endif

            return entries?.Length ?? 0;
        }
    }
#endif
}