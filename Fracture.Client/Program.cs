namespace Fracture.Client
{
    public static class Program
    {
        private static void Main(string[] args)
        {
            using var game = new ShatteredWorldGame();
            
            game.Run();
        }
    }
}