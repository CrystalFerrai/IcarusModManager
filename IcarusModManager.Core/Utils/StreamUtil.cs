// Copyright 2025 Crystal Ferrai
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.IO;

namespace IcarusModManager.Core.Utils
{
	/// <summary>
	/// Extension methods for streams
	/// </summary>
	internal static class StreamUtil
	{
		/// <summary>
		/// Read from a stream until either the specified number of bytes has been read or the stream ends.
		/// </summary>
		/// <param name="stream">The stream to read</param>
		/// <param name="buffer">The buffer to read into</param>
		/// <param name="offset">The index into the buffer at which to begin storing bytes read from the stream</param>
		/// <param name="count">The maximum number of bytes to read from the stream</param>
		/// <returns>The number of bytes read from the stream</returns>
		/// <remarks>
		/// Starting in .NET 6, Stream.Read is not guaranteed to read the number of bytes requested. This method
		/// will keep reading from the stream until the bytes have been read or the stream ends, similar to the
		/// behavior of Stream.Read prior to .NET 6. While the behavior change only applies to streams which wrap
		/// other streams, this function should be safe to use with any stream.
		/// See: https://docs.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/6.0/partial-byte-reads-in-streams
		/// </remarks>
		public static int ReadAll(this Stream stream, byte[] buffer, int offset, int count)
		{
			int total = 0;
			while (count > 0)
			{
				int read = stream.Read(buffer, offset, count);
				if (read == 0) return total;
				total += read;
				offset += read;
				count -= read;
			}
			return total;
		}

		/// <summary>
		/// Reads the specified number of bytes from a stream
		/// </summary>
		/// <param name="stream">The stream to read from</param>
		/// <param name="count">The number of bytes to read from the stream</param>
		/// <returns>The bytes that were read</returns>
		public static byte[] ReadBytes(this Stream stream, int count)
		{
			byte[] buffer = new byte[count];
			stream.ReadAll(buffer, 0, count);
			return buffer;
		}
	}
}
