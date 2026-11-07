using R3;

namespace MviCore.Net;

/// <summary>A feature whose wishes are applied directly as effects.</summary>
public class ReducerFeature<TWish, TState, TNews> : BaseFeature<TWish, TWish, TWish, TState, TNews>
{
    /// <summary>Creates a reducer-only feature with optional bootstrapping and news.</summary>
    public ReducerFeature(TState initialState, Reducer<TState, TWish> reducer,
        Bootstrapper<TWish>? bootstrapper = null, SimpleNewsPublisher<TWish, TState, TNews>? newsPublisher = null)
        : base(initialState, wish => wish, (_, wish) => Observable.Return(wish), reducer, bootstrapper,
            newsPublisher: newsPublisher == null ? null : (wish, _, state) => newsPublisher(wish, state)) { }
}

/// <summary>A reducer-only feature without news.</summary>
public class ReducerFeature<TWish, TState> : ReducerFeature<TWish, TState, Nothing>
{
    /// <summary>Creates a reducer-only feature without a news publisher.</summary>
    public ReducerFeature(TState initialState, Reducer<TState, TWish> reducer, Bootstrapper<TWish>? bootstrapper = null)
        : base(initialState, reducer, bootstrapper) { }
}
