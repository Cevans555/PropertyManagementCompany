using System.Linq.Expressions;

namespace PropertyManagement.Web.Services;

internal static class QueryableSortExtensions
{
    public static IOrderedQueryable<T> OrderBy<T, TKey>(
        this IQueryable<T> source, Expression<Func<T, TKey>> key, bool descending)
    {
        if (descending)
            return source.OrderByDescending(key);

        return source.OrderBy(key);
    }

    public static IOrderedQueryable<T> ThenBy<T, TKey>(
        this IOrderedQueryable<T> source, Expression<Func<T, TKey>> key, bool descending)
    {
        if (descending)
            return source.ThenByDescending(key);

        return source.ThenBy(key);
    }
}
