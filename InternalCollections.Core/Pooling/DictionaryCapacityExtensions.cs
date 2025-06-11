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
    {
        if(dictionary == null)
        {
            return 0;
        }

#if NET9_0_OR_GREATER
        return dictionary.Capacity;
#else
        var field = CapacityGetter<TKey, TValue>.EntriesField;
        var entries = (Array)field.GetValue(dictionary)!;

        if (entries == null)
        {
            return 0;
        }

        return entries.Length;
#endif
    }

#if !NET9_0_OR_GREATER
    static class CapacityGetter<TKey, TValue>
    {
        public static readonly FieldInfo EntriesField = typeof(Dictionary<TKey, TValue>)
            .GetField("_entries", BindingFlags.NonPublic | BindingFlags.Instance);
    }
#endif
}