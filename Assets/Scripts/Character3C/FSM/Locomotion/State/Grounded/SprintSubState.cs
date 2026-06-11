


// public class SprintSubState : IState<GroundedStateId>
// {
//     public GroundedStateId Id => GroundedStateId.Sprint;

//     private LocomotionContext _context;
//     private GroundedStateContext _groundedContext;
//     private ChangeSubState ChangeSubState;

//     public SprintSubState(LocomotionContext context, GroundedStateContext groundedContext, ChangeSubState changeSubState)
//     {
//         _context = context;
//         _groundedContext = groundedContext;
//         ChangeSubState = changeSubState;
//     }

//     public void Enter()
//     {
//         _context.GroundedStateId = GroundedStateId.Sprint;
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

//         if (_context.InputFrame.DashPressed)
//         {
//             ChangeSubState(GroundedStateId.Dash);
//             return;
//         }
//     }
// }