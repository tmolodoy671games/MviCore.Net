using R3;

namespace MviCore.Net;

public class ReducerFeature<TWish, TState, TNews>(
    TState initialState, Reducer<TState, TWish> reducer,
    Bootstrapper<TWish>? bootstrapper = null, SimpleNewsPublisher<TWish, TState, TNews>? newsPublisher = null)
    : BaseFeature<TWish, TWish, TWish, TState, TNews>(
        initialState, wish => wish, (_, wish) => Observable.Return(wish), reducer, bootstrapper,
        newsPublisher: newsPublisher == null ? null : (wish, _, state) => newsPublisher(wish, state));

public class ReducerFeature<TWish, TState>(
    TState initialState, Reducer<TState, TWish> reducer, Bootstrapper<TWish>? bootstrapper = null)
    : ReducerFeature<TWish, TState, Nothing>(initialState, reducer, bootstrapper);
