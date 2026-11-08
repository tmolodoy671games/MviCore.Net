# T.Molodoy.MviCore.Net

Упрощённый перенос ядра [Badoo MVICore](https://github.com/badoo/MVICore) на C# и
[R3](https://github.com/Cysharp/R3). Библиотека на `netstandard2.1`, зависимость — R3 1.3.1.

## Установка

```sh
dotnet add package T.Molodoy.MviCore.Net --version 0.0.1
```

## Быстрый старт

```csharp
using MviCore.Net;
using SharpExtensions;
using SharpExtensions.R3;
using Single = SharpExtensions.R3.Single;
using R3;

using var counter = new ReducerFeature<int, int>(
    initialState: 0,
    reducer: (state, delta) => state + delta);

using var subscription = counter.Subscribe(Console.WriteLine);
counter.Accept(1);
counter.Accept(2);
Console.WriteLine(counter.State);
```

Feature работает сразу после создания, даже без подписчиков. Новый подписчик получает
текущее состояние, затем результат каждой редукции, включая равные предыдущему состояния.
`States` позволяет подписываться через `IStore` / `IFeature`. Сам feature также является
R3 `Observable<TState>` и поддерживает обычные операторы R3.

## API

| Тип | Назначение |
| --- | --- |
| `ReducerFeature<Wish, State, News>` | Wish сразу передаётся в Reducer как Effect |
| `ActorReducerFeature<Wish, Effect, State, News>` | Actor преобразует Wish в один Effect |
| `BaseFeature<Wish, Action, Effect, State, News>` | Полная цепочка с внутренними Action и PostProcessor |
| `IStore<Wish, State>` | `Accept(wish)`, `State`, `States` |
| `IFeature<Wish, State, News>` | Store плюс `News`, `IsDisposed`, `Dispose()` |

Все три класса имеют вариант без последнего параметра `News`; в нём используется `Nothing`.
Компоненты задаются делегатами или интерфейсами. Сигнатуры делегатов:

```text
WishToAction:  Wish                  → Action
Bootstrapper: ()                    → Observable<Action>
Actor:        (State, Action)       → Single<Effect>
Reducer:      (State, Effect)       → State
PostProcessor:(Action, Effect, State) → Optional<Action>
NewsPublisher:(Action, Effect, State) → Optional<News>
```

Обязательные параметры конструктора идут первыми; необязательные `bootstrapper`,
`postProcessor` и `newsPublisher` удобно передавать по имени. Сохранены основные имена,
порядок generic-параметров и модель исполнения оригинала. Это не двоично совместимый порт.
Binder, middleware, time travel и платформенные интеграции в это ядро не входят.

## Реализация компонентов классами

Для каждого делегата есть интерфейс с методом `Invoke`: `IWishToAction`, `IActor`,
`IReducer`, `IBootstrapper`, `IPostProcessor`, `INewsPublisher`, `ISimpleNewsPublisher`.
Конструкторы всех вариантов Feature принимают реализации этих интерфейсов:

```csharp
using MviCore.Net;
using SharpExtensions.R3;
using Single = SharpExtensions.R3.Single;

public sealed class CounterActor : IActor<int, int, int>
{
    public Single<int> Invoke(int state, int action) => Single.Just(action);
}

public sealed class CounterReducer : IReducer<int, int>
{
    public int Invoke(int state, int effect) => state + effect;
}

// В вызывающем методе:
// using var feature = new ActorReducerFeature<int, int, int>(
//     0, new CounterActor(), new CounterReducer());
```

Перегрузки с делегатами по-прежнему принимают лямбды. Для смешивания объектов и лямбд
можно передавать метод объекта: `actor: new CounterActor().Invoke`.
Имя сборки — `MviCore.Net.R3.dll`; `T.Molodoy` — префикс NuGet-пакета.

## Actor и новости

```csharp
using var feature = new ActorReducerFeature<int, int, int, string>(
    initialState: 0,
    actor: (state, wish) => Single.Just(wish * 2),
    reducer: (state, effect) => state + effect,
    newsPublisher: (wish, effect, state) => state >= 10
        ? $"Достигнуто {state}"
        : default(Optional<string>));

using var states = feature.Subscribe(Console.WriteLine);
using var news = feature.News.Subscribe(Console.WriteLine);
feature.Accept(5);
```

Actor возвращает `Single<Effect>`: одно значение или ошибку. При преобразовании
`Observable` через `ToSingle()` значение поступает после успешного завершения; пустой
поток или несколько значений приводят к ошибке.
Actor получает состояние на момент поступления Action. Его Effect применяется
к **последнему** состоянию, в том числе если другие Actor уже успели его изменить.
Потоки разных Actor могут существовать одновременно; новый Wish не отменяет предыдущие.
Новости получают только текущие подписчики, без сохранения и повторной выдачи.
У `ReducerFeature` сокращённый `SimpleNewsPublisher` принимает только `(wish, state)`.

`default(Optional<T>)` (или `default`) означает отсутствие Action/News. Само значение неявно
преобразуется в `Optional<T>`; `null` для ссылочного типа считается присутствующим значением.
`Optional<T>` предоставляется зависимостью `T.Molodoy.SharpExtensions`. Это позволяет использовать `int`, enum и
struct: `0` и `false` являются полноценными событиями, а не признаком отсутствия результата.
В лямбдах для отсутствия результата предпочтителен явный `default(Optional<T>)`.

## Внутренние действия и запуск

```csharp
using var feature = new BaseFeature<string, int, int, int>(
    initialState: 0,
    wishToAction: int.Parse,
    actor: (_, action) => Single.Just(action),
    reducer: (state, effect) => state + effect,
    bootstrapper: () => Observable.Return(1),
    postProcessor: (_, _, state) => state == 2 ? 10 : default(Optional<int>));

Console.WriteLine(feature.State);
feature.Accept("1");
```

Bootstrapper вызывается один раз в конструкторе и может возвращать длительный поток.
Синхронный запуск происходит до возврата из конструктора: поля производного класса,
которые инициализируются позже, ещё недоступны, а начальные новости не буферизуются.
Для отложенного запуска можно вернуть внешний Subject с действиями и отправить событие
после создания feature и подписки на выходы.

После Reducer новое состояние сохраняется и публикуется; затем вызывается PostProcessor,
затем NewsPublisher. Оба получают именно состояние этой редукции. Вызовы синхронны:
внутреннее действие из PostProcessor выполняется до публикации новости исходного действия.
Повторный `Accept` из подписчика тоже выполняется сразу. Очереди нет; бесконечные или
очень глубокие цепочки внутренних действий нужно исключить в бизнес-логике.

## Потоки, ошибки и освобождение ресурсов

Все `Accept`, уведомления Actor/Bootstrapper, подписки и `Dispose` должны выполняться
в одном потоке. Собственных `lock`, проверок thread ID, scheduler и синхронизации нет.
Внутри самих примитивов R3 может использоваться синхронизация. Для асинхронных операций
нужно самостоятельно вернуть уведомления в поток feature, например через
`ObserveOn(context)`, где `context` — нужный `SynchronizationContext`, перед возвратом потока из Actor.

Ошибки уведомлений следуют модели R3: `OnErrorResume`, неуспешное завершение Actor или
Bootstrapper и исключения Reducer/PostProcessor/NewsPublisher передаются глобальному
`ObservableSystem.RegisterUnhandledExceptionHandler`. Для Bootstrapper `OnErrorResume`
не отменяет поток; для Actor он завершает `Single` ошибкой. Неуспешное завершение
отменяет только конкретную подписку. Feature остаётся работоспособным.
Исключения WishToAction и фабрики Actor при прямом `Accept` выходят вызывающему коду.
Ошибки, являющиеся частью бизнес-логики, следует преобразовывать в Effect внутри Actor.

`Dispose()` отменяет все активные Actor и Bootstrapper, завершает `States` и `News` и
допускает повторный вызов. После него `Accept` бросает `ObjectDisposedException`, а `State`
сохраняет последнее значение. Подписки, завершившиеся раньше, сразу удаляются из учёта.
Отписка отдельного наблюдателя не останавливает feature. Если освобождение upstream
бросает исключение, остальные ресурсы всё равно освобождаются, затем выбрасывается
`AggregateException`.

## Проверка

Для тестов нужен .NET SDK 9 или новее:

```sh
dotnet test MviCore.Net.sln --configuration Release
dotnet pack MviCore.Net.R3/MviCore.Net.R3.csproj --configuration Release
```

Тесты проверяют актуальность состояния при чередовании Effect, порядок PostProcessor/News,
bootstrap, повторный вход, обработку ошибок, освобождение подписок и отсутствие удержания
Action после синхронного завершения Actor.
