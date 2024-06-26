﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDo.MinioPlugin.Http
{
    /// <summary>
    /// 若要报告进度，需要向 httpClient 传递 HttpCompletionOption.ResponseHeadersRead 参数
    /// 例：httpClient.GetAsync(path, HttpCompletionOption.ResponseHeadersRead)
    /// copy from: https://github.com/ezequias2d/Ez.IO/blob/main/src/Ez.IO/ProgressStream.cs
    /// Provides a wrapper for <see cref="Stream"/> instances with a event to report the progress
    /// of the <see cref="Position"/> in relation of <see cref="Length"/>.
    /// </summary>
    public sealed class ProgressStream : Stream
    {
        private readonly Stream _inner;
        private readonly long _basePosition;
        private readonly bool _leaveOpen;
        private long _current;
        private long _size;
        private double _invSize;

        /// <summary>
        /// Creates a new <see cref="ProgressStream"/> instance.
        /// </summary>
        /// <param name="inner">The inner stream.</param>
        /// <param name="leaveOpen"><see langword="true"/> to leave the inner stream open after the 
        /// <see cref="ProgressStream"/> object is disposed; otherwise, <see langword="false"/>.</param>
        public ProgressStream(Stream inner, bool leaveOpen = false)
        {
            _inner = inner;
            _current = 0;
            _leaveOpen = leaveOpen;

            _basePosition = inner.Position;
            UpdateSize();
        }

        /// <inheritdoc/>
        public override bool CanRead => _inner.CanRead;

        /// <inheritdoc/>
        public override bool CanSeek => _inner.CanSeek;

        /// <inheritdoc/>
        public override bool CanWrite => _inner.CanWrite;

        /// <inheritdoc/>
        public override long Length => _inner.Length;

        /// <inheritdoc/>
        public override long Position
        {
            get => _inner.Position;
            set
            {
                _inner.Position = value;
                if (value > _basePosition + _size)
                    UpdateSize();
            }
        }

        /// <inheritdoc/>
        public override void Flush() => _inner.Flush();

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            var bytes = _inner.Read(buffer, offset, count);
            _current += bytes;
            InvokeReport();
            return bytes;
        }

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin)
        {
            var position = _inner.Seek(offset, origin);
            _current = position - _basePosition;
            InvokeReport();
            return position;
        }

        /// <inheritdoc/>
        public override void SetLength(long value)
        {
            _inner.SetLength(value);
            UpdateSize();
        }

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            _inner.Write(buffer, offset, count);
            _current = Position - _basePosition;
            UpdateSize();
            InvokeReport();
        }

        private readonly List<IProgress<ProgressReport>> _progressReports = [];
        /// <summary>
        /// 添加进度回调
        /// </summary>
        /// <param name="progressReport"></param>
        /// <returns></returns>
        public ProgressStream WithProgress(IProgress<ProgressReport> progressReport)
        {
            if(progressReport != null)
                _progressReports.Add(progressReport);
            return this;
        }

        private void InvokeReport()
        {
            int percentage = (int)(_current * _invSize * 100);
            // 保证最后一个进度为 100
            if (_current == _size) percentage = 100;

            foreach (var progressReport in _progressReports)
            {
                progressReport.Report(new ProgressReport
                {
                    Percentage = percentage,
                    TotalBytesTransferred = _current
                });
            }
        }

        /// <summary>
        /// 更新大小
        /// </summary>
        private void UpdateSize()
        {
            var aux = _inner.Position;
            _size = _inner.Length - _basePosition;
            if (_inner.Position != aux)
                _inner.Position = aux;
            _invSize = 1.0 / _size;
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (!_leaveOpen)
                _inner.Dispose();
            else base.Dispose(disposing);
        }
    }
}
