namespace RLBot
{
    class Program
    {
        public static void Main(string[] args)
            => new RLBot().RunAndBlockAsync(args).GetAwaiter().GetResult();
    }
}