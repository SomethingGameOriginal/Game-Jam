using UnityEngine;

public class Poshalka : MonoBehaviour
{
    public GameTimer gameTimer;
    public Sprite WASD;

    public bool Use = false;

    public void Poskhal()
    {
        CanvasTooltip.Show("Промедление смерти подобно! - Не шути со временем!", WASD);
        gameTimer.AddTime(1488);

        Use = false;
    }

    public void FixedUpdate()
    {
        if(gameTimer.GetTime() <= 40)
        {
            if (Use) return;

            Use = true;
            Poskhal();
        }
        
    }
}
