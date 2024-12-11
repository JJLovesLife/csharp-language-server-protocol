using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.JsonRpc.Server;

namespace OmniSharp.Extensions.JsonRpc
{
    internal class ProcessScheduler : IDisposable
    {
        private readonly IObserver<(RequestProcessType type, string name, SchedulerDelegate request)> _enqueue;
        private readonly CompositeDisposable _disposable = new CompositeDisposable();

        public ProcessScheduler(
            ILoggerFactory loggerFactory,
            bool supportContentModified,
            int? concurrency,
            IScheduler scheduler
        )
        {
            var concurrency1 = concurrency;
            var logger = loggerFactory.CreateLogger<ProcessScheduler>();

            var subject = new Subject<(RequestProcessType type, string name, SchedulerDelegate request)>();
            _disposable.Add(subject);
            _enqueue = subject;

            var obs = Observable.Create<Unit>(
                observer => {
                    var cd = new CompositeDisposable();

                    var observableQueue =
                        new BehaviorSubject<(RequestProcessType type, ReplaySubject<IObservable<Unit>> observer, Subject<Unit>? contentModifiedSource)>(
                            ( RequestProcessType.Serial, new ReplaySubject<IObservable<Unit>>(int.MaxValue, Scheduler.Immediate), supportContentModified ? new Subject<Unit>() : null )
                            // 为什么用 ReplaySubject？看下来我感觉是因为下面 Select 之后使用了 Concat，这个会导致 `.Subscribe(_ => { })` 不会立刻 consume 当前 observableQueue.Value.observer 中的内容。
                            // 它需要等到下一个 observableQueue.(Old)Value.observer的内容 consume 之后才会开始 observe 这个 observer。这里应该是存在并发（尚未验证）导致 .Value.observer不会同步立刻被consume。
                            // 所以它需要使用 ReplaySubject 来记住 concat 还没有切换到最新值的时候的所有 next value，然后当 concat 切换到下一个 .Value.observer 的时候把之前所有记录下来的 value 全部一次性发过去。
                            // 不用 ReplaySubject 的话，之前的那些 value 会因为没有 observer 监听而被直接丢弃掉。
                        );

                    cd.Add(
                        subject.Subscribe(
                            item => { // [InputHandler sync]
                                if (observableQueue.Value.type != item.type)
                                {
                                    logger.LogDebug("Swapping from {From} to {To}", observableQueue.Value.type, item.type);
                                    if (supportContentModified && observableQueue.Value.type == RequestProcessType.Parallel)
                                    {
                                        logger.LogDebug("Cancelling any outstanding requests (switch from parallel to serial)");
                                        observableQueue.Value.contentModifiedSource?.OnNext(Unit.Default);
                                        observableQueue.Value.contentModifiedSource?.OnCompleted();
                                    }

                                    logger.LogDebug("Completing existing request process type {Type}", observableQueue.Value.type);
                                    observableQueue.Value.observer.OnCompleted();
                                    observableQueue.OnNext(( item.type, new ReplaySubject<IObservable<Unit>>(int.MaxValue, Scheduler.Immediate), supportContentModified ? new Subject<Unit>() : null ));
                                }

                                logger.LogDebug("Queueing {Type}:{Name} request for processing", item.type, item.name);
                                observableQueue.Value.observer.OnNext(
                                    HandleRequest(item.name, item.request(observableQueue.Value.contentModifiedSource ?? Observable.Never<Unit>(), scheduler))
                                );
                            }
                        )
                    );

                    cd.Add(
                        observableQueue
                           .Select(
                                item => {
                                    var (type, replay, _) = item;

                                    if (type == RequestProcessType.Serial)
                                    {
                                        // logger.LogDebug("Changing to serial processing");
                                        return replay.Concat();
                                    }

                                    if (concurrency1.HasValue)
                                    {
                                        // logger.LogDebug("Changing to parallel processing with concurrency of {Concurrency}", concurrency1.Value);
                                        return replay.Merge(concurrency1.Value);
                                    }

                                    // logger.LogDebug("Changing to parallel processing with concurrency of {Concurrency}", "Unlimited");
                                    return replay.Merge();
                                }
                            )
                           .Concat()
                           .Subscribe(observer) // observableQueue 是 BehaviorSubject 这里会触发一次 OnNext
                    );

                    return cd;
                }
            );

            _disposable.Add(
                obs
                   .ObserveOn(scheduler)
                   .Subscribe(_ => { }) // 会触发 obs Create传入的lambada
            );

            IObservable<Unit> HandleRequest(string name, IObservable<Unit> request) // [InputHandler sync]
            {
                return request
                      .Catch<Unit, RequestCancelledException>(
                           ex => {
                               logger.LogDebug(ex, "Request {Name} was explicitly cancelled", name);
                               return Observable.Empty<Unit>();
                           }
                       )
                      .Catch<Unit, ContentModifiedException>(
                           ex => {
                               logger.LogDebug(ex, "Request {Name} was cancelled, due to content being modified", name);
                               return Observable.Empty<Unit>();
                           }
                       )
                      .Catch<Unit, OperationCanceledException>(
                           ex => {
                               logger.LogDebug(ex, "Request {Name} was cancelled, due to timeout", name);
                               return Observable.Empty<Unit>();
                           }
                       )
                      .Catch<Unit, Exception>(
                           ex => {
                               logger.LogCritical(Events.UnhandledException, ex, "Unhandled exception executing {Name}", name);
                               return Observable.Empty<Unit>();
                           }
                       )
                    // .Do(v => {
                    //     logger.LogDebug("Request {Name} was processed", name);
                    // }, (ex) => {
                    //     logger.LogCritical(Events.UnhandledException, ex, "Request {Name} encountered and unhandled exception", name);
                    // }, () => {
                    //     logger.LogDebug("Request {Name} was completed", name);
                    // })
                    ;
            }
        }

        public void Add(RequestProcessType type, string name, SchedulerDelegate request) => _enqueue.OnNext(( type, name, request ));

        public void Dispose() => _disposable.Dispose();
    }
}
