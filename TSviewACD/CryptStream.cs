using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace TSviewACD
{
    public class CryptCTR
    {
        static byte[] _salt = Encoding.ASCII.GetBytes("PseudoRandomStream");
        static byte[] _saltnonce = Encoding.ASCII.GetBytes("nonce_salt");
        const int BlockSize = 128;
        const int KeySize = 256;

        static byte[] Key;
        static string _password;
        public static string password
        {
            get { return _password; }
            set
            {
                if (_password == value) return;
                if (value == null) return;
                _password = value;
                Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(_password, _salt);

                Key = key.GetBytes(KeySize / 8);
            }
        }

        class PseudoRandomStream : Stream
        {
            AesCryptoServiceProvider aes;
            ICryptoTransform encryptor;
            byte[] cryptbuf = new byte[BlockSize / 8];
            byte[] counter = new byte[BlockSize / 8];

            long _Length = 0;
            long _Position = 0;

            public PseudoRandomStream(string nonce) : base()
            {
                aes = new AesCryptoServiceProvider();
                aes.BlockSize = BlockSize;
                aes.KeySize = KeySize;
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.None;

                aes.Key = Key;
                encryptor = aes.CreateEncryptor();

                Rfc2898DeriveBytes noncebyte = new Rfc2898DeriveBytes(nonce, _saltnonce);
                Array.Copy(noncebyte.GetBytes((BlockSize - 64) / 8), counter, (BlockSize - 64) / 8);
            }

            private void SetCounter(long count)
            {
                Array.Copy(BitConverter.GetBytes(count), 0, counter, (BlockSize - 64) / 8, sizeof(long));
            }

            public override long Length { get { return _Length; } }
            public override bool CanRead { get { return true; } }
            public override bool CanWrite { get { return false; } }
            public override bool CanSeek { get { return true; } }
            public override void Flush() { /* do nothing */ }

            public override long Position
            {
                get
                {
                    return _Position;
                }
                set
                {
                    if (_Length < value) _Length = value;
                    _Position = value;
                }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                int ret = count;
                while (count > 0)
                {
                    var block = Position / (BlockSize / 8);
                    SetCounter(block);
                    encryptor.TransformBlock(counter, 0, counter.Length, cryptbuf, 0);
                    int srcoffset = (int)(Position - block * (BlockSize / 8));
                    int len = cryptbuf.Length - srcoffset;
                    if (len > count) len = count;
                    Array.Copy(cryptbuf, srcoffset, buffer, offset, len);
                    offset += len;
                    Position += len;
                    count -= len;
                }
                return ret;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                long newOffset = 0;
                switch (origin)
                {
                    case SeekOrigin.Begin:
                        newOffset = offset;
                        break;
                    case SeekOrigin.Current:
                        newOffset = Position + offset;
                        break;
                    case SeekOrigin.End:
                        newOffset = _Length - offset;
                        break;
                }
                Position = newOffset;
                return Position;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                // do nothing
            }

            public override void SetLength(long value)
            {
                _Length = value;
            }
        }


        public class AES256CTR_CryptStream : Stream, IHashStream
        {
            Stream innerStream;
            PseudoRandomStream RandomStream;
            long offset;

            public AES256CTR_CryptStream(Stream baseStream, string nonce, long offset = 0) : base()
            {
                innerStream = baseStream;
                RandomStream = new PseudoRandomStream(nonce);
                this.offset = offset;
                RandomStream.Position = offset;
            }

            public override long Length { get { return innerStream.Length; } }
            public override bool CanRead { get { return true; } }
            public override bool CanWrite { get { return false; } }
            public override bool CanSeek { get { return true; } }
            public override void Flush() { /* do nothing */ }

            public override long Position
            {
                get
                {
                    return innerStream.Position;
                }
                set
                {
                    innerStream.Position = value;
                    RandomStream.Position = value + offset;
                }
            }

            public string Hash
            {
                get
                {
                    return (innerStream as IHashStream).Hash;
                }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                byte[] buf = new byte[count];
                int len = innerStream.Read(buffer, offset, count);
                RandomStream.Read(buf, 0, len);
                for (int i = 0; i < len; i++)
                {
                    buffer[i + offset] ^= buf[i];
                }
                return len;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                long newOffset = 0;
                switch (origin)
                {
                    case SeekOrigin.Begin:
                        newOffset = offset;
                        break;
                    case SeekOrigin.Current:
                        newOffset = Position + offset;
                        break;
                    case SeekOrigin.End:
                        newOffset = Length - offset;
                        break;
                }
                Position = newOffset;
                return Position;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                // do nothing
            }

            public override void SetLength(long value)
            {
                innerStream.SetLength(value);
            }
        }
    }
}
