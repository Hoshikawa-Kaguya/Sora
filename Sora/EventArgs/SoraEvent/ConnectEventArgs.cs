using Sora.Model.SoraModel;

namespace Sora.EventArgs.SoraEvent
{
    public class ConnectEventArgs : BaseSoraEventArgs
    {
        #region 构造函数

        public ConnectEventArgs(SoraApi api, string eventName, long selfId) : base(api, eventName, selfId){ }
        #endregion
    }
}
