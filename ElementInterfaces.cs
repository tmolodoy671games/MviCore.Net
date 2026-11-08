using R3;
using SharpExtensions;
using SharpExtensions.R3;

namespace MviCore.Net;

/// <summary>Maps a wish to an action.</summary>
public interface IWishToAction<in TWish, out TAction>
{
    /// <summary>Maps a wish to an action.</summary>
    TAction Invoke(TWish wish);
}

/// <summary>Creates one effect using the state at action arrival.</summary>
public interface IActor<in TState, in TAction, TEffect>
{
    /// <summary>Creates one effect using the state at action arrival.</summary>
    Single<TEffect> Invoke(TState state, TAction action);
}

/// <summary>Applies an effect to the latest state.</summary>
public interface IReducer<TState, in TEffect>
{
    /// <summary>Applies an effect to the latest state.</summary>
    TState Invoke(TState state, TEffect effect);
}

/// <summary>Creates the action stream subscribed to during construction.</summary>
public interface IBootstrapper<TAction>
{
    /// <summary>Creates the action stream subscribed to during construction.</summary>
    Observable<TAction> Invoke();
}

/// <summary>Optionally requests another action after reduction.</summary>
public interface IPostProcessor<TAction, in TEffect, in TState>
{
    /// <summary>Optionally requests another action after reduction.</summary>
    Optional<TAction> Invoke(TAction action, TEffect effect, TState state);
}

/// <summary>Optionally creates news after reduction and postprocessing.</summary>
public interface INewsPublisher<in TAction, in TEffect, in TState, TNews>
{
    /// <summary>Optionally creates news after reduction and postprocessing.</summary>
    Optional<TNews> Invoke(TAction action, TEffect effect, TState state);
}

/// <summary>Optionally creates news for a reducer-only feature.</summary>
public interface ISimpleNewsPublisher<in TWish, in TState, TNews>
{
    /// <summary>Optionally creates news for a reducer-only feature.</summary>
    Optional<TNews> Invoke(TWish wish, TState state);
}
