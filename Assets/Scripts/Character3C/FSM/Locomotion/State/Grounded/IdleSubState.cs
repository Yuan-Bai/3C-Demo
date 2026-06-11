
// public class IdleSubState : IState<GroundedStateId>
// {
//     public GroundedStateId Id => GroundedStateId.Idle;

//     private LocomotionContext _context;
//     private GroundedStateContext _groundedContext;
//     private ChangeSubState ChangeSubState;

//     public IdleSubState(LocomotionContext context, GroundedStateContext groundedContext, ChangeSubState changeSubState)
//     {
//         _context = context;
//         _groundedContext = groundedContext;
//         ChangeSubState = changeSubState;
//     }

//     public void Enter()
//     {
//         _context.GroundedStateId = GroundedStateId.Idle;
//     }

//     public void Exit()
//     {
//     }

//     public void Tick(float deltaTime)
//     {
//         if (_context.HasMoveInput)
//         {
//             if (_groundedContext.PreferWalk)
//             {
//                 ChangeSubState(GroundedStateId.Walk);
//             }
//             else
//             {
//                 ChangeSubState(GroundedStateId.Run);
//             }

//             return;
//         }

//         if (_context.InputFrame.DashPressed)
//         {
//             ChangeSubState(GroundedStateId.Dash);
//             return;
//         }
//     }
// }