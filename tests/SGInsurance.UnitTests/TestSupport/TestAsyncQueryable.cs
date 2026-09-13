using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace SGInsurance.UnitTests.TestSupport;

/// <summary>
/// Minimal IAsyncQueryProvider/IAsyncEnumerable shim so plain in-memory lists can
/// be used behind IRepository&lt;T&gt;.Query() in unit tests wherever a service calls
/// an EF Core async LINQ operator (CountAsync/ToListAsync/FirstOrDefaultAsync/etc.)
/// against it via Moq, without needing a real database.
/// </summary>
public static class TestAsyncQueryableExtensions
{
    public static IQueryable<T> AsTestAsyncQueryable<T>(this IEnumerable<T> source) => new TestAsyncEnumerable<T>(source);
}

internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
    public TestAsyncEnumerable(Expression expression) : base(expression) { }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
        new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}

internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;
    public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;
    public T Current => _inner.Current;
    public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(_inner.MoveNext());
    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return ValueTask.CompletedTask;
    }
}

internal class TestAsyncQueryProvider<T> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;
    public TestAsyncQueryProvider(IQueryProvider inner) => _inner = inner;

    public IQueryable CreateQuery(Expression expression) => new TestAsyncEnumerable<T>(expression);
    public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new TestAsyncEnumerable<TElement>(expression);
    public object? Execute(Expression expression) => _inner.Execute(expression);
    public TResult Execute<TResult>(Expression expression) => _inner.Execute<TResult>(expression);

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        // TResult is Task<X> for async LINQ operators (CountAsync/FirstOrDefaultAsync/etc.)
        var resultType = typeof(TResult).GetGenericArguments().FirstOrDefault() ?? typeof(object);
        var executionResult = typeof(IQueryProvider)
            .GetMethod(nameof(IQueryProvider.Execute), 1, new[] { typeof(Expression) })!
            .MakeGenericMethod(resultType)
            .Invoke(_inner, new object[] { expression });

        var taskFromResultMethod = typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(resultType);
        return (TResult)taskFromResultMethod.Invoke(null, new[] { executionResult })!;
    }
}
