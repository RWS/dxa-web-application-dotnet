namespace Sdl.Web.Common.Utils
{
    public static class Hash
    {
        public static int CombineHashCodes(params int[] hashcodes)
        {
            int hash1 = (5381 << 16) + 5381;
            int hash2 = hash1;

            int i = 0;
            foreach (var hash in hashcodes)
            {
                if (i % 2 == 0)
                    hash1 = ((hash1 << 5) + hash1 + (hash1 >> 27)) ^ hash;
                else
                    hash2 = ((hash2 << 5) + hash2 + (hash2 >> 27)) ^ hash;

                ++i;
            }

            return hash1 + (hash2 * 1566083941);
        }
    }
}
