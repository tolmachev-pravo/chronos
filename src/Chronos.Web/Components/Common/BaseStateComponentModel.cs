namespace Chronos.Web.Components.Common
{
    public abstract class BaseStateComponentModel : IStateComponentModel
    {
        private ComponentModelState _state = ComponentModelState.Unknown;
        public ComponentModelState State => _state;
        public bool InProgress => State == ComponentModelState.InProgress;

        public void StateTo(ComponentModelState state)
        {
            _state = state;
        }
    }
}
