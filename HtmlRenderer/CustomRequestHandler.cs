﻿using CefSharp;
using CefSharp.Handler;

namespace RainbowMage.HtmlRenderer
{
    class CustomRequestHandler : RequestHandler
    {
        readonly Renderer _renderer;

        public CustomRequestHandler(Renderer renderer) : base()
        {
            _renderer = renderer;
        }

        protected override void OnRenderProcessTerminated(IWebBrowser chromiumWebBrowser, IBrowser browser, CefTerminationStatus status)
        {
            var msg = string.Format(Resources.BrowserCrashed, status);
            _renderer.Browser_ConsoleMessage(this, new ConsoleMessageEventArgs(browser, LogSeverity.Error, msg, "internal", 1));

            _renderer.InitBrowser();
            _renderer.BeginRender();
        }
    }
}
