namespace MviCore.Net;

/// <summary>A feature whose wishes are passed directly to its actor.</summary>
public class ActorReducerFeature<TWish, TEffect, TState, TNews> : BaseFeature<TWish, TWish, TEffect, TState, TNews>
{
    /// <summary>Creates a feature with an actor and reducer, without internal actions.</summary>
    public ActorReducerFeature(TState initialState, Actor<TState, TWish, TEffect> actor,
        Reducer<TState, TEffect> reducer, Bootstrapper<TWish>? bootstrapper = null,
        NewsPublisher<TWish, TEffect, TState, TNews>? newsPublisher = null)
        : base(initialState, wish => wish, actor, reducer, bootstrapper, newsPublisher: newsPublisher) { }
}

/// <summary>An actor/reducer feature without news.</summary>
public class ActorReducerFeature<TWish, TEffect, TState> : ActorReducerFeature<TWish, TEffect, TState, Nothing>
{
    /// <summary>Creates an actor/reducer feature without a news publisher.</summary>
    public ActorReducerFeature(TState initialState, Actor<TState, TWish, TEffect> actor,
        Reducer<TState, TEffect> reducer, Bootstrapper<TWish>? bootstrapper = null)
        : base(initialState, actor, reducer, bootstrapper) { }
}
