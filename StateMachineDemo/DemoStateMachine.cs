using System;
using System.Text.Json;
using System.Threading;
using Microsoft.Extensions.Logging;
using Utils;

namespace StateMachineDemo
{
    public class DemoStateMachine : StateMachineBase
    {
        public DemoStateMachine(ILogger<DemoStateMachine> logger) : base(logger)
        {
            GetState(StateMachineBaseState.Idle)
                .On(Command.Initialise)
                .Goto(State.Initialising);

            AddState(State.Initialising)
                .OnEnter(OnInitialisingEnter)
                .OnExit(OnInitialisingExit)
                .On(Command.Finish)
                .Goto(State.Finishing)
                .TimeoutAfter(TimeSpan.FromSeconds(30));

            AddState(State.Finishing)
                .OnEnter(OnFinishingEnter)
                .On(StateMachineBaseCommand.Done)
                .Goto(StateMachineBaseState.Idle);
        }

        void OnRunningEnter(object data, CancellationToken ct)
        {
            logger.LogInformation($"enter State 'Running' - {JsonSerializer.Serialize(data)}");
        }
        
        protected override void OnIdleEnter(object data, CancellationToken cancellationToken)
        {
            logger.LogInformation($"enter State 'idle' - {JsonSerializer.Serialize(data)}");
            base.OnIdleEnter(data, cancellationToken);
        }

        protected override void OnIdleExit(object data, CancellationToken cancellationToken)
        {
            logger.LogInformation($"exit State 'idle' - {JsonSerializer.Serialize(data)}");
            base.OnIdleExit(data, cancellationToken);
        }

        protected override void OnCancelEnter(object data, CancellationToken cancellationToken)
        {
            logger.LogInformation($"enter State 'cancel' - {JsonSerializer.Serialize(data)}");
            base.OnCancelEnter(data, cancellationToken);
        }

        protected override void OnCancelExit(object data, CancellationToken cancellationToken)
        {
            logger.LogInformation($"exit State 'cancel' - {JsonSerializer.Serialize(data)}");
            base.OnCancelExit(data, cancellationToken);
        }

        private void OnInitialisingEnter(object data, CancellationToken ct)
        {
            logger.LogInformation($"enter State 'Initialising' - {JsonSerializer.Serialize(data)}");
        }
        
        private void OnInitialisingExit(object data, CancellationToken ct)
        {
            logger.LogInformation($"exit State 'Initialising' - {JsonSerializer.Serialize(data)}");
        }
        
        private void OnFinishingEnter(object data, CancellationToken ct)
        {
            logger.LogInformation($"enter State 'finishing' - {JsonSerializer.Serialize(data)}");
        }
    }
}