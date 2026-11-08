using System;
using R3;

namespace MviCore.Net;

public interface IConsumer<in T>
{
    void Accept(T value);
}

public interface IStore<in TWish, TState> : IConsumer<TWish>
{
    TState State { get; }

    Observable<TState> States { get; }
}

public interface IFeature<in TWish, TState, TNews> : IStore<TWish, TState>, IDisposable
{
    Observable<TNews> News { get; }

    bool IsDisposed { get; }
}
