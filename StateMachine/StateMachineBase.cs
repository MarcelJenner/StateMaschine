using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Timer = System.Timers.Timer;

namespace Utils
{
    public abstract class StateMachineBase : IDisposable
    {
        private class TransitionCommand
        {
            public Enum Command { get; }
            public object Data { get; }

            public TransitionCommand(Enum command, object data)
            {
                Command = command;
                Data = data;
            }
        }

        protected readonly ILogger logger;
        
        public State LastState { get; private set; }
        public State CurrentState { get; private set; }
        private readonly List<State> states = new List<State>();

        private Dictionary<string, object> additionalData = new Dictionary<string, object>();

        private readonly Timer timeoutTimer;

        private readonly CancellationTokenSource commandHandlerCts = new CancellationTokenSource();
        private readonly Task commandHandler;
        
        private CancellationTokenSource transitionCts = new CancellationTokenSource();
        
        private readonly BlockingCollection<TransitionCommand> commandQueue = new BlockingCollection<TransitionCommand>();
        private readonly Lock commandEnqueueLock = new Lock();
        private readonly Lock commandDequeueLock = new Lock();

        protected StateMachineBase(ILogger<StateMachineBase> logger)
        {
            this.logger = logger;

            AddState(StateMachineBaseState.Idle)
                .OnEnter(OnIdleEnter)
                .OnExit(OnIdleExit);
            AddState(StateMachineBaseState.Canceled)
                .OnEnter(OnCancelEnter)
                .OnExit(OnCancelExit)
                .On(StateMachineBaseCommand.Done)
                .Goto(StateMachineBaseState.Idle);

            CurrentState = states.First(s => Equals(StateMachineBaseState.Idle, s.Name));

            commandHandler = Task.Run(HandleCommands);

            timeoutTimer = new Timer
            {
                AutoReset = false
            };
            timeoutTimer.Elapsed += (sender, e) => Cancel("timeout exceeded");
        }

        private void HandleCommands()
        {
            while (!commandHandlerCts.Token.IsCancellationRequested)
            {
                try
                {
                    TransitionCommand cmd;
                    lock (commandDequeueLock)
                    {
                        cmd = commandQueue.Take(transitionCts.Token);
                    }
                    MoveNext(cmd.Command, transitionCts.Token, cmd.Data);
                }
                catch(OperationCanceledException e)
                {
                    // ignore transition cancelled exception
                    // StateMachine.Cancel() will clear the queue and enqueue a cancel transition
                }
                catch(Exception e)
                {
                    logger.LogError(e.Message);
                }
            }
        }

        public void EnqueueTransition(Enum command, object data = null)
        {
            if (Equals(command, StateMachineBaseCommand.Cancel))
                throw new NotSupportedException("use \"StateMachine.Cancel()\" instead of enqueueing a cancel transition");

            lock (commandEnqueueLock)
            {
                commandQueue.Add(new TransitionCommand(command, data));
            }
        }

        protected void MoveNext(Enum command, CancellationToken cancellationToken, object data = null)
        {
            try
            {
                var transition = CurrentState.Transitions.FirstOrDefault(trans => Equals(trans.Command, command));
                if (transition == null) 
                    throw new ArgumentNullException($"{nameof(transition)}  --> transition: {command} from State: {CurrentState.Name} was not registered");

                cancellationToken.ThrowIfCancellationRequested();
                if(!Equals(command, StateMachineBaseCommand.Cancel))
                    CurrentState.Exit(data, cancellationToken);

                timeoutTimer.Stop();

                cancellationToken.ThrowIfCancellationRequested();

                LastState = CurrentState;
                CurrentState = states.First(s => Equals(s.Name, transition.NextState));

                cancellationToken.ThrowIfCancellationRequested();

                StartTimeoutTimer();

                CurrentState.Enter(data, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();
            }
            catch (Exception e) when (!(e is OperationCanceledException))
            {
                Cancel(e.Message);
            }
            
            void StartTimeoutTimer()
            {
                if (CurrentState.Timeout <= 0)
                    return;

                timeoutTimer.Interval = CurrentState.Timeout;
                timeoutTimer.Start();
            }
        }

        public void Cancel(string reason = "undefined")
        {
            transitionCts.Cancel();

            lock (new[] { commandEnqueueLock, commandDequeueLock })
            {
                while (commandQueue.Count > 0)
                    commandQueue.Take();

                commandQueue.Add(new TransitionCommand(StateMachineBaseCommand.Cancel, reason));

                transitionCts = new CancellationTokenSource();
            }
        }

        protected State AddState(Enum name)
        {
            if(states.Exists(s => Equals(s.Name, name)))
                throw new Exception($"State {name} already exists");
            if (name.GetType() != typeof(StateMachineBaseState) && Enum.IsDefined(typeof(StateMachineBaseState), name.ToString()))
                throw new Exception($"\"{name}\" is already defined in {nameof(StateMachineBaseState)}!");

            var state = new State(name)
                .On(StateMachineBaseCommand.Cancel)
                .Goto(StateMachineBaseState.Canceled);
            
            states.Add(state);
            return state;
        }

        protected State GetState(Enum name)
        {
            var state = states.FirstOrDefault(s => Equals(s.Name, name));
            if(state == null) throw new NullReferenceException(nameof(state));
            return state;
        }

        protected virtual void OnIdleEnter(object data, CancellationToken cancellationToken)
        {
            additionalData = new Dictionary<string, object>();
        }

        protected virtual void OnIdleExit(object data, CancellationToken cancellationToken)
        {
        }

        protected virtual void OnCancelEnter(object data, CancellationToken cancellationToken)
        {
            MoveNext(StateMachineBaseCommand.Done, cancellationToken);
        }

        protected virtual void OnCancelExit(object data, CancellationToken cancellationToken)
        {
        }

        public virtual void Dispose()
        {
            timeoutTimer?.Stop();
            timeoutTimer?.Dispose();

            commandHandlerCts?.Cancel();
            transitionCts?.Cancel();

            commandHandler?.Wait();
            commandHandler?.Dispose();

            commandHandlerCts?.Dispose();
            transitionCts?.Dispose();
        }
    }
}
