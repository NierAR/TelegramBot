namespace TelegramBot
{
    class Program
    {
        public static void Main()
        {
            BotController Bot = new();
            Bot.Start();
            Console.ReadKey();
        }
    }
}