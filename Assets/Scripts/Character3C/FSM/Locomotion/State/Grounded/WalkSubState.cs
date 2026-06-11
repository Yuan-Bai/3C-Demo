


// public class WalkSubState : IState<GroundedStateId>
// {
//     public GroundedStateId Id => GroundedStateId.Walk;

//     private LocomotionContext _context;
//     private GroundedStateContext _groundedContext;
//     private ChangeSubState ChangeSubState;

//     public WalkSubState(LocomotionContext context, GroundedStateContext groundedContext, ChangeSubState changeSubState)
//     {
//         _context = context;
//         _groundedContext = groundedContext;
//         ChangeSubState = changeSubState;
//     }

//     public void Enter()
//     {
//         _context.GroundedStateId = GroundedStateId.Walk;
//     }

//     public void Exit()
//     {
//     }

//     public void Tick(float deltaTime)
//     {
//         if (!_context.HasMoveInput)
//         {
//             ChangeSubState(GroundedStateId.MoveStop);
//             return;
//         }

//         if (!_groundedContext.PreferWalk)
//         {
//             ChangeSubState(GroundedStateId.Run);
//             return;
//         }
//     }
// }