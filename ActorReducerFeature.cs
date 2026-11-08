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
);

public class ActorReducerFeature<TWish, TEffect, TState>(
    TState initialState,
    Actor<TState, TWish, TEffect> actor,
    Reducer<TState, TEffect> reducer,
    Bootstrapper<TWish>? bootstrapper = null
) : ActorReducerFeature<TWish, TEffect, TState, Nothing>(initialState, actor, reducer, bootstrapper);