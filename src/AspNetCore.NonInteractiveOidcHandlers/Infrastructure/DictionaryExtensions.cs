using System.Collections.Generic;
using System.Diagnostics;

namespace AspNetCore.NonInteractiveOidcHandlers.Infrastructure
{
    internal static class DictionaryExtensions
    {
	    [DebuggerStepThrough]
	    public static void AddRange<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, IEnumerable<KeyValuePair<TKey, TValue>> items)
	    {
		    if (items == null)
			    return;

		    foreach (var item in items)
		    {
			    dictionary.Add(item);
		    }
	    }
    }
}
