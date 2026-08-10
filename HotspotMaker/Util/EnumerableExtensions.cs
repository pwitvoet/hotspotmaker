using System.Collections.Generic;
using System.Linq;

namespace HotspotMaker.Util
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> enumerable)
            => enumerable.Where(item => item != null)!;
    }
}
