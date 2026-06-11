// public class LandingState : LocomotionStateBase
// {
//     private const float LandingDuration = 0.1f;
//     private float _remainingLandingTime;

//     public LandingState(LocomotionStateId id, StateMachine<LocomotionStateId> stateMachine, LocomotionContext context) : base(id, stateMachine, context)
//     {
//     }

//     public override void Enter()
//     {
//         base.Enter();
//         _remainingLandingTime = LandingDuration;
//     }

//     public override void Tick(float deltaTime)
//     {
//         base.Tick(deltaTime);

//         if (!Context.IsStableOnGround)
//         {
//             ChangeState(LocomotionStateId.Airborne);
//             return;
//         }

//         _remainingLandingTime -= deltaTime;
//         if (_remainingLandingTime <= 0.0f)
//         {
//             ChangeState(LocomotionStateId.Grounded);
//         }
//     }
// }
