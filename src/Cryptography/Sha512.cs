using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Interop.Libsodium;

namespace NSec.Cryptography
{
    //
    //  SHA-512
    //
    //      FIPS Secure Hash Algorithm (SHA) with a 512-bit message digest
    //
    //  References:
    //
    //      RFC 6234 - US Secure Hash Algorithms (SHA and SHA-based HMAC and
    //          HKDF)
    //
    //      RFC 5754 - Using SHA2 Algorithms with Cryptographic Message Syntax
    //
    //  Parameters:
    //
    //      Input Size - Between 0 and 2^125-1 bytes. (A Span<byte> can only
    //          hold up to 2^31-1 bytes.)
    //
    //      Hash Size - 64 bytes (256 bits of security). The output can be
    //          truncated to 32 bytes (128 bits of security). Note that SHA-512
    //          truncated to 32 bytes/256 bits is not the same as SHA-512/256,
    //          which uses a different initial hash value.
    //
    public sealed class Sha512 : HashAlgorithm
    {
        private static readonly Lazy<bool> s_selfTest = new Lazy<bool>(new Func<bool>(SelfTest));

        public Sha512() : base(
            minHashSize: crypto_hash_sha512_BYTES / 2,
            defaultHashSize: crypto_hash_sha512_BYTES,
            maxHashSize: crypto_hash_sha512_BYTES)
        {
            if (!s_selfTest.Value)
            {
                throw Error.Cryptographic_InitializationFailed(8681.ToString("X"));
            }
        }

        private protected override void HashCore(
            ReadOnlySpan<byte> data,
            Span<byte> hash)
        {
            Debug.Assert(hash.Length >= crypto_hash_sha512_BYTES / 2);
            Debug.Assert(hash.Length <= crypto_hash_sha512_BYTES);

            crypto_hash_sha512_init(out crypto_hash_sha512_state state);
            crypto_hash_sha512_update(ref state, in MemoryMarshal.GetReference(data), (ulong)data.Length);

            // crypto_hash_sha512_final expects an output buffer with a length
            // of exactly crypto_hash_sha512_BYTES. So we need to copy when a
            // truncated output is requested.

            if (hash.Length == crypto_hash_sha512_BYTES)
            {
                crypto_hash_sha512_final(ref state, ref MemoryMarshal.GetReference(hash));
            }
            else
            {
                Span<byte> temp = stackalloc byte[crypto_hash_sha512_BYTES];
                crypto_hash_sha512_final(ref state, ref MemoryMarshal.GetReference(temp));
                temp.Slice(0, hash.Length).CopyTo(hash);
            }
        }

        private protected override bool TryVerifyCore(
            ReadOnlySpan<byte> data,
            ReadOnlySpan<byte> hash)
        {
            Debug.Assert(hash.Length <= crypto_hash_sha512_BYTES);

            Span<byte> temp = stackalloc byte[crypto_hash_sha512_BYTES];

            crypto_hash_sha512_init(out crypto_hash_sha512_state state);
            crypto_hash_sha512_update(ref state, in MemoryMarshal.GetReference(data), (ulong)data.Length);
            crypto_hash_sha512_final(ref state, ref MemoryMarshal.GetReference(temp));

            int result = sodium_memcmp(in MemoryMarshal.GetReference(temp), in MemoryMarshal.GetReference(hash), (UIntPtr)hash.Length);

            return result == 0;
        }

        private static bool SelfTest()
        {
            return (crypto_hash_sha512_bytes() == (UIntPtr)crypto_hash_sha512_BYTES)
                && (crypto_hash_sha512_statebytes() == (UIntPtr)Unsafe.SizeOf<crypto_hash_sha512_state>());
        }
    }
}
