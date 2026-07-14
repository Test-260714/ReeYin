using ReeYin_V.Core.Services.Project;
using System;

namespace ReeYin_V.Hardware.ControlCard.Models
{
    public enum AxisViewIoJogDirection
    {
        Up,
        Down,
        Left,
        Right
    }

    public sealed class AxisViewIoJogController : IDisposable
    {
        private readonly ControlCardBase _controlCard;
        private readonly ControlCardConfigModel _config;
        private AxisViewIoJogDirection? _activeDirection;
        private EN_SpeedType _activeSpeedType;

        public AxisViewIoJogController(ControlCardBase controlCard, ControlCardConfigModel config)
        {
            _controlCard = controlCard ?? throw new ArgumentNullException(nameof(controlCard));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public static (En_AxisNum Axis, MoveDirection Direction) ResolveMovement(AxisViewIoJogDirection direction)
        {
            return direction switch
            {
                AxisViewIoJogDirection.Up => (En_AxisNum.Y, MoveDirection.正向),
                AxisViewIoJogDirection.Down => (En_AxisNum.Y, MoveDirection.反向),
                AxisViewIoJogDirection.Left => (En_AxisNum.X, MoveDirection.反向),
                AxisViewIoJogDirection.Right => (En_AxisNum.X, MoveDirection.正向),
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
            };
        }

        public void Update(
            bool[]? inputs,
            EN_SpeedType speedType,
            Func<En_AxisNum, bool> isAxisConfigured,
            Func<En_AxisNum, MoveDirection, bool> canStartJog)
        {
            if (isAxisConfigured == null)
            {
                throw new ArgumentNullException(nameof(isAxisConfigured));
            }

            if (canStartJog == null)
            {
                throw new ArgumentNullException(nameof(canStartJog));
            }

            if (!_config.IsAxisViewIoJogEnabled || _controlCard.IsReseting || !_controlCard.IsReady || inputs == null)
            {
                StopActiveJog();
                return;
            }

            var triggeredCount = 0;
            AxisViewIoJogDirection triggeredDirection = default;
            AddTriggeredDirection(inputs, _config.AxisViewIoJogUpInputPort, AxisViewIoJogDirection.Up, ref triggeredDirection, ref triggeredCount);
            AddTriggeredDirection(inputs, _config.AxisViewIoJogDownInputPort, AxisViewIoJogDirection.Down, ref triggeredDirection, ref triggeredCount);
            AddTriggeredDirection(inputs, _config.AxisViewIoJogLeftInputPort, AxisViewIoJogDirection.Left, ref triggeredDirection, ref triggeredCount);
            AddTriggeredDirection(inputs, _config.AxisViewIoJogRightInputPort, AxisViewIoJogDirection.Right, ref triggeredDirection, ref triggeredCount);

            if (triggeredCount != 1)
            {
                StopActiveJog();
                return;
            }

            var movement = ResolveMovement(triggeredDirection);
            if (!isAxisConfigured(movement.Axis))
            {
                StopActiveJog();
                return;
            }

            if (_activeDirection == triggeredDirection && _activeSpeedType == speedType)
            {
                if (!canStartJog(movement.Axis, movement.Direction))
                {
                    StopActiveJog();
                }

                return;
            }

            if (!StopActiveJog())
            {
                return;
            }

            if (!canStartJog(movement.Axis, movement.Direction))
            {
                return;
            }

            if (_controlCard.JogAxis(movement.Axis, movement.Direction, speedType, true))
            {
                _activeDirection = triggeredDirection;
                _activeSpeedType = speedType;
            }
        }

        public bool StopActiveJog()
        {
            if (!_activeDirection.HasValue)
            {
                return true;
            }

            var activeDirection = _activeDirection.Value;
            var activeSpeedType = _activeSpeedType;
            var movement = ResolveMovement(activeDirection);
            if (!_controlCard.JogAxis(movement.Axis, movement.Direction, activeSpeedType, false))
            {
                return false;
            }

            _activeDirection = null;
            return true;
        }

        public void ResetActiveState()
        {
            _activeDirection = null;
        }

        public void Dispose()
        {
            StopActiveJog();
        }

        private static void AddTriggeredDirection(
            bool[] inputs,
            int port,
            AxisViewIoJogDirection direction,
            ref AxisViewIoJogDirection triggeredDirection,
            ref int triggeredCount)
        {
            if (!IsPortTriggered(inputs, port))
            {
                return;
            }

            triggeredDirection = direction;
            triggeredCount++;
        }

        private static bool IsPortTriggered(bool[] inputs, int port)
        {
            return port >= 0 && port < inputs.Length && inputs[port];
        }
    }
}
