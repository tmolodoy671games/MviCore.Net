using System;
using System.Collections.Generic;
using R3;
using SharpExtensions.R3;

namespace MviCore.Net;

public class BaseFeature<TWish, TAction, TEffect, TState, TNews> : Observable<TState>,
    IFeature<TWish, TState, TNews>
{
    private readonly ReactiveProperty<TState> states;
    private readonly Subject<TNews> news = new();
    private readonly HashSet<IDisposable> subscriptions = new();
    private readonly WishToAction<TWish, TAction> wishToAction;
    private readonly Actor<TState, TAction, TEffect> actor;
    private readonly Reducer<TState, TEffect> reducer;
    private readonly PostProcessor<TAction, TEffect, TState>? postProcessor;
    private readonly NewsPublisher<TAction, TEffect, TState, TNews>? newsPublisher;

    public BaseFeature(
        TState initialState,
        IWishToAction<TWish, TAction> wishToAction,
        IActor<TState, TAction, TEffect> actor,
        IReducer<TState, TEffect> reducer,
        IBootstrapper<TAction>? bootstrapper = null,
        IPostProcessor<TAction, TEffect, TState>? postProcessor = null,
        INewsPublisher<TAction, TEffect, TState, TNews>? newsPublisher = null
    ) : this(initialState,
        (wishToAction ?? throw new ArgumentNullException(nameof(wishToAction))).Invoke,
        (actor ?? throw new ArgumentNullException(nameof(actor))).Invoke,
        (reducer ?? throw new ArgumentNullException(nameof(reducer))).Invoke,
        bootstrapper == null ? null : bootstrapper.Invoke,
        postProcessor == null ? null : postProcessor.Invoke,
        newsPublisher == null ? null : newsPublisher.Invoke)
    {
    }

    public BaseFeature(
        TState initialState,
        WishToAction<TWish, TAction> wishToAction,
        Actor<TState, TAction, TEffect> actor,
        Reducer<TState, TEffect> reducer,
        Bootstrapper<TAction>? bootstrapper = null,
        PostProcessor<TAction, TEffect, TState>? postProcessor = null,
        NewsPublisher<TAction, TEffect, TState, TNews>? newsPublisher = null)
    {
        this.wishToAction = wishToAction ?? throw new ArgumentNullException(nameof(wishToAction));
        this.actor = actor ?? throw new ArgumentNullException(nameof(actor));
        this.reducer = reducer ?? throw new ArgumentNullException(nameof(reducer));
        this.postProcessor = postProcessor;
        this.newsPublisher = newsPublisher;
        states = new ReactiveProperty<TState>(initialState);
        News = news.AsObservable();

        try
        {
            if (bootstrapper != null) Track(bootstrapper(), ProcessAction);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public TState State => states.CurrentValue;

    public Observable<TState> States => this;

    public Observable<TNews> News { get; }

    public bool IsDisposed { get; private set; }

    public void Accept(TWish wish)
    {
        ThrowIfDisposed();
        ProcessAction(wishToAction(wish));
    }

    protected override IDisposable SubscribeCore(Observer<TState> observer) => states.Subscribe(observer.Wrap());

    private void ProcessAction(TAction action)
    {
        if (IsDisposed) return;
        var source = actor(State, action)
            ?? throw new InvalidOperationException("An actor returned null.");
        Track(source.ToObservable(), effect => ProcessEffect(action, effect));
    }

    private void ProcessEffect(TAction action, TEffect effect)
    {
        if (IsDisposed)
            return;
        var nextState = reducer(State, effect);
        if (IsDisposed)
            return;

        states.OnNext(nextState);
        if (IsDisposed)
            return;

        if (postProcessor != null)
        {
            var nextAction = postProcessor(action, effect, nextState);
            if (IsDisposed) return;
            if (nextAction.HasValue) ProcessAction(nextAction.Value);
        }

        if (IsDisposed || newsPublisher == null) return;
        var item = newsPublisher(action, effect, nextState);
        if (!IsDisposed && item.HasValue) news.OnNext(item.Value);
    }

    private void Track<T>(Observable<T> source, Action<T> onNext)
    {
        if (source == null) throw new InvalidOperationException("An actor or bootstrapper returned null.");
        if (IsDisposed) return;

        var observer = new TrackedObserver<T>(onNext, subscription => subscriptions.Remove(subscription));
        subscriptions.Add(observer);
        source.Subscribe(observer);
    }

    public void Dispose()
    {
        if (IsDisposed)
            return;
        IsDisposed = true;

        var active = new IDisposable[subscriptions.Count];
        subscriptions.CopyTo(active);
        subscriptions.Clear();
        List<Exception>? errors = null;
        foreach (var subscription in active) DisposeResource(subscription);
        DisposeResource(states);
        DisposeResource(news);
        if (errors != null)
            throw new AggregateException(errors);
        return;

        void DisposeResource(IDisposable resource)
        {
            try
            {
                resource.Dispose();
            }
            catch (Exception error)
            {
                (errors ??= []).Add(error);
            }
        }
    }

    private void ThrowIfDisposed()
    {
        if (IsDisposed) throw new ObjectDisposedException(GetType().Name);
    }

    private sealed class TrackedObserver<T> : Observer<T>
    {
        private Action<T>? onNext;
        private Action<IDisposable>? onDispose;

        public TrackedObserver(Action<T> onNext, Action<IDisposable> onDispose)
        {
            this.onNext = onNext;
            this.onDispose = onDispose;
        }

        protected override void OnNextCore(T value) => onNext?.Invoke(value);

        protected override void OnErrorResumeCore(Exception error) =>
            ObservableSystem.GetUnhandledExceptionHandler()(error);

        protected override void OnCompletedCore(Result result)
        {
            if (result.IsFailure) ObservableSystem.GetUnhandledExceptionHandler()(result.Exception!);
        }

        protected override void DisposeCore()
        {
            var unregister = onDispose;
            onNext = null;
            onDispose = null;
            unregister?.Invoke(this);
        }
    }
}

public class BaseFeature<TWish, TAction, TEffect, TState>(
    TState initialState,
    WishToAction<TWish, TAction> wishToAction,
    Actor<TState, TAction, TEffect> actor,
    Reducer<TState, TEffect> reducer,
    Bootstrapper<TAction>? bootstrapper = null,
    PostProcessor<TAction, TEffect, TState>? postProcessor = null
) : BaseFeature<TWish, TAction, TEffect, TState, Nothing>(
    initialState,
    wishToAction,
    actor,
    reducer,
    bootstrapper,
    postProcessor
)
{
    public BaseFeature(
        TState initialState,
        IWishToAction<TWish, TAction> wishToAction,
        IActor<TState, TAction, TEffect> actor,
        IReducer<TState, TEffect> reducer,
        IBootstrapper<TAction>? bootstrapper = null,
        IPostProcessor<TAction, TEffect, TState>? postProcessor = null
    ) : this(initialState,
        (wishToAction ?? throw new ArgumentNullException(nameof(wishToAction))).Invoke,
        (actor ?? throw new ArgumentNullException(nameof(actor))).Invoke,
        (reducer ?? throw new ArgumentNullException(nameof(reducer))).Invoke,
        bootstrapper == null ? null : bootstrapper.Invoke,
        postProcessor == null ? null : postProcessor.Invoke)
    {
    }
}
