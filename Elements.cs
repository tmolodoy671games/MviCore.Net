using R3;
using SharpExtensions;

namespace MviCore.Net;

/// <summary>Maps a public wish to an internal action.</summary>
public delegate TAction WishToAction<in TWish, out TAction>(TWish wish);

/// <summary>Creates effects using the state at the time the action arrives.</summary>
public delegate Observable<TEffect> Actor<in TState, in TAction, TEffect>(TState state, TAction action);

/// <summary>Applies an effect to the latest state.</summary>
public delegate TState Reducer<TState, in TEffect>(TState state, TEffect effect);

/// <summary>Creates the action stream subscribed to once during feature construction.</summary>
public delegate Observable<TAction> Bootstrapper<TAction>();

/// <summary>Optionally requests another action after reduction.</summary>
public delegate Optional<TAction> PostProcessor<TAction, in TEffect, in TState>(
    TAction action, TEffect effect, TState state
);

/// <summary>Optionally creates a non-replayed event after reduction and postprocessing.</summary>
public delegate Optional<TNews> NewsPublisher<in TAction, in TEffect, in TState, TNews>(
    TAction action, TEffect effect, TState state
);

/// <summary>Creates an optional event for a reducer-only feature.</summary>
public delegate Optional<TNews> SimpleNewsPublisher<in TWish, in TState, TNews>(TWish wish, TState state);