using System.Drawing;

namespace RainbowMage.OverlayPlugin
{
    interface IApiBase : IEventReceiver
    {
        void SetAcceptFocus(bool accept);

        void OverlayMessage(string msg);

        void InitModernAPI();

        Bitmap Screenshot();
    }
}
