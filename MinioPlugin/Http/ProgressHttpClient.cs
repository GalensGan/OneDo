using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Handlers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OneDo.MinioPlugin.Http
{
    /// <summary>
    /// 带有 Progress 的 HttpClient
    /// </summary>
    public class ProgressHttpClient : HttpClient
    {
        public ProgressMessageHandler ProgressMessageHandler { get; private set; }
        public ProgressHttpClient(ProgressMessageHandler handler, bool disposeHandler) : base(handler, disposeHandler)
        {
            ProgressMessageHandler = handler;
            ProgressMessageHandler.HttpSendProgress += ProgressMessageHandler_HttpSendProgress;
            ProgressMessageHandler.HttpReceiveProgress += ProgressMessageHandler_HttpReceiveProgress;
        }

        #region 进度相关
        /// <summary>
        /// 向请求消息体中添加自定义数据
        /// 该数据不会影响请求，可以使用这些数据来标识请求
        /// 这种方式多线程时，可能会出现异常
        /// 或许可以使用 ThreadLocal 进行重构，一个线程一个请求设置
        /// </summary>
        public Action<HttpRequestMessage> SetRequestMessageOption { get; set; }

        public override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // 向 request 中添加当前的 HttpClient，方便在事件中进行判断
            request.Options.Set(new HttpRequestOptionsKey<HttpClient>("httpClient"), this);
            // 调用外部设置
            SetRequestMessageOption?.Invoke(request);
            SetRequestMessageOption = null;

            return base.Send(request, cancellationToken);
        }

        /// <summary>
        /// 发送进度
        /// </summary>
        public event Action<object?, HttpProgressEventArgs> HttpSendProgress;

        /// <summary>
        /// 接收进度
        /// </summary>
        public event Action<object?, HttpProgressEventArgs> HttpReceiveProgress;

        private void ProgressMessageHandler_HttpReceiveProgress(object? request, HttpProgressEventArgs e)
        {
            if (DisableInvokeProgressEvent) return;
            if (request is not HttpRequestMessage requestMessage) return;

            // 判断request中是否有当前的 HttpClient
            if (!requestMessage.Options.TryGetValue(new HttpRequestOptionsKey<HttpClient>("httpClient"), out var httpClient)) return;
            if (httpClient != this) return;

            HttpReceiveProgress?.Invoke(request, e);
        }

        private void ProgressMessageHandler_HttpSendProgress(object? request, HttpProgressEventArgs e)
        {
            if (DisableInvokeProgressEvent) return;
            HttpSendProgress?.Invoke(request, e);
        }

        public bool DisableInvokeProgressEvent { get; set; } = false;
        #endregion


        #region 创建 ProgressHttpClient
        /// <summary>
        /// 创建一个带有进度的 HttpClient
        /// 最好全局只用一个 HttpClient
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="disposeHandler"></param>
        /// <returns></returns>
        private static HttpClientHandler _clientHandler;

        /// <summary>
        /// 创建 ProgressHttpClient
        /// 如果传入了 handler，在 httpClient 会用完成后，会释放
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public static ProgressHttpClient Create(HttpMessageHandler? handler = null)
        {
            if (handler != null)
            {
                var progressMessageHandler = new ProgressMessageHandler(handler);
                return new ProgressHttpClient(progressMessageHandler, true);
            }

            _clientHandler ??= new HttpClientHandler();
            return new ProgressHttpClient(new ProgressMessageHandler(_clientHandler), false);
        }
        #endregion
    }
}
