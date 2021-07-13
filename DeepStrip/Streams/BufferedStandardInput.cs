using System;
using System.IO;

namespace DeepStrip.Streams
{
	internal class BufferedStandardInput : Stream
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

		public BufferedStandardInput()
		{
			using var stdin = Console.OpenStandardInput();

			_buffer = new MemoryStream();
			stdin.CopyTo(_buffer);
		}

		public override void Flush() { }

		public override int Read(byte[] buffer, int offset, int count) => _buffer.Read(buffer, offset, count);

		public override long Seek(long offset, SeekOrigin origin) => _buffer.Seek(offset, origin);

		public override void SetLength(long value) => _buffer.SetLength(value);

		public override void Write(byte[] buffer, int offset, int count) => _buffer.Write(buffer, offset, count);

		protected override void Dispose(bool disposing)
		{
			if (disposing)
				_buffer.Dispose();

			base.Dispose(disposing);
		}
	}
}
