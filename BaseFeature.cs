using System;
using System.Collections.Generic;
using R3;

namespace MviCore.Net;

/// <summary>
/// A single-threaded MVICore pipeline. All calls and stream notifications must use the same thread.
/// No scheduling, locking, or serialization is added by the feature.
/// </summary>
public class BaseFeature<TWish, TAction, TEffect, TState, TNews> : Observable<TState>,
    IFeature<TWish, TState, TNews>
{
    private readonly ReactiveProperty<TState> states;
    private readonly Subject<TNews> news = new Subject<TNews>();
    private readonly HashSet<IDisposable> subscriptions = new HashSet<IDisposable>();
    private readonly WishToAction<TWish, TAction> wishToAction;
    private readonly Actor<TState, TAction, TEffect> actor;
    private readonly Reducer<TState, TEffect> reducer;
    private readonly PostProcessor<TAction, TEffect, TState>? postProcessor;
    private readonly NewsPublisher<TAction, TEffect, TState, TNews>? newsPublisher;

    /// <summary>Creates a hot feature and immediately subscribes to its optional bootstrapper.</summary>
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

    /// <inheritdoc />
    public TState State => states.CurrentValue;

    /// <inheritdoc />
    public Observable<TState> States => this;

    /// <inheritdoc />
    public Observable<TNews> News { get; }

    /// <inheritdoc />
    public bool IsDisposed { get; private set; }

    /// <inheritdoc />
    public void Accept(TWish wish)
    {
        ThrowIfDisposed();
        ProcessAction(wishToAction(wish));
    }

    /// <inheritdoc />
    protected override IDisposable SubscribeCore(Observer<TState> observer) => states.Subscribe(observer.Wrap());

    private void ProcessAction(TAction action)
    {
        if (IsDisposed) return;
        Track(actor(State, action), effect => ProcessEffect(action, effect));
    }

    private void ProcessEffect(TAction action, TEffect effect)
    {
        if (IsDisposed) return;
        var nextState = reducer(State, effect);
        if (IsDisposed) return;

        // OnNext deliberately emits equal states, matching MVICore's BehaviorSubject.
        states.OnNext(nextState);
        if (IsDisposed) return;

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

        // Register before subscribing: a synchronous stream may complete or dispose the feature here.
        var observer = new TrackedObserver<T>(onNext, subscription => subscriptions.Remove(subscription));
        subscriptions.Add(observer);
        source.Subscribe(observer);
    }

    /// <summary>Cancels active streams and completes state and news subscriptions. Idempotent.</summary>
    public void Dispose()
    {
        if (IsDisposed) return;
        IsDisposed = true;

        var active = new IDisposable[subscriptions.Count];
        subscriptions.CopyTo(active);
        subscriptions.Clear();
        List<Exception>? errors = null;
        foreach (var subscription in active) DisposeResource(subscription);
        DisposeResource(states);
        DisposeResource(news);
        if (errors != null) throw new AggregateException(errors);

        void DisposeResource(IDisposable resource)
        {
            try { resource.Dispose(); }
            catch (Exception error) { (errors ??= new List<Exception>()).Add(error); }
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
            // Completed R3 subjects may retain their observer nodes until disposal.
            // Release captured actions and the feature even when the source stays alive.
            var unregister = onDispose;
            onNext = null;
            onDispose = null;
            unregister?.Invoke(this);
        }
    }
}

/// <summary>A base feature without news.</summary>
public class BaseFeature<TWish, TAction, TEffect, TState> : BaseFeature<TWish, TAction, TEffect, TState, Nothing>
{
    /// <summary>Creates a feature without a news publisher.</summary>
    public BaseFeature(TState initialState, WishToAction<TWish, TAction> wishToAction,
        Actor<TState, TAction, TEffect> actor, Reducer<TState, TEffect> reducer,
        Bootstrapper<TAction>? bootstrapper = null, PostProcessor<TAction, TEffect, TState>? postProcessor = null)
        : base(initialState, wishToAction, actor, reducer, bootstrapper, postProcessor) { }
}
