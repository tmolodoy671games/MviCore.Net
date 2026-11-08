using System;

namespace MviCore.Net;

public class ActorReducerFeature<TWish, TEffect, TState, TNews>(
    TState initialState,
    Actor<TState, TWish, TEffect> actor,
    Reducer<TState, TEffect> reducer,
    Bootstrapper<TWish>? bootstrapper = null,
    NewsPublisher<TWish, TEffect, TState, TNews>? newsPublisher = null
) : BaseFeature<TWish, TWish, TEffect, TState, TNews>(
    initialState,
    wish => wish,
    actor,
    reducer,
    bootstrapper,
    newsPublisher: newsPublisher
)
{
    public ActorReducerFeature(
        TState initialState,
        IActor<TState, TWish, TEffect> actor,
        IReducer<TState, TEffect> reducer,
        IBootstrapper<TWish>? bootstrapper = null,
        INewsPublisher<TWish, TEffect, TState, TNews>? newsPublisher = null
    ) : this(initialState,
        (actor ?? throw new ArgumentNullException(nameof(actor))).Invoke,
        (reducer ?? throw new ArgumentNullException(nameof(reducer))).Invoke,
        bootstrapper == null ? null : bootstrapper.Invoke,
        newsPublisher == null ? null : newsPublisher.Invoke)
    {
    }
}

public class ActorReducerFeature<TWish, TEffect, TState>(
    TState initialState,
    Actor<TState, TWish, TEffect> actor,
    Reducer<TState, TEffect> reducer,
    Bootstrapper<TWish>? bootstrapper = null
) : ActorReducerFeature<TWish, TEffect, TState, Nothing>(initialState, actor, reducer, bootstrapper)
{
    public ActorReducerFeature(
        TState initialState,
        IActor<TState, TWish, TEffect> actor,
        IReducer<TState, TEffect> reducer,
        IBootstrapper<TWish>? bootstrapper = null
    ) : this(initialState,
        (actor ?? throw new ArgumentNullException(nameof(actor))).Invoke,
        (reducer ?? throw new ArgumentNullException(nameof(reducer))).Invoke,
        bootstrapper == null ? null : bootstrapper.Invoke)
    {
    }
}
