namespace PixelArt.Services;

public class PlayerService
{
    public int Coins { get; private set; }

    public void AddCoins(int coins)
    {
        Coins += coins;
    }
}