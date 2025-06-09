// This file is ported and adapted from the .NET source code (dotnet/runtime)
// https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/Dictionary.cs

using System;
using System.Collections.Generic;
using System.Text;

namespace InternalCollections;

public struct HashEntry<TKey, TValue>
{
    internal uint HashCode;
    internal int Next;
    internal TKey Key;
    internal TValue Value;
}
