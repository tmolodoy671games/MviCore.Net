using System;
using R3;
using SharpExtensions.R3;
using Single = SharpExtensions.R3.Single;

namespace MviCore.Net;

public class ReducerFeature<TWish, TState, TNews>(
    TState initialState,
    Reducer<TState, TWish> reducer,
    Bootstrapper<TWish>? bootstrapper = null,
    SimpleNewsPublisher<TWish, TState, TNews>? newsPublisher = null
) : BaseFeature<TWish, TWish, TWish, TState, TNews>(
    initialState, 
    wish => wish, (_, wish) => Single.Just(wish), reducer, bootstrapper,
    newsPublisher: newsPublisher == null ? null : (wish, _, state) => newsPublisher(wish, state)
)
{
    public ReducerFeature(
        TState initialState,
        IReducer<TState, TWish> reducer,
        IBootstrapper<TWish>? bootstrapper = null,
        ISimpleNewsPublisher<TWish, TState, TNews>? newsPublisher = null
    ) : this(initialState,
        (reducer ?? throw new ArgumentNullException(nameof(reducer))).Invoke,
        bootstrapper == null ? null : bootstrapper.Invoke,
        newsPublisher == null ? null : newsPublisher.Invoke)
    {
    }
}

public class ReducerFeature<TWish, TState>(
    TState initialState,
    Reducer<TState, TWish> reducer,
    Bootstrapper<TWish>? bootstrapper = null)
    : ReducerFeature<TWish, TState, Nothing>(initialState, reducer, bootstrapper)
{
    public ReducerFeature(
        TState initialState,
        IReducer<TState, TWish> reducer,
        IBootstrapper<TWish>? bootstrapper = null
    ) : this(initialState,
        (reducer ?? throw new ArgumentNullException(nameof(reducer))).Invoke,
        bootstrapper == null ? null : bootstrapper.Invoke)
    {
    }
}
