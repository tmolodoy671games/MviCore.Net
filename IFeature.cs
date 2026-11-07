using System;
using R3;

namespace MviCore.Net;

/// <summary>An endpoint accepting wishes.</summary>
public interface IConsumer<in T>
{
    /// <summary>Processes a value synchronously on the caller's thread.</summary>
    void Accept(T value);
}

/// <summary>A wish consumer with a current state and a replaying state stream.</summary>
public interface IStore<in TWish, TState> : IConsumer<TWish>
{
    /// <summary>The latest state.</summary>
    TState State { get; }

    /// <summary>Emits the current state on subscription and every subsequent reduction.</summary>
    Observable<TState> States { get; }
}

/// <summary>A disposable store with a non-replaying news stream.</summary>
public interface IFeature<in TWish, TState, TNews> : IStore<TWish, TState>, IDisposable
{
    /// <summary>Events delivered only to current subscribers.</summary>
    Observable<TNews> News { get; }

    /// <summary>Whether the feature has been disposed.</summary>
    bool IsDisposed { get; }
}
