using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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

        protected override void OnIdleEnter(object data, CancellationToken cancellationToken)
        {
            logger.LogInformation($"enter State 'idle' - {JsonConvert.SerializeObject(data)}");
            base.OnIdleEnter(data, cancellationToken);
        }

        protected override void OnIdleExit(object data, CancellationToken cancellationToken)
        {
            logger.LogInformation($"exit State 'idle' - {JsonConvert.SerializeObject(data)}");
            base.OnIdleExit(data, cancellationToken);
        }

        protected override void OnCancelEnter(object data, CancellationToken cancellationToken)
        {
            logger.LogInformation($"enter State 'cancel' - {JsonConvert.SerializeObject(data)}");
            base.OnCancelEnter(data, cancellationToken);
        }

        protected override void OnCancelExit(object data, CancellationToken cancellationToken)
        {
            logger.LogInformation($"exit State 'cancel' - {JsonConvert.SerializeObject(data)}");
            base.OnCancelExit(data, cancellationToken);
        }

        private void OnInitialisingEnter(object data, CancellationToken ct)
        {
            logger.LogInformation($"enter State 'Initialising' - {JsonConvert.SerializeObject(data)}");
        }
        
        private void OnInitialisingExit(object data, CancellationToken ct)
        {
            logger.LogInformation($"exit State 'Initialising' - {JsonConvert.SerializeObject(data)}");
        }
        
        private void OnFinishingEnter(object data, CancellationToken ct)
        {
            logger.LogInformation($"enter State 'finishing' - {JsonConvert.SerializeObject(data)}");
        }
    }
}