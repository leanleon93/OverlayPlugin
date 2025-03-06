using Newtonsoft.Json.Linq;

namespace RainbowMage.OverlayPlugin
{
    interface IEventReceiver
    {
        string Name { get; }

        void HandleEvent(JObject e);
    }
}
