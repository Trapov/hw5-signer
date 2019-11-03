using System;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Signer.Application
{
    public static class Signers
    {
        public const string Salt = "";
        public static int OverheatedToggle = 0; 

        public static void OverHeatLock()
        {
            while (Interlocked.CompareExchange(ref OverheatedToggle, 1, 0) != 0)
            {
                Console.WriteLine("Over-heated...");
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
        }

        public static void OverHeatUnLock()
        {
            while (Interlocked.CompareExchange(ref OverheatedToggle, 0, 1) != 1)
            {
                Console.WriteLine("Over-heated...");
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
        }

        public static async Task<string> DataSignerMd5(string input)
        {
            OverHeatLock();
            try
            {
                using var signer = MD5.Create();
                var inputBytes = Encoding.UTF8.GetBytes(input + Salt);
                var hash = Encoding.UTF8.GetString(signer.ComputeHash(inputBytes));
                await Task.Delay(TimeSpan.FromMilliseconds(10));
                return hash;
            }
            finally
            {
                OverHeatUnLock();
            }

        }

        public static async Task<string> DataSignerCrc32(string input)
        {
            var inputBytes = Encoding.UTF8.GetBytes(input + Salt);
            var hash = Force.Crc32.Crc32Algorithm.Compute(inputBytes).ToString();
            await Task.Delay(TimeSpan.FromSeconds(1));
            return hash;
        }
    }
}
