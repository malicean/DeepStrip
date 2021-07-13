using System;
using System.IO;

namespace DeepStrip.Streams
{
	internal class BufferedStandardOutput : Stream
	{
		private readonly Stream _buffer;

		public override bool CanRead => _buffer.CanRead;
		public override bool CanSeek => _buffer.CanSeek;
		public override bool CanWrite => _buffer.CanWrite;
		public override long Length => _buffer.Length;
		public override long Position
		{
			get => _buffer.Position;
			set => _buffer.Position = value;
		}

		public BufferedStandardOutput()
		{
			_buffer = new MemoryStream();
		}

		public override void Flush()
		{
			using var stdout = Console.OpenStandardOutput();

			_buffer.CopyTo(stdout);
		}

		public override int Read(byte[] buffer, int offset, int count) => _buffer.Read(buffer, offset, count);

		public override long Seek(long offset, SeekOrigin origin) => _buffer.Seek(offset, origin);

		public override void SetLength(long value) => _buffer.SetLength(value);

		public override void Write(byte[] buffer, int offset, int count) => _buffer.Write(buffer, offset, count);

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				Flush();

				_buffer.Dispose();
			}

			base.Dispose(disposing);
		}
	}
}
